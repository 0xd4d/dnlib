// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.TestUtils {
	/// <summary>
	/// Converts numbers to and from the bytes of a file. All files that dnlib reads and writes are little
	/// endian: the first byte of a number is its lowest byte. The tests use this instead of
	/// <see cref="BitConverter"/>, which uses the byte order of the CPU, so that they also pass on a big endian
	/// CPU (eg. s390x). The methods are the ones of <see cref="BitConverter"/>.
	/// </summary>
	public static class LittleEndian {
		public static byte[] GetBytes(bool value) => new byte[] { value ? (byte)1 : (byte)0 };
		public static byte[] GetBytes(char value) => GetBytes((ushort)value);
		public static byte[] GetBytes(short value) => GetBytes((ushort)value);
		public static byte[] GetBytes(ushort value) => new byte[] { (byte)value, (byte)(value >> 8) };
		public static byte[] GetBytes(int value) => GetBytes((uint)value);
		public static byte[] GetBytes(uint value) => new byte[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
		public static byte[] GetBytes(long value) => GetBytes((ulong)value);

		public static byte[] GetBytes(ulong value) {
			var bytes = new byte[8];
			for (int i = 0; i < bytes.Length; i++)
				bytes[i] = (byte)(value >> (i * 8));
			return bytes;
		}

		public static unsafe byte[] GetBytes(float value) => GetBytes(*(uint*)&value);
		public static byte[] GetBytes(double value) => GetBytes(BitConverter.DoubleToInt64Bits(value));

		public static ushort ToUInt16(byte[] bytes, int index) => (ushort)(bytes[index] | (bytes[index + 1] << 8));
		public static short ToInt16(byte[] bytes, int index) => (short)ToUInt16(bytes, index);

		public static uint ToUInt32(byte[] bytes, int index) =>
			bytes[index] | ((uint)bytes[index + 1] << 8) | ((uint)bytes[index + 2] << 16) | ((uint)bytes[index + 3] << 24);

		public static int ToInt32(byte[] bytes, int index) => (int)ToUInt32(bytes, index);
		public static ulong ToUInt64(byte[] bytes, int index) => ToUInt32(bytes, index) | ((ulong)ToUInt32(bytes, index + 4) << 32);
		public static long ToInt64(byte[] bytes, int index) => (long)ToUInt64(bytes, index);
	}
}
