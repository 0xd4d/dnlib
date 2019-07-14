// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Stores a byte array
	/// </summary>
	public sealed class ByteArrayChunk : IReuseChunk {
		readonly byte[] array;
		FileOffset offset;
		RVA rva;

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <summary>
		/// Gets the data
		/// </summary>
		public byte[] Data => array;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="array">The data. It will be owned by this instance and can't be modified by
		/// other code if this instance is inserted as a <c>key</c> in a dictionary (because
		/// <see cref="GetHashCode"/> return value will be different if you modify the array). If
		/// it's never inserted as a <c>key</c> in a dictionary, then the contents can be modified,
		/// but shouldn't be resized after <see cref="SetOffset"/> has been called.</param>
		public ByteArrayChunk(byte[] array) => this.array = array ?? Array2.Empty<byte>();

		bool IReuseChunk.CanReuse(RVA origRva, uint origSize) => (uint)array.Length <= origSize;

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() => (uint)array.Length;

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) => writer.WriteBytes(array);

		/// <inheritdoc/>
		public override int GetHashCode() => Utils.GetHashCode(array);

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			var other = obj as ByteArrayChunk;
			return !(other is null) && Utils.Equals(array, other.array);
		}
	}
}
