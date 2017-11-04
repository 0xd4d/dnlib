// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	/// <summary>
	/// Creates a <see cref="SymbolReader"/> instance
	/// </summary>
	static class SymbolReaderCreator {
		/// <summary>
		/// Creates a new <see cref="SymbolReader"/> instance
		/// </summary>
		/// <param name="assemblyFileName">Path to assembly</param>
		/// <returns>A new <see cref="SymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file.</returns>
		public static SymbolReader CreateFromAssemblyFile(string assemblyFileName) {
			return Create(Path.ChangeExtension(assemblyFileName, "pdb"));
		}

		/// <summary>
		/// Creates a new <see cref="SymbolReader"/> instance
		/// </summary>
		/// <param name="pdbFileName">Path to PDB file</param>
		/// <returns>A new <see cref="SymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk.</returns>
		public static SymbolReader Create(string pdbFileName) {
			return Create(ImageStreamUtils.OpenImageStream(pdbFileName));
		}

		/// <summary>
		/// Creates a new <see cref="SymbolReader"/> instance
		/// </summary>
		/// <param name="pdbData">PDB file data</param>
		/// <returns>A new <see cref="SymbolReader"/> instance or <c>null</c>.</returns>
		public static SymbolReader Create(byte[] pdbData) {
			return Create(MemoryImageStream.Create(pdbData));
		}

		/// <summary>
		/// Creates a new <see cref="SymbolReader"/> instance
		/// </summary>
		/// <param name="pdbStream">PDB file stream which is now owned by this method</param>
		/// <returns>A new <see cref="SymbolReader"/> instance or <c>null</c>.</returns>
		public static SymbolReader Create(IImageStream pdbStream) {
			if (pdbStream == null)
				return null;
			try {
				var pdbReader = new PdbReader();
				pdbReader.Read(pdbStream);
				return pdbReader;
			}
			catch (PdbException) {
			}
			catch (IOException) {
			}
			finally {
				if (pdbStream != null)
					pdbStream.Dispose();
			}
			return null;
		}
	}
}
