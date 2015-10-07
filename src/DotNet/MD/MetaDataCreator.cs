// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Low level access to a .NET file's metadata
	/// </summary>
	public static class MetaDataCreator {
		enum MetaDataType {
			Unknown,
			Compressed,	// #~ (normal)
			ENC,		// #- (edit and continue)
		}

		/// <summary>
		/// Create a <see cref="MetaData"/> instance
		/// </summary>
		/// <param name="fileName">The file to load</param>
		/// <returns>A new <see cref="MetaData"/> instance</returns>
		internal static MetaData Load(string fileName) {
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
		/// Create a <see cref="MetaData"/> instance
		/// </summary>
		/// <param name="data">The .NET file data</param>
		/// <returns>A new <see cref="MetaData"/> instance</returns>
		internal static MetaData Load(byte[] data) {
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
		/// Create a <see cref="MetaData"/> instance
		/// </summary>
		/// <param name="addr">Address of a .NET file in memory</param>
		/// <returns>A new <see cref="MetaData"/> instance</returns>
		internal static MetaData Load(IntPtr addr) {
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
		/// Create a <see cref="MetaData"/> instance
		/// </summary>
		/// <param name="addr">Address of a .NET file in memory</param>
		/// <param name="imageLayout">Image layout of the file in memory</param>
		/// <returns>A new <see cref="MetaData"/> instance</returns>
		internal static MetaData Load(IntPtr addr, ImageLayout imageLayout) {
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
		/// Create a <see cref="MetaData"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <returns>A new <see cref="MetaData"/> instance</returns>
		internal static MetaData Load(IPEImage peImage) {
			return Create(peImage, true);
		}

		/// <summary>
		/// Create a <see cref="IMetaData"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <returns>A new <see cref="IMetaData"/> instance</returns>
		public static IMetaData CreateMetaData(IPEImage peImage) {
			return Create(peImage, true);
		}

		/// <summary>
		/// Create a <see cref="IMetaData"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="verify"><c>true</c> if we should verify that it's a .NET PE file</param>
		/// <returns>A new <see cref="IMetaData"/> instance</returns>
		public static IMetaData CreateMetaData(IPEImage peImage, bool verify) {
			return Create(peImage, verify);
		}

		/// <summary>
		/// Create a <see cref="MetaData"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="verify"><c>true</c> if we should verify that it's a .NET PE file</param>
		/// <returns>A new <see cref="MetaData"/> instance</returns>
		internal static MetaData Create(IPEImage peImage, bool verify) {
			IImageStream cor20HeaderStream = null, mdHeaderStream = null;
			MetaData md = null;
			try {
				var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
				if (dotNetDir.VirtualAddress == 0)
					throw new BadImageFormatException(".NET data directory RVA is 0");
				if (dotNetDir.Size < 0x48)
					throw new BadImageFormatException(".NET data directory size < 0x48");
				var cor20Header = new ImageCor20Header(cor20HeaderStream = peImage.CreateStream(dotNetDir.VirtualAddress, 0x48), verify);
				if (cor20Header.MetaData.VirtualAddress == 0)
					throw new BadImageFormatException(".NET MetaData RVA is 0");
				if (cor20Header.MetaData.Size < 16)
					throw new BadImageFormatException(".NET MetaData size is too small");
				var mdSize = cor20Header.MetaData.Size;
				var mdRva = cor20Header.MetaData.VirtualAddress;
				var mdHeader = new MetaDataHeader(mdHeaderStream = peImage.CreateStream(mdRva, mdSize), verify);
				if (verify) {
					foreach (var sh in mdHeader.StreamHeaders) {
						if (sh.Offset + sh.StreamSize < sh.Offset || sh.Offset + sh.StreamSize > mdSize)
							throw new BadImageFormatException("Invalid stream header");
					}
				}

				switch (GetMetaDataType(mdHeader.StreamHeaders)) {
				case MetaDataType.Compressed:
					md = new CompressedMetaData(peImage, cor20Header, mdHeader);
					break;

				case MetaDataType.ENC:
					md = new ENCMetaData(peImage, cor20Header, mdHeader);
					break;

				default:
					throw new BadImageFormatException("No #~ or #- stream found");
				}
				md.Initialize();

				return md;
			}
			catch {
				if (md != null)
					md.Dispose();
				throw;
			}
			finally {
				if (cor20HeaderStream != null)
					cor20HeaderStream.Dispose();
				if (mdHeaderStream != null)
					mdHeaderStream.Dispose();
			}
		}

		static MetaDataType GetMetaDataType(IList<StreamHeader> streamHeaders) {
			MetaDataType? mdType = null;
			foreach (var sh in streamHeaders) {
				if (mdType == null) {
					if (sh.Name == "#~")
						mdType = MetaDataType.Compressed;
					else if (sh.Name == "#-")
						mdType = MetaDataType.ENC;
				}
				if (sh.Name == "#Schema")
					mdType = MetaDataType.ENC;
			}
			if (mdType == null)
				return MetaDataType.Unknown;
			return mdType.Value;
		}
	}
}
