// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using dnlib.IO;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Reads strings from #US heap
	/// </summary>
	public interface IStringResolver {
		/// <summary>
		/// Reads a string from the #US heap
		/// </summary>
		/// <param name="token">String token</param>
		/// <returns>A string</returns>
		string ReadUserString(uint token);
	}

	/// <summary>
	/// Resolves instruction operands
	/// </summary>
	public interface IInstructionOperandResolver : ITokenResolver, IStringResolver {
	}

	public static partial class Extensions {
		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="self">An <see cref="IInstructionOperandResolver"/> object</param>
		/// <param name="token">The metadata token</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or <c>null</c> if <paramref name="token"/> is invalid</returns>
		public static IMDTokenProvider ResolveToken(this IInstructionOperandResolver self, uint token) =>
			self.ResolveToken(token, new GenericParamContext());
	}

	/// <summary>
	/// Reads a .NET method body (header, locals, instructions, exception handlers)
	/// </summary>
	public sealed class MethodBodyReader : MethodBodyReaderBase {
		readonly IInstructionOperandResolver opResolver;
		bool hasReadHeader;
		byte headerSize;
		ushort flags;
		ushort maxStack;
		uint codeSize;
		uint localVarSigTok;
		uint startOfHeader;
		uint totalBodySize;
		DataReader? exceptionsReader;
		readonly GenericParamContext gpContext;

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="reader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="method">Use parameters from this method</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, DataReader reader, MethodDef method) =>
			CreateCilBody(opResolver, reader, null, method.Parameters, new GenericParamContext());

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="reader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="method">Use parameters from this method</param>
		/// <param name="gpContext">Generic parameter context</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, DataReader reader, MethodDef method, GenericParamContext gpContext) =>
			CreateCilBody(opResolver, reader, null, method.Parameters, gpContext);

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="reader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="parameters">Method parameters</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, DataReader reader, IList<Parameter> parameters) =>
			CreateCilBody(opResolver, reader, null, parameters, new GenericParamContext());

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="reader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="parameters">Method parameters</param>
		/// <param name="gpContext">Generic parameter context</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, DataReader reader, IList<Parameter> parameters, GenericParamContext gpContext) =>
			CreateCilBody(opResolver, reader, null, parameters, gpContext);

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="reader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="parameters">Method parameters</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <param name="context">The module context</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, DataReader reader, IList<Parameter> parameters, GenericParamContext gpContext, ModuleContext context) =>
			CreateCilBody(opResolver, reader, null, parameters, gpContext, context);

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="code"/> is not
		/// a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="code">All code</param>
		/// <param name="exceptions">Exceptions or <c>null</c> if all exception handlers are in
		/// <paramref name="code"/></param>
		/// <param name="parameters">Method parameters</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, byte[] code, byte[] exceptions, IList<Parameter> parameters) =>
			CreateCilBody(opResolver, ByteArrayDataReaderFactory.CreateReader(code), exceptions is null ? (DataReader?)null : ByteArrayDataReaderFactory.CreateReader(exceptions), parameters, new GenericParamContext());

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="code"/> is not
		/// a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="code">All code</param>
		/// <param name="exceptions">Exceptions or <c>null</c> if all exception handlers are in
		/// <paramref name="code"/></param>
		/// <param name="parameters">Method parameters</param>
		/// <param name="gpContext">Generic parameter context</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, byte[] code, byte[] exceptions, IList<Parameter> parameters, GenericParamContext gpContext) =>
			CreateCilBody(opResolver, ByteArrayDataReaderFactory.CreateReader(code), exceptions is null ? (DataReader?)null : ByteArrayDataReaderFactory.CreateReader(exceptions), parameters, gpContext);

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="codeReader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="codeReader">A reader positioned at the start of a .NET method body</param>
		/// <param name="ehReader">Exception handler reader or <c>null</c> if exceptions aren't
		/// present or if <paramref name="codeReader"/> contains the exception handlers</param>
		/// <param name="parameters">Method parameters</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, DataReader codeReader, DataReader? ehReader, IList<Parameter> parameters) =>
			CreateCilBody(opResolver, codeReader, ehReader, parameters, new GenericParamContext());

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="codeReader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="codeReader">A reader positioned at the start of a .NET method body</param>
		/// <param name="ehReader">Exception handler reader or <c>null</c> if exceptions aren't
		/// present or if <paramref name="codeReader"/> contains the exception handlers</param>
		/// <param name="parameters">Method parameters</param>
		/// <param name="gpContext">Generic parameter context</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, DataReader codeReader, DataReader? ehReader, IList<Parameter> parameters, GenericParamContext gpContext) =>
			CreateCilBody(opResolver, codeReader, ehReader, parameters, gpContext, null);

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="codeReader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="codeReader">A reader positioned at the start of a .NET method body</param>
		/// <param name="ehReader">Exception handler reader or <c>null</c> if exceptions aren't
		/// present or if <paramref name="codeReader"/> contains the exception handlers</param>
		/// <param name="parameters">Method parameters</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <param name="context">The module context</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, DataReader codeReader, DataReader? ehReader, IList<Parameter> parameters, GenericParamContext gpContext, ModuleContext context) {
			var mbReader = new MethodBodyReader(opResolver, codeReader, ehReader, parameters, gpContext, context);
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
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, byte[] code, byte[] exceptions, IList<Parameter> parameters, ushort flags, ushort maxStack, uint codeSize, uint localVarSigTok) =>
			CreateCilBody(opResolver, code, exceptions, parameters, flags, maxStack, codeSize, localVarSigTok, new GenericParamContext());

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
		/// <param name="gpContext">Generic parameter context</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, byte[] code, byte[] exceptions, IList<Parameter> parameters, ushort flags, ushort maxStack, uint codeSize, uint localVarSigTok, GenericParamContext gpContext) =>
			CreateCilBody(opResolver, code, exceptions, parameters, flags, maxStack, codeSize, localVarSigTok, gpContext, null);

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
		/// <param name="gpContext">Generic parameter context</param>
		/// <param name="context">The module context</param>
		public static CilBody CreateCilBody(IInstructionOperandResolver opResolver, byte[] code, byte[] exceptions, IList<Parameter> parameters, ushort flags, ushort maxStack, uint codeSize, uint localVarSigTok, GenericParamContext gpContext, ModuleContext context) {
			var codeReader = ByteArrayDataReaderFactory.CreateReader(code);
			var ehReader = exceptions is null ? (DataReader?)null : ByteArrayDataReaderFactory.CreateReader(exceptions);
			var mbReader = new MethodBodyReader(opResolver, codeReader, ehReader, parameters, gpContext, context);
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
		public MethodBodyReader(IInstructionOperandResolver opResolver, DataReader reader, MethodDef method)
			: this(opResolver, reader, null, method.Parameters, new GenericParamContext()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="method">Use parameters from this method</param>
		/// <param name="gpContext">Generic parameter context</param>
		public MethodBodyReader(IInstructionOperandResolver opResolver, DataReader reader, MethodDef method, GenericParamContext gpContext)
			: this(opResolver, reader, null, method.Parameters, gpContext) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="parameters">Method parameters</param>
		public MethodBodyReader(IInstructionOperandResolver opResolver, DataReader reader, IList<Parameter> parameters)
			: this(opResolver, reader, null, parameters, new GenericParamContext()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="parameters">Method parameters</param>
		/// <param name="gpContext">Generic parameter context</param>
		public MethodBodyReader(IInstructionOperandResolver opResolver, DataReader reader, IList<Parameter> parameters, GenericParamContext gpContext)
			: this(opResolver, reader, null, parameters, gpContext) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="codeReader">A reader positioned at the start of a .NET method body</param>
		/// <param name="ehReader">Exception handler reader or <c>null</c> if exceptions aren't
		/// present or if <paramref name="codeReader"/> contains the exception handlers</param>
		/// <param name="parameters">Method parameters</param>
		public MethodBodyReader(IInstructionOperandResolver opResolver, DataReader codeReader, DataReader? ehReader, IList<Parameter> parameters)
			: this(opResolver, codeReader, ehReader, parameters, new GenericParamContext()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="codeReader">A reader positioned at the start of a .NET method body</param>
		/// <param name="ehReader">Exception handler reader or <c>null</c> if exceptions aren't
		/// present or if <paramref name="codeReader"/> contains the exception handlers</param>
		/// <param name="parameters">Method parameters</param>
		/// <param name="gpContext">Generic parameter context</param>
		public MethodBodyReader(IInstructionOperandResolver opResolver, DataReader codeReader, DataReader? ehReader, IList<Parameter> parameters, GenericParamContext gpContext)
			: this(opResolver, codeReader, ehReader, parameters, gpContext, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="opResolver">The operand resolver</param>
		/// <param name="codeReader">A reader positioned at the start of a .NET method body</param>
		/// <param name="ehReader">Exception handler reader or <c>null</c> if exceptions aren't
		/// present or if <paramref name="codeReader"/> contains the exception handlers</param>
		/// <param name="parameters">Method parameters</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <param name="context">Module context</param>
		public MethodBodyReader(IInstructionOperandResolver opResolver, DataReader codeReader, DataReader? ehReader, IList<Parameter> parameters, GenericParamContext gpContext, ModuleContext context)
			: base(codeReader, parameters, context) {
			this.opResolver = opResolver;
			exceptionsReader = ehReader;
			this.gpContext = gpContext;
			startOfHeader = uint.MaxValue;
		}

		/// <summary>
		/// Initializes the method header
		/// </summary>
		/// <param name="flags">Header flags, eg. 2 if it's a tiny method</param>
		/// <param name="maxStack">Max stack</param>
		/// <param name="codeSize">Code size</param>
		/// <param name="localVarSigTok">Local variable signature token</param>
		void SetHeader(ushort flags, ushort maxStack, uint codeSize, uint localVarSigTok) {
			hasReadHeader = true;
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
				ReadExceptionHandlers(out totalBodySize);
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

			startOfHeader = reader.Position;
			byte b = reader.ReadByte();
			switch (b & 7) {
			case 2:
			case 6:
				// Tiny header. [7:2] = code size, max stack is 8, no locals or exception handlers
				flags = 2;
				maxStack = 8;
				codeSize = (uint)(b >> 2);
				localVarSigTok = 0;
				headerSize = 1;
				break;

			case 3:
				// Fat header. Can have locals and exception handlers
				flags = (ushort)((reader.ReadByte() << 8) | b);
				headerSize = (byte)(flags >> 12);
				maxStack = reader.ReadUInt16();
				codeSize = reader.ReadUInt32();
				localVarSigTok = reader.ReadUInt32();

				// The CLR allows the code to start inside the method header. But if it does,
				// the CLR doesn't read any exceptions.
				reader.Position = reader.Position - 12 + headerSize * 4U;
				if (headerSize < 3)
					flags &= 0xFFF7;
				headerSize *= 4;
				break;

			default:
				return false;
			}

			if ((ulong)reader.Position + codeSize > reader.Length)
				return false;

			return true;
		}

		/// <summary>
		/// Reads the locals
		/// </summary>
		/// <returns>All locals or <c>null</c> if there are none</returns>
		IList<TypeSig> ReadLocals() {
			var standAloneSig = opResolver.ResolveToken(localVarSigTok, gpContext) as StandAloneSig;
			if (standAloneSig is null)
				return null;
			var localSig = standAloneSig.LocalSig;
			if (localSig is null)
				return null;
			return localSig.Locals;
		}

		/// <summary>
		/// Reads all instructions
		/// </summary>
		void ReadInstructions() => ReadInstructionsNumBytes(codeSize);

		/// <inheritdoc/>
		protected override IField ReadInlineField(Instruction instr) => opResolver.ResolveToken(reader.ReadUInt32(), gpContext) as IField;

		/// <inheritdoc/>
		protected override IMethod ReadInlineMethod(Instruction instr) => opResolver.ResolveToken(reader.ReadUInt32(), gpContext) as IMethod;

		/// <inheritdoc/>
		protected override MethodSig ReadInlineSig(Instruction instr) {
			var standAloneSig = opResolver.ResolveToken(reader.ReadUInt32(), gpContext) as StandAloneSig;
			if (standAloneSig is null)
				return null;
			var sig = standAloneSig.MethodSig;
			if (sig is not null)
				sig.OriginalToken = standAloneSig.MDToken.Raw;
			return sig;
		}

		/// <inheritdoc/>
		protected override string ReadInlineString(Instruction instr) => opResolver.ReadUserString(reader.ReadUInt32()) ?? string.Empty;

		/// <inheritdoc/>
		protected override ITokenOperand ReadInlineTok(Instruction instr) => opResolver.ResolveToken(reader.ReadUInt32(), gpContext) as ITokenOperand;

		/// <inheritdoc/>
		protected override ITypeDefOrRef ReadInlineType(Instruction instr) => opResolver.ResolveToken(reader.ReadUInt32(), gpContext) as ITypeDefOrRef;

		/// <summary>
		/// Reads all exception handlers
		/// </summary>
		void ReadExceptionHandlers(out uint totalBodySize) {
			if ((flags & 8) == 0) {
				totalBodySize = startOfHeader == uint.MaxValue ? 0 : reader.Position - startOfHeader;
				return;
			}
			bool canSaveTotalBodySize;
			DataReader ehReader;
			if (exceptionsReader is not null) {
				canSaveTotalBodySize = false;
				ehReader = exceptionsReader.Value;
			}
			else {
				canSaveTotalBodySize = true;
				ehReader = reader;
				ehReader.Position = (ehReader.Position + 3) & ~3U;
			}
			// Only read the first one. Any others aren't used.
			byte b = ehReader.ReadByte();
			if ((b & 0x3F) != 1) {
				totalBodySize = startOfHeader == uint.MaxValue ? 0 : reader.Position - startOfHeader;
				return;	// Not exception handler clauses
			}
			if ((b & 0x40) != 0)
				ReadFatExceptionHandlers(ref ehReader);
			else
				ReadSmallExceptionHandlers(ref ehReader);
			if (canSaveTotalBodySize)
				totalBodySize = startOfHeader == uint.MaxValue ? 0 : ehReader.Position - startOfHeader;
			else
				totalBodySize = 0;
		}

		void ReadFatExceptionHandlers(ref DataReader ehReader) {
			ehReader.Position--;
			int num = (int)((ehReader.ReadUInt32() >> 8) / 24);
			for (int i = 0; i < num; i++) {
				var eh = new ExceptionHandler((ExceptionHandlerType)ehReader.ReadUInt32());
				uint offs = ehReader.ReadUInt32();
				eh.TryStart = GetInstruction(offs);
				eh.TryEnd = GetInstruction(offs + ehReader.ReadUInt32());
				offs = ehReader.ReadUInt32();
				eh.HandlerStart = GetInstruction(offs);
				eh.HandlerEnd = GetInstruction(offs + ehReader.ReadUInt32());
				if (eh.IsCatch)
					eh.CatchType = opResolver.ResolveToken(ehReader.ReadUInt32(), gpContext) as ITypeDefOrRef;
				else if (eh.IsFilter)
					eh.FilterStart = GetInstruction(ehReader.ReadUInt32());
				else
					ehReader.ReadUInt32();
				Add(eh);
			}
		}

		void ReadSmallExceptionHandlers(ref DataReader ehReader) {
			int num = (int)((uint)ehReader.ReadByte() / 12);
			ehReader.Position += 2;
			for (int i = 0; i < num; i++) {
				var eh = new ExceptionHandler((ExceptionHandlerType)ehReader.ReadUInt16());
				uint offs = ehReader.ReadUInt16();
				eh.TryStart = GetInstruction(offs);
				eh.TryEnd = GetInstruction(offs + ehReader.ReadByte());
				offs = ehReader.ReadUInt16();
				eh.HandlerStart = GetInstruction(offs);
				eh.HandlerEnd = GetInstruction(offs + ehReader.ReadByte());
				if (eh.IsCatch)
					eh.CatchType = opResolver.ResolveToken(ehReader.ReadUInt32(), gpContext) as ITypeDefOrRef;
				else if (eh.IsFilter)
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
			cilBody.HeaderSize = headerSize;
			cilBody.MaxStack = maxStack;
			cilBody.LocalVarSigTok = localVarSigTok;
			cilBody.MetadataBodySize = totalBodySize;
			instructions = null;
			exceptionHandlers = null;
			locals = null;
			return cilBody;
		}
	}
}
