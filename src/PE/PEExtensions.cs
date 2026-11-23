// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.IO;

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
		/// <param name="checkSumOffset">Stream position of the checksum field</param>
		/// <returns>PE checksum</returns>
		internal static uint CalculatePECheckSum(this Stream stream, long length, long checkSumOffset) {
			// Offset relative to the start of the image
			long ckOffs = checkSumOffset - stream.Position;
			const int ChecksumFieldSize = 4;
			// The buffer size is even so every read except the last one is an even number of bytes. If the length
			// is odd, the last byte is hashed as a 16-bit word with a zero high byte (like Windows' CheckSumMappedFile)
			var buffer = new byte[(int)Math.Min((length + 1) & ~1L, 0x2000)];
			uint checkSum = 0;
			for (long offset = 0; offset < length;) {
				int len = (int)Math.Min(length - offset, buffer.Length);
				StreamUtils.ReadAll(stream, buffer, 0, len);

				// The checksum field is hashed as zeros
				long end = Math.Min(ckOffs + ChecksumFieldSize, offset + len);
				for (long i = Math.Max(ckOffs, offset); i < end; i++)
					buffer[i - offset] = 0;
				if ((len & 1) != 0)
					buffer[len] = 0;

				for (int i = 0; i < len; i += 2) {
					checkSum += buffer[i] | ((uint)buffer[i + 1] << 8);
					checkSum = (ushort)(checkSum + (checkSum >> 16));
				}

				offset += len;
			}
			ulong cks = (ulong)checkSum + (ulong)length;
			return (uint)cks + (uint)(cks >> 32);
		}
	}
}
