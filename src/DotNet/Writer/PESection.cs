// dnlib: See LICENSE.txt for more info

using System.Text;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// A PE section
	/// </summary>
	public sealed class PESection : ChunkList<IChunk> {
		string name;
		uint characteristics;

		/// <summary>
		/// Gets the name
		/// </summary>
		public string Name {
			get => name;
			set => name = value;
		}

		/// <summary>
		/// Gets the Characteristics
		/// </summary>
		public uint Characteristics {
			get => characteristics;
			set => characteristics = value;
		}

		/// <summary>
		/// <c>true</c> if this is a code section
		/// </summary>
		public bool IsCode => (characteristics & 0x20) != 0;

		/// <summary>
		/// <c>true</c> if this is an initialized data section
		/// </summary>
		public bool IsInitializedData => (characteristics & 0x40) != 0;

		/// <summary>
		/// <c>true</c> if this is an uninitialized data section
		/// </summary>
		public bool IsUninitializedData => (characteristics & 0x80) != 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Section name</param>
		/// <param name="characteristics">Section characteristics</param>
		public PESection(string name, uint characteristics) {
			this.name = name;
			this.characteristics = characteristics;
		}

		/// <summary>
		/// Writes the section header to <paramref name="writer"/> at its current position.
		/// Returns aligned virtual size (aligned to <paramref name="sectionAlignment"/>)
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="fileAlignment">File alignment</param>
		/// <param name="sectionAlignment">Section alignment</param>
		/// <param name="rva">Current <see cref="RVA"/></param>
		public uint WriteHeaderTo(DataWriter writer, uint fileAlignment, uint sectionAlignment, uint rva) {
			uint vs = GetVirtualSize();
			uint fileLen = GetFileLength();
			uint alignedVs = Utils.AlignUp(vs, sectionAlignment);
			uint rawSize = Utils.AlignUp(fileLen, fileAlignment);
			uint dataOffset = (uint)FileOffset;

			writer.WriteBytes(Encoding.UTF8.GetBytes(Name + "\0\0\0\0\0\0\0\0"), 0, 8);
			writer.WriteUInt32(vs);			// VirtualSize
			writer.WriteUInt32(rva);		// VirtualAddress
			writer.WriteUInt32(rawSize);	// SizeOfRawData
			writer.WriteUInt32(dataOffset);	// PointerToRawData
			writer.WriteInt32(0);			// PointerToRelocations
			writer.WriteInt32(0);			// PointerToLinenumbers
			writer.WriteUInt16(0);			// NumberOfRelocations
			writer.WriteUInt16(0);			// NumberOfLinenumbers
			writer.WriteUInt32(Characteristics);

			return alignedVs;
		}
	}
}
