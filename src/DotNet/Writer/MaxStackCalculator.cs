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
		ushort?[] stackHeights;
		uint maxStack;
		bool hasError;

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

		/// <summary>
		/// Gets the stack height for each instruction
		/// </summary>
		/// <param name="instructions">All instructions</param>
		/// <param name="exceptionHandlers">All exception handlers</param>
		/// <returns>The stack height for each instruction</returns>
		public static ushort?[] GetStackHeights(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			var helper = new MaxStackCalculator(instructions, exceptionHandlers);
			helper.ExploreAll();
			return helper.stackHeights;
		}

		internal static MaxStackCalculator Create() => new MaxStackCalculator();

		MaxStackCalculator(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) => Reset(instructions, exceptionHandlers);

		internal void Reset(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			this.instructions = instructions;
			this.exceptionHandlers = exceptionHandlers;
			stackHeights = new ushort?[instructions.Count];
			maxStack = 0;
			hasError = false;
		}

		internal bool Calculate(out uint maxStack) {
			ExploreAll();
			maxStack = this.maxStack;
			return !hasError;
		}

		void ExploreAll() {
			Explore(0, 0);

			foreach (var handler in exceptionHandlers) {
				if (handler.FilterStart is not null) {
					Explore(handler.FilterStart, 1);
				}
				if (handler.HandlerStart is not null) {
					bool pushed = handler.IsCatch || handler.IsFilter;
					Explore(handler.HandlerStart, (ushort)(pushed ? 1 : 0));
				}
			}
		}

		void Explore(Instruction instr, ushort stackHeight) => Explore(instructions.IndexOf(instr), stackHeight);

		void Explore(int index, ushort stackHeight) {
start:
			var previous = stackHeights[index];
			if (previous is not null) {
				if (previous != stackHeight) {
					hasError = true;
				}
				return; // already visited this instruction
			}
			stackHeights[index] = stackHeight;
			if (stackHeight > maxStack)
				maxStack = stackHeight;

			var instr = instructions[index];
			instr.CalculateStackUsage(out int pushes, out int pops);
			if (pops == -1) {
				stackHeight = 0;
			}
			else {
				if (stackHeight < pops) {
					hasError = true; // stack underflow
					return;
				}
				stackHeight -= (ushort)pops;
				stackHeight += (ushort)pushes;
			}
			switch (instr.OpCode.FlowControl) {
			case FlowControl.Break:
			case FlowControl.Call:
			case FlowControl.Meta:
			case FlowControl.Next: {
				if (instr.OpCode.Code == Code.Jmp) {
					return; // method terminates here
				}
				else {
					index++;
					goto start; // just continue to the next instruction
				}
			}
			case FlowControl.Return: {
				if (stackHeight > 1)
					hasError = true;
				return; // method terminates here
			}
			case FlowControl.Throw: {
				return; // method terminates here
			}
			case FlowControl.Branch: // unconditional branch
			{
				var target = (Instruction)instr.Operand;
				index = instructions.IndexOf(target);
				goto start; // tail recursion
			}
			case FlowControl.Cond_Branch: {
				if (instr.OpCode.Code == Code.Switch) {
					foreach (var target in (IList<Instruction>)instr.Operand) {
						Explore(target, stackHeight); // explore the branch target
					}
					index++;
					goto start; // explore the next instruction
				}
				else {
					var target = (Instruction)instr.Operand;
					Explore(target, stackHeight); // explore the branch target
					index++;
					goto start; // explore the next instruction
				}
			}
			default:
				hasError = true;
				return;
			}
		}
	}
}
