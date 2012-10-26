using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Relocations directory
	/// </summary>
	public sealed class RelocDirectory : IChunk {
		FileOffset offset;
		RVA rva;

		/// <summary>
		/// Gets/sets the <see cref="StartupStub"/>
		/// </summary>
		public StartupStub StartupStub { get; set; }

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
			return 12;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			uint rva = (uint)StartupStub.RelocRVA;
			writer.Write(rva & ~0xFFFU);
			writer.Write(12);
			writer.Write((ushort)(0x3000 | (rva & 0xFFF)));
			writer.Write((ushort)0);
		}
	}
}
