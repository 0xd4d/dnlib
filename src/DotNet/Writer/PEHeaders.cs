// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// <see cref="PEHeaders"/> options
	/// </summary>
	public sealed class PEHeadersOptions {
		/// <summary>
		/// Default DLL characteristics
		/// </summary>
		public const DllCharacteristics DefaultDllCharacteristics = dnlib.PE.DllCharacteristics.TerminalServerAware | dnlib.PE.DllCharacteristics.NoSeh | dnlib.PE.DllCharacteristics.NxCompat | dnlib.PE.DllCharacteristics.DynamicBase;

		/// <summary>
		/// Default subsystem value
		/// </summary>
		public const Subsystem DEFAULT_SUBSYSTEM = dnlib.PE.Subsystem.WindowsGui;

		/// <summary>
		/// Default major linker version. Roslyn C# defaults to 0x30, and Roslyn VB defaults to 0x50.
		/// </summary>
		public const byte DEFAULT_MAJOR_LINKER_VERSION = 11;

		/// <summary>
		/// Default minor linker version
		/// </summary>
		public const byte DEFAULT_MINOR_LINKER_VERSION = 0;

		/// <summary>
		/// IMAGE_FILE_HEADER.Machine value
		/// </summary>
		public Machine? Machine;

		/// <summary>
		/// IMAGE_FILE_HEADER.TimeDateStamp value
		/// </summary>
		public uint? TimeDateStamp;

		/// <summary>
		/// IMAGE_FILE_HEADER.PointerToSymbolTable value
		/// </summary>
		public uint? PointerToSymbolTable;

		/// <summary>
		/// IMAGE_FILE_HEADER.NumberOfSymbols value
		/// </summary>
		public uint? NumberOfSymbols;

		/// <summary>
		/// IMAGE_FILE_HEADER.Characteristics value. <see cref="dnlib.PE.Characteristics.Dll"/> bit
		/// is ignored and set/cleared depending on whether it's a EXE or a DLL file.
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

		/// <summary>
		/// Creates a new time date stamp using current time
		/// </summary>
		/// <returns>A new time date stamp</returns>
		public static uint CreateNewTimeDateStamp() => (uint)(DateTime.UtcNow - Epoch).TotalSeconds;
		static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	}

	/// <summary>
	/// DOS and PE headers
	/// </summary>
	public sealed class PEHeaders : IChunk {
		IList<PESection> sections;
		readonly PEHeadersOptions options;
		FileOffset offset;
		RVA rva;
		uint length;
		readonly uint sectionAlignment;
		readonly uint fileAlignment;
		ulong imageBase;
		long startOffset;
		long checkSumOffset;
		bool isExeFile;

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

		/// <summary>
		/// Gets/sets the native entry point
		/// </summary>
		public StartupStub StartupStub { get; set; }

		/// <summary>
		/// Gets/sets the COR20 header
		/// </summary>
		public ImageCor20Header ImageCor20Header { get; set; }

		/// <summary>
		/// Gets/sets the IAT
		/// </summary>
		public ImportAddressTable ImportAddressTable { get; set; }

		/// <summary>
		/// Gets/sets the <see cref="ImportDirectory"/>
		/// </summary>
		public ImportDirectory ImportDirectory { get; set; }

		/// <summary>
		/// Gets/sets the Win32 resources
		/// </summary>
		public Win32ResourcesChunk Win32Resources { get; set; }

		/// <summary>
		/// Gets/sets the relocation directory
		/// </summary>
		public RelocDirectory RelocDirectory { get; set; }

		/// <summary>
		/// Gets/sets the debug directory
		/// </summary>
		public DebugDirectory DebugDirectory { get; set; }

		internal IChunk ExportDirectory { get; set; }

		/// <summary>
		/// Gets the image base
		/// </summary>
		public ulong ImageBase => imageBase;

		/// <summary>
		/// Gets/sets a value indicating whether this is a EXE or a DLL file
		/// </summary>
		public bool IsExeFile {
			get => isExeFile;
			set => isExeFile = value;
		}

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <summary>
		/// Gets the section alignment
		/// </summary>
		public uint SectionAlignment => sectionAlignment;

		/// <summary>
		/// Gets the file alignment
		/// </summary>
		public uint FileAlignment => fileAlignment;

		/// <summary>
		/// Gets/sets the <see cref="PESection"/>s
		/// </summary>
		public IList<PESection> PESections {
			get => sections;
			set => sections = value;
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
			sectionAlignment = this.options.SectionAlignment ?? 0x2000;
			fileAlignment = this.options.FileAlignment ?? 0x200;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			length = (uint)dosHeader.Length;
			length += 4 + 0x14;
			length += Use32BitOptionalHeader() ? 0xE0U : 0xF0;
			length += (uint)sections.Count * 0x28;

			if (Use32BitOptionalHeader())
				imageBase = options.ImageBase ?? (IsExeFile ? 0x00400000UL : 0x10000000);
			else
				imageBase = options.ImageBase ?? (IsExeFile ? 0x0000000140000000UL : 0x0000000180000000);
		}

		int SectionsCount {
			get {
				int count = 0;
				foreach (var section in sections) {
					if (section.GetVirtualSize() != 0)
						count++;
				}
				return count;
			}
		}

		/// <inheritdoc/>
		public uint GetFileLength() => length;

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		IEnumerable<SectionSizeInfo> GetSectionSizeInfos() {
			foreach (var section in sections) {
				uint virtSize = section.GetVirtualSize();
				if (virtSize != 0)
					yield return new SectionSizeInfo(virtSize, section.Characteristics);
			}
		}

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			startOffset = writer.Position;

			// DOS header
			writer.WriteBytes(dosHeader);

			// PE magic
			writer.WriteInt32(0x00004550);

			// Image file header
			writer.WriteUInt16((ushort)GetMachine());
			writer.WriteUInt16((ushort)SectionsCount);
			Debug.Assert(SectionsCount == sections.Count, "One or more sections are empty! The PE file could be bigger than it should be. Empty sections should be removed.");
			writer.WriteUInt32(options.TimeDateStamp ?? PEHeadersOptions.CreateNewTimeDateStamp());
			writer.WriteUInt32(options.PointerToSymbolTable ?? 0);
			writer.WriteUInt32(options.NumberOfSymbols ?? 0);
			writer.WriteUInt16((ushort)(Use32BitOptionalHeader() ? 0xE0U : 0xF0));
			writer.WriteUInt16((ushort)GetCharacteristics());

			var sectionSizes = new SectionSizes(fileAlignment, sectionAlignment, length, () => GetSectionSizeInfos());

			// Image optional header
			uint ep = StartupStub is null || !StartupStub.Enable ? 0 : (uint)StartupStub.EntryPointRVA;
			if (Use32BitOptionalHeader()) {
				writer.WriteUInt16((ushort)0x010B);
				writer.WriteByte(options.MajorLinkerVersion ?? PEHeadersOptions.DEFAULT_MAJOR_LINKER_VERSION);
				writer.WriteByte(options.MinorLinkerVersion ?? PEHeadersOptions.DEFAULT_MINOR_LINKER_VERSION);
				writer.WriteUInt32(sectionSizes.SizeOfCode);
				writer.WriteUInt32(sectionSizes.SizeOfInitdData);
				writer.WriteUInt32(sectionSizes.SizeOfUninitdData);
				writer.WriteUInt32(ep);
				writer.WriteUInt32(sectionSizes.BaseOfCode);
				writer.WriteUInt32(sectionSizes.BaseOfData);
				writer.WriteUInt32((uint)imageBase);
				writer.WriteUInt32(sectionAlignment);
				writer.WriteUInt32(fileAlignment);
				writer.WriteUInt16(options.MajorOperatingSystemVersion ?? 4);
				writer.WriteUInt16(options.MinorOperatingSystemVersion ?? 0);
				writer.WriteUInt16(options.MajorImageVersion ?? 0);
				writer.WriteUInt16(options.MinorImageVersion ?? 0);
				writer.WriteUInt16(options.MajorSubsystemVersion ?? 4);
				writer.WriteUInt16(options.MinorSubsystemVersion ?? 0);
				writer.WriteUInt32(options.Win32VersionValue ?? 0);
				writer.WriteUInt32(sectionSizes.SizeOfImage);
				writer.WriteUInt32(sectionSizes.SizeOfHeaders);
				checkSumOffset = writer.Position;
				writer.WriteInt32(0);	// CheckSum
				writer.WriteUInt16((ushort)(options.Subsystem ?? PEHeadersOptions.DEFAULT_SUBSYSTEM));
				writer.WriteUInt16((ushort)(options.DllCharacteristics ?? PEHeadersOptions.DefaultDllCharacteristics));
				writer.WriteUInt32((uint)(options.SizeOfStackReserve ?? 0x00100000));
				writer.WriteUInt32((uint)(options.SizeOfStackCommit ?? 0x00001000));
				writer.WriteUInt32((uint)(options.SizeOfHeapReserve ?? 0x00100000));
				writer.WriteUInt32((uint)(options.SizeOfHeapCommit ?? 0x00001000));
				writer.WriteUInt32(options.LoaderFlags ?? 0x00000000);
				writer.WriteUInt32(options.NumberOfRvaAndSizes ?? 0x00000010);
			}
			else {
				writer.WriteUInt16((ushort)0x020B);
				writer.WriteByte(options.MajorLinkerVersion ?? PEHeadersOptions.DEFAULT_MAJOR_LINKER_VERSION);
				writer.WriteByte(options.MinorLinkerVersion ?? PEHeadersOptions.DEFAULT_MINOR_LINKER_VERSION);
				writer.WriteUInt32(sectionSizes.SizeOfCode);
				writer.WriteUInt32(sectionSizes.SizeOfInitdData);
				writer.WriteUInt32(sectionSizes.SizeOfUninitdData);
				writer.WriteUInt32(ep);
				writer.WriteUInt32(sectionSizes.BaseOfCode);
				writer.WriteUInt64(imageBase);
				writer.WriteUInt32(sectionAlignment);
				writer.WriteUInt32(fileAlignment);
				writer.WriteUInt16(options.MajorOperatingSystemVersion ?? 4);
				writer.WriteUInt16(options.MinorOperatingSystemVersion ?? 0);
				writer.WriteUInt16(options.MajorImageVersion ?? 0);
				writer.WriteUInt16(options.MinorImageVersion ?? 0);
				writer.WriteUInt16(options.MajorSubsystemVersion ?? 4);
				writer.WriteUInt16(options.MinorSubsystemVersion ?? 0);
				writer.WriteUInt32(options.Win32VersionValue ?? 0);
				writer.WriteUInt32(sectionSizes.SizeOfImage);
				writer.WriteUInt32(sectionSizes.SizeOfHeaders);
				checkSumOffset = writer.Position;
				writer.WriteInt32(0);	// CheckSum
				writer.WriteUInt16((ushort)(options.Subsystem ?? PEHeadersOptions.DEFAULT_SUBSYSTEM));
				writer.WriteUInt16((ushort)(options.DllCharacteristics ?? PEHeadersOptions.DefaultDllCharacteristics));
				writer.WriteUInt64(options.SizeOfStackReserve ?? 0x0000000000400000);
				writer.WriteUInt64(options.SizeOfStackCommit ?? 0x0000000000004000);
				writer.WriteUInt64(options.SizeOfHeapReserve ?? 0x0000000000100000);
				writer.WriteUInt64(options.SizeOfHeapCommit ?? 0x0000000000002000);
				writer.WriteUInt32(options.LoaderFlags ?? 0x00000000);
				writer.WriteUInt32(options.NumberOfRvaAndSizes ?? 0x00000010);
			}

			writer.WriteDataDirectory(ExportDirectory);
			writer.WriteDataDirectory(ImportDirectory);
			writer.WriteDataDirectory(Win32Resources);
			writer.WriteDataDirectory(null);	// Exception table
			writer.WriteDataDirectory(null);	// Certificate table
			writer.WriteDataDirectory(RelocDirectory);
			writer.WriteDebugDirectory(DebugDirectory);
			writer.WriteDataDirectory(null);	// Architecture-specific data
			writer.WriteDataDirectory(null);	// Global pointer register RVA
			writer.WriteDataDirectory(null);	// Thread local storage
			writer.WriteDataDirectory(null);	// Load configuration table
			writer.WriteDataDirectory(null);	// Bound import table
			writer.WriteDataDirectory(ImportAddressTable);
			writer.WriteDataDirectory(null);	// Delay import descriptor
			writer.WriteDataDirectory(ImageCor20Header);
			writer.WriteDataDirectory(null);	// Reserved

			// Sections
			uint rva = Utils.AlignUp(sectionSizes.SizeOfHeaders, sectionAlignment);
			int emptySections = 0;
			foreach (var section in sections) {
				if (section.GetVirtualSize() != 0)
					rva += section.WriteHeaderTo(writer, fileAlignment, sectionAlignment, rva);
				else
					emptySections++;
			}
			if (emptySections != 0)
				writer.Position += emptySections * 0x28;
		}

		/// <summary>
		/// Calculates the PE checksum and writes it to the checksum field
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="length">Length of PE file</param>
		public void WriteCheckSum(DataWriter writer, long length) {
			writer.Position = startOffset;
			uint checkSum = writer.InternalStream.CalculatePECheckSum(length, checkSumOffset);
			writer.Position = checkSumOffset;
			writer.WriteUInt32(checkSum);
		}

		Machine GetMachine() => options.Machine ?? Machine.I386;

		bool Use32BitOptionalHeader() => !GetMachine().Is64Bit();

		Characteristics GetCharacteristics() {
			var chr = options.Characteristics ?? GetDefaultCharacteristics();
			if (IsExeFile)
				chr &= ~Characteristics.Dll;
			else
				chr |= Characteristics.Dll;
			return chr;
		}

		Characteristics GetDefaultCharacteristics() {
			if (Use32BitOptionalHeader())
				return Characteristics.Bit32Machine | Characteristics.ExecutableImage;
			return Characteristics.ExecutableImage | Characteristics.LargeAddressAware;
		}
	}
}
