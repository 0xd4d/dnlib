// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;

namespace dnlib.DotNet.Pdb.Symbols {
	/// <summary>
	/// A scope
	/// </summary>
	public abstract class SymbolScope {
		/// <summary>
		/// Gets the method
		/// </summary>
		public abstract SymbolMethod Method { get; }

		/// <summary>
		/// Gets the parent scope
		/// </summary>
		public abstract SymbolScope Parent { get; }

		/// <summary>
		/// Gets the start offset of the scope in the method
		/// </summary>
		public abstract int StartOffset { get; }

		/// <summary>
		/// Gets the end offset of the scope in the method
		/// </summary>
		public abstract int EndOffset { get; }

		/// <summary>
		/// Gets all child scopes
		/// </summary>
		public abstract IList<SymbolScope> Children { get; }

		/// <summary>
		/// Gets all locals defined in this scope
		/// </summary>
		public abstract IList<SymbolVariable> Locals { get; }

		/// <summary>
		/// Gets all namespaces in this scope
		/// </summary>
		public abstract IList<SymbolNamespace> Namespaces { get; }

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public abstract IList<PdbCustomDebugInfo> CustomDebugInfos { get; }

		/// <summary>
		/// Gets the import scope or null if none
		/// </summary>
		public abstract PdbImportScope ImportScope { get; }

		/// <summary>
		/// Gets all the constants
		/// </summary>
		/// <param name="module">Owner module if a signature must be read from the #Blob</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns></returns>
		public abstract IList<PdbConstant> GetConstants(ModuleDef module, GenericParamContext gpContext);
	}
}
