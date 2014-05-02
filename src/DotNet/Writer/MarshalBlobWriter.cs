/*
    Copyright (C) 2012-2014 de4dot@gmail.com

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

using System;
using System.IO;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Writes field marshal blobs
	/// </summary>
	public struct MarshalBlobWriter : IDisposable, IFullNameCreatorHelper {
		readonly ModuleDef module;
		readonly MemoryStream outStream;
		readonly BinaryWriter writer;
		readonly IWriterError helper;

		/// <summary>
		/// Creates a field marshal blob from <paramref name="marshalType"/>
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="marshalType">Marshal type</param>
		/// <param name="helper">Helps this class</param>
		/// <returns>A field marshal blob or <c>null</c> if <paramref name="marshalType"/> is
		/// <c>null</c></returns>
		public static byte[] Write(ModuleDef module, MarshalType marshalType, IWriterError helper) {
			using (var writer = new MarshalBlobWriter(module, helper))
				return writer.Write(marshalType);
		}

		MarshalBlobWriter(ModuleDef module, IWriterError helper) {
			this.module = module;
			this.outStream = new MemoryStream();
			this.writer = new BinaryWriter(outStream);
			this.helper = helper;
		}

		byte[] Write(MarshalType marshalType) {
			if (marshalType == null)
				return null;

			var type = marshalType.NativeType;
			if (type != NativeType.RawBlob) {
				if ((uint)type > byte.MaxValue)
					helper.Error("Invalid MarshalType.NativeType");
				writer.Write((byte)type);
			}
			bool canWrite = true;
			switch (type) {
			case NativeType.FixedSysString:
				var fixedSysString = (FixedSysStringMarshalType)marshalType;
				if (fixedSysString.IsSizeValid)
					WriteCompressedUInt32((uint)fixedSysString.Size);
				break;

			case NativeType.SafeArray:
				var safeArray = (SafeArrayMarshalType)marshalType;
				if (UpdateCanWrite(safeArray.IsVariantTypeValid, "VariantType", ref canWrite))
					WriteCompressedUInt32((uint)safeArray.VariantType);
				if (UpdateCanWrite(safeArray.IsUserDefinedSubTypeValid, "UserDefinedSubType", ref canWrite))
					Write(safeArray.UserDefinedSubType.AssemblyQualifiedName);
				break;

			case NativeType.FixedArray:
				var fixedArray = (FixedArrayMarshalType)marshalType;
				if (UpdateCanWrite(fixedArray.IsSizeValid, "Size", ref canWrite))
					WriteCompressedUInt32((uint)fixedArray.Size);
				if (UpdateCanWrite(fixedArray.IsElementTypeValid, "ElementType", ref canWrite))
					WriteCompressedUInt32((uint)fixedArray.ElementType);
				break;

			case NativeType.Array:
				var array = (ArrayMarshalType)marshalType;
				if (UpdateCanWrite(array.IsElementTypeValid, "ElementType", ref canWrite))
					WriteCompressedUInt32((uint)array.ElementType);
				if (UpdateCanWrite(array.IsParamNumberValid, "ParamNumber", ref canWrite))
					WriteCompressedUInt32((uint)array.ParamNumber);
				if (UpdateCanWrite(array.IsSizeValid, "Size", ref canWrite))
					WriteCompressedUInt32((uint)array.Size);
				if (UpdateCanWrite(array.IsFlagsValid, "Flags", ref canWrite))
					WriteCompressedUInt32((uint)array.Flags);
				break;

			case NativeType.CustomMarshaler:
				var custMarshaler = (CustomMarshalType)marshalType;
				Write(custMarshaler.Guid);
				Write(custMarshaler.NativeTypeName);
				var cm = custMarshaler.CustomMarshaler;
				var cmName = cm == null ? string.Empty : FullNameCreator.AssemblyQualifiedName(cm, this);
				Write(cmName);
				Write(custMarshaler.Cookie);
				break;

			case NativeType.IUnknown:
			case NativeType.IDispatch:
			case NativeType.IntF:
				var iface = (InterfaceMarshalType)marshalType;
				if (iface.IsIidParamIndexValid)
					WriteCompressedUInt32((uint)iface.IidParamIndex);
				break;

			case NativeType.RawBlob:
				var data = ((RawMarshalType)marshalType).Data;
				if (data != null)
					writer.Write(data);
				break;

			default:
				break;
			}

			writer.Flush();
			return outStream.ToArray();
		}

		bool UpdateCanWrite(bool isValid, string field, ref bool canWriteMore) {
			if (!canWriteMore) {
				if (isValid)
					helper.Error(string.Format("MarshalType field {0} is valid even though a previous field was invalid", field));
				return canWriteMore;
			}

			if (!isValid)
				canWriteMore = false;

			return canWriteMore;
		}

		uint WriteCompressedUInt32(uint value) {
			return writer.WriteCompressedUInt32(helper, value);
		}

		void Write(UTF8String s) {
			writer.Write(helper, s);
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (outStream != null)
				outStream.Dispose();
		}

		bool IFullNameCreatorHelper.MustUseAssemblyName(IType type) {
			return FullNameCreator.MustUseAssemblyName(module, type);
		}
	}
}
