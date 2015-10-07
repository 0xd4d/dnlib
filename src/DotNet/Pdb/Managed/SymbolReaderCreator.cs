// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Security;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	/// <summary>
	/// Creates a <see cref="ISymbolReader"/> instance
	/// </summary>
	public static class SymbolReaderCreator {
		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="assemblyFileName">Path to assembly</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file.</returns>
		public static ISymbolReader CreateFromAssemblyFile(string assemblyFileName) {
			return Create(Path.ChangeExtension(assemblyFileName, "pdb"));
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="pdbFileName">Path to PDB file</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk.</returns>
		public static ISymbolReader Create(string pdbFileName) {
			return Create(OpenImageStream(pdbFileName));
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="pdbData">PDB file data</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c>.</returns>
		public static ISymbolReader Create(byte[] pdbData) {
			return Create(MemoryImageStream.Create(pdbData));
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="pdbStream">PDB file stream which is now owned by this method</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c>.</returns>
		public static ISymbolReader Create(IImageStream pdbStream) {
			if (pdbStream == null)
				return null;
			try {
				var pdbReader = new PdbReader();
				pdbReader.Read(pdbStream);
				return pdbReader;
			}
			catch (IOException) {
			}
			catch (UnauthorizedAccessException) {
			}
			catch (SecurityException) {
			}
			finally {
				if (pdbStream != null)
					pdbStream.Dispose();
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
	}
}
