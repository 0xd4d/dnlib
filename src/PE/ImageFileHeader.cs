/*
    Copyright (C) 2012-2013 de4dot@gmail.com

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

﻿using System;
using System.IO;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_FILE_HEADER PE section
	/// </summary>
	public sealed class ImageFileHeader : FileSection {
		Machine machine;
		ushort numberOfSections;
		uint timeDateStamp;
		uint pointerToSymbolTable;
		uint numberOfSymbols;
		ushort sizeOfOptionalHeader;
		Characteristics characteristics;

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
		public Characteristics Characteristics {
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
			this.characteristics = (Characteristics)reader.ReadUInt16();
			SetEndoffset(reader);
			if (verify && this.sizeOfOptionalHeader == 0)
				throw new BadImageFormatException("Invalid SizeOfOptionalHeader");
		}
	}
}
