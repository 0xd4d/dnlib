using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Stores a byte array
	/// </summary>
	public class ByteArrayChunk : IChunk {
		byte[] array;
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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="array">The data (now owned by us and can't be modified by the caller)</param>
		public ByteArrayChunk(byte[] array) {
			this.array = array ?? new byte[0];
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetLength() {
			return (uint)array.Length;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			writer.Write(array);
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return Utils.GetHashCode(array);
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			var other = obj as ByteArrayChunk;
			return other != null && Utils.Equals(array, other.array);
		}
	}
}
