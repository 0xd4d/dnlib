// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_DOS_HEADER PE section
	/// </summary>
	public sealed class ImageDosHeader : FileSection {
		readonly uint ntHeadersOffset;

		/// <summary>
		/// File offset of the NT headers
		/// </summary>
		public uint NTHeadersOffset => ntHeadersOffset;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageDosHeader(ref DataReader reader, bool verify) {
			SetStartOffset(ref reader);
			ushort sig = reader.ReadUInt16();
			if (verify && sig != 0x5A4D)
				throw new BadImageFormatException("Invalid DOS signature");
			reader.Position = (uint)startOffset + 0x3C;
			ntHeadersOffset = reader.ReadUInt32();
			SetEndoffset(ref reader);
		}
	}
}
