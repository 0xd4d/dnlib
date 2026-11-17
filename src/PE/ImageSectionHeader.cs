// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Text;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_SECTION_HEADER PE section
	/// </summary>
	[DebuggerDisplay("RVA:{virtualAddress} VS:{virtualSize} FO:{pointerToRawData} FS:{sizeOfRawData} {displayName}")]
	public sealed class ImageSectionHeader : FileSection {
		readonly string displayName;
		readonly byte[] name;
		readonly uint virtualSize;
		readonly RVA virtualAddress;
		readonly uint sizeOfRawData;
		readonly uint pointerToRawData;
		readonly uint pointerToRelocations;
		readonly uint pointerToLinenumbers;
		readonly ushort numberOfRelocations;
		readonly ushort numberOfLinenumbers;
		readonly uint characteristics;

		/// <summary>
		/// Returns the human readable section name, ignoring everything after
		/// the first nul byte
		/// </summary>
		public string DisplayName => displayName;

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.Name field
		/// </summary>
		public byte[] Name => name;

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.VirtualSize field
		/// </summary>
		public uint VirtualSize => virtualSize;

		/// <summary>
		/// Gets the size of the section in memory. If <see cref="VirtualSize"/> is 0, the Windows loader
		/// uses <see cref="SizeOfRawData"/> instead (some linkers don't initialize VirtualSize).
		/// </summary>
		internal uint LoadedVirtualSize => virtualSize != 0 ? virtualSize : sizeOfRawData;

		/// <summary>
		/// Gets the number of bytes the Windows loader reads from the file. If <see cref="PointerToRawData"/>
		/// is 0, the section has no file data (eg. uninitialized data) and this property returns 0.
		/// </summary>
		internal uint LoadedSizeOfRawData => pointerToRawData != 0 ? sizeOfRawData : 0;

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.VirtualAddress field
		/// </summary>
		public RVA VirtualAddress => virtualAddress;

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.SizeOfRawData field
		/// </summary>
		public uint SizeOfRawData => sizeOfRawData;

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.PointerToRawData field
		/// </summary>
		public uint PointerToRawData => pointerToRawData;

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.PointerToRelocations field
		/// </summary>
		public uint PointerToRelocations => pointerToRelocations;

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.PointerToLinenumbers field
		/// </summary>
		public uint PointerToLinenumbers => pointerToLinenumbers;

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.NumberOfRelocations field
		/// </summary>
		public ushort NumberOfRelocations => numberOfRelocations;

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.NumberOfLinenumbers field
		/// </summary>
		public ushort NumberOfLinenumbers => numberOfLinenumbers;

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.Characteristics field
		/// </summary>
		public uint Characteristics => characteristics;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageSectionHeader(ref DataReader reader, bool verify) {
			SetStartOffset(ref reader);
			name = reader.ReadBytes(8);
			virtualSize = reader.ReadUInt32();
			virtualAddress = (RVA)reader.ReadUInt32();
			sizeOfRawData = reader.ReadUInt32();
			pointerToRawData = reader.ReadUInt32();
			pointerToRelocations = reader.ReadUInt32();
			pointerToLinenumbers = reader.ReadUInt32();
			numberOfRelocations = reader.ReadUInt16();
			numberOfLinenumbers = reader.ReadUInt16();
			characteristics = reader.ReadUInt32();
			SetEndoffset(ref reader);
			displayName = ToString(name);
		}

		static string ToString(byte[] name) {
			var sb = new StringBuilder(name.Length);
			foreach (var b in name) {
				if (b == 0)
					break;
				sb.Append((char)b);
			}
			return sb.ToString();
		}
	}
}
