// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Reads all PE sections from a PE stream
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
				if ((long)offset >= section.PointerToRawData && (long)offset < section.PointerToRawData + section.SizeOfRawData)
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
			foreach (var section in imageSectionHeaders) {
				if (rva >= section.VirtualAddress && rva < section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData))
					return section;
			}
			return null;
		}

		/// <summary>
		/// Converts a <see cref="FileOffset"/> to an <see cref="RVA"/>
		/// </summary>
		/// <param name="offset">The file offset to convert</param>
		/// <returns>The RVA</returns>
		public RVA ToRVA(FileOffset offset) {
			var section = ToImageSectionHeader(offset);
			if (!(section is null))
				return (uint)(offset - section.PointerToRawData) + section.VirtualAddress;
			return (RVA)offset;
		}

		/// <summary>
		/// Converts an <see cref="RVA"/> to a <see cref="FileOffset"/>
		/// </summary>
		/// <param name="rva">The RVA to convert</param>
		/// <returns>The file offset</returns>
		public FileOffset ToFileOffset(RVA rva) {
			var section = ToImageSectionHeader(rva);
			if (!(section is null))
				return (FileOffset)(rva - section.VirtualAddress + section.PointerToRawData);
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
			ulong length = AlignUp(optHdr.SizeOfHeaders, alignment);
			foreach (var section in imageSectionHeaders) {
				ulong length2 = AlignUp((ulong)section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData), alignment);
				if (length2 > length)
					length = length2;
			}
			return (uint)Math.Min(length, uint.MaxValue);
		}
	}
}
