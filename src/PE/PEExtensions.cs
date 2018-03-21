// dnlib: See LICENSE.txt for more info

using System;
using System.IO;

namespace dnlib.PE {
	/// <summary>
	/// Extension methods
	/// </summary>
	public static partial class PEExtensions {
		/// <summary>
		/// Calculates a PE checksum
		/// </summary>
		/// <param name="stream">PE image stream positioned at the MZ bytes</param>
		/// <param name="length">Length of image</param>
		/// <param name="checkSumOffset">Offset of checksum</param>
		/// <returns>PE checksum</returns>
		internal static uint CalculatePECheckSum(this Stream stream, long length, long checkSumOffset) {
			if ((length & 1) != 0)
				ThrowInvalidOperationException("Invalid PE length");
			var buffer = new byte[(int)Math.Min(length, 0x2000)];
			uint checkSum = 0;
			checkSum = CalculatePECheckSum(stream, checkSumOffset, checkSum, buffer);
			const int ChecksumFieldSize = 4;
			stream.Position += ChecksumFieldSize;
			checkSum = CalculatePECheckSum(stream, length - checkSumOffset - ChecksumFieldSize, checkSum, buffer);
			ulong cks = (ulong)checkSum + (ulong)length;
			return (uint)cks + (uint)(cks >> 32);
		}

		static uint CalculatePECheckSum(Stream stream, long length, uint checkSum, byte[] buffer) {
			for (long offset = 0; offset < length;) {
				int len = (int)Math.Min(length - offset, buffer.Length);
				int count = stream.Read(buffer, 0, len);
				if (count != len)
					ThrowInvalidOperationException("Couldn't read all bytes");

				for (int i = 0; i < count;) {
					checkSum += buffer[i++] | ((uint)buffer[i++] << 8);
					checkSum = (ushort)(checkSum + (checkSum >> 16));
				}

				offset += count;
			}
			return checkSum;
		}

		static void ThrowInvalidOperationException(string message) => throw new InvalidOperationException(message);
	}
}
