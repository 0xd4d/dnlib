using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// A <see cref="IBinaryReader"/> chunk
	/// </summary>
	public class BinaryReaderChunk : IChunk {
		FileOffset offset;
		RVA rva;
		IBinaryReader data;

		/// <summary>
		/// Gets the data
		/// </summary>
		public IBinaryReader Data {
			get { return data; }
		}

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
		/// <param name="data">The data</param>
		public BinaryReaderChunk(IBinaryReader data) {
			this.data = data;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetLength() {
			return (uint)data.Length;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			data.Position = 0;
			data.WriteTo(writer);
		}
	}
}
