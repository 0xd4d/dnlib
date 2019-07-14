// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Returns tokens of token types, strings and signatures
	/// </summary>
	public interface ITokenProvider : IWriterError {
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
		readonly ITokenProvider helper;
		CilBody cilBody;
		bool keepMaxStack;
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
		public byte[] Code => code;

		/// <summary>
		/// Gets the extra sections (exception handlers) as a byte array or <c>null</c> if there
		/// are no exception handlers. This is valid only after calling <see cref="Write()"/>
		/// </summary>
		public byte[] ExtraSections => extraSections;

		/// <summary>
		/// Gets the token of the locals
		/// </summary>
		public uint LocalVarSigTok => localVarSigTok;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="helper">Helps this instance</param>
		/// <param name="cilBody">The CIL method body</param>
		public MethodBodyWriter(ITokenProvider helper, CilBody cilBody)
			: this(helper, cilBody, false) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="helper">Helps this instance</param>
		/// <param name="cilBody">The CIL method body</param>
		/// <param name="keepMaxStack">Keep the original max stack value that has been initialized
		/// in <paramref name="cilBody"/></param>
		public MethodBodyWriter(ITokenProvider helper, CilBody cilBody, bool keepMaxStack)
			: base(cilBody.Instructions, cilBody.ExceptionHandlers) {
			this.helper = helper;
			this.cilBody = cilBody;
			this.keepMaxStack = keepMaxStack;
		}

		internal MethodBodyWriter(ITokenProvider helper) => this.helper = helper;

		internal void Reset(CilBody cilBody, bool keepMaxStack) {
			Reset(cilBody.Instructions, cilBody.ExceptionHandlers);
			this.cilBody = cilBody;
			this.keepMaxStack = keepMaxStack;
			codeSize = 0;
			maxStack = 0;
			code = null;
			extraSections = null;
			localVarSigTok = 0;
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
			var bytes = new byte[code.Length + (extraSections is null ? 0 : padding + extraSections.Length)];
			Array.Copy(code, 0, bytes, 0, code.Length);
			if (!(extraSections is null))
				Array.Copy(extraSections, 0, bytes, code.Length + padding, extraSections.Length);
			return bytes;
		}

		bool NeedFatHeader() {
			//TODO: If locals has cust attrs or custom debug infos, we also need a fat header
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
			var writer = new ArrayWriter(code);
			writer.WriteUInt16(flags);
			writer.WriteUInt16((ushort)maxStack);
			writer.WriteUInt32(codeSize);
			writer.WriteUInt32(localVarSigTok = helper.GetToken(GetLocals(), cilBody.LocalVarSigTok).Raw);
			if (WriteInstructions(ref writer) != codeSize)
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
			var writer = new ArrayWriter(code);
			writer.WriteByte((byte)((codeSize << 2) | 2));
			if (WriteInstructions(ref writer) != codeSize)
				Error("Didn't write all code bytes");
		}

		void WriteExceptionHandlers() {
			if (NeedFatExceptionClauses())
				extraSections = WriteFatExceptionClauses();
			else
				extraSections = WriteSmallExceptionClauses();
		}

		bool NeedFatExceptionClauses() {
			// Size must fit in a byte, and since one small exception record is 12 bytes
			// and header is 4 bytes: x*12+4 <= 255 ==> x <= 20
			var exceptionHandlers = this.exceptionHandlers;
			if (exceptionHandlers.Count > 20)
				return true;

			for (int i = 0; i < exceptionHandlers.Count; i++) {
				var eh = exceptionHandlers[i];
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
			if (instr is null)
				return codeSize;
			return GetOffset(instr);
		}

		byte[] WriteFatExceptionClauses() {
			const int maxExceptionHandlers = (0x00FFFFFF - 4) / 24;
			var exceptionHandlers = this.exceptionHandlers;
			int numExceptionHandlers = exceptionHandlers.Count;
			if (numExceptionHandlers > maxExceptionHandlers) {
				Error("Too many exception handlers");
				numExceptionHandlers = maxExceptionHandlers;
			}

			var data = new byte[numExceptionHandlers * 24 + 4];
			var writer = new ArrayWriter(data);
			writer.WriteUInt32((((uint)numExceptionHandlers * 24 + 4) << 8) | 0x41);
			for (int i = 0; i < numExceptionHandlers; i++) {
				var eh = exceptionHandlers[i];
				uint offs1, offs2;

				writer.WriteUInt32((uint)eh.HandlerType);

				offs1 = GetOffset2(eh.TryStart);
				offs2 = GetOffset2(eh.TryEnd);
				if (offs2 <= offs1)
					Error("Exception handler: TryEnd <= TryStart");
				writer.WriteUInt32(offs1);
				writer.WriteUInt32(offs2 - offs1);

				offs1 = GetOffset2(eh.HandlerStart);
				offs2 = GetOffset2(eh.HandlerEnd);
				if (offs2 <= offs1)
					Error("Exception handler: HandlerEnd <= HandlerStart");
				writer.WriteUInt32(offs1);
				writer.WriteUInt32(offs2 - offs1);

				if (eh.HandlerType == ExceptionHandlerType.Catch)
					writer.WriteUInt32(helper.GetToken(eh.CatchType).Raw);
				else if (eh.HandlerType == ExceptionHandlerType.Filter)
					writer.WriteUInt32(GetOffset2(eh.FilterStart));
				else
					writer.WriteInt32(0);
			}

			if (writer.Position != data.Length)
				throw new InvalidOperationException();
			return data;
		}

		byte[] WriteSmallExceptionClauses() {
			const int maxExceptionHandlers = (0xFF - 4) / 12;
			var exceptionHandlers = this.exceptionHandlers;
			int numExceptionHandlers = exceptionHandlers.Count;
			if (numExceptionHandlers > maxExceptionHandlers) {
				Error("Too many exception handlers");
				numExceptionHandlers = maxExceptionHandlers;
			}

			var data = new byte[numExceptionHandlers * 12 + 4];
			var writer = new ArrayWriter(data);
			writer.WriteUInt32((((uint)numExceptionHandlers * 12 + 4) << 8) | 1);
			for (int i = 0; i < numExceptionHandlers; i++) {
				var eh = exceptionHandlers[i];
				uint offs1, offs2;

				writer.WriteUInt16((ushort)eh.HandlerType);

				offs1 = GetOffset2(eh.TryStart);
				offs2 = GetOffset2(eh.TryEnd);
				if (offs2 <= offs1)
					Error("Exception handler: TryEnd <= TryStart");
				writer.WriteUInt16((ushort)offs1);
				writer.WriteByte((byte)(offs2 - offs1));

				offs1 = GetOffset2(eh.HandlerStart);
				offs2 = GetOffset2(eh.HandlerEnd);
				if (offs2 <= offs1)
					Error("Exception handler: HandlerEnd <= HandlerStart");
				writer.WriteUInt16((ushort)offs1);
				writer.WriteByte((byte)(offs2 - offs1));

				if (eh.HandlerType == ExceptionHandlerType.Catch)
					writer.WriteUInt32(helper.GetToken(eh.CatchType).Raw);
				else if (eh.HandlerType == ExceptionHandlerType.Filter)
					writer.WriteUInt32(GetOffset2(eh.FilterStart));
				else
					writer.WriteInt32(0);
			}

			if (writer.Position != data.Length)
				throw new InvalidOperationException();
			return data;
		}

		/// <inheritdoc/>
		protected override void ErrorImpl(string message) => helper.Error(message);

		/// <inheritdoc/>
		protected override void WriteInlineField(ref ArrayWriter writer, Instruction instr) => writer.WriteUInt32(helper.GetToken(instr.Operand).Raw);

		/// <inheritdoc/>
		protected override void WriteInlineMethod(ref ArrayWriter writer, Instruction instr) => writer.WriteUInt32(helper.GetToken(instr.Operand).Raw);

		/// <inheritdoc/>
		protected override void WriteInlineSig(ref ArrayWriter writer, Instruction instr) => writer.WriteUInt32(helper.GetToken(instr.Operand).Raw);

		/// <inheritdoc/>
		protected override void WriteInlineString(ref ArrayWriter writer, Instruction instr) => writer.WriteUInt32(helper.GetToken(instr.Operand).Raw);

		/// <inheritdoc/>
		protected override void WriteInlineTok(ref ArrayWriter writer, Instruction instr) => writer.WriteUInt32(helper.GetToken(instr.Operand).Raw);

		/// <inheritdoc/>
		protected override void WriteInlineType(ref ArrayWriter writer, Instruction instr) => writer.WriteUInt32(helper.GetToken(instr.Operand).Raw);
	}
}
