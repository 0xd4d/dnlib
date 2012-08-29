using System;
using System.Collections.Generic;
using System.IO;
using dot10.PE;

namespace dot10.dotNET {
	public class DotNetFile {
		public static DotNetFile Load(string filename) {
			return Load(new PEImage(filename));
		}

		public static DotNetFile Load(byte[] data) {
			return Load(new PEImage(data));
		}

		public static DotNetFile Load(IntPtr addr) {
			return Load(new PEImage(addr));
		}

		public static DotNetFile Load(IPEImage peImage) {
			bool verify = true;

			var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
			if (dotNetDir.VirtualAddress == RVA.Zero)
				throw new BadImageFormatException(".NET data directory RVA is 0");
			if (dotNetDir.Size < 0x48)
				throw new BadImageFormatException(".NET data directory size < 0x48");
			var cor20Header = new ImageCor20Header(peImage.CreateReader(dotNetDir.VirtualAddress, 0x48), verify);
			if (cor20Header.HasNativeHeader)
				throw new BadImageFormatException(".NET native header isn't supported");	//TODO: Fix this
			if (cor20Header.MetaData.VirtualAddress == RVA.Zero)
				throw new BadImageFormatException(".NET MetaData RVA is 0");
			if (cor20Header.MetaData.Size < 16)
				throw new BadImageFormatException(".NET MetaData size is too small");
			var mdSize = cor20Header.MetaData.Size;
			var mdRva = cor20Header.MetaData.VirtualAddress;
			var mdHeader = new MetaDataHeader(peImage.CreateReader(mdRva, mdSize), verify);
			if (verify) {
				foreach (var sh in mdHeader.StreamHeaders) {
					if (sh.Offset + sh.Size < sh.Offset || sh.Offset > mdSize || sh.Offset + sh.Size > mdSize)
						throw new BadImageFormatException("Invalid stream header");
				}
			}

			var allStreams = new List<DotNetStream>(mdHeader.StreamHeaders.Count);
			StringsStream stringsStream = null;
			USStream usStream = null;
			BlobStream blobStream = null;
			GuidStream guidStream = null;
			foreach (var sh in mdHeader.StreamHeaders) {
				var data = peImage.CreateStream(mdRva + sh.Offset, sh.Size);
				switch (sh.Name) {
				case "#Strings":
					allStreams.Add(stringsStream = new StringsStream(data, sh));
					break;

				case "#US":
					allStreams.Add(usStream = new USStream(data, sh));
					break;

				case "#Blob":
					allStreams.Add(blobStream = new BlobStream(data, sh));
					break;

				case "#GUID":
					allStreams.Add(guidStream = new GuidStream(data, sh));
					break;

				case "#~":
				case "#-":
				case "#Schema":
				default:
					allStreams.Add(new DotNetStream(data, sh));
					break;
				}
			}

			throw new NotImplementedException();	//TODO:
		}
	}
}
