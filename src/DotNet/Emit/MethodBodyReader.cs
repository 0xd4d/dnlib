/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.DotNet.MD;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Resolves instruction operands
	/// </summary>
	public interface IInstructionOperandResolver {
		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="token">The metadata token</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or <c>null</c> if <paramref name="token"/> is invalid</returns>
		IMDTokenProvider ResolveToken(uint token);

		/// <summary>
		/// Reads a string from the #US heap
		/// </summary>
		/// <param name="token">String token</param>
		/// <returns>A string</returns>
		string ReadUserString(uint token);
	}

	/// <summary>
	/// Reads a .NET method body (header, locals, instructions, exception handlers)
	/// </summary>
	public sealed class MethodBodyReader : MethodBodyReaderBase {
		readonly IInstructionOperandResolver opResolver;
		bool hasReadHeader;
		ushort flags;
		ushort maxStack;
		uint codeSize;
		uint localVarSigTok;
		IBinaryReader exceptionsReader;

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="reader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="method">Use parameters from this method</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, IBinaryReader reader, MethodDef method) {
			return CreateCilBody(opResolver, reader, method.Parameters);
		}

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="reader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="parameters">Method parameters</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, IBinaryReader reader, IList<Parameter> parameters) {
			return CreateCilBody(opResolver, reader, null, parameters);
		}

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="code"/> is not
		/// a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="code">All code</param>
		/// <param name="exceptions">Exceptions or <c>null</c> if all exception handlers are in
		/// <paramref name="code"/></param>
		/// <param name="parameters">Method parameters</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, byte[] code, byte[] exceptions, IList<Parameter> parameters) {
			return CreateCilBody(opResolver, MemoryImageStream.Create(code), exceptions == null ? null : MemoryImageStream.Create(exceptions), parameters);
		}

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="codeReader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="codeReader">A reader positioned at the start of a .NET method body</param>
		/// <param name="ehReader">Exception handler reader or <c>null</c> if exceptions aren't
		/// present or if <paramref name="codeReader"/> contains the exception handlers</param>
		/// <param name="parameters">Method parameters</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, IBinaryReader codeReader, IBinaryReader ehReader, IList<Parameter> parameters) {
			var mbReader = new MethodBodyReader(opResolver, codeReader, ehReader, parameters);
			if (!mbReader.Read())
				return new CilBody();
			return mbReader.CreateCilBody();
		}

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="code"/> is not
		/// a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="code">All code</param>
		/// <param name="exceptions">Exceptions or <c>null</c> if all exception handlers are in
		/// <paramref name="code"/></param>
		/// <param name="parameters">Method parameters</param>
		/// <param name="flags">Method header flags, eg. 2 if tiny method</param>
		/// <param name="maxStack">Max stack</param>
		/// <param name="codeSize">Code size</param>
		/// <param name="localVarSigTok">Local variable signature token or 0 if none</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, byte[] code, byte[] exceptions, IList<Parameter> parameters, ushort flags, ushort maxStack, uint codeSize, uint localVarSigTok) {
			var codeReader = MemoryImageStream.Create(code);
			var ehReader = exceptions == null ? null : MemoryImageStream.Create(exceptions);
			var mbReader = new MethodBodyReader(opResolver, codeReader, ehReader, parameters);
			mbReader.SetHeader(flags, maxStack, codeSize, localVarSigTok);
			if (!mbReader.Read())
				return new CilBody();
			return mbReader.CreateCilBody();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="method">Use parameters from this method</param>
		public MethodBodyReader(IInstructionOperandResolver opResolver, IBinaryReader reader, MethodDef method)
			: this(opResolver, reader, null, method.Parameters) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="parameters">Method parameters</param>
		public MethodBodyReader(IInstructionOperandResolver opResolver, IBinaryReader reader, IList<Parameter> parameters)
			: this(opResolver, reader, null, parameters) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="codeReader">A reader positioned at the start of a .NET method body</param>
		/// <param name="ehReader">Exception handler reader or <c>null</c> if exceptions aren't
		/// present or if <paramref name="codeReader"/> contains the exception handlers</param>
		/// <param name="parameters">Method parameters</param>
		public MethodBodyReader(IInstructionOperandResolver opResolver, IBinaryReader codeReader, IBinaryReader ehReader, IList<Parameter> parameters)
			: base(codeReader, parameters) {
			this.opResolver = opResolver;
			this.exceptionsReader = ehReader;
		}

		/// <summary>
		/// Initializes the method header
		/// </summary>
		/// <param name="flags">Header flags, eg. 2 if it's a tiny method</param>
		/// <param name="maxStack">Max stack</param>
		/// <param name="codeSize">Code size</param>
		/// <param name="localVarSigTok">Local variable signature token</param>
		void SetHeader(ushort flags, ushort maxStack, uint codeSize, uint localVarSigTok) {
			this.hasReadHeader = true;
			this.flags = flags;
			this.maxStack = maxStack;
			this.codeSize = codeSize;
			this.localVarSigTok = localVarSigTok;
		}

		/// <summary>
		/// Reads the method body header, locals, all instructions, and the exception handlers (if any)
		/// </summary>
		/// <returns><c>true</c> if it worked, and <c>false</c> if something failed</returns>
		public bool Read() {
			try {
				if (!ReadHeader())
					return false;
				SetLocals(ReadLocals());
				ReadInstructions();
				ReadExceptionHandlers();
				return true;
			}
			catch (InvalidMethodException) {
				return false;
			}
			catch (IOException) {
				return false;
			}
		}

		/// <summary>
		/// Reads the method header
		/// </summary>
		bool ReadHeader() {
			if (hasReadHeader)
				return true;
			hasReadHeader = true;

			byte b = reader.ReadByte();
			switch (b & 7) {
			case 2:
			case 6:
				// Tiny header. [7:2] = code size, max stack is 8, no locals or exception handlers
				flags = 2;
				maxStack = 8;
				codeSize = (uint)(b >> 2);
				localVarSigTok = 0;
				break;

			case 3:
				// Fat header. Can have locals and exception handlers
				flags = (ushort)((reader.ReadByte() << 8) | b);
				uint headerSize = (uint)flags >> 12;
				maxStack = reader.ReadUInt16();
				codeSize = reader.ReadUInt32();
				localVarSigTok = reader.ReadUInt32();

				// The CLR allows the code to start inside the method header. But if it does,
				// the CLR doesn't read any exceptions.
				reader.Position += -12 + headerSize * 4;
				if (headerSize < 3)
					flags &= 0xFFF7;
				break;

			default:
				return false;
			}

			if (reader.Position + codeSize < reader.Position || reader.Position + codeSize > reader.Length)
				return false;

			return true;
		}

		/// <summary>
		/// Reads the locals
		/// </summary>
		/// <returns>All locals or <c>null</c> if there are none</returns>
		IList<TypeSig> ReadLocals() {
			var standAloneSig = opResolver.ResolveToken(localVarSigTok) as StandAloneSig;
			if (standAloneSig == null)
				return null;
			var localSig = standAloneSig.LocalSig;
			if (localSig == null)
				return null;
			return localSig.Locals;
		}

		/// <summary>
		/// Reads all instructions
		/// </summary>
		void ReadInstructions() {
			ReadInstructionsNumBytes(codeSize);
		}

		/// <inheritdoc/>
		protected override IField ReadInlineField(Instruction instr) {
			return opResolver.ResolveToken(reader.ReadUInt32()) as IField;
		}

		/// <inheritdoc/>
		protected override IMethod ReadInlineMethod(Instruction instr) {
			return opResolver.ResolveToken(reader.ReadUInt32()) as IMethod;
		}

		/// <inheritdoc/>
		protected override MethodSig ReadInlineSig(Instruction instr) {
			var standAloneSig = opResolver.ResolveToken(reader.ReadUInt32()) as StandAloneSig;
			if (standAloneSig == null)
				return null;
			var sig = standAloneSig.MethodSig;
			if (sig != null)
				sig.OriginalToken = standAloneSig.MDToken.Raw;
			return sig;
		}

		/// <inheritdoc/>
		protected override string ReadInlineString(Instruction instr) {
			return opResolver.ReadUserString(reader.ReadUInt32()) ?? string.Empty;
		}

		/// <inheritdoc/>
		protected override ITokenOperand ReadInlineTok(Instruction instr) {
			return opResolver.ResolveToken(reader.ReadUInt32()) as ITokenOperand;
		}

		/// <inheritdoc/>
		protected override ITypeDefOrRef ReadInlineType(Instruction instr) {
			return opResolver.ResolveToken(reader.ReadUInt32()) as ITypeDefOrRef;
		}

		/// <summary>
		/// Reads all exception handlers
		/// </summary>
		void ReadExceptionHandlers() {
			if ((flags & 8) == 0)
				return;
			IBinaryReader ehReader;
			if (exceptionsReader != null)
				ehReader = exceptionsReader;
			else {
				ehReader = reader;
				ehReader.Position = (ehReader.Position + 3) & ~3;
			}
			// Only read the first one. Any others aren't used.
			byte b = ehReader.ReadByte();
			if ((b & 0x3F) != 1)
				return;	// Not exception handler clauses
			if ((b & 0x40) != 0)
				ReadFatExceptionHandlers(ehReader);
			else
				ReadSmallExceptionHandlers(ehReader);
		}

		static ushort GetNumberOfExceptionHandlers(uint num) {
			// The CLR truncates the count so num handlers is always <= FFFFh.
			return (ushort)num;
		}

		void ReadFatExceptionHandlers(IBinaryReader ehReader) {
			ehReader.Position--;
			int num = GetNumberOfExceptionHandlers((ehReader.ReadUInt32() >> 8) / 24);
			for (int i = 0; i < num; i++) {
				var eh = new ExceptionHandler((ExceptionHandlerType)ehReader.ReadUInt32());
				uint offs = ehReader.ReadUInt32();
				eh.TryStart = GetInstruction(offs);
				eh.TryEnd = GetInstruction(offs + ehReader.ReadUInt32());
				offs = ehReader.ReadUInt32();
				eh.HandlerStart = GetInstruction(offs);
				eh.HandlerEnd = GetInstruction(offs + ehReader.ReadUInt32());
				if (eh.HandlerType == ExceptionHandlerType.Catch)
					eh.CatchType = opResolver.ResolveToken(ehReader.ReadUInt32()) as ITypeDefOrRef;
				else if (eh.HandlerType == ExceptionHandlerType.Filter)
					eh.FilterStart = GetInstruction(ehReader.ReadUInt32());
				else
					ehReader.ReadUInt32();
				Add(eh);
			}
		}

		void ReadSmallExceptionHandlers(IBinaryReader ehReader) {
			int num = GetNumberOfExceptionHandlers((uint)ehReader.ReadByte() / 12);
			ehReader.Position += 2;
			for (int i = 0; i < num; i++) {
				var eh = new ExceptionHandler((ExceptionHandlerType)ehReader.ReadUInt16());
				uint offs = ehReader.ReadUInt16();
				eh.TryStart = GetInstruction(offs);
				eh.TryEnd = GetInstruction(offs + ehReader.ReadByte());
				offs = ehReader.ReadUInt16();
				eh.HandlerStart = GetInstruction(offs);
				eh.HandlerEnd = GetInstruction(offs + ehReader.ReadByte());
				if (eh.HandlerType == ExceptionHandlerType.Catch)
					eh.CatchType = opResolver.ResolveToken(ehReader.ReadUInt32()) as ITypeDefOrRef;
				else if (eh.HandlerType == ExceptionHandlerType.Filter)
					eh.FilterStart = GetInstruction(ehReader.ReadUInt32());
				else
					ehReader.ReadUInt32();
				Add(eh);
			}
		}

		/// <summary>
		/// Creates a CIL body. Must be called after <see cref="Read()"/>, and can only be
		/// called once.
		/// </summary>
		/// <returns>A new <see cref="CilBody"/> instance</returns>
		public CilBody CreateCilBody() {
			// Set init locals if it's a tiny method or if the init locals bit is set (fat header)
			bool initLocals = flags == 2 || (flags & 0x10) != 0;
			var cilBody = new CilBody(initLocals, instructions, exceptionHandlers, locals);
			cilBody.MaxStack = maxStack;
			cilBody.LocalVarSigTok = localVarSigTok;
			instructions = null;
			exceptionHandlers = null;
			locals = null;
			return cilBody;
		}
	}
}
