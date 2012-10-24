using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Strong name signature chunk
	/// </summary>
	public class StrongNameSignature : IChunk {
		FileOffset offset;
		RVA rva;
		int size;

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
		/// <param name="size">Size of strong name signature</param>
		public StrongNameSignature(int size) {
			this.size = size;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetLength() {
			return (uint)this.size;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			writer.WriteZeros(size);
		}
	}
}
