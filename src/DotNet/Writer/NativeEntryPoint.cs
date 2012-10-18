using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Stores the instruction that jumps to _CorExeMain/_CorDllMain
	/// </summary>
	class NativeEntryPoint : IChunk {
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
