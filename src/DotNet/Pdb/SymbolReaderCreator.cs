// dnlib: See LICENSE.txt for more info

using System.Diagnostics.SymbolStore;
using dnlib.DotNet.MD;
using dnlib.IO;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// Creates a <see cref="ISymbolReader"/> instance
	/// </summary>
	public static class SymbolReaderCreator {
		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="assemblyFileName">Path to assembly</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk or if it's not possible to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(string assemblyFileName) {
			return Dss.SymbolReaderCreator.Create(assemblyFileName);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="metaData">.NET metadata</param>
		/// <param name="pdbFileName">Path to PDB file</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk or if it's not possible to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(IMetaData metaData, string pdbFileName) {
			return Dss.SymbolReaderCreator.Create(metaData, pdbFileName);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="metaData">.NET metadata</param>
		/// <param name="pdbData">PDB file data</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if it's not possible
		/// to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(IMetaData metaData, byte[] pdbData) {
			return Dss.SymbolReaderCreator.Create(metaData, pdbData);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="metaData">.NET metadata</param>
		/// <param name="pdbStream">PDB file stream which is now owned by us</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if it's not possible
		/// to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(IMetaData metaData, IImageStream pdbStream) {
			return Dss.SymbolReaderCreator.Create(metaData, pdbStream);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="mdStream">.NET metadata stream which is now owned by us</param>
		/// <param name="pdbFileName">Path to PDB file</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk or if it's not possible to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(IImageStream mdStream, string pdbFileName) {
			return Dss.SymbolReaderCreator.Create(mdStream, pdbFileName);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="mdStream">.NET metadata stream which is now owned by us</param>
		/// <param name="pdbData">PDB file data</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if it's not possible
		/// to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(IImageStream mdStream, byte[] pdbData) {
			return Dss.SymbolReaderCreator.Create(mdStream, pdbData);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="mdStream">.NET metadata stream which is now owned by us</param>
		/// <param name="pdbStream">PDB file stream which is now owned by us</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if it's not possible
		/// to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(IImageStream mdStream, IImageStream pdbStream) {
			return Dss.SymbolReaderCreator.Create(mdStream, pdbStream);
		}
	}
}
