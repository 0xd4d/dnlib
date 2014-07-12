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

using System;
﻿using System.IO;
using dnlib.DotNet.Pdb;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Debug directory chunk
	/// </summary>
	public sealed class DebugDirectory : IChunk {
		FileOffset offset;
		RVA rva;
		bool dontWriteAnything;
		uint length;
		internal IMAGE_DEBUG_DIRECTORY debugDirData;
		uint timeDateStamp;
		byte[] data;

		/// <summary>
		/// Size of <see cref="IMAGE_DEBUG_DIRECTORY"/>
		/// </summary>
		public const int HEADER_SIZE = 28;

		/// <summary>
		/// Gets/sets the time date stamp that should be written. This should be the same time date
		/// stamp that is written to the PE header.
		/// </summary>
		public uint TimeDateStamp {
			get { return timeDateStamp; }
			set { timeDateStamp = value; }
		}

		/// <summary>
		/// Gets/sets the raw debug data
		/// </summary>
		public byte[] Data {
			get { return data; }
			set { data = value; }
		}

		/// <summary>
		/// Set it to <c>true</c> if eg. the PDB file couldn't be created. If <c>true</c>, the size
		/// of this chunk will be 0.
		/// </summary>
		public bool DontWriteAnything {
			get { return dontWriteAnything; }
			set {
				if (length != 0)
					throw new InvalidOperationException("SetOffset() has already been called");
				dontWriteAnything = value;
			}
		}

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			length = HEADER_SIZE;
			if (data != null)	// Could be null if dontWriteAnything is true
				length += (uint)data.Length;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			if (dontWriteAnything)
				return 0;
			return length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			if (dontWriteAnything)
				return;

			writer.Write(debugDirData.Characteristics);
			writer.Write(timeDateStamp);
			writer.Write(debugDirData.MajorVersion);
			writer.Write(debugDirData.MinorVersion);
			writer.Write(debugDirData.Type);
			writer.Write(debugDirData.SizeOfData);
			writer.Write((uint)rva + HEADER_SIZE);
			writer.Write((uint)offset + HEADER_SIZE);
			writer.Write(data);
		}
	}
}
