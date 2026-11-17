// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_FILE_HEADER PE section
	/// </summary>
	public sealed class ImageFileHeader : FileSection {
		readonly Machine machine;
		readonly ushort numberOfSections;
		readonly uint timeDateStamp;
		readonly uint pointerToSymbolTable;
		readonly uint numberOfSymbols;
		readonly ushort sizeOfOptionalHeader;
		readonly Characteristics characteristics;

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.Machine field
		/// </summary>
		public Machine Machine => machine;

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.NumberOfSections field
		/// </summary>
		public int NumberOfSections => numberOfSections;

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.TimeDateStamp field
		/// </summary>
		public uint TimeDateStamp => timeDateStamp;

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.PointerToSymbolTable field
		/// </summary>
		public uint PointerToSymbolTable => pointerToSymbolTable;

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.NumberOfSymbols field
		/// </summary>
		public uint NumberOfSymbols => numberOfSymbols;

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.SizeOfOptionalHeader field
		/// </summary>
		public uint SizeOfOptionalHeader => sizeOfOptionalHeader;

		/// <summary>
		/// Returns the IMAGE_FILE_HEADER.Characteristics field
		/// </summary>
		public Characteristics Characteristics => characteristics;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageFileHeader(ref DataReader reader, bool verify) {
			SetStartOffset(ref reader);
			machine = (Machine)reader.ReadUInt16();
			numberOfSections = reader.ReadUInt16();
			timeDateStamp = reader.ReadUInt32();
			pointerToSymbolTable = reader.ReadUInt32();
			numberOfSymbols = reader.ReadUInt32();
			sizeOfOptionalHeader = reader.ReadUInt16();
			characteristics = (Characteristics)reader.ReadUInt16();
			SetEndoffset(ref reader);
			if (verify && sizeOfOptionalHeader == 0)
				throw new BadImageFormatException("Invalid SizeOfOptionalHeader");
		}
	}
}
