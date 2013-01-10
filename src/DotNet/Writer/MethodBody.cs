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

﻿using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Method body chunk
	/// </summary>
	public sealed class MethodBody : IChunk {
		const uint EXTRA_SECTIONS_ALIGNMENT = 4;

		readonly bool isTiny;
		readonly byte[] code;
		readonly byte[] extraSections;
		uint length;
		FileOffset offset;
		RVA rva;

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Gets the code
		/// </summary>
		public byte[] Code {
			get { return code; }
		}

		/// <summary>
		/// Gets the extra sections (exception handlers) or <c>null</c>
		/// </summary>
		public byte[] ExtraSections {
			get { return extraSections; }
		}

		/// <summary>
		/// <c>true</c> if it's a fat body
		/// </summary>
		public bool IsFat {
			get { return !isTiny; }
		}

		/// <summary>
		/// <c>true</c> if it's a tiny body
		/// </summary>
		public bool IsTiny {
			get { return isTiny; }
		}

		/// <summary>
		/// <c>true</c> if there's an extra section
		/// </summary>
		public bool HasExtraSections {
			get { return extraSections != null && extraSections.Length > 0; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Code</param>
		public MethodBody(byte[] code)
			: this(code, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Code</param>
		/// <param name="extraSections">Extra sections or <c>null</c></param>
		public MethodBody(byte[] code, byte[] extraSections) {
			this.isTiny = (code[0] & 3) == 2;
			this.code = code;
			this.extraSections = extraSections;
		}

		/// <summary>
		/// Gets the approximate size of the method body (code + exception handlers)
		/// </summary>
		public int GetSizeOfMethodBody() {
			int len = code.Length;
			if (extraSections != null) {
				len = Utils.AlignUp(len, EXTRA_SECTIONS_ALIGNMENT);
				len += extraSections.Length;
				len = Utils.AlignUp(len, EXTRA_SECTIONS_ALIGNMENT);
			}
			return len;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
			if (HasExtraSections) {
				RVA rva2 = rva + (uint)code.Length;
				rva2 = rva2.AlignUp(EXTRA_SECTIONS_ALIGNMENT);
				rva2 += (uint)extraSections.Length;
				length = (uint)rva2 - (uint)rva;
			}
			else
				length = (uint)code.Length;
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
			writer.Write(code);
			if (HasExtraSections) {
				RVA rva2 = rva + (uint)code.Length;
				writer.WriteZeros((int)rva2.AlignUp(EXTRA_SECTIONS_ALIGNMENT) - (int)rva2);
				writer.Write(extraSections);
			}
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return Utils.GetHashCode(code) + Utils.GetHashCode(extraSections);
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			var other = obj as MethodBody;
			if (other == null)
				return false;
			return Utils.Equals(code, other.code) &&
				Utils.Equals(extraSections, other.extraSections);
		}
	}
}
