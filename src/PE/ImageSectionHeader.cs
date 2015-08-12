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
		public string DisplayName {
			get { return displayName; }
		}

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.Name field
		/// </summary>
		public byte[] Name {
			get { return name; }
		}

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.VirtualSize field
		/// </summary>
		public uint VirtualSize {
			get { return virtualSize; }
		}

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.VirtualAddress field
		/// </summary>
		public RVA VirtualAddress {
			get { return virtualAddress; }
		}

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.SizeOfRawData field
		/// </summary>
		public uint SizeOfRawData {
			get { return sizeOfRawData; }
		}

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.PointerToRawData field
		/// </summary>
		public uint PointerToRawData {
			get { return pointerToRawData; }
		}

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.PointerToRelocations field
		/// </summary>
		public uint PointerToRelocations {
			get { return pointerToRelocations; }
		}

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.PointerToLinenumbers field
		/// </summary>
		public uint PointerToLinenumbers {
			get { return pointerToLinenumbers; }
		}

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.NumberOfRelocations field
		/// </summary>
		public ushort NumberOfRelocations {
			get { return numberOfRelocations; }
		}

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.NumberOfLinenumbers field
		/// </summary>
		public ushort NumberOfLinenumbers {
			get { return numberOfLinenumbers; }
		}

		/// <summary>
		/// Returns the IMAGE_SECTION_HEADER.Characteristics field
		/// </summary>
		public uint Characteristics {
			get { return characteristics; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageSectionHeader(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			this.name = reader.ReadBytes(8);
			this.virtualSize = reader.ReadUInt32();
			this.virtualAddress = (RVA)reader.ReadUInt32();
			this.sizeOfRawData = reader.ReadUInt32();
			this.pointerToRawData = reader.ReadUInt32();
			this.pointerToRelocations = reader.ReadUInt32();
			this.pointerToLinenumbers = reader.ReadUInt32();
			this.numberOfRelocations = reader.ReadUInt16();
			this.numberOfLinenumbers = reader.ReadUInt16();
			this.characteristics = reader.ReadUInt32();
			SetEndoffset(reader);
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
