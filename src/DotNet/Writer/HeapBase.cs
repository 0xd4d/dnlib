using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Base class of most heaps
	/// </summary>
	public abstract class HeapBase : IHeap {
		const uint ALIGNMENT = 4;
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
			get { return GetRawLength() <= 1; }
		}

		/// <summary>
		/// <c>true</c> if offsets require 4 bytes instead of 2 bytes.
		/// </summary>
		public bool IsBig {
			get { return GetLength() > 0xFFFF; }
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetLength() {
			return Utils.AlignUp(GetRawLength(), ALIGNMENT);
		}

		/// <inheritdoc/>
		public abstract uint GetRawLength();

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			WriteToImpl(writer);
			writer.WriteZeros((int)(Utils.AlignUp(GetRawLength(), ALIGNMENT) - GetRawLength()));
		}

		/// <inheritdoc/>
		public abstract void WriteToImpl(BinaryWriter writer);

		/// <inheritdoc/>
		public override string ToString() {
			return Name;
		}
	}
}
