using System.Collections.Generic;
using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	abstract class ChunkListBase<T> : IChunk where T : IChunk {
		protected List<Elem> chunks;
		RVA rva;
		uint length;
		protected bool setOffsetCalled;

		protected struct Elem {
			public readonly T chunk;
			public readonly uint alignment;

			public Elem(T chunk, uint alignment) {
				this.chunk = chunk;
				this.alignment = alignment;
			}

			/// <inheritdoc/>
			public override int GetHashCode() {
				int hash = 0;
				if (chunk != null)
					hash += chunk.GetHashCode();
				hash += (int)alignment;
				return hash;
			}

			/// <inheritdoc/>
			public override bool Equals(object obj) {
				if (!(obj is Elem))
					return false;
				var other = (Elem)obj;
				return alignment == other.alignment &&
					chunk.Equals(other.chunk);
			}
		}

		/// <inheritdoc/>
		public virtual void SetOffset(FileOffset offset, RVA rva) {
			setOffsetCalled = true;
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
				writer.WriteZeros((int)rva2.AlignUp(elem.alignment) - (int)rva2);
				elem.chunk.VerifyWriteTo(writer);
				rva2 += elem.chunk.GetLength();
			}
		}
	}
}
