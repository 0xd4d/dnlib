// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet {
	/// <summary>
	/// Module kind
	/// </summary>
	public enum ModuleKind {
		/// <summary>
		/// Console UI module
		/// </summary>
		Console,

		/// <summary>
		/// Windows GUI module
		/// </summary>
		Windows,

		/// <summary>
		/// DLL module
		/// </summary>
		Dll,

		/// <summary>
		/// Netmodule (it has no assembly manifest)
		/// </summary>
		NetModule,
	}
}
