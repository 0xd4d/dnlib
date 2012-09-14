using System;
using System.Collections.Generic;

namespace dot10.DotNet.Emit {
	/// <summary>
	/// A CIL instruction (opcode + operand)
	/// </summary>
	public class Instruction {
		/// <summary>
		/// The opcode
		/// </summary>
		public OpCode OpCode;

		/// <summary>
		/// The opcode operand
		/// </summary>
		public object Operand;

		/// <summary>
		/// Default constructor
		/// </summary>
		public Instruction() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opCode">Opcode</param>
		public Instruction(OpCode opCode) {
			this.OpCode = opCode;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opCode">Opcode</param>
		/// <param name="operand">The operand</param>
		public Instruction(OpCode opCode, object operand) {
			this.OpCode = opCode;
			this.Operand = operand;
		}

		/// <summary>
		/// Creates a new instruction with no operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode) {
			if (opCode.OperandType != OperandType.InlineNone)
				throw new ArgumentException("Must be a no-operand opcode", "opCode");
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
				throw new ArgumentException("Opcode does not have a byte operand", "opCode");
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
				throw new ArgumentException("Opcode does not have a sbyte operand", "opCode");
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
				throw new ArgumentException("Opcode does not have an int32 operand", "opCode");
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
				throw new ArgumentException("Opcode does not have an int64 operand", "opCode");
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
				throw new ArgumentException("Opcode does not have a real4 operand", "opCode");
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
				throw new ArgumentException("Opcode does not have a real8 operand", "opCode");
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
				throw new ArgumentException("Opcode does not have a string operand", "opCode");
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
				throw new ArgumentException("Opcode does not have an instruction operand", "opCode");
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
				throw new ArgumentException("Opcode does not have a targets array operand", "opCode");
			return new Instruction(opCode, targets);
		}

		/// <summary>
		/// Creates a new instruction with a type operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="type">The type</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, ITypeDefOrRef type) {
			if (opCode.OperandType != OperandType.InlineType)
				throw new ArgumentException("Opcode does not have a type operand", "opCode");
			return new Instruction(opCode, type);
		}

		/// <summary>
		/// Creates a new instruction with a field operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="field">The field</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, IField field) {
			if (opCode.OperandType != OperandType.InlineField)
				throw new ArgumentException("Opcode does not have a field operand", "opCode");
			return new Instruction(opCode, field);
		}

		/// <summary>
		/// Creates a new instruction with a method operand
		/// </summary>
		/// <param name="opCode">The opcode</param>
		/// <param name="method">The method</param>
		/// <returns>A new <see cref="Instruction"/> instance</returns>
		public static Instruction Create(OpCode opCode, IMethod method) {
			if (opCode.OperandType != OperandType.InlineMethod)
				throw new ArgumentException("Opcode does not have a method operand", "opCode");
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
				throw new ArgumentException("Opcode does not have a token operand", "opCode");
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
				throw new ArgumentException("Opcode does not have a method sig operand", "opCode");
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
				throw new ArgumentException("Opcode does not have a method parameter operand", "opCode");
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
				throw new ArgumentException("Opcode does not have a method local operand", "opCode");
			return new Instruction(opCode, local);
		}
	}
}
