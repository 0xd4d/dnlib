// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Calculates max stack usage by traversing the instruction flow. This value
	/// can be placed in the fat method header's MaxStack field.
	/// </summary>
	public struct MaxStackCalculator {
		struct State {
			public int InstructionIndex;
			public int StackSize;
		}

		IList<Instruction> instructions;
		Stack<State> agenda;
		int?[] stackSizes;
		Dictionary<int, List<ExceptionHandler>> exHandlers;

		/// <summary>
		/// Gets max stack value
		/// </summary>
		/// <param name="instructions">All instructions</param>
		/// <param name="exceptionHandlers">All exception handlers</param>
		/// <returns>Max stack value</returns>
		public static uint GetMaxStack(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			new MaxStackCalculator(instructions, exceptionHandlers).Calculate(out uint maxStack);
			return maxStack;
		}

		/// <summary>
		/// Gets max stack value
		/// </summary>
		/// <param name="instructions">All instructions</param>
		/// <param name="exceptionHandlers">All exception handlers</param>
		/// <param name="maxStack">Updated with max stack value</param>
		/// <returns><c>true</c> if no errors were detected, <c>false</c> otherwise</returns>
		public static bool GetMaxStack(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers, out uint maxStack) =>
			new MaxStackCalculator(instructions, exceptionHandlers).Calculate(out maxStack);

		internal static MaxStackCalculator Create() => new MaxStackCalculator(true);

		MaxStackCalculator(bool dummy) {
			instructions = null;
			agenda = null;
			stackSizes = null;
			exHandlers = null;
		}

		MaxStackCalculator(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) =>
			Reset(instructions, exceptionHandlers);

		internal void Reset(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			this.instructions = instructions;

			if (instructions.Count == 0) {
				agenda = null;
				stackSizes = null;
				exHandlers = null;
				return;
			}

			agenda = new Stack<State>(instructions.Count);
			stackSizes = new int?[instructions.Count];

			if (exceptionHandlers.Count == 0) {
				exHandlers = null;
				return;
			}

			exHandlers = new Dictionary<int, List<ExceptionHandler>>(exceptionHandlers.Count);
			foreach (var handler in exceptionHandlers) {
				if (handler?.TryStart is null)
					continue;

				var idx = instructions.IndexOf(handler.TryStart);
				if (idx < 0)
					continue;
				if (!exHandlers.TryGetValue(idx, out var list))
					exHandlers.Add(idx, list = new List<ExceptionHandler>());

				list.Add(handler);
			}
		}

		internal bool Calculate(out uint maxStack) {
			maxStack = 0;
			if (instructions.Count == 0)
				return true;

			agenda.Push(new State { InstructionIndex = 0, StackSize = 0 });

			int currentMaxStack = 0;

			while (agenda.Count > 0) {
				var state = agenda.Pop();

				if (state.InstructionIndex == -1 || state.InstructionIndex >= instructions.Count) {
					// We went out of bounds of the method body or there is a branch to a non-existing instruction.
					return false;
				}

				int? previous = stackSizes[state.InstructionIndex];
				if (previous is not null) {
					if (previous != state.StackSize) {
						// Stack size mismatch.
						return false;
					}
				}
				else {
					stackSizes[state.InstructionIndex] = state.StackSize;

					if (!AddTargets(state))
						return false;
					if (!AddEHTargets(state))
						return false;
				}

				if (state.StackSize > currentMaxStack)
					currentMaxStack = state.StackSize;
			}

			maxStack = (uint)currentMaxStack;

			return true;
		}

		private bool AddTargets(State state) {
			var instr = instructions[state.InstructionIndex];
			if (instr is null)
				return false;
			instr.CalculateStackUsage(out int pushes, out int pops);

			int stack = pops == -1 ? 0 : state.StackSize - pops;
			if (stack < 0) {
				// Stack underflow.
				return false;
			}

			stack += pushes;

			if (instr.OpCode.Code == Code.Jmp) {
				// We don't add the next instruction since jmp doesn't continue execution after it.
				return stack == 0;
			}

			switch (instr.OpCode.FlowControl) {
			case FlowControl.Break:
			case FlowControl.Call:
			case FlowControl.Meta:
			case FlowControl.Next:
				AddNextInstruction(state, stack);
				break;
			case FlowControl.Branch:
				if (instr.Operand is not Instruction target)
					return false;
				AddInstruction(target, stack);
				break;
			case FlowControl.Cond_Branch:
				AddNextInstruction(state, stack);

				if (instr.Operand is Instruction condTarget)
					AddInstruction(condTarget, stack);
				else if (instr.Operand is IList<Instruction> targets) {
					for (int i = 0; i < targets.Count; i++) {
						var t = targets[i];
						if (t is null)
							return false;
						AddInstruction(t, stack);
					}
				}
				else
					return false;
				break;
			case FlowControl.Return:
				// We should check stack == 0 here, but MaxStackCalculator does not have enough information to know,
				// if the method returns a value or not, so we cannot do it.
				break;
			case FlowControl.Throw:
				// Throwing an exception clears the stack, so we cannot have too many values on the stack.
				break;
			default:
				// Unknown flow control.
				return false;
			}

			return true;
		}

		private bool AddEHTargets(State state) {
			if (exHandlers is null)
				return true;

			if (!exHandlers.TryGetValue(state.InstructionIndex, out var handlers))
				return true;

			if (state.StackSize != 0) {
				// Try block must be entered with an empty stack.
				return false;
			}

			for (int i = 0; i < handlers.Count; i++) {
				var handler = handlers[i];

				if (handler.HandlerStart is not null)
					AddInstruction(handler.HandlerStart, handler.IsCatch || handler.IsFilter ? 1 : 0);

				if (handler.FilterStart is not null)
					AddInstruction(handler.FilterStart, 1);
			}

			return true;
		}

		private void AddNextInstruction(State state, int stack) =>
			agenda.Push(new State { InstructionIndex = state.InstructionIndex + 1, StackSize = stack });

		private void AddInstruction(Instruction instr, int stack) =>
			agenda.Push(new State { InstructionIndex = instructions.IndexOf(instr), StackSize = stack });
	}
}
