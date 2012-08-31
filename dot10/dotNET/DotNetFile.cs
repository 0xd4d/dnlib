using System;
using System.Collections.Generic;
using System.IO;
using dot10.PE;

namespace dot10.dotNET {
	/// <summary>
	/// 
	/// </summary>
	public class DotNetFile : IDisposable {
		IMetaData metaData;

		enum MetaDataType {
			Unknown,
			Compressed,	// #~ (normal)
			ENC,		// #- (edit and continue)
		}

		/// <summary>
		/// Create a <see cref="DotNetFile"/> instance
		/// </summary>
		/// <param name="filename">The file to load</param>
		/// <returns>A new <see cref="DotNetFile"/> instance</returns>
		public static DotNetFile Load(string filename) {
			return Load(new PEImage(filename));
		}

		/// <summary>
		/// Create a <see cref="DotNetFile"/> instance
		/// </summary>
		/// <param name="data">The .NET file data</param>
		/// <returns>A new <see cref="DotNetFile"/> instance</returns>
		public static DotNetFile Load(byte[] data) {
			return Load(new PEImage(data));
		}

		/// <summary>
		/// Create a <see cref="DotNetFile"/> instance
		/// </summary>
		/// <param name="addr">Address of a .NET file in memory</param>
		/// <returns>A new <see cref="DotNetFile"/> instance</returns>
		public static DotNetFile Load(IntPtr addr) {
			return Load(new PEImage(addr));
		}

		/// <summary>
		/// Create a <see cref="DotNetFile"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <returns>A new <see cref="DotNetFile"/> instance</returns>
		public static DotNetFile Load(IPEImage peImage) {
			return Load(peImage, true);
		}

		/// <summary>
		/// Create a <see cref="DotNetFile"/> instance
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="verify">true if we should verify that it's a .NET PE file</param>
		/// <returns>A new <see cref="DotNetFile"/> instance</returns>
		public static DotNetFile Load(IPEImage peImage, bool verify) {
			var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
			if (dotNetDir.VirtualAddress == RVA.Zero)
				throw new BadImageFormatException(".NET data directory RVA is 0");
			if (dotNetDir.Size < 0x48)
				throw new BadImageFormatException(".NET data directory size < 0x48");
			var cor20Header = new ImageCor20Header(peImage.CreateStream(dotNetDir.VirtualAddress, 0x48), verify);
			if (cor20Header.HasNativeHeader)
				throw new BadImageFormatException(".NET native header isn't supported");	//TODO: Fix this
			if (cor20Header.MetaData.VirtualAddress == RVA.Zero)
				throw new BadImageFormatException(".NET MetaData RVA is 0");
			if (cor20Header.MetaData.Size < 16)
				throw new BadImageFormatException(".NET MetaData size is too small");
			var mdSize = cor20Header.MetaData.Size;
			var mdRva = cor20Header.MetaData.VirtualAddress;
			var mdHeader = new MetaDataHeader(peImage.CreateStream(mdRva, mdSize), verify);
			if (verify) {
				foreach (var sh in mdHeader.StreamHeaders) {
					if (sh.Offset + sh.Size < sh.Offset || sh.Offset + sh.Size > mdSize)
						throw new BadImageFormatException("Invalid stream header");
				}
			}

			IMetaData md;
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

			return new DotNetFile(md);
		}

		DotNetFile(IMetaData metaData) {
			this.metaData = metaData;
		}

		static MetaDataType GetMetaDataType(IList<StreamHeader> streamHeaders) {
			foreach (var sh in streamHeaders) {
				if (sh.Name == "#~")
					return MetaDataType.Compressed;
				if (sh.Name == "#-")
					return MetaDataType.ENC;
			}
			return MetaDataType.Unknown;
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (metaData != null)
				metaData.Dispose();
			metaData = null;
		}
	}
}
