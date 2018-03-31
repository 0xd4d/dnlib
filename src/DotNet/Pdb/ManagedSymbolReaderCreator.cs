// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb {
	static class ManagedSymbolReaderCreator {
		public static SymbolReader CreateFromAssemblyFile(PdbReaderContext pdbContext, Metadata metadata, string assemblyFileName) =>
			Create(pdbContext, metadata, Path.ChangeExtension(assemblyFileName, "pdb"));

		public static SymbolReader Create(PdbReaderContext pdbContext, Metadata metadata, string pdbFileName) =>
			Create(pdbContext, metadata, DataReaderFactoryUtils.TryCreateDataReaderFactory(pdbFileName));

		public static SymbolReader Create(PdbReaderContext pdbContext, Metadata metadata, byte[] pdbData) =>
			Create(pdbContext, metadata, ByteArrayDataReaderFactory.Create(pdbData, filename: null));

		public static SymbolReader Create(PdbReaderContext pdbContext, Metadata metadata, DataReaderFactory pdbStream) {
			try {
				// Embedded pdbs have priority
				var res = Create(pdbContext, metadata);
				if (res != null) {
					pdbStream?.Dispose();
					return res;
				}

				return CreateCore(pdbContext, pdbStream);
			}
			catch {
				pdbStream?.Dispose();
				throw;
			}
		}

		static SymbolReader CreateCore(PdbReaderContext pdbContext, DataReaderFactory pdbStream) {
			if (pdbStream == null)
				return null;
			try {
				var reader = pdbStream.CreateReader();
				if (reader.Length >= 4) {
					uint sig = reader.ReadUInt32();
					if (sig == 0x424A5342)
						return Portable.SymbolReaderCreator.TryCreate(pdbContext, pdbStream, isEmbeddedPortablePdb: false);
					return Managed.SymbolReaderCreator.Create(pdbContext, pdbStream);
				}
			}
			catch (IOException) {
			}
			pdbStream?.Dispose();
			return null;
		}

		internal static SymbolReader Create(PdbReaderContext pdbContext, Metadata metadata) =>
			Portable.SymbolReaderCreator.TryCreate(pdbContext, metadata);
	}
}
