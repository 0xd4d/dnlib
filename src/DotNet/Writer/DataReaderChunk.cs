// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// A <see cref="DataReader"/> chunk
	/// </summary>
	public class DataReaderChunk : IChunk {
		FileOffset offset;
		RVA rva;
		DataReader data;
		readonly uint virtualSize;

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The data</param>
		public DataReaderChunk(DataReader data)
			: this(ref data) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The data</param>
		/// <param name="virtualSize">Virtual size of <paramref name="data"/></param>
		public DataReaderChunk(DataReader data, uint virtualSize)
			: this(ref data, virtualSize) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The data</param>
		internal DataReaderChunk(ref DataReader data)
			: this(ref data, (uint)data.Length) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The data</param>
		/// <param name="virtualSize">Virtual size of <paramref name="data"/></param>
		internal DataReaderChunk(ref DataReader data, uint virtualSize) {
			this.data = data;
			this.virtualSize = virtualSize;
		}

		/// <summary>
		/// Gets the data reader
		/// </summary>
		public DataReader GetReader() => data;

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() => (uint)data.Length;

		/// <inheritdoc/>
		public uint GetVirtualSize() => virtualSize;

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			data.Position = 0;
			data.CopyTo(writer);
		}
	}
}
