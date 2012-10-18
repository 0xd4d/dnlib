using System.IO;
using dot10.IO;
using dot10.PE;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Options to <see cref="ImageCor20Header"/>
	/// </summary>
	struct Cor20HeaderOptions {
		/// <summary>
		/// Default major runtime version
		/// </summary>
		public const ushort DEFAULT_MAJOR_RT_VER = 2;

		/// <summary>
		/// Default minor runtime version
		/// </summary>
		public const ushort DEFAULT_MINOR_RT_VER = 5;

		/// <summary>
		/// Major runtime version
		/// </summary>
		public ushort MajorRuntimeVersion;

		/// <summary>
		/// Minor runtime version
		/// </summary>
		public ushort MinorRuntimeVersion;

		/// <summary>
		/// Flags
		/// </summary>
		public ComImageFlags Flags;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		public Cor20HeaderOptions(ComImageFlags flags)
			: this(DEFAULT_MAJOR_RT_VER, DEFAULT_MINOR_RT_VER, flags) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="major">Major runtime version (default is <see cref="DEFAULT_MAJOR_RT_VER"/>)</param>
		/// <param name="minor">Minor runtime version (default is <see cref="DEFAULT_MINOR_RT_VER"/>)</param>
		/// <param name="flags">Flags</param>
		public Cor20HeaderOptions(ushort major, ushort minor, ComImageFlags flags) {
			this.MajorRuntimeVersion = major;
			this.MinorRuntimeVersion = minor;
			this.Flags = flags;
		}
	}

	/// <summary>
	/// .NET header
	/// </summary>
	class ImageCor20Header : IChunk {
		FileOffset offset;
		RVA rva;
		Cor20HeaderOptions options;

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
		/// <param name="options">Options</param>
		public ImageCor20Header(Cor20HeaderOptions options) {
			this.options = options;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetLength() {
			return 0x48;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			writer.Write(0x48);	// cb
			writer.Write(options.MajorRuntimeVersion);
			writer.Write(options.MinorRuntimeVersion);
			writer.Write(0);	// MD RVA
			writer.Write(0);	// MD size
			writer.Write((uint)options.Flags);
			writer.Write(0);	// Entry point token
			writer.Write(0);	// Resources RVA
			writer.Write(0);	// Resources size
			writer.Write(0);	// Strong name signature RVA
			writer.Write(0);	// Strong name signature size
			writer.Write(0);	// Code manager table RVA
			writer.Write(0);	// Code manager table size
			writer.Write(0);	// Vtable fixups RVA
			writer.Write(0);	// Vtable fixups size
			writer.Write(0);	// Export address table jumps RVA
			writer.Write(0);	// Export address table jumps size
			writer.Write(0);	// Managed native header RVA
			writer.Write(0);	// Managed native header size
		}
	}
}
