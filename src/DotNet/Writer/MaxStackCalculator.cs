// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Calculates max stack usage by using a simple pass over all instructions. This value
	/// can be placed in the fat method header's MaxStack field.
	/// </summary>
	public struct MaxStackCalculator {
		IList<Instruction> instructions;
		IList<ExceptionHandler> exceptionHandlers;
		readonly Dictionary<Instruction, int> stackHeights;
		bool hasError;
		int currentMaxStack;

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
			exceptionHandlers = null;
			stackHeights = new Dictionary<Instruction, int>();
			hasError = false;
			currentMaxStack = 0;
		}

		MaxStackCalculator(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			this.instructions = instructions;
			this.exceptionHandlers = exceptionHandlers;
			stackHeights = new Dictionary<Instruction, int>();
			hasError = false;
			currentMaxStack = 0;
		}

		internal void Reset(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			this.instructions = instructions;
			this.exceptionHandlers = exceptionHandlers;
			stackHeights.Clear();
			hasError = false;
			currentMaxStack = 0;
		}

		internal bool Calculate(out uint maxStack) {
			var exceptionHandlers = this.exceptionHandlers;
			var stackHeights = this.stackHeights;
			for (int i = 0; i < exceptionHandlers.Count; i++) {
				var eh = exceptionHandlers[i];
				if (eh is null)
					continue;
				Instruction instr;
				if (!((instr = eh.TryStart) is null))
					stackHeights[instr] = 0;
				if (!((instr = eh.FilterStart) is null)) {
					stackHeights[instr] = 1;
					currentMaxStack = 1;
				}
				if (!((instr = eh.HandlerStart) is null)) {
					bool pushed = eh.HandlerType == ExceptionHandlerType.Catch || eh.HandlerType == ExceptionHandlerType.Filter;
					if (pushed) {
						stackHeights[instr] = 1;
						currentMaxStack = 1;
					}
					else
						stackHeights[instr] = 0;
				}
			}

			int stack = 0;
			bool resetStack = false;
			var instructions = this.instructions;
			for (int i = 0; i < instructions.Count; i++) {
				var instr = instructions[i];
				if (instr is null)
					continue;

				if (resetStack) {
					stackHeights.TryGetValue(instr, out stack);
					resetStack = false;
				}
				stack = WriteStack(instr, stack);
				var opCode = instr.OpCode;
				var code = opCode.Code;
				if (code == Code.Jmp) {
					if (stack != 0)
						hasError = true;
				}
				else {
					instr.CalculateStackUsage(out int pushes, out int pops);
					if (pops == -1)
						stack = 0;
					else {
						stack -= pops;
						if (stack < 0) {
							hasError = true;
							stack = 0;
						}
						stack += pushes;
					}
				}
				if (stack < 0) {
					hasError = true;
					stack = 0;
				}

				switch (opCode.FlowControl) {
				case FlowControl.Branch:
					WriteStack(instr.Operand as Instruction, stack);
					resetStack = true;
					break;

				case FlowControl.Call:
					if (code == Code.Jmp)
						resetStack = true;
					break;

				case FlowControl.Cond_Branch:
					if (code == Code.Switch) {
						if (instr.Operand is IList<Instruction> targets) {
							for (int j = 0; j < targets.Count; j++)
								WriteStack(targets[j], stack);
						}
					}
					else
						WriteStack(instr.Operand as Instruction, stack);
					break;

				case FlowControl.Return:
				case FlowControl.Throw:
					resetStack = true;
					break;
				}
			}

			maxStack = (uint)currentMaxStack;
			return !hasError;
		}

		int WriteStack(Instruction instr, int stack) {
			if (instr is null) {
				hasError = true;
				return stack;
			}
			var stackHeights = this.stackHeights;
			if (stackHeights.TryGetValue(instr, out int stack2)) {
				if (stack != stack2)
					hasError = true;
				return stack2;
			}
			stackHeights[instr] = stack;
			if (stack > currentMaxStack)
				currentMaxStack = stack;
			return stack;
		}
	}
}
