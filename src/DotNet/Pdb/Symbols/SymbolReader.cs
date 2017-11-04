// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Collections.ObjectModel;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet.Pdb.Symbols {
	/// <summary>
	/// Reads symbols from a PDB file
	/// </summary>
	public abstract class SymbolReader {
		/// <summary>
		/// Gets the user entry point token or 0 if none
		/// </summary>
		public abstract int UserEntryPoint { get; }

		/// <summary>
		/// Gets all documents
		/// </summary>
		public abstract ReadOnlyCollection<SymbolDocument> Documents { get; }

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="method">Method token</param>
		/// <param name="version">Edit and continue version</param>
		/// <returns></returns>
		public abstract SymbolMethod GetMethod(int method, int version);

		/// <summary>
		/// Reads custom debug info
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="body">Method body</param>
		/// <param name="result">Updated with custom debug info</param>
		public abstract void GetCustomDebugInfo(MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result);
	}
}
