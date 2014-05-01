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
using dnlib.IO;

namespace dnlib.DotNet {
	/// <summary>
	/// Reads <see cref="MarshalType"/>s
	/// </summary>
	public struct MarshalBlobReader : IDisposable {
		readonly IBinaryReader reader;

		/// <summary>
		/// Reads a <see cref="MarshalType"/> from the <c>#Blob</c> heap
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="sig">Blob offset</param>
		/// <returns>A new <see cref="MarshalType"/> instance</returns>
		public static MarshalType Read(ModuleDefMD module, uint sig) {
			return Read(module.BlobStream.CreateStream(sig));
		}

		/// <summary>
		/// Reads a <see cref="MarshalType"/> from <paramref name="data"/>
		/// </summary>
		/// <param name="data">Marshal data</param>
		/// <returns>A new <see cref="MarshalType"/> instance</returns>
		public static MarshalType Read(byte[] data) {
			return Read(MemoryImageStream.Create(data));
		}

		/// <summary>
		/// Reads a <see cref="MarshalType"/> from <see cref="reader"/>
		/// </summary>
		/// <param name="reader">A reader that will be owned by us</param>
		/// <returns>A new <see cref="MarshalType"/> instance</returns>
		public static MarshalType Read(IBinaryReader reader) {
			using (var marshalReader = new MarshalBlobReader(reader))
				return marshalReader.Read();
		}

		MarshalBlobReader(IBinaryReader reader) {
			this.reader = reader;
		}

		MarshalType Read() {
			MarshalType returnValue;
			try {
				var nativeType = (NativeType)reader.ReadByte();
				NativeType nt;
				int size;
				switch (nativeType) {
				case NativeType.FixedSysString:
					size = CanRead() ? (int)reader.ReadCompressedUInt32() : -1;
					returnValue = new FixedSysStringMarshalType(size);
					break;

				case NativeType.SafeArray:
					var vt = CanRead() ? (VariantType)reader.ReadCompressedUInt32() : VariantType.NotInitialized;
					var name = CanRead() ? ReadUTF8String() : null;
					returnValue = new SafeArrayMarshalType(vt, name);
					break;

				case NativeType.FixedArray:
					size = CanRead() ? (int)reader.ReadCompressedUInt32() : -1;
					nt = CanRead() ? (NativeType)reader.ReadCompressedUInt32() : NativeType.NotInitialized;
					returnValue = new FixedArrayMarshalType(size, nt);
					break;

				case NativeType.Array:
					nt = CanRead() ? (NativeType)reader.ReadCompressedUInt32() : NativeType.NotInitialized;
					int paramNum = CanRead() ? (int)reader.ReadCompressedUInt32() : -1;
					size = CanRead() ? (int)reader.ReadCompressedUInt32() : -1;
					int flags = CanRead() ? (int)reader.ReadCompressedUInt32() : -1;
					returnValue = new ArrayMarshalType(nt, paramNum, size, flags);
					break;

				case NativeType.CustomMarshaler:
					var guid = ReadUTF8String();
					var nativeTypeName = ReadUTF8String();
					var custMarshalerName = ReadUTF8String();
					var cookie = ReadUTF8String();
					returnValue = new CustomMarshalType(guid, nativeTypeName, custMarshalerName, cookie);
					break;

				case NativeType.IUnknown:
				case NativeType.IDispatch:
				case NativeType.IntF:
					int iidParamIndex = CanRead() ? (int)reader.ReadCompressedUInt32() : -1;
					return new InterfaceMarshalType(nativeType, iidParamIndex);

				default:
					returnValue = new MarshalType(nativeType);
					break;
				}
			}
			catch {
				returnValue = new RawMarshalType(reader.ReadAllBytes());
			}

			return returnValue;
		}

		bool CanRead() {
			return reader.Position < reader.Length;
		}

		UTF8String ReadUTF8String() {
			uint len = reader.ReadCompressedUInt32();
			return len == 0 ? UTF8String.Empty : new UTF8String(reader.ReadBytes((int)len));
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (reader != null)
				reader.Dispose();
		}
	}
}
