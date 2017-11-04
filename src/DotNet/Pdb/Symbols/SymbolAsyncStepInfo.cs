// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Pdb.Symbols {
	/// <summary>
	/// Async step info
	/// </summary>
	public struct SymbolAsyncStepInfo {
		/// <summary>
		/// Yield offset
		/// </summary>
		public uint YieldOffset;

		/// <summary>
		/// Breakpoint offset
		/// </summary>
		public uint BreakpointOffset;

		/// <summary>
		/// Breakpoint method token
		/// </summary>
		public uint BreakpointMethod;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="yieldOffset">Yield offset</param>
		/// <param name="breakpointOffset">Breakpoint offset</param>
		/// <param name="breakpointMethod">Breakpoint method token</param>
		public SymbolAsyncStepInfo(uint yieldOffset, uint breakpointOffset, uint breakpointMethod) {
			YieldOffset = yieldOffset;
			BreakpointOffset = breakpointOffset;
			BreakpointMethod = breakpointMethod;
		}
	}
}
