using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Base class of most heaps
	/// </summary>
	public abstract class HeapBase : IHeap {
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

		/// <inheritdoc/>
		public abstract string Name { get; }

		/// <inheritdoc/>
		public bool IsEmpty {
			get { return GetLength() <= 1; }
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public abstract uint GetLength();

		/// <inheritdoc/>
		public abstract void WriteTo(BinaryWriter writer);

		/// <inheritdoc/>
		public override string ToString() {
			return Name;
		}
	}
}
