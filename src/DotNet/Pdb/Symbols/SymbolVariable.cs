// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Pdb.Symbols {
	/// <summary>
	/// A variable
	/// </summary>
	public abstract class SymbolVariable {
		/// <summary>
		/// Gets the name
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the attributes
		/// </summary>
		public abstract PdbLocalAttributes Attributes { get; }

		/// <summary>
		/// Gets the index of the variable
		/// </summary>
		public abstract int Index { get; }

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public abstract PdbCustomDebugInfo[] CustomDebugInfos { get; }
	}
}
