// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using dnlib.IO;
using dnlib.Threading;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Method body reader base class
	/// </summary>
	public abstract class MethodBodyReaderBase {
		/// <summary>The method reader</summary>
		protected IBinaryReader reader;
		/// <summary>All parameters</summary>
		protected IList<Parameter> parameters;
		/// <summary>All locals</summary>
		protected IList<Local> locals = new List<Local>();
		/// <summary>All instructions</summary>
		protected IList<Instruction> instructions;
		/// <summary>All exception handlers</summary>
		protected IList<ExceptionHandler> exceptionHandlers = new List<ExceptionHandler>();
		uint currentOffset;
		/// <summary>First byte after the end of the code</summary>
		protected long codeEndOffs;
		/// <summary>Start offset of method</summary>
		protected long codeStartOffs;

		/// <summary>
		/// Gets all parameters
		/// </summary>
		public IList<Parameter> Parameters {
			get { return parameters; }
		}

		/// <summary>
		/// Gets all locals
		/// </summary>
		public IList<Local> Locals {
			get { return locals; }
		}

		/// <summary>
		/// Gets all instructions
		/// </summary>
		public IList<Instruction> Instructions {
			get { return instructions; }
		}

		/// <summary>
		/// Gets all exception handlers
		/// </summary>
		public IList<ExceptionHandler> ExceptionHandlers {
			get { return exceptionHandlers; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		protected MethodBodyReaderBase() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">The reader</param>
		protected MethodBodyReaderBase(IBinaryReader reader)
			: this(reader, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">The reader</param>
		/// <param name="parameters">Method parameters or <c>null</c> if they're not known yet</param>
		protected MethodBodyReaderBase(IBinaryReader reader, IList<Parameter> parameters) {
			this.reader = reader;
			this.parameters = parameters;
		}

		/// <summary>
		/// Sets new locals
		/// </summary>
		/// <param name="newLocals">A list of types of all locals or <c>null</c> if none</param>
		protected void SetLocals(IList<TypeSig> newLocals) {
			locals.Clear();
			if (newLocals == null)
				return;
			foreach (var typeSig in newLocals.GetSafeEnumerable())
				locals.Add(new Local(typeSig));
		}

		/// <summary>
		/// Sets new locals
		/// </summary>
		/// <param name="newLocals">A list of types of all locals or <c>null</c> if none</param>
		protected void SetLocals(IList<Local> newLocals) {
			locals.Clear();
			if (newLocals == null)
				return;
			foreach (var local in newLocals.GetSafeEnumerable())
				locals.Add(new Local(local.Type));
		}

		/// <summary>
		/// Reads all instructions
		/// </summary>
		/// <param name="numInstrs">Number of instructions to read</param>
		protected void ReadInstructions(int numInstrs) {
			codeStartOffs = reader.Position;
			codeEndOffs = reader.Length;	// We don't know the end pos so use the last one
			instructions = new List<Instruction>(numInstrs);
			currentOffset = 0;
			for (int i = 0; i < numInstrs && reader.Position < codeEndOffs; i++)
				instructions.Add(ReadOneInstruction());
			FixBranches();
		}

		/// <summary>
		/// Reads all instructions
		/// </summary>
		/// <param name="codeSize">Size of code</param>
		protected void ReadInstructionsNumBytes(uint codeSize) {
			codeStartOffs = reader.Position;
			codeEndOffs = reader.Position + codeSize;
			if (codeEndOffs < codeStartOffs || codeEndOffs > reader.Length)
				throw new InvalidMethodException("Invalid code size");

			instructions = new List<Instruction>();	//TODO: Estimate number of instructions based on codeSize
			currentOffset = 0;
			while (reader.Position < codeEndOffs)
				instructions.Add(ReadOneInstruction());
			reader.Position = codeEndOffs;
			FixBranches();
		}

		/// <summary>
		/// Fixes all branch instructions so their operands are set to an <see cref="Instruction"/>
		/// instead of an offset.
		/// </summary>
		void FixBranches() {
			foreach (var instr in instructions) {
				switch (instr.OpCode.OperandType) {
				case OperandType.InlineBrTarget:
				case OperandType.ShortInlineBrTarget:
					instr.Operand = GetInstruction((uint)instr.Operand);
					break;

				case OperandType.InlineSwitch:
					var uintTargets = (IList<uint>)instr.Operand;
					var targets = new Instruction[uintTargets.Count];
					for (int i = 0; i < uintTargets.Count; i++)
						targets[i] = GetInstruction(uintTargets[i]);
					instr.Operand = targets;
					break;
				}
			}
		}

		/// <summary>
		/// Finds an instruction
		/// </summary>
		/// <param name="offset">Offset of instruction</param>
		/// <returns>The instruction or <c>null</c> if there's no instruction at <paramref name="offset"/>.</returns>
		protected Instruction GetInstruction(uint offset) {
			// The instructions are sorted and all Offset fields are correct. Do a binary search.
			int lo = 0, hi = instructions.Count - 1;
			while (lo <= hi && hi != -1) {
				int i = (lo + hi) / 2;
				var instr = instructions[i];
				if (instr.Offset == offset)
					return instr;
				if (offset < instr.Offset)
					hi = i - 1;
				else
					lo = i + 1;
			}
			return null;
		}

		/// <summary>
		/// Finds an instruction and throws if it's not present
		/// </summary>
		/// <param name="offset">Offset of instruction</param>
		/// <returns>The instruction</returns>
		/// <exception cref="InvalidOperationException">There's no instruction at
		/// <paramref name="offset"/></exception>
		protected Instruction GetInstructionThrow(uint offset) {
			var instr = GetInstruction(offset);
			if (instr != null)
				return instr;
			throw new InvalidOperationException(string.Format("There's no instruction @ {0:X4}", offset));
		}

		/// <summary>
		/// Reads the next instruction
		/// </summary>
		Instruction ReadOneInstruction() {
			var instr = new Instruction();
			instr.Offset = currentOffset;
			instr.OpCode = ReadOpCode();
			instr.Operand = ReadOperand(instr);

			if (instr.OpCode.Code == Code.Switch) {
				var targets = (IList<uint>)instr.Operand;
				currentOffset += (uint)(instr.OpCode.Size + 4 + 4 * targets.Count);
			}
			else
				currentOffset += (uint)instr.GetSize();
			if (currentOffset < instr.Offset)
				reader.Position = codeEndOffs;
			return instr;
		}

		/// <summary>
		/// Reads the next OpCode from the current position
		/// </summary>
		OpCode ReadOpCode() {
			var op = reader.ReadByte();
			if (op != 0xFE)
				return OpCodes.OneByteOpCodes[op];
			return OpCodes.TwoByteOpCodes[reader.ReadByte()];
		}

		/// <summary>
		/// Reads the instruction operand (if any)
		/// </summary>
		/// <param name="instr">The instruction</param>
		object ReadOperand(Instruction instr) {
			switch (instr.OpCode.OperandType) {
			case OperandType.InlineBrTarget:	return ReadInlineBrTarget(instr);
			case OperandType.InlineField:		return ReadInlineField(instr);
			case OperandType.InlineI:			return ReadInlineI(instr);
			case OperandType.InlineI8:			return ReadInlineI8(instr);
			case OperandType.InlineMethod:		return ReadInlineMethod(instr);
			case OperandType.InlineNone:		return ReadInlineNone(instr);
			case OperandType.InlinePhi:			return ReadInlinePhi(instr);
			case OperandType.InlineR:			return ReadInlineR(instr);
			case OperandType.InlineSig:			return ReadInlineSig(instr);
			case OperandType.InlineString:		return ReadInlineString(instr);
			case OperandType.InlineSwitch:		return ReadInlineSwitch(instr);
			case OperandType.InlineTok:			return ReadInlineTok(instr);
			case OperandType.InlineType:		return ReadInlineType(instr);
			case OperandType.InlineVar:			return ReadInlineVar(instr);
			case OperandType.ShortInlineBrTarget: return ReadShortInlineBrTarget(instr);
			case OperandType.ShortInlineI:		return ReadShortInlineI(instr);
			case OperandType.ShortInlineR:		return ReadShortInlineR(instr);
			case OperandType.ShortInlineVar:	return ReadShortInlineVar(instr);
			default: throw new InvalidOperationException("Invalid OpCode.OperandType");
			}
		}

		/// <summary>
		/// Reads a <see cref="OperandType.InlineBrTarget"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual uint ReadInlineBrTarget(Instruction instr) {
			return instr.Offset + (uint)instr.GetSize() + reader.ReadUInt32();
		}

		/// <summary>
		/// Reads a <see cref="OperandType.InlineField"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected abstract IField ReadInlineField(Instruction instr);

		/// <summary>
		/// Reads a <see cref="OperandType.InlineI"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual int ReadInlineI(Instruction instr) {
			return reader.ReadInt32();
		}

		/// <summary>
		/// Reads a <see cref="OperandType.InlineI8"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual long ReadInlineI8(Instruction instr) {
			return reader.ReadInt64();
		}

		/// <summary>
		/// Reads a <see cref="OperandType.InlineMethod"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected abstract IMethod ReadInlineMethod(Instruction instr);

		/// <summary>
		/// Reads a <see cref="OperandType.InlineNone"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual object ReadInlineNone(Instruction instr) {
			return null;
		}

		/// <summary>
		/// Reads a <see cref="OperandType.InlinePhi"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual object ReadInlinePhi(Instruction instr) {
			return null;
		}

		/// <summary>
		/// Reads a <see cref="OperandType.InlineR"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual double ReadInlineR(Instruction instr) {
			return reader.ReadDouble();
		}

		/// <summary>
		/// Reads a <see cref="OperandType.InlineSig"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected abstract MethodSig ReadInlineSig(Instruction instr);

		/// <summary>
		/// Reads a <see cref="OperandType.InlineString"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected abstract string ReadInlineString(Instruction instr);

		/// <summary>
		/// Reads a <see cref="OperandType.InlineSwitch"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual IList<uint> ReadInlineSwitch(Instruction instr) {
			var num = reader.ReadUInt32();
			long offsetAfterInstr = (long)instr.Offset + (long)instr.OpCode.Size + 4L + (long)num * 4;
			if (offsetAfterInstr > uint.MaxValue || codeStartOffs + offsetAfterInstr > codeEndOffs) {
				reader.Position = codeEndOffs;
				return new uint[0];
			}

			var targets = new uint[num];
			uint offset = (uint)offsetAfterInstr;
			for (int i = 0; i < targets.Length; i++)
				targets[i] = offset + reader.ReadUInt32();
			return targets;
		}

		/// <summary>
		/// Reads a <see cref="OperandType.InlineTok"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected abstract ITokenOperand ReadInlineTok(Instruction instr);

		/// <summary>
		/// Reads a <see cref="OperandType.InlineType"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected abstract ITypeDefOrRef ReadInlineType(Instruction instr);

		/// <summary>
		/// Reads a <see cref="OperandType.InlineVar"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual IVariable ReadInlineVar(Instruction instr) {
			if (IsArgOperandInstruction(instr))
				return ReadInlineVarArg(instr);
			return ReadInlineVarLocal(instr);
		}

		/// <summary>
		/// Reads a <see cref="OperandType.InlineVar"/> (a parameter) operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual Parameter ReadInlineVarArg(Instruction instr) {
			return GetParameter(reader.ReadUInt16());
		}

		/// <summary>
		/// Reads a <see cref="OperandType.InlineVar"/> (a local) operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual Local ReadInlineVarLocal(Instruction instr) {
			return GetLocal(reader.ReadUInt16());
		}

		/// <summary>
		/// Reads a <see cref="OperandType.ShortInlineBrTarget"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual uint ReadShortInlineBrTarget(Instruction instr) {
			return instr.Offset + (uint)instr.GetSize() + (uint)reader.ReadSByte();
		}

		/// <summary>
		/// Reads a <see cref="OperandType.ShortInlineI"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual object ReadShortInlineI(Instruction instr) {
			if (instr.OpCode.Code == Code.Ldc_I4_S)
				return reader.ReadSByte();
			return reader.ReadByte();
		}

		/// <summary>
		/// Reads a <see cref="OperandType.ShortInlineR"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual float ReadShortInlineR(Instruction instr) {
			return reader.ReadSingle();
		}

		/// <summary>
		/// Reads a <see cref="OperandType.ShortInlineVar"/> operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual IVariable ReadShortInlineVar(Instruction instr) {
			if (IsArgOperandInstruction(instr))
				return ReadShortInlineVarArg(instr);
			return ReadShortInlineVarLocal(instr);
		}

		/// <summary>
		/// Reads a <see cref="OperandType.ShortInlineVar"/> (a parameter) operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual Parameter ReadShortInlineVarArg(Instruction instr) {
			return GetParameter(reader.ReadByte());
		}

		/// <summary>
		/// Reads a <see cref="OperandType.ShortInlineVar"/> (a local) operand
		/// </summary>
		/// <param name="instr">The current instruction</param>
		/// <returns>The operand</returns>
		protected virtual Local ReadShortInlineVarLocal(Instruction instr) {
			return GetLocal(reader.ReadByte());
		}

		/// <summary>
		/// Returns <c>true</c> if it's one of the ldarg/starg instructions that have an operand
		/// </summary>
		/// <param name="instr">The instruction to check</param>
		protected static bool IsArgOperandInstruction(Instruction instr) {
			switch (instr.OpCode.Code) {
			case Code.Ldarg:
			case Code.Ldarg_S:
			case Code.Ldarga:
			case Code.Ldarga_S:
			case Code.Starg:
			case Code.Starg_S:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Returns a parameter
		/// </summary>
		/// <param name="index">A parameter index</param>
		/// <returns>A <see cref="Parameter"/> or <c>null</c> if <paramref name="index"/> is invalid</returns>
		protected Parameter GetParameter(int index) {
			return parameters.Get(index, null);
		}

		/// <summary>
		/// Returns a local
		/// </summary>
		/// <param name="index">A local index</param>
		/// <returns>A <see cref="Local"/> or <c>null</c> if <paramref name="index"/> is invalid</returns>
		protected Local GetLocal(int index) {
			return locals.Get(index, null);
		}

		/// <summary>
		/// Add an exception handler if it appears valid
		/// </summary>
		/// <param name="eh">The exception handler</param>
		/// <returns><c>true</c> if it was added, <c>false</c> otherwise</returns>
		protected bool Add(ExceptionHandler eh) {
			uint tryStart = GetOffset(eh.TryStart);
			uint tryEnd = GetOffset(eh.TryEnd);
			if (tryEnd <= tryStart)
				return false;

			uint handlerStart = GetOffset(eh.HandlerStart);
			uint handlerEnd = GetOffset(eh.HandlerEnd);
			if (handlerEnd <= handlerStart)
				return false;

			if (eh.HandlerType == ExceptionHandlerType.Filter) {
				if (eh.FilterStart == null)
					return false;
				if (eh.FilterStart.Offset >= handlerStart)
					return false;
			}

			if (handlerStart <= tryStart && tryStart < handlerEnd)
				return false;
			if (handlerStart < tryEnd && tryEnd <= handlerEnd)
				return false;

			if (tryStart <= handlerStart && handlerStart < tryEnd)
				return false;
			if (tryStart < handlerEnd && handlerEnd <= tryEnd)
				return false;

			// It's probably valid, so let's add it.
			exceptionHandlers.Add(eh);
			return true;
		}

		/// <summary>
		/// Gets the offset of an instruction
		/// </summary>
		/// <param name="instr">The instruction or <c>null</c> if the offset is the first offset
		/// at the end of the method.</param>
		/// <returns>The instruction offset</returns>
		uint GetOffset(Instruction instr) {
			if (instr != null)
				return instr.Offset;
			if (instructions.Count == 0)
				return 0;
			return instructions[instructions.Count - 1].Offset;
		}

		/// <summary>
		/// Restores a <see cref="MethodDef"/>'s body with the parsed method instructions
		/// and exception handlers
		/// </summary>
		/// <param name="method">The method that gets updated with the instructions, locals, and
		/// exception handlers.</param>
		public virtual void RestoreMethod(MethodDef method) {
			var body = method.Body;

			body.Variables.Clear();
			if (locals != null) {
				foreach (var local in locals)
					body.Variables.Add(local);
			}

			body.Instructions.Clear();
			if (instructions != null) {
				foreach (var instr in instructions)
					body.Instructions.Add(instr);
			}

			body.ExceptionHandlers.Clear();
			if (exceptionHandlers != null) {
				foreach (var eh in exceptionHandlers)
					body.ExceptionHandlers.Add(eh);
			}
		}
	}
}
