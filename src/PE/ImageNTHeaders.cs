// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_NT_HEADERS PE section
	/// </summary>
	public sealed class ImageNTHeaders : FileSection {
		readonly uint signature;
		readonly ImageFileHeader imageFileHeader;
		readonly IImageOptionalHeader imageOptionalHeader;

		/// <summary>
		/// Returns the IMAGE_NT_HEADERS.Signature field
		/// </summary>
		public uint Signature => signature;

		/// <summary>
		/// Returns the IMAGE_NT_HEADERS.FileHeader field
		/// </summary>
		public ImageFileHeader FileHeader => imageFileHeader;

		/// <summary>
		/// Returns the IMAGE_NT_HEADERS.OptionalHeader field
		/// </summary>
		public IImageOptionalHeader OptionalHeader => imageOptionalHeader;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageNTHeaders(ref DataReader reader, bool verify) {
			SetStartOffset(ref reader);
			signature = reader.ReadUInt32();
			// Mono only checks the low 2 bytes
			if (verify && (ushort)signature != 0x4550)
				throw new BadImageFormatException("Invalid NT headers signature");
			imageFileHeader = new ImageFileHeader(ref reader, verify);
			imageOptionalHeader = CreateImageOptionalHeader(ref reader, verify);
			SetEndoffset(ref reader);
		}

		/// <summary>
		/// Creates an IImageOptionalHeader
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of the optional header</param>
		/// <param name="verify">Verify section</param>
		/// <returns>The created IImageOptionalHeader</returns>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		IImageOptionalHeader CreateImageOptionalHeader(ref DataReader reader, bool verify) {
			ushort magic = reader.ReadUInt16();
			reader.Position -= 2;
			switch (magic) {
			case 0x010B: return new ImageOptionalHeader32(ref reader, imageFileHeader.SizeOfOptionalHeader, verify);
			case 0x020B: return new ImageOptionalHeader64(ref reader, imageFileHeader.SizeOfOptionalHeader, verify);
			default: throw new BadImageFormatException("Invalid optional header magic");
			}
		}
	}
}
