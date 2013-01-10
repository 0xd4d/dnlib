/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;

namespace dnlib.DotNet {
	/// <summary>
	/// Reads signatures from the #Blob stream
	/// </summary>
	struct SignatureReader : IDisposable {
		ModuleDefMD readerModule;
		IImageStream reader;
		RecursionCounter recursionCounter;

		/// <summary>
		/// Reads a signature from the #Blob stream
		/// </summary>
		/// <param name="readerModule">Reader module</param>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public static CallingConventionSig ReadSig(ModuleDefMD readerModule, uint sig) {
			try {
				using (var reader = new SignatureReader(readerModule, sig)) {
					if (reader.reader.Length == 0)
						return null;
					var csig = reader.ReadSig();
					if (csig != null)
						csig.ExtraData = reader.GetExtraData();
					return csig;
				}
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
		public static TypeSig ReadTypeSig(ModuleDefMD readerModule, uint sig) {
			try {
				using (var reader = new SignatureReader(readerModule, sig))
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
		public static TypeSig ReadTypeSig(ModuleDefMD readerModule, uint sig, out byte[] extraData) {
			try {
				using (var reader = new SignatureReader(readerModule, sig)) {
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
		SignatureReader(ModuleDefMD readerModule, uint sig) {
			this.readerModule = readerModule;
			this.reader = readerModule.BlobStream.CreateStream(sig);
			this.recursionCounter = new RecursionCounter();
		}

		byte[] GetExtraData() {
			if (reader.Position >= reader.Length)
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
			case CallingConvention.NativeVarArg:
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

			uint numParams;
			if (!reader.ReadCompressedUInt32(out numParams))
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
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if invalid element type</returns>
		TypeSig ReadType() {
			if (!recursionCounter.Increment())
				return null;

			uint num;
			TypeSig nextType, result = null;
			switch ((ElementType)reader.ReadByte()) {
			case ElementType.Void: result = readerModule.CorLibTypes.Void; break;
			case ElementType.Boolean: result = readerModule.CorLibTypes.Boolean; break;
			case ElementType.Char: result = readerModule.CorLibTypes.Char; break;
			case ElementType.I1: result = readerModule.CorLibTypes.SByte; break;
			case ElementType.U1: result = readerModule.CorLibTypes.Byte; break;
			case ElementType.I2: result = readerModule.CorLibTypes.Int16; break;
			case ElementType.U2: result = readerModule.CorLibTypes.UInt16; break;
			case ElementType.I4: result = readerModule.CorLibTypes.Int32; break;
			case ElementType.U4: result = readerModule.CorLibTypes.UInt32; break;
			case ElementType.I8: result = readerModule.CorLibTypes.Int64; break;
			case ElementType.U8: result = readerModule.CorLibTypes.UInt64; break;
			case ElementType.R4: result = readerModule.CorLibTypes.Single; break;
			case ElementType.R8: result = readerModule.CorLibTypes.Double; break;
			case ElementType.String: result = readerModule.CorLibTypes.String; break;
			case ElementType.TypedByRef: result = readerModule.CorLibTypes.TypedReference; break;
			case ElementType.I: result = readerModule.CorLibTypes.IntPtr; break;
			case ElementType.U: result = readerModule.CorLibTypes.UIntPtr; break;
			case ElementType.Object: result = readerModule.CorLibTypes.Object; break;

			case ElementType.Ptr: result = new PtrSig(ReadType()); break;
			case ElementType.ByRef: result = new ByRefSig(ReadType()); break;
			case ElementType.ValueType: result = new ValueTypeSig(ReadTypeDefOrRef()); break;
			case ElementType.Class: result = new ClassSig(ReadTypeDefOrRef()); break;
			case ElementType.FnPtr: result = new FnPtrSig(ReadSig()); break;
			case ElementType.SZArray: result = new SZArraySig(ReadType()); break;
			case ElementType.CModReqd: result = new CModReqdSig(ReadTypeDefOrRef(), ReadType()); break;
			case ElementType.CModOpt: result = new CModOptSig(ReadTypeDefOrRef(), ReadType()); break;
			case ElementType.Sentinel: result = new SentinelSig(); break;
			case ElementType.Pinned: result = new PinnedSig(ReadType()); break;

			case ElementType.Var:
				if (!reader.ReadCompressedUInt32(out num))
					break;
				result = new GenericVar(num);
				break;

			case ElementType.MVar:
				if (!reader.ReadCompressedUInt32(out num))
					break;
				result = new GenericMVar(num);
				break;

			case ElementType.ValueArray:
				nextType = ReadType();
				if (!reader.ReadCompressedUInt32(out num))
					break;
				result = new ValueArraySig(nextType, num);
				break;

			case ElementType.Module:
				if (!reader.ReadCompressedUInt32(out num))
					break;
				result = new ModuleSig(num, ReadType());
				break;

			case ElementType.GenericInst:
				nextType = ReadType();
				if (!reader.ReadCompressedUInt32(out num))
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
				if (!reader.ReadCompressedUInt32(out rank))
					break;
				if (rank == 0) {
					result = new ArraySig(nextType, rank);
					break;
				}
				if (!reader.ReadCompressedUInt32(out num))
					break;
				var sizes = new List<uint>((int)num);
				for (uint i = 0; i < num; i++) {
					uint size;
					if (!reader.ReadCompressedUInt32(out size))
						goto exit;
					sizes.Add(size);
				}
				if (!reader.ReadCompressedUInt32(out num))
					break;
				var lowerBounds = new List<int>((int)num);
				for (uint i = 0; i < num; i++) {
					int size;
					if (!reader.ReadCompressedInt32(out size))
						goto exit;
					lowerBounds.Add(size);
				}
				result = new ArraySig(nextType, rank, sizes, lowerBounds);
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
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
			uint codedToken;
			if (!reader.ReadCompressedUInt32(out codedToken))
				return null;
			//TODO: Perhaps we should read this lazily. If so, update ValueTypeSig, etc to take a coded token
			return readerModule.ResolveTypeDefOrRef(codedToken);
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (reader != null)
				reader.Dispose();
		}
	}
}
