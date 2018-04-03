// dnlib: See LICENSE.txt for more info

using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Base class of most heaps
	/// </summary>
	public abstract class HeapBase : IHeap {
		internal const uint ALIGNMENT = 4;
		FileOffset offset;
		RVA rva;

		/// <summary>
		/// <c>true</c> if <see cref="SetReadOnly"/> has been called
		/// </summary>
		protected bool isReadOnly;

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <inheritdoc/>
		public abstract string Name { get; }

		/// <inheritdoc/>
		public bool IsEmpty => GetRawLength() <= 1;

		/// <summary>
		/// <c>true</c> if offsets require 4 bytes instead of 2 bytes.
		/// </summary>
		public bool IsBig => GetFileLength() > 0xFFFF;

		/// <inheritdoc/>
		public void SetReadOnly() => isReadOnly = true;

		/// <inheritdoc/>
		public virtual void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			// NOTE: This method can be called twice by NativeModuleWriter, see Metadata.SetOffset() for more info
		}

		/// <inheritdoc/>
		public uint GetFileLength() => Utils.AlignUp(GetRawLength(), ALIGNMENT);

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <summary>
		/// Gets the raw length of the heap
		/// </summary>
		/// <returns>Raw length of the heap</returns>
		public abstract uint GetRawLength();

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			WriteToImpl(writer);
			writer.WriteZeroes((int)(Utils.AlignUp(GetRawLength(), ALIGNMENT) - GetRawLength()));
		}

		/// <summary>
		/// Writes all data to <paramref name="writer"/> at its current location.
		/// </summary>
		/// <param name="writer">Destination</param>
		protected abstract void WriteToImpl(DataWriter writer);

		/// <inheritdoc/>
		public override string ToString() => Name;
	}
}
