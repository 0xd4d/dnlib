// dnlib: See LICENSE.txt for more info

using System;
using System.IO;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Helps <see cref="SignatureWriter"/> map <see cref="ITypeDefOrRef"/>s to tokens
	/// </summary>
	public interface ISignatureWriterHelper : IWriterError {
		/// <summary>
		/// Returns a <c>TypeDefOrRef</c> encoded token
		/// </summary>
		/// <param name="typeDefOrRef">A <c>TypeDefOrRef</c> type</param>
		uint ToEncodedToken(ITypeDefOrRef typeDefOrRef);
	}

	/// <summary>
	/// Writes signatures
	/// </summary>
	public struct SignatureWriter : IDisposable {
		readonly ISignatureWriterHelper helper;
		RecursionCounter recursionCounter;
		readonly MemoryStream outStream;
		readonly DataWriter writer;
		readonly bool disposeStream;

		/// <summary>
		/// Write a <see cref="TypeSig"/> signature
		/// </summary>
		/// <param name="helper">Helper</param>
		/// <param name="typeSig">The type</param>
		/// <returns>The signature as a byte array</returns>
		public static byte[] Write(ISignatureWriterHelper helper, TypeSig typeSig) {
			using (var writer = new SignatureWriter(helper)) {
				writer.Write(typeSig);
				return writer.GetResult();
			}
		}

		internal static byte[] Write(ISignatureWriterHelper helper, TypeSig typeSig, DataWriterContext context) {
			using (var writer = new SignatureWriter(helper, context)) {
				writer.Write(typeSig);
				return writer.GetResult();
			}
		}

		/// <summary>
		/// Write a <see cref="CallingConventionSig"/> signature
		/// </summary>
		/// <param name="helper">Helper</param>
		/// <param name="sig">The signature</param>
		/// <returns>The signature as a byte array</returns>
		public static byte[] Write(ISignatureWriterHelper helper, CallingConventionSig sig) {
			using (var writer = new SignatureWriter(helper)) {
				writer.Write(sig);
				return writer.GetResult();
			}
		}

		internal static byte[] Write(ISignatureWriterHelper helper, CallingConventionSig sig, DataWriterContext context) {
			using (var writer = new SignatureWriter(helper, context)) {
				writer.Write(sig);
				return writer.GetResult();
			}
		}

		SignatureWriter(ISignatureWriterHelper helper) {
			this.helper = helper;
			recursionCounter = new RecursionCounter();
			outStream = new MemoryStream();
			writer = new DataWriter(outStream);
			disposeStream = true;
		}

		SignatureWriter(ISignatureWriterHelper helper, DataWriterContext context) {
			this.helper = helper;
			recursionCounter = new RecursionCounter();
			outStream = context.OutStream;
			writer = context.Writer;
			disposeStream = false;
			outStream.SetLength(0);
			outStream.Position = 0;
		}

		byte[] GetResult() => outStream.ToArray();
		uint WriteCompressedUInt32(uint value) => writer.WriteCompressedUInt32(helper, value);
		int WriteCompressedInt32(int value) => writer.WriteCompressedInt32(helper, value);

		void Write(TypeSig typeSig) {
			const ElementType DEFAULT_ELEMENT_TYPE = ElementType.Boolean;
			if (typeSig is null) {
				helper.Error("TypeSig is null");
				writer.WriteByte((byte)DEFAULT_ELEMENT_TYPE);
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				writer.WriteByte((byte)DEFAULT_ELEMENT_TYPE);
				return;
			}

			uint count;
			switch (typeSig.ElementType) {
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
			case ElementType.Sentinel:
				writer.WriteByte((byte)typeSig.ElementType);
				break;

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.SZArray:
			case ElementType.Pinned:
				writer.WriteByte((byte)typeSig.ElementType);
				Write(typeSig.Next);
				break;

			case ElementType.ValueType:
			case ElementType.Class:
				writer.WriteByte((byte)typeSig.ElementType);
				Write(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				break;

			case ElementType.Var:
			case ElementType.MVar:
				writer.WriteByte((byte)typeSig.ElementType);
				WriteCompressedUInt32(((GenericSig)typeSig).Number);
				break;

			case ElementType.Array:
				writer.WriteByte((byte)typeSig.ElementType);
				var ary = (ArraySig)typeSig;
				Write(ary.Next);
				WriteCompressedUInt32(ary.Rank);
				if (ary.Rank == 0)
					break;
				count = WriteCompressedUInt32((uint)ary.Sizes.Count);
				for (uint i = 0; i < count; i++)
					WriteCompressedUInt32(ary.Sizes[(int)i]);
				count = WriteCompressedUInt32((uint)ary.LowerBounds.Count);
				for (uint i = 0; i < count; i++)
					WriteCompressedInt32(ary.LowerBounds[(int)i]);
				break;

			case ElementType.GenericInst:
				writer.WriteByte((byte)typeSig.ElementType);
				var gis = (GenericInstSig)typeSig;
				Write(gis.GenericType);
				count = WriteCompressedUInt32((uint)gis.GenericArguments.Count);
				for (uint i = 0; i < count; i++)
					Write(gis.GenericArguments[(int)i]);
				break;

			case ElementType.ValueArray:
				writer.WriteByte((byte)typeSig.ElementType);
				Write(typeSig.Next);
				WriteCompressedUInt32((typeSig as ValueArraySig).Size);
				break;

			case ElementType.FnPtr:
				writer.WriteByte((byte)typeSig.ElementType);
				Write((typeSig as FnPtrSig).Signature);
				break;

			case ElementType.CModReqd:
			case ElementType.CModOpt:
				writer.WriteByte((byte)typeSig.ElementType);
				Write((typeSig as ModifierSig).Modifier);
				Write(typeSig.Next);
				break;

			case ElementType.Module:
				writer.WriteByte((byte)typeSig.ElementType);
				WriteCompressedUInt32((typeSig as ModuleSig).Index);
				Write(typeSig.Next);
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				helper.Error("Unknown or unsupported element type");
				writer.WriteByte((byte)DEFAULT_ELEMENT_TYPE);
				break;
			}

			recursionCounter.Decrement();
		}

		void Write(ITypeDefOrRef tdr) {
			if (tdr is null) {
				helper.Error("TypeDefOrRef is null");
				WriteCompressedUInt32(0);
				return;
			}

			uint encodedToken = helper.ToEncodedToken(tdr);
			if (encodedToken > 0x1FFFFFFF) {
				helper.Error("Encoded token doesn't fit in 29 bits");
				encodedToken = 0;
			}
			WriteCompressedUInt32(encodedToken);
		}

		void Write(CallingConventionSig sig) {
			if (sig is null) {
				helper.Error("sig is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			MethodBaseSig mbs;
			FieldSig fs;
			LocalSig ls;
			GenericInstMethodSig gim;

			if (!((mbs = sig as MethodBaseSig) is null))
				Write(mbs);
			else if (!((fs = sig as FieldSig) is null))
				Write(fs);
			else if (!((ls = sig as LocalSig) is null))
				Write(ls);
			else if (!((gim = sig as GenericInstMethodSig) is null))
				Write(gim);
			else {
				helper.Error("Unknown calling convention sig");
				writer.WriteByte((byte)sig.GetCallingConvention());
			}

			recursionCounter.Decrement();
		}

		void Write(MethodBaseSig sig) {
			if (sig is null) {
				helper.Error("sig is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			writer.WriteByte((byte)sig.GetCallingConvention());
			if (sig.Generic)
				WriteCompressedUInt32(sig.GenParamCount);

			uint numParams = (uint)sig.Params.Count;
			if (!(sig.ParamsAfterSentinel is null))
				numParams += (uint)sig.ParamsAfterSentinel.Count;

			uint count = WriteCompressedUInt32(numParams);
			Write(sig.RetType);
			for (uint i = 0; i < count && i < (uint)sig.Params.Count; i++)
				Write(sig.Params[(int)i]);

			if (!(sig.ParamsAfterSentinel is null) && sig.ParamsAfterSentinel.Count > 0) {
				writer.WriteByte((byte)ElementType.Sentinel);
				for (uint i = 0, j = (uint)sig.Params.Count; i < (uint)sig.ParamsAfterSentinel.Count && j < count; i++, j++)
					Write(sig.ParamsAfterSentinel[(int)i]);
			}

			recursionCounter.Decrement();
		}

		void Write(FieldSig sig) {
			if (sig is null) {
				helper.Error("sig is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			writer.WriteByte((byte)sig.GetCallingConvention());
			Write(sig.Type);

			recursionCounter.Decrement();
		}

		void Write(LocalSig sig) {
			if (sig is null) {
				helper.Error("sig is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			writer.WriteByte((byte)sig.GetCallingConvention());
			uint count = WriteCompressedUInt32((uint)sig.Locals.Count);
			if (count >= 0x10000) {
				// ldloc 0xFFFF is invalid, see the ldloc documentation
				helper.Error("Too many locals, max number of locals is 65535 (0xFFFF)");
			}
			for (uint i = 0; i < count; i++)
				Write(sig.Locals[(int)i]);

			recursionCounter.Decrement();
		}

		void Write(GenericInstMethodSig sig) {
			if (sig is null) {
				helper.Error("sig is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			writer.WriteByte((byte)sig.GetCallingConvention());
			uint count = WriteCompressedUInt32((uint)sig.GenericArguments.Count);
			for (uint i = 0; i < count; i++)
				Write(sig.GenericArguments[(int)i]);

			recursionCounter.Decrement();
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (!disposeStream)
				return;
			if (!(outStream is null))
				outStream.Dispose();
		}
	}
}
