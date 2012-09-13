namespace dot10.DotNet.Emit {
	/// <summary>
	/// A CIL opcode
	/// </summary>
	public class OpCode {
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
			get { return Code < (Code)0x100 ? 1 : 2; }
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
	}
}
