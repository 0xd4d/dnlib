// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dnlib.IO;
using dnlib.PE;
using dnlib.W32Resources;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Writes Win32 resources
	/// </summary>
	public sealed class Win32ResourcesChunk : IChunk {
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
		readonly Dictionary<IBinaryReader, uint> dataDict = new Dictionary<IBinaryReader, uint>();
		readonly List<IBinaryReader> dataList = new List<IBinaryReader>();

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="win32Resources">Win32 resources</param>
		public Win32ResourcesChunk(Win32Resources win32Resources) {
			this.win32Resources = win32Resources;
		}

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
		public bool GetFileOffsetAndRvaOf(ResourceDirectoryEntry dirEntry, out FileOffset fileOffset, out RVA rva) {
			var dir = dirEntry as ResourceDirectory;
			if (dir != null)
				return GetFileOffsetAndRvaOf(dir, out fileOffset, out rva);

			var dataHeader = dirEntry as ResourceData;
			if (dataHeader != null)
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
		public FileOffset GetFileOffset(ResourceDirectoryEntry dirEntry) {
			FileOffset fileOffset;
			RVA rva;
			GetFileOffsetAndRvaOf(dirEntry, out fileOffset, out rva);
			return fileOffset;
		}

		/// <summary>
		/// Returns the <see cref="RVA"/> of a <see cref="ResourceDirectoryEntry"/>.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="dirEntry">A <see cref="ResourceDirectoryEntry"/></param>
		/// <returns>The RVA or 0 if <paramref name="dirEntry"/> is invalid</returns>
		public RVA GetRVA(ResourceDirectoryEntry dirEntry) {
			FileOffset fileOffset;
			RVA rva;
			GetFileOffsetAndRvaOf(dirEntry, out fileOffset, out rva);
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
		public bool GetFileOffsetAndRvaOf(ResourceDirectory dir, out FileOffset fileOffset, out RVA rva) {
			uint offs;
			if (dir == null || !dirDict.TryGetValue(dir, out offs)) {
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
		public FileOffset GetFileOffset(ResourceDirectory dir) {
			FileOffset fileOffset;
			RVA rva;
			GetFileOffsetAndRvaOf(dir, out fileOffset, out rva);
			return fileOffset;
		}

		/// <summary>
		/// Returns the <see cref="RVA"/> of a <see cref="ResourceDirectory"/>.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="dir">A <see cref="ResourceDirectory"/></param>
		/// <returns>The RVA or 0 if <paramref name="dir"/> is invalid</returns>
		public RVA GetRVA(ResourceDirectory dir) {
			FileOffset fileOffset;
			RVA rva;
			GetFileOffsetAndRvaOf(dir, out fileOffset, out rva);
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
		public bool GetFileOffsetAndRvaOf(ResourceData dataHeader, out FileOffset fileOffset, out RVA rva) {
			uint offs;
			if (dataHeader == null || !dataHeaderDict.TryGetValue(dataHeader, out offs)) {
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
		public FileOffset GetFileOffset(ResourceData dataHeader) {
			FileOffset fileOffset;
			RVA rva;
			GetFileOffsetAndRvaOf(dataHeader, out fileOffset, out rva);
			return fileOffset;
		}

		/// <summary>
		/// Returns the <see cref="RVA"/> of a <see cref="ResourceData"/>.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="dataHeader">A <see cref="ResourceData"/></param>
		/// <returns>The RVA or 0 if <paramref name="dataHeader"/> is invalid</returns>
		public RVA GetRVA(ResourceData dataHeader) {
			FileOffset fileOffset;
			RVA rva;
			GetFileOffsetAndRvaOf(dataHeader, out fileOffset, out rva);
			return rva;
		}

		/// <summary>
		/// Returns the <see cref="FileOffset"/> and <see cref="RVA"/> of the raw data
		/// owned by a <see cref="ResourceData"/>. <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="data">A <see cref="ResourceData"/>'s <see cref="IBinaryReader"/></param>
		/// <param name="fileOffset">Updated with the file offset</param>
		/// <param name="rva">Updated with the RVA</param>
		/// <returns><c>true</c> if <paramref name="data"/> is valid and
		/// <paramref name="fileOffset"/> and <paramref name="rva"/> have been updated. <c>false</c>
		/// if <paramref name="data"/> is not part of the Win32 resources.</returns>
		public bool GetFileOffsetAndRvaOf(IBinaryReader data, out FileOffset fileOffset, out RVA rva) {
			uint offs;
			if (data == null || !dataDict.TryGetValue(data, out offs)) {
				fileOffset = 0;
				rva = 0;
				return false;
			}

			fileOffset = offset + offs;
			rva = this.rva + offs;
			return true;
		}

		/// <summary>
		/// Returns the <see cref="FileOffset"/> of the raw data owned by a
		/// <see cref="ResourceData"/>. <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="data">A <see cref="ResourceData"/>'s <see cref="IBinaryReader"/></param>
		/// <returns>The file offset or 0 if <paramref name="data"/> is invalid</returns>
		public FileOffset GetFileOffset(IBinaryReader data) {
			FileOffset fileOffset;
			RVA rva;
			GetFileOffsetAndRvaOf(data, out fileOffset, out rva);
			return fileOffset;
		}

		/// <summary>
		/// Returns the <see cref="RVA"/> of the raw data owned by a <see cref="ResourceData"/>.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="data">A <see cref="ResourceData"/>'s <see cref="IBinaryReader"/></param>
		/// <returns>The RVA or 0 if <paramref name="data"/> is invalid</returns>
		public RVA GetRVA(IBinaryReader data) {
			FileOffset fileOffset;
			RVA rva;
			GetFileOffsetAndRvaOf(data, out fileOffset, out rva);
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
			uint offs;
			if (name == null || !stringsDict.TryGetValue(name, out offs)) {
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
			FileOffset fileOffset;
			RVA rva;
			GetFileOffsetAndRvaOf(name, out fileOffset, out rva);
			return fileOffset;
		}

		/// <summary>
		/// Returns the <see cref="RVA"/> of a <see cref="ResourceDirectoryEntry"/>'s name.
		/// <see cref="SetOffset"/> must have been called.
		/// </summary>
		/// <param name="name">The name of a <see cref="ResourceDirectoryEntry"/></param>
		/// <returns>The RVA or 0 if <paramref name="name"/> is invalid</returns>
		public RVA GetRVA(string name) {
			FileOffset fileOffset;
			RVA rva;
			GetFileOffsetAndRvaOf(name, out fileOffset, out rva);
			return rva;
		}

		const uint RESOURCE_DIR_ALIGNMENT = 4;
		const uint RESOURCE_DATA_HEADER_ALIGNMENT = 4;
		const uint RESOURCE_STRING_ALIGNMENT = 2;
		const uint RESOURCE_DATA_ALIGNMENT = 4;

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
			if (win32Resources == null)
				return;

			FindDirectoryEntries();

			// Place everything in the following order:
			//	1. All resource directories. The root is always first.
			//	2. All resource data headers.
			//	3. All the strings.
			//	4. All resource data.

			uint rsrcOffset = 0;

			uint maxAlignment = 1;
			maxAlignment = Math.Max(maxAlignment, RESOURCE_DIR_ALIGNMENT);
			maxAlignment = Math.Max(maxAlignment, RESOURCE_DATA_HEADER_ALIGNMENT);
			maxAlignment = Math.Max(maxAlignment, RESOURCE_STRING_ALIGNMENT);
			maxAlignment = Math.Max(maxAlignment, RESOURCE_DATA_ALIGNMENT);
			if (((uint)offset & (maxAlignment - 1)) != 0)
				throw new ModuleWriterException(string.Format("Win32 resources section isn't {0}-byte aligned", maxAlignment));
			if (maxAlignment > ModuleWriterBase.DEFAULT_WIN32_RESOURCES_ALIGNMENT)
				throw new ModuleWriterException("maxAlignment > DEFAULT_WIN32_RESOURCES_ALIGNMENT");

			foreach (var dir in dirList) {
				rsrcOffset = Utils.AlignUp(rsrcOffset, RESOURCE_DIR_ALIGNMENT);
				dirDict[dir] = rsrcOffset;
				if (dir != dirList[0])
					AddString(dir.Name);
				rsrcOffset += 16 + (uint)(dir.Directories.Count + dir.Data.Count) * 8;
			}

			foreach (var data in dataHeaderList) {
				rsrcOffset = Utils.AlignUp(rsrcOffset, RESOURCE_DATA_HEADER_ALIGNMENT);
				dataHeaderDict[data] = rsrcOffset;
				AddString(data.Name);
				AddData(data.Data);
				rsrcOffset += 16;
			}

			foreach (var s in stringsList) {
				rsrcOffset = Utils.AlignUp(rsrcOffset, RESOURCE_STRING_ALIGNMENT);
				stringsDict[s] = rsrcOffset;
				rsrcOffset += 2 + (uint)(s.Length * 2);
			}

			foreach (var data in dataList) {
				rsrcOffset = Utils.AlignUp(rsrcOffset, RESOURCE_DATA_ALIGNMENT);
				dataDict[data] = rsrcOffset;
				rsrcOffset += (uint)data.Length;
			}

			length = rsrcOffset;
		}

		void AddData(IBinaryReader data) {
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
			FindDirectoryEntries(win32Resources.Root);
		}

		void FindDirectoryEntries(ResourceDirectory dir) {
			if (dirDict.ContainsKey(dir))
				return;
			dirList.Add(dir);
			dirDict[dir] = 0;
			foreach (var dir2 in dir.Directories)
				FindDirectoryEntries(dir2);
			foreach (var data in dir.Data) {
				if (dataHeaderDict.ContainsKey(data))
					continue;
				dataHeaderList.Add(data);
				dataHeaderDict[data] = 0;
			}
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return Utils.AlignUp(length, ModuleWriterBase.DEFAULT_WIN32_RESOURCES_ALIGNMENT);
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
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

				var bytes = Encoding.Unicode.GetBytes(s);
				if (bytes.Length / 2 > ushort.MaxValue)
					throw new ModuleWriterException("Win32 resource entry name is too long");
				writer.Write((ushort)(bytes.Length / 2));
				writer.Write(bytes);
				offset += 2 + (uint)bytes.Length;
			}

			byte[] dataBuffer = new byte[0x2000];
			foreach (var data in dataList) {
				uint padding = Utils.AlignUp(offset, RESOURCE_DATA_ALIGNMENT) - offset;
				writer.WriteZeros((int)padding);
				offset += padding;
				if (dataDict[data] != offset)
					throw new ModuleWriterException("Invalid Win32 resource data offset");

				data.Position = 0;
				offset += data.WriteTo(writer, dataBuffer);
			}

			writer.WriteZeros((int)(Utils.AlignUp(length, ModuleWriterBase.DEFAULT_WIN32_RESOURCES_ALIGNMENT) - length));
		}

		uint WriteTo(BinaryWriter writer, ResourceDirectory dir) {
			writer.Write(dir.Characteristics);
			writer.Write(dir.TimeDateStamp);
			writer.Write(dir.MajorVersion);
			writer.Write(dir.MinorVersion);

			List<ResourceDirectoryEntry> named;
			List<ResourceDirectoryEntry> ids;
			GetNamedAndIds(dir, out named, out ids);
			if (named.Count > ushort.MaxValue || ids.Count > ushort.MaxValue)
				throw new ModuleWriterException("Too many named/id Win32 resource entries");
			writer.Write((ushort)named.Count);
			writer.Write((ushort)ids.Count);

			// These must be sorted in ascending order. Names are case insensitive.
			named.Sort((a, b) => a.Name.Name.ToUpperInvariant().CompareTo(b.Name.Name.ToUpperInvariant()));
			ids.Sort((a, b) => a.Name.Id.CompareTo(b.Name.Id));

			foreach (var d in named) {
				writer.Write(0x80000000 | stringsDict[d.Name.Name]);
				writer.Write(GetDirectoryEntryOffset(d));
			}

			foreach (var d in ids) {
				writer.Write(d.Name.Id);
				writer.Write(GetDirectoryEntryOffset(d));
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
			foreach (var d in dir.Directories) {
				if (d.Name.HasId)
					ids.Add(d);
				else
					named.Add(d);
			}
			foreach (var d in dir.Data) {
				if (d.Name.HasId)
					ids.Add(d);
				else
					named.Add(d);
			}
		}

		uint WriteTo(BinaryWriter writer, ResourceData dataHeader) {
			writer.Write((uint)rva + dataDict[dataHeader.Data]);
			writer.Write((uint)dataHeader.Data.Length);
			writer.Write(dataHeader.CodePage);
			writer.Write(dataHeader.Reserved);
			return 16;
		}
	}
}
