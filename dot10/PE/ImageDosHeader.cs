using System;
using System.IO;

namespace dot10.PE {
	/// <summary>
	/// Represents the IMAGE_DOS_HEADER PE section
	/// </summary>
	class ImageDosHeader : FileSection {
		uint ntHeadersOffset;

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
		public ImageDosHeader(BinaryReader reader, bool verify) {
			SetStartOffset(reader);
			ushort sig = reader.ReadUInt16();
			if (verify && sig != 0x5A4D)
				throw new BadImageFormatException("Invalid DOS signature");
			reader.BaseStream.Position = startOffset.Value + 0x3C;
			this.ntHeadersOffset = reader.ReadUInt32();
			SetEndoffset(reader);
		}
	}
}
