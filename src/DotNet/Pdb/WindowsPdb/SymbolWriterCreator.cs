// dnlib: See LICENSE.txt for more info

using System.IO;

namespace dnlib.DotNet.Pdb.WindowsPdb {
	/// <summary>
	/// Creates a <see cref="ISymbolWriter2"/>
	/// </summary>
	static class SymbolWriterCreator {
		/// <summary>
		/// Creates a new <see cref="ISymbolWriter2"/> instance
		/// </summary>
		/// <param name="pdbFileName">PDB file name</param>
		/// <returns>A new <see cref="ISymbolWriter2"/> instance</returns>
		public static ISymbolWriter2 Create(string pdbFileName) {
			return Dss.SymbolWriterCreator.Create(pdbFileName);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolWriter2"/> instance
		/// </summary>
		/// <param name="pdbStream">PDB output stream</param>
		/// <param name="pdbFileName">PDB file name</param>
		/// <returns>A new <see cref="ISymbolWriter2"/> instance</returns>
		public static ISymbolWriter2 Create(Stream pdbStream, string pdbFileName) {
			return Dss.SymbolWriterCreator.Create(pdbStream, pdbFileName);
		}
	}
}
