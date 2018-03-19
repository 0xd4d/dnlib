// dnlib: See LICENSE.txt for more info

using System;
using System.IO;

namespace dnlib.DotNet.Writer {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
	/// <summary>
	/// Writes data
	/// </summary>
	public sealed class DataWriter {
		readonly Stream stream;
		readonly byte[] buffer;
		const int BUFFER_LEN = 8;

		internal Stream InternalStream => stream;

		public long Position {
			get => stream.Position;
			set => stream.Position = value;
		}

		public DataWriter(Stream stream) {
			if (stream == null)
				ThrowArgumentNullException(nameof(stream));
			this.stream = stream;
			buffer = new byte[BUFFER_LEN];
		}

		static void ThrowArgumentNullException(string paramName) => throw new ArgumentNullException(paramName);
		static void ThrowArgumentOutOfRangeException(string message) => throw new ArgumentOutOfRangeException(message);

		public void WriteBoolean(bool value) => stream.WriteByte(value ? (byte)1 : (byte)0);
		public void WriteSByte(sbyte value) => stream.WriteByte((byte)value);
		public void WriteByte(byte value) => stream.WriteByte(value);

		public void WriteInt16(short value) {
			var buffer = this.buffer;
			buffer[0] = (byte)value;
			buffer[1] = (byte)(value >> 8);
			stream.Write(buffer, 0, 2);
		}

		public void WriteUInt16(ushort value) {
			var buffer = this.buffer;
			buffer[0] = (byte)value;
			buffer[1] = (byte)(value >> 8);
			stream.Write(buffer, 0, 2);
		}

		public void WriteInt32(int value) {
			var buffer = this.buffer;
			buffer[0] = (byte)value;
			buffer[1] = (byte)(value >> 8);
			buffer[2] = (byte)(value >> 16);
			buffer[3] = (byte)(value >> 24);
			stream.Write(buffer, 0, 4);
		}

		public void WriteUInt32(uint value) {
			var buffer = this.buffer;
			buffer[0] = (byte)value;
			buffer[1] = (byte)(value >> 8);
			buffer[2] = (byte)(value >> 16);
			buffer[3] = (byte)(value >> 24);
			stream.Write(buffer, 0, 4);
		}

		public void WriteInt64(long value) {
			var buffer = this.buffer;
			buffer[0] = (byte)value;
			buffer[1] = (byte)(value >> 8);
			buffer[2] = (byte)(value >> 16);
			buffer[3] = (byte)(value >> 24);
			buffer[4] = (byte)(value >> 32);
			buffer[5] = (byte)(value >> 40);
			buffer[6] = (byte)(value >> 48);
			buffer[7] = (byte)(value >> 56);
			stream.Write(buffer, 0, 8);
		}

		public void WriteUInt64(ulong value) {
			var buffer = this.buffer;
			buffer[0] = (byte)value;
			buffer[1] = (byte)(value >> 8);
			buffer[2] = (byte)(value >> 16);
			buffer[3] = (byte)(value >> 24);
			buffer[4] = (byte)(value >> 32);
			buffer[5] = (byte)(value >> 40);
			buffer[6] = (byte)(value >> 48);
			buffer[7] = (byte)(value >> 56);
			stream.Write(buffer, 0, 8);
		}

		public unsafe void WriteSingle(float value) {
			uint tmp = *(uint*)&value;
			var buffer = this.buffer;
			buffer[0] = (byte)tmp;
			buffer[1] = (byte)(tmp >> 8);
			buffer[2] = (byte)(tmp >> 16);
			buffer[3] = (byte)(tmp >> 24);
			stream.Write(buffer, 0, 4);
		}

		public unsafe void WriteDouble(double value) {
			ulong tmp = *(ulong*)&value;
			var buffer = this.buffer;
			buffer[0] = (byte)tmp;
			buffer[1] = (byte)(tmp >> 8);
			buffer[2] = (byte)(tmp >> 16);
			buffer[3] = (byte)(tmp >> 24);
			buffer[4] = (byte)(tmp >> 32);
			buffer[5] = (byte)(tmp >> 40);
			buffer[6] = (byte)(tmp >> 48);
			buffer[7] = (byte)(tmp >> 56);
			stream.Write(buffer, 0, 8);
		}

		public void WriteBytes(byte[] source) => stream.Write(source, 0, source.Length);
		public void WriteBytes(byte[] source, int index, int length) => stream.Write(source, index, length);

		public void WriteCompressedUInt32(uint value) {
			if (value <= 0x7F)
				stream.WriteByte((byte)value);
			else if (value <= 0x3FFF) {
				stream.WriteByte((byte)((value >> 8) | 0x80));
				stream.WriteByte((byte)value);
			}
			else if (value <= 0x1FFFFFFF) {
				stream.WriteByte((byte)((value >> 24) | 0xC0));
				stream.WriteByte((byte)(value >> 16));
				stream.WriteByte((byte)(value >> 8));
				stream.WriteByte((byte)value);
			}
			else
				ThrowArgumentOutOfRangeException("UInt32 value can't be compressed");
		}

		public void WriteCompressedInt32(int value) {
			// This is almost identical to compressing a UInt32, except that we first
			// recode value so the sign bit is in bit 0. Then we compress it the same
			// way a UInt32 is compressed.
			uint sign = (uint)value >> 31;
			if (-0x40 <= value && value <= 0x3F) {
				uint v = (uint)((value & 0x3F) << 1) | sign;
				stream.WriteByte((byte)v);
			}
			else if (-0x2000 <= value && value <= 0x1FFF) {
				uint v = ((uint)(value & 0x1FFF) << 1) | sign;
				stream.WriteByte((byte)((v >> 8) | 0x80));
				stream.WriteByte((byte)v);
			}
			else if (-0x10000000 <= value && value <= 0x0FFFFFFF) {
				uint v = ((uint)(value & 0x0FFFFFFF) << 1) | sign;
				stream.WriteByte((byte)((v >> 24) | 0xC0));
				stream.WriteByte((byte)(v >> 16));
				stream.WriteByte((byte)(v >> 8));
				stream.WriteByte((byte)v);
			}
			else
				ThrowArgumentOutOfRangeException("Int32 value can't be compressed");
		}

		public static int GetCompressedUInt32Length(uint value) {
			if (value <= 0x7F)
				return 1;
			if (value <= 0x3FFF)
				return 2;
			if (value <= 0x1FFFFFFF)
				return 4;
			ThrowArgumentOutOfRangeException("UInt32 value can't be compressed");
			return 0;
		}
	}
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
}
