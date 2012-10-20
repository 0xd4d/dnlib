using System.Collections.Generic;
using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Base class of chunk list types
	/// </summary>
	/// <typeparam name="T">Chunk type</typeparam>
	abstract class ChunkListBase<T> : IChunk where T : IChunk {
		protected List<Elem> chunks;
		uint length;
		protected bool setOffsetCalled;
		FileOffset offset;
		RVA rva;

		protected struct Elem {
			public readonly T chunk;
			public readonly uint alignment;

			public Elem(T chunk, uint alignment) {
				this.chunk = chunk;
				this.alignment = alignment;
			}
		}

		/// <summary>
		/// Equality comparer for <see cref="Elem"/>
		/// </summary>
		protected class ElemEqualityComparer : IEqualityComparer<Elem> {
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
			foreach (var elem in chunks) {
				uint padding = (uint)rva.AlignUp(elem.alignment) - (uint)rva;
				offset += padding;
				rva += padding;
				elem.chunk.SetOffset(offset, rva);
				uint chunkLen = elem.chunk.GetLength();
				offset += chunkLen;
				rva += chunkLen;
				length += chunkLen;
			}
		}

		/// <inheritdoc/>
		public uint GetLength() {
			return length;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			RVA rva2 = rva;
			foreach (var elem in chunks) {
				int padding = (int)rva2.AlignUp(elem.alignment) - (int)rva2;
				writer.WriteZeros(padding);
				elem.chunk.VerifyWriteTo(writer);
				rva2 += (uint)padding + elem.chunk.GetLength();
			}
		}
	}
}
