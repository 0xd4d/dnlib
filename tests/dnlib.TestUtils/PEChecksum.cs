// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;

namespace dnlib.TestUtils {
	/// <summary>
	/// Calculates the checksum of a PE file like Windows does (<c>CheckSumMappedFile</c>), without dnlib.
	/// Windows only checks it when it loads drivers and some system DLLs.
	/// </summary>
	public static class PEChecksum {
		// The offset of the CheckSum field in the optional header. It's the same in PE32 and PE32+ files.
		const int checksumOffsetInOptionalHeader = 64;

		/// <summary>
		/// Reads the checksum that's stored in the PE file's optional header
		/// </summary>
		public static uint Read(byte[] image) => LittleEndian.ToUInt32(image, GetChecksumOffset(image));

		/// <summary>
		/// Calculates the checksum the PE file should have
		/// </summary>
		public static uint Calculate(byte[] image) {
			int checksumOffset = GetChecksumOffset(image);

			// Add all 16-bit numbers of the file. If the sum doesn't fit in 16 bits, the bits above are added
			// to it again. The 4 bytes of the stored checksum count as zeros, and so does the missing byte of
			// the last number if the file's length is odd.
			uint sum = 0;
			for (int offset = 0; offset < image.Length; offset += 2) {
				uint low = GetByte(image, offset, checksumOffset);
				uint high = GetByte(image, offset + 1, checksumOffset);
				sum += low | (high << 8);
				sum = (sum & 0xFFFF) + (sum >> 16);
			}
			return sum + (uint)image.Length;
		}

		static uint GetByte(byte[] image, int offset, int checksumOffset) {
			if (offset >= image.Length)
				return 0;
			if (offset >= checksumOffset && offset < checksumOffset + 4)
				return 0;
			return image[offset];
		}

		/// <summary>
		/// Gets the offset of the stored checksum in the PE file
		/// </summary>
		public static int GetChecksumOffset(byte[] image) {
			using (var peReader = new PEReader(ImmutableArray.Create(image)))
				return peReader.PEHeaders.PEHeaderStartOffset + checksumOffsetInOptionalHeader;
		}
	}
}
