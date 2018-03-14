// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb {
	static class ManagedSymbolReaderCreator {
		public static SymbolReader CreateFromAssemblyFile(Metadata metadata, string assemblyFileName) =>
			Create(metadata, Path.ChangeExtension(assemblyFileName, "pdb"));

		public static SymbolReader Create(Metadata metadata, string pdbFileName) =>
			Create(metadata, DataReaderFactoryUtils.TryCreateDataReaderFactory(pdbFileName));

		public static SymbolReader Create(Metadata metadata, byte[] pdbData) =>
			Create(metadata, ByteArrayDataReaderFactory.Create(pdbData, filename: null));

		public static SymbolReader Create(Metadata metadata, DataReaderFactory pdbStream) {
			try {
				// Embedded pdbs have priority
				var res = Create(metadata);
				if (res != null) {
					pdbStream?.Dispose();
					return res;
				}

				return CreateCore(pdbStream);
			}
			catch {
				pdbStream?.Dispose();
				throw;
			}
		}

		static SymbolReader CreateCore(DataReaderFactory pdbStream) {
			if (pdbStream == null)
				return null;
			try {
				uint sig = pdbStream.CreateReader().ReadUInt32();
				if (sig == 0x424A5342)
					return Portable.SymbolReaderCreator.TryCreate(pdbStream, false);
				return Managed.SymbolReaderCreator.Create(pdbStream);
			}
			catch (IOException) {
			}
			pdbStream?.Dispose();
			return null;
		}

		internal static SymbolReader Create(Metadata metadata) => Portable.SymbolReaderCreator.TryCreate(metadata);
	}
}
