// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet {
	/// <summary>
	/// Interface to access a local or a parameter
	/// </summary>
	public interface IVariable {
		/// <summary>
		/// Gets the variable type
		/// </summary>
		TypeSig Type { get; }

		/// <summary>
		/// Gets the 0-based position
		/// </summary>
		int Index { get; }

		/// <summary>
		/// Gets/sets the variable name
		/// </summary>
		string Name { get; set; }
	}
}
