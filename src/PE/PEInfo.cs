// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Reads all PE sections from a PE stream, for more information see https://docs.microsoft.com/en-us/windows/win32/debug/pe-format
	/// </summary>
	sealed class PEInfo {
		readonly ImageDosHeader imageDosHeader;
		readonly ImageNTHeaders imageNTHeaders;
		readonly ImageSectionHeader[] imageSectionHeaders;

		/// <summary>
		/// Returns the DOS header
		/// </summary>
		public ImageDosHeader ImageDosHeader => imageDosHeader;

		/// <summary>
		/// Returns the NT headers
		/// </summary>
		public ImageNTHeaders ImageNTHeaders => imageNTHeaders;

		/// <summary>
		/// Returns the section headers
		/// </summary>
		public ImageSectionHeader[] ImageSectionHeaders => imageSectionHeaders;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify sections</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public PEInfo(ref DataReader reader, bool verify) {
			reader.Position = 0;
			imageDosHeader = new ImageDosHeader(ref reader, verify);

			if (verify && imageDosHeader.NTHeadersOffset == 0)
				throw new BadImageFormatException("Invalid NT headers offset");
			reader.Position = imageDosHeader.NTHeadersOffset;
			imageNTHeaders = new ImageNTHeaders(ref reader, verify);

			reader.Position = (uint)imageNTHeaders.OptionalHeader.StartOffset + imageNTHeaders.FileHeader.SizeOfOptionalHeader;
			int numSections = imageNTHeaders.FileHeader.NumberOfSections;
			if (numSections > 0) {
				// Mono doesn't verify the section count
				var tempReader = reader;
				tempReader.Position += 0x14;
				uint firstSectionOffset = tempReader.ReadUInt32();
				numSections = Math.Min(numSections, (int)((firstSectionOffset - reader.Position) / 0x28));
			}
			imageSectionHeaders = new ImageSectionHeader[numSections];
			for (int i = 0; i < imageSectionHeaders.Length; i++)
				imageSectionHeaders[i] = new ImageSectionHeader(ref reader, verify);
		}

		/// <summary>
		/// Returns the first <see cref="ImageSectionHeader"/> that has data at file offset
		/// <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">The file offset</param>
		/// <returns></returns>
		public ImageSectionHeader ToImageSectionHeader(FileOffset offset) {
			foreach (var section in imageSectionHeaders) {
				if ((uint)offset >= section.PointerToRawData && (uint)offset < section.PointerToRawData + section.SizeOfRawData)
					return section;
			}
			return null;
		}

		/// <summary>
		/// Returns the first <see cref="ImageSectionHeader"/> that has data at RVA
		/// <paramref name="rva"/>
		/// </summary>
		/// <param name="rva">The RVA</param>
		/// <returns></returns>
		public ImageSectionHeader ToImageSectionHeader(RVA rva) {
			uint alignment = imageNTHeaders.OptionalHeader.SectionAlignment;
			foreach (var section in imageSectionHeaders) {
				if (rva >= section.VirtualAddress && rva < section.VirtualAddress + DotNet.Utils.AlignUp(section.VirtualSize, alignment))
					return section;
			}
			return null;
		}

		/// <summary>
		/// Converts a <see cref="FileOffset"/> to an <see cref="RVA"/>, returns 0 if out of range
		/// </summary>
		/// <param name="offset">The file offset to convert</param>
		/// <returns>The RVA</returns>
		public RVA ToRVA(FileOffset offset) {
			// In pe headers
			if (imageSectionHeaders.Length == 0)
				return (RVA)offset;

			// In pe additional data, like digital signature, won't be loaded into memory
			var lastSection = imageSectionHeaders[imageSectionHeaders.Length - 1];
			if ((uint)offset > lastSection.PointerToRawData + lastSection.SizeOfRawData)
				return 0;

			// In a section
			var section = ToImageSectionHeader(offset);
			if (section is not null)
				return (uint)(offset - section.PointerToRawData) + section.VirtualAddress;

			// In pe headers
			return (RVA)offset;
		}

		/// <summary>
		/// Converts an <see cref="RVA"/> to a <see cref="FileOffset"/>, returns 0 if out of range
		/// </summary>
		/// <param name="rva">The RVA to convert</param>
		/// <returns>The file offset</returns>
		public FileOffset ToFileOffset(RVA rva) {
			// Check if rva is larger than memory layout size
			if ((uint)rva >= imageNTHeaders.OptionalHeader.SizeOfImage)
				return 0;

			var section = ToImageSectionHeader(rva);
			if (section is not null) {
				uint offset = rva - section.VirtualAddress;
				// Virtual size may be bigger than raw size and there may be no corresponding file offset to rva
				if (offset < section.SizeOfRawData)
					return (FileOffset)offset + section.PointerToRawData;
				return 0;
			}

			// If not in any section, rva is in pe headers and don't convert it
			return (FileOffset)rva;
		}

		static ulong AlignUp(ulong val, uint alignment) => (val + alignment - 1) & ~(ulong)(alignment - 1);

		/// <summary>
		/// Returns size of image rounded up to <see cref="IImageOptionalHeader.SectionAlignment"/>
		/// </summary>
		/// <remarks>It calculates the size itself, and does not return <see cref="IImageOptionalHeader.SizeOfImage"/></remarks>
		/// <returns>Size of image in bytes</returns>
		public uint GetImageSize() {
			var optHdr = ImageNTHeaders.OptionalHeader;
			uint alignment = optHdr.SectionAlignment;
			if (imageSectionHeaders.Length == 0)
				return (uint)AlignUp(optHdr.SizeOfHeaders, alignment);

			// Section headers must be in ascending order and adjacent
			var section = imageSectionHeaders[imageSectionHeaders.Length - 1];
			return (uint)Math.Min(AlignUp((ulong)section.VirtualAddress + section.VirtualSize, alignment), uint.MaxValue);
		}
	}
}
