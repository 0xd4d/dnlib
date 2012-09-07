using dot10.IO;
using dot10.dotNET.MD;

namespace dot10.dotNET.Hi {
	/// <summary>
	/// Reads signatures from the #Blob stream
	/// </summary>
	class SignatureReader {
		ModuleDefMD readerModule;
		IImageStream reader;

		/// <summary>
		/// Reads a signature from the #Blob stream
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <returns>A new <see cref="ISignature"/> instance</returns>
		public static ISignature Read(ModuleDefMD readerModule, uint sig) {
			try {
				return new SignatureReader(readerModule, sig).Read();
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="sig">#Blob stream offset of signature</param>
		SignatureReader(ModuleDefMD readerModule, uint sig) {
			this.readerModule = readerModule;
			this.reader = readerModule.BlobStream.ImageStream;
			this.reader.Position = sig;
		}

		/// <summary>
		/// Reads the signature
		/// </summary>
		/// <returns>A new <see cref="ISignature"/> instance</returns>
		ISignature Read() {
			var callingConvention = (CallingConvention)reader.ReadByte();
			switch (callingConvention & CallingConvention.Mask) {
			case CallingConvention.Default:
			case CallingConvention.C:
			case CallingConvention.StdCall:
			case CallingConvention.ThisCall:
			case CallingConvention.FastCall:
			case CallingConvention.VarArg:
			case CallingConvention.LocalSig:
			case CallingConvention.Property:
			case CallingConvention.Unmanaged:
			case CallingConvention.GenericInst:
			case CallingConvention.NativeVarArg:
			default:
				return null;

			case CallingConvention.Field:
				return ReadField(callingConvention);
			}
		}

		/// <summary>
		/// Reads a <see cref="FieldSig"/>
		/// </summary>
		/// <param name="callingConvention">First byte of signature</param>
		/// <returns>A new <see cref="FieldSig"/> instance</returns>
		FieldSig ReadField(CallingConvention callingConvention) {
			var type = ReadType();
			return null;
		}

		/// <summary>
		/// Reads the next type
		/// </summary>
		/// <returns>A new <see cref="ITypeSig"/> instance</returns>
		ITypeSig ReadType() {
			uint num;
			switch ((ElementType)reader.ReadByte()) {
			case ElementType.Void: return readerModule.Void;
			case ElementType.Boolean: return readerModule.Boolean;
			case ElementType.Char: return readerModule.Char;
			case ElementType.I1: return readerModule.SByte;
			case ElementType.U1: return readerModule.Byte;
			case ElementType.I2: return readerModule.Int16;
			case ElementType.U2: return readerModule.UInt16;
			case ElementType.I4: return readerModule.Int32;
			case ElementType.U4: return readerModule.UInt32;
			case ElementType.I8: return readerModule.Int64;
			case ElementType.U8: return readerModule.UInt64;
			case ElementType.R4: return readerModule.Single;
			case ElementType.R8: return readerModule.Double;
			case ElementType.String: return readerModule.String;
			case ElementType.TypedByRef: return readerModule.TypedReference;
			case ElementType.I: return readerModule.IntPtr;
			case ElementType.U: return readerModule.UIntPtr;
			case ElementType.Object: return readerModule.Object;

			case ElementType.Ptr: return new PtrSig(ReadType());
			case ElementType.ByRef: return new ByRefSig(ReadType());
			case ElementType.ValueType: return new ValueTypeSig(ReadType());
			case ElementType.Class: return new ClassSig(ReadType());
			case ElementType.SZArray: return new SZArraySig(ReadType());
			case ElementType.CModReqd: return new CModReqdSig(ReadTypeDefOrRef(), ReadType());
			case ElementType.CModOpt: return new CModOptSig(ReadTypeDefOrRef(), ReadType());
			case ElementType.Sentinel: return new SentinelSig(ReadType());
			case ElementType.Pinned: return new PinnedSig(ReadType());

			case ElementType.Var:
				if (!reader.ReadCompressedUInt32(out num))
					return null;
				return new VarSig(num);

			case ElementType.MVar:
				if (!reader.ReadCompressedUInt32(out num))
					return null;
				return new MVarSig(num);

			case ElementType.Array:
			case ElementType.GenericInst:
			case ElementType.FnPtr:
			case ElementType.End:
			case ElementType.Internal:
			case ElementType.Modifier:
			default:
				return null;
			}
		}

		/// <summary>
		/// Reads a <c>TypeDefOrRef</c>
		/// </summary>
		/// <returns>A <see cref="ITypeDefOrRef"/> instance</returns>
		ITypeDefOrRef ReadTypeDefOrRef() {
			uint codedToken;
			if (!reader.ReadCompressedUInt32(out codedToken))
				return null;
			return readerModule.ResolveTypeDefOrRef(codedToken);
		}
	}
}
