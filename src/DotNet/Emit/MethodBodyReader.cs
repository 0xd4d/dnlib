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
		/// Constructor
		/// </summary>
		/// <param name="module">The reader module</param>
		/// <param name="reader">A reader positioned at the start of a .NET method body</param>
		/// <param name="method">Use parameters from this method</param>
		public MethodBodyReader(ModuleDefMD module, IBinaryReader reader, MethodDef method)
			: this(module, reader, method.Parameters.Parameters) {
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
		/// Reads the method body header, all instructions, and the exception handlers (if any)
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
				if ((flags >> 12) < 4)
					throw new InvalidMethodException("Header size in dwords < 4");
				maxStack = reader.ReadUInt16();
				codeSize = reader.ReadUInt32();
				localVarSigTok = reader.ReadUInt32();
				reader.Position += ((flags >> 12) - 4) * 4;
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
		/// <exception cref="InvalidMethodException">If <see cref="localVarSigTok"/> is invalid</exception>
		/// <returns>All locals or null if there are none</returns>
		IList<ITypeSig> ReadLocals() {
			if (localVarSigTok == 0)
				return null;
			if (MDToken.ToTable(localVarSigTok) != Table.StandAloneSig)
				throw new InvalidMethodException(string.Format("LocalVarSigTok ({0:X8}) is not a StandAloneSig token", localVarSigTok));
			var standAloneSig = module.ResolveStandAloneSig(MDToken.ToRID(localVarSigTok));
			if (standAloneSig == null)
				throw new InvalidMethodException(string.Format("LocalVarSigTok ({0:X8}) has an invalid RID", localVarSigTok));
			var localSig = standAloneSig.LocalSig;
			if (localSig == null)
				throw new InvalidMethodException(string.Format("LocalVarSigTok ({0:X8}) isn't a locals sig", localVarSigTok));
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
			return module.USStream.Read(reader.ReadUInt32() & 0x00FFFFFF);
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
			//TODO:
		}
	}
}
