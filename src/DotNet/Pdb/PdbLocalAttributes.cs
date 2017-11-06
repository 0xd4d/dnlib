// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// Local attributes
	/// </summary>
	[Flags]
	public enum PdbLocalAttributes {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Local should be hidden in debugger variables windows. Not all compiler generated locals have this flag set.
		/// </summary>
		DebuggerHidden			= 0x00000001,
	}
}
