using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Debug directory chunk
	/// </summary>
	class DebugDirectory : IChunk {
		FileOffset offset;

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
		}

		/// <inheritdoc/>
		public uint GetLength() {
			return 0;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
		}
	}
}
