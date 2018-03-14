// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_OPTIONAL_HEADER64 PE section
	/// </summary>
	public sealed class ImageOptionalHeader64 : FileSection, IImageOptionalHeader {
		readonly ushort magic;
		readonly byte majorLinkerVersion;
		readonly byte minorLinkerVersion;
		readonly uint sizeOfCode;
		readonly uint sizeOfInitializedData;
		readonly uint sizeOfUninitializedData;
		readonly RVA addressOfEntryPoint;
		readonly RVA baseOfCode;
		readonly ulong imageBase;
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
		readonly ulong sizeOfStackReserve;
		readonly ulong sizeOfStackCommit;
		readonly ulong sizeOfHeapReserve;
		readonly ulong sizeOfHeapCommit;
		readonly uint loaderFlags;
		readonly uint numberOfRvaAndSizes;
		readonly ImageDataDirectory[] dataDirectories = new ImageDataDirectory[16];

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.Magic field
		/// </summary>
		public ushort Magic => magic;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MajorLinkerVersion field
		/// </summary>
		public byte MajorLinkerVersion => majorLinkerVersion;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MinorLinkerVersion field
		/// </summary>
		public byte MinorLinkerVersion => minorLinkerVersion;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfCode field
		/// </summary>
		public uint SizeOfCode => sizeOfCode;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfInitializedData field
		/// </summary>
		public uint SizeOfInitializedData => sizeOfInitializedData;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfUninitializedData field
		/// </summary>
		public uint SizeOfUninitializedData => sizeOfUninitializedData;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.AddressOfEntryPoint field
		/// </summary>
		public RVA AddressOfEntryPoint => addressOfEntryPoint;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.BaseOfCode field
		/// </summary>
		public RVA BaseOfCode => baseOfCode;

		/// <summary>
		/// Returns 0 since BaseOfData is not present in IMAGE_OPTIONAL_HEADER64
		/// </summary>
		public RVA BaseOfData => 0;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.ImageBase field
		/// </summary>
		public ulong ImageBase => imageBase;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SectionAlignment field
		/// </summary>
		public uint SectionAlignment => sectionAlignment;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.FileAlignment field
		/// </summary>
		public uint FileAlignment => fileAlignment;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MajorOperatingSystemVersion field
		/// </summary>
		public ushort MajorOperatingSystemVersion => majorOperatingSystemVersion;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MinorOperatingSystemVersion field
		/// </summary>
		public ushort MinorOperatingSystemVersion => minorOperatingSystemVersion;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MajorImageVersion field
		/// </summary>
		public ushort MajorImageVersion => majorImageVersion;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MinorImageVersion field
		/// </summary>
		public ushort MinorImageVersion => minorImageVersion;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MajorSubsystemVersion field
		/// </summary>
		public ushort MajorSubsystemVersion => majorSubsystemVersion;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MinorSubsystemVersion field
		/// </summary>
		public ushort MinorSubsystemVersion => minorSubsystemVersion;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.Win32VersionValue field
		/// </summary>
		public uint Win32VersionValue => win32VersionValue;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfImage field
		/// </summary>
		public uint SizeOfImage => sizeOfImage;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfHeaders field
		/// </summary>
		public uint SizeOfHeaders => sizeOfHeaders;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.CheckSum field
		/// </summary>
		public uint CheckSum => checkSum;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.Subsystem field
		/// </summary>
		public Subsystem Subsystem => subsystem;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.DllCharacteristics field
		/// </summary>
		public DllCharacteristics DllCharacteristics => dllCharacteristics;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfStackReserve field
		/// </summary>
		public ulong SizeOfStackReserve => sizeOfStackReserve;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfStackCommit field
		/// </summary>
		public ulong SizeOfStackCommit => sizeOfStackCommit;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfHeapReserve field
		/// </summary>
		public ulong SizeOfHeapReserve => sizeOfHeapReserve;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfHeapCommit field
		/// </summary>
		public ulong SizeOfHeapCommit => sizeOfHeapCommit;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.LoaderFlags field
		/// </summary>
		public uint LoaderFlags => loaderFlags;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.NumberOfRvaAndSizes field
		/// </summary>
		public uint NumberOfRvaAndSizes => numberOfRvaAndSizes;

		/// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.DataDirectories field
		/// </summary>
		public ImageDataDirectory[] DataDirectories => dataDirectories;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="totalSize">Total size of this optional header (from the file header)</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageOptionalHeader64(ref DataReader reader, uint totalSize, bool verify) {
			if (totalSize < 0x70)
				throw new BadImageFormatException("Invalid optional header size");
			if (verify && (ulong)reader.Position + totalSize > reader.Length)
				throw new BadImageFormatException("Invalid optional header size");
			SetStartOffset(ref reader);
			magic = reader.ReadUInt16();
			majorLinkerVersion = reader.ReadByte();
			minorLinkerVersion = reader.ReadByte();
			sizeOfCode = reader.ReadUInt32();
			sizeOfInitializedData = reader.ReadUInt32();
			sizeOfUninitializedData = reader.ReadUInt32();
			addressOfEntryPoint = (RVA)reader.ReadUInt32();
			baseOfCode = (RVA)reader.ReadUInt32();
			imageBase = reader.ReadUInt64();
			sectionAlignment = reader.ReadUInt32();
			fileAlignment = reader.ReadUInt32();
			majorOperatingSystemVersion = reader.ReadUInt16();
			minorOperatingSystemVersion = reader.ReadUInt16();
			majorImageVersion = reader.ReadUInt16();
			minorImageVersion = reader.ReadUInt16();
			majorSubsystemVersion = reader.ReadUInt16();
			minorSubsystemVersion = reader.ReadUInt16();
			win32VersionValue = reader.ReadUInt32();
			sizeOfImage = reader.ReadUInt32();
			sizeOfHeaders = reader.ReadUInt32();
			checkSum = reader.ReadUInt32();
			subsystem = (Subsystem)reader.ReadUInt16();
			dllCharacteristics = (DllCharacteristics)reader.ReadUInt16();
			sizeOfStackReserve = reader.ReadUInt64();
			sizeOfStackCommit = reader.ReadUInt64();
			sizeOfHeapReserve = reader.ReadUInt64();
			sizeOfHeapCommit = reader.ReadUInt64();
			loaderFlags = reader.ReadUInt32();
			numberOfRvaAndSizes = reader.ReadUInt32();
			for (int i = 0; i < dataDirectories.Length; i++) {
				uint len = reader.Position - (uint)startOffset;
				if (len + 8 <= totalSize)
					dataDirectories[i] = new ImageDataDirectory(ref reader, verify);
				else
					dataDirectories[i] = new ImageDataDirectory();
			}
			reader.Position = (uint)startOffset + totalSize;
			SetEndoffset(ref reader);
		}
	}
}
