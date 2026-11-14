// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet.MD;
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
		// .NET Core and .NET Framework limit arrays to 32 dimensions. Use a bigger limit
		// so it's possible to read some bad MD, but not big enough to allocate a ton of mem.
		const uint MaxArrayRank = 64;

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
				if (csig is not null)
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

		/// <summary>
		/// Reads a <c>calli</c> signature. The CLR reads it as a method signature whatever its calling
		/// convention is, and calling conventions that aren't method calling conventions are treated
		/// like <see cref="CallingConvention.Default"/>.
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="MethodSig"/> instance or <c>null</c> if <paramref name="sig"/> is invalid</returns>
		internal static MethodSig ReadCalliSig(ModuleDefMD readerModule, uint sig, GenericParamContext gpContext) {
			try {
				return new SignatureReader(readerModule, sig, gpContext).ReadCalliSig();
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Reads a <c>calli</c> signature. See <see cref="ReadCalliSig(ModuleDefMD, uint, GenericParamContext)"/>
		/// </summary>
		/// <param name="helper">Token resolver</param>
		/// <param name="corLibTypes">A <see cref="ICorLibTypes"/> instance</param>
		/// <param name="signature">The signature data</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="MethodSig"/> instance or <c>null</c> if <paramref name="signature"/> is invalid</returns>
		internal static MethodSig ReadCalliSig(ISignatureReaderHelper helper, ICorLibTypes corLibTypes, byte[] signature, GenericParamContext gpContext) {
			try {
				var reader = ByteArrayDataReaderFactory.CreateReader(signature);
				return new SignatureReader(helper, corLibTypes, ref reader, gpContext).ReadCalliSig();
			}
			catch {
				return null;
			}
		}

		MethodSig ReadCalliSig() {
			if (reader.Length == 0)
				return null;
			var callingConvention = (CallingConvention)reader.ReadByte();
			switch (callingConvention & CallingConvention.Mask) {
			case CallingConvention.Default:
			case CallingConvention.C:
			case CallingConvention.StdCall:
			case CallingConvention.ThisCall:
			case CallingConvention.FastCall:
			case CallingConvention.VarArg:
			case CallingConvention.Unmanaged:
			case CallingConvention.NativeVarArg:
			case CallingConvention.Async:
				break;
			default:
				// The JIT treats them like the default calling convention (it rejects LocalSig but we allow it)
				callingConvention &= ~CallingConvention.Mask;
				break;
			}
			var sig = ReadSig(new MethodSig(callingConvention));
			if (sig is not null)
				sig.ExtraData = GetExtraData();
			return sig;
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
			if (!recursionCounter.Increment()) {
				// Skip it so the rest of the signature is read from the correct position
				SkipSig();
				return null;
			}

			CallingConventionSig result;
			var callingConvention = (CallingConvention)reader.ReadByte();
			switch (callingConvention & CallingConvention.Mask) {
			case CallingConvention.Default:
			case CallingConvention.C:
			case CallingConvention.StdCall:
			case CallingConvention.ThisCall:
			case CallingConvention.FastCall:
			case CallingConvention.VarArg:
			case CallingConvention.Unmanaged:
			case CallingConvention.NativeVarArg:
			case CallingConvention.Async:
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
				// The CLR only allows up to 0xFFFF generic parameters
				if (!reader.TryReadCompressedUInt32(out uint count) || count > 0xFFFF)
					return null;
				methodSig.GenParamCount = count;
			}

			// The CLR only allows up to 0xFFFF parameters
			if (!reader.TryReadCompressedUInt32(out uint numParams) || numParams > 0xFFFF || numParams > reader.BytesLeft)
				return null;

			methodSig.RetType = ReadType();

			var parameters = methodSig.Params;
			for (uint i = 0; i < numParams; i++) {
				// A sentinel is part of the next param, ReadType() skips it
				if (methodSig.ParamsAfterSentinel is null && PeekIsSentinel())
					methodSig.ParamsAfterSentinel = parameters = new List<TypeSig>((int)(numParams - i));
				parameters.Add(ReadType());
			}

			return methodSig;
		}

		/// <summary>
		/// Reads a <see cref="LocalSig"/>
		/// </summary>
		/// <param name="callingConvention">First byte of signature</param>
		/// <returns>A new <see cref="LocalSig"/> instance</returns>
		LocalSig ReadLocalSig(CallingConvention callingConvention) {
			// The CLR only allows up to 0xFFFF locals
			if (!reader.TryReadCompressedUInt32(out uint count) || count > 0xFFFF || count > reader.BytesLeft)
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
			// The CLR only allows up to 0xFFFF generic arguments
			if (!reader.TryReadCompressedUInt32(out uint count) || count > 0xFFFF || count > reader.BytesLeft)
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
		/// <param name="allowTypeSpec"><c>true</c> if a <c>TypeSpec</c> is allowed if the next type is a class/value-type</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if invalid element type</returns>
		TypeSig ReadType(bool allowTypeSpec = false) {
			if (!recursionCounter.Increment()) {
				// Skip it so the rest of the signature is read from the correct position
				SkipType();
				return null;
			}

			uint num, i;
			TypeSig nextType, result = null;
			var etype = (ElementType)reader.ReadByte();
			// The CLR ignores one sentinel before any type (SigParser::SkipAnyVASentinel())
			if (etype == ElementType.Sentinel)
				etype = (ElementType)reader.ReadByte();
			switch (etype) {
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
			case ElementType.ValueType:	result = new ValueTypeSig(ReadTypeDefOrRef(allowTypeSpec)); break;
			case ElementType.Class:		result = new ClassSig(ReadTypeDefOrRef(allowTypeSpec)); break;
			case ElementType.FnPtr:		result = new FnPtrSig(ReadSig()); break;
			case ElementType.SZArray:	result = new SZArraySig(ReadType()); break;
			case ElementType.CModReqd:	result = new CModReqdSig(ReadTypeDefOrRef(true), ReadType()); break;
			case ElementType.CModOpt:	result = new CModOptSig(ReadTypeDefOrRef(true), ReadType()); break;
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
				// The CLR only allows up to 0xFFFF generic arguments
				if (!reader.TryReadCompressedUInt32(out num) || num > 0xFFFF || num > reader.BytesLeft)
					break;
				var genericInstSig = new GenericInstSig(nextType as ClassOrValueTypeSig, num);
				var args = genericInstSig.GenericArguments;
				for (i = 0; i < num; i++)
					args.Add(ReadType());
				result = genericInstSig;
				break;

			case ElementType.Array:
				nextType = ReadType();
				uint rank;
				if (!reader.TryReadCompressedUInt32(out rank))
					break;
				if (rank > MaxArrayRank)
					break;
				if (rank == 0) {
					result = new ArraySig(nextType, rank);
					break;
				}
				if (!reader.TryReadCompressedUInt32(out num))
					break;
				// The CLR doesn't compare it with the rank. Each value is at least one byte.
				if (num > reader.BytesLeft)
					break;
				var sizes = new List<uint>((int)num);
				for (i = 0; i < num; i++) {
					if (!reader.TryReadCompressedUInt32(out uint size))
						goto exit;
					sizes.Add(size);
				}
				if (!reader.TryReadCompressedUInt32(out num))
					break;
				// The CLR doesn't compare it with the rank. Each value is at least one byte.
				if (num > reader.BytesLeft)
					break;
				var lowerBounds = new List<int>((int)num);
				for (i = 0; i < num; i++) {
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

			case ElementType.CModInternal:
				// The CLR allows this even in metadata signatures
				bool isRequired = reader.ReadByte() != 0;
				if (IntPtr.Size == 4)
					address = new IntPtr(reader.ReadInt32());
				else
					address = new IntPtr(reader.ReadInt64());
				var modifier = helper.ConvertRTInternalAddress(address).ToTypeDefOrRef();
				nextType = ReadType();
				// If the address can't be converted (always the case when reading a file), ignore the
				// modifier like the CLR does in most cases. When it doesn't, it reads the memory at
				// the address (eg. reflection, unmanaged calling conventions) and will likely crash.
				if (modifier is null)
					result = nextType;
				else if (isRequired)
					result = new CModReqdSig(modifier, nextType);
				else
					result = new CModOptSig(modifier, nextType);
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Sentinel:
			default:
				result = null;
				break;
			}
exit:
			recursionCounter.Decrement();
			return result;
		}

		enum SkipKind : byte {
			Types,
			GenericInstArgs,
			ArrayShape,
			CompressedUInt32,
		}

		readonly struct SkipItem {
			public readonly SkipKind Kind;
			public readonly uint Count;
			public SkipItem(SkipKind kind, uint count) {
				Kind = kind;
				Count = count;
			}
		}

		/// <summary>
		/// Skips a type without using recursion. Called when the recursion limit has been reached.
		/// It accepts the same data as <see cref="ReadType(bool)"/>.
		/// </summary>
		void SkipType() {
			var stack = new List<SkipItem>();
			stack.Add(new SkipItem(SkipKind.Types, 1));
			SkipItems(stack);
		}

		/// <summary>
		/// Skips a signature without using recursion. Called when the recursion limit has been reached.
		/// It accepts the same data as <see cref="ReadSig()"/>.
		/// </summary>
		void SkipSig() {
			var stack = new List<SkipItem>();
			SkipSigHeader(stack);
			SkipItems(stack);
		}

		/// <summary>
		/// Skips everything in <paramref name="stack"/>. If an item is invalid, it's treated like
		/// <see cref="ReadType(bool)"/> treats it: only that item (and its unread children) is
		/// abandoned, the remaining items are still skipped so the same bytes are consumed.
		/// </summary>
		/// <param name="stack">Things to skip, the last element is skipped first</param>
		void SkipItems(List<SkipItem> stack) {
			while (stack.Count > 0) {
				int index = stack.Count - 1;
				var item = stack[index];
				stack.RemoveAt(index);
				switch (item.Kind) {
				case SkipKind.Types:
					if (item.Count == 0)
						break;
					if (item.Count > 1)
						stack.Add(new SkipItem(SkipKind.Types, item.Count - 1));
					SkipOneType(stack);
					break;

				case SkipKind.GenericInstArgs:
					// Same checks as ReadType()
					if (reader.TryReadCompressedUInt32(out uint num) && num <= 0xFFFF && num <= reader.BytesLeft)
						stack.Add(new SkipItem(SkipKind.Types, num));
					break;

				case SkipKind.ArrayShape:
					SkipArrayShape();
					break;

				case SkipKind.CompressedUInt32:
					reader.TryReadCompressedUInt32(out _);
					break;

				default:
					throw new InvalidOperationException();
				}
			}
		}

		/// <summary>
		/// Reads the element type and its data, and pushes the things that must be skipped after it.
		/// If it's invalid, nothing is pushed so only this type is abandoned, like in <see cref="ReadType(bool)"/>.
		/// </summary>
		/// <param name="stack">Things to skip, the last element is skipped first</param>
		void SkipOneType(List<SkipItem> stack) {
			var etype = (ElementType)reader.ReadByte();
			// The CLR ignores one sentinel before any type (SigParser::SkipAnyVASentinel())
			if (etype == ElementType.Sentinel)
				etype = (ElementType)reader.ReadByte();
			switch (etype) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
				return;

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.SZArray:
			case ElementType.Pinned:
				stack.Add(new SkipItem(SkipKind.Types, 1));
				return;

			case ElementType.ValueType:
			case ElementType.Class:
			case ElementType.Var:
			case ElementType.MVar:
				reader.TryReadCompressedUInt32(out _);
				return;

			// ReadType() reads the next type even if the token is invalid
			case ElementType.CModReqd:
			case ElementType.CModOpt:
				reader.TryReadCompressedUInt32(out _);
				stack.Add(new SkipItem(SkipKind.Types, 1));
				return;

			case ElementType.Module:
				if (!reader.TryReadCompressedUInt32(out _))
					return;
				stack.Add(new SkipItem(SkipKind.Types, 1));
				return;

			case ElementType.FnPtr:
				SkipSigHeader(stack);
				return;

			// The next type comes first so it's pushed last
			case ElementType.ValueArray:
				stack.Add(new SkipItem(SkipKind.CompressedUInt32, 0));
				stack.Add(new SkipItem(SkipKind.Types, 1));
				return;

			case ElementType.GenericInst:
				stack.Add(new SkipItem(SkipKind.GenericInstArgs, 0));
				stack.Add(new SkipItem(SkipKind.Types, 1));
				return;

			case ElementType.Array:
				stack.Add(new SkipItem(SkipKind.ArrayShape, 0));
				stack.Add(new SkipItem(SkipKind.Types, 1));
				return;

			case ElementType.Internal:
				SkipInternalAddress();
				return;

			case ElementType.CModInternal:
				reader.ReadByte();
				SkipInternalAddress();
				stack.Add(new SkipItem(SkipKind.Types, 1));
				return;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Sentinel:
			default:
				return;
			}
		}

		void SkipInternalAddress() {
			// Same size as in ReadType()
			if (IntPtr.Size == 4)
				reader.ReadInt32();
			else
				reader.ReadInt64();
		}

		/// <summary>
		/// Skips the array rank, sizes and lower bounds. Same checks as <see cref="ReadType(bool)"/>
		/// </summary>
		void SkipArrayShape() {
			if (!reader.TryReadCompressedUInt32(out uint rank) || rank > MaxArrayRank)
				return;
			if (rank == 0)
				return;
			if (!reader.TryReadCompressedUInt32(out uint num) || num > reader.BytesLeft)
				return;
			for (uint i = 0; i < num; i++) {
				if (!reader.TryReadCompressedUInt32(out _))
					return;
			}
			if (!reader.TryReadCompressedUInt32(out num) || num > reader.BytesLeft)
				return;
			for (uint i = 0; i < num; i++) {
				if (!reader.TryReadCompressedInt32(out _))
					return;
			}
		}

		/// <summary>
		/// Reads the calling convention and counts of a signature, and pushes the types that must
		/// be skipped. Same checks as <see cref="ReadSig()"/>. If it's invalid, nothing is pushed.
		/// </summary>
		/// <param name="stack">Things to skip, the last element is skipped first</param>
		void SkipSigHeader(List<SkipItem> stack) {
			var callingConvention = (CallingConvention)reader.ReadByte();
			uint count;
			switch (callingConvention & CallingConvention.Mask) {
			case CallingConvention.Default:
			case CallingConvention.C:
			case CallingConvention.StdCall:
			case CallingConvention.ThisCall:
			case CallingConvention.FastCall:
			case CallingConvention.VarArg:
			case CallingConvention.Unmanaged:
			case CallingConvention.NativeVarArg:
			case CallingConvention.Async:
			case CallingConvention.Property:
				if ((callingConvention & CallingConvention.Generic) != 0) {
					if (!reader.TryReadCompressedUInt32(out count) || count > 0xFFFF)
						return;
				}
				if (!reader.TryReadCompressedUInt32(out count) || count > 0xFFFF || count > reader.BytesLeft)
					return;
				// Return type + params
				stack.Add(new SkipItem(SkipKind.Types, count + 1));
				return;

			case CallingConvention.Field:
				stack.Add(new SkipItem(SkipKind.Types, 1));
				return;

			case CallingConvention.LocalSig:
			case CallingConvention.GenericInst:
				if (!reader.TryReadCompressedUInt32(out count) || count > 0xFFFF || count > reader.BytesLeft)
					return;
				stack.Add(new SkipItem(SkipKind.Types, count));
				return;

			default:
				return;
			}
		}

		bool PeekIsSentinel() {
			if (reader.BytesLeft == 0)
				return false;
			var pos = reader.Position;
			var etype = (ElementType)reader.ReadByte();
			reader.Position = pos;
			return etype == ElementType.Sentinel;
		}

		ITypeDefOrRef ReadTypeDefOrRef(bool allowTypeSpec) {
			if (!reader.TryReadCompressedUInt32(out uint codedToken))
				return null;
			if (!allowTypeSpec && CodedToken.TypeDefOrRef.Decode2(codedToken).Table == Table.TypeSpec)
				return null;
			return helper.ResolveTypeDefOrRef(codedToken, default);
		}
	}
}
