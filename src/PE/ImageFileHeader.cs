using System;
using System.IO;
using dot10.IO;

namespace dot10.PE {
	/// <summary>
	/// IMAGE_FILE_HEADER.Machine enum
	/// </summary>
	public enum Machine : ushort {
		/// <summary>
		/// x86
		/// </summary>
		I386 = 0x014C,

		/// <summary>
		/// IA-64
		/// </summary>
		IA64 = 0x0200,

		/// <summary>
		/// x64
		/// </summary>
		AMD64 = 0x8664,
	}

	/// <summary>
	/// Represents the IMAGE_FILE_HEADER PE section
	/// </summary>
	public class ImageFileHeader : FileSection {
		Machine machine;
		ushort numberOfSections;
		uint timeDateStamp;
		uint pointerToSymbolTable;
		uint numberOfSymbols;
		ushort sizeOfOptionalHeader;
		ushort characteristics;

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.Machine field
		/// </summary>
		public Machine Machine {
			get { return machine; }
		}

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.NumberOfSections field
		/// </summary>
		public int NumberOfSections {
			get { return numberOfSections; }
		}

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.TimeDateStamp field
		/// </summary>
		public uint TimeDateStamp {
			get { return timeDateStamp; }
		}

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.PointerToSymbolTable field
		/// </summary>
		public uint PointerToSymbolTable {
			get { return pointerToSymbolTable; }
		}

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.NumberOfSymbols field
		/// </summary>
		public uint NumberOfSymbols {
			get { return numberOfSymbols; }
		}

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.SizeOfOptionalHeader field
		/// </summary>
		public uint SizeOfOptionalHeader {
			get { return sizeOfOptionalHeader; }
		}

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.Characteristics field
		/// </summary>
		public ushort Characteristics {
			get { return characteristics; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageFileHeader(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			this.machine = (Machine)reader.ReadUInt16();
			this.numberOfSections = reader.ReadUInt16();
			this.timeDateStamp = reader.ReadUInt32();
			this.pointerToSymbolTable = reader.ReadUInt32();
			this.numberOfSymbols = reader.ReadUInt32();
			this.sizeOfOptionalHeader = reader.ReadUInt16();
			this.characteristics = reader.ReadUInt16();
			SetEndoffset(reader);
			if (verify && this.sizeOfOptionalHeader == 0)
				throw new BadImageFormatException("Invalid SizeOfOptionalHeader");
		}
	}
}
