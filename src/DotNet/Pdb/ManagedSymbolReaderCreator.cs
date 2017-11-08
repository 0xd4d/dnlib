// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb {
	static class ManagedSymbolReaderCreator {
		public static SymbolReader CreateFromAssemblyFile(IMetaData metaData, string assemblyFileName) {
			return Create(metaData, Path.ChangeExtension(assemblyFileName, "pdb"));
		}

		public static SymbolReader Create(IMetaData metaData, string pdbFileName) {
			return Create(metaData, ImageStreamUtils.OpenImageStream(pdbFileName));
		}

		public static SymbolReader Create(IMetaData metaData, byte[] pdbData) {
			return Create(metaData, MemoryImageStream.Create(pdbData));
		}

		public static SymbolReader Create(IMetaData metaData, IImageStream pdbStream) {
			try {
				// Embedded pdbs have priority
				var res = Create(metaData);
				if (res != null) {
					if (pdbStream != null)
						pdbStream.Dispose();
					return res;
				}

				return CreateCore(pdbStream);
			}
			catch {
				if (pdbStream != null)
					pdbStream.Dispose();
				throw;
			}
		}

		static SymbolReader CreateCore(IImageStream pdbStream) {
			if (pdbStream == null)
				return null;
			try {
				uint sig = pdbStream.ReadUInt32();
				pdbStream.Position = 0;
				if (sig == 0x424A5342)
					return Portable.SymbolReaderCreator.TryCreate(pdbStream, false);
				return Managed.SymbolReaderCreator.Create(pdbStream);
			}
			catch (IOException) {
			}
			if (pdbStream != null)
				pdbStream.Dispose();
			return null;
		}

		internal static SymbolReader Create(IMetaData metaData) {
			return Portable.SymbolReaderCreator.TryCreate(metaData);
		}
	}
}
