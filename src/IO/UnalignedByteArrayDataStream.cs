// dnlib: See LICENSE.txt for more info

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace dnlib.IO {
	sealed unsafe class UnalignedByteArrayDataStream : DataStream {
		readonly byte[] data;

		public UnalignedByteArrayDataStream(byte[] data) => this.data = data;

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
			fixed (byte* p = data)
				return *(uint*)(p + offset);
		}

		public override ulong ReadUInt64(uint offset) {
			fixed (byte* p = data)
				return *(ulong*)(p + offset);
		}

		public override float ReadSingle(uint offset) {
			fixed (byte* p = data)
				return *(float*)(p + offset);
		}

		public override double ReadDouble(uint offset) {
			fixed (byte* p = data)
				return *(double*)(p + offset);
		}

		public override Guid ReadGuid(uint offset) {
			fixed (byte* p = data)
				return *(Guid*)(p + offset);
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
