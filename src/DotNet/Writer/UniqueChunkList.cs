// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
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
