// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// A <c>IMAGE_DEBUG_DIRECTORY</c>
	/// </summary>
	[DebuggerDisplay("{type}: TS:{timeDateStamp,h} V:{majorVersion,d}.{minorVersion,d} SZ:{sizeOfData} RVA:{addressOfRawData,h} FO:{pointerToRawData,h}")]
	public sealed class ImageDebugDirectory : FileSection {
		readonly uint characteristics;
		readonly uint timeDateStamp;
		readonly ushort majorVersion;
		readonly ushort minorVersion;
		readonly ImageDebugType type;
		readonly uint sizeOfData;
		readonly uint addressOfRawData;
		readonly uint pointerToRawData;

		/// <summary>
		/// Gets the characteristics (reserved)
		/// </summary>
		public uint Characteristics {
			get { return characteristics; }
		}

		/// <summary>
		/// Gets the timestamp
		/// </summary>
		public uint TimeDateStamp {
			get { return timeDateStamp; }
		}

		/// <summary>
		/// Gets the major version
		/// </summary>
		public ushort MajorVersion {
			get { return majorVersion; }
		}

		/// <summary>
		/// Gets the minor version
		/// </summary>
		public ushort MinorVersion {
			get { return minorVersion; }
		}

		/// <summary>
		/// Gets the type
		/// </summary>
		public ImageDebugType Type {
			get { return type; }
		}

		/// <summary>
		/// Gets the size of data
		/// </summary>
		public uint SizeOfData {
			get { return sizeOfData; }
		}

		/// <summary>
		/// RVA of the data
		/// </summary>
		public RVA AddressOfRawData {
			get { return (RVA)addressOfRawData; }
		}

		/// <summary>
		/// File offset of the data
		/// </summary>
		public FileOffset PointerToRawData {
			get { return (FileOffset)pointerToRawData; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageDebugDirectory(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			characteristics = reader.ReadUInt32();
			timeDateStamp = reader.ReadUInt32();
			majorVersion = reader.ReadUInt16();
			minorVersion = reader.ReadUInt16();
			type = (ImageDebugType)reader.ReadUInt32();
			sizeOfData = reader.ReadUInt32();
			addressOfRawData = reader.ReadUInt32();
			pointerToRawData = reader.ReadUInt32();
			SetEndoffset(reader);
		}
	}
}
