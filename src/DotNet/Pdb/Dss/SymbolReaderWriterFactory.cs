// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using dnlib.IO;
using dnlib.DotNet.Pdb.Symbols;
using System.Diagnostics;
using System.Runtime.Versioning;
using dnlib.PE;
using dnlib.DotNet.Writer;
using dnlib.DotNet.Pdb.WindowsPdb;

namespace dnlib.DotNet.Pdb.Dss {
#if NETCOREAPP
	[SupportedOSPlatform("windows")]
#endif
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

		[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories | DllImportSearchPath.AssemblyDirectory)]
		[DllImport("Microsoft.DiaSymReader.Native.arm64.dll", EntryPoint = "CreateSymReader")]
		static extern void CreateSymReader_arm64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symReader);

		[DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
		[DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymWriter")]
		static extern void CreateSymWriter_x86(ref Guid guid, [MarshalAs(UnmanagedType.IUnknown)] out object symWriter);

		[DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
		[DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymWriter")]
		static extern void CreateSymWriter_x64(ref Guid guid, [MarshalAs(UnmanagedType.IUnknown)] out object symWriter);

		[DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
		[DllImport("Microsoft.DiaSymReader.Native.arm.dll", EntryPoint = "CreateSymWriter")]
		static extern void CreateSymWriter_arm(ref Guid guid, [MarshalAs(UnmanagedType.IUnknown)] out object symWriter);

		[DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
		[DllImport("Microsoft.DiaSymReader.Native.arm64.dll", EntryPoint = "CreateSymWriter")]
		static extern void CreateSymWriter_arm64(ref Guid guid, [MarshalAs(UnmanagedType.IUnknown)] out object symWriter);

		static readonly Guid CLSID_CorSymReader_SxS = new Guid("0A3976C5-4529-4ef8-B0B0-42EED37082CD");
		static Type CorSymReader_Type;

		static readonly Guid CLSID_CorSymWriter_SxS = new Guid(0x0AE2DEB0, 0xF901, 0x478B, 0xBB, 0x9F, 0x88, 0x1E, 0xE8, 0x06, 0x67, 0x88);
		static Type CorSymWriterType;

		static volatile bool canTry_Microsoft_DiaSymReader_Native = true;

		/// <summary>
		/// <c>false</c> if built-in COM interop has been disabled. It's disabled by default in trimmed and
		/// NativeAOT apps, and the trimmer then removes the code that uses COM.
		/// </summary>
#if NET9_0_OR_GREATER
		[FeatureSwitchDefinition(BuiltInComInteropSwitchName)]
#endif
		static bool IsBuiltInComSupported {
			get {
#if NET
				if (AppContext.TryGetSwitch(BuiltInComInteropSwitchName, out bool isSupported))
					return isSupported;
#endif
				return true;
			}
		}
		const string BuiltInComInteropSwitchName = "System.Runtime.InteropServices.BuiltInComInterop.IsSupported";

		const string ComJustification = "Only called if built-in COM interop is enabled. It's disabled by default in trimmed and AOT " +
			"apps, and the trimmer then removes this code. If an app enables it, KeepComInterfaces() keeps all COM interface members.";
		const string ComTypeJustification = "The type is a COM class (from its CLSID) and not a managed type that could be trimmed. " + ComJustification;

		/// <summary>
		/// The trimmer removes unused interface members, but a COM interface's vtable must have all of its methods.
		/// This method is only kept (and only keeps them) if the COM code isn't removed by the trimmer.
		/// </summary>
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IMetaDataEmit))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IMetaDataImport))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IPdbWriter))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedAsyncMethod))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedAsyncMethodPropertiesWriter))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedConstant))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedDispose))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedDocument))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedDocumentWriter))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedMethod))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedMethod2))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedNamespace))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedReader))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedReader2))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedReader3))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedReader4))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedReader5))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedScope))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedScope2))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedSourceServerModule))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedVariable))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedWriter))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedWriter2))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedWriter3))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedWriter4))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedWriter5))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedWriter6))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedWriter7))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISymUnmanagedWriter8))]
		static void KeepComInterfaces() { }

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

		[UnconditionalSuppressMessage("Trimming", "IL2050", Justification = ComJustification)]
		[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = ComTypeJustification)]
		[UnconditionalSuppressMessage("Trimming", "IL2077", Justification = ComTypeJustification)]
		static ISymUnmanagedReader CreateSymUnmanagedReader(PdbReaderOptions options) {
			if (!IsBuiltInComSupported)
				return null;
			KeepComInterfaces();
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

					case Machine.ARM64:
						CreateSymReader_arm64(ref guid, out symReaderObj);
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
				return (ISymUnmanagedReader)Activator.CreateInstance(CorSymReader_Type ??= Type.GetTypeFromCLSID(CLSID_CorSymReader_SxS));

			return null;
		}

		[UnconditionalSuppressMessage("Trimming", "IL2050", Justification = ComJustification)]
		[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = ComTypeJustification)]
		[UnconditionalSuppressMessage("Trimming", "IL2077", Justification = ComTypeJustification)]
		static ISymUnmanagedWriter2 CreateSymUnmanagedWriter2(PdbWriterOptions options) {
			if (!IsBuiltInComSupported)
				return null;
			KeepComInterfaces();
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

					case Machine.ARM64:
						CreateSymWriter_arm64(ref guid, out symWriterObj);
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
				return (ISymUnmanagedWriter2)Activator.CreateInstance(CorSymWriterType ??= Type.GetTypeFromCLSID(CLSID_CorSymWriter_SxS));

			return null;
		}

		public static SymbolWriter Create(PdbWriterOptions options, string pdbFileName) {
			// Create the COM writer first so the old PDB isn't deleted if there's no COM writer
			var writer = CreateSymUnmanagedWriter2(options);
			if (writer is null)
				return null;
			FileStream pdbStream = null;
			try {
				if (File.Exists(pdbFileName))
					File.Delete(pdbFileName);
				pdbStream = File.Create(pdbFileName);
				return new SymbolWriterImpl(writer, pdbFileName, pdbStream, options, ownsStream: true);
			}
			catch {
				Marshal.FinalReleaseComObject(writer);
				if (pdbStream is not null) {
					// We created it, don't leave an open empty file behind
					pdbStream.Dispose();
					try {
						File.Delete(pdbFileName);
					}
					catch {
					}
				}
				throw;
			}
		}

		public static SymbolWriter Create(PdbWriterOptions options, Stream pdbStream, string pdbFileName) {
			var writer = CreateSymUnmanagedWriter2(options);
			if (writer is null)
				return null;
			try {
				return new SymbolWriterImpl(writer, pdbFileName, pdbStream, options, ownsStream: false);
			}
			catch {
				Marshal.FinalReleaseComObject(writer);
				throw;
			}
		}
	}
}
