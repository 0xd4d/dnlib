// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.IO;
using dnlib.PE;
using dnlib.W32Resources;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Writes Win32 resources
	/// </summary>
	public sealed class Win32ResourcesChunk : IReuseChunk {
		readonly Win32Resources win32Resources;
		FileOffset offset;
		RVA rva;
		uint length;
		readonly Dictionary<ResourceDirectory, uint> dirDict = new Dictionary<ResourceDirectory, uint>();
		readonly List<ResourceDirectory> dirList = new List<ResourceDirectory>();
		readonly Dictionary<ResourceData, uint> dataHeaderDict = new Dictionary<ResourceData, uint>();
		readonly List<ResourceData> dataHeaderList = new List<ResourceData>();
		readonly Dictionary<string, uint> stringsDict = new Dictionary<string, uint>(StringComparer.Ordinal);
		readonly List<string> stringsList = new List<string>();
		readonly Dictionary<ResourceData, uint> dataDict = new Dictionary<ResourceData, uint>();
		readonly List<ResourceData> dataList = new List<ResourceData>();
		readonly List<DataBlock> dataBlocks = new List<DataBlock>();

		// A range of bytes that is written once. All resource data whose bytes overlap in the
		// same source are written as one block, so data shared by many entries (eg. many
		// entries pointing at the same data header) isn't written more than once.
		sealed class DataBlock {
			public readonly DataReaderFactory Factory;
			public readonly uint Start;
			public uint End;
			public uint Offset;
			public bool Added;
			// true if the block has data that doesn't start at the block's start
			public bool HasInnerStarts;
			public DataBlock(DataReaderFactory factory, uint start, uint end) {
				Factory = factory;
				Start = start;
				End = end;
			}
		}

		sealed class ReferenceComparer : IEqualityComparer<DataReaderFactory> {
			public static readonly ReferenceComparer Instance = new ReferenceComparer();
			public bool Equals(DataReaderFactory? x, DataReaderFactory? y) => (object?)x == y;
			public int GetHashCode(DataReaderFactory obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
		}

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="win32Resources">Win32 resources</param>
		public Win32ResourcesChunk(Win32Resources win32Resources) => this.win32Resources = win32Resources;

		/// <summary>
		/// Returns the <see cref="FileOffset"/> and <see cref="RVA"/> of a
		/// <see cref="ResourceDirectoryEntry"/>. <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="dirEntry">A <see cref="ResourceDirectoryEntry"/></param>
		/// <param name="fileOffset">Updated with the file offset</param>
		/// <param name="rva">Updated with the RVA</param>
		/// <returns><c>true</c> if <paramref name="dirEntry"/> is valid and
		/// <paramref name="fileOffset"/> and <paramref name="rva"/> have been updated. <c>false</c>
		/// if <paramref name="dirEntry"/> is not part of the Win32 resources.</returns>
		public bool GetFileOffsetAndRvaOf(ResourceDirectoryEntry? dirEntry, out FileOffset fileOffset, out RVA rva) {
			if (dirEntry is ResourceDirectory dir)
				return GetFileOffsetAndRvaOf(dir, out fileOffset, out rva);

			if (dirEntry is ResourceData dataHeader)
				return GetFileOffsetAndRvaOf(dataHeader, out fileOffset, out rva);

			fileOffset = 0;
			rva = 0;
			return false;
		}

		/// <summary>
		/// Returns the <see cref="FileOffset"/> of a <see cref="ResourceDirectoryEntry"/>.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="dirEntry">A <see cref="ResourceDirectoryEntry"/></param>
		/// <returns>The file offset or 0 if <paramref name="dirEntry"/> is invalid</returns>
		public FileOffset GetFileOffset(ResourceDirectoryEntry? dirEntry) {
			GetFileOffsetAndRvaOf(dirEntry, out var fileOffset, out var rva);
			return fileOffset;
		}

		/// <summary>
		/// Returns the <see cref="RVA"/> of a <see cref="ResourceDirectoryEntry"/>.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="dirEntry">A <see cref="ResourceDirectoryEntry"/></param>
		/// <returns>The RVA or 0 if <paramref name="dirEntry"/> is invalid</returns>
		public RVA GetRVA(ResourceDirectoryEntry? dirEntry) {
			GetFileOffsetAndRvaOf(dirEntry, out var fileOffset, out var rva);
			return rva;
		}

		/// <summary>
		/// Returns the <see cref="FileOffset"/> and <see cref="RVA"/> of a
		/// <see cref="ResourceDirectory"/>. <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="dir">A <see cref="ResourceDirectory"/></param>
		/// <param name="fileOffset">Updated with the file offset</param>
		/// <param name="rva">Updated with the RVA</param>
		/// <returns><c>true</c> if <paramref name="dir"/> is valid and
		/// <paramref name="fileOffset"/> and <paramref name="rva"/> have been updated. <c>false</c>
		/// if <paramref name="dir"/> is not part of the Win32 resources.</returns>
		public bool GetFileOffsetAndRvaOf(ResourceDirectory? dir, out FileOffset fileOffset, out RVA rva) {
			if (dir is null || !dirDict.TryGetValue(dir, out uint offs)) {
				fileOffset = 0;
				rva = 0;
				return false;
			}

			fileOffset = offset + offs;
			rva = this.rva + offs;
			return true;
		}

		/// <summary>
		/// Returns the <see cref="FileOffset"/> of a <see cref="ResourceDirectory"/>.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="dir">A <see cref="ResourceDirectory"/></param>
		/// <returns>The file offset or 0 if <paramref name="dir"/> is invalid</returns>
		public FileOffset GetFileOffset(ResourceDirectory? dir) {
			GetFileOffsetAndRvaOf(dir, out var fileOffset, out var rva);
			return fileOffset;
		}

		/// <summary>
		/// Returns the <see cref="RVA"/> of a <see cref="ResourceDirectory"/>.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="dir">A <see cref="ResourceDirectory"/></param>
		/// <returns>The RVA or 0 if <paramref name="dir"/> is invalid</returns>
		public RVA GetRVA(ResourceDirectory? dir) {
			GetFileOffsetAndRvaOf(dir, out var fileOffset, out var rva);
			return rva;
		}

		/// <summary>
		/// Returns the <see cref="FileOffset"/> and <see cref="RVA"/> of a
		/// <see cref="ResourceData"/>. <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="dataHeader">A <see cref="ResourceData"/></param>
		/// <param name="fileOffset">Updated with the file offset</param>
		/// <param name="rva">Updated with the RVA</param>
		/// <returns><c>true</c> if <paramref name="dataHeader"/> is valid and
		/// <paramref name="fileOffset"/> and <paramref name="rva"/> have been updated. <c>false</c>
		/// if <paramref name="dataHeader"/> is not part of the Win32 resources.</returns>
		public bool GetFileOffsetAndRvaOf(ResourceData? dataHeader, out FileOffset fileOffset, out RVA rva) {
			if (dataHeader is null || !dataHeaderDict.TryGetValue(dataHeader, out uint offs)) {
				fileOffset = 0;
				rva = 0;
				return false;
			}

			fileOffset = offset + offs;
			rva = this.rva + offs;
			return true;
		}

		/// <summary>
		/// Returns the <see cref="FileOffset"/> of a <see cref="ResourceData"/>.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="dataHeader">A <see cref="ResourceData"/></param>
		/// <returns>The file offset or 0 if <paramref name="dataHeader"/> is invalid</returns>
		public FileOffset GetFileOffset(ResourceData? dataHeader) {
			GetFileOffsetAndRvaOf(dataHeader, out var fileOffset, out var rva);
			return fileOffset;
		}

		/// <summary>
		/// Returns the <see cref="RVA"/> of a <see cref="ResourceData"/>.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="dataHeader">A <see cref="ResourceData"/></param>
		/// <returns>The RVA or 0 if <paramref name="dataHeader"/> is invalid</returns>
		public RVA GetRVA(ResourceData? dataHeader) {
			GetFileOffsetAndRvaOf(dataHeader, out var fileOffset, out var rva);
			return rva;
		}

		/// <summary>
		/// Returns the <see cref="FileOffset"/> and <see cref="RVA"/> of a
		/// <see cref="ResourceDirectoryEntry"/>'s name. <see cref="SetOffset"/> must have been
		/// called.
		/// </summary>
		/// <param name="name">The name of a <see cref="ResourceDirectoryEntry"/></param>
		/// <param name="fileOffset">Updated with the file offset</param>
		/// <param name="rva">Updated with the RVA</param>
		/// <returns><c>true</c> if <paramref name="name"/> is valid and
		/// <paramref name="fileOffset"/> and <paramref name="rva"/> have been updated. <c>false</c>
		/// if <paramref name="name"/> is not part of the Win32 resources.</returns>
		public bool GetFileOffsetAndRvaOf(string name, out FileOffset fileOffset, out RVA rva) {
			if (name is null || !stringsDict.TryGetValue(name, out uint offs)) {
				fileOffset = 0;
				rva = 0;
				return false;
			}

			fileOffset = offset + offs;
			rva = this.rva + offs;
			return true;
		}

		/// <summary>
		/// Returns the <see cref="FileOffset"/> of a <see cref="ResourceDirectoryEntry"/>'s name.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="name">The name of a <see cref="ResourceDirectoryEntry"/></param>
		/// <returns>The file offset or 0 if <paramref name="name"/> is invalid</returns>
		public FileOffset GetFileOffset(string name) {
			GetFileOffsetAndRvaOf(name, out var fileOffset, out var rva);
			return fileOffset;
		}

		/// <summary>
		/// Returns the <see cref="RVA"/> of a <see cref="ResourceDirectoryEntry"/>'s name.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="name">The name of a <see cref="ResourceDirectoryEntry"/></param>
		/// <returns>The RVA or 0 if <paramref name="name"/> is invalid</returns>
		public RVA GetRVA(string name) {
			GetFileOffsetAndRvaOf(name, out var fileOffset, out var rva);
			return rva;
		}

		const uint RESOURCE_DIR_ALIGNMENT = 4;
		const uint RESOURCE_DATA_HEADER_ALIGNMENT = 4;
		const uint RESOURCE_STRING_ALIGNMENT = 2;
		const uint RESOURCE_DATA_ALIGNMENT = 4;

		bool IReuseChunk.CanReuse(RVA origRva, uint origSize) {
			Debug.Assert(rva != 0);
			if (rva == 0)
				throw new InvalidOperationException();
			return length <= origSize;
		}

		internal bool CheckValidOffset(FileOffset offset) {
			GetMaxAlignment(offset, out var error);
			return error is null;
		}

		static uint GetMaxAlignment(FileOffset offset, out string? error) {
			error = null;
			uint maxAlignment = 1;
			maxAlignment = Math.Max(maxAlignment, RESOURCE_DIR_ALIGNMENT);
			maxAlignment = Math.Max(maxAlignment, RESOURCE_DATA_HEADER_ALIGNMENT);
			maxAlignment = Math.Max(maxAlignment, RESOURCE_STRING_ALIGNMENT);
			maxAlignment = Math.Max(maxAlignment, RESOURCE_DATA_ALIGNMENT);
			if (((uint)offset & (maxAlignment - 1)) != 0)
				error = $"Win32 resources section isn't {maxAlignment}-byte aligned";
			else if (maxAlignment > ModuleWriterBase.DEFAULT_WIN32_RESOURCES_ALIGNMENT)
				error = "maxAlignment > DEFAULT_WIN32_RESOURCES_ALIGNMENT";
			return maxAlignment;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			// NOTE: This method can be called twice by NativeModuleWriter, see Metadata.SetOffset() for more info
			bool initAll = this.offset == 0;
			this.offset = offset;
			this.rva = rva;
			if (win32Resources is null)
				return;

			if (!initAll) {
				// If it's called a second time, re-do everything
				dirDict.Clear();
				dirList.Clear();
				dataHeaderDict.Clear();
				dataHeaderList.Clear();
				stringsDict.Clear();
				stringsList.Clear();
				dataDict.Clear();
				dataList.Clear();
				dataBlocks.Clear();
			}

			FindDirectoryEntries();

			// Place everything in the following order:
			//	1. All resource directories. The root is always first.
			//	2. All resource data headers.
			//	3. All the strings.
			//	4. All resource data.

			// A ulong can't overflow here: every list has less than 2^31 elements and every element is smaller than 2^33 bytes
			ulong rsrcOffset = 0;
			var maxAlignment = GetMaxAlignment(offset, out var error);
			if (error is not null)
				throw new ModuleWriterException(error);

			foreach (var dir in dirList) {
				rsrcOffset = AlignUp(rsrcOffset, RESOURCE_DIR_ALIGNMENT);
				dirDict[dir] = (uint)rsrcOffset;
				if (dir != dirList[0])
					AddString(dir.Name);
				rsrcOffset += 16 + ((ulong)dir.Directories.Count + (ulong)dir.Data.Count) * 8;
			}

			foreach (var data in dataHeaderList) {
				rsrcOffset = AlignUp(rsrcOffset, RESOURCE_DATA_HEADER_ALIGNMENT);
				dataHeaderDict[data] = (uint)rsrcOffset;
				AddString(data.Name);
				AddData(data);
				rsrcOffset += 16;
			}

			foreach (var s in stringsList) {
				rsrcOffset = AlignUp(rsrcOffset, RESOURCE_STRING_ALIGNMENT);
				stringsDict[s] = (uint)rsrcOffset;
				rsrcOffset += 2 + (ulong)s.Length * 2;
			}

			var dataToBlock = CreateDataBlocks(out var dataStarts);
			foreach (var block in dataBlocks) {
				rsrcOffset = GetBlockOffset(rsrcOffset, block);
				block.Offset = (uint)rsrcOffset;
				rsrcOffset += block.End - block.Start;
			}
			for (int i = 0; i < dataList.Count; i++) {
				var block = dataToBlock[i];
				// Can wrap if the total is too big but then we throw below
				dataDict[dataList[i]] = block.Offset + (dataStarts[i] - block.Start);
			}

			// All offsets and the length are 32-bit values. If the total doesn't fit, the offsets
			// stored above have wrapped and are invalid.
			if (rsrcOffset > uint.MaxValue)
				throw new ModuleWriterException("Win32 resources are too big");

			length = (uint)rsrcOffset;
		}

		DataBlock[] CreateDataBlocks(out uint[] dataStarts) {
			int count = dataList.Count;
			var dataToBlock = new DataBlock[count];
			dataStarts = new uint[count];
			var dataEnds = new uint[count];
			var factories = new DataReaderFactory[count];
			var indexes = new int[count];
			var factoryIds = new Dictionary<DataReaderFactory, int>(ReferenceComparer.Instance);
			var factoryIdOf = new int[count];
			for (int i = 0; i < count; i++) {
				var data = dataList[i];
				var reader = data.CreateReader();
				// Only possible with a custom DataReaderFactory that creates an invalid reader
				if (reader.EndOffset < reader.StartOffset)
					throw new ModuleWriterException("Invalid Win32 resource data reader");
				factories[i] = data.DataReaderFactory;
				dataStarts[i] = reader.StartOffset;
				dataEnds[i] = reader.EndOffset;
				if (!factoryIds.TryGetValue(factories[i], out int id))
					factoryIds.Add(factories[i], id = factoryIds.Count);
				factoryIdOf[i] = id;
				indexes[i] = i;
			}

			// Sort by source and start offset, then merge overlapping ranges
			var starts = dataStarts;
			Array.Sort(indexes, (a, b) => {
				int c = factoryIdOf[a].CompareTo(factoryIdOf[b]);
				if (c != 0)
					return c;
				c = starts[a].CompareTo(starts[b]);
				if (c != 0)
					return c;
				return a.CompareTo(b);
			});
			DataBlock? current = null;
			foreach (int i in indexes) {
				// Ranges that only touch aren't merged so normal files (no shared data) are written
				// exactly like before. Empty ranges at the start of a block are part of the block.
				if (current is not null && current.Factory == factories[i] &&
					(dataStarts[i] < current.End || dataStarts[i] == current.Start)) {
					if (dataEnds[i] > current.End)
						current.End = dataEnds[i];
					if (dataStarts[i] != current.Start)
						current.HasInnerStarts = true;
				}
				else
					current = new DataBlock(factories[i], dataStarts[i], dataEnds[i]);
				dataToBlock[i] = current;
			}

			// Keep the original order (first use of each block)
			for (int i = 0; i < count; i++) {
				var block = dataToBlock[i];
				if (block.Added)
					continue;
				block.Added = true;
				dataBlocks.Add(block);
			}
			return dataToBlock;
		}

		// Data inside a block keeps its distance from the block start. If the block has data that
		// starts after the block start, the block is placed so its offset mod 4 is the same as in
		// the source, so all its data keep their source alignment. Other blocks are 4-byte aligned
		// like before.
		static ulong GetBlockOffset(ulong offset, DataBlock block) {
			offset = AlignUp(offset, RESOURCE_DATA_ALIGNMENT);
			if (block.HasInnerStarts)
				offset += (block.Start - offset) & (RESOURCE_DATA_ALIGNMENT - 1);
			return offset;
		}

		static ulong AlignUp(ulong v, uint alignment) => (v + alignment - 1) & ~(ulong)(alignment - 1);

		void AddData(ResourceData data) {
			if (dataDict.ContainsKey(data))
				return;
			dataList.Add(data);
			dataDict.Add(data, 0);
		}

		void AddString(ResourceName name) {
			if (!name.HasName || stringsDict.ContainsKey(name.Name))
				return;
			stringsList.Add(name.Name);
			stringsDict.Add(name.Name, 0);
		}

		void FindDirectoryEntries() {
			var root = win32Resources.Root;
			if (root is null)
				throw new ModuleWriterException("Win32 resources root directory is null");
			FindDirectoryEntries(root);
		}

		void FindDirectoryEntries(ResourceDirectory dir) {
			if (dir is null)
				throw new ModuleWriterException("A Win32 resource directory is null");
			if (dirDict.ContainsKey(dir))
				return;
			dirList.Add(dir);
			dirDict[dir] = 0;
			var dirs = dir.Directories;
			int count = dirs.Count;
			for (int i = 0; i < count; i++)
				FindDirectoryEntries(dirs[i]);
			var dirData = dir.Data;
			count = dirData.Count;
			for (int i = 0; i < count; i++) {
				var data = dirData[i];
				if (data is null)
					throw new ModuleWriterException("A Win32 resource data entry is null");
				if (dataHeaderDict.ContainsKey(data))
					continue;
				dataHeaderList.Add(data);
				dataHeaderDict[data] = 0;
			}
		}

		/// <inheritdoc/>
		public uint GetFileLength() => length;

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <inheritdoc/>
		public uint CalculateAlignment() => 0;

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			uint offset = 0;

			// The order here must be the same as in SetOffset()

			foreach (var dir in dirList) {
				uint padding = Utils.AlignUp(offset, RESOURCE_DIR_ALIGNMENT) - offset;
				writer.WriteZeros((int)padding);
				offset += padding;
				if (dirDict[dir] != offset)
					throw new ModuleWriterException("Invalid Win32 resource directory offset");
				offset += WriteTo(writer, dir);
			}

			foreach (var dataHeader in dataHeaderList) {
				uint padding = Utils.AlignUp(offset, RESOURCE_DATA_HEADER_ALIGNMENT) - offset;
				writer.WriteZeros((int)padding);
				offset += padding;
				if (dataHeaderDict[dataHeader] != offset)
					throw new ModuleWriterException("Invalid Win32 resource data header offset");
				offset += WriteTo(writer, dataHeader);
			}

			foreach (var s in stringsList) {
				uint padding = Utils.AlignUp(offset, RESOURCE_STRING_ALIGNMENT) - offset;
				writer.WriteZeros((int)padding);
				offset += padding;
				if (stringsDict[s] != offset)
					throw new ModuleWriterException("Invalid Win32 resource string offset");

				if (s.Length > ushort.MaxValue)
					throw new ModuleWriterException("Win32 resource entry name is too long");
				writer.WriteUInt16((ushort)s.Length);
				// Write the UTF-16 code units as is (the reader reads them as is). An encoder would
				// replace lone surrogates with U+FFFD and the name would change.
				foreach (var c in s)
					writer.WriteUInt16(c);
				offset += 2 + (uint)s.Length * 2;
			}

			var dataBuffer = new byte[0x2000];
			foreach (var block in dataBlocks) {
				uint padding = (uint)(GetBlockOffset(offset, block) - offset);
				writer.WriteZeros((int)padding);
				offset += padding;
				if (block.Offset != offset)
					throw new ModuleWriterException("Invalid Win32 resource data offset");

				uint blockLength = block.End - block.Start;
				var reader = block.Factory.CreateReader(block.Start, blockLength);
				if (reader.Length != blockLength)
					throw new ModuleWriterException("Invalid Win32 resource data length");
				offset += blockLength;
				reader.CopyTo(writer, dataBuffer);
			}
		}

		uint WriteTo(DataWriter writer, ResourceDirectory dir) {
			writer.WriteUInt32(dir.Characteristics);
			writer.WriteUInt32(dir.TimeDateStamp);
			writer.WriteUInt16(dir.MajorVersion);
			writer.WriteUInt16(dir.MinorVersion);

			GetNamedAndIds(dir, out var named, out var ids);
			if (named.Count > ushort.MaxValue || ids.Count > ushort.MaxValue)
				throw new ModuleWriterException("Too many named/id Win32 resource entries");
			writer.WriteUInt16((ushort)named.Count);
			writer.WriteUInt16((ushort)ids.Count);

			// These must be sorted in ascending order. Names are case insensitive.
			// Each name is uppercased once, not in every comparison, since many entries can
			// have the same long name.
			var upperNames = new Dictionary<string, string>(StringComparer.Ordinal);
			var sortedNamed = new List<KeyValuePair<string, ResourceDirectoryEntry>>(named.Count);
			foreach (var d in named) {
				var name = d.Name.Name!;
				if (!upperNames.TryGetValue(name, out var upperName)) {
					upperName = name.ToUpperInvariant();
					upperNames.Add(name, upperName);
				}
				sortedNamed.Add(new KeyValuePair<string, ResourceDirectoryEntry>(upperName, d));
			}
			// The comparison must be ordinal, it's what Windows uses and it doesn't depend on
			// the current culture.
			sortedNamed.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
			ids.Sort((a, b) => a.Name.Id.CompareTo(b.Name.Id));

			foreach (var kv in sortedNamed) {
				var d = kv.Value;
				writer.WriteUInt32(0x80000000 | stringsDict[d.Name.Name!]);
				writer.WriteUInt32(GetDirectoryEntryOffset(d));
			}

			foreach (var d in ids) {
				writer.WriteInt32(d.Name.Id);
				writer.WriteUInt32(GetDirectoryEntryOffset(d));
			}

			return 16 + (uint)(named.Count + ids.Count) * 8;
		}

		uint GetDirectoryEntryOffset(ResourceDirectoryEntry e) {
			if (e is ResourceData)
				return dataHeaderDict[(ResourceData)e];
			return 0x80000000 | dirDict[(ResourceDirectory)e];
		}

		static void GetNamedAndIds(ResourceDirectory dir, out List<ResourceDirectoryEntry> named, out List<ResourceDirectoryEntry> ids) {
			named = new List<ResourceDirectoryEntry>();
			ids = new List<ResourceDirectoryEntry>();
			var dirs = dir.Directories;
			int count = dirs.Count;
			for (int i = 0; i < count; i++) {
				var d = dirs[i];
				if (d.Name.HasId)
					ids.Add(d);
				else
					named.Add(d);
			}
			var dirData = dir.Data;
			count = dirData.Count;
			for (int i = 0; i < count; i++) {
				var d = dirData[i];
				if (d.Name.HasId)
					ids.Add(d);
				else
					named.Add(d);
			}
		}

		uint WriteTo(DataWriter writer, ResourceData dataHeader) {
			writer.WriteUInt32((uint)rva + dataDict[dataHeader]);
			writer.WriteUInt32((uint)dataHeader.CreateReader().Length);
			writer.WriteUInt32(dataHeader.CodePage);
			writer.WriteUInt32(dataHeader.Reserved);
			return 16;
		}
	}
}
