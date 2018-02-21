// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet.MD;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Hot metadata table base class
	/// </summary>
	public abstract class HotTable : IChunk {
		/// <summary>
		/// At most 64K rows can be used when only a partial table is stored in the
		/// hot table. The first level table indexes into the second level table,
		/// and the index is 16 bits.
		/// </summary>
		public const int MAX_ROWS = 0x10000;
		internal const uint HT_ALIGNMENT = 4;

		FileOffset offset;
		RVA rva;

		readonly MetaData metadata;
		internal readonly IMDTable mdTable;
		readonly HotHeapVersion version;
		readonly int hotTableHeaderSize;
		internal readonly int alignedHotTableHeaderSize;
		uint totalLength;

		// full table fields
		byte[] data;

		// partial table fields
		internal List<uint> rids;
		internal Dictionary<uint, byte[]> partialData;
		internal int shift;
		uint mask;
		internal ushort[] firstLevelTable;
		internal byte[] secondLevelTable;
		internal uint dataOffset;
		internal uint firstLevelOffset;
		internal uint secondLevelOffset;

		/// <summary>
		/// <c>true</c> if we can write a partial table, <c>false</c> if we must write
		/// the full table.
		/// </summary>
		public bool CanWritePartialTable {
			get {
				return data == null && rids != null && rids.Count <= MAX_ROWS;
			}
		}

		/// <summary>
		/// Gets the full size of the table
		/// </summary>
		uint FullTableSize {
			get { return (uint)(mdTable.Rows * mdTable.TableInfo.RowSize); }
		}

		/// <summary>
		/// Gets the table type
		/// </summary>
		public Table Table {
			get { return mdTable.Table; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="metadata">Metadata owner</param>
		/// <param name="version">Hot heap version</param>
		/// <param name="mdTable">The MD table</param>
		internal HotTable(MetaData metadata, HotHeapVersion version, IMDTable mdTable) {
			this.metadata = metadata;
			this.mdTable = mdTable;
			this.version = version;

			switch (version) {
			case HotHeapVersion.CLR20:
				hotTableHeaderSize = 0x12;
				break;
			case HotHeapVersion.CLR40:
				hotTableHeaderSize = 0x16;
				break;
			default:
				throw new ArgumentException("Invalid hot heap version");
			}

			this.alignedHotTableHeaderSize = Utils.AlignUp(this.hotTableHeaderSize, HT_ALIGNMENT);
		}

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			mdTable.SetReadOnly();
			if (CanWritePartialTable) {
				InitializePartialData();
				totalLength = CalculatePartialTableLength();
			}
			else
				totalLength = CalculateFullTableLength();
		}

		/// <summary>
		/// Calculates the total size required to write a partial table. It is only called if
		/// the partial table will be written.
		/// </summary>
		internal abstract uint CalculatePartialTableLength();

		/// <summary>
		/// Calculates the total size required to write a full table. It is only called if
		/// the full table will be written.
		/// </summary>
		uint CalculateFullTableLength() {
			return (uint)alignedHotTableHeaderSize + FullTableSize;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return totalLength;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <summary>
		/// Adds a row. This method must be called to write a partial table. If too many rows
		/// are added (see <see cref="MAX_ROWS"/>), then the full table will be written. If this
		/// method is never called, a full table will be written.
		/// </summary>
		/// <param name="rid">The row ID. This must be the new rid, so this method can only be
		/// called after the table row has been assigned a new rid.</param>
		public void Add(uint rid) {
			if (totalLength != 0)
				throw new InvalidOperationException("Can't call Add() after SetOffset() has been called");
			if (partialData != null || data != null)
				throw new InvalidOperationException("Can't add data when full/partial data has been created");
			if (rid == 0 || rid > (uint)mdTable.Rows)
				return;
			if (rids == null)
				rids = new List<uint>();
			rids.Add(rid);
		}

		/// <summary>
		/// Calls the <see cref="IMDTable"/> to write all its rows to a buffer. This is the data
		/// that will be written to this hot table. If this is not explicitly called, it will
		/// be implicitly called later when all data must be written. The table will be set to
		/// read-only. If this method is called, all data will be written to the heap even if
		/// <see cref="Add(uint)"/> has been called.
		/// </summary>
		public void CreateFullData() {
			mdTable.SetReadOnly();
			rids = null;
			if (data != null)
				return;

			data = new byte[FullTableSize];
			var writer = new BinaryWriter(new MemoryStream(data));
			writer.Write(metadata, mdTable);
			if (writer.BaseStream.Position != data.Length)
				throw new InvalidOperationException("Didn't write all MD table data");
		}

		/// <summary>
		/// Creates the partial data of all rows that have been <see cref="Add"/>'d so far.
		/// If a partial table can't be created, <see cref="CreateFullData"/> is automatically
		/// called instead. If this method isn't explicitly called, it will be implicitly called
		/// later when the partial data must be written. The table will be set to read-only.
		/// </summary>
		public void CreatePartialData() {
			mdTable.SetReadOnly();
			if (!CanWritePartialTable) {
				CreateFullData();
				return;
			}
			InitializePartialData();
			var memStream = new MemoryStream(mdTable.TableInfo.RowSize);
			var writer = new BinaryWriter(memStream);
			foreach (var rid in rids) {
				memStream.Position = 0;
				var row = mdTable.Get(rid);
				writer.Write(metadata, mdTable, row);
				partialData[rid] = memStream.ToArray();
			}
		}

		void InitializePartialData() {
			if (partialData != null)
				return;

			partialData = new Dictionary<uint, byte[]>(rids.Count);
			foreach (var rid in rids)
				partialData[rid] = null;
			InitializePartialRids();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			if (CanWritePartialTable)
				PartialWriteTo(writer);
			else
				FullWriteTo(writer);

			firstLevelTable = null;
			secondLevelTable = null;
			partialData = null;
			rids = null;
			data = null;
		}

		/// <summary>
		/// Writes the full table to the hot table
		/// </summary>
		/// <param name="writer">Writer</param>
		void FullWriteTo(BinaryWriter writer) {
			CreateFullData();

			var startPos = writer.BaseStream.Position;
			writer.Write(mdTable.Rows);	// hot records
			writer.Write(0);			// offset of 1st level table
			writer.Write(0);			// offset of 2nd level table
			if (version == HotHeapVersion.CLR40)
				writer.Write(0);		// offset of indexes table
			writer.Write(alignedHotTableHeaderSize);	// offset of hot data (4-byte aligned)
			writer.Write((ushort)0);	// shift count
			writer.WriteZeros(alignedHotTableHeaderSize - (int)(writer.BaseStream.Position - startPos));
			writer.Write(data);
		}

		/// <summary>
		/// Writes the partial table to the hot table
		/// </summary>
		/// <param name="writer">Writer</param>
		internal abstract void PartialWriteTo(BinaryWriter writer);

		static int CountMaxBits(uint val) {
			int bits = 0;
			while (val != 0) {
				val >>= 1;
				bits++;
			}
			return bits;
		}

		void InitializePartialRids() {
			shift = CalculateShift();
			mask = (1U << shift) - 1;
			SortRids();
			CreateFirstAndSecondLevelTables();
		}

		int CalculateShift() {
			mdTable.SetReadOnly();
			int maxBits = CountMaxBits((uint)mdTable.Rows);
			if (maxBits >= 16)
				return maxBits - 8;
			else
				return maxBits / 2;
		}

		void SortRids() {
			// Make sure dupes are removed
			rids.Clear();
			rids.AddRange(partialData.Keys);

			rids.Sort((a, b) => {
				uint la = a & mask;
				uint lb = b & mask;
				if (la != lb)
					return la.CompareTo(lb);
				return (a >> shift).CompareTo(b >> shift);
			});
		}

		void CreateFirstAndSecondLevelTables() {
			// rids has already been sorted, first on lower bits, then on upper bits
			firstLevelTable = new ushort[(1 << shift) + 1];
			int prevRid = 0, i2 = 0;
			for (; i2 < partialData.Count; i2++) {
				int rid = (int)(rids[i2] & mask);
				while (prevRid <= rid)
					firstLevelTable[prevRid++] = (ushort)i2;
			}
			while (prevRid < firstLevelTable.Length)
				firstLevelTable[prevRid++] = (ushort)i2;

			secondLevelTable = new byte[partialData.Count];
			for (int i = 0; i < secondLevelTable.Length; i++)
				secondLevelTable[i] = (byte)(rids[i] >> shift);
		}

		/// <summary>
		/// Writes the data
		/// </summary>
		/// <param name="writer">Writer</param>
		internal void WritePartialData(BinaryWriter writer) {
			foreach (var rid in rids)
				writer.Write(partialData[rid]);
		}

		/// <summary>
		/// Writes the first level table
		/// </summary>
		/// <param name="writer">Writer</param>
		internal void WriteFirstLevelTable(BinaryWriter writer) {
			foreach (var s in firstLevelTable)
				writer.Write(s);
		}

		/// <summary>
		/// Writes the second level table
		/// </summary>
		/// <param name="writer">Writer</param>
		internal void WriteSecondLevelTable(BinaryWriter writer) {
			writer.Write(secondLevelTable);
		}
	}

	/// <summary>
	/// CLR 2.0 (.NET 2.0 - 3.5) hot table
	/// </summary>
	sealed class HotTable20 : HotTable {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="metadata">Metadata owner</param>
		/// <param name="mdTable">The MD table</param>
		public HotTable20(MetaData metadata, IMDTable mdTable)
			: base(metadata, HotHeapVersion.CLR20, mdTable) {
		}

		/// <inheritdoc/>
		internal override uint CalculatePartialTableLength() {
			uint len = 0;

			len += (uint)alignedHotTableHeaderSize;

			// Data
			len = Utils.AlignUp(len, HT_ALIGNMENT);
			dataOffset = len;
			len += (uint)(partialData.Count * mdTable.TableInfo.RowSize);

			// First level table
			len = Utils.AlignUp(len, HT_ALIGNMENT);
			firstLevelOffset = len;
			len += (uint)(firstLevelTable.Length * 2);

			// Second level table
			len = Utils.AlignUp(len, HT_ALIGNMENT);
			secondLevelOffset = len;
			len += (uint)secondLevelTable.Length;

			return len;
		}

		/// <inheritdoc/>
		internal override void PartialWriteTo(BinaryWriter writer) {
			var startPos = writer.BaseStream.Position;
			writer.Write(partialData.Count);// hot records
			writer.Write(firstLevelOffset);	// any alignment, all bits are used
			writer.Write(secondLevelOffset);// any alignment, all bits are used
			writer.Write(dataOffset);	// any alignment, all bits are used
			writer.Write((ushort)shift);// shift count
			writer.WriteZeros(alignedHotTableHeaderSize - (int)(writer.BaseStream.Position - startPos));

			uint offs;

			// Data
			offs = (uint)(writer.BaseStream.Position - startPos);
			writer.WriteZeros((int)(dataOffset - offs));
			WritePartialData(writer);

			// First level table
			offs = (uint)(writer.BaseStream.Position - startPos);
			writer.WriteZeros((int)(firstLevelOffset - offs));
			WriteFirstLevelTable(writer);

			// Second level table
			offs = (uint)(writer.BaseStream.Position - startPos);
			writer.WriteZeros((int)(secondLevelOffset - offs));
			WriteSecondLevelTable(writer);
		}
	}

	/// <summary>
	/// CLR 4.0 (.NET 4.0 - 4.5) partial hot table
	/// </summary>
	sealed class HotTable40 : HotTable {
		uint indexesOffset;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="metadata">Metadata owner</param>
		/// <param name="mdTable">The MD table</param>
		public HotTable40(MetaData metadata, IMDTable mdTable)
			: base(metadata, HotHeapVersion.CLR40, mdTable) {
		}

		/// <inheritdoc/>
		internal override uint CalculatePartialTableLength() {
			uint len = 0;

			len += (uint)alignedHotTableHeaderSize;

			// Data
			len = Utils.AlignUp(len, HT_ALIGNMENT);
			dataOffset = len;
			len += (uint)(partialData.Count * mdTable.TableInfo.RowSize);

			// First level table
			len = Utils.AlignUp(len, HT_ALIGNMENT);
			firstLevelOffset = len;
			len += (uint)(firstLevelTable.Length * 2);

			// Second level table
			len = Utils.AlignUp(len, HT_ALIGNMENT);
			secondLevelOffset = len;
			len += (uint)secondLevelTable.Length;

			// Indexes table
			len = Utils.AlignUp(len, HT_ALIGNMENT);
			indexesOffset = len;
			len += (uint)(partialData.Count * 2);

			return len;
		}

		/// <inheritdoc/>
		internal override void PartialWriteTo(BinaryWriter writer) {
			var startPos = writer.BaseStream.Position;
			writer.Write(partialData.Count);// hot records
			writer.Write(firstLevelOffset);	// any alignment, all bits are used
			writer.Write(secondLevelOffset);// any alignment, all bits are used
			writer.Write(indexesOffset);// any alignment, all bits are used
			writer.Write(dataOffset);	// any alignment, all bits are used
			writer.Write((ushort)shift);// shift count
			writer.WriteZeros(alignedHotTableHeaderSize - (int)(writer.BaseStream.Position - startPos));

			uint offs;

			// Data
			offs = (uint)(writer.BaseStream.Position - startPos);
			writer.WriteZeros((int)(dataOffset - offs));
			WritePartialData(writer);

			// First level table
			offs = (uint)(writer.BaseStream.Position - startPos);
			writer.WriteZeros((int)(firstLevelOffset - offs));
			WriteFirstLevelTable(writer);

			// Second level table
			offs = (uint)(writer.BaseStream.Position - startPos);
			writer.WriteZeros((int)(secondLevelOffset - offs));
			WriteSecondLevelTable(writer);

			// Indexes table
			offs = (uint)(writer.BaseStream.Position - startPos);
			writer.WriteZeros((int)(indexesOffset - offs));
			WriteIndexesTable(writer);
		}

		void WriteIndexesTable(BinaryWriter writer) {
			for (int i = 0; i < partialData.Count; i++)
				writer.Write((ushort)i);
		}
	}
}
