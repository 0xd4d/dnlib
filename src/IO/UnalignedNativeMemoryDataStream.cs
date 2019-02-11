// dnlib: See LICENSE.txt for more info

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace dnlib.IO {
	sealed unsafe class UnalignedNativeMemoryDataStream : DataStream {
		readonly byte* data;

		public UnalignedNativeMemoryDataStream(byte* data) => this.data = data;

		public override void ReadBytes(uint offset, void* destination, int length) {
			var ps = data + offset;
			var pd = (byte*)destination;
			int count = length / 4;
			length = length % 4;
			for (int i = 0; i < count; i++) {
				//TODO: Align one of the pointers. destination is more likely to be aligned
				*(uint*)pd = *(uint*)ps;
				pd += 4;
				ps += 4;
			}
			for (int i = 0; i < length; i++, ps++, pd++)
				*pd = *ps;
		}

		public override void ReadBytes(uint offset, byte[] destination, int destinationIndex, int length) =>
			Marshal.Copy((IntPtr)(data + offset), destination, destinationIndex, length);

		public override byte ReadByte(uint offset) => *(data + offset);
		public override ushort ReadUInt16(uint offset) => *(ushort*)(data + offset);
		public override uint ReadUInt32(uint offset) => *(uint*)(data + offset);
		public override ulong ReadUInt64(uint offset) => *(ulong*)(data + offset);
		public override float ReadSingle(uint offset) => *(float*)(data + offset);
		public override double ReadDouble(uint offset) => *(double*)(data + offset);
		public override Guid ReadGuid(uint offset) => *(Guid*)(data + offset);
		public override string ReadUtf16String(uint offset, int chars) => new string((char*)(data + offset), 0, chars);
		public override string ReadString(uint offset, int length, Encoding encoding) => new string((sbyte*)(data + offset), 0, length, encoding);

		public override bool TryGetOffsetOf(uint offset, uint endOffset, byte value, out uint valueOffset) {
			var pd = data;

			// If this code gets updated, also update the other DataStream implementations

			byte* p = pd + offset;

			uint count = (endOffset - offset) / 4;
			for (uint i = 0; i < count; i++) {
				if (*p == value) {
					valueOffset = (uint)(p - pd);
					return true;
				}
				p++;
				if (*p == value) {
					valueOffset = (uint)(p - pd);
					return true;
				}
				p++;
				if (*p == value) {
					valueOffset = (uint)(p - pd);
					return true;
				}
				p++;
				if (*p == value) {
					valueOffset = (uint)(p - pd);
					return true;
				}
				p++;
			}

			byte* pe = pd + endOffset;
			while (p != pe) {
				if (*p == value) {
					valueOffset = (uint)(p - pd);
					return true;
				}
				p++;
			}
			valueOffset = 0;
			return false;
		}
	}
}
