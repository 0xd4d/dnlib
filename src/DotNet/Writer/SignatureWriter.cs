using System;
using System.Collections.Generic;
using System.IO;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Helps <see cref="SignatureWriter"/> map <see cref="ITypeDefOrRef"/>s to tokens
	/// </summary>
	public interface ISignatureWriterHelper {
		/// <summary>
		/// Returns a <c>TypeDefOrRef</c> encoded token
		/// </summary>
		/// <param name="typeDefOrRef">A <c>TypeDefOrRef</c> type</param>
		uint ToEncodedToken(ITypeDefOrRef typeDefOrRef);

		/// <summary>
		/// Called when an error is detected (eg. a null pointer). The error can be
		/// ignored but the signature won't be valid.
		/// </summary>
		/// <param name="message">Error message</param>
		void Error(string message);
	}

	/// <summary>
	/// Writes signatures
	/// </summary>
	public struct SignatureWriter : IDisposable {
		ISignatureWriterHelper helper;
		RecursionCounter recursionCounter;
		MemoryStream outStream;
		BinaryWriter writer;

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

		void Write(TypeSig typeSig) {
			if (typeSig == null) {
				helper.Error("TypeSig is null");
				return;
			}
			if (!recursionCounter.Increment()) {
				helper.Error("Infinite recursion");
				return;
			}

			writer.Write((byte)typeSig.ElementType);

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
				break;

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.SZArray:
			case ElementType.Pinned:
				Write(typeSig.Next);
				break;

			case ElementType.ValueType:
			case ElementType.Class:
				Write(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				break;

			case ElementType.Var:
			case ElementType.MVar:
				writer.WriteCompressedUInt32(((GenericSig)typeSig).Number);
				break;

			case ElementType.Array:
				var ary = (ArraySig)typeSig;
				Write(ary.Next);
				writer.WriteCompressedUInt32(ary.Rank);
				if (ary.Rank == 0)
					break;
				writer.WriteCompressedUInt32((uint)ary.Sizes.Count);
				foreach (var size in ary.Sizes)
					writer.WriteCompressedUInt32(size);
				writer.WriteCompressedUInt32((uint)ary.LowerBounds.Count);
				foreach (var lb in ary.LowerBounds)
					writer.WriteCompressedInt32(lb);
				break;

			case ElementType.GenericInst:
				var gis = (GenericInstSig)typeSig;
				Write(gis.GenericType);
				writer.WriteCompressedUInt32((uint)gis.GenericArguments.Count);
				foreach (var ga in gis.GenericArguments)
					Write(ga);
				break;

			case ElementType.ValueArray:
				Write(typeSig.Next);
				writer.WriteCompressedUInt32((typeSig as ValueArraySig).Size);
				break;

			case ElementType.FnPtr:
				Write((typeSig as FnPtrSig).Signature);
				break;

			case ElementType.CModReqd:
			case ElementType.CModOpt:
				Write((typeSig as ModifierSig).Modifier);
				Write(typeSig.Next);
				break;

			case ElementType.Module:
				writer.WriteCompressedUInt32((typeSig as ModuleSig).Index);
				Write(typeSig.Next);
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				helper.Error("Unknown or unsupported element type");
				break;
			}

			recursionCounter.Decrement();
		}

		void Write(ITypeDefOrRef tdr) {
			if (tdr == null) {
				helper.Error("TypeDefOrRef is null");
				writer.WriteCompressedUInt32(0);
				return;
			}

			uint encodedToken = helper.ToEncodedToken(tdr);
			if (encodedToken > 0x1FFFFFFF) {
				helper.Error("Encoded token is too big");
				encodedToken = 0;
			}
			writer.WriteCompressedUInt32(encodedToken);
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
				writer.WriteCompressedUInt32(sig.GenParamCount);

			uint numParams = (uint)sig.Params.Count;
			if (sig.ParamsAfterSentinel != null)
				numParams += (uint)sig.ParamsAfterSentinel.Count;

			writer.WriteCompressedUInt32(numParams);
			Write(sig.RetType);
			Write(sig.Params);

			if (sig.ParamsAfterSentinel != null && sig.ParamsAfterSentinel.Count > 0) {
				writer.Write((byte)ElementType.Sentinel);
				Write(sig.ParamsAfterSentinel);
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
			writer.WriteCompressedUInt32((uint)sig.Locals.Count);
			Write(sig.Locals);

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
			writer.WriteCompressedUInt32((uint)sig.GenericArguments.Count);
			Write(sig.GenericArguments);

			recursionCounter.Decrement();
		}

		void Write(IList<TypeSig> sigs) {
			foreach (var sig in sigs)
				Write(sig);
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
