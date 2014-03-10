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

﻿using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Base class of chunk list types
	/// </summary>
	/// <typeparam name="T">Chunk type</typeparam>
	public abstract class ChunkListBase<T> : IChunk where T : IChunk {
		/// <summary>All chunks</summary>
		protected List<Elem> chunks;
		uint length;
		uint virtualSize;
		/// <summary><c>true</c> if <see cref="SetOffset"/> has been called</summary>
		protected bool setOffsetCalled;
		FileOffset offset;
		RVA rva;

		/// <summary>
		/// Helper struct
		/// </summary>
		protected struct Elem {
			/// <summary>Data</summary>
			public readonly T chunk;
			/// <summary>Alignment</summary>
			public readonly uint alignment;

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="chunk">Chunk</param>
			/// <param name="alignment">Alignment</param>
			public Elem(T chunk, uint alignment) {
				this.chunk = chunk;
				this.alignment = alignment;
			}
		}

		/// <summary>
		/// Equality comparer for <see cref="Elem"/>
		/// </summary>
		protected sealed class ElemEqualityComparer : IEqualityComparer<Elem> {
			IEqualityComparer<T> chunkComparer;

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="chunkComparer">Compares the chunk type</param>
			public ElemEqualityComparer(IEqualityComparer<T> chunkComparer) {
				this.chunkComparer = chunkComparer;
			}

			/// <inheritdoc/>
			public bool Equals(Elem x, Elem y) {
				return x.alignment == y.alignment &&
					chunkComparer.Equals(x.chunk, y.chunk);
			}

			/// <inheritdoc/>
			public int GetHashCode(Elem obj) {
				return (int)obj.alignment + chunkComparer.GetHashCode(obj.chunk);
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
		public virtual void SetOffset(FileOffset offset, RVA rva) {
			setOffsetCalled = true;
			this.offset = offset;
			this.rva = rva;
			length = 0;
			virtualSize = 0;
			foreach (var elem in chunks) {
				uint paddingF = (uint)offset.AlignUp(elem.alignment) - (uint)offset;
				uint paddingV = (uint)rva.AlignUp(elem.alignment) - (uint)rva;
				offset += paddingF;
				rva += paddingV;
				elem.chunk.SetOffset(offset, rva);
				uint chunkLenF = elem.chunk.GetFileLength();
				uint chunkLenV = elem.chunk.GetVirtualSize();
				offset += chunkLenF;
				rva += chunkLenV;
				length += paddingF + chunkLenF;
				virtualSize += paddingV + chunkLenV;
			}
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return virtualSize;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			FileOffset offset2 = offset;
			foreach (var elem in chunks) {
				int paddingF = (int)offset2.AlignUp(elem.alignment) - (int)offset2;
				writer.WriteZeros(paddingF);
				elem.chunk.VerifyWriteTo(writer);
				offset2 += (uint)paddingF + elem.chunk.GetFileLength();
			}
		}
	}
}
