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
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// A <see cref="IBinaryReader"/> chunk
	/// </summary>
	public class BinaryReaderChunk : IChunk {
		FileOffset offset;
		RVA rva;
		readonly IBinaryReader data;
		readonly uint virtualSize;

		/// <summary>
		/// Gets the data
		/// </summary>
		public IBinaryReader Data {
			get { return data; }
		}

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The data</param>
		public BinaryReaderChunk(IBinaryReader data)
			: this(data, (uint)data.Length) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The data</param>
		/// <param name="virtualSize">Virtual size of <paramref name="data"/></param>
		public BinaryReaderChunk(IBinaryReader data, uint virtualSize) {
			this.data = data;
			this.virtualSize = virtualSize;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return (uint)data.Length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return virtualSize;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			data.Position = 0;
			data.WriteTo(writer);
		}
	}
}
