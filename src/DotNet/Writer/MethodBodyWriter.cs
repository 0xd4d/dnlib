// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Returns tokens of token types, strings and signatures
	/// </summary>
	public interface ITokenCreator : IWriterError {
		/// <summary>
		/// Gets the token of <paramref name="o"/>
		/// </summary>
		/// <param name="o">A token type or a string or a signature</param>
		/// <returns>The token</returns>
		MDToken GetToken(object o);

		/// <summary>
		/// Gets a <c>StandAloneSig</c> token
		/// </summary>
		/// <param name="locals">All locals</param>
		/// <param name="origToken">The original token or <c>0</c> if none</param>
		/// <returns>A <c>StandAloneSig</c> token or <c>0</c> if <paramref name="locals"/> is
		/// empty.</returns>
		MDToken GetToken(IList<TypeSig> locals, uint origToken);
	}

	/// <summary>
	/// Writes CIL method bodies
	/// </summary>
	public sealed class MethodBodyWriter : MethodBodyWriterBase {
		readonly ITokenCreator helper;
		readonly CilBody cilBody;
		readonly bool keepMaxStack;
		uint codeSize;
		uint maxStack;
		byte[] code;
		byte[] extraSections;
		uint localVarSigTok;

		/// <summary>
		/// Gets the code as a byte array. This is valid only after calling <see cref="Write()"/>.
		/// The size of this array is not necessarily a multiple of 4, even if there are exception
		/// handlers present. See also <see cref="GetFullMethodBody()"/>
		/// </summary>
		public byte[] Code {
			get { return code; }
		}

		/// <summary>
		/// Gets the extra sections (exception handlers) as a byte array or <c>null</c> if there
		/// are no exception handlers. This is valid only after calling <see cref="Write()"/>
		/// </summary>
		public byte[] ExtraSections {
			get { return extraSections; }
		}

		/// <summary>
		/// Gets the token of the locals
		/// </summary>
		public uint LocalVarSigTok {
			get { return localVarSigTok; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="helper">Helps this instance</param>
		/// <param name="cilBody">The CIL method body</param>
		public MethodBodyWriter(ITokenCreator helper, CilBody cilBody)
			: this(helper, cilBody, false) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="helper">Helps this instance</param>
		/// <param name="cilBody">The CIL method body</param>
		/// <param name="keepMaxStack">Keep the original max stack value that has been initialized
		/// in <paramref name="cilBody"/></param>
		public MethodBodyWriter(ITokenCreator helper, CilBody cilBody, bool keepMaxStack)
			: base(cilBody.Instructions, cilBody.ExceptionHandlers) {
			this.helper = helper;
			this.cilBody = cilBody;
			this.keepMaxStack = keepMaxStack;
		}

		/// <summary>
		/// Writes the method body
		/// </summary>
		public void Write() {
			codeSize = InitializeInstructionOffsets();
			maxStack = keepMaxStack ? cilBody.MaxStack : GetMaxStack();
			if (NeedFatHeader())
				WriteFatHeader();
			else
				WriteTinyHeader();
			if (exceptionHandlers.Count > 0)
				WriteExceptionHandlers();
		}

		/// <summary>
		/// Gets the code and (possible) exception handlers in one array. The exception handlers
		/// are 4-byte aligned.
		/// </summary>
		/// <returns>The code and any exception handlers</returns>
		public byte[] GetFullMethodBody() {
			int padding = Utils.AlignUp(code.Length, 4) - code.Length;
			var bytes = new byte[code.Length + (extraSections == null ? 0 : padding + extraSections.Length)];
			Array.Copy(code, 0, bytes, 0, code.Length);
			if (extraSections != null)
				Array.Copy(extraSections, 0, bytes, code.Length + padding, extraSections.Length);
			return bytes;
		}

		bool NeedFatHeader() {
			//TODO: If locals has cust attrs, we also need a fat header
			return codeSize > 0x3F ||
					exceptionHandlers.Count > 0 ||
					cilBody.HasVariables ||
					maxStack > 8;
		}

		void WriteFatHeader() {
			if (maxStack > ushort.MaxValue) {
				Error("MaxStack is too big");
				maxStack = ushort.MaxValue;
			}

			ushort flags = 0x3003;
			if (exceptionHandlers.Count > 0)
				flags |= 8;
			if (cilBody.InitLocals)
				flags |= 0x10;

			code = new byte[12 + codeSize];
			var writer = new BinaryWriter(new MemoryStream(code));
			writer.Write(flags);
			writer.Write((ushort)maxStack);
			writer.Write(codeSize);
			writer.Write(localVarSigTok = helper.GetToken(GetLocals(), cilBody.LocalVarSigTok).Raw);
			if (WriteInstructions(writer) != codeSize)
				Error("Didn't write all code bytes");
		}

		IList<TypeSig> GetLocals() {
			var localsSig = new TypeSig[cilBody.Variables.Count];
			for (int i = 0; i < cilBody.Variables.Count; i++)
				localsSig[i] = cilBody.Variables[i].Type;
			return localsSig;
		}

		void WriteTinyHeader() {
			localVarSigTok = 0;
			code = new byte[1 + codeSize];
			var writer = new BinaryWriter(new MemoryStream(code));
			writer.Write((byte)((codeSize << 2) | 2));
			if (WriteInstructions(writer) != codeSize)
				Error("Didn't write all code bytes");
		}

		void WriteExceptionHandlers() {
			var outStream = new MemoryStream();
			var writer = new BinaryWriter(outStream);
			if (NeedFatExceptionClauses())
				WriteFatExceptionClauses(writer);
			else
				WriteSmallExceptionClauses(writer);
			extraSections = outStream.ToArray();
		}

		bool NeedFatExceptionClauses() {
			// Size must fit in a byte, and since one small exception record is 12 bytes
			// and header is 4 bytes: x*12+4 <= 255 ==> x <= 20
			if (exceptionHandlers.Count > 20)
				return true;

			foreach (var eh in exceptionHandlers) {
				if (!FitsInSmallExceptionClause(eh.TryStart, eh.TryEnd))
					return true;
				if (!FitsInSmallExceptionClause(eh.HandlerStart, eh.HandlerEnd))
					return true;
			}

			return false;
		}

		bool FitsInSmallExceptionClause(Instruction start, Instruction end) {
			uint offs1 = GetOffset2(start);
			uint offs2 = GetOffset2(end);
			if (offs2 < offs1)
				return false;
			return offs1 <= ushort.MaxValue && offs2 - offs1 <= byte.MaxValue;
		}

		uint GetOffset2(Instruction instr) {
			if (instr == null)
				return codeSize;
			return GetOffset(instr);
		}

		void WriteFatExceptionClauses(BinaryWriter writer) {
			const int maxExceptionHandlers = (0x00FFFFFF - 4) / 24;
			int numExceptionHandlers = exceptionHandlers.Count;
			if (numExceptionHandlers > maxExceptionHandlers) {
				Error("Too many exception handlers");
				numExceptionHandlers = maxExceptionHandlers;
			}

			writer.Write((((uint)numExceptionHandlers * 24 + 4) << 8) | 0x41);
			for (int i = 0; i < numExceptionHandlers; i++) {
				var eh = exceptionHandlers[i];
				uint offs1, offs2;

				writer.Write((uint)eh.HandlerType);

				offs1 = GetOffset2(eh.TryStart);
				offs2 = GetOffset2(eh.TryEnd);
				if (offs2 <= offs1)
					Error("Exception handler: TryEnd <= TryStart");
				writer.Write(offs1);
				writer.Write(offs2 - offs1);

				offs1 = GetOffset2(eh.HandlerStart);
				offs2 = GetOffset2(eh.HandlerEnd);
				if (offs2 <= offs1)
					Error("Exception handler: HandlerEnd <= HandlerStart");
				writer.Write(offs1);
				writer.Write(offs2 - offs1);

				if (eh.HandlerType == ExceptionHandlerType.Catch)
					writer.Write(helper.GetToken(eh.CatchType).Raw);
				else if (eh.HandlerType == ExceptionHandlerType.Filter)
					writer.Write(GetOffset2(eh.FilterStart));
				else
					writer.Write(0);
			}
		}

		void WriteSmallExceptionClauses(BinaryWriter writer) {
			const int maxExceptionHandlers = (0xFF - 4) / 12;
			int numExceptionHandlers = exceptionHandlers.Count;
			if (numExceptionHandlers > maxExceptionHandlers) {
				Error("Too many exception handlers");
				numExceptionHandlers = maxExceptionHandlers;
			}

			writer.Write((((uint)numExceptionHandlers * 12 + 4) << 8) | 1);
			for (int i = 0; i < numExceptionHandlers; i++) {
				var eh = exceptionHandlers[i];
				uint offs1, offs2;

				writer.Write((ushort)eh.HandlerType);

				offs1 = GetOffset2(eh.TryStart);
				offs2 = GetOffset2(eh.TryEnd);
				if (offs2 <= offs1)
					Error("Exception handler: TryEnd <= TryStart");
				writer.Write((ushort)offs1);
				writer.Write((byte)(offs2 - offs1));

				offs1 = GetOffset2(eh.HandlerStart);
				offs2 = GetOffset2(eh.HandlerEnd);
				if (offs2 <= offs1)
					Error("Exception handler: HandlerEnd <= HandlerStart");
				writer.Write((ushort)offs1);
				writer.Write((byte)(offs2 - offs1));

				if (eh.HandlerType == ExceptionHandlerType.Catch)
					writer.Write(helper.GetToken(eh.CatchType).Raw);
				else if (eh.HandlerType == ExceptionHandlerType.Filter)
					writer.Write(GetOffset2(eh.FilterStart));
				else
					writer.Write(0);
			}
		}

		/// <inheritdoc/>
		protected override void ErrorImpl(string message) {
			helper.Error(message);
		}

		/// <inheritdoc/>
		protected override void WriteInlineField(BinaryWriter writer, Instruction instr) {
			writer.Write(helper.GetToken(instr.Operand).Raw);
		}

		/// <inheritdoc/>
		protected override void WriteInlineMethod(BinaryWriter writer, Instruction instr) {
			writer.Write(helper.GetToken(instr.Operand).Raw);
		}

		/// <inheritdoc/>
		protected override void WriteInlineSig(BinaryWriter writer, Instruction instr) {
			writer.Write(helper.GetToken(instr.Operand).Raw);
		}

		/// <inheritdoc/>
		protected override void WriteInlineString(BinaryWriter writer, Instruction instr) {
			writer.Write(helper.GetToken(instr.Operand).Raw);
		}

		/// <inheritdoc/>
		protected override void WriteInlineTok(BinaryWriter writer, Instruction instr) {
			writer.Write(helper.GetToken(instr.Operand).Raw);
		}

		/// <inheritdoc/>
		protected override void WriteInlineType(BinaryWriter writer, Instruction instr) {
			writer.Write(helper.GetToken(instr.Operand).Raw);
		}
	}
}
