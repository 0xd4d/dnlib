using System;
using System.IO;

namespace dot10.IO {
	/// <summary>
	/// Creates <see cref="MemoryStream"/>s to partially access a byte[]
	/// </summary>
	/// <seealso cref="UnmanagedMemoryStreamCreator"/>
	public class MemoryStreamCreator : IStreamCreator {
		byte[] data;
		int dataOffset;
		int dataLength;
		string filename;

		/// <summary>
		/// The file name
		/// </summary>
		public string Filename {
			get { return filename; }
			set { filename = value; }
		}

		/// <inheritdoc/>
		public long Length {
			get { return dataLength; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The data</param>
		public MemoryStreamCreator(byte[] data)
			: this(data, 0, data.Length) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The data</param>
		/// <param name="offset">Start offset in <paramref name="data"/></param>
		/// <param name="length">Length of data starting from <paramref name="offset"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If one of the args is invalid</exception>
		public MemoryStreamCreator(byte[] data, int offset, int length) {
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");
			if (length < 0 || offset + length < offset)
				throw new ArgumentOutOfRangeException("length");
			if (offset + length > data.Length)
				throw new ArgumentOutOfRangeException("length");
			this.data = data;
			this.dataOffset = offset;
			this.dataLength = length;
		}

		/// <inheritdoc/>
		public Stream Create(FileOffset offset, long length) {
			if (offset.Value < 0 || offset.Value > int.MaxValue)
				throw new ArgumentOutOfRangeException("offset");
			if (length < 0 || length > int.MaxValue)
				throw new ArgumentOutOfRangeException("length");
			int offs = (int)offset.Value;
			int len = (int)length;
			if (offs + len < offs)
				throw new ArgumentOutOfRangeException("length");
			if (offs > dataLength)
				throw new ArgumentOutOfRangeException("offset");
			if (offs + len > dataLength)
				throw new ArgumentOutOfRangeException("length");
			return new MemoryStream(data, dataOffset + offs, len, false);
		}

		/// <inheritdoc/>
		public Stream CreateFull() {
			return new MemoryStream(data, dataOffset, dataLength, false);
		}

		/// <inheritdoc/>
		public void Dispose() {
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("byte[]: O:{0:X8} L:{1:X8} {2}", dataOffset, dataLength, filename);
		}
	}
}
