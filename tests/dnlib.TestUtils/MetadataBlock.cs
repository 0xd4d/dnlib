// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;

namespace dnlib.TestUtils {
	/// <summary>
	/// A stream of the metadata: its name and its bytes
	/// </summary>
	public sealed class MetadataBlockStream {
		public string Name { get; set; }
		public byte[] Data { get; set; }

		/// <summary>
		/// The offset and the size that are written to the stream's header, or null to write the real ones
		/// </summary>
		public uint? OffsetInHeader { get; set; }
		public uint? SizeInHeader { get; set; }

		public MetadataBlockStream(string name, byte[] data) {
			Name = name;
			Data = data;
		}

		public MetadataBlockStream Clone() => new MetadataBlockStream(Name, (byte[])Data.Clone());

		public override string ToString() => $"{Name} ({Data.Length} bytes)";
	}

	/// <summary>
	/// The metadata of a PE file as its header fields and a list of streams. A test changes the list (adds a
	/// second stream with the same name, renames one, changes the order, changes the bytes of a stream) and
	/// <see cref="Write"/> creates the file with that metadata. It doesn't use dnlib's reader or writer.
	///
	/// The new metadata is written where the old one was, so it can't be bigger: a test starts with a file
	/// that has a big stream that it removes (see <see cref="RemoveStream"/>).
	/// </summary>
	public sealed class MetadataBlock {
		readonly byte[] image;
		readonly int metadataOffset;
		readonly int metadataSize;
		readonly int metadataSizeOffset;

		/// <summary>
		/// The size of the metadata that's written to the .NET header, or null to keep the size of the file
		/// </summary>
		public uint? SizeInDotNetHeader { get; set; }

		public uint Signature { get; set; }
		public ushort MajorVersion { get; set; }
		public ushort MinorVersion { get; set; }
		public uint Reserved { get; set; }

		/// <summary>
		/// The bytes of the version string, with the zero bytes at its end
		/// </summary>
		public byte[] VersionString { get; set; }

		public byte StorageFlags { get; set; }
		public byte StorageReserved { get; set; }

		/// <summary>
		/// Bytes between the storage header and the first stream header (normally none)
		/// </summary>
		public byte[] DataAfterStorageHeader { get; set; } = new byte[0];

		/// <summary>
		/// The number of streams that's written to the header, or null to write the real number
		/// </summary>
		public ushort? StreamCount { get; set; }

		public List<MetadataBlockStream> Streams { get; } = new List<MetadataBlockStream>();

		public MetadataBlock(byte[] image) {
			this.image = (byte[])image.Clone();
			using (var peReader = new PEReader(new MemoryStream(image))) {
				var directory = peReader.PEHeaders.CorHeader!.MetadataDirectory;
				if (!peReader.PEHeaders.TryGetDirectoryOffset(directory, out metadataOffset))
					throw new ArgumentException("The file has no metadata");
				metadataSize = directory.Size;
				// The .NET header: its size, the runtime version, and then the RVA and the size of the metadata
				metadataSizeOffset = peReader.PEHeaders.CorHeaderStartOffset + 12;
			}

			int position = metadataOffset;
			Signature = LittleEndian.ToUInt32(image, position);
			MajorVersion = LittleEndian.ToUInt16(image, position + 4);
			MinorVersion = LittleEndian.ToUInt16(image, position + 6);
			Reserved = LittleEndian.ToUInt32(image, position + 8);
			int versionLength = LittleEndian.ToInt32(image, position + 12);
			VersionString = image.Skip(position + 16).Take(versionLength).ToArray();
			position += 16 + versionLength;
			StorageFlags = image[position];
			StorageReserved = image[position + 1];
			int streamCount = LittleEndian.ToUInt16(image, position + 2);
			position += 4;
			for (int i = 0; i < streamCount; i++) {
				int offset = LittleEndian.ToInt32(image, position);
				int size = LittleEndian.ToInt32(image, position + 4);
				int nameEnd = Array.IndexOf(image, (byte)0, position + 8);
				string name = Encoding.ASCII.GetString(image, position + 8, nameEnd - (position + 8));
				Streams.Add(new MetadataBlockStream(name, image.Skip(metadataOffset + offset).Take(size).ToArray()));
				position = Align(nameEnd + 1 - metadataOffset) + metadataOffset;
			}
		}

		static int Align(int value) => (value + 3) & ~3;

		/// <summary>
		/// The first stream with the name
		/// </summary>
		public MetadataBlockStream GetStream(string name) => Streams.First(a => a.Name == name);

		/// <summary>
		/// Removes the first stream with the name
		/// </summary>
		public MetadataBlock RemoveStream(string name) {
			Streams.Remove(GetStream(name));
			return this;
		}

		/// <summary>
		/// The bytes of the metadata: the header, the stream headers, and then the streams in the same order
		/// </summary>
		public byte[] CreateMetadata() {
			var output = new MemoryStream();
			var writer = new BinaryWriter(output);
			writer.Write(Signature);
			writer.Write(MajorVersion);
			writer.Write(MinorVersion);
			writer.Write(Reserved);
			writer.Write(VersionString.Length);
			writer.Write(VersionString);
			writer.Write(StorageFlags);
			writer.Write(StorageReserved);
			writer.Write(StreamCount ?? (ushort)Streams.Count);
			writer.Write(DataAfterStorageHeader);

			// The size of all stream headers says where the first stream starts
			int dataOffset = (int)output.Length;
			foreach (var stream in Streams)
				dataOffset += 8 + Align(Encoding.ASCII.GetByteCount(stream.Name) + 1);
			foreach (var stream in Streams) {
				writer.Write(stream.OffsetInHeader ?? (uint)dataOffset);
				writer.Write(stream.SizeInHeader ?? (uint)stream.Data.Length);
				var name = Encoding.ASCII.GetBytes(stream.Name);
				writer.Write(name);
				writer.Write(new byte[Align(name.Length + 1) - name.Length]);
				dataOffset += Align(stream.Data.Length);
			}
			foreach (var stream in Streams) {
				writer.Write(stream.Data);
				writer.Write(new byte[Align(stream.Data.Length) - stream.Data.Length]);
			}
			return output.ToArray();
		}

		/// <summary>
		/// Creates the file with the metadata of this instance
		/// </summary>
		public byte[] Write() {
			var metadata = CreateMetadata();
			if (metadata.Length > metadataSize)
				throw new InvalidOperationException($"The metadata has {metadata.Length} bytes, the file only has room for {metadataSize} bytes");
			var newImage = (byte[])image.Clone();
			Array.Clear(newImage, metadataOffset, metadataSize);
			metadata.CopyTo(newImage, metadataOffset);
			if (SizeInDotNetHeader is not null)
				LittleEndian.GetBytes(SizeInDotNetHeader.Value).CopyTo(newImage, metadataSizeOffset);
			return newImage;
		}
	}
}
