using System;
using System.Collections.Generic;
using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.MD {
	/// <summary>
	/// .NET metadata tables stream
	/// </summary>
	public sealed partial class TablesStream : DotNetStream {
		uint reserved1;
		byte majorVersion;
		byte minorVersion;
		MDStreamFlags flags;
		byte log2Rid;
		ulong validMask;
		ulong sortedMask;
		uint extraData;
		MDTable[] mdTables;

		/// <summary>
		/// Gets <see cref="MDStreamFlags"/>
		/// </summary>
		public MDStreamFlags Flags {
			get { return flags; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.BigStrings"/> bit
		/// </summary>
		public bool HasBigStrings {
			get { return (flags & MDStreamFlags.BigStrings) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.BigGUID"/> bit
		/// </summary>
		public bool HasBigGUID {
			get { return (flags & MDStreamFlags.BigGUID) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.BigBlob"/> bit
		/// </summary>
		public bool HasBigBlob {
			get { return (flags & MDStreamFlags.BigBlob) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.Padding"/> bit
		/// </summary>
		public bool HasPadding {
			get { return (flags & MDStreamFlags.Padding) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.DeltaOnly"/> bit
		/// </summary>
		public bool HasDeltaOnly {
			get { return (flags & MDStreamFlags.DeltaOnly) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.ExtraData"/> bit
		/// </summary>
		public bool HasExtraData {
			get { return (flags & MDStreamFlags.ExtraData) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.HasDelete"/> bit
		/// </summary>
		public bool HasDelete {
			get { return (flags & MDStreamFlags.HasDelete) != 0; }
		}

		/// <inheritdoc/>
		public TablesStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}

		/// <summary>
		/// Initialize MD tables
		/// </summary>
		/// <param name="peImage">The PEImage</param>
		public void Initialize(IPEImage peImage) {
			reserved1 = imageStream.ReadUInt32();
			majorVersion = imageStream.ReadByte();
			minorVersion = imageStream.ReadByte();
			flags = (MDStreamFlags)imageStream.ReadByte();
			log2Rid = imageStream.ReadByte();
			validMask = imageStream.ReadUInt64();
			sortedMask = imageStream.ReadUInt64();

			int maxPresentTables;
			var dnTableSizes = new DotNetTableSizes();
			var tableInfos = dnTableSizes.CreateTables(majorVersion, minorVersion, out maxPresentTables);
			mdTables = new MDTable[tableInfos.Length];

			ulong valid = validMask;
			var sizes = new uint[64];
			for (int i = 0; i < 64; valid >>= 1, i++) {
				uint rows = (valid & 1) == 0 ? 0 : imageStream.ReadUInt32();
				if (i >= maxPresentTables)
					rows = 0;
				sizes[i] = rows;
				if (i < mdTables.Length)
					mdTables[i] = new MDTable((Table)i, rows, tableInfos[i]);
			}

			if (HasExtraData)
				extraData = imageStream.ReadUInt32();

			dnTableSizes.InitializeSizes(HasBigStrings, HasBigGUID, HasBigBlob, sizes);

			var currentRva = peImage.ToRVA(imageStream.FileOffset) + (uint)imageStream.Position;
			foreach (var mdTable in mdTables) {
				var dataLen = (long)mdTable.TableInfo.RowSize * (long)mdTable.Rows;
				mdTable.ImageStream = peImage.CreateStream(currentRva, dataLen);
				var newRva = currentRva + (uint)dataLen;
				if (newRva < currentRva)
					throw new BadImageFormatException("Too big MD table");
				currentRva = newRva;
			}
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				if (mdTables != null) {
					foreach (var mdTable in mdTables) {
						if (mdTable != null)
							mdTable.Dispose();
					}
					mdTables = null;
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Returns a MD table
		/// </summary>
		/// <param name="table">The table type</param>
		/// <returns>A <see cref="MDTable"/> or <c>null</c> if table doesn't exist</returns>
		public MDTable Get(Table table) {
			int index = (int)table;
			if ((uint)index >= (uint)mdTables.Length)
				return null;
			return mdTables[index];
		}

		/// <summary>
		/// Checks whether a table exists
		/// </summary>
		/// <param name="table">The table type</param>
		/// <returns><c>true</c> if the table exists</returns>
		public bool HasTable(Table table) {
			return (uint)table < (uint)mdTables.Length;
		}

		/// <summary>
		/// Checks whether table <paramref name="table"/> is sorted
		/// </summary>
		/// <param name="table">The table</param>
		public bool IsSorted(Table table) {
			int index = (int)table;
			if ((uint)index >= 64)
				return false;
			return (sortedMask & (1UL << index)) != 0;
		}
	}
}
