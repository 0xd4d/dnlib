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

﻿using System;
using System.IO;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_NT_HEADERS PE section
	/// </summary>
	public sealed class ImageNTHeaders : FileSection {
		readonly uint signature;
		readonly ImageFileHeader imageFileHeader;
		readonly IImageOptionalHeader imageOptionalHeader;

		/// <summary>
		/// Returns the IMAGE_NT_HEADERS.Signature field
		/// </summary>
		public uint Signature {
			get { return signature; }
		}

		/// <summary>
		/// Returns the IMAGE_NT_HEADERS.FileHeader field
		/// </summary>
		public ImageFileHeader FileHeader {
			get { return imageFileHeader; }
		}

		/// <summary>
		/// Returns the IMAGE_NT_HEADERS.OptionalHeader field
		/// </summary>
		public IImageOptionalHeader OptionalHeader {
			get { return imageOptionalHeader; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageNTHeaders(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			this.signature = reader.ReadUInt32();
			if (verify && this.signature != 0x4550)
				throw new BadImageFormatException("Invalid NT headers signature");
			this.imageFileHeader = new ImageFileHeader(reader, verify);
			this.imageOptionalHeader = CreateImageOptionalHeader(reader, verify);
			SetEndoffset(reader);
		}

		/// <summary>
		/// Creates an IImageOptionalHeader
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of the optional header</param>
		/// <param name="verify">Verify section</param>
		/// <returns>The created IImageOptionalHeader</returns>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		IImageOptionalHeader CreateImageOptionalHeader(IImageStream reader, bool verify) {
			ushort magic = reader.ReadUInt16();
			reader.Position -= 2;
			switch (magic) {
			case 0x010B: return new ImageOptionalHeader32(reader, imageFileHeader.SizeOfOptionalHeader, verify);
			case 0x020B: return new ImageOptionalHeader64(reader, imageFileHeader.SizeOfOptionalHeader, verify);
			default: throw new BadImageFormatException("Invalid optional header magic");
			}
		}
	}
}
