// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
using dnlib.Threading;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// A CIL opcode
	/// </summary>
	public sealed class OpCode {
		/// <summary>
		/// The opcode name
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// The opcode as a <see cref="Code"/> enum
		/// </summary>
		public readonly Code Code;

		/// <summary>
		/// Operand type
		/// </summary>
		public readonly OperandType OperandType;

		/// <summary>
		/// Flow control info
		/// </summary>
		public readonly FlowControl FlowControl;

		/// <summary>
		/// Opcode type
		/// </summary>
		public readonly OpCodeType OpCodeType;

		/// <summary>
		/// Push stack behavior
		/// </summary>
		public readonly StackBehaviour StackBehaviourPush;	// UK spelling for compatibility with Reflection

		/// <summary>
		/// Pop stack behavior
		/// </summary>
		public readonly StackBehaviour StackBehaviourPop;	// UK spelling for compatibility with Reflection

		/// <summary>
		/// Gets the value which is compatible with <see cref="System.Reflection.Emit.OpCode.Value"/>
		/// </summary>
		public short Value {
			get { return (short)Code; }
		}

		/// <summary>
		/// Gets the size of the opcode. It's either 1 or 2 bytes.
		/// </summary>
		public int Size {
			get { return Code < (Code)0x100 || Code == Code.UNKNOWN1 ? 1 : 2; }
		}

		internal OpCode(string name, Code code, OperandType operandType, FlowControl flowControl, OpCodeType opCodeType, StackBehaviour push, StackBehaviour pop) {
			this.Name = name;
			this.Code = code;
			this.OperandType = operandType;
			this.FlowControl = flowControl;
			this.OpCodeType = opCodeType;
			this.StackBehaviourPush = push;
			this.StackBehaviourPop = pop;
			if (((ushort)code >> 8) == 0)
				OpCodes.OneByteOpCodes[(byte)code] = this;
			else if (((ushort)code >> 8) == 0xFE)
				OpCodes.TwoByteOpCodes[(byte)code] = this;
		}

		/// <summary>
		/// Creates a new instruction with no operand
		/// </summary>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction() {
			return Instruction.Create(this);
		}

		/// <summary>
		/// Creates a new instruction with a <see cref="byte"/> operand
		/// </summary>
		/// <param name="value">The value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(byte value) {
			return Instruction.Create(this, value);
		}

		/// <summary>
		/// Creates a new instruction with a <see cref="sbyte"/> operand
		/// </summary>
		/// <param name="value">The value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(sbyte value) {
			return Instruction.Create(this, value);
		}

		/// <summary>
		/// Creates a new instruction with an <see cref="int"/> operand
		/// </summary>
		/// <param name="value">The value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(int value) {
			return Instruction.Create(this, value);
		}

		/// <summary>
		/// Creates a new instruction with a <see cref="long"/> operand
		/// </summary>
		/// <param name="value">The value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(long value) {
			return Instruction.Create(this, value);
		}

		/// <summary>
		/// Creates a new instruction with a <see cref="float"/> operand
		/// </summary>
		/// <param name="value">The value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(float value) {
			return Instruction.Create(this, value);
		}

		/// <summary>
		/// Creates a new instruction with a <see cref="double"/> operand
		/// </summary>
		/// <param name="value">The value</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(double value) {
			return Instruction.Create(this, value);
		}

		/// <summary>
		/// Creates a new instruction with a string operand
		/// </summary>
		/// <param name="s">The string</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(string s) {
			return Instruction.Create(this, s);
		}

		/// <summary>
		/// Creates a new instruction with an instruction target operand
		/// </summary>
		/// <param name="target">Target instruction</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(Instruction target) {
			return Instruction.Create(this, target);
		}

		/// <summary>
		/// Creates a new instruction with an instruction target list operand
		/// </summary>
		/// <param name="targets">The targets</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(IList<Instruction> targets) {
			return Instruction.Create(this, ThreadSafeListCreator.MakeThreadSafe(targets));
		}

		/// <summary>
		/// Creates a new instruction with a type operand
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(ITypeDefOrRef type) {
			return Instruction.Create(this, type);
		}

		/// <summary>
		/// Creates a new instruction with a type operand
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(CorLibTypeSig type) {
			return Instruction.Create(this, type.TypeDefOrRef);
		}

		/// <summary>
		/// Creates a new instruction with a method/field operand
		/// </summary>
		/// <param name="mr">The method/field</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(MemberRef mr) {
			return Instruction.Create(this, mr);
		}

		/// <summary>
		/// Creates a new instruction with a field operand
		/// </summary>
		/// <param name="field">The field</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(IField field) {
			return Instruction.Create(this, field);
		}

		/// <summary>
		/// Creates a new instruction with a method operand
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(IMethod method) {
			return Instruction.Create(this, method);
		}

		/// <summary>
		/// Creates a new instruction with a token operand
		/// </summary>
		/// <param name="token">The token</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(ITokenOperand token) {
			return Instruction.Create(this, token);
		}

		/// <summary>
		/// Creates a new instruction with a method signature operand
		/// </summary>
		/// <param name="methodSig">The method signature</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(MethodSig methodSig) {
			return Instruction.Create(this, methodSig);
		}

		/// <summary>
		/// Creates a new instruction with a method parameter operand
		/// </summary>
		/// <param name="parameter">The method parameter</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(Parameter parameter) {
			return Instruction.Create(this, parameter);
		}

		/// <summary>
		/// Creates a new instruction with a method local operand
		/// </summary>
		/// <param name="local">The method local</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public Instruction ToInstruction(Local local) {
			return Instruction.Create(this, local);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return Name;
		}
	}
}
