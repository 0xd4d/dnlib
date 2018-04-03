// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
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

		internal bool IsEmpty => chunks.Count == 0;

		/// <summary>
		/// Helper struct
		/// </summary>
		protected readonly struct Elem {
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
			public ElemEqualityComparer(IEqualityComparer<T> chunkComparer) => this.chunkComparer = chunkComparer;

			/// <inheritdoc/>
			public bool Equals(Elem x, Elem y) =>
				x.alignment == y.alignment &&
				chunkComparer.Equals(x.chunk, y.chunk);

			/// <inheritdoc/>
			public int GetHashCode(Elem obj) => (int)obj.alignment + chunkComparer.GetHashCode(obj.chunk);
		}

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

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
				if (elem.chunk.GetVirtualSize() == 0) {
					offset -= paddingF;
					rva -= paddingV;
				}
				else {
					uint chunkLenF = elem.chunk.GetFileLength();
					uint chunkLenV = elem.chunk.GetVirtualSize();
					offset += chunkLenF;
					rva += chunkLenV;
					length += paddingF + chunkLenF;
					virtualSize += paddingV + chunkLenV;
				}
			}
		}

		/// <inheritdoc/>
		public uint GetFileLength() => length;

		/// <inheritdoc/>
		public uint GetVirtualSize() => virtualSize;

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			var offset2 = offset;
			foreach (var elem in chunks) {
				if (elem.chunk.GetVirtualSize() == 0)
					continue;
				int paddingF = (int)offset2.AlignUp(elem.alignment) - (int)offset2;
				writer.WriteZeroes(paddingF);
				elem.chunk.VerifyWriteTo(writer);
				offset2 += (uint)paddingF + elem.chunk.GetFileLength();
			}
		}
	}
}
