// dnlib: See LICENSE.txt for more info

﻿using System.IO;
using System.Text;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Import directory chunk
	/// </summary>
	public sealed class ImportDirectory : IChunk {
		FileOffset offset;
		RVA rva;
		bool isExeFile;
		uint length;
		RVA importLookupTableRVA;
		RVA corXxxMainRVA;
		RVA mscoreeDllRVA;
		int stringsPadding;

		/// <summary>
		/// Gets/sets the <see cref="ImportAddressTable"/>
		/// </summary>
		public ImportAddressTable ImportAddressTable { get; set; }

		/// <summary>
		/// Gets the RVA of _CorDllMain/_CorExeMain in the import lookup table
		/// </summary>
		public RVA CorXxxMainRVA {
			get { return corXxxMainRVA; }
		}

		/// <summary>
		/// Gets RVA of _CorExeMain/_CorDllMain in the IAT
		/// </summary>
		public RVA IatCorXxxMainRVA {
			get { return ImportAddressTable.RVA; }
		}

		/// <summary>
		/// Gets/sets a value indicating whether this is a EXE or a DLL file
		/// </summary>
		public bool IsExeFile {
			get { return isExeFile; }
			set { isExeFile = value; }
		}

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		const uint STRINGS_ALIGNMENT = 16;

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			length = 0x28;
			importLookupTableRVA = rva + length;
			length += 8;

			stringsPadding = (int)(rva.AlignUp(STRINGS_ALIGNMENT) - rva);
			length += (uint)stringsPadding;
			corXxxMainRVA = rva + length;
			length += 0xE;
			mscoreeDllRVA = rva + length;
			length += 0xC;
			length++;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			writer.Write((uint)importLookupTableRVA);
			writer.Write(0);	// DateTimeStamp
			writer.Write(0);	// ForwarderChain
			writer.Write((uint)mscoreeDllRVA);	// Name
			writer.Write((uint)ImportAddressTable.RVA);
			writer.Write(0UL);
			writer.Write(0UL);
			writer.Write(0);

			// ImportLookupTable
			writer.Write((uint)corXxxMainRVA);
			writer.Write(0);

			writer.WriteZeros(stringsPadding);
			writer.Write((ushort)0);
			writer.Write(Encoding.UTF8.GetBytes(IsExeFile ? "_CorExeMain\0" : "_CorDllMain\0"));
			writer.Write(Encoding.UTF8.GetBytes("mscoree.dll\0"));

			writer.Write((byte)0);
		}
	}
}
