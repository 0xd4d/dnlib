// dnlib: See LICENSE.txt for more info

using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Pdb.Portable {
	static class SymbolReaderFactory {
		public static SymbolReader TryCreate(PdbReaderContext pdbContext, DataReaderFactory pdbStream, bool isEmbeddedPortablePdb) {
			bool disposePdbStream = true;
			try {
				if (!pdbContext.HasDebugInfo)
					return null;
				if (pdbStream is null)
					return null;
				if (pdbStream.Length < 4)
					return null;
				if (pdbStream.CreateReader().ReadUInt32() != 0x424A5342)
					return null;

				var debugDir = pdbContext.CodeViewDebugDirectory;
				if (debugDir is null)
					return null;
				// Don't check that debugDir.MinorVersion == PortablePdbConstants.PortableCodeViewVersionMagic
				// and debugDir.MajorVersion == PortablePdbConstants.FormatVersion since it could be a converted
				// WindowsPDB file: https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PE-COFF.md#codeview-debug-directory-entry-type-2
				if (!pdbContext.TryGetCodeViewData(out var pdbGuid, out uint age))
					return null;

				var reader = new PortablePdbReader(pdbStream, isEmbeddedPortablePdb ? PdbFileKind.EmbeddedPortablePDB : PdbFileKind.PortablePDB);
				if (!reader.MatchesModule(pdbGuid, debugDir.TimeDateStamp, age))
					return null;
				disposePdbStream = false;
				return reader;
			}
			catch (IOException) {
			}
			finally {
				if (disposePdbStream)
					pdbStream?.Dispose();
			}
			return null;
		}

		public static SymbolReader TryCreateEmbeddedPortablePdbReader(PdbReaderContext pdbContext, Metadata metadata) {
			if (metadata is null)
				return null;
			try {
				if (!pdbContext.HasDebugInfo)
					return null;
				var embeddedDir = pdbContext.TryGetDebugDirectoryEntry(ImageDebugType.EmbeddedPortablePdb);
				if (embeddedDir is null)
					return null;
				var reader = pdbContext.CreateReader(embeddedDir.AddressOfRawData, embeddedDir.SizeOfData);
				if (reader.Length < 8)
					return null;
				// "MPDB" = 0x4244504D
				if (reader.ReadUInt32() != 0x4244504D)
					return null;
				uint uncompressedSize = reader.ReadUInt32();
				// If this fails, see the (hopefully) updated spec:
				//		https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PE-COFF.md#embedded-portable-pdb-debug-directory-entry-type-17
				bool newVersion = (uncompressedSize & 0x80000000) != 0;
				Debug.Assert(!newVersion);
				if (newVersion)
					return null;
				var decompressedBytes = new byte[uncompressedSize];
				using (var deflateStream = new DeflateStream(reader.AsStream(), CompressionMode.Decompress)) {
					int pos = 0;
					while (pos < decompressedBytes.Length) {
						int read = deflateStream.Read(decompressedBytes, pos, decompressedBytes.Length - pos);
						if (read == 0)
							break;
						pos += read;
					}
					if (pos != decompressedBytes.Length)
						return null;
					var stream = ByteArrayDataReaderFactory.Create(decompressedBytes, filename: null);
					return TryCreate(pdbContext, stream, isEmbeddedPortablePdb: true);
				}
			}
			catch (IOException) {
			}
			return null;
		}
	}
}
