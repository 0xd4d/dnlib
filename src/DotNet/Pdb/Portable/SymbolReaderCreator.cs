// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Portable {
	static class SymbolReaderCreator {
		public static SymbolReader TryCreate(IMetaData metaData, IImageStream pdbStream) {
			if (metaData == null)
				return null;
			if (pdbStream == null)
				return null;
			try {
				pdbStream.Position = 0;
				if (pdbStream.ReadUInt32() != 0x424A5342)
					return null;
				pdbStream.Position = 0;
				return null;//TODO:
			}
			catch (IOException) {
			}
			finally {
				if (pdbStream != null)
					pdbStream.Dispose();
			}
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
