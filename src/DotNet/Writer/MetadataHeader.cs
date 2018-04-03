// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Text;
using dnlib.IO;
using dnlib.PE;
using dnlib.DotNet.MD;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// <see cref="MetadataHeader"/> options
	/// </summary>
	public sealed class MetadataHeaderOptions {
		/// <summary>
		/// Default version string
		/// </summary>
		public const string DEFAULT_VERSION_STRING = MDHeaderRuntimeVersion.MS_CLR_20;

		/// <summary>
		/// Default header signature
		/// </summary>
		public const uint DEFAULT_SIGNATURE = 0x424A5342;

		/// <summary>
		/// MD header signature. Default value is <see cref="DEFAULT_SIGNATURE"/>
		/// </summary>
		public uint? Signature;

		/// <summary>
		/// Major version. Default is 1. MS' CLR supports v0.x (x >= 19) and v1.1, nothing else.
		/// </summary>
		public ushort? MajorVersion;

		/// <summary>
		/// Minor version. Default is 1.
		/// </summary>
		public ushort? MinorVersion;

		/// <summary>
		/// Reserved and should be 0.
		/// </summary>
		public uint? Reserved1;

		/// <summary>
		/// Version string. Default is <see cref="DEFAULT_VERSION_STRING"/>. It's stored as a
		/// zero-terminated UTF-8 string. Length should be &lt;= 255 bytes.
		/// </summary>
		public string VersionString;

		/// <summary>
		/// Storage flags should be 0
		/// </summary>
		public StorageFlags? StorageFlags;

		/// <summary>
		/// Reserved and should be 0
		/// </summary>
		public byte? Reserved2;

		/// <summary>
		/// Creates portable PDB v1.0 options
		/// </summary>
		/// <returns></returns>
		public static MetadataHeaderOptions CreatePortablePdbV1_0() =>
			new MetadataHeaderOptions() {
				Signature = DEFAULT_SIGNATURE,
				MajorVersion = 1,
				MinorVersion = 1,
				Reserved1 = 0,
				VersionString = MDHeaderRuntimeVersion.PORTABLE_PDB_V1_0,
				StorageFlags = 0,
				Reserved2 = 0,
			};
	}

	/// <summary>
	/// Meta data header. IMAGE_COR20_HEADER.Metadata points to this header.
	/// </summary>
	public sealed class MetadataHeader : IChunk {
		IList<IHeap> heaps;
		readonly MetadataHeaderOptions options;
		uint length;
		FileOffset offset;
		RVA rva;

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <summary>
		/// Gets/sets the heaps
		/// </summary>
		public IList<IHeap> Heaps {
			get => heaps;
			set => heaps = value;
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MetadataHeader()
			: this(null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options</param>
		public MetadataHeader(MetadataHeaderOptions options) => this.options = options ?? new MetadataHeaderOptions();

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			length = 16;
			length += (uint)GetVersionString().Length;
			length = Utils.AlignUp(length, 4);
			length += 4;
			var heaps = this.heaps;
			int count = heaps.Count;
			for (int i = 0; i < count; i++) {
				var heap = heaps[i];
				length += 8;
				length += (uint)GetAsciizName(heap.Name).Length;
				length = Utils.AlignUp(length, 4);
			}
		}

		/// <inheritdoc/>
		public uint GetFileLength() => length;

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			writer.WriteUInt32(options.Signature ?? MetadataHeaderOptions.DEFAULT_SIGNATURE);
			writer.WriteUInt16(options.MajorVersion ?? 1);
			writer.WriteUInt16(options.MinorVersion ?? 1);
			writer.WriteUInt32(options.Reserved1 ?? 0);
			var s = GetVersionString();
			writer.WriteInt32(Utils.AlignUp(s.Length, 4));
			writer.WriteBytes(s);
			writer.WriteZeroes(Utils.AlignUp(s.Length, 4) - s.Length);
			writer.WriteByte((byte)(options.StorageFlags ?? 0));
			writer.WriteByte(options.Reserved2 ?? 0);
			var heaps = this.heaps;
			writer.WriteUInt16((ushort)heaps.Count);
			int count = heaps.Count;
			for (int i = 0; i < count; i++) {
				var heap = heaps[i];
				writer.WriteUInt32((uint)(heap.FileOffset - offset));
				writer.WriteUInt32(heap.GetFileLength());
				writer.WriteBytes(s = GetAsciizName(heap.Name));
				if (s.Length > 32)
					throw new ModuleWriterException($"Heap name '{heap.Name}' is > 32 bytes");
				writer.WriteZeroes(Utils.AlignUp(s.Length, 4) - s.Length);
			}
		}

		byte[] GetVersionString() => Encoding.UTF8.GetBytes((options.VersionString ?? MetadataHeaderOptions.DEFAULT_VERSION_STRING) + "\0");
		byte[] GetAsciizName(string s) => Encoding.ASCII.GetBytes(s + "\0");
	}
}
