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
using System.Collections.Generic;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Contains a list of <see cref="IChunk"/>s
	/// </summary>
	public class ChunkList<T> : ChunkListBase<T> where T : class, IChunk {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ChunkList() {
			this.chunks = new List<Elem>();
		}

		/// <summary>
		/// Adds a <see cref="IChunk"/>
		/// </summary>
		/// <param name="chunk">The chunk to add or <c>null</c> if none</param>
		/// <param name="alignment">Chunk alignment</param>
		public void Add(T chunk, uint alignment) {
			if (setOffsetCalled)
				throw new InvalidOperationException("SetOffset() has already been called");
			if (chunk != null)
				chunks.Add(new Elem(chunk, alignment));
		}

		/// <summary>
		/// Remove a <see cref="IChunk"/>
		/// </summary>
		/// <param name="chunk">The chunk to remove or <c>null</c> if none</param>
		/// <returns>Alignment of the chunk, or null if the chunk cannot be removed.</returns>
		public uint? Remove(T chunk) {
			if (setOffsetCalled)
				throw new InvalidOperationException("SetOffset() has already been called");
			if (chunk != null) {
				for (int i = 0; i < chunks.Count; i++) {
					if (chunks[i].chunk == chunk) {
						uint alignment = chunks[i].alignment;
						chunks.RemoveAt(i);
						return alignment;
					}
				}
			}
			return null;
		}
	}
}
