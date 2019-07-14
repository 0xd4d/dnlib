// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Runtime.InteropServices;
using dnlib.IO;
using dnlib.DotNet.Pdb.Symbols;
using System.Diagnostics;
using dnlib.PE;
using dnlib.DotNet.Writer;
using dnlib.DotNet.Pdb.WindowsPdb;

namespace dnlib.DotNet.Pdb.Dss {
	static class SymbolReaderWriterFactory {
		[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories | DllImportSearchPath.AssemblyDirectory)]
		[DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymReader")]
		static extern void CreateSymReader_x86(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symReader);

		[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories | DllImportSearchPath.AssemblyDirectory)]
		[DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymReader")]
		static extern void CreateSymReader_x64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symReader);

		[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories | DllImportSearchPath.AssemblyDirectory)]
		[DllImport("Microsoft.DiaSymReader.Native.arm.dll", EntryPoint = "CreateSymReader")]
		static extern void CreateSymReader_arm(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symReader);

		[DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
		[DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymWriter")]
		static extern void CreateSymWriter_x86(ref Guid guid, [MarshalAs(UnmanagedType.IUnknown)] out object symWriter);

		[DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
		[DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymWriter")]
		static extern void CreateSymWriter_x64(ref Guid guid, [MarshalAs(UnmanagedType.IUnknown)] out object symWriter);

		[DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
		[DllImport("Microsoft.DiaSymReader.Native.arm.dll", EntryPoint = "CreateSymWriter")]
		static extern void CreateSymWriter_arm(ref Guid guid, [MarshalAs(UnmanagedType.IUnknown)] out object symWriter);

		static readonly Guid CLSID_CorSymReader_SxS = new Guid("0A3976C5-4529-4ef8-B0B0-42EED37082CD");
		static Type CorSymReader_Type;

		static readonly Guid CLSID_CorSymWriter_SxS = new Guid(0x0AE2DEB0, 0xF901, 0x478B, 0xBB, 0x9F, 0x88, 0x1E, 0xE8, 0x06, 0x67, 0x88);
		static Type CorSymWriterType;

		static volatile bool canTry_Microsoft_DiaSymReader_Native = true;

		public static SymbolReader Create(PdbReaderContext pdbContext, MD.Metadata metadata, DataReaderFactory pdbStream) {
			ISymUnmanagedReader unmanagedReader = null;
			SymbolReaderImpl symReader = null;
			ReaderMetaDataImport mdImporter = null;
			DataReaderIStream comPdbStream = null;
			bool error = true;
			try {
				if (pdbStream is null)
					return null;
				var debugDir = pdbContext.CodeViewDebugDirectory;
				if (debugDir is null)
					return null;
				if (!pdbContext.TryGetCodeViewData(out var pdbGuid, out uint age))
					return null;

				unmanagedReader = CreateSymUnmanagedReader(pdbContext.Options);
				if (unmanagedReader is null)
					return null;

				mdImporter = new ReaderMetaDataImport(metadata);
				comPdbStream = new DataReaderIStream(pdbStream);
				int hr = unmanagedReader.Initialize(mdImporter, null, null, comPdbStream);
				if (hr < 0)
					return null;

				symReader = new SymbolReaderImpl(unmanagedReader, new object[] { pdbStream, mdImporter, comPdbStream });
				if (!symReader.MatchesModule(pdbGuid, debugDir.TimeDateStamp, age))
					return null;

				error = false;
				return symReader;
			}
			catch (IOException) {
			}
			catch (InvalidCastException) {
			}
			catch (COMException) {
			}
			finally {
				if (error) {
					pdbStream?.Dispose();
					symReader?.Dispose();
					mdImporter?.Dispose();
					comPdbStream?.Dispose();
					(unmanagedReader as ISymUnmanagedDispose)?.Destroy();
				}
			}
			return null;
		}

		static ISymUnmanagedReader CreateSymUnmanagedReader(PdbReaderOptions options) {
			bool useDiaSymReader = (options & PdbReaderOptions.NoDiaSymReader) == 0;
			bool useOldDiaSymReader = (options & PdbReaderOptions.NoOldDiaSymReader) == 0;

			if (useDiaSymReader && canTry_Microsoft_DiaSymReader_Native) {
				try {
					var guid = CLSID_CorSymReader_SxS;
					object symReaderObj;
					var machine = ProcessorArchUtils.GetProcessCpuArchitecture();
					switch (machine) {
					case Machine.AMD64:
						CreateSymReader_x64(ref guid, out symReaderObj);
						break;

					case Machine.I386:
						CreateSymReader_x86(ref guid, out symReaderObj);
						break;

					case Machine.ARMNT:
						CreateSymReader_arm(ref guid, out symReaderObj);
						break;

					default:
						Debug.Fail($"Microsoft.DiaSymReader.Native doesn't support this CPU arch: {machine}");
						symReaderObj = null;
						break;
					}
					if (symReaderObj is ISymUnmanagedReader symReader)
						return symReader;
				}
				catch (DllNotFoundException) {
					Debug.WriteLine("Microsoft.DiaSymReader.Native not found, using diasymreader.dll instead");
				}
				catch {
				}
				canTry_Microsoft_DiaSymReader_Native = false;
			}

			if (useOldDiaSymReader)
				return (ISymUnmanagedReader)Activator.CreateInstance(CorSymReader_Type ?? (CorSymReader_Type = Type.GetTypeFromCLSID(CLSID_CorSymReader_SxS)));

			return null;
		}

		static ISymUnmanagedWriter2 CreateSymUnmanagedWriter2(PdbWriterOptions options) {
			bool useDiaSymReader = (options & PdbWriterOptions.NoDiaSymReader) == 0;
			bool useOldDiaSymReader = (options & PdbWriterOptions.NoOldDiaSymReader) == 0;

			if (useDiaSymReader && canTry_Microsoft_DiaSymReader_Native) {
				try {
					var guid = CLSID_CorSymWriter_SxS;
					object symWriterObj;
					var machine = ProcessorArchUtils.GetProcessCpuArchitecture();
					switch (machine) {
					case Machine.AMD64:
						CreateSymWriter_x64(ref guid, out symWriterObj);
						break;

					case Machine.I386:
						CreateSymWriter_x86(ref guid, out symWriterObj);
						break;

					case Machine.ARMNT:
						CreateSymWriter_arm(ref guid, out symWriterObj);
						break;

					default:
						Debug.Fail($"Microsoft.DiaSymReader.Native doesn't support this CPU arch: {machine}");
						symWriterObj = null;
						break;
					}
					if (symWriterObj is ISymUnmanagedWriter2 symWriter)
						return symWriter;
				}
				catch (DllNotFoundException) {
					Debug.WriteLine("Microsoft.DiaSymReader.Native not found, using diasymreader.dll instead");
				}
				catch {
				}
				canTry_Microsoft_DiaSymReader_Native = false;
			}

			if (useOldDiaSymReader)
				return (ISymUnmanagedWriter2)Activator.CreateInstance(CorSymWriterType ?? (CorSymWriterType = Type.GetTypeFromCLSID(CLSID_CorSymWriter_SxS)));

			return null;
		}

		public static SymbolWriter Create(PdbWriterOptions options, string pdbFileName) {
			if (File.Exists(pdbFileName))
				File.Delete(pdbFileName);
			return new SymbolWriterImpl(CreateSymUnmanagedWriter2(options), pdbFileName, File.Create(pdbFileName), options, ownsStream: true);
		}

		public static SymbolWriter Create(PdbWriterOptions options, Stream pdbStream, string pdbFileName) =>
			new SymbolWriterImpl(CreateSymUnmanagedWriter2(options), pdbFileName, pdbStream, options, ownsStream: false);
	}
}
