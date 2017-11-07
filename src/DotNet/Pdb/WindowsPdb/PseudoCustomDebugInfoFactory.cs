// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.WindowsPdb {
	static class PseudoCustomDebugInfoFactory {
		public static PdbAsyncMethodCustomDebugInfo TryCreateAsyncMethod(ModuleDef module, MethodDef method, CilBody body, int asyncKickoffMethod, IList<SymbolAsyncStepInfo> asyncStepInfos, uint? asyncCatchHandlerILOffset) {
			var kickoffToken = new MDToken(asyncKickoffMethod);
			if (kickoffToken.Table != Table.Method)
				return null;
			var kickoffMethod = module.ResolveToken(kickoffToken) as MethodDef;

			var asyncMethod = new PdbAsyncMethodCustomDebugInfo(asyncStepInfos.Count);
			asyncMethod.KickoffMethod = kickoffMethod;

			if (asyncCatchHandlerILOffset != null) {
				asyncMethod.CatchHandlerInstruction = GetInstruction(body, asyncCatchHandlerILOffset.Value);
				Debug.Assert(asyncMethod.CatchHandlerInstruction != null);
			}

			foreach (var rawInfo in asyncStepInfos) {
				var yieldInstruction = GetInstruction(body, rawInfo.YieldOffset);
				Debug.Assert(yieldInstruction != null);
				if (yieldInstruction == null)
					continue;
				MethodDef breakpointMethod;
				Instruction breakpointInstruction;
				if (method.MDToken.Raw == rawInfo.BreakpointMethod) {
					breakpointMethod = method;
					breakpointInstruction = GetInstruction(body, rawInfo.BreakpointOffset);
				}
				else {
					var breakpointMethodToken = new MDToken(rawInfo.BreakpointMethod);
					Debug.Assert(breakpointMethodToken.Table == Table.Method);
					if (breakpointMethodToken.Table != Table.Method)
						continue;
					breakpointMethod = module.ResolveToken(breakpointMethodToken) as MethodDef;
					Debug.Assert(breakpointMethod != null);
					if (breakpointMethod == null)
						continue;
					breakpointInstruction = GetInstruction(breakpointMethod.Body, rawInfo.BreakpointOffset);
				}
				Debug.Assert(breakpointInstruction != null);
				if (breakpointInstruction == null)
					continue;

				asyncMethod.StepInfos.Add(new PdbAsyncStepInfo(yieldInstruction, breakpointMethod, breakpointInstruction));
			}

			return asyncMethod;
		}

		static Instruction GetInstruction(CilBody body, uint offset) {
			if (body == null)
				return null;
			var instructions = body.Instructions;
			int lo = 0, hi = instructions.Count - 1;
			while (lo <= hi && hi != -1) {
				int i = (lo + hi) / 2;
				var instr = instructions[i];
				if (instr.Offset == offset)
					return instr;
				if (offset < instr.Offset)
					hi = i - 1;
				else
					lo = i + 1;
			}
			return null;
		}
	}
}
