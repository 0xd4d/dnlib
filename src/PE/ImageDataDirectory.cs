// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_DATA_DIRECTORY PE section
	/// </summary>
	[DebuggerDisplay("{virtualAddress} {dataSize}")]
	public sealed class ImageDataDirectory : FileSection {
		readonly RVA virtualAddress;
		readonly uint dataSize;

		/// <summary>
		/// Returns the IMAGE_DATA_DIRECTORY.VirtualAddress field
		/// </summary>
		public RVA VirtualAddress => virtualAddress;

		/// <summary>
		/// Returns the IMAGE_DATA_DIRECTORY.Size field
		/// </summary>
		public uint Size => dataSize;

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
		public ImageDataDirectory(ref DataReader reader, bool verify) {
			SetStartOffset(ref reader);
			virtualAddress = (RVA)reader.ReadUInt32();
			dataSize = reader.ReadUInt32();
			SetEndoffset(ref reader);
		}
	}
}
