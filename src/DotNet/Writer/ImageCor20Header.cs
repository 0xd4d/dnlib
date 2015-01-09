// dnlib: See LICENSE.txt for more info

﻿using System.IO;
using dnlib.IO;
using dnlib.PE;
using dnlib.DotNet.MD;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Options to <see cref="ImageCor20Header"/>
	/// </summary>
	public sealed class Cor20HeaderOptions {
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
		public ushort? MajorRuntimeVersion;

		/// <summary>
		/// Minor runtime version
		/// </summary>
		public ushort? MinorRuntimeVersion;

		/// <summary>
		/// Flags
		/// </summary>
		public ComImageFlags? Flags;

		/// <summary>
		/// Entry point or <c>null</c>. Either a Method/File token or an RVA.
		/// </summary>
		public uint? EntryPoint;

		/// <summary>
		/// Default constructor
		/// </summary>
		public Cor20HeaderOptions() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		public Cor20HeaderOptions(ComImageFlags flags) {
			this.Flags = flags;
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
	public sealed class ImageCor20Header : IChunk {
		FileOffset offset;
		RVA rva;
		Cor20HeaderOptions options;

		/// <summary>
		/// Gets/sets the <see cref="MetaData"/>
		/// </summary>
		public MetaData MetaData { get; set; }

		/// <summary>
		/// Gets/sets the .NET resources
		/// </summary>
		public NetResources NetResources { get; set; }

		/// <summary>
		/// Gets/sets the strong name signature
		/// </summary>
		public StrongNameSignature StrongNameSignature { get; set; }

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
		public uint GetFileLength() {
			return 0x48;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			writer.Write(0x48);	// cb
			writer.Write(options.MajorRuntimeVersion ?? Cor20HeaderOptions.DEFAULT_MAJOR_RT_VER);
			writer.Write(options.MinorRuntimeVersion ?? Cor20HeaderOptions.DEFAULT_MINOR_RT_VER);
			writer.WriteDataDirectory(MetaData);
			writer.Write((uint)(options.Flags ?? ComImageFlags.ILOnly));
			writer.Write(options.EntryPoint ?? 0);
			writer.WriteDataDirectory(NetResources);
			writer.WriteDataDirectory(StrongNameSignature);
			writer.WriteDataDirectory(null);	// Code manager table
			writer.WriteDataDirectory(null);	// Vtable fixups
			writer.WriteDataDirectory(null);	// Export address table jumps
			writer.WriteDataDirectory(null);	// Managed native header
		}
	}
}
