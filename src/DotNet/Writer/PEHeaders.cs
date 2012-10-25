using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// <see cref="PEHeaders"/> options
	/// </summary>
	public class PEHeadersOptions {
		/// <summary>
		/// IMAGE_FILE_HEADER.Machine value
		/// </summary>
		public Machine? Machine;

		/// <summary>
		/// IMAGE_FILE_HEADER.TimeDateStamp value
		/// </summary>
		public uint? TimeDateStamp;

		/// <summary>
		/// IMAGE_FILE_HEADER.Characteristics value
		/// </summary>
		public Characteristics? Characteristics;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.MajorLinkerVersion value
		/// </summary>
		public byte? MajorLinkerVersion;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.MinorLinkerVersion value
		/// </summary>
		public byte? MinorLinkerVersion;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.ImageBase value
		/// </summary>
		public ulong? ImageBase;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.SectionAlignment value
		/// </summary>
		public uint? SectionAlignment;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.FileAlignment value
		/// </summary>
		public uint? FileAlignment;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.MajorOperatingSystemVersion value
		/// </summary>
		public ushort? MajorOperatingSystemVersion;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.MinorOperatingSystemVersion value
		/// </summary>
		public ushort? MinorOperatingSystemVersion;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.MajorImageVersion value
		/// </summary>
		public ushort? MajorImageVersion;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.MinorImageVersion value
		/// </summary>
		public ushort? MinorImageVersion;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.MajorSubsystemVersion value
		/// </summary>
		public ushort? MajorSubsystemVersion;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.MinorSubsystemVersion value
		/// </summary>
		public ushort? MinorSubsystemVersion;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.Win32VersionValue value
		/// </summary>
		public uint? Win32VersionValue;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.Subsystem value
		/// </summary>
		public Subsystem? Subsystem;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.DllCharacteristics value
		/// </summary>
		public DllCharacteristics? DllCharacteristics;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.SizeOfStackReserve value
		/// </summary>
		public ulong? SizeOfStackReserve;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.SizeOfStackCommit value
		/// </summary>
		public ulong? SizeOfStackCommit;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.SizeOfHeapReserve value
		/// </summary>
		public ulong? SizeOfHeapReserve;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.SizeOfHeapCommit value
		/// </summary>
		public ulong? SizeOfHeapCommit;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.LoaderFlags value
		/// </summary>
		public uint? LoaderFlags;

		/// <summary>
		/// IMAGE_OPTIONAL_HEADER.NumberOfRvaAndSizes value
		/// </summary>
		public uint? NumberOfRvaAndSizes;
	}

	/// <summary>
	/// DOS and PE headers
	/// </summary>
	public class PEHeaders : IChunk {
		IList<PESection> sections;
		PEHeadersOptions options;
		FileOffset offset;
		RVA rva;
		uint length;
		uint sectionAlignment;
		uint fileAlignment;
		static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		long startOffset;
		long sizeOfCodeOffset;
		long sizeOfImageOffset;
		long sectionsOffset;
		long dataDirectoryOffset;

		// Copied from Partition II.25.2.1
		static readonly byte[] dosHeader = new byte[0x80] {
			0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00,
			0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00,
			0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
			0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD,
			0x21, 0xB8, 0x01, 0x4C, 0xCD, 0x21, 0x54, 0x68,
			0x69, 0x73, 0x20, 0x70, 0x72, 0x6F, 0x67, 0x72,
			0x61, 0x6D, 0x20, 0x63, 0x61, 0x6E, 0x6E, 0x6F,
			0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6E,
			0x20, 0x69, 0x6E, 0x20, 0x44, 0x4F, 0x53, 0x20,
			0x6D, 0x6F, 0x64, 0x65, 0x2E, 0x0D, 0x0D, 0x0A,
			0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		};

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Gets the section alignment
		/// </summary>
		public uint SectionAlignment {
			get { return sectionAlignment; }
		}

		/// <summary>
		/// Gets the file alignment
		/// </summary>
		public uint FileAlignment {
			get { return fileAlignment; }
		}

		/// <summary>
		/// Gets/sets the <see cref="PESection"/>s
		/// </summary>
		public IList<PESection> PESections {
			get { return sections; }
			set { sections = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public PEHeaders()
			: this(new PEHeadersOptions()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options</param>
		public PEHeaders(PEHeadersOptions options) {
			this.options = options ?? new PEHeadersOptions();
			this.sectionAlignment = this.options.SectionAlignment ?? 0x2000;
			this.fileAlignment = this.options.FileAlignment ?? 0x200;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			length = (uint)dosHeader.Length;
			length += 4 + 0x14;
			length += Use32BitOptionalHeader() ? 0xE0U : 0xF0;
			length += (uint)sections.Count * 0x28;
		}

		/// <inheritdoc/>
		public uint GetLength() {
			return length;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			startOffset = writer.BaseStream.Position;

			// DOS header
			writer.Write(dosHeader);

			// PE magic
			writer.Write(0x00004550);

			// Image file header
			writer.Write((ushort)GetMachine());
			writer.Write((ushort)sections.Count);
			writer.Write(options.TimeDateStamp ?? (uint)(DateTime.UtcNow - Epoch).TotalSeconds);
			writer.Write(0);
			writer.Write(0);
			writer.Write((ushort)(Use32BitOptionalHeader() ? 0xE0U : 0xF0));
			writer.Write((ushort)(options.Characteristics ?? GetDefaultCharacteristics()));

			// Image optional header
			if (Use32BitOptionalHeader()) {
				writer.Write((ushort)0x010B);
				writer.Write(options.MajorLinkerVersion ?? 11);
				writer.Write(options.MinorLinkerVersion ?? 0);
				sizeOfCodeOffset = writer.BaseStream.Position;
				writer.Write(0);	// SizeOfCode
				writer.Write(0);	// SizeOfInitializedData
				writer.Write(0);	// SizeOfUninitializedData
				writer.Write(0);	// AddressOfEntryPoint
				writer.Write(0);	// BaseOfCode
				writer.Write(0);	// BaseOfData
				writer.Write((uint)(options.ImageBase ?? 0x00400000));
				writer.Write(sectionAlignment);
				writer.Write(fileAlignment);
				writer.Write(options.MajorOperatingSystemVersion ?? 4);
				writer.Write(options.MinorOperatingSystemVersion ?? 0);
				writer.Write(options.MajorImageVersion ?? 0);
				writer.Write(options.MinorImageVersion ?? 0);
				writer.Write(options.MajorSubsystemVersion ?? 4);
				writer.Write(options.MinorSubsystemVersion ?? 0);
				writer.Write(options.Win32VersionValue ?? 0);
				sizeOfImageOffset = writer.BaseStream.Position;
				writer.Write(0);	// SizeOfImage
				writer.Write(0);	// SizeOfHeaders
				writer.Write(0);	// CheckSum
				writer.Write((ushort)(options.Subsystem ?? Subsystem.WindowsGui));
				writer.Write((ushort)(options.DllCharacteristics ?? DllCharacteristics.TerminalServerAware | DllCharacteristics.NoSeh | DllCharacteristics.NxCompat | DllCharacteristics.DynamicBase));
				writer.Write((uint)(options.SizeOfStackReserve ?? 0x00100000));
				writer.Write((uint)(options.SizeOfStackCommit ?? 0x00001000));
				writer.Write((uint)(options.SizeOfHeapReserve ?? 0x00100000));
				writer.Write((uint)(options.SizeOfHeapCommit ?? 0x00001000));
				writer.Write(options.LoaderFlags ?? 0x00000000);
				writer.Write(options.NumberOfRvaAndSizes ?? 0x00000010);
			}
			else {
				writer.Write((ushort)0x020B);
				writer.Write(options.MajorLinkerVersion ?? 11);
				writer.Write(options.MinorLinkerVersion ?? 0);
				sizeOfCodeOffset = writer.BaseStream.Position;
				writer.Write(0);	// SizeOfCode
				writer.Write(0);	// SizeOfInitializedData
				writer.Write(0);	// SizeOfUninitializedData
				writer.Write(0);	// AddressOfEntryPoint
				writer.Write(0);	// BaseOfCode
				writer.Write(options.ImageBase ?? 0x0000000140000000);
				writer.Write(sectionAlignment);
				writer.Write(fileAlignment);
				writer.Write(options.MajorOperatingSystemVersion ?? 4);
				writer.Write(options.MinorOperatingSystemVersion ?? 0);
				writer.Write(options.MajorImageVersion ?? 0);
				writer.Write(options.MinorImageVersion ?? 0);
				writer.Write(options.MajorSubsystemVersion ?? 4);
				writer.Write(options.MinorSubsystemVersion ?? 0);
				writer.Write(options.Win32VersionValue ?? 0);
				sizeOfImageOffset = writer.BaseStream.Position;
				writer.Write(0);	// SizeOfImage
				writer.Write(0);	// SizeOfHeaders
				writer.Write(0);	// CheckSum
				writer.Write((ushort)(options.Subsystem ?? Subsystem.WindowsGui));
				writer.Write((ushort)(options.DllCharacteristics ?? DllCharacteristics.TerminalServerAware | DllCharacteristics.NoSeh | DllCharacteristics.NxCompat | DllCharacteristics.DynamicBase));
				writer.Write(options.SizeOfStackReserve ?? 0x0000000000400000);
				writer.Write(options.SizeOfStackCommit ?? 0x0000000000004000);
				writer.Write(options.SizeOfHeapReserve ?? 0x0000000000100000);
				writer.Write(options.SizeOfHeapCommit ?? 0x0000000000002000);
				writer.Write(options.LoaderFlags ?? 0x00000000);
				writer.Write(options.NumberOfRvaAndSizes ?? 0x00000010);
			}

			dataDirectoryOffset = writer.BaseStream.Position;
			writer.Write(0UL);	// Export table
			writer.Write(0UL);	// Import table
			writer.Write(0UL);	// Resource table
			writer.Write(0UL);	// Exception table
			writer.Write(0UL);	// Certificate table
			writer.Write(0UL);	// Base relocation table
			writer.Write(0UL);	// Debugging information
			writer.Write(0UL);	// Architecture-specific data
			writer.Write(0UL);	// Global pointer register RVA
			writer.Write(0UL);	// Thread local storage
			writer.Write(0UL);	// Load configuration table
			writer.Write(0UL);	// Bound import table
			writer.Write(0UL);	// Import address table
			writer.Write(0UL);	// Delay import descriptor
			writer.Write(0UL);	// The CLR header
			writer.Write(0UL);	// Reserved

			// Sections
			sectionsOffset = writer.BaseStream.Position;
			foreach (var section in sections) {
				writer.Write(Encoding.UTF8.GetBytes(section.Name + "\0\0\0\0\0\0\0\0"), 0, 8);
				writer.Write(0);	// VirtualSize
				writer.Write(0);	// VirtualAddress
				writer.Write(0);	// SizeOfRawData
				writer.Write(0);	// PointerToRawData
				writer.Write(0);	// PointerToRelocations
				writer.Write(0);	// PointerToLinenumbers
				writer.Write((ushort)0);	// NumberOfRelocations
				writer.Write((ushort)0);	// NumberOfLinenumbers
				writer.Write(section.Characteristics);
			}
		}

		/// <summary>
		/// Updates the section fields in the optional header
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="entryPoint">Entry point</param>
		public void UpdateSectionFields(BinaryWriter writer, RVA entryPoint) {
			uint sizeOfHeaders = Utils.AlignUp(length, fileAlignment);

			writer.BaseStream.Position = sectionsOffset + 8;
			uint sizeOfImage = Utils.AlignUp(sizeOfHeaders, sectionAlignment);
			uint baseOfData = 0, baseOfCode = 0;
			uint sizeOfCode = 0, sizeOfInitdData = 0, sizeOfUninitdData = 0;
			foreach (var section in sections) {
				uint vs = section.GetLength();
				uint alignedVs = Utils.AlignUp(vs, sectionAlignment);
				uint rva = sizeOfImage;
				uint rawSize = Utils.AlignUp(section.GetLength(), fileAlignment);
				uint dataOffset = (uint)section.FileOffset;
				sizeOfImage += alignedVs;

				writer.Write(vs);			// VirtualSize
				writer.Write(rva);			// VirtualAddress
				writer.Write(rawSize);		// SizeOfRawData
				writer.Write(dataOffset);	// PointerToRawData
				writer.BaseStream.Position += 0x18;

				if (baseOfCode == 0 && section.IsCode)
					baseOfCode = rva;
				if (baseOfData == 0 && (section.IsInitializedData || section.IsUninitializedData))
					baseOfData = rva;
				if (section.IsCode)
					sizeOfCode += alignedVs;
				if (section.IsInitializedData)
					sizeOfInitdData += alignedVs;
				if (section.IsUninitializedData)
					sizeOfUninitdData += alignedVs;
			}

			writer.BaseStream.Position = sizeOfCodeOffset;
			writer.Write(sizeOfCode);
			writer.Write(sizeOfInitdData);
			writer.Write(sizeOfUninitdData);
			writer.Write((uint)entryPoint);
			writer.Write(baseOfCode);
			if (Use32BitOptionalHeader())
				writer.Write(baseOfData);

			writer.BaseStream.Position = sizeOfImageOffset;
			writer.Write(sizeOfImage);
			writer.Write(sizeOfHeaders);
		}

		/// <summary>
		/// Update a data directory
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="index">Data directory index (eg. 14 for .NET directory)</param>
		/// <param name="rva">RVA</param>
		/// <param name="length">Size</param>
		public void WriteDataDirectory(BinaryWriter writer, int index, RVA rva, uint length) {
			if ((uint)index >= 16)
				throw new ModuleWriterException(string.Format("Invalid data directory index: {0}", index));
			writer.BaseStream.Position = dataDirectoryOffset + index * 8;
			writer.Write(length == 0 ? 0 : (uint)rva);
			writer.Write(length);
		}

		/// <summary>
		/// Calculates the PE checksum and writes it to the checksum field
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="length">Length of PE file</param>
		public void WriteCheckSum(BinaryWriter writer, long length) {
			writer.BaseStream.Position = startOffset;
			long checkSumOffset = (long)sizeOfImageOffset + 8;
			uint checkSum = CalculateCheckSum(new BinaryReader(writer.BaseStream), length, checkSumOffset);
			writer.BaseStream.Position = checkSumOffset;
			writer.Write(checkSum);
		}

		static uint CalculateCheckSum(BinaryReader reader, long length, long checkSumOffset) {
			uint checkSum = 0;
			for (long i = 0; i < length; i += 2) {
				if (i == checkSumOffset) {
					reader.ReadUInt32();
					i += 2;
					continue;
				}
				checkSum += reader.ReadUInt16();
				checkSum = (ushort)(checkSum + (checkSum >> 16));
			}
			ulong cks = (ulong)checkSum + (ulong)length;
			return (uint)cks + (uint)(cks >> 32);
		}

		Machine GetMachine() {
			return options.Machine ?? Machine.I386;
		}

		bool Use32BitOptionalHeader() {
			return GetMachine() == Machine.I386;
		}

		Characteristics GetDefaultCharacteristics() {
			if (Use32BitOptionalHeader())
				return Characteristics._32BitMachine | Characteristics.ExecutableImage;
			return Characteristics.ExecutableImage | Characteristics.LargeAddressAware;
		}
	}
}
