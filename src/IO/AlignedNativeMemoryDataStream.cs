// dnlib: See LICENSE.txt for more info

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace dnlib.IO {
	sealed unsafe class AlignedNativeMemoryDataStream : DataStream {
		readonly byte* data;

		public AlignedNativeMemoryDataStream(byte* data) => this.data = data;

		public override void ReadBytes(uint offset, void* destination, int length) {
			var ps = data + offset;
			var pd = (byte*)destination;
			int count = length / 4;
			length = length % 4;
			for (int i = 0; i < count; i++) {
				*pd = *ps;
				pd++;
				ps++;

				*pd = *ps;
				pd++;
				ps++;

				*pd = *ps;
				pd++;
				ps++;

				*pd = *ps;
				pd++;
				ps++;
			}
			for (int i = 0; i < length; i++, ps++, pd++)
				*pd = *ps;
		}

		public override void ReadBytes(uint offset, byte[] destination, int destinationIndex, int length) =>
			Marshal.Copy((IntPtr)(data + offset), destination, destinationIndex, length);

		public override byte ReadByte(uint offset) => *(data + offset);

		public override ushort ReadUInt16(uint offset) {
			var p = data + offset;
			return (ushort)(*p++ | (*p << 8));
		}

		public override uint ReadUInt32(uint offset) {
			var p = data + offset;
			return *p++ | ((uint)*p++ << 8) | ((uint)*p++ << 16) | ((uint)*p << 24);
		}

		public override ulong ReadUInt64(uint offset) {
			var p = data + offset;
			return *p++ | ((ulong)*p++ << 8) | ((ulong)*p++ << 16) | ((ulong)*p++ << 24) |
				((ulong)*p++ << 32) | ((ulong)*p++ << 40) | ((ulong)*p++ << 48) | ((ulong)*p << 56);
		}

		public override float ReadSingle(uint offset) {
			var p = data + offset;
			uint value = *p++ | ((uint)*p++ << 8) | ((uint)*p++ << 16) | ((uint)*p << 24);
			return *(float*)&value;
		}

		public override double ReadDouble(uint offset) {
			var p = data + offset;
			ulong value = *p++ | ((ulong)*p++ << 8) | ((ulong)*p++ << 16) | ((ulong)*p++ << 24) |
				((ulong)*p++ << 32) | ((ulong)*p++ << 40) | ((ulong)*p++ << 48) | ((ulong)*p << 56);
			return *(double*)&value;
		}

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
