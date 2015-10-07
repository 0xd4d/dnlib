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
		readonly BinaryWriter writer;

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

		SignatureWriter(ISignatureWriterHelper helper) {
			this.helper = helper;
			this.recursionCounter = new RecursionCounter();
			this.outStream = new MemoryStream();
			this.writer = new BinaryWriter(outStream);
		}

		byte[] GetResult() {
			return outStream.ToArray();
		}

		uint WriteCompressedUInt32(uint value) {
			return writer.WriteCompressedUInt32(helper, value);
		}

		int WriteCompressedInt32(int value) {
			return writer.WriteCompressedInt32(helper, value);
		}

		void Write(TypeSig typeSig) {
			const ElementType DEFAULT_ELEMENT_TYPE = ElementType.Boolean;
			if (typeSig == null) {
				helper.Error("TypeSig is null");
				writer.Write((byte)DEFAULT_ELEMENT_TYPE);
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				writer.Write((byte)DEFAULT_ELEMENT_TYPE);
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
				writer.Write((byte)typeSig.ElementType);
				break;

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.SZArray:
			case ElementType.Pinned:
				writer.Write((byte)typeSig.ElementType);
				Write(typeSig.Next);
				break;

			case ElementType.ValueType:
			case ElementType.Class:
				writer.Write((byte)typeSig.ElementType);
				Write(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				break;

			case ElementType.Var:
			case ElementType.MVar:
				writer.Write((byte)typeSig.ElementType);
				WriteCompressedUInt32(((GenericSig)typeSig).Number);
				break;

			case ElementType.Array:
				writer.Write((byte)typeSig.ElementType);
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
				writer.Write((byte)typeSig.ElementType);
				var gis = (GenericInstSig)typeSig;
				Write(gis.GenericType);
				count = WriteCompressedUInt32((uint)gis.GenericArguments.Count);
				for (uint i = 0; i < count; i++)
					Write(gis.GenericArguments[(int)i]);
				break;

			case ElementType.ValueArray:
				writer.Write((byte)typeSig.ElementType);
				Write(typeSig.Next);
				WriteCompressedUInt32((typeSig as ValueArraySig).Size);
				break;

			case ElementType.FnPtr:
				writer.Write((byte)typeSig.ElementType);
				Write((typeSig as FnPtrSig).Signature);
				break;

			case ElementType.CModReqd:
			case ElementType.CModOpt:
				writer.Write((byte)typeSig.ElementType);
				Write((typeSig as ModifierSig).Modifier);
				Write(typeSig.Next);
				break;

			case ElementType.Module:
				writer.Write((byte)typeSig.ElementType);
				WriteCompressedUInt32((typeSig as ModuleSig).Index);
				Write(typeSig.Next);
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				helper.Error("Unknown or unsupported element type");
				writer.Write((byte)DEFAULT_ELEMENT_TYPE);
				break;
			}

			recursionCounter.Decrement();
		}

		void Write(ITypeDefOrRef tdr) {
			if (tdr == null) {
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
			if (sig == null) {
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

			if ((mbs = sig as MethodBaseSig) != null)
				Write(mbs);
			else if ((fs = sig as FieldSig) != null)
				Write(fs);
			else if ((ls = sig as LocalSig) != null)
				Write(ls);
			else if ((gim = sig as GenericInstMethodSig) != null)
				Write(gim);
			else {
				helper.Error("Unknown calling convention sig");
				writer.Write((byte)sig.GetCallingConvention());
			}

			recursionCounter.Decrement();
		}

		void Write(MethodBaseSig sig) {
			if (sig == null) {
				helper.Error("sig is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			writer.Write((byte)sig.GetCallingConvention());
			if (sig.Generic)
				WriteCompressedUInt32(sig.GenParamCount);

			uint numParams = (uint)sig.Params.Count;
			if (sig.ParamsAfterSentinel != null)
				numParams += (uint)sig.ParamsAfterSentinel.Count;

			uint count = WriteCompressedUInt32(numParams);
			Write(sig.RetType);
			for (uint i = 0; i < count && i < (uint)sig.Params.Count; i++)
				Write(sig.Params[(int)i]);

			if (sig.ParamsAfterSentinel != null && sig.ParamsAfterSentinel.Count > 0) {
				writer.Write((byte)ElementType.Sentinel);
				for (uint i = 0, j = (uint)sig.Params.Count; i < (uint)sig.ParamsAfterSentinel.Count && j < count; i++, j++)
					Write(sig.ParamsAfterSentinel[(int)i]);
			}

			recursionCounter.Decrement();
		}

		void Write(FieldSig sig) {
			if (sig == null) {
				helper.Error("sig is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			writer.Write((byte)sig.GetCallingConvention());
			Write(sig.Type);

			recursionCounter.Decrement();
		}

		void Write(LocalSig sig) {
			if (sig == null) {
				helper.Error("sig is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			writer.Write((byte)sig.GetCallingConvention());
			uint count = WriteCompressedUInt32((uint)sig.Locals.Count);
			for (uint i = 0; i < count; i++)
				Write(sig.Locals[(int)i]);

			recursionCounter.Decrement();
		}

		void Write(GenericInstMethodSig sig) {
			if (sig == null) {
				helper.Error("sig is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			writer.Write((byte)sig.GetCallingConvention());
			uint count = WriteCompressedUInt32((uint)sig.GenericArguments.Count);
			for (uint i = 0; i < count; i++)
				Write(sig.GenericArguments[(int)i]);

			recursionCounter.Decrement();
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (outStream != null)
				outStream.Dispose();
			if (writer != null)
				((IDisposable)writer).Dispose();
		}
	}
}
