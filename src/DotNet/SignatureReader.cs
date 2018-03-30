// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;

namespace dnlib.DotNet {
	/// <summary>
	/// Helps <see cref="SignatureReader"/> resolve types
	/// </summary>
	public interface ISignatureReaderHelper {
		/// <summary>
		/// Resolves a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="codedToken">A <c>TypeDefOrRef</c> coded token</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="ITypeDefOrRef"/> or <c>null</c> if <paramref name="codedToken"/>
		/// is invalid</returns>
		ITypeDefOrRef ResolveTypeDefOrRef(uint codedToken, GenericParamContext gpContext);

		/// <summary>
		/// Converts the address of a <see cref="Type"/> to a <see cref="TypeSig"/>
		/// </summary>
		/// <seealso cref="Emit.MethodTableToTypeConverter"/>
		/// <param name="address">Address of <see cref="Type"/>. This is also known as the
		/// method table and has the same value as <see cref="RuntimeTypeHandle.Value"/></param>
		/// <returns>A <see cref="TypeSig"/> or <c>null</c> if not supported</returns>
		TypeSig ConvertRTInternalAddress(IntPtr address);
	}

	/// <summary>
	/// Reads signatures from the #Blob stream
	/// </summary>
	public struct SignatureReader {
		readonly ISignatureReaderHelper helper;
		readonly ICorLibTypes corLibTypes;
		DataReader reader;
		readonly GenericParamContext gpContext;
		RecursionCounter recursionCounter;

		/// <summary>
		/// Reads a signature from the #Blob stream
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public static CallingConventionSig ReadSig(ModuleDefMD readerModule, uint sig) =>
			ReadSig(readerModule, sig, new GenericParamContext());

		/// <summary>
		/// Reads a signature from the #Blob stream
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public static CallingConventionSig ReadSig(ModuleDefMD readerModule, uint sig, GenericParamContext gpContext) {
			try {
				var reader = new SignatureReader(readerModule, sig, gpContext);
				if (reader.reader.Length == 0)
					return null;
				var csig = reader.ReadSig();
				if (csig != null)
					csig.ExtraData = reader.GetExtraData();
				return csig;
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Reads a <see cref="CallingConventionSig"/> signature
		/// </summary>
		/// <param name="module">The module where the signature is located in</param>
		/// <param name="signature">The signature data</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static CallingConventionSig ReadSig(ModuleDefMD module, byte[] signature) =>
			ReadSig(module, module.CorLibTypes, ByteArrayDataReaderFactory.CreateReader(signature), new GenericParamContext());

		/// <summary>
		/// Reads a <see cref="CallingConventionSig"/> signature
		/// </summary>
		/// <param name="module">The module where the signature is located in</param>
		/// <param name="signature">The signature data</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static CallingConventionSig ReadSig(ModuleDefMD module, byte[] signature, GenericParamContext gpContext) =>
			ReadSig(module, module.CorLibTypes, ByteArrayDataReaderFactory.CreateReader(signature), gpContext);

		/// <summary>
		/// Reads a <see cref="CallingConventionSig"/> signature
		/// </summary>
		/// <param name="module">The module where the signature is located in</param>
		/// <param name="signature">The signature reader</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static CallingConventionSig ReadSig(ModuleDefMD module, DataReader signature) =>
			ReadSig(module, module.CorLibTypes, signature, new GenericParamContext());

		/// <summary>
		/// Reads a <see cref="CallingConventionSig"/> signature
		/// </summary>
		/// <param name="module">The module where the signature is located in</param>
		/// <param name="signature">The signature reader</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static CallingConventionSig ReadSig(ModuleDefMD module, DataReader signature, GenericParamContext gpContext) =>
			ReadSig(module, module.CorLibTypes, signature, gpContext);

		/// <summary>
		/// Reads a <see cref="CallingConventionSig"/> signature
		/// </summary>
		/// <param name="helper">Token resolver</param>
		/// <param name="corLibTypes">A <see cref="ICorLibTypes"/> instance</param>
		/// <param name="signature">The signature data</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static CallingConventionSig ReadSig(ISignatureReaderHelper helper, ICorLibTypes corLibTypes, byte[] signature) =>
			ReadSig(helper, corLibTypes, ByteArrayDataReaderFactory.CreateReader(signature), new GenericParamContext());

		/// <summary>
		/// Reads a <see cref="CallingConventionSig"/> signature
		/// </summary>
		/// <param name="helper">Token resolver</param>
		/// <param name="corLibTypes">A <see cref="ICorLibTypes"/> instance</param>
		/// <param name="signature">The signature data</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static CallingConventionSig ReadSig(ISignatureReaderHelper helper, ICorLibTypes corLibTypes, byte[] signature, GenericParamContext gpContext) =>
			ReadSig(helper, corLibTypes, ByteArrayDataReaderFactory.CreateReader(signature), gpContext);

		/// <summary>
		/// Reads a <see cref="CallingConventionSig"/> signature
		/// </summary>
		/// <param name="helper">Token resolver</param>
		/// <param name="corLibTypes">A <see cref="ICorLibTypes"/> instance</param>
		/// <param name="signature">The signature reader</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static CallingConventionSig ReadSig(ISignatureReaderHelper helper, ICorLibTypes corLibTypes, DataReader signature) =>
			ReadSig(helper, corLibTypes, signature, new GenericParamContext());

		/// <summary>
		/// Reads a <see cref="CallingConventionSig"/> signature
		/// </summary>
		/// <param name="helper">Token resolver</param>
		/// <param name="corLibTypes">A <see cref="ICorLibTypes"/> instance</param>
		/// <param name="signature">The signature reader</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static CallingConventionSig ReadSig(ISignatureReaderHelper helper, ICorLibTypes corLibTypes, DataReader signature, GenericParamContext gpContext) {
			try {
				var reader = new SignatureReader(helper, corLibTypes, ref signature, gpContext);
				if (reader.reader.Length == 0)
					return null;
				return reader.ReadSig();
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Reads a type signature from the #Blob stream
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ModuleDefMD readerModule, uint sig) =>
			ReadTypeSig(readerModule, sig, new GenericParamContext());

		/// <summary>
		/// Reads a type signature from the #Blob stream
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ModuleDefMD readerModule, uint sig, GenericParamContext gpContext) {
			try {
				var reader = new SignatureReader(readerModule, sig, gpContext);
				return reader.ReadType();
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Reads a type signature from the #Blob stream
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <param name="extraData">If there's any extra data after the signature, it's saved
		/// here, else this will be <c>null</c></param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ModuleDefMD readerModule, uint sig, out byte[] extraData) =>
			ReadTypeSig(readerModule, sig, new GenericParamContext(), out extraData);

		/// <summary>
		/// Reads a type signature from the #Blob stream
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <param name="extraData">If there's any extra data after the signature, it's saved
		/// here, else this will be <c>null</c></param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ModuleDefMD readerModule, uint sig, GenericParamContext gpContext, out byte[] extraData) {
			try {
				var reader = new SignatureReader(readerModule, sig, gpContext);
				TypeSig ts;
				try {
					ts = reader.ReadType();
				}
				catch (IOException) {
					reader.reader.Position = 0;
					ts = null;
				}
				extraData = reader.GetExtraData();
				return ts;
			}
			catch {
				extraData = null;
				return null;
			}
		}

		/// <summary>
		/// Reads a <see cref="TypeSig"/> signature
		/// </summary>
		/// <param name="module">The module where the signature is located in</param>
		/// <param name="signature">The signature data</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ModuleDefMD module, byte[] signature) =>
			ReadTypeSig(module, module.CorLibTypes, ByteArrayDataReaderFactory.CreateReader(signature), new GenericParamContext());

		/// <summary>
		/// Reads a <see cref="TypeSig"/> signature
		/// </summary>
		/// <param name="module">The module where the signature is located in</param>
		/// <param name="signature">The signature data</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ModuleDefMD module, byte[] signature, GenericParamContext gpContext) =>
			ReadTypeSig(module, module.CorLibTypes, ByteArrayDataReaderFactory.CreateReader(signature), gpContext);

		/// <summary>
		/// Reads a <see cref="TypeSig"/> signature
		/// </summary>
		/// <param name="module">The module where the signature is located in</param>
		/// <param name="signature">The signature reader</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ModuleDefMD module, DataReader signature) =>
			ReadTypeSig(module, module.CorLibTypes, signature, new GenericParamContext());

		/// <summary>
		/// Reads a <see cref="TypeSig"/> signature
		/// </summary>
		/// <param name="module">The module where the signature is located in</param>
		/// <param name="signature">The signature reader</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ModuleDefMD module, DataReader signature, GenericParamContext gpContext) =>
			ReadTypeSig(module, module.CorLibTypes, signature, gpContext);

		/// <summary>
		/// Reads a <see cref="TypeSig"/> signature
		/// </summary>
		/// <param name="helper">Token resolver</param>
		/// <param name="corLibTypes">A <see cref="ICorLibTypes"/> instance</param>
		/// <param name="signature">The signature data</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ISignatureReaderHelper helper, ICorLibTypes corLibTypes, byte[] signature) =>
			ReadTypeSig(helper, corLibTypes, ByteArrayDataReaderFactory.CreateReader(signature), new GenericParamContext());

		/// <summary>
		/// Reads a <see cref="TypeSig"/> signature
		/// </summary>
		/// <param name="helper">Token resolver</param>
		/// <param name="corLibTypes">A <see cref="ICorLibTypes"/> instance</param>
		/// <param name="signature">The signature data</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ISignatureReaderHelper helper, ICorLibTypes corLibTypes, byte[] signature, GenericParamContext gpContext) =>
			ReadTypeSig(helper, corLibTypes, ByteArrayDataReaderFactory.CreateReader(signature), gpContext);

		/// <summary>
		/// Reads a <see cref="TypeSig"/> signature
		/// </summary>
		/// <param name="helper">Token resolver</param>
		/// <param name="corLibTypes">A <see cref="ICorLibTypes"/> instance</param>
		/// <param name="signature">The signature reader</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ISignatureReaderHelper helper, ICorLibTypes corLibTypes, DataReader signature) =>
			ReadTypeSig(helper, corLibTypes, signature, new GenericParamContext());

		/// <summary>
		/// Reads a <see cref="TypeSig"/> signature
		/// </summary>
		/// <param name="helper">Token resolver</param>
		/// <param name="corLibTypes">A <see cref="ICorLibTypes"/> instance</param>
		/// <param name="signature">The signature reader</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ISignatureReaderHelper helper, ICorLibTypes corLibTypes, DataReader signature, GenericParamContext gpContext) =>
			ReadTypeSig(helper, corLibTypes, signature, gpContext, out var extraData);

		/// <summary>
		/// Reads a <see cref="TypeSig"/> signature
		/// </summary>
		/// <param name="helper">Token resolver</param>
		/// <param name="corLibTypes">A <see cref="ICorLibTypes"/> instance</param>
		/// <param name="signature">The signature data</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <param name="extraData">If there's any extra data after the signature, it's saved
		/// here, else this will be <c>null</c></param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ISignatureReaderHelper helper, ICorLibTypes corLibTypes, byte[] signature, GenericParamContext gpContext, out byte[] extraData) =>
			ReadTypeSig(helper, corLibTypes, ByteArrayDataReaderFactory.CreateReader(signature), gpContext, out extraData);

		/// <summary>
		/// Reads a <see cref="TypeSig"/> signature
		/// </summary>
		/// <param name="helper">Token resolver</param>
		/// <param name="corLibTypes">A <see cref="ICorLibTypes"/> instance</param>
		/// <param name="signature">The signature reader</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <param name="extraData">If there's any extra data after the signature, it's saved
		/// here, else this will be <c>null</c></param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="signature"/> is invalid.</returns>
		public static TypeSig ReadTypeSig(ISignatureReaderHelper helper, ICorLibTypes corLibTypes, DataReader signature, GenericParamContext gpContext, out byte[] extraData) {
			try {
				var reader = new SignatureReader(helper, corLibTypes, ref signature, gpContext);
				TypeSig ts;
				try {
					ts = reader.ReadType();
				}
				catch (IOException) {
					reader.reader.Position = 0;
					ts = null;
				}
				extraData = reader.GetExtraData();
				return ts;
			}
			catch {
				extraData = null;
				return null;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <param name="gpContext">Generic parameter context</param>
		SignatureReader(ModuleDefMD readerModule, uint sig, GenericParamContext gpContext) {
			helper = readerModule;
			corLibTypes = readerModule.CorLibTypes;
			reader = readerModule.BlobStream.CreateReader(sig);
			this.gpContext = gpContext;
			recursionCounter = new RecursionCounter();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="helper">Token resolver</param>
		/// <param name="corLibTypes">A <see cref="ICorLibTypes"/> instance</param>
		/// <param name="reader">The signature data</param>
		/// <param name="gpContext">Generic parameter context</param>
		SignatureReader(ISignatureReaderHelper helper, ICorLibTypes corLibTypes, ref DataReader reader, GenericParamContext gpContext) {
			this.helper = helper;
			this.corLibTypes = corLibTypes;
			this.reader = reader;
			this.gpContext = gpContext;
			recursionCounter = new RecursionCounter();
		}

		byte[] GetExtraData() {
			if (reader.Position == reader.Length)
				return null;
			return reader.ReadRemainingBytes();
		}

		/// <summary>
		/// Reads the signature
		/// </summary>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if invalid signature</returns>
		CallingConventionSig ReadSig() {
			if (!recursionCounter.Increment())
				return null;

			CallingConventionSig result;
			var callingConvention = (CallingConvention)reader.ReadByte();
			switch (callingConvention & CallingConvention.Mask) {
			case CallingConvention.Default:
			case CallingConvention.C:
			case CallingConvention.StdCall:
			case CallingConvention.ThisCall:
			case CallingConvention.FastCall:
			case CallingConvention.VarArg:
			case CallingConvention.NativeVarArg:
				result = ReadMethod(callingConvention);
				break;

			case CallingConvention.Field:
				result = ReadField(callingConvention);
				break;

			case CallingConvention.LocalSig:
				result = ReadLocalSig(callingConvention);
				break;

			case CallingConvention.Property:
				result = ReadProperty(callingConvention);
				break;

			case CallingConvention.GenericInst:
				result = ReadGenericInstMethod(callingConvention);
				break;

			case CallingConvention.Unmanaged:
			default:
				result = null;
				break;
			}

			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Reads a <see cref="FieldSig"/>
		/// </summary>
		/// <param name="callingConvention">First byte of signature</param>
		/// <returns>A new <see cref="FieldSig"/> instance</returns>
		FieldSig ReadField(CallingConvention callingConvention) => new FieldSig(callingConvention, ReadType());

		/// <summary>
		/// Reads a <see cref="MethodSig"/>
		/// </summary>
		/// <param name="callingConvention">First byte of signature</param>
		/// <returns>A new <see cref="MethodSig"/> instance</returns>
		MethodSig ReadMethod(CallingConvention callingConvention) => ReadSig(new MethodSig(callingConvention));

		/// <summary>
		/// Reads a <see cref="PropertySig"/>
		/// </summary>
		/// <param name="callingConvention">First byte of signature</param>
		/// <returns>A new <see cref="PropertySig"/> instance</returns>
		PropertySig ReadProperty(CallingConvention callingConvention) => ReadSig(new PropertySig(callingConvention));

		T ReadSig<T>(T methodSig) where T : MethodBaseSig {
			if (methodSig.Generic) {
				if (!reader.TryReadCompressedUInt32(out uint count))
					return null;
				methodSig.GenParamCount = count;
			}

			if (!reader.TryReadCompressedUInt32(out uint numParams))
				return null;

			methodSig.RetType = ReadType();

			var parameters = methodSig.Params;
			for (uint i = 0; i < numParams; i++) {
				var type = ReadType();
				if (type is SentinelSig) {
					if (methodSig.ParamsAfterSentinel == null)
						methodSig.ParamsAfterSentinel = parameters = new List<TypeSig>((int)(numParams - i));
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
			if (!reader.TryReadCompressedUInt32(out uint count))
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
			if (!reader.TryReadCompressedUInt32(out uint count))
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
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if invalid element type</returns>
		TypeSig ReadType() {
			if (!recursionCounter.Increment())
				return null;

			uint num;
			TypeSig nextType, result = null;
			switch ((ElementType)reader.ReadByte()) {
			case ElementType.Void:		result = corLibTypes.Void; break;
			case ElementType.Boolean:	result = corLibTypes.Boolean; break;
			case ElementType.Char:		result = corLibTypes.Char; break;
			case ElementType.I1:		result = corLibTypes.SByte; break;
			case ElementType.U1:		result = corLibTypes.Byte; break;
			case ElementType.I2:		result = corLibTypes.Int16; break;
			case ElementType.U2:		result = corLibTypes.UInt16; break;
			case ElementType.I4:		result = corLibTypes.Int32; break;
			case ElementType.U4:		result = corLibTypes.UInt32; break;
			case ElementType.I8:		result = corLibTypes.Int64; break;
			case ElementType.U8:		result = corLibTypes.UInt64; break;
			case ElementType.R4:		result = corLibTypes.Single; break;
			case ElementType.R8:		result = corLibTypes.Double; break;
			case ElementType.String:	result = corLibTypes.String; break;
			case ElementType.TypedByRef:result = corLibTypes.TypedReference; break;
			case ElementType.I:			result = corLibTypes.IntPtr; break;
			case ElementType.U:			result = corLibTypes.UIntPtr; break;
			case ElementType.Object:	result = corLibTypes.Object; break;

			case ElementType.Ptr:		result = new PtrSig(ReadType()); break;
			case ElementType.ByRef:		result = new ByRefSig(ReadType()); break;
			case ElementType.ValueType:	result = new ValueTypeSig(ReadTypeDefOrRef()); break;
			case ElementType.Class:		result = new ClassSig(ReadTypeDefOrRef()); break;
			case ElementType.FnPtr:		result = new FnPtrSig(ReadSig()); break;
			case ElementType.SZArray:	result = new SZArraySig(ReadType()); break;
			case ElementType.CModReqd:	result = new CModReqdSig(ReadTypeDefOrRef(), ReadType()); break;
			case ElementType.CModOpt:	result = new CModOptSig(ReadTypeDefOrRef(), ReadType()); break;
			case ElementType.Sentinel:	result = new SentinelSig(); break;
			case ElementType.Pinned:	result = new PinnedSig(ReadType()); break;

			case ElementType.Var:
				if (!reader.TryReadCompressedUInt32(out num))
					break;
				result = new GenericVar(num, gpContext.Type);
				break;

			case ElementType.MVar:
				if (!reader.TryReadCompressedUInt32(out num))
					break;
				result = new GenericMVar(num, gpContext.Method);
				break;

			case ElementType.ValueArray:
				nextType = ReadType();
				if (!reader.TryReadCompressedUInt32(out num))
					break;
				result = new ValueArraySig(nextType, num);
				break;

			case ElementType.Module:
				if (!reader.TryReadCompressedUInt32(out num))
					break;
				result = new ModuleSig(num, ReadType());
				break;

			case ElementType.GenericInst:
				nextType = ReadType();
				if (!reader.TryReadCompressedUInt32(out num))
					break;
				var genericInstSig = new GenericInstSig(nextType as ClassOrValueTypeSig, num);
				var args = genericInstSig.GenericArguments;
				for (uint i = 0; i < num; i++)
					args.Add(ReadType());
				result = genericInstSig;
				break;

			case ElementType.Array:
				nextType = ReadType();
				uint rank;
				if (!reader.TryReadCompressedUInt32(out rank))
					break;
				if (rank == 0) {
					result = new ArraySig(nextType, rank);
					break;
				}
				if (!reader.TryReadCompressedUInt32(out num))
					break;
				var sizes = new List<uint>((int)num);
				for (uint i = 0; i < num; i++) {
					if (!reader.TryReadCompressedUInt32(out uint size))
						goto exit;
					sizes.Add(size);
				}
				if (!reader.TryReadCompressedUInt32(out num))
					break;
				var lowerBounds = new List<int>((int)num);
				for (uint i = 0; i < num; i++) {
					if (!reader.TryReadCompressedInt32(out int size))
						goto exit;
					lowerBounds.Add(size);
				}
				result = new ArraySig(nextType, rank, sizes, lowerBounds);
				break;

			case ElementType.Internal:
				IntPtr address;
				if (IntPtr.Size == 4)
					address = new IntPtr(reader.ReadInt32());
				else
					address = new IntPtr(reader.ReadInt64());
				result = helper.ConvertRTInternalAddress(address);
				break;

			case ElementType.End:
			case ElementType.R:
			default:
				result = null;
				break;
			}
exit:
			recursionCounter.Decrement();
			return result;
		}

		/// <summary>
		/// Reads a <c>TypeDefOrRef</c>
		/// </summary>
		/// <returns>A <see cref="ITypeDefOrRef"/> instance</returns>
		ITypeDefOrRef ReadTypeDefOrRef() {
			if (!reader.TryReadCompressedUInt32(out uint codedToken))
				return null;
			return helper.ResolveTypeDefOrRef(codedToken, gpContext);
		}
	}
}
