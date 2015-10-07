// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;
using System.Security;
using dnlib.DotNet.MD;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Pdb.Dss {
	/// <summary>
	/// Creates a <see cref="ISymbolReader"/> instance
	/// </summary>
	public static class SymbolReaderCreator {
		[DllImport("ole32")]
		static extern int CoCreateInstance([In] ref Guid rclsid, IntPtr pUnkOuter, [In] uint dwClsContext, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppv);

		static readonly Guid CLSID_CorSymBinder_SxS = new Guid(0x0A29FF9E, 0x7F9C, 0x4437, 0x8B, 0x11, 0xF4, 0x24, 0x49, 0x1E, 0x39, 0x31);

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="assemblyFileName">Path to assembly</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk or if any of the COM methods fail.</returns>
		public static ISymbolReader Create(string assemblyFileName) {
			try {
				object mdDispObj;
				Guid CLSID_CorMetaDataDispenser = new Guid(0xE5CB7A31, 0x7512, 0x11D2, 0x89, 0xCE, 0x0, 0x80, 0xC7, 0x92, 0xE5, 0xD8);
				Guid IID_IMetaDataDispenser = new Guid(0x809C652E, 0x7396, 0x11D2, 0x97, 0x71, 0x00, 0xA0, 0xC9, 0xB4, 0xD5, 0x0C);
				int hr = CoCreateInstance(ref CLSID_CorMetaDataDispenser, IntPtr.Zero, 1, ref IID_IMetaDataDispenser, out mdDispObj);
				if (hr < 0)
					return null;

				object mdImportObj;
				var mdDisp = (IMetaDataDispenser)mdDispObj;
				Guid IID_IMetaDataImport = new Guid(0x7DAC8207, 0xD3AE, 0x4C75, 0x9B, 0x67, 0x92, 0x80, 0x1A, 0x49, 0x7D, 0x44);
				mdDisp.OpenScope(assemblyFileName, 0, ref IID_IMetaDataImport, out mdImportObj);
				Marshal.FinalReleaseComObject(mdDispObj);

				ISymUnmanagedReader symReader;
				var binder = (ISymUnmanagedBinder)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_CorSymBinder_SxS));
				hr = binder.GetReaderForFile((IMetaDataImport)mdImportObj, assemblyFileName, null, out symReader);
				Marshal.FinalReleaseComObject(mdImportObj);
				Marshal.FinalReleaseComObject(binder);
				if (hr >= 0)
					return new SymbolReader(symReader);
			}
			catch (IOException) {
			}
			catch (UnauthorizedAccessException) {
			}
			catch (SecurityException) {
			}
			catch (InvalidCastException) {
			}
			catch (COMException) {
			}
			return null;
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

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="metaData">.NET metadata</param>
		/// <param name="pdbFileName">Path to PDB file</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk or if any of the COM methods fail.</returns>
		public static ISymbolReader Create(IMetaData metaData, string pdbFileName) {
			var mdStream = CreateMetaDataStream(metaData);
			try {
				return Create(mdStream, OpenImageStream(pdbFileName));
			}
			catch {
				if (mdStream != null)
					mdStream.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="metaData">.NET metadata</param>
		/// <param name="pdbData">PDB file data</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if any of the COM
		/// methods fail.</returns>
		public static ISymbolReader Create(IMetaData metaData, byte[] pdbData) {
			if (pdbData == null)
				return null;
			var mdStream = CreateMetaDataStream(metaData);
			try {
				return Create(mdStream, MemoryImageStream.Create(pdbData));
			}
			catch {
				if (mdStream != null)
					mdStream.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="metaData">.NET metadata</param>
		/// <param name="pdbStream">PDB file stream which is now owned by this method</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if any of the COM
		/// methods fail.</returns>
		public static ISymbolReader Create(IMetaData metaData, IImageStream pdbStream) {
			try {
				return Create(CreateMetaDataStream(metaData), pdbStream);
			}
			catch {
				if (pdbStream != null)
					pdbStream.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="mdStream">.NET metadata stream which is now owned by this method</param>
		/// <param name="pdbFileName">Path to PDB file</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk or if any of the COM methods fail.</returns>
		public static ISymbolReader Create(IImageStream mdStream, string pdbFileName) {
			try {
				return Create(mdStream, OpenImageStream(pdbFileName));
			}
			catch {
				if (mdStream != null)
					mdStream.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="mdStream">.NET metadata stream which is now owned by this method</param>
		/// <param name="pdbData">PDB file data</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if any of the COM
		/// methods fail.</returns>
		public static ISymbolReader Create(IImageStream mdStream, byte[] pdbData) {
			if (pdbData == null) {
				if (mdStream != null)
					mdStream.Dispose();
				return null;
			}
			try {
				return Create(mdStream, MemoryImageStream.Create(pdbData));
			}
			catch {
				if (mdStream != null)
					mdStream.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="mdStream">.NET metadata stream which is now owned by this method</param>
		/// <param name="pdbStream">PDB file stream which is now owned by this method</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if any of the COM
		/// methods fail.</returns>
		public static ISymbolReader Create(IImageStream mdStream, IImageStream pdbStream) {
			ImageStreamIStream stream = null;
			PinnedMetaData pinnedMd = null;
			bool error = true;
			try {
				if (pdbStream == null || mdStream == null)
					return null;

				object mdDispObj;
				Guid CLSID_CorMetaDataDispenser = new Guid(0xE5CB7A31, 0x7512, 0x11D2, 0x89, 0xCE, 0x0, 0x80, 0xC7, 0x92, 0xE5, 0xD8);
				Guid IID_IMetaDataDispenser = new Guid(0x809C652E, 0x7396, 0x11D2, 0x97, 0x71, 0x00, 0xA0, 0xC9, 0xB4, 0xD5, 0x0C);
				int hr = CoCreateInstance(ref CLSID_CorMetaDataDispenser, IntPtr.Zero, 1, ref IID_IMetaDataDispenser, out mdDispObj);
				if (hr < 0)
					return null;

				object mdImportObj;
				var mdDisp = (IMetaDataDispenser)mdDispObj;
				Guid IID_IMetaDataImport = new Guid(0x7DAC8207, 0xD3AE, 0x4C75, 0x9B, 0x67, 0x92, 0x80, 0x1A, 0x49, 0x7D, 0x44);
				pinnedMd = new PinnedMetaData(mdStream);
				mdDisp.OpenScopeOnMemory(pinnedMd.Address, (uint)pinnedMd.Size, 0x10, ref IID_IMetaDataImport, out mdImportObj);
				Marshal.FinalReleaseComObject(mdDispObj);

				ISymUnmanagedReader symReader;
				var binder = (ISymUnmanagedBinder)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_CorSymBinder_SxS));
				stream = new ImageStreamIStream(pdbStream, null) { UserData = pinnedMd };
				hr = binder.GetReaderFromStream((IMetaDataImport)mdImportObj, stream, out symReader);
				Marshal.FinalReleaseComObject(mdImportObj);
				Marshal.FinalReleaseComObject(binder);
				if (hr >= 0) {
					error = false;
					return new SymbolReader(symReader);
				}
			}
			catch (IOException) {
			}
			catch (InvalidCastException) {
			}
			catch (COMException) {
			}
			finally {
				if (error) {
					if (stream != null)
						stream.Dispose();
					if (pinnedMd != null)
						pinnedMd.Dispose();
					if (mdStream != null)
						mdStream.Dispose();
					if (pdbStream != null)
						pdbStream.Dispose();
				}
			}
			return null;
		}

		static IImageStream CreateMetaDataStream(IMetaData metaData) {
			var peImage = metaData.PEImage;
			var mdDataDir = metaData.ImageCor20Header.MetaData;
			return peImage.CreateStream(mdDataDir.VirtualAddress, mdDataDir.Size);
		}
	}
}
