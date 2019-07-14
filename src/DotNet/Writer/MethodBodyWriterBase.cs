// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Base class of all CIL method body writers
	/// </summary>
	public abstract class MethodBodyWriterBase {
		/// <summary/>
		protected IList<Instruction> instructions;
		/// <summary/>
		protected IList<ExceptionHandler> exceptionHandlers;
		readonly Dictionary<Instruction, uint> offsets = new Dictionary<Instruction, uint>();
		uint firstInstructionOffset;
		int errors;
		MaxStackCalculator maxStackCalculator = MaxStackCalculator.Create();

		/// <summary>
		/// <c>true</c> if there was at least one error
		/// </summary>
		public bool ErrorDetected => errors > 0;

		internal MethodBodyWriterBase() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="instructions">All instructions</param>
		/// <param name="exceptionHandlers">All exception handlers</param>
		protected MethodBodyWriterBase(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			this.instructions = instructions;
			this.exceptionHandlers = exceptionHandlers;
		}

		internal void Reset(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			this.instructions = instructions;
			this.exceptionHandlers = exceptionHandlers;
			offsets.Clear();
			firstInstructionOffset = 0;
			errors = 0;
		}

		/// <summary>
		/// Called when an error is detected (eg. a null pointer). The error can be
		/// ignored but the method won't be valid.
		/// </summary>
		/// <param name="message">Error message</param>
		protected void Error(string message) {
			errors++;
			ErrorImpl(message);
		}

		/// <summary>
		/// Called when an error is detected (eg. a null pointer). The error can be
		/// ignored but the method won't be valid.
		/// </summary>
		/// <param name="message">Error message</param>
		protected virtual void ErrorImpl(string message) {
		}

		/// <summary>
		/// Gets max stack value
		/// </summary>
		protected uint GetMaxStack() {
			if (instructions.Count == 0)
				return 0;
			maxStackCalculator.Reset(instructions, exceptionHandlers);
			if (!maxStackCalculator.Calculate(out uint maxStack)) {
				Error("Error calculating max stack value. If the method's obfuscated, set CilBody.KeepOldMaxStack or MetadataOptions.Flags (KeepOldMaxStack, global option) to ignore this error. Otherwise fix your generated CIL code so it conforms to the ECMA standard.");
				maxStack += 8;
			}
			return maxStack;
		}

		/// <summary>
		/// Gets the offset of an instruction
		/// </summary>
		/// <param name="instr">The instruction</param>
		/// <returns>The offset or <c>0</c> if <paramref name="instr"/> is <c>null</c> or not
		/// present in the list of all instructions.</returns>
		protected uint GetOffset(Instruction instr) {
			if (instr is null) {
				Error("Instruction is null");
				return 0;
			}
			if (offsets.TryGetValue(instr, out uint offset))
				return offset;
			Error("Found some other method's instruction or a removed instruction. You probably removed an instruction that is the target of a branch instruction or an instruction that's the first/last instruction in an exception handler.");
			return 0;
		}

		/// <summary>
		/// Initializes instruction offsets and returns the total code size
		/// </summary>
		/// <returns>Size of code</returns>
		protected uint InitializeInstructionOffsets() {
			uint offset = 0;
			var instructions = this.instructions;
			for (int i = 0; i < instructions.Count; i++) {
				var instr = instructions[i];
				if (instr is null)
					continue;
				offsets[instr] = offset;
				offset += GetSizeOfInstruction(instr);
			}
			return offset;
		}

		/// <summary>
		/// Gets the size of an instruction
		/// </summary>
		/// <param name="instr">The instruction</param>
		/// <returns>Size of the instruction in bytes</returns>
		protected virtual uint GetSizeOfInstruction(Instruction instr) => (uint)instr.GetSize();

		/// <summary>
		/// Writes all instructions to <paramref name="writer"/> at its current offset
		/// </summary>
		/// <param name="writer">The instruction writer</param>
		/// <returns>Number of bytes written</returns>
		protected uint WriteInstructions(ref ArrayWriter writer) {
			firstInstructionOffset = (uint)writer.Position;
			var instructions = this.instructions;
			for (int i = 0; i < instructions.Count; i++) {
				var instr = instructions[i];
				if (instr is null)
					continue;
				WriteInstruction(ref writer, instr);
			}
			return ToInstructionOffset(ref writer);
		}

		/// <summary>
		/// Gets the current offset in the instruction stream. This offset is relative to
		/// the first written instruction.
		/// </summary>
		/// <param name="writer">The instruction writer</param>
		/// <returns>Current offset, relative to the first written instruction</returns>
		protected uint ToInstructionOffset(ref ArrayWriter writer) => (uint)writer.Position - firstInstructionOffset;

		/// <summary>
		/// Writes an instruction
		/// </summary>
		/// <param name="writer">The instruction writer</param>
		/// <param name="instr">The instruction</param>
		protected virtual void WriteInstruction(ref ArrayWriter writer, Instruction instr) {
			WriteOpCode(ref writer, instr);
			WriteOperand(ref writer, instr);
		}

		/// <summary>
		/// Writes an instruction's opcode
		/// </summary>
		/// <param name="writer">The instruction writer</param>
		/// <param name="instr">The instruction</param>
		protected void WriteOpCode(ref ArrayWriter writer, Instruction instr) {
			var code = instr.OpCode.Code;
			if ((ushort)code <= 0xFF)
				writer.WriteByte((byte)code);
			else if (((ushort)code >> 8) == 0xFE) {
				writer.WriteByte((byte)((ushort)code >> 8));
				writer.WriteByte((byte)code);
			}
			else if (code == Code.UNKNOWN1)
				writer.WriteByte((byte)Code.Nop);
			else if (code == Code.UNKNOWN2)
				writer.WriteUInt16((ushort)(((ushort)Code.Nop << 8) | Code.Nop));
			else {
				Error("Unknown instruction");
				writer.WriteByte((byte)Code.Nop);
			}
		}

		/// <summary>
		/// Writes an instruction's operand
		/// </summary>
		/// <param name="writer">The instruction writer</param>
		/// <param name="instr">The instruction</param>
		protected void WriteOperand(ref ArrayWriter writer, Instruction instr) {
			switch (instr.OpCode.OperandType) {
			case OperandType.InlineBrTarget:	WriteInlineBrTarget(ref writer, instr); break;
			case OperandType.InlineField:		WriteInlineField(ref writer, instr); break;
			case OperandType.InlineI:			WriteInlineI(ref writer, instr); break;
			case OperandType.InlineI8:			WriteInlineI8(ref writer, instr); break;
			case OperandType.InlineMethod:		WriteInlineMethod(ref writer, instr); break;
			case OperandType.InlineNone:		WriteInlineNone(ref writer, instr); break;
			case OperandType.InlinePhi:			WriteInlinePhi(ref writer, instr); break;
			case OperandType.InlineR:			WriteInlineR(ref writer, instr); break;
			case OperandType.InlineSig:			WriteInlineSig(ref writer, instr); break;
			case OperandType.InlineString:		WriteInlineString(ref writer, instr); break;
			case OperandType.InlineSwitch:		WriteInlineSwitch(ref writer, instr); break;
			case OperandType.InlineTok:			WriteInlineTok(ref writer, instr); break;
			case OperandType.InlineType:		WriteInlineType(ref writer, instr); break;
			case OperandType.InlineVar:			WriteInlineVar(ref writer, instr); break;
			case OperandType.ShortInlineBrTarget: WriteShortInlineBrTarget(ref writer, instr); break;
			case OperandType.ShortInlineI:		WriteShortInlineI(ref writer, instr); break;
			case OperandType.ShortInlineR:		WriteShortInlineR(ref writer, instr); break;
			case OperandType.ShortInlineVar:	WriteShortInlineVar(ref writer, instr); break;

			default:
				Error("Unknown operand type");
				break;
			}
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineBrTarget"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineBrTarget(ref ArrayWriter writer, Instruction instr) {
			uint displ = GetOffset(instr.Operand as Instruction) - (ToInstructionOffset(ref writer) + 4);
			writer.WriteUInt32(displ);
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineField"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected abstract void WriteInlineField(ref ArrayWriter writer, Instruction instr);

		/// <summary>
		/// Writes an <see cref="OperandType.InlineI"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineI(ref ArrayWriter writer, Instruction instr) {
			if (instr.Operand is int)
				writer.WriteInt32((int)instr.Operand);
			else {
				Error("Operand is not an Int32");
				writer.WriteInt32(0);
			}
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineI8"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineI8(ref ArrayWriter writer, Instruction instr) {
			if (instr.Operand is long)
				writer.WriteInt64((long)instr.Operand);
			else {
				Error("Operand is not an Int64");
				writer.WriteInt64(0);
			}
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineMethod"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected abstract void WriteInlineMethod(ref ArrayWriter writer, Instruction instr);

		/// <summary>
		/// Writes an <see cref="OperandType.InlineNone"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineNone(ref ArrayWriter writer, Instruction instr) {
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlinePhi"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlinePhi(ref ArrayWriter writer, Instruction instr) {
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineR"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineR(ref ArrayWriter writer, Instruction instr) {
			if (instr.Operand is double)
				writer.WriteDouble((double)instr.Operand);
			else {
				Error("Operand is not a Double");
				writer.WriteDouble(0);
			}
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineSig"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected abstract void WriteInlineSig(ref ArrayWriter writer, Instruction instr);

		/// <summary>
		/// Writes an <see cref="OperandType.InlineString"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected abstract void WriteInlineString(ref ArrayWriter writer, Instruction instr);

		/// <summary>
		/// Writes an <see cref="OperandType.InlineSwitch"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineSwitch(ref ArrayWriter writer, Instruction instr) {
			var targets = instr.Operand as IList<Instruction>;
			if (targets is null) {
				Error("switch operand is not a list of instructions");
				writer.WriteInt32(0);
			}
			else {
				uint offsetAfter = (uint)(ToInstructionOffset(ref writer) + 4 + targets.Count * 4);
				writer.WriteInt32(targets.Count);
				for (int i = 0; i < targets.Count; i++) {
					var target = targets[i];
					writer.WriteUInt32(GetOffset(target) - offsetAfter);
				}
			}
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineTok"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected abstract void WriteInlineTok(ref ArrayWriter writer, Instruction instr);

		/// <summary>
		/// Writes an <see cref="OperandType.InlineType"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected abstract void WriteInlineType(ref ArrayWriter writer, Instruction instr);

		/// <summary>
		/// Writes an <see cref="OperandType.InlineVar"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineVar(ref ArrayWriter writer, Instruction instr) {
			var variable = instr.Operand as IVariable;
			if (variable is null) {
				Error("Operand is not a local/arg");
				writer.WriteUInt16(0);
			}
			else {
				int index = variable.Index;
				if (ushort.MinValue <= index && index <= ushort.MaxValue)
					writer.WriteUInt16((ushort)index);
				else {
					Error("Local/arg index doesn't fit in a UInt16");
					writer.WriteUInt16(0);
				}
			}
		}

		/// <summary>
		/// Writes a <see cref="OperandType.ShortInlineBrTarget"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteShortInlineBrTarget(ref ArrayWriter writer, Instruction instr) {
			int displ = (int)(GetOffset(instr.Operand as Instruction) - (ToInstructionOffset(ref writer) + 1));
			if (sbyte.MinValue <= displ && displ <= sbyte.MaxValue)
				writer.WriteSByte((sbyte)displ);
			else {
				Error("Target instruction is too far away for a short branch. Use the long branch or call CilBody.SimplifyBranches() and CilBody.OptimizeBranches()");
				writer.WriteByte(0);
			}
		}

		/// <summary>
		/// Writes a <see cref="OperandType.ShortInlineI"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteShortInlineI(ref ArrayWriter writer, Instruction instr) {
			if (instr.Operand is sbyte)
				writer.WriteSByte((sbyte)instr.Operand);
			else if (instr.Operand is byte)
				writer.WriteByte((byte)instr.Operand);
			else {
				Error("Operand is not a Byte or a SByte");
				writer.WriteByte(0);
			}
		}

		/// <summary>
		/// Writes a <see cref="OperandType.ShortInlineR"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteShortInlineR(ref ArrayWriter writer, Instruction instr) {
			if (instr.Operand is float)
				writer.WriteSingle((float)instr.Operand);
			else {
				Error("Operand is not a Single");
				writer.WriteSingle(0);
			}
		}

		/// <summary>
		/// Writes a <see cref="OperandType.ShortInlineVar"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteShortInlineVar(ref ArrayWriter writer, Instruction instr) {
			var variable = instr.Operand as IVariable;
			if (variable is null) {
				Error("Operand is not a local/arg");
				writer.WriteByte(0);
			}
			else {
				int index = variable.Index;
				if (byte.MinValue <= index && index <= byte.MaxValue)
					writer.WriteByte((byte)index);
				else {
					Error("Local/arg index doesn't fit in a Byte. Use the longer ldloc/ldarg/stloc/starg instruction.");
					writer.WriteByte(0);
				}
			}
		}
	}
}
