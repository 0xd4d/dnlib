// dnlib: See LICENSE.txt for more info

using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb {
	interface ISymbolMethod2 : ISymbolMethod {
		bool IsAsyncMethod { get; }
		uint KickoffMethod { get; }
		uint? CatchHandlerILOffset { get; }
		RawAsyncStepInfo[] GetAsyncStepInfos();
	}

	struct RawAsyncStepInfo {
		public uint YieldOffset;
		public uint BreakpointOffset;
		public uint BreakpointMethod;
		public RawAsyncStepInfo(uint yieldOffset, uint breakpointOffset, uint breakpointMethod) {
			YieldOffset = yieldOffset;
			BreakpointOffset = breakpointOffset;
			BreakpointMethod = breakpointMethod;
		}
	}
}
