// dnlib: See LICENSE.txt for more info

using dnlib.DotNet.Emit;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// Async method info
	/// </summary>
	public sealed class PdbAsyncMethod {
		readonly ThreadSafe.IList<PdbAsyncStepInfo> asyncStepInfos;

		/// <summary>
		/// Gets/sets the starting method that initiates the async operation
		/// </summary>
		public MethodDef KickoffMethod { get; set; }

		/// <summary>
		/// Gets/sets the instruction for the compiler generated catch handler that wraps an async method.
		/// This can be null.
		/// </summary>
		public Instruction CatchHandlerInstruction { get; set; }

		/// <summary>
		/// Gets all step infos used by the debugger
		/// </summary>
		public ThreadSafe.IList<PdbAsyncStepInfo> StepInfos {
			get { return asyncStepInfos; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbAsyncMethod() {
			asyncStepInfos = ThreadSafeListCreator.Create<PdbAsyncStepInfo>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stepInfosCapacity">Default capacity for <see cref="StepInfos"/></param>
		public PdbAsyncMethod(int stepInfosCapacity) {
			asyncStepInfos = ThreadSafeListCreator.Create<PdbAsyncStepInfo>(stepInfosCapacity);
		}
	}

	/// <summary>
	/// Async step info used by debuggers
	/// </summary>
	public struct PdbAsyncStepInfo {
		/// <summary>
		/// The yield instruction
		/// </summary>
		public Instruction YieldInstruction;

		/// <summary>
		/// Resume method
		/// </summary>
		public MethodDef BreakpointMethod;

		/// <summary>
		/// Resume instruction (where the debugger puts a breakpoint)
		/// </summary>
		public Instruction BreakpointInstruction;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="yieldInstruction">The yield instruction</param>
		/// <param name="breakpointMethod">Resume method</param>
		/// <param name="breakpointInstruction">Resume instruction (where the debugger puts a breakpoint)</param>
		public PdbAsyncStepInfo(Instruction yieldInstruction, MethodDef breakpointMethod, Instruction breakpointInstruction) {
			YieldInstruction = yieldInstruction;
			BreakpointMethod = breakpointMethod;
			BreakpointInstruction = breakpointInstruction;
		}
	}
}
