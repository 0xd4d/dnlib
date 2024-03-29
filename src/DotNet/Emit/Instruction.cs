// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.DotNet.Pdb;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// A CIL instruction (opcode + operand)
	/// </summary>
	public sealed class Instruction {
		/// <summary>
		/// The opcode
		/// </summary>
		public OpCode OpCode;

		/// <summary>
		/// The opcode operand
		/// </summary>
		public object Operand;

		/// <summary>
		/// Offset of the instruction in the method body
		/// </summary>
		public uint Offset;

		/// <summary>
		/// PDB sequence point or <c>null</c> if none
		/// </summary>
		public SequencePoint SequencePoint;

		/// <summary>
		/// Default constructor
		/// </summary>
		public Instruction() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opCode">Opcode</param>
		public Instruction(OpCode opCode) => OpCode = opCode;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opCode">Opcode</param>
		/// <param name="operand">The operand</param>
		public Instruction(OpCode opCode, object operand) {
			OpCode = opCode;
			Operand = operand;
		}

		/// <summary>
		/// Creates a new instruction with no operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode) {
			if (opCode.OperandType != OperandType.InlineNone)
				throw new ArgumentException("Must be a no-operand opcode", nameof(opCode));
			return new Instruction(opCode);
		}

		/// <summary>
		/// Creates a new instruction with a <see cref="byte"/> operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="value">The value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, byte value) {
			if (opCode.Code != Code.Unaligned)
				throw new ArgumentException("Opcode does not have a byte operand", nameof(opCode));
			return new Instruction(opCode, value);
		}

		/// <summary>
		/// Creates a new instruction with a <see cref="sbyte"/> operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="value">The value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, sbyte value) {
			if (opCode.Code != Code.Ldc_I4_S)
				throw new ArgumentException("Opcode does not have a sbyte operand", nameof(opCode));
			return new Instruction(opCode, value);
		}

		/// <summary>
		/// Creates a new instruction with an <see cref="int"/> operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="value">The value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, int value) {
			if (opCode.OperandType != OperandType.InlineI)
				throw new ArgumentException("Opcode does not have an int32 operand", nameof(opCode));
			return new Instruction(opCode, value);
		}

		/// <summary>
		/// Creates a new instruction with a <see cref="long"/> operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="value">The value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, long value) {
			if (opCode.OperandType != OperandType.InlineI8)
				throw new ArgumentException("Opcode does not have an int64 operand", nameof(opCode));
			return new Instruction(opCode, value);
		}

		/// <summary>
		/// Creates a new instruction with a <see cref="float"/> operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="value">The value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, float value) {
			if (opCode.OperandType != OperandType.ShortInlineR)
				throw new ArgumentException("Opcode does not have a real4 operand", nameof(opCode));
			return new Instruction(opCode, value);
		}

		/// <summary>
		/// Creates a new instruction with a <see cref="double"/> operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="value">The value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, double value) {
			if (opCode.OperandType != OperandType.InlineR)
				throw new ArgumentException("Opcode does not have a real8 operand", nameof(opCode));
			return new Instruction(opCode, value);
		}

		/// <summary>
		/// Creates a new instruction with a string operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="s">The string</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, string s) {
			if (opCode.OperandType != OperandType.InlineString)
				throw new ArgumentException("Opcode does not have a string operand", nameof(opCode));
			return new Instruction(opCode, s);
		}

		/// <summary>
		/// Creates a new instruction with an instruction target operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="target">Target instruction</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, Instruction target) {
			if (opCode.OperandType != OperandType.ShortInlineBrTarget && opCode.OperandType != OperandType.InlineBrTarget)
				throw new ArgumentException("Opcode does not have an instruction operand", nameof(opCode));
			return new Instruction(opCode, target);
		}

		/// <summary>
		/// Creates a new instruction with an instruction target list operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="targets">The targets</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, IList<Instruction> targets) {
			if (opCode.OperandType != OperandType.InlineSwitch)
				throw new ArgumentException("Opcode does not have a targets array operand", nameof(opCode));
			return new Instruction(opCode, targets);
		}

		/// <summary>
		/// Creates a new instruction with a type operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="type">The type</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, ITypeDefOrRef type) {
			if (opCode.OperandType != OperandType.InlineType && opCode.OperandType != OperandType.InlineTok)
				throw new ArgumentException("Opcode does not have a type operand", nameof(opCode));
			return new Instruction(opCode, type);
		}

		/// <summary>
		/// Creates a new instruction with a type operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="type">The type</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, CorLibTypeSig type) => Create(opCode, type.TypeDefOrRef);

		/// <summary>
		/// Creates a new instruction with a method/field operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="mr">The method/field</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, MemberRef mr) {
			if (opCode.OperandType != OperandType.InlineField && opCode.OperandType != OperandType.InlineMethod && opCode.OperandType != OperandType.InlineTok)
				throw new ArgumentException("Opcode does not have a field operand", nameof(opCode));
			return new Instruction(opCode, mr);
		}

		/// <summary>
		/// Creates a new instruction with a field operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="field">The field</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, IField field) {
			if (opCode.OperandType != OperandType.InlineField && opCode.OperandType != OperandType.InlineTok)
				throw new ArgumentException("Opcode does not have a field operand", nameof(opCode));
			return new Instruction(opCode, field);
		}

		/// <summary>
		/// Creates a new instruction with a method operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="method">The method</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, IMethod method) {
			if (opCode.OperandType != OperandType.InlineMethod && opCode.OperandType != OperandType.InlineTok)
				throw new ArgumentException("Opcode does not have a method operand", nameof(opCode));
			return new Instruction(opCode, method);
		}

		/// <summary>
		/// Creates a new instruction with a token operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="token">The token</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, ITokenOperand token) {
			if (opCode.OperandType != OperandType.InlineTok)
				throw new ArgumentException("Opcode does not have a token operand", nameof(opCode));
			return new Instruction(opCode, token);
		}

		/// <summary>
		/// Creates a new instruction with a method signature operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="methodSig">The method signature</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, MethodSig methodSig) {
			if (opCode.OperandType != OperandType.InlineSig)
				throw new ArgumentException("Opcode does not have a method sig operand", nameof(opCode));
			return new Instruction(opCode, methodSig);
		}

		/// <summary>
		/// Creates a new instruction with a method parameter operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="parameter">The method parameter</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, Parameter parameter) {
			if (opCode.OperandType != OperandType.ShortInlineVar && opCode.OperandType != OperandType.InlineVar)
				throw new ArgumentException("Opcode does not have a method parameter operand", nameof(opCode));
			return new Instruction(opCode, parameter);
		}

		/// <summary>
		/// Creates a new instruction with a method local operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="local">The method local</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, Local local) {
			if (opCode.OperandType != OperandType.ShortInlineVar && opCode.OperandType != OperandType.InlineVar)
				throw new ArgumentException("Opcode does not have a method local operand", nameof(opCode));
			return new Instruction(opCode, local);
		}

		/// <summary>
		/// Creates a <c>ldci4</c> instruction
		/// </summary>
		/// <param name="value">Operand value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction CreateLdcI4(int value) {
			switch (value) {
			case -1:return OpCodes.Ldc_I4_M1.ToInstruction();
			case 0: return OpCodes.Ldc_I4_0.ToInstruction();
			case 1: return OpCodes.Ldc_I4_1.ToInstruction();
			case 2: return OpCodes.Ldc_I4_2.ToInstruction();
			case 3: return OpCodes.Ldc_I4_3.ToInstruction();
			case 4: return OpCodes.Ldc_I4_4.ToInstruction();
			case 5: return OpCodes.Ldc_I4_5.ToInstruction();
			case 6: return OpCodes.Ldc_I4_6.ToInstruction();
			case 7: return OpCodes.Ldc_I4_7.ToInstruction();
			case 8: return OpCodes.Ldc_I4_8.ToInstruction();
			}
			if (sbyte.MinValue <= value && value <= sbyte.MaxValue)
				return new Instruction(OpCodes.Ldc_I4_S, (sbyte)value);
			return new Instruction(OpCodes.Ldc_I4, value);
		}

		/// <summary>
		/// Gets the size in bytes of the instruction
		/// </summary>
		/// <returns></returns>
		public int GetSize() {
			var opCode = OpCode;
			switch (opCode.OperandType) {
			case OperandType.InlineBrTarget:
			case OperandType.InlineField:
			case OperandType.InlineI:
			case OperandType.InlineMethod:
			case OperandType.InlineSig:
			case OperandType.InlineString:
			case OperandType.InlineTok:
			case OperandType.InlineType:
			case OperandType.ShortInlineR:
				return opCode.Size + 4;

			case OperandType.InlineI8:
			case OperandType.InlineR:
				return opCode.Size + 8;

			case OperandType.InlineNone:
			case OperandType.InlinePhi:
			default:
				return opCode.Size;

			case OperandType.InlineSwitch:
				var targets = Operand as IList<Instruction>;
				return opCode.Size + 4 + (targets is null ? 0 : targets.Count * 4);

			case OperandType.InlineVar:
				return opCode.Size + 2;

			case OperandType.ShortInlineBrTarget:
			case OperandType.ShortInlineI:
			case OperandType.ShortInlineVar:
				return opCode.Size + 1;
			}
		}

		static bool IsSystemVoid(TypeSig type) => type.RemovePinnedAndModifiers().GetElementType() == ElementType.Void;

		/// <summary>
		/// Updates <paramref name="stack"/> with the new stack size
		/// </summary>
		/// <param name="stack">Current stack size</param>
		public void UpdateStack(ref int stack) => UpdateStack(ref stack, false);

		/// <summary>
		/// Updates <paramref name="stack"/> with the new stack size
		/// </summary>
		/// <param name="stack">Current stack size</param>
		/// <param name="methodHasReturnValue"><c>true</c> if the method has a return value,
		/// <c>false</c> otherwise</param>
		public void UpdateStack(ref int stack, bool methodHasReturnValue) {
			CalculateStackUsage(methodHasReturnValue, out int pushes, out int pops);
			if (pops == -1)
				stack = 0;
			else
				stack += pushes - pops;
		}

		/// <summary>
		/// Calculates stack usage
		/// </summary>
		/// <param name="pushes">Updated with number of stack pushes</param>
		/// <param name="pops">Updated with number of stack pops or <c>-1</c> if the stack should
		/// be cleared.</param>
		public void CalculateStackUsage(out int pushes, out int pops) => CalculateStackUsage(false, out pushes, out pops);

		/// <summary>
		/// Calculates stack usage
		/// </summary>
		/// <param name="methodHasReturnValue"><c>true</c> if method has a return value</param>
		/// <param name="pushes">Updated with number of stack pushes</param>
		/// <param name="pops">Updated with number of stack pops or <c>-1</c> if the stack should
		/// be cleared.</param>
		public void CalculateStackUsage(bool methodHasReturnValue, out int pushes, out int pops) {
			var opCode = OpCode;
			if (opCode.FlowControl == FlowControl.Call)
				CalculateStackUsageCall(opCode.Code, out pushes, out pops);
			else
				CalculateStackUsageNonCall(opCode, methodHasReturnValue, out pushes, out pops);
		}

		void CalculateStackUsageCall(Code code, out int pushes, out int pops) {
			pushes = 0;
			pops = 0;

			// It doesn't push or pop anything. The stack should be empty when JMP is executed.
			if (code == Code.Jmp)
				return;

			MethodSig sig;
			var op = Operand;
			if (op is IMethod method)
				sig = method.MethodSig;
			else
				sig = op as MethodSig;	// calli instruction
			if (sig is null)
				return;
			bool implicitThis = sig.ImplicitThis;
			if (!IsSystemVoid(sig.RetType) || (code == Code.Newobj && sig.HasThis))
				pushes++;

			pops += sig.Params.Count;
			var paramsAfterSentinel = sig.ParamsAfterSentinel;
			if (paramsAfterSentinel is not null)
				pops += paramsAfterSentinel.Count;
			if (implicitThis && code != Code.Newobj)
				pops++;
			if (code == Code.Calli)
				pops++;
		}

		void CalculateStackUsageNonCall(OpCode opCode, bool hasReturnValue, out int pushes, out int pops) {
			switch (opCode.StackBehaviourPush) {
			case StackBehaviour.Push0:
				pushes = 0;
				break;

			case StackBehaviour.Push1:
			case StackBehaviour.Pushi:
			case StackBehaviour.Pushi8:
			case StackBehaviour.Pushr4:
			case StackBehaviour.Pushr8:
			case StackBehaviour.Pushref:
				pushes = 1;
				break;

			case StackBehaviour.Push1_push1:
				pushes = 2;
				break;

			case StackBehaviour.Varpush:	// only call, calli, callvirt which are handled elsewhere
			default:
				pushes = 0;
				break;
			}

			switch (opCode.StackBehaviourPop) {
			case StackBehaviour.Pop0:
				pops = 0;
				break;

			case StackBehaviour.Pop1:
			case StackBehaviour.Popi:
			case StackBehaviour.Popref:
				pops = 1;
				break;

			case StackBehaviour.Pop1_pop1:
			case StackBehaviour.Popi_pop1:
			case StackBehaviour.Popi_popi:
			case StackBehaviour.Popi_popi8:
			case StackBehaviour.Popi_popr4:
			case StackBehaviour.Popi_popr8:
			case StackBehaviour.Popref_pop1:
			case StackBehaviour.Popref_popi:
				pops = 2;
				break;

			case StackBehaviour.Popi_popi_popi:
			case StackBehaviour.Popref_popi_popi:
			case StackBehaviour.Popref_popi_popi8:
			case StackBehaviour.Popref_popi_popr4:
			case StackBehaviour.Popref_popi_popr8:
			case StackBehaviour.Popref_popi_popref:
			case StackBehaviour.Popref_popi_pop1:
				pops = 3;
				break;

			case StackBehaviour.PopAll:
				pops = -1;
				break;

			case StackBehaviour.Varpop:	// call, calli, callvirt, newobj (all handled elsewhere), and ret
				if (hasReturnValue)
					pops = 1;
				else
					pops = 0;
				break;

			default:
				pops = 0;
				break;
			}
		}

		/// <summary>
		/// Checks whether it's one of the <c>leave</c> instructions
		/// </summary>
		public bool IsLeave() => OpCode == OpCodes.Leave || OpCode == OpCodes.Leave_S;

		/// <summary>
		/// Checks whether it's one of the <c>br</c> instructions
		/// </summary>
		public bool IsBr() => OpCode == OpCodes.Br || OpCode == OpCodes.Br_S;

		/// <summary>
		/// Checks whether it's one of the <c>brfalse</c> instructions
		/// </summary>
		public bool IsBrfalse() => OpCode == OpCodes.Brfalse || OpCode == OpCodes.Brfalse_S;

		/// <summary>
		/// Checks whether it's one of the <c>brtrue</c> instructions
		/// </summary>
		public bool IsBrtrue() => OpCode == OpCodes.Brtrue || OpCode == OpCodes.Brtrue_S;

		/// <summary>
		/// Checks whether it's one of the conditional branch instructions (bcc, brtrue, brfalse)
		/// </summary>
		public bool IsConditionalBranch() {
			switch (OpCode.Code) {
			case Code.Bge:
			case Code.Bge_S:
			case Code.Bge_Un:
			case Code.Bge_Un_S:
			case Code.Blt:
			case Code.Blt_S:
			case Code.Blt_Un:
			case Code.Blt_Un_S:
			case Code.Bgt:
			case Code.Bgt_S:
			case Code.Bgt_Un:
			case Code.Bgt_Un_S:
			case Code.Ble:
			case Code.Ble_S:
			case Code.Ble_Un:
			case Code.Ble_Un_S:
			case Code.Brfalse:
			case Code.Brfalse_S:
			case Code.Brtrue:
			case Code.Brtrue_S:
			case Code.Beq:
			case Code.Beq_S:
			case Code.Bne_Un:
			case Code.Bne_Un_S:
				return true;

			default:
				return false;
			}
		}

		/// <summary>
		/// Checks whether this is one of the <c>ldc.i4</c> instructions
		/// </summary>
		public bool IsLdcI4() {
			switch (OpCode.Code) {
			case Code.Ldc_I4_M1:
			case Code.Ldc_I4_0:
			case Code.Ldc_I4_1:
			case Code.Ldc_I4_2:
			case Code.Ldc_I4_3:
			case Code.Ldc_I4_4:
			case Code.Ldc_I4_5:
			case Code.Ldc_I4_6:
			case Code.Ldc_I4_7:
			case Code.Ldc_I4_8:
			case Code.Ldc_I4_S:
			case Code.Ldc_I4:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Returns a <c>ldc.i4</c> instruction's operand
		/// </summary>
		/// <returns>The integer value</returns>
		/// <exception cref="InvalidOperationException"><see cref="OpCode"/> isn't one of the
		/// <c>ldc.i4</c> opcodes</exception>
		public int GetLdcI4Value() =>
			OpCode.Code switch {
				Code.Ldc_I4_M1 => -1,
				Code.Ldc_I4_0 => 0,
				Code.Ldc_I4_1 => 1,
				Code.Ldc_I4_2 => 2,
				Code.Ldc_I4_3 => 3,
				Code.Ldc_I4_4 => 4,
				Code.Ldc_I4_5 => 5,
				Code.Ldc_I4_6 => 6,
				Code.Ldc_I4_7 => 7,
				Code.Ldc_I4_8 => 8,
				Code.Ldc_I4_S => (sbyte)Operand,
				Code.Ldc_I4 => (int)Operand,
				_ => throw new InvalidOperationException($"Not a ldc.i4 instruction: {this}"),
			};

		/// <summary>
		/// Checks whether it's one of the <c>ldarg</c> instructions, but does <c>not</c> check
		/// whether it's one of the <c>ldarga</c> instructions.
		/// </summary>
		public bool IsLdarg() {
			switch (OpCode.Code) {
			case Code.Ldarg:
			case Code.Ldarg_S:
			case Code.Ldarg_0:
			case Code.Ldarg_1:
			case Code.Ldarg_2:
			case Code.Ldarg_3:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Checks whether it's one of the <c>ldloc</c> instructions, but does <c>not</c> check
		/// whether it's one of the <c>ldloca</c> instructions.
		/// </summary>
		public bool IsLdloc() {
			switch (OpCode.Code) {
			case Code.Ldloc:
			case Code.Ldloc_0:
			case Code.Ldloc_1:
			case Code.Ldloc_2:
			case Code.Ldloc_3:
			case Code.Ldloc_S:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Checks whether it's one of the <c>starg</c> instructions
		/// </summary>
		public bool IsStarg() {
			switch (OpCode.Code) {
			case Code.Starg:
			case Code.Starg_S:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Checks whether it's one of the <c>stloc</c> instructions
		/// </summary>
		public bool IsStloc() {
			switch (OpCode.Code) {
			case Code.Stloc:
			case Code.Stloc_0:
			case Code.Stloc_1:
			case Code.Stloc_2:
			case Code.Stloc_3:
			case Code.Stloc_S:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Returns the local if it's a <c>ldloc</c>, <c>stloc</c> or <c>ldloca</c> instruction
		/// </summary>
		/// <param name="locals">The locals</param>
		/// <returns>The local or <c>null</c> if it's not a <c>ldloc</c>, <c>stloc</c> or <c>ldloca</c>
		/// instruction or if the local doesn't exist.</returns>
		public Local GetLocal(IList<Local> locals) {
			int index;
			var code = OpCode.Code;
			switch (code) {
			case Code.Ldloc:
			case Code.Ldloc_S:
			case Code.Stloc:
			case Code.Stloc_S:
			case Code.Ldloca:
			case Code.Ldloca_S:
				return Operand as Local;

			case Code.Ldloc_0:
			case Code.Ldloc_1:
			case Code.Ldloc_2:
			case Code.Ldloc_3:
				index = code - Code.Ldloc_0;
				break;

			case Code.Stloc_0:
			case Code.Stloc_1:
			case Code.Stloc_2:
			case Code.Stloc_3:
				index = code - Code.Stloc_0;
				break;

			default:
				return null;
			}

			if ((uint)index < (uint)locals.Count)
				return locals[index];
			return null;
		}

		/// <summary>
		/// Gets the index of the instruction's parameter operand or <c>-1</c> if the parameter
		/// is missing or if it's not an instruction with a parameter operand.
		/// </summary>
		public int GetParameterIndex() {
			switch (OpCode.Code) {
			case Code.Ldarg_0: return 0;
			case Code.Ldarg_1: return 1;
			case Code.Ldarg_2: return 2;
			case Code.Ldarg_3: return 3;

			case Code.Starg:
			case Code.Starg_S:
			case Code.Ldarga:
			case Code.Ldarga_S:
			case Code.Ldarg:
			case Code.Ldarg_S:
				var parameter = Operand as Parameter;
				if (parameter is not null)
					return parameter.Index;
				break;
			}

			return -1;
		}

		/// <summary>
		/// Returns a method parameter
		/// </summary>
		/// <param name="parameters">All parameters</param>
		/// <returns>A parameter or <c>null</c> if it doesn't exist</returns>
		public Parameter GetParameter(IList<Parameter> parameters) {
			int i = GetParameterIndex();
			if ((uint)i < (uint)parameters.Count)
				return parameters[i];
			return null;
		}

		/// <summary>
		/// Returns an argument type
		/// </summary>
		/// <param name="methodSig">Method signature</param>
		/// <param name="declaringType">Declaring type (only needed if it's an instance method)</param>
		/// <returns>The type or <c>null</c> if it doesn't exist</returns>
		public TypeSig GetArgumentType(MethodSig methodSig, ITypeDefOrRef declaringType) {
			if (methodSig is null)
				return null;
			int index = GetParameterIndex();
			if (index == 0 && methodSig.ImplicitThis) {
				if (declaringType is null)
					return null;
				TypeSig declSig;
				bool isValueType;
				if (declaringType is TypeSpec spec) {
					declSig = spec.TypeSig;
					isValueType = declSig.IsValueType;
				}
				else {
					// Consistent with ParameterList.UpdateThisParameterType
					var td = declaringType.ResolveTypeDef();
					if (td is null)
						return declaringType.ToTypeSig();
					isValueType = td.IsValueType;
					ClassOrValueTypeSig cvSig = isValueType ? new ValueTypeSig(td) : new ClassSig(td);
					if (td.HasGenericParameters) {
						int gpCount = td.GenericParameters.Count;
						var genArgs = new List<TypeSig>(gpCount);
						for (int i = 0; i < gpCount; i++)
							genArgs.Add(new GenericVar(i, td));
						declSig = new GenericInstSig(cvSig, genArgs);
					}
					else
						declSig = cvSig;
				}
				return isValueType ? new ByRefSig(declSig) : declSig;
			}
			if (methodSig.ImplicitThis)
				index--;
			if ((uint)index < (uint)methodSig.Params.Count)
				return methodSig.Params[index];
			return null;
		}

		/// <summary>
		/// Clone this instance. The <see cref="Operand"/> and <see cref="SequencePoint"/> fields
		/// are shared by this instance and the created instance.
		/// </summary>
		public Instruction Clone() =>
			new Instruction {
				Offset = Offset,
				OpCode = OpCode,
				Operand = Operand,
				SequencePoint = SequencePoint,
			};

		/// <inheritdoc/>
		public override string ToString() => InstructionPrinter.ToString(this);
	}

	static partial class Extensions {
		/// <summary>
		/// Gets the opcode or <see cref="OpCodes.UNKNOWN1"/> if <paramref name="self"/> is <c>null</c>
		/// </summary>
		/// <param name="self">this</param>
		/// <returns></returns>
		public static OpCode GetOpCode(this Instruction self) => self?.OpCode ?? OpCodes.UNKNOWN1;

		/// <summary>
		/// Gets the operand or <c>null</c> if <paramref name="self"/> is <c>null</c>
		/// </summary>
		/// <param name="self">this</param>
		/// <returns></returns>
		public static object GetOperand(this Instruction self) => self?.Operand;

		/// <summary>
		/// Gets the offset or 0 if <paramref name="self"/> is <c>null</c>
		/// </summary>
		/// <param name="self">this</param>
		/// <returns></returns>
		public static uint GetOffset(this Instruction self) => self?.Offset ?? 0;

		/// <summary>
		/// Gets the sequence point or <c>null</c> if <paramref name="self"/> is <c>null</c>
		/// </summary>
		/// <param name="self">this</param>
		/// <returns></returns>
		public static dnlib.DotNet.Pdb.SequencePoint GetSequencePoint(this Instruction self) => self?.SequencePoint;
	}
}
