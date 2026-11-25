// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace dnlib.TestUtils {
	/// <summary>
	/// A Win32 resource in a PE file, eg. the version info, the manifest or an icon
	/// </summary>
	public sealed class PEWin32Resource {
		/// <summary>
		/// The names or IDs of the directories it's in and its own, eg. <c>16/1/0</c> (type/name/language).
		/// A name is in quotes, an ID isn't.
		/// </summary>
		public string Path { get; }

		/// <summary>
		/// Where the data is in the file when it's loaded (its RVA)
		/// </summary>
		public int DataRva { get; }

		/// <summary>
		/// The code page that's stored with the data
		/// </summary>
		public int CodePage { get; }

		/// <summary>
		/// The resource's data
		/// </summary>
		public byte[] Data { get; }

		public PEWin32Resource(string path, int dataRva, int codePage, byte[] data) {
			Path = path;
			DataRva = dataRva;
			CodePage = codePage;
			Data = data;
		}

		public override string ToString() => $"{Path}: {Data.Length} bytes";
	}

	/// <summary>
	/// Reads the Win32 resources of a PE file (its resource directory), without dnlib: the PE headers are read
	/// by System.Reflection.Metadata's <see cref="PEReader"/>.
	/// </summary>
	public static class PEWin32Resources {
		/// <summary>
		/// Returns all resources in the order they're stored in the file. The list is empty if the file has no
		/// resource directory.
		/// </summary>
		public static List<PEWin32Resource> Read(byte[] image) {
			var resources = new List<PEWin32Resource>();
			using (var peReader = new PEReader(ImmutableArray.Create(image))) {
				var directory = peReader.PEHeaders.PEHeader!.ResourceTableDirectory;
				if (directory.Size != 0)
					ReadDirectory(peReader, directory.RelativeVirtualAddress, directoryOffset: 0, path: string.Empty, depth: 0, resources);
			}
			return resources;
		}

		// All offsets in the resource directory are relative to its start (resourcesRva)
		static BlobReader GetReader(PEReader peReader, int resourcesRva, int offset) =>
			peReader.GetSectionData(resourcesRva + offset).GetReader();

		static void ReadDirectory(PEReader peReader, int resourcesRva, int directoryOffset, string path, int depth, List<PEWin32Resource> resources) {
			// A valid file has 3 levels (type, name, language). Stop if the directories contain each other.
			if (depth > 10)
				return;

			// IMAGE_RESOURCE_DIRECTORY: Characteristics, TimeDateStamp, MajorVersion and MinorVersion (12 bytes),
			// NumberOfNamedEntries, NumberOfIdEntries. The entries follow: first the named ones, then the ones
			// with an ID.
			var reader = GetReader(peReader, resourcesRva, directoryOffset);
			reader.Offset = 12;
			int entryCount = reader.ReadUInt16() + reader.ReadUInt16();
			for (int i = 0; i < entryCount; i++) {
				// IMAGE_RESOURCE_DIRECTORY_ENTRY: a name or ID, and the offset of a directory or of a data entry.
				// The highest bit says which one it is.
				uint nameOrId = reader.ReadUInt32();
				uint offset = reader.ReadUInt32();
				const uint highBit = 0x80000000;
				string name = (nameOrId & highBit) != 0 ? "\"" + ReadName(peReader, resourcesRva, (int)(nameOrId & ~highBit)) + "\"" : nameOrId.ToString();
				string entryPath = path.Length == 0 ? name : path + "/" + name;
				if ((offset & highBit) != 0)
					ReadDirectory(peReader, resourcesRva, (int)(offset & ~highBit), entryPath, depth + 1, resources);
				else
					resources.Add(ReadData(peReader, resourcesRva, (int)offset, entryPath));
			}
		}

		// A name is its length in chars followed by UTF-16 chars
		static string ReadName(PEReader peReader, int resourcesRva, int offset) {
			var reader = GetReader(peReader, resourcesRva, offset);
			int length = reader.ReadUInt16();
			return reader.ReadUTF16(length * 2);
		}

		// IMAGE_RESOURCE_DATA_ENTRY: the RVA of the data (not an offset), its size, the code page and a reserved value
		static PEWin32Resource ReadData(PEReader peReader, int resourcesRva, int offset, string path) {
			var reader = GetReader(peReader, resourcesRva, offset);
			int dataRva = reader.ReadInt32();
			int size = reader.ReadInt32();
			int codePage = reader.ReadInt32();
			var data = peReader.GetSectionData(dataRva).GetReader().ReadBytes(size);
			return new PEWin32Resource(path, dataRva, codePage, data);
		}
	}
}
