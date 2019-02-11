// dnlib: See LICENSE.txt for more info

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace dnlib.IO {
	sealed unsafe class AlignedByteArrayDataStream : DataStream {
		readonly byte[] data;

		public AlignedByteArrayDataStream(byte[] data) => this.data = data;

		public override void ReadBytes(uint offset, void* destination, int length) =>
			Marshal.Copy(data, (int)offset, (IntPtr)destination, length);

		public override void ReadBytes(uint offset, byte[] destination, int destinationIndex, int length) =>
			Array.Copy(data, (int)offset, destination, destinationIndex, length);

		public override byte ReadByte(uint offset) => data[(int)offset];

		public override ushort ReadUInt16(uint offset) {
			int i = (int)offset;
			var data = this.data;
			return (ushort)(data[i++] | (data[i] << 8));
		}

		public override uint ReadUInt32(uint offset) {
			int i = (int)offset;
			var data = this.data;
			return data[i++] | ((uint)data[i++] << 8) | ((uint)data[i++] << 16) | ((uint)data[i] << 24);
		}

		public override ulong ReadUInt64(uint offset) {
			int i = (int)offset;
			var data = this.data;
			return data[i++] | ((ulong)data[i++] << 8) | ((ulong)data[i++] << 16) | ((ulong)data[i++] << 24) |
				((ulong)data[i++] << 32) | ((ulong)data[i++] << 40) | ((ulong)data[i++] << 48) | ((ulong)data[i] << 56);
		}

		public override float ReadSingle(uint offset) {
			int i = (int)offset;
			var data = this.data;
			uint value = data[i++] | ((uint)data[i++] << 8) | ((uint)data[i++] << 16) | ((uint)data[i] << 24);
			return *(float*)&value;
		}

		public override double ReadDouble(uint offset) {
			int i = (int)offset;
			var data = this.data;
			ulong value = data[i++] | ((ulong)data[i++] << 8) | ((ulong)data[i++] << 16) | ((ulong)data[i++] << 24) |
				((ulong)data[i++] << 32) | ((ulong)data[i++] << 40) | ((ulong)data[i++] << 48) | ((ulong)data[i] << 56);
			return *(double*)&value;
		}

		public override string ReadUtf16String(uint offset, int chars) {
			fixed (byte* p = data)
				return new string((char*)(p + offset), 0, chars);
		}

		public override string ReadString(uint offset, int length, Encoding encoding) {
			fixed (byte* p = data)
				return new string((sbyte*)(p + offset), 0, length, encoding);
		}

		public override bool TryGetOffsetOf(uint offset, uint endOffset, byte value, out uint valueOffset) {
			fixed (byte* pd = data) {
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
}
