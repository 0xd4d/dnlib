// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
using dnlib.Threading;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Instruction utility methods
	/// </summary>
	public static class MethodUtils {
		/// <summary>
		/// Shorter instructions are converted to the longer form, eg. <c>Ldc_I4_1</c> is
		/// converted to <c>Ldc_I4</c> with a <c>1</c> as the operand.
		/// </summary>
		/// <param name="instructions">All instructions</param>
		/// <param name="locals">All locals</param>
		/// <param name="parameters">All method parameters, including the hidden 'this' parameter
		/// if it's an instance method. Use <see cref="MethodDef.Parameters"/>.</param>
		public static void SimplifyMacros(this IList<Instruction> instructions, IList<Local> locals, IList<Parameter> parameters) {
			foreach (var instr in instructions.GetSafeEnumerable()) {
				switch (instr.OpCode.Code) {
				case Code.Beq_S:
					instr.OpCode = OpCodes.Beq;
					break;

				case Code.Bge_S:
					instr.OpCode = OpCodes.Bge;
					break;

				case Code.Bge_Un_S:
					instr.OpCode = OpCodes.Bge_Un;
					break;

				case Code.Bgt_S:
					instr.OpCode = OpCodes.Bgt;
					break;

				case Code.Bgt_Un_S:
					instr.OpCode = OpCodes.Bgt_Un;
					break;

				case Code.Ble_S:
					instr.OpCode = OpCodes.Ble;
					break;

				case Code.Ble_Un_S:
					instr.OpCode = OpCodes.Ble_Un;
					break;

				case Code.Blt_S:
					instr.OpCode = OpCodes.Blt;
					break;

				case Code.Blt_Un_S:
					instr.OpCode = OpCodes.Blt_Un;
					break;

				case Code.Bne_Un_S:
					instr.OpCode = OpCodes.Bne_Un;
					break;

				case Code.Br_S:
					instr.OpCode = OpCodes.Br;
					break;

				case Code.Brfalse_S:
					instr.OpCode = OpCodes.Brfalse;
					break;

				case Code.Brtrue_S:
					instr.OpCode = OpCodes.Brtrue;
					break;

				case Code.Ldarg_0:
					instr.OpCode = OpCodes.Ldarg;
					instr.Operand = ReadList(parameters, 0);
					break;

				case Code.Ldarg_1:
					instr.OpCode = OpCodes.Ldarg;
					instr.Operand = ReadList(parameters, 1);
					break;

				case Code.Ldarg_2:
					instr.OpCode = OpCodes.Ldarg;
					instr.Operand = ReadList(parameters, 2);
					break;

				case Code.Ldarg_3:
					instr.OpCode = OpCodes.Ldarg;
					instr.Operand = ReadList(parameters, 3);
					break;

				case Code.Ldarg_S:
					instr.OpCode = OpCodes.Ldarg;
					break;

				case Code.Ldarga_S:
					instr.OpCode = OpCodes.Ldarga;
					break;

				case Code.Ldc_I4_0:
					instr.OpCode = OpCodes.Ldc_I4;
					instr.Operand = 0;
					break;

				case Code.Ldc_I4_1:
					instr.OpCode = OpCodes.Ldc_I4;
					instr.Operand = 1;
					break;

				case Code.Ldc_I4_2:
					instr.OpCode = OpCodes.Ldc_I4;
					instr.Operand = 2;
					break;

				case Code.Ldc_I4_3:
					instr.OpCode = OpCodes.Ldc_I4;
					instr.Operand = 3;
					break;

				case Code.Ldc_I4_4:
					instr.OpCode = OpCodes.Ldc_I4;
					instr.Operand = 4;
					break;

				case Code.Ldc_I4_5:
					instr.OpCode = OpCodes.Ldc_I4;
					instr.Operand = 5;
					break;

				case Code.Ldc_I4_6:
					instr.OpCode = OpCodes.Ldc_I4;
					instr.Operand = 6;
					break;

				case Code.Ldc_I4_7:
					instr.OpCode = OpCodes.Ldc_I4;
					instr.Operand = 7;
					break;

				case Code.Ldc_I4_8:
					instr.OpCode = OpCodes.Ldc_I4;
					instr.Operand = 8;
					break;

				case Code.Ldc_I4_M1:
					instr.OpCode = OpCodes.Ldc_I4;
					instr.Operand = -1;
					break;

				case Code.Ldc_I4_S:
					instr.OpCode = OpCodes.Ldc_I4;
					instr.Operand = (int)(sbyte)instr.Operand;
					break;

				case Code.Ldloc_0:
					instr.OpCode = OpCodes.Ldloc;
					instr.Operand = ReadList(locals, 0);
					break;

				case Code.Ldloc_1:
					instr.OpCode = OpCodes.Ldloc;
					instr.Operand = ReadList(locals, 1);
					break;

				case Code.Ldloc_2:
					instr.OpCode = OpCodes.Ldloc;
					instr.Operand = ReadList(locals, 2);
					break;

				case Code.Ldloc_3:
					instr.OpCode = OpCodes.Ldloc;
					instr.Operand = ReadList(locals, 3);
					break;

				case Code.Ldloc_S:
					instr.OpCode = OpCodes.Ldloc;
					break;

				case Code.Ldloca_S:
					instr.OpCode = OpCodes.Ldloca;
					break;

				case Code.Leave_S:
					instr.OpCode = OpCodes.Leave;
					break;

				case Code.Starg_S:
					instr.OpCode = OpCodes.Starg;
					break;

				case Code.Stloc_0:
					instr.OpCode = OpCodes.Stloc;
					instr.Operand = ReadList(locals, 0);
					break;

				case Code.Stloc_1:
					instr.OpCode = OpCodes.Stloc;
					instr.Operand = ReadList(locals, 1);
					break;

				case Code.Stloc_2:
					instr.OpCode = OpCodes.Stloc;
					instr.Operand = ReadList(locals, 2);
					break;

				case Code.Stloc_3:
					instr.OpCode = OpCodes.Stloc;
					instr.Operand = ReadList(locals, 3);
					break;

				case Code.Stloc_S:
					instr.OpCode = OpCodes.Stloc;
					break;
				}
			}
		}

		static T ReadList<T>(IList<T> list, int index) {
			if (list == null)
				return default(T);
			return list.Get(index, default(T));
		}

		/// <summary>
		/// Optimizes instructions by using the shorter form if possible. Eg. <c>Ldc_I4</c> <c>1</c>
		/// will be replaced with <c>Ldc_I4_1</c>.
		/// </summary>
		/// <param name="instructions">All instructions</param>
		public static void OptimizeMacros(this IList<Instruction> instructions) {
			foreach (var instr in instructions.GetSafeEnumerable()) {
				Parameter arg;
				Local local;
				switch (instr.OpCode.Code) {
				case Code.Ldarg:
				case Code.Ldarg_S:
					arg = instr.Operand as Parameter;
					if (arg == null)
						break;
					if (arg.Index == 0) {
						instr.OpCode = OpCodes.Ldarg_0;
						instr.Operand = null;
					}
					else if (arg.Index == 1) {
						instr.OpCode = OpCodes.Ldarg_1;
						instr.Operand = null;
					}
					else if (arg.Index == 2) {
						instr.OpCode = OpCodes.Ldarg_2;
						instr.Operand = null;
					}
					else if (arg.Index == 3) {
						instr.OpCode = OpCodes.Ldarg_3;
						instr.Operand = null;
					}
					else if (byte.MinValue <= arg.Index && arg.Index <= byte.MaxValue)
						instr.OpCode = OpCodes.Ldarg_S;
					break;

				case Code.Ldarga:
					arg = instr.Operand as Parameter;
					if (arg == null)
						break;
					if (byte.MinValue <= arg.Index && arg.Index <= byte.MaxValue)
						instr.OpCode = OpCodes.Ldarga_S;
					break;

				case Code.Ldc_I4:
				case Code.Ldc_I4_S:
					int i4;
					if (instr.Operand is int)
						i4 = (int)instr.Operand;
					else if (instr.Operand is sbyte)
						i4 = (sbyte)instr.Operand;
					else
						break;
					switch (i4) {
					case 0:
						instr.OpCode = OpCodes.Ldc_I4_0;
						instr.Operand = null;
						break;

					case 1:
						instr.OpCode = OpCodes.Ldc_I4_1;
						instr.Operand = null;
						break;

					case 2:
						instr.OpCode = OpCodes.Ldc_I4_2;
						instr.Operand = null;
						break;

					case 3:
						instr.OpCode = OpCodes.Ldc_I4_3;
						instr.Operand = null;
						break;

					case 4:
						instr.OpCode = OpCodes.Ldc_I4_4;
						instr.Operand = null;
						break;

					case 5:
						instr.OpCode = OpCodes.Ldc_I4_5;
						instr.Operand = null;
						break;

					case 6:
						instr.OpCode = OpCodes.Ldc_I4_6;
						instr.Operand = null;
						break;

					case 7:
						instr.OpCode = OpCodes.Ldc_I4_7;
						instr.Operand = null;
						break;

					case 8:
						instr.OpCode = OpCodes.Ldc_I4_8;
						instr.Operand = null;
						break;

					case -1:
						instr.OpCode = OpCodes.Ldc_I4_M1;
						instr.Operand = null;
						break;

					default:
						if (sbyte.MinValue <= i4 && i4 <= sbyte.MaxValue) {
							instr.OpCode = OpCodes.Ldc_I4_S;
							instr.Operand = (sbyte)i4;
						}
						break;
					}
					break;

				case Code.Ldloc:
				case Code.Ldloc_S:
					local = instr.Operand as Local;
					if (local == null)
						break;
					if (local.Index == 0) {
						instr.OpCode = OpCodes.Ldloc_0;
						instr.Operand = null;
					}
					else if (local.Index == 1) {
						instr.OpCode = OpCodes.Ldloc_1;
						instr.Operand = null;
					}
					else if (local.Index == 2) {
						instr.OpCode = OpCodes.Ldloc_2;
						instr.Operand = null;
					}
					else if (local.Index == 3) {
						instr.OpCode = OpCodes.Ldloc_3;
						instr.Operand = null;
					}
					else if (byte.MinValue <= local.Index && local.Index <= byte.MaxValue)
						instr.OpCode = OpCodes.Ldloc_S;
					break;

				case Code.Ldloca:
					local = instr.Operand as Local;
					if (local == null)
						break;
					if (byte.MinValue <= local.Index && local.Index <= byte.MaxValue)
						instr.OpCode = OpCodes.Ldloca_S;
					break;

				case Code.Starg:
					arg = instr.Operand as Parameter;
					if (arg == null)
						break;
					if (byte.MinValue <= arg.Index && arg.Index <= byte.MaxValue)
						instr.OpCode = OpCodes.Starg_S;
					break;

				case Code.Stloc:
				case Code.Stloc_S:
					local = instr.Operand as Local;
					if (local == null)
						break;
					if (local.Index == 0) {
						instr.OpCode = OpCodes.Stloc_0;
						instr.Operand = null;
					}
					else if (local.Index == 1) {
						instr.OpCode = OpCodes.Stloc_1;
						instr.Operand = null;
					}
					else if (local.Index == 2) {
						instr.OpCode = OpCodes.Stloc_2;
						instr.Operand = null;
					}
					else if (local.Index == 3) {
						instr.OpCode = OpCodes.Stloc_3;
						instr.Operand = null;
					}
					else if (byte.MinValue <= local.Index && local.Index <= byte.MaxValue)
						instr.OpCode = OpCodes.Stloc_S;
					break;
				}
			}

			OptimizeBranches(instructions);
		}

		/// <summary>
		/// Short branch instructions are converted to the long form, eg. <c>Beq_S</c> is
		/// converted to <c>Beq</c>.
		/// </summary>
		/// <param name="instructions">All instructions</param>
		public static void SimplifyBranches(this IList<Instruction> instructions) {
			foreach (var instr in instructions.GetSafeEnumerable()) {
				switch (instr.OpCode.Code) {
				case Code.Beq_S:	instr.OpCode = OpCodes.Beq; break;
				case Code.Bge_S:	instr.OpCode = OpCodes.Bge; break;
				case Code.Bgt_S:	instr.OpCode = OpCodes.Bgt; break;
				case Code.Ble_S:	instr.OpCode = OpCodes.Ble; break;
				case Code.Blt_S:	instr.OpCode = OpCodes.Blt; break;
				case Code.Bne_Un_S:	instr.OpCode = OpCodes.Bne_Un; break;
				case Code.Bge_Un_S:	instr.OpCode = OpCodes.Bge_Un; break;
				case Code.Bgt_Un_S:	instr.OpCode = OpCodes.Bgt_Un; break;
				case Code.Ble_Un_S:	instr.OpCode = OpCodes.Ble_Un; break;
				case Code.Blt_Un_S:	instr.OpCode = OpCodes.Blt_Un; break;
				case Code.Br_S:		instr.OpCode = OpCodes.Br; break;
				case Code.Brfalse_S:instr.OpCode = OpCodes.Brfalse; break;
				case Code.Brtrue_S:	instr.OpCode = OpCodes.Brtrue; break;
				case Code.Leave_S:	instr.OpCode = OpCodes.Leave; break;
				}
			}
		}

		/// <summary>
		/// Optimizes branches by using the smallest possible branch
		/// </summary>
		/// <param name="instructions">All instructions</param>
		public static void OptimizeBranches(this IList<Instruction> instructions) {
			while (true) {
				UpdateInstructionOffsets(instructions);

				bool modified = false;
				foreach (var instr in instructions.GetSafeEnumerable()) {
					OpCode shortOpCode;
					switch (instr.OpCode.Code) {
					case Code.Beq:		shortOpCode = OpCodes.Beq_S; break;
					case Code.Bge:		shortOpCode = OpCodes.Bge_S; break;
					case Code.Bge_Un:	shortOpCode = OpCodes.Bge_Un_S; break;
					case Code.Bgt:		shortOpCode = OpCodes.Bgt_S; break;
					case Code.Bgt_Un:	shortOpCode = OpCodes.Bgt_Un_S; break;
					case Code.Ble:		shortOpCode = OpCodes.Ble_S; break;
					case Code.Ble_Un:	shortOpCode = OpCodes.Ble_Un_S; break;
					case Code.Blt:		shortOpCode = OpCodes.Blt_S; break;
					case Code.Blt_Un:	shortOpCode = OpCodes.Blt_Un_S; break;
					case Code.Bne_Un:	shortOpCode = OpCodes.Bne_Un_S; break;
					case Code.Br:		shortOpCode = OpCodes.Br_S; break;
					case Code.Brfalse:	shortOpCode = OpCodes.Brfalse_S; break;
					case Code.Brtrue:	shortOpCode = OpCodes.Brtrue_S; break;
					case Code.Leave:	shortOpCode = OpCodes.Leave_S; break;
					default: continue;
					}
					var targetInstr = instr.Operand as Instruction;
					if (targetInstr == null)
						continue;

					int afterShortInstr;
					if (targetInstr.Offset >= instr.Offset) {
						// Target is >= this instruction so use the offset after
						// current instruction
						afterShortInstr = (int)instr.Offset + instr.GetSize();
					}
					else {
						// Target is < this instruction so use the offset after
						// the short instruction
						const int operandSize = 1;
						afterShortInstr = (int)instr.Offset + shortOpCode.Size + operandSize;
					}

					int displ = (int)targetInstr.Offset - afterShortInstr;
					if (sbyte.MinValue <= displ && displ <= sbyte.MaxValue) {
						instr.OpCode = shortOpCode;
						modified = true;
					}
				}
				if (!modified)
					break;
			}
		}

		/// <summary>
		/// Updates each instruction's offset
		/// </summary>
		/// <param name="instructions">All instructions</param>
		/// <returns>Total size in bytes of all instructions</returns>
		public static uint UpdateInstructionOffsets(this IList<Instruction> instructions) {
			uint offset = 0;
			foreach (var instr in instructions.GetSafeEnumerable()) {
				instr.Offset = offset;
				offset += (uint)instr.GetSize();
			}
			return offset;
		}
	}
}
