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
		public uint NTHeadersOffset {
			get { return ntHeadersOffset; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageDosHeader(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			ushort sig = reader.ReadUInt16();
			if (verify && sig != 0x5A4D)
				throw new BadImageFormatException("Invalid DOS signature");
			reader.Position = (long)startOffset + 0x3C;
			this.ntHeadersOffset = reader.ReadUInt32();
			SetEndoffset(reader);
		}
	}
}
