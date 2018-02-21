// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet.MD;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Hot pool
	/// </summary>
	abstract class HotPool : IChunk {
		internal const uint HP_ALIGNMENT = 4;
		FileOffset offset;
		RVA rva;
		readonly HeapType heapType;
		Dictionary<uint, byte[]> allData;
		internal DataInfo[] dataList;
		int[] sortedIndexes;
		internal uint dataOffset;
		internal uint indexesOffset;
		internal uint ridsOffset;
		internal uint headerOffset;
		uint totalLength;
		bool indexesIsSorted;

		internal class DataInfo {
			public readonly uint HeapOffset;
			public uint PoolOffset;
			public readonly byte[] Data;
			public DataInfo(uint heapOffset, byte[] data) {
				this.HeapOffset = heapOffset;
				this.Data = data;
			}
		}

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Gets the offset of the data relative to the start of this chunk. This is valid only
		/// after <see cref="SetOffset(FileOffset,RVA)"/> has been called.
		/// </summary>
		public uint DataOffset {
			get { return dataOffset; }
		}

		/// <summary>
		/// Gets the offset of the indexes relative to the start of this chunk. This is valid only
		/// after <see cref="SetOffset(FileOffset,RVA)"/> has been called.
		/// </summary>
		public uint IndexesOffset {
			get { return indexesOffset; }
		}

		/// <summary>
		/// Gets the offset of the rids relative to the start of this chunk. This is valid only
		/// after <see cref="SetOffset(FileOffset,RVA)"/> has been called.
		/// </summary>
		public uint RidsOffset {
			get { return ridsOffset; }
		}

		/// <summary>
		/// Gets the offset of the header relative to the start of this chunk. This is valid only
		/// after <see cref="SetOffset(FileOffset,RVA)"/> has been called.
		/// </summary>
		public uint HeaderOffset {
			get { return headerOffset; }
		}

		/// <summary>
		/// Gets the pool type
		/// </summary>
		public HeapType HeapType {
			get { return heapType; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="heapType">Pool type</param>
		internal HotPool(HeapType heapType) {
			this.heapType = heapType;
			this.allData = new Dictionary<uint, byte[]>();
		}

		/// <summary>
		/// Adds raw data
		/// </summary>
		/// <param name="offset">Offset of data in the original heap. If it's the #GUID, this
		/// should be <c>(index - 1) * 16</c>.</param>
		/// <param name="rawData"></param>
		public void Add(uint offset, byte[] rawData) {
			if (dataList != null)
				throw new InvalidOperationException("Can't Add() after CreateData() has been called");
			allData[offset] = rawData;
		}

		/// <summary>
		/// Creates <see cref="dataList"/> if it hasn't been created yet
		/// </summary>
		void CreateData() {
			if (dataList != null)
				return;

			dataList = new DataInfo[allData.Count];
			int i = 0;
			foreach (var kv in allData)
				dataList[i++] = new DataInfo(kv.Key, kv.Value);
			// Make sure it's sorted by heap offset
			Array.Sort(dataList, (a, b) => a.HeapOffset.CompareTo(b.HeapOffset));

			sortedIndexes = new int[allData.Count];
			for (i = 0; i < sortedIndexes.Length; i++)
				sortedIndexes[i] = i;

			allData = null;
			indexesIsSorted = false;
		}

		/// <summary>
		/// Creates the data and sorts it according to the original data's heap offsets
		/// </summary>
		public void SortData() {
			CreateData();
			Array.Sort(sortedIndexes, (a, b) => a.CompareTo(b));
			indexesIsSorted = true;
		}

		/// <summary>
		/// Creates the data and shuffles it
		/// </summary>
		public void ShuffleData() {
			ShuffleData(new Random());
		}

		/// <summary>
		/// Creates the data and shuffles it
		/// </summary>
		/// <param name="rand">Random number generator instance that should be used</param>
		public void ShuffleData(Random rand) {
			CreateData();

			int start = 0, len = sortedIndexes.Length;
			GetValidShuffleRange(ref start, ref len);
			Shuffle(rand, sortedIndexes, start, len);

			indexesIsSorted = true;
		}

		/// <summary>
		/// Returns the range that can be shuffled
		/// </summary>
		/// <param name="start">Start index</param>
		/// <param name="length">Length</param>
		internal virtual void GetValidShuffleRange(ref int start, ref int length) {
		}

		static void Shuffle<T>(Random rand, IList<T> list, int start, int count) {
			if (list == null || count <= 1)
				return;

			// Fisher-Yates algo, see http://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
			for (int i = count - 1; i > 0; i--) {
				int j = rand.Next(i + 1);
				T tmp = list[start + i];
				list[start + i] = list[start + j];
				list[start + j] = tmp;
			}
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			CreateData();
			totalLength = SetOffsetImpl();
		}

		/// <summary>
		/// Initializes all offsets
		/// </summary>
		/// <returns>Total size of data</returns>
		internal abstract uint SetOffsetImpl();

		internal uint GetDataSize() {
			uint size = 0;
			foreach (var data in dataList)
				size += (uint)data.Data.Length;
			return size;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return totalLength;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			// Sort it unless it's already been sorted. Don't want random order
			// unless the user wants it.
			if (!indexesIsSorted)
				SortData();

			WriteToImpl(writer);
			dataList = null;
			sortedIndexes = null;
		}

		/// <summary>
		/// Writes all data
		/// </summary>
		internal abstract void WriteToImpl(BinaryWriter writer);

		internal DataInfo[] GetPoolDataOrder() {
			var dataList2 = (DataInfo[])dataList.Clone();
			Array.Sort(sortedIndexes, dataList2);
			uint offset = 0;
			foreach (var info in dataList2) {
				info.PoolOffset = offset;
				offset += (uint)info.Data.Length;
			}
			return dataList2;
		}
	}

	/// <summary>
	/// CLR 2.0 (.NET 2.0 - 3.5) hot pool writer
	/// </summary>
	sealed class HotPool20 : HotPool {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="heapType">Pool type</param>
		public HotPool20(HeapType heapType)
			: base(heapType) {
		}

		/// <inheritdoc/>
		internal override void GetValidShuffleRange(ref int start, ref int length) {
			// First one must always be first
			start++;
			length--;
		}

		/// <inheritdoc/>
		internal override uint SetOffsetImpl() {
			uint offs = 0;

			// data can be anywhere except where rids can be
			offs = Utils.AlignUp(offs, HP_ALIGNMENT);
			dataOffset = offs;
			offs += GetDataSize();

			// indexes can be anywhere except where rids can be
			offs = Utils.AlignUp(offs, HP_ALIGNMENT);
			indexesOffset = offs;
			offs += ((uint)dataList.Length - 1) * 4;

			// rids must be right before the header
			offs = Utils.AlignUp(offs, HP_ALIGNMENT);
			ridsOffset = offs;
			offs += (uint)dataList.Length * 4;

			offs = Utils.AlignUp(offs, HP_ALIGNMENT);
			headerOffset = offs;
			offs += 3 * 4;

			return offs;
		}

		/// <inheritdoc/>
		internal override void WriteToImpl(BinaryWriter writer) {
			uint offs = 0;
			long startPos = writer.BaseStream.Position;
			var dataList2 = GetPoolDataOrder();

			// Write data
			writer.WriteZeros((int)(dataOffset - offs));
			foreach (var kv in dataList2)
				writer.Write(kv.Data);
			offs = (uint)(writer.BaseStream.Position - startPos);

			// Write indexes (hot pool offsets)
			writer.WriteZeros((int)(indexesOffset - offs));
			if (dataList.Length > 1) {
				for (int i = 1; i < dataList.Length; i++)
					writer.Write(dataList[i].PoolOffset);
			}
			offs = (uint)(writer.BaseStream.Position - startPos);

			// Write rids (heap offsets)
			writer.WriteZeros((int)(ridsOffset - offs));
			foreach (var kv in dataList)
				writer.Write(kv.HeapOffset);
			offs = (uint)(writer.BaseStream.Position - startPos);

			// Write header
			writer.WriteZeros((int)(headerOffset - offs));
			writer.Write(headerOffset - dataOffset);	// any alignment
			writer.Write(headerOffset - indexesOffset);	// low 2 bits are ignored
			writer.Write(headerOffset - ridsOffset);	// low 2 bits are ignored
		}
	}

	/// <summary>
	/// CLR 4.0 (.NET 4.0 - 4.5) hot pool writer
	/// </summary>
	sealed class HotPool40 : HotPool {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="heapType">Pool type</param>
		public HotPool40(HeapType heapType)
			: base(heapType) {
		}

		/// <inheritdoc/>
		internal override uint SetOffsetImpl() {
			uint offs = 0;

			// data can be anywhere except where rids can be
			offs = Utils.AlignUp(offs, HP_ALIGNMENT);
			dataOffset = offs;
			offs += GetDataSize();

			// indexes can be anywhere except where rids can be
			offs = Utils.AlignUp(offs, HP_ALIGNMENT);
			indexesOffset = offs;
			offs += (uint)dataList.Length * 4;

			// rids must be right before the header
			offs = Utils.AlignUp(offs, HP_ALIGNMENT);
			ridsOffset = offs;
			offs += (uint)dataList.Length * 4;

			offs = Utils.AlignUp(offs, HP_ALIGNMENT);
			headerOffset = offs;
			offs += 3 * 4;

			return offs;
		}

		/// <inheritdoc/>
		internal override void WriteToImpl(BinaryWriter writer) {
			uint offs = 0;
			long startPos = writer.BaseStream.Position;
			var dataList2 = GetPoolDataOrder();

			// Write data
			writer.WriteZeros((int)(dataOffset - offs));
			foreach (var info in dataList2)
				writer.Write(info.Data);
			offs = (uint)(writer.BaseStream.Position - startPos);

			// Write indexes (hot pool offsets)
			writer.WriteZeros((int)(indexesOffset - offs));
			foreach (var info in dataList)
				writer.Write(info.PoolOffset);
			offs = (uint)(writer.BaseStream.Position - startPos);

			// Write rids (heap offsets)
			writer.WriteZeros((int)(ridsOffset - offs));
			foreach (var kv in dataList)
				writer.Write(kv.HeapOffset);
			offs = (uint)(writer.BaseStream.Position - startPos);

			// Write header
			writer.WriteZeros((int)(headerOffset - offs));
			writer.Write(headerOffset - ridsOffset);	// must be 4-byte aligned
			writer.Write(headerOffset - indexesOffset);	// must be 4-byte aligned
			writer.Write(headerOffset - dataOffset);	// any alignment
		}
	}
}
