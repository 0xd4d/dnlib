// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_OPTIONAL_HEADER (32-bit) PE section
	/// </summary>
	public sealed class ImageOptionalHeader32 : FileSection, IImageOptionalHeader {
		readonly ushort magic;
		readonly byte majorLinkerVersion;
		readonly byte minorLinkerVersion;
		readonly uint sizeOfCode;
		readonly uint sizeOfInitializedData;
		readonly uint sizeOfUninitializedData;
		readonly RVA addressOfEntryPoint;
		readonly RVA baseOfCode;
		readonly RVA baseOfData;
		readonly uint imageBase;
		readonly uint sectionAlignment;
		readonly uint fileAlignment;
		readonly ushort majorOperatingSystemVersion;
		readonly ushort minorOperatingSystemVersion;
		readonly ushort majorImageVersion;
		readonly ushort minorImageVersion;
		readonly ushort majorSubsystemVersion;
		readonly ushort minorSubsystemVersion;
		readonly uint win32VersionValue;
		readonly uint sizeOfImage;
		readonly uint sizeOfHeaders;
		readonly uint checkSum;
		readonly Subsystem subsystem;
		readonly DllCharacteristics dllCharacteristics;
		readonly uint sizeOfStackReserve;
		readonly uint sizeOfStackCommit;
		readonly uint sizeOfHeapReserve;
		readonly uint sizeOfHeapCommit;
		readonly uint loaderFlags;
		readonly uint numberOfRvaAndSizes;
		readonly ImageDataDirectory[] dataDirectories = new ImageDataDirectory[16];

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
		public RVA AddressOfEntryPoint {
			get { return addressOfEntryPoint; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.BaseOfCode field
		/// </summary>
		public RVA BaseOfCode {
			get { return baseOfCode; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.BaseOfData field
		/// </summary>
		public RVA BaseOfData {
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
		public Subsystem Subsystem {
			get { return subsystem; }
		}

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.DllCharacteristics field
		/// </summary>
		public DllCharacteristics DllCharacteristics {
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
		public ImageOptionalHeader32(IImageStream reader, uint totalSize, bool verify) {
			if (totalSize < 0x60)
				throw new BadImageFormatException("Invalid optional header size");
			if (verify && reader.Position + totalSize > reader.Length)
				throw new BadImageFormatException("Invalid optional header size");
			SetStartOffset(reader);
			this.magic = reader.ReadUInt16();
			this.majorLinkerVersion = reader.ReadByte();
			this.minorLinkerVersion = reader.ReadByte();
			this.sizeOfCode = reader.ReadUInt32();
			this.sizeOfInitializedData = reader.ReadUInt32();
			this.sizeOfUninitializedData = reader.ReadUInt32();
			this.addressOfEntryPoint = (RVA)reader.ReadUInt32();
			this.baseOfCode = (RVA)reader.ReadUInt32();
			this.baseOfData = (RVA)reader.ReadUInt32();
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
			this.subsystem = (Subsystem)reader.ReadUInt16();
			this.dllCharacteristics = (DllCharacteristics)reader.ReadUInt16();
			this.sizeOfStackReserve = reader.ReadUInt32();
			this.sizeOfStackCommit = reader.ReadUInt32();
			this.sizeOfHeapReserve = reader.ReadUInt32();
			this.sizeOfHeapCommit = reader.ReadUInt32();
			this.loaderFlags = reader.ReadUInt32();
			this.numberOfRvaAndSizes = reader.ReadUInt32();
			for (int i = 0; i < dataDirectories.Length; i++) {
				uint len = (uint)(reader.Position - startOffset);
				if (len + 8 <= totalSize)
					dataDirectories[i] = new ImageDataDirectory(reader, verify);
				else
					dataDirectories[i] = new ImageDataDirectory();
			}
			reader.Position = (long)startOffset + totalSize;
			SetEndoffset(reader);
		}
	}
}
