// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Low level access to a .NET file's metadata
	/// </summary>
	public static class MetadataFactory {
		enum MetadataType {
			Unknown,
			Compressed,	// #~ (normal)
			ENC,		// #- (edit and continue)
		}

		/// <summary>
		/// Create a <see cref="MetadataBase"/> instance
		/// </summary>
		/// <param name="fileName">The file to load</param>
		/// <returns>A new <see cref="MetadataBase"/> instance</returns>
		internal static MetadataBase Load(string fileName) {
			IPEImage peImage = null;
			try {
				return Load(peImage = new PEImage(fileName));
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a <see cref="MetadataBase"/> instance
		/// </summary>
		/// <param name="data">The .NET file data</param>
		/// <returns>A new <see cref="MetadataBase"/> instance</returns>
		internal static MetadataBase Load(byte[] data) {
			IPEImage peImage = null;
			try {
				return Load(peImage = new PEImage(data));
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a <see cref="MetadataBase"/> instance
		/// </summary>
		/// <param name="addr">Address of a .NET file in memory</param>
		/// <returns>A new <see cref="MetadataBase"/> instance</returns>
		internal static MetadataBase Load(IntPtr addr) {
			IPEImage peImage = null;

			// We don't know what layout it is. Memory is more common so try that first.
			try {
				return Load(peImage = new PEImage(addr, ImageLayout.Memory, true));
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				peImage = null;
			}

			try {
				return Load(peImage = new PEImage(addr, ImageLayout.File, true));
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a <see cref="MetadataBase"/> instance
		/// </summary>
		/// <param name="addr">Address of a .NET file in memory</param>
		/// <param name="imageLayout">Image layout of the file in memory</param>
		/// <returns>A new <see cref="MetadataBase"/> instance</returns>
		internal static MetadataBase Load(IntPtr addr, ImageLayout imageLayout) {
			IPEImage peImage = null;
			try {
				return Load(peImage = new PEImage(addr, imageLayout, true));
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a <see cref="MetadataBase"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <returns>A new <see cref="MetadataBase"/> instance</returns>
		internal static MetadataBase Load(IPEImage peImage) => Create(peImage, true);

		/// <summary>
		/// Create a <see cref="Metadata"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <returns>A new <see cref="Metadata"/> instance</returns>
		public static Metadata CreateMetadata(IPEImage peImage) => Create(peImage, true);

		/// <summary>
		/// Create a <see cref="Metadata"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="verify"><c>true</c> if we should verify that it's a .NET PE file</param>
		/// <returns>A new <see cref="Metadata"/> instance</returns>
		public static Metadata CreateMetadata(IPEImage peImage, bool verify) => Create(peImage, verify);

		/// <summary>
		/// Create a <see cref="MetadataBase"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="verify"><c>true</c> if we should verify that it's a .NET PE file</param>
		/// <returns>A new <see cref="MetadataBase"/> instance</returns>
		static MetadataBase Create(IPEImage peImage, bool verify) {
			MetadataBase md = null;
			try {
				var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
				if (dotNetDir.VirtualAddress == 0)
					throw new BadImageFormatException(".NET data directory RVA is 0");
				if (dotNetDir.Size < 0x48)
					throw new BadImageFormatException(".NET data directory size < 0x48");
				var cor20HeaderReader = peImage.CreateReader(dotNetDir.VirtualAddress, 0x48);
				var cor20Header = new ImageCor20Header(ref cor20HeaderReader, verify);
				if (cor20Header.Metadata.VirtualAddress == 0)
					throw new BadImageFormatException(".NET metadata RVA is 0");
				if (cor20Header.Metadata.Size < 16)
					throw new BadImageFormatException(".NET metadata size is too small");
				var mdSize = cor20Header.Metadata.Size;
				var mdRva = cor20Header.Metadata.VirtualAddress;
				var mdHeaderReader = peImage.CreateReader(mdRva, mdSize);
				var mdHeader = new MetadataHeader(ref mdHeaderReader, verify);
				if (verify) {
					foreach (var sh in mdHeader.StreamHeaders) {
						if (sh.Offset + sh.StreamSize < sh.Offset || sh.Offset + sh.StreamSize > mdSize)
							throw new BadImageFormatException("Invalid stream header");
					}
				}

				switch (GetMetadataType(mdHeader.StreamHeaders)) {
				case MetadataType.Compressed:
					md = new CompressedMetadata(peImage, cor20Header, mdHeader);
					break;

				case MetadataType.ENC:
					md = new ENCMetadata(peImage, cor20Header, mdHeader);
					break;

				default:
					throw new BadImageFormatException("No #~ or #- stream found");
				}
				md.Initialize(null);

				return md;
			}
			catch {
				if (md != null)
					md.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a standalone portable PDB <see cref="MetadataBase"/> instance
		/// </summary>
		/// <param name="mdReaderFactory">Metadata stream</param>
		/// <param name="verify"><c>true</c> if we should verify that it's a .NET PE file</param>
		/// <returns>A new <see cref="MetadataBase"/> instance</returns>
		internal static MetadataBase CreateStandalonePortablePDB(DataReaderFactory mdReaderFactory, bool verify) {
			MetadataBase md = null;
			try {
				var reader = mdReaderFactory.CreateReader();
				var mdHeader = new MetadataHeader(ref reader, verify);
				if (verify) {
					foreach (var sh in mdHeader.StreamHeaders) {
						if (sh.Offset + sh.StreamSize < sh.Offset || sh.Offset + sh.StreamSize > reader.Length)
							throw new BadImageFormatException("Invalid stream header");
					}
				}

				switch (GetMetadataType(mdHeader.StreamHeaders)) {
				case MetadataType.Compressed:
					md = new CompressedMetadata(mdHeader, true);
					break;

				case MetadataType.ENC:
					md = new ENCMetadata(mdHeader, true);
					break;

				default:
					throw new BadImageFormatException("No #~ or #- stream found");
				}
				md.Initialize(mdReaderFactory);

				return md;
			}
			catch {
				md?.Dispose();
				throw;
			}
		}

		static MetadataType GetMetadataType(IList<StreamHeader> streamHeaders) {
			MetadataType? mdType = null;
			foreach (var sh in streamHeaders) {
				if (mdType == null) {
					if (sh.Name == "#~")
						mdType = MetadataType.Compressed;
					else if (sh.Name == "#-")
						mdType = MetadataType.ENC;
				}
				if (sh.Name == "#Schema")
					mdType = MetadataType.ENC;
			}
			if (mdType == null)
				return MetadataType.Unknown;
			return mdType.Value;
		}
	}
}
