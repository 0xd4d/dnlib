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

		internal static MetadataBase Load(string fileName, CLRRuntimeReaderKind runtime) {
			IPEImage peImage = null;
			try {
				return Load(peImage = new PEImage(fileName), runtime);
			}
			catch {
				if (!(peImage is null))
					peImage.Dispose();
				throw;
			}
		}

		internal static MetadataBase Load(byte[] data, CLRRuntimeReaderKind runtime) {
			IPEImage peImage = null;
			try {
				return Load(peImage = new PEImage(data), runtime);
			}
			catch {
				if (!(peImage is null))
					peImage.Dispose();
				throw;
			}
		}

		internal static MetadataBase Load(IntPtr addr, CLRRuntimeReaderKind runtime) {
			IPEImage peImage = null;

			// We don't know what layout it is. Memory is more common so try that first.
			try {
				return Load(peImage = new PEImage(addr, ImageLayout.Memory, true), runtime);
			}
			catch {
				if (!(peImage is null))
					peImage.Dispose();
				peImage = null;
			}

			try {
				return Load(peImage = new PEImage(addr, ImageLayout.File, true), runtime);
			}
			catch {
				if (!(peImage is null))
					peImage.Dispose();
				throw;
			}
		}

		internal static MetadataBase Load(IntPtr addr, ImageLayout imageLayout, CLRRuntimeReaderKind runtime) {
			IPEImage peImage = null;
			try {
				return Load(peImage = new PEImage(addr, imageLayout, true), runtime);
			}
			catch {
				if (!(peImage is null))
					peImage.Dispose();
				throw;
			}
		}

		internal static MetadataBase Load(IPEImage peImage, CLRRuntimeReaderKind runtime) => Create(peImage, runtime, true);

		/// <summary>
		/// Create a <see cref="Metadata"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <returns>A new <see cref="Metadata"/> instance</returns>
		public static Metadata CreateMetadata(IPEImage peImage) => CreateMetadata(peImage, CLRRuntimeReaderKind.CLR);

		/// <summary>
		/// Create a <see cref="Metadata"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="runtime">Runtime reader kind</param>
		/// <returns>A new <see cref="Metadata"/> instance</returns>
		public static Metadata CreateMetadata(IPEImage peImage, CLRRuntimeReaderKind runtime) => Create(peImage, runtime, true);

		/// <summary>
		/// Create a <see cref="Metadata"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="verify"><c>true</c> if we should verify that it's a .NET PE file</param>
		/// <returns>A new <see cref="Metadata"/> instance</returns>
		public static Metadata CreateMetadata(IPEImage peImage, bool verify) => CreateMetadata(peImage, CLRRuntimeReaderKind.CLR, verify);

		/// <summary>
		/// Create a <see cref="Metadata"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="runtime">Runtime reader kind</param>
		/// <param name="verify"><c>true</c> if we should verify that it's a .NET PE file</param>
		/// <returns>A new <see cref="Metadata"/> instance</returns>
		public static Metadata CreateMetadata(IPEImage peImage, CLRRuntimeReaderKind runtime, bool verify) => Create(peImage, runtime, verify);

		/// <summary>
		/// Create a <see cref="MetadataBase"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="runtime">Runtime reader kind</param>
		/// <param name="verify"><c>true</c> if we should verify that it's a .NET PE file</param>
		/// <returns>A new <see cref="MetadataBase"/> instance</returns>
		static MetadataBase Create(IPEImage peImage, CLRRuntimeReaderKind runtime, bool verify) {
			MetadataBase md = null;
			try {
				var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
				// Mono doesn't check that the Size field is >= 0x48
				if (dotNetDir.VirtualAddress == 0)
					throw new BadImageFormatException(".NET data directory RVA is 0");
				var cor20HeaderReader = peImage.CreateReader(dotNetDir.VirtualAddress, 0x48);
				var cor20Header = new ImageCor20Header(ref cor20HeaderReader, verify && runtime == CLRRuntimeReaderKind.CLR);
				if (cor20Header.Metadata.VirtualAddress == 0)
					throw new BadImageFormatException(".NET metadata RVA is 0");
				var mdRva = cor20Header.Metadata.VirtualAddress;
				// Don't use the size field, Mono ignores it. Create a reader that can read to EOF.
				var mdHeaderReader = peImage.CreateReader(mdRva);
				var mdHeader = new MetadataHeader(ref mdHeaderReader, runtime, verify);
				if (verify) {
					foreach (var sh in mdHeader.StreamHeaders) {
						if ((ulong)sh.Offset + sh.StreamSize > mdHeaderReader.EndOffset)
							throw new BadImageFormatException("Invalid stream header");
					}
				}

				md = GetMetadataType(mdHeader.StreamHeaders, runtime) switch {
					MetadataType.Compressed => new CompressedMetadata(peImage, cor20Header, mdHeader, runtime),
					MetadataType.ENC => new ENCMetadata(peImage, cor20Header, mdHeader, runtime),
					_ => throw new BadImageFormatException("No #~ or #- stream found"),
				};
				md.Initialize(null);

				return md;
			}
			catch {
				if (!(md is null))
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
			const CLRRuntimeReaderKind runtime = CLRRuntimeReaderKind.CLR;
			MetadataBase md = null;
			try {
				var reader = mdReaderFactory.CreateReader();
				var mdHeader = new MetadataHeader(ref reader, runtime, verify);
				if (verify) {
					foreach (var sh in mdHeader.StreamHeaders) {
						if (sh.Offset + sh.StreamSize < sh.Offset || sh.Offset + sh.StreamSize > reader.Length)
							throw new BadImageFormatException("Invalid stream header");
					}
				}

				md = GetMetadataType(mdHeader.StreamHeaders, runtime) switch {
					MetadataType.Compressed => new CompressedMetadata(mdHeader, true, runtime),
					MetadataType.ENC => new ENCMetadata(mdHeader, true, runtime),
					_ => throw new BadImageFormatException("No #~ or #- stream found"),
				};
				md.Initialize(mdReaderFactory);

				return md;
			}
			catch {
				md?.Dispose();
				throw;
			}
		}

		static MetadataType GetMetadataType(IList<StreamHeader> streamHeaders, CLRRuntimeReaderKind runtime) {
			MetadataType? mdType = null;
			if (runtime == CLRRuntimeReaderKind.CLR) {
				foreach (var sh in streamHeaders) {
					if (mdType is null) {
						if (sh.Name == "#~")
							mdType = MetadataType.Compressed;
						else if (sh.Name == "#-")
							mdType = MetadataType.ENC;
					}
					if (sh.Name == "#Schema")
						mdType = MetadataType.ENC;
				}
			}
			else if (runtime == CLRRuntimeReaderKind.Mono) {
				foreach (var sh in streamHeaders) {
					if (sh.Name == "#~")
						mdType = MetadataType.Compressed;
					else if (sh.Name == "#-") {
						mdType = MetadataType.ENC;
						break;
					}
				}
			}
			else
				throw new ArgumentOutOfRangeException(nameof(runtime));
			if (mdType is null)
				return MetadataType.Unknown;
			return mdType.Value;
		}
	}
}
