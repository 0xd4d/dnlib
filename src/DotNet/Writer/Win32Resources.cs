using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Win32 resources
	/// </summary>
	public sealed class Win32Resources : IChunk {
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
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetLength() {
			return 0;	//TODO:
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			//TODO:
		}
	}
}
