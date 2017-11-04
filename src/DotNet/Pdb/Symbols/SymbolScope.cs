// dnlib: See LICENSE.txt for more info

using System.Collections.ObjectModel;

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
		public abstract ReadOnlyCollection<SymbolScope> Children { get; }

		/// <summary>
		/// Gets all locals defined in this scope
		/// </summary>
		public abstract ReadOnlyCollection<SymbolVariable> Locals { get; }

		/// <summary>
		/// Gets all namespaces in this scope
		/// </summary>
		public abstract ReadOnlyCollection<SymbolNamespace> Namespaces { get; }

		/// <summary>
		/// Gets all the constants
		/// </summary>
		/// <param name="module">Owner module if a signature must be read from the #Blob</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns></returns>
		public abstract PdbConstant[] GetConstants(ModuleDefMD module, GenericParamContext gpContext);
	}
}
