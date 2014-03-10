/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

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
