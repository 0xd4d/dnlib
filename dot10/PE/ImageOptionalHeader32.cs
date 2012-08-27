using System;
using System.Collections.Generic;
using System.IO;

namespace dot10.PE {
	/// <summary>
	/// Represents the IMAGE_OPTIONAL_HEADER (32-bit) PE section
	/// </summary>
	public class ImageOptionalHeader32 : FileSection, IImageOptionalHeader {
		ushort magic;
		byte majorLinkerVersion;
		byte minorLinkerVersion;
		uint sizeOfCode;
		uint sizeOfInitializedData;
		uint sizeOfUninitializedData;
		uint addressOfEntryPoint;
		uint baseOfCode;
		uint baseOfData;
		uint imageBase;
		uint sectionAlignment;
		uint fileAlignment;
		ushort majorOperatingSystemVersion;
		ushort minorOperatingSystemVersion;
		ushort majorImageVersion;
		ushort minorImageVersion;
		ushort majorSubsystemVersion;
		ushort minorSubsystemVersion;
		uint win32VersionValue;
		uint sizeOfImage;
		uint sizeOfHeaders;
		uint checkSum;
		ushort subsystem;
		ushort dllCharacteristics;
		uint sizeOfStackReserve;
		uint sizeOfStackCommit;
		uint sizeOfHeapReserve;
		uint sizeOfHeapCommit;
		uint loaderFlags;
		uint numberOfRvaAndSizes;
		ImageDataDirectory[] dataDirectories = new ImageDataDirectory[16];

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.Magic field
		/// </summary>
		public ushort Magic {
			get { return magic; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MajorLinkerVersion field
		/// </summary>
		public byte MajorLinkerVersion {
			get { return majorLinkerVersion; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MinorLinkerVersion field
		/// </summary>
		public byte MinorLinkerVersion {
			get { return minorLinkerVersion; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfCode field
		/// </summary>
		public uint SizeOfCode {
			get { return sizeOfCode; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfInitializedData field
		/// </summary>
		public uint SizeOfInitializedData {
			get { return sizeOfInitializedData; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfUninitializedData field
		/// </summary>
		public uint SizeOfUninitializedData {
			get { return sizeOfUninitializedData; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.AddressOfEntryPoint field
		/// </summary>
		public uint AddressOfEntryPoint {
			get { return addressOfEntryPoint; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.BaseOfCode field
		/// </summary>
		public uint BaseOfCode {
			get { return baseOfCode; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.BaseOfData field
		/// </summary>
		public uint BaseOfData {
			get { return baseOfData; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.ImageBase field
		/// </summary>
		public ulong ImageBase {
			get { return imageBase; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SectionAlignment field
		/// </summary>
		public uint SectionAlignment {
			get { return sectionAlignment; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.FileAlignment field
		/// </summary>
		public uint FileAlignment {
			get { return fileAlignment; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MajorOperatingSystemVersion field
		/// </summary>
		public ushort MajorOperatingSystemVersion {
			get { return majorOperatingSystemVersion; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MinorOperatingSystemVersion field
		/// </summary>
		public ushort MinorOperatingSystemVersion {
			get { return minorOperatingSystemVersion; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MajorImageVersion field
		/// </summary>
		public ushort MajorImageVersion {
			get { return majorImageVersion; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MinorImageVersion field
		/// </summary>
		public ushort MinorImageVersion {
			get { return minorImageVersion; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MajorSubsystemVersion field
		/// </summary>
		public ushort MajorSubsystemVersion {
			get { return majorSubsystemVersion; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MinorSubsystemVersion field
		/// </summary>
		public ushort MinorSubsystemVersion {
			get { return minorSubsystemVersion; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.Win32VersionValue field
		/// </summary>
		public uint Win32VersionValue {
			get { return win32VersionValue; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfImage field
		/// </summary>
		public uint SizeOfImage {
			get { return sizeOfImage; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfHeaders field
		/// </summary>
		public uint SizeOfHeaders {
			get { return sizeOfHeaders; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.CheckSum field
		/// </summary>
		public uint CheckSum {
			get { return checkSum; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.Subsystem field
		/// </summary>
		public ushort Subsystem {
			get { return subsystem; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.DllCharacteristics field
		/// </summary>
		public ushort DllCharacteristics {
			get { return dllCharacteristics; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfStackReserve field
		/// </summary>
		public ulong SizeOfStackReserve {
			get { return sizeOfStackReserve; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfStackCommit field
		/// </summary>
		public ulong SizeOfStackCommit {
			get { return sizeOfStackCommit; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfHeapReserve field
		/// </summary>
		public ulong SizeOfHeapReserve {
			get { return sizeOfHeapReserve; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfHeapCommit field
		/// </summary>
		public ulong SizeOfHeapCommit {
			get { return sizeOfHeapCommit; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.LoaderFlags field
		/// </summary>
		public uint LoaderFlags {
			get { return loaderFlags; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.NumberOfRvaAndSizes field
		/// </summary>
		public uint NumberOfRvaAndSizes {
			get { return numberOfRvaAndSizes; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.DataDirectories field
		/// </summary>
		public ImageDataDirectory[] DataDirectories {
			get { return dataDirectories; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="totalSize">Total size of this optional header (from the file header)</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageOptionalHeader32(BinaryReader reader, uint totalSize, bool verify) {
			if (totalSize < 0x60)
				throw new BadImageFormatException("Invalid optional header size");
			if (verify && reader.BaseStream.Position + totalSize > reader.BaseStream.Length)
				throw new BadImageFormatException("Invalid optional header size");
			SetStartOffset(reader);
			this.magic = reader.ReadUInt16();
			this.majorLinkerVersion = reader.ReadByte();
			this.minorLinkerVersion = reader.ReadByte();
			this.sizeOfCode = reader.ReadUInt32();
			this.sizeOfInitializedData = reader.ReadUInt32();
			this.sizeOfUninitializedData = reader.ReadUInt32();
			this.addressOfEntryPoint = reader.ReadUInt32();
			this.baseOfCode = reader.ReadUInt32();
			this.baseOfData = reader.ReadUInt32();
			this.imageBase = reader.ReadUInt32();
			this.sectionAlignment = reader.ReadUInt32();
			this.fileAlignment = reader.ReadUInt32();
			this.majorOperatingSystemVersion = reader.ReadUInt16();
			this.minorOperatingSystemVersion = reader.ReadUInt16();
			this.majorImageVersion = reader.ReadUInt16();
			this.minorImageVersion = reader.ReadUInt16();
			this.majorSubsystemVersion = reader.ReadUInt16();
			this.minorSubsystemVersion = reader.ReadUInt16();
			this.win32VersionValue = reader.ReadUInt32();
			this.sizeOfImage = reader.ReadUInt32();
			this.sizeOfHeaders = reader.ReadUInt32();
			this.checkSum = reader.ReadUInt32();
			this.subsystem = reader.ReadUInt16();
			this.dllCharacteristics = reader.ReadUInt16();
			this.sizeOfStackReserve = reader.ReadUInt32();
			this.sizeOfStackCommit = reader.ReadUInt32();
			this.sizeOfHeapReserve = reader.ReadUInt32();
			this.sizeOfHeapCommit = reader.ReadUInt32();
			this.loaderFlags = reader.ReadUInt32();
			this.numberOfRvaAndSizes = reader.ReadUInt32();
			for (int i = 0; i < dataDirectories.Length; i++) {
				uint len = (uint)(reader.BaseStream.Position - startOffset.Value);
				if (len + 8 <= totalSize)
					dataDirectories[i] = new ImageDataDirectory(reader, verify);
				else
					dataDirectories[i] = new ImageDataDirectory();
			}
			reader.BaseStream.Position = startOffset.Value + totalSize;
			SetEndoffset(reader);
		}
	}
}
