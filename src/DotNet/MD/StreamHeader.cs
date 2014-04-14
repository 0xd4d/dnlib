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
using System.Diagnostics;
using System.IO;
using System.Text;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// A metadata stream header
	/// </summary>
	[DebuggerDisplay("O:{offset} L:{streamSize} {name}")]
	public sealed class StreamHeader : FileSection {
		readonly uint offset;
		readonly uint streamSize;
		readonly string name;

		/// <summary>
		/// The offset of the stream relative to the start of the MetaData header
		/// </summary>
		public uint Offset {
			get { return offset; }
		}

		/// <summary>
		/// The size of the stream
		/// </summary>
		public uint StreamSize {
			get { return streamSize; }
		}

		/// <summary>
		/// The name of the stream
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public StreamHeader(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			this.offset = reader.ReadUInt32();
			this.streamSize = reader.ReadUInt32();
			this.name = ReadString(reader, 32, verify);
			SetEndoffset(reader);
			if (verify && offset + size < offset)
				throw new BadImageFormatException("Invalid stream header");
		}

		static string ReadString(IImageStream reader, int maxLen, bool verify) {
			var origPos = reader.Position;
			var sb = new StringBuilder(maxLen);
			int i;
			for (i = 0; i < maxLen; i++) {
				byte b = reader.ReadByte();
				if (b == 0)
					break;
				sb.Append((char)b);
			}
			if (verify && i == maxLen)
				throw new BadImageFormatException("Invalid stream name string");
			if (i != maxLen)
				reader.Position = origPos + ((i + 1 + 3) & ~3);
			return sb.ToString();
		}
	}
}
