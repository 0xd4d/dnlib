// dnlib: See LICENSE.txt for more info

using System.IO;
using System.IO.Compression;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Pdb.Portable {
	static class SymbolReaderCreator {
		public static SymbolReader TryCreate(IImageStream pdbStream, bool isEmbeddedPortablePdb) {
			try {
				if (pdbStream != null) {
					pdbStream.Position = 0;
					if (pdbStream.ReadUInt32() == 0x424A5342) {
						pdbStream.Position = 0;
						return new PortablePdbReader(pdbStream, isEmbeddedPortablePdb ? PdbFileKind.EmbeddedPortablePDB : PdbFileKind.PortablePDB);
					}
				}
			}
			catch (IOException) {
			}
			if (pdbStream != null)
				pdbStream.Dispose();
			return null;
		}

		public static SymbolReader TryCreate(IMetaData metaData) {
			if (metaData == null)
				return null;
			try {
				var peImage = metaData.PEImage;
				if (peImage == null)
					return null;
				var embeddedDir = TryGetEmbeddedDebugDirectory(peImage);
				if (embeddedDir == null)
					return null;
				using (var reader = peImage.CreateStream(embeddedDir.PointerToRawData, embeddedDir.SizeOfData)) {
					// "MPDB" = 0x4244504D
					if (reader.ReadUInt32() != 0x4244504D)
						return null;
					uint uncompressedSize = reader.ReadUInt32();
					if (uncompressedSize > int.MaxValue)
						return null;
					var decompressedBytes = new byte[uncompressedSize];
					using (var deflateStream = new DeflateStream(new MemoryStream(reader.ReadRemainingBytes()), CompressionMode.Decompress)) {
						int pos = 0;
						while (pos < decompressedBytes.Length) {
							int read = deflateStream.Read(decompressedBytes, pos, decompressedBytes.Length - pos);
							if (read == 0)
								break;
							pos += read;
						}
						if (pos != decompressedBytes.Length)
							return null;
						var stream = MemoryImageStream.Create(decompressedBytes);
						return TryCreate(stream, true);
					}
				}
			}
			catch (IOException) {
			}
			return null;
		}

		static ImageDebugDirectory TryGetEmbeddedDebugDirectory(IPEImage peImage) {
			foreach (var idd in peImage.ImageDebugDirectories) {
				if (idd.Type == ImageDebugType.EmbeddedPortablePdb)
					return idd;
			}
			return null;
		}
	}
}
