using System;
using System.IO;

namespace dot10.PE {
	/// <summary>
	/// Represents the IMAGE_DATA_DIRECTORY PE section
	/// </summary>
	class ImageDataDirectory : FileSection {
		RVA virtualAddress;
		uint dataSize;

		/// <summary>
		/// Default constructor
		/// </summary>
		public ImageDataDirectory() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageDataDirectory(BinaryReader reader, bool verify) {
			SetStartOffset(reader);
			this.virtualAddress = new RVA(reader.ReadUInt32());
			this.dataSize = reader.ReadUInt32();
			SetEndoffset(reader);
		}
	}
}
