using System;
using System.Collections.Generic;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Contains a list of <see cref="IChunk"/>s
	/// </summary>
	public class ChunkList<T> : ChunkListBase<T> where T : IChunk {
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
	}
}
