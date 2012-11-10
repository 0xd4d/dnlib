using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dot10.IO;
using dot10.PE;
using dot10.W32Resources;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Win32 resources
	/// </summary>
	public sealed class Win32ResourcesChunk : IChunk {
		readonly Win32Resources win32Resources;
		FileOffset offset;
		RVA rva;
		uint length;
		Dictionary<ResourceDirectory, uint> dirDict = new Dictionary<ResourceDirectory, uint>();
		List<ResourceDirectory> dirList = new List<ResourceDirectory>();
		Dictionary<ResourceData, uint> dataHeaderDict = new Dictionary<ResourceData, uint>();
		List<ResourceData> dataHeaderList = new List<ResourceData>();
		Dictionary<string, uint> stringsDict = new Dictionary<string, uint>(StringComparer.Ordinal);
		List<string> stringsList = new List<string>();
		Dictionary<IBinaryReader, uint> dataDict = new Dictionary<IBinaryReader, uint>();
		List<IBinaryReader> dataList = new List<IBinaryReader>();

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

		// None of these can be > ModuleWriter.DEFAULT_RESOURCE_ALIGNMENT
		const uint RESOURCE_DIR_ALIGNMENT = 4;
		const uint RESOURCE_DATA_HEADER_ALIGNMENT = 4;
		const uint RESOURCE_DATA_ALIGNMENT = 4;
		const uint RESOURCE_STRING_ALIGNMENT = 2;

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
			maxAlignment = Math.Max(maxAlignment, RESOURCE_DATA_ALIGNMENT);
			maxAlignment = Math.Max(maxAlignment, RESOURCE_STRING_ALIGNMENT);
			if (((uint)offset & (maxAlignment - 1)) != 0)
				throw new ModuleWriterException(string.Format("Win32 resources section isn't {0}-byte aligned", maxAlignment));

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
		public uint GetLength() {
			return length;
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

				if (data.Length > uint.MaxValue)
					throw new ModuleWriterException("Win32 resource data is too big");
				data.Position = 0;
				uint lenLeft = (uint)data.Length;
				offset += lenLeft;
				while (lenLeft > 0) {
					int num = (int)Math.Min((uint)dataBuffer.Length, lenLeft);
					lenLeft -= (uint)num;
					if (num != data.Read(dataBuffer, 0, num))
						throw new ModuleWriterException("Could not read all Win32 resource data bytes");
					writer.Write(dataBuffer, 0, num);
				}
			}
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
