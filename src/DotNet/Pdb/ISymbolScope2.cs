// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// The managed version of <c>ISymUnmanagedScope2</c>
	/// </summary>
	interface ISymbolScope2 : ISymbolScope {
		/// <summary>
		/// Gets all the constants
		/// </summary>
		/// <param name="module">Owner module if a signature must be read from the #Blob</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns></returns>
		PdbConstant[] GetConstants(ModuleDefMD module, GenericParamContext gpContext);
	}
}
