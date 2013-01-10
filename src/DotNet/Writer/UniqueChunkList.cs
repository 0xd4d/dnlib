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
using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Re-uses existing chunks to save space
	/// </summary>
	/// <typeparam name="T">Chunk type</typeparam>
	public sealed class UniqueChunkList<T> : ChunkListBase<T> where T : class, IChunk {
		Dictionary<Elem, Elem> dict;

		/// <summary>
		/// Default constructor
		/// </summary>
		public UniqueChunkList()
			: this(EqualityComparer<T>.Default) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="chunkComparer">Compares the chunk type</param>
		public UniqueChunkList(IEqualityComparer<T> chunkComparer) {
			this.chunks = new List<Elem>();
			this.dict = new Dictionary<Elem, Elem>(new ElemEqualityComparer(chunkComparer));
		}

		/// <inheritdoc/>
		public override void SetOffset(FileOffset offset, RVA rva) {
			dict = null;
			base.SetOffset(offset, rva);
		}

		/// <summary>
		/// Adds a <see cref="IChunk"/> if not already present
		/// </summary>
		/// <param name="chunk">The chunk to add or <c>null</c> if none</param>
		/// <param name="alignment">Chunk alignment</param>
		/// <returns>The original input if it wasn't present, or the cached one</returns>
		public T Add(T chunk, uint alignment) {
			if (setOffsetCalled)
				throw new InvalidOperationException("SetOffset() has already been called");
			if (chunk == null)
				return null;
			var elem = new Elem(chunk, alignment);
			Elem other;
			if (dict.TryGetValue(elem, out other))
				return other.chunk;
			dict[elem] = elem;
			chunks.Add(elem);
			return elem.chunk;
		}
	}
}
