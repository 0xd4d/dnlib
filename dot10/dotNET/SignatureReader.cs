using System.Collections.Generic;
using dot10.IO;
using dot10.dotNET.MD;

namespace dot10.dotNET {
	/// <summary>
	/// Reads signatures from the #Blob stream
	/// </summary>
	class SignatureReader {
		ModuleDefMD readerModule;
		IImageStream reader;
		uint sigLen;

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
			if (!reader.ReadCompressedUInt32(out sigLen))
				return null;
			var callingConvention = (CallingConvention)reader.ReadByte();
			switch (callingConvention & CallingConvention.Mask) {
			case CallingConvention.Default:
			case CallingConvention.C:
			case CallingConvention.StdCall:
			case CallingConvention.ThisCall:
			case CallingConvention.FastCall:
			case CallingConvention.VarArg:
				return ReadMethod(callingConvention);

			case CallingConvention.Field:
				return ReadField(callingConvention);

			case CallingConvention.LocalSig:
				return ReadLocalSig(callingConvention);

			case CallingConvention.Property:
				return ReadProperty(callingConvention);

			case CallingConvention.GenericInst:
				return ReadGenericInstMethod(callingConvention);

			case CallingConvention.Unmanaged:
			case CallingConvention.NativeVarArg:
			default:
				return null;
			}
		}

		/// <summary>
		/// Reads a <see cref="FieldSig"/>
		/// </summary>
		/// <param name="callingConvention">First byte of signature</param>
		/// <returns>A new <see cref="FieldSig"/> instance</returns>
		FieldSig ReadField(CallingConvention callingConvention) {
			return new FieldSig(callingConvention, ReadType());
		}

		/// <summary>
		/// Reads a <see cref="MethodSig"/>
		/// </summary>
		/// <param name="callingConvention">First byte of signature</param>
		/// <returns>A new <see cref="MethodSig"/> instance</returns>
		MethodSig ReadMethod(CallingConvention callingConvention) {
			return ReadSig(new MethodSig(callingConvention));
		}

		/// <summary>
		/// Reads a <see cref="PropertySig"/>
		/// </summary>
		/// <param name="callingConvention">First byte of signature</param>
		/// <returns>A new <see cref="PropertySig"/> instance</returns>
		PropertySig ReadProperty(CallingConvention callingConvention) {
			return ReadSig(new PropertySig(callingConvention));
		}

		T ReadSig<T>(T methodSig) where T : MethodBaseSig {
			if (methodSig.Generic) {
				uint count;
				if (!reader.ReadCompressedUInt32(out count))
					return null;
				methodSig.GenParamCount = count;
			}

			var parameters = methodSig.Params;
			uint numParams;
			if (!reader.ReadCompressedUInt32(out numParams))
				return null;

			methodSig.RetType = ReadType();

			for (uint i = 0; i < numParams; i++) {
				var type = ReadType();
				if (type is SentinelSig && methodSig.ParamsAfterSentinel == null) {
					methodSig.ParamsAfterSentinel = parameters = new List<ITypeSig>((int)(numParams - i));
					i--;
				}
				else
					parameters.Add(type);
			}

			return methodSig;
		}

		/// <summary>
		/// Reads a <see cref="LocalSig"/>
		/// </summary>
		/// <param name="callingConvention">First byte of signature</param>
		/// <returns>A new <see cref="LocalSig"/> instance</returns>
		LocalSig ReadLocalSig(CallingConvention callingConvention) {
			uint count;
			if (!reader.ReadCompressedUInt32(out count))
				return null;
			var sig = new LocalSig(callingConvention, count);
			var locals = sig.Locals;
			for (uint i = 0; i < count; i++)
				locals.Add(ReadType());
			return sig;
		}

		/// <summary>
		/// Reads a <see cref="GenericInstMethodSig"/>
		/// </summary>
		/// <param name="callingConvention">First byte of signature</param>
		/// <returns>A new <see cref="GenericInstMethodSig"/> instance</returns>
		GenericInstMethodSig ReadGenericInstMethod(CallingConvention callingConvention) {
			uint count;
			if (!reader.ReadCompressedUInt32(out count))
				return null;
			var sig = new GenericInstMethodSig(callingConvention, count);
			var args = sig.GenericArguments;
			for (uint i = 0; i < count; i++)
				args.Add(ReadType());
			return sig;
		}

		/// <summary>
		/// Reads the next type
		/// </summary>
		/// <returns>A new <see cref="ITypeSig"/> instance</returns>
		ITypeSig ReadType() {
			uint num;
			switch ((ElementType)reader.ReadByte()) {
			case ElementType.Void: return readerModule.CorLibTypes.Void;
			case ElementType.Boolean: return readerModule.CorLibTypes.Boolean;
			case ElementType.Char: return readerModule.CorLibTypes.Char;
			case ElementType.I1: return readerModule.CorLibTypes.SByte;
			case ElementType.U1: return readerModule.CorLibTypes.Byte;
			case ElementType.I2: return readerModule.CorLibTypes.Int16;
			case ElementType.U2: return readerModule.CorLibTypes.UInt16;
			case ElementType.I4: return readerModule.CorLibTypes.Int32;
			case ElementType.U4: return readerModule.CorLibTypes.UInt32;
			case ElementType.I8: return readerModule.CorLibTypes.Int64;
			case ElementType.U8: return readerModule.CorLibTypes.UInt64;
			case ElementType.R4: return readerModule.CorLibTypes.Single;
			case ElementType.R8: return readerModule.CorLibTypes.Double;
			case ElementType.String: return readerModule.CorLibTypes.String;
			case ElementType.TypedByRef: return readerModule.CorLibTypes.TypedReference;
			case ElementType.I: return readerModule.CorLibTypes.IntPtr;
			case ElementType.U: return readerModule.CorLibTypes.UIntPtr;
			case ElementType.Object: return readerModule.CorLibTypes.Object;

			case ElementType.Ptr: return new PtrSig(ReadType());
			case ElementType.ByRef: return new ByRefSig(ReadType());
			case ElementType.ValueType: return new ValueTypeSig(ReadTypeDefOrRef());
			case ElementType.Class: return new ClassSig(ReadTypeDefOrRef());
			case ElementType.SZArray: return new SZArraySig(ReadType());
			case ElementType.CModReqd: return new CModReqdSig(ReadTypeDefOrRef(), ReadType());
			case ElementType.CModOpt: return new CModOptSig(ReadTypeDefOrRef(), ReadType());
			case ElementType.Sentinel: return new SentinelSig();
			case ElementType.Pinned: return new PinnedSig(ReadType());

			case ElementType.Var:
				if (!reader.ReadCompressedUInt32(out num))
					return null;
				return new GenericVar(num);

			case ElementType.MVar:
				if (!reader.ReadCompressedUInt32(out num))
					return null;
				return new GenericMVar(num);

			case ElementType.ValueArray:
				var nextType = ReadType();
				if (!reader.ReadCompressedUInt32(out num))
					return null;
				return new ValueArraySig(nextType, num);

			case ElementType.Module:
				if (!reader.ReadCompressedUInt32(out num))
					return null;
				return new ModuleSig(num, ReadType());

			case ElementType.Array:
			case ElementType.GenericInst:
			case ElementType.FnPtr:
			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
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
			//TODO: Perhaps we should read this lazily. If so, update ValueTypeSig, etc to take a coded token
			return readerModule.ResolveTypeDefOrRef(codedToken);
		}
	}
}
