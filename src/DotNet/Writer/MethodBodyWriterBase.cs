// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
using System.IO;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Base class of all CIL method body writers
	/// </summary>
	public abstract class MethodBodyWriterBase {
		/// <summary/>
		protected readonly IList<Instruction> instructions;
		/// <summary/>
		protected readonly IList<ExceptionHandler> exceptionHandlers;
		readonly Dictionary<Instruction, uint> offsets = new Dictionary<Instruction, uint>();
		uint firstInstructionOffset;
		int errors;

		/// <summary>
		/// <c>true</c> if there was at least one error
		/// </summary>
		public bool ErrorDetected {
			get { return errors > 0; }
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
			uint maxStack;
			if (!MaxStackCalculator.GetMaxStack(instructions, exceptionHandlers, out maxStack)) {
				Error("Error calculating max stack value. If the method's obfuscated, set CilBody.KeepOldMaxStack or MetaDataOptions.Flags (KeepOldMaxStack, global option) to ignore this error. Otherwise fix your generated CIL code so it conforms to the ECMA standard.");
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
			if (instr == null) {
				Error("Instruction is null");
				return 0;
			}
			uint offset;
			if (offsets.TryGetValue(instr, out offset))
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
			foreach (var instr in instructions) {
				if (instr == null)
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
		protected virtual uint GetSizeOfInstruction(Instruction instr) {
			return (uint)instr.GetSize();
		}

		/// <summary>
		/// Writes all instructions to <paramref name="writer"/> at its current offset
		/// </summary>
		/// <param name="writer">The instruction writer</param>
		/// <returns>Number of bytes written</returns>
		protected uint WriteInstructions(BinaryWriter writer) {
			firstInstructionOffset = (uint)writer.BaseStream.Position;
			foreach (var instr in instructions) {
				if (instr == null)
					continue;
				WriteInstruction(writer, instr);
			}
			return ToInstructionOffset(writer);
		}

		/// <summary>
		/// Gets the current offset in the instruction stream. This offset is relative to
		/// the first written instruction.
		/// </summary>
		/// <param name="writer">The instruction writer</param>
		/// <returns>Current offset, relative to the first written instruction</returns>
		protected uint ToInstructionOffset(BinaryWriter writer) {
			return (uint)writer.BaseStream.Position - firstInstructionOffset;
		}

		/// <summary>
		/// Writes an instruction
		/// </summary>
		/// <param name="writer">The instruction writer</param>
		/// <param name="instr">The instruction</param>
		protected virtual void WriteInstruction(BinaryWriter writer, Instruction instr) {
			WriteOpCode(writer, instr);
			WriteOperand(writer, instr);
		}

		/// <summary>
		/// Writes an instruction's opcode
		/// </summary>
		/// <param name="writer">The instruction writer</param>
		/// <param name="instr">The instruction</param>
		protected virtual void WriteOpCode(BinaryWriter writer, Instruction instr) {
			var code = instr.OpCode.Code;
			if ((ushort)code <= 0xFF)
				writer.Write((byte)code);
			else if (((ushort)code >> 8) == 0xFE) {
				writer.Write((byte)((ushort)code >> 8));
				writer.Write((byte)code);
			}
			else if (code == Code.UNKNOWN1)
				writer.Write((byte)Code.Nop);
			else if (code == Code.UNKNOWN2)
				writer.Write((ushort)(((ushort)Code.Nop << 8) | Code.Nop));
			else {
				Error("Unknown instruction");
				writer.Write((byte)Code.Nop);
			}
		}

		/// <summary>
		/// Writes an instruction's operand
		/// </summary>
		/// <param name="writer">The instruction writer</param>
		/// <param name="instr">The instruction</param>
		protected virtual void WriteOperand(BinaryWriter writer, Instruction instr) {
			switch (instr.OpCode.OperandType) {
			case OperandType.InlineBrTarget:	WriteInlineBrTarget(writer, instr); break;
			case OperandType.InlineField:		WriteInlineField(writer, instr); break;
			case OperandType.InlineI:			WriteInlineI(writer, instr); break;
			case OperandType.InlineI8:			WriteInlineI8(writer, instr); break;
			case OperandType.InlineMethod:		WriteInlineMethod(writer, instr); break;
			case OperandType.InlineNone:		WriteInlineNone(writer, instr); break;
			case OperandType.InlinePhi:			WriteInlinePhi(writer, instr); break;
			case OperandType.InlineR:			WriteInlineR(writer, instr); break;
			case OperandType.InlineSig:			WriteInlineSig(writer, instr); break;
			case OperandType.InlineString:		WriteInlineString(writer, instr); break;
			case OperandType.InlineSwitch:		WriteInlineSwitch(writer, instr); break;
			case OperandType.InlineTok:			WriteInlineTok(writer, instr); break;
			case OperandType.InlineType:		WriteInlineType(writer, instr); break;
			case OperandType.InlineVar:			WriteInlineVar(writer, instr); break;
			case OperandType.ShortInlineBrTarget: WriteShortInlineBrTarget(writer, instr); break;
			case OperandType.ShortInlineI:		WriteShortInlineI(writer, instr); break;
			case OperandType.ShortInlineR:		WriteShortInlineR(writer, instr); break;
			case OperandType.ShortInlineVar:	WriteShortInlineVar(writer, instr); break;

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
		protected virtual void WriteInlineBrTarget(BinaryWriter writer, Instruction instr) {
			uint displ = GetOffset(instr.Operand as Instruction) - (ToInstructionOffset(writer) + 4);
			writer.Write(displ);
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineField"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected abstract void WriteInlineField(BinaryWriter writer, Instruction instr);

		/// <summary>
		/// Writes an <see cref="OperandType.InlineI"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineI(BinaryWriter writer, Instruction instr) {
			if (instr.Operand is int)
				writer.Write((int)instr.Operand);
			else {
				Error("Operand is not an Int32");
				writer.Write(0);
			}
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineI8"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineI8(BinaryWriter writer, Instruction instr) {
			if (instr.Operand is long)
				writer.Write((long)instr.Operand);
			else {
				Error("Operand is not an Int64");
				writer.Write(0L);
			}
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineMethod"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected abstract void WriteInlineMethod(BinaryWriter writer, Instruction instr);

		/// <summary>
		/// Writes an <see cref="OperandType.InlineNone"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineNone(BinaryWriter writer, Instruction instr) {
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlinePhi"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlinePhi(BinaryWriter writer, Instruction instr) {
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineR"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineR(BinaryWriter writer, Instruction instr) {
			if (instr.Operand is double)
				writer.Write((double)instr.Operand);
			else {
				Error("Operand is not a Double");
				writer.Write(0D);
			}
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineSig"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected abstract void WriteInlineSig(BinaryWriter writer, Instruction instr);

		/// <summary>
		/// Writes an <see cref="OperandType.InlineString"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected abstract void WriteInlineString(BinaryWriter writer, Instruction instr);

		/// <summary>
		/// Writes an <see cref="OperandType.InlineSwitch"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineSwitch(BinaryWriter writer, Instruction instr) {
			var targets = instr.Operand as IList<Instruction>;
			if (targets == null) {
				Error("switch operand is not a list of instructions");
				writer.Write(0);
			}
			else {
				uint offsetAfter = (uint)(ToInstructionOffset(writer) + 4 + targets.Count * 4);
				writer.Write(targets.Count);
				foreach (var target in targets)
					writer.Write(GetOffset(target) - offsetAfter);
			}
		}

		/// <summary>
		/// Writes an <see cref="OperandType.InlineTok"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected abstract void WriteInlineTok(BinaryWriter writer, Instruction instr);

		/// <summary>
		/// Writes an <see cref="OperandType.InlineType"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected abstract void WriteInlineType(BinaryWriter writer, Instruction instr);

		/// <summary>
		/// Writes an <see cref="OperandType.InlineVar"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteInlineVar(BinaryWriter writer, Instruction instr) {
			var variable = instr.Operand as IVariable;
			if (variable == null) {
				Error("Operand is not a local/arg");
				writer.Write((ushort)0);
			}
			else if (ushort.MinValue <= variable.Index && variable.Index <= ushort.MaxValue)
				writer.Write((ushort)variable.Index);
			else {
				Error("Local/arg index doesn't fit in a UInt16");
				writer.Write((ushort)0);
			}
		}

		/// <summary>
		/// Writes a <see cref="OperandType.ShortInlineBrTarget"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteShortInlineBrTarget(BinaryWriter writer, Instruction instr) {
			int displ = (int)(GetOffset(instr.Operand as Instruction) - (ToInstructionOffset(writer) + 1));
			if (sbyte.MinValue <= displ && displ <= sbyte.MaxValue)
				writer.Write((sbyte)displ);
			else {
				Error("Target instruction is too far away for a short branch. Use the long branch or call CilBody.SimplifyBranches() and CilBody.OptimizeBranches()");
				writer.Write((byte)0);
			}
		}

		/// <summary>
		/// Writes a <see cref="OperandType.ShortInlineI"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteShortInlineI(BinaryWriter writer, Instruction instr) {
			if (instr.Operand is sbyte)
				writer.Write((sbyte)instr.Operand);
			else if (instr.Operand is byte)
				writer.Write((byte)instr.Operand);
			else {
				Error("Operand is not a Byte or a SByte");
				writer.Write((byte)0);
			}
		}

		/// <summary>
		/// Writes a <see cref="OperandType.ShortInlineR"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteShortInlineR(BinaryWriter writer, Instruction instr) {
			if (instr.Operand is float)
				writer.Write((float)instr.Operand);
			else {
				Error("Operand is not a Single");
				writer.Write(0F);
			}
		}

		/// <summary>
		/// Writes a <see cref="OperandType.ShortInlineVar"/> operand
		/// </summary>
		/// <param name="writer">Instruction writer</param>
		/// <param name="instr">Instruction</param>
		protected virtual void WriteShortInlineVar(BinaryWriter writer, Instruction instr) {
			var variable = instr.Operand as IVariable;
			if (variable == null) {
				Error("Operand is not a local/arg");
				writer.Write((byte)0);
			}
			else if (byte.MinValue <= variable.Index && variable.Index <= byte.MaxValue)
				writer.Write((byte)variable.Index);
			else {
				Error("Local/arg index doesn't fit in a Byte. Use the longer ldloc/ldarg/stloc/starg instruction.");
				writer.Write((byte)0);
			}
		}
	}
}
