using System;
using System.Collections.Generic;
using dot10.IO;
using dot10.DotNet.MD;

namespace dot10.DotNet.Emit {
	/// <summary>
	/// Reads a .NET method body (header, locals, instructions, exception handlers)
	/// </summary>
	sealed class MethodBodyReader : MethodBodyReaderBase {
		readonly ModuleDefMD module;
		ushort flags;
		ushort maxStack;
		uint codeSize;
		uint localVarSigTok;

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="reader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="module">The reader module</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="method">Use parameters from this method</param>
		public static CilBody Create(ModuleDefMD module, IBinaryReader reader, MethodDef method) {
			return Create(module, reader, method.Parameters);
		}

		/// <summary>
		/// Creates a CIL method body or returns an empty one if <paramref name="reader"/> doesn't
		/// point to the start of a valid CIL method body.
		/// </summary>
		/// <param name="module">The reader module</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="parameters">Method parameters</param>
		public static CilBody Create(ModuleDefMD module, IBinaryReader reader, IList<Parameter> parameters) {
			try {
				var mbReader = new MethodBodyReader(module, reader, parameters);
				mbReader.Read();
				return mbReader.CreateCilBody();
			}
			catch (InvalidMethodException) {
				return new CilBody();
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The reader module</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="method">Use parameters from this method</param>
		public MethodBodyReader(ModuleDefMD module, IBinaryReader reader, MethodDef method)
			: this(module, reader, method.Parameters) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The reader module</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="parameters">Method parameters</param>
		public MethodBodyReader(ModuleDefMD module, IBinaryReader reader, IList<Parameter> parameters)
			: base(reader, parameters) {
			this.module = module;
		}

		/// <summary>
		/// Reads the method body header, locals, all instructions, and the exception handlers (if any)
		/// </summary>
		/// <exception cref="InvalidMethodException">If it's an invalid method body. It's not thrown
		/// if an invalid instruction is found.</exception>
		/// <exception cref="OutOfMemoryException">If we can't allocate enough memory</exception>
		public void Read() {
			try {
				ReadHeader();
				SetLocals(ReadLocals());
				ReadInstructions();
				ReadExceptionHandlers();
			}
			catch (OutOfMemoryException) {
				throw;
			}
			catch (InvalidMethodException) {
				throw;
			}
			catch (Exception ex) {
				throw new InvalidMethodException("Could not read method", ex);
			}
		}

		/// <summary>
		/// Reads the method header
		/// </summary>
		/// <exception cref="InvalidMethodException">If the header is invalid</exception>
		void ReadHeader() {
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
				if ((flags >> 12) < 3)
					throw new InvalidMethodException("Header size in dwords < 3");
				maxStack = reader.ReadUInt16();
				codeSize = reader.ReadUInt32();
				localVarSigTok = reader.ReadUInt32();
				reader.Position += ((flags >> 12) - 3) * 4;
				break;

			default:
				throw new InvalidMethodException("It's not a tiny or a fat method header");
			}

			if (reader.Position + codeSize < reader.Position || reader.Position + codeSize > reader.Length)
				throw new InvalidMethodException("Invalid code size");
		}

		/// <summary>
		/// Reads the locals
		/// </summary>
		/// <returns>All locals or <c>null</c> if there are none</returns>
		IList<TypeSig> ReadLocals() {
			if (localVarSigTok == 0)
				return null;
			if (MDToken.ToTable(localVarSigTok) != Table.StandAloneSig)
				return null;
			var standAloneSig = module.ResolveStandAloneSig(MDToken.ToRID(localVarSigTok));
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
			return module.ResolveToken(reader.ReadUInt32()) as IField;
		}

		/// <inheritdoc/>
		protected override IMethod ReadInlineMethod(Instruction instr) {
			return module.ResolveToken(reader.ReadUInt32()) as IMethod;
		}

		/// <inheritdoc/>
		protected override MethodSig ReadInlineSig(Instruction instr) {
			uint token = reader.ReadUInt32();
			if (MDToken.ToTable(token) != Table.StandAloneSig)
				return null;
			var standAloneSig = module.ResolveStandAloneSig(MDToken.ToRID(token));
			if (standAloneSig == null)
				return null;
			return standAloneSig.MethodSig;
		}

		/// <inheritdoc/>
		protected override string ReadInlineString(Instruction instr) {
			return module.USStream.ReadNoNull(reader.ReadUInt32() & 0x00FFFFFF);
		}

		/// <inheritdoc/>
		protected override ITokenOperand ReadInlineTok(Instruction instr) {
			return module.ResolveToken(reader.ReadUInt32()) as ITokenOperand;
		}

		/// <inheritdoc/>
		protected override ITypeDefOrRef ReadInlineType(Instruction instr) {
			return module.ResolveToken(reader.ReadUInt32()) as ITypeDefOrRef;
		}

		/// <summary>
		/// Reads all exception handlers
		/// </summary>
		void ReadExceptionHandlers() {
			if ((flags & 8) == 0)
				return;
			reader.Position = (reader.Position + 3) & ~3;
			// Only read the first one. Any others aren't used.
			//TODO: Verify this
			byte b = reader.ReadByte();
			if ((b & 0x3F) != 1)
				return;	// Not exception handler clauses
			if ((b & 0x40) != 0)
				ReadFatExceptionHandlers();
			else
				ReadSmallExceptionHandlers();
		}

		void ReadFatExceptionHandlers() {
			reader.Position--;
			int num = (int)((reader.ReadUInt32() >> 8) / 24);
			for (int i = 0; i < num; i++) {
				var eh = new ExceptionHandler((ExceptionHandlerType)reader.ReadUInt32());
				uint offs = reader.ReadUInt32();
				eh.TryStart = GetInstruction(offs);
				eh.TryEnd = GetInstruction(offs + reader.ReadUInt32());
				offs = reader.ReadUInt32();
				eh.HandlerStart = GetInstruction(offs);
				eh.HandlerEnd = GetInstruction(offs + reader.ReadUInt32());
				if (eh.HandlerType == ExceptionHandlerType.Catch)
					eh.CatchType = module.ResolveToken(reader.ReadUInt32()) as ITypeDefOrRef;
				else if (eh.HandlerType == ExceptionHandlerType.Filter)
					eh.FilterStart = GetInstruction(reader.ReadUInt32());
				else
					reader.ReadUInt32();
				Add(eh);
			}
		}

		void ReadSmallExceptionHandlers() {
			int num = reader.ReadByte() / 12;
			reader.Position += 2;
			for (int i = 0; i < num; i++) {
				var eh = new ExceptionHandler((ExceptionHandlerType)reader.ReadUInt16());
				uint offs = reader.ReadUInt16();
				eh.TryStart = GetInstruction(offs);
				eh.TryEnd = GetInstruction(offs + reader.ReadByte());
				offs = reader.ReadUInt16();
				eh.HandlerStart = GetInstruction(offs);
				eh.HandlerEnd = GetInstruction(offs + reader.ReadByte());
				if (eh.HandlerType == ExceptionHandlerType.Catch)
					eh.CatchType = module.ResolveToken(reader.ReadUInt32()) as ITypeDefOrRef;
				else if (eh.HandlerType == ExceptionHandlerType.Filter)
					eh.FilterStart = GetInstruction(reader.ReadUInt32());
				else
					reader.ReadUInt32();
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
