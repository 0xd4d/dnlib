// dnlib: See LICENSE.txt for more info

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
		/// Add a <see cref="IChunk"/>
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
		/// <returns>Alignment of the chunk, or <c>null</c> if the chunk cannot be removed.</returns>
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
