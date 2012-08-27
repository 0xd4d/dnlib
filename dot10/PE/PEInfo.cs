using System;
using System.IO;

namespace dot10.PE {
	/// <summary>
	/// Reads all PE sections from a PE stream
	/// </summary>
	public class PEInfo {
		readonly ImageDosHeader imageDosHeader;
		readonly ImageNTHeaders imageNTHeaders;
		readonly ImageSectionHeader[] imageSectionHeaders;

		/// <summary>
		/// Returns the DOS header
		/// </summary>
		public ImageDosHeader ImageDosHeader {
			get { return imageDosHeader; }
		}

		/// <summary>
		/// Returns the NT headers
		/// </summary>
		public ImageNTHeaders ImageNTHeaders {
			get { return imageNTHeaders; }
		}

		/// <summary>
		/// Returns the section headers
		/// </summary>
		public ImageSectionHeader[] ImageSectionHeaders {
			get { return imageSectionHeaders; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify sections</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public PEInfo(BinaryReader reader, bool verify) {
			reader.BaseStream.Position = 0;
			this.imageDosHeader = new ImageDosHeader(reader, verify);

			if (verify && this.imageDosHeader.NTHeadersOffset == 0)
				throw new BadImageFormatException("Invalid NT headers offset");
			reader.BaseStream.Position = this.imageDosHeader.NTHeadersOffset;
			this.imageNTHeaders = new ImageNTHeaders(reader, verify);

			reader.BaseStream.Position = (this.imageNTHeaders.OptionalHeader.StartOffset + this.imageNTHeaders.FileHeader.SizeOfOptionalHeader).Value;
			this.imageSectionHeaders = new ImageSectionHeader[this.imageNTHeaders.FileHeader.NumberOfSections];
			for (int i = 0; i < this.imageSectionHeaders.Length; i++)
				this.imageSectionHeaders[i] = new ImageSectionHeader(reader, verify);
		}

		/// <summary>
		/// Returns the first <see cref="ImageSectionHeader"/> that has data at file offset
		/// <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">The file offset</param>
		/// <returns></returns>
		public ImageSectionHeader ToImageSectionHeader(FileOffset offset) {
			foreach (var section in imageSectionHeaders) {
				if (offset.Value >= section.PointerToRawData && offset.Value < section.PointerToRawData + section.SizeOfRawData)
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
			if (section != null)
				return (uint)(offset.Value - section.PointerToRawData) + section.VirtualAddress;
			return new RVA((uint)offset.Value);
		}

		/// <summary>
		/// Converts an <see cref="RVA"/> to a <see cref="FileOffset"/>
		/// </summary>
		/// <param name="rva">The RVA to convert</param>
		/// <returns>The file offset</returns>
		public FileOffset ToFileOffset(RVA rva) {
			var section = ToImageSectionHeader(rva);
			if (section != null)
				return new FileOffset((long)(rva.Value - section.VirtualAddress.Value) + section.PointerToRawData);
			return new FileOffset(rva.Value);
		}
	}
}
