// dnlib: See LICENSE.txt for more info

using System;

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
		public abstract SymbolVariableAttributes Attributes { get; }

		/// <summary>
		/// Gets the index of the variable
		/// </summary>
		public abstract int Index { get; }
	}

	/// <summary>
	/// Variable flags
	/// </summary>
	[Flags]
	public enum SymbolVariableAttributes {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// It's a compiler generated variable
		/// </summary>
		CompilerGenerated		= 0x00000001,
	}
}
