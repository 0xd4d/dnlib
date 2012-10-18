using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	class ImportAddressTable : IChunk {
		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			throw new System.NotImplementedException();
		}

		/// <inheritdoc/>
		public uint GetLength() {
			throw new System.NotImplementedException();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			throw new System.NotImplementedException();
		}
	}
}
