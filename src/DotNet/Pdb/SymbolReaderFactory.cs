// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Text;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb {
	static class SymbolReaderFactory {
		public static SymbolReader CreateFromAssemblyFile(PdbReaderOptions options, Metadata metadata, string assemblyFileName) {
			var pdbContext = new PdbReaderContext(metadata.PEImage, options);
			if (!pdbContext.HasDebugInfo)
				return null;
			if (!pdbContext.TryGetCodeViewData(out var guid, out uint age, out var pdbWindowsFilename))
				return null;

			string pdbFilename;
			int index = pdbWindowsFilename.LastIndexOfAny(windowsPathSepChars);
			if (index >= 0)
				pdbFilename = pdbWindowsFilename.Substring(index + 1);
			else
				pdbFilename = pdbWindowsFilename;

			string fileToCheck;
			try {
				fileToCheck = assemblyFileName == string.Empty ? pdbFilename : Path.Combine(Path.GetDirectoryName(assemblyFileName), pdbFilename);
				if (!File.Exists(fileToCheck)) {
					var ext = Path.GetExtension(pdbFilename);
					if (string.IsNullOrEmpty(ext))
						ext = "pdb";
					fileToCheck = Path.ChangeExtension(assemblyFileName, ext);
				}
			}
			catch (ArgumentException) {
				return null;// Invalid filename
			}
			return Create(options, metadata, fileToCheck);
		}
		static readonly char[] windowsPathSepChars = new char[] { '\\', '/' };

		public static SymbolReader Create(PdbReaderOptions options, Metadata metadata, string pdbFileName) {
			var pdbContext = new PdbReaderContext(metadata.PEImage, options);
			if (!pdbContext.HasDebugInfo)
				return null;
			return CreateCore(pdbContext, metadata, DataReaderFactoryUtils.TryCreateDataReaderFactory(pdbFileName));
		}

		public static SymbolReader Create(PdbReaderOptions options, Metadata metadata, byte[] pdbData) {
			var pdbContext = new PdbReaderContext(metadata.PEImage, options);
			if (!pdbContext.HasDebugInfo)
				return null;
			return CreateCore(pdbContext, metadata, ByteArrayDataReaderFactory.Create(pdbData, filename: null));
		}

		public static SymbolReader Create(PdbReaderOptions options, Metadata metadata, DataReaderFactory pdbStream) {
			var pdbContext = new PdbReaderContext(metadata.PEImage, options);
			return CreateCore(pdbContext, metadata, pdbStream);
		}

		static SymbolReader CreateCore(PdbReaderContext pdbContext, Metadata metadata, DataReaderFactory pdbStream) {
			SymbolReader symReader = null;
			bool error = true;
			try {
				if (!pdbContext.HasDebugInfo)
					return null;

				if ((pdbContext.Options & PdbReaderOptions.MicrosoftComReader) != 0 && !(pdbStream is null) && IsWindowsPdb(pdbStream.CreateReader()))
					symReader = Dss.SymbolReaderWriterFactory.Create(pdbContext, metadata, pdbStream);
				else
					symReader = CreateManaged(pdbContext, metadata, pdbStream);

				if (!(symReader is null)) {
					error = false;
					return symReader;
				}
			}
			catch (IOException) {
			}
			finally {
				if (error) {
					pdbStream?.Dispose();
					symReader?.Dispose();
				}
			}
			return null;
		}

		static bool IsWindowsPdb(DataReader reader) {
			const string SIG = "Microsoft C/C++ MSF 7.00\r\n\u001ADS\0";
			if (!reader.CanRead(SIG.Length))
				return false;
			return reader.ReadString(SIG.Length, Encoding.ASCII) == SIG;
		}

		public static SymbolReader TryCreateEmbeddedPdbReader(PdbReaderOptions options, Metadata metadata) {
			var pdbContext = new PdbReaderContext(metadata.PEImage, options);
			if (!pdbContext.HasDebugInfo)
				return null;
			return TryCreateEmbeddedPortablePdbReader(pdbContext, metadata);
		}

		static SymbolReader CreateManaged(PdbReaderContext pdbContext, Metadata metadata, DataReaderFactory pdbStream) {
			try {
				// Embedded PDBs have priority
				var embeddedReader = TryCreateEmbeddedPortablePdbReader(pdbContext, metadata);
				if (!(embeddedReader is null)) {
					pdbStream?.Dispose();
					return embeddedReader;
				}

				return CreateManagedCore(pdbContext, pdbStream);
			}
			catch {
				pdbStream?.Dispose();
				throw;
			}
		}

		static SymbolReader CreateManagedCore(PdbReaderContext pdbContext, DataReaderFactory pdbStream) {
			if (pdbStream is null)
				return null;
			try {
				var reader = pdbStream.CreateReader();
				if (reader.Length >= 4) {
					uint sig = reader.ReadUInt32();
					if (sig == 0x424A5342)
						return Portable.SymbolReaderFactory.TryCreate(pdbContext, pdbStream, isEmbeddedPortablePdb: false);
					return Managed.SymbolReaderFactory.Create(pdbContext, pdbStream);
				}
			}
			catch (IOException) {
			}
			pdbStream?.Dispose();
			return null;
		}

		static SymbolReader TryCreateEmbeddedPortablePdbReader(PdbReaderContext pdbContext, Metadata metadata) =>
			Portable.SymbolReaderFactory.TryCreateEmbeddedPortablePdbReader(pdbContext, metadata);
	}
}
