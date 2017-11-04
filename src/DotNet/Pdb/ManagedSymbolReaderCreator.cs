// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Security;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb {
	static class ManagedSymbolReaderCreator {
		public static SymbolReader CreateFromAssemblyFile(IMetaData metaData, string assemblyFileName) {
			return Create(metaData, Path.ChangeExtension(assemblyFileName, "pdb"));
		}

		public static SymbolReader Create(IMetaData metaData, string pdbFileName) {
			return Create(metaData, OpenImageStream(pdbFileName));
		}

		public static SymbolReader Create(IMetaData metaData, byte[] pdbData) {
			return Create(metaData, MemoryImageStream.Create(pdbData));
		}

		public static SymbolReader Create(IMetaData metaData, IImageStream pdbStream) {
			try {
				// Embedded pdbs have priority
				var res = Create(metaData);
				if (res != null)
					return res;

				return CreateCore(metaData, pdbStream);
			}
			finally {
				if (pdbStream != null)
					pdbStream.Dispose();
			}
		}

		static SymbolReader CreateCore(IMetaData metaData, IImageStream pdbStream) {
			if (pdbStream == null)
				return null;
			try {
				uint sig = pdbStream.ReadUInt32();
				pdbStream.Position = 0;
				if (sig == 0x424A5342)
					return Portable.SymbolReaderCreator.TryCreate(metaData, pdbStream);
				return Managed.SymbolReaderCreator.Create(pdbStream);
			}
			catch (IOException) {
			}
			finally {
				if (pdbStream != null)
					pdbStream.Dispose();
			}
			return null;
		}

		internal static SymbolReader Create(IMetaData metaData) {
			return Portable.SymbolReaderCreator.TryCreate(metaData);
		}

		static IImageStream OpenImageStream(string fileName) {
			try {
				if (!File.Exists(fileName))
					return null;
				return ImageStreamCreator.CreateImageStream(fileName);
			}
			catch (IOException) {
			}
			catch (UnauthorizedAccessException) {
			}
			catch (SecurityException) {
			}
			return null;
		}
	}
}
