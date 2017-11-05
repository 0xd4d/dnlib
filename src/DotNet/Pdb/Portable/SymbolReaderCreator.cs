// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Portable {
	static class SymbolReaderCreator {
		public static SymbolReader TryCreate(IMetaData metaData, IImageStream pdbStream) {
			try {
				if (metaData != null && pdbStream != null) {
					pdbStream.Position = 0;
					if (pdbStream.ReadUInt32() == 0x424A5342) {
						pdbStream.Position = 0;
						return new PortablePdbReader(metaData, pdbStream);
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
				//TODO:
			}
			catch (IOException) {
			}
			return null;
		}
	}
}
