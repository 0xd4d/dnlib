using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	class StrongNameSignature : IChunk {
		FileOffset offset;
		int size;

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
