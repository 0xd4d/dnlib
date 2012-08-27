using System;
using System.IO;

namespace dot10.IO {
	/// <summary>
	/// Creates <see cref="UnmanagedMemoryStream"/>s to partially access an
	/// unmanaged memory range
	/// </summary>
	/// <seealso cref="MemoryStreamCreator"/>
	/// <seealso cref="FileStreamCreator"/>
	public class UnmanagedMemoryStreamCreator : IStreamCreator {
		IntPtr data;
		long dataLength;

		/// <summary>
		/// Size of the data
		/// </summary>
		public long Length {
			get { return dataLength; }
			set { dataLength = value; }
		}

		/// <summary>
		/// Constructor for 0 bytes of data
		/// </summary>
		/// <param name="data">Pointer to the data</param>
		public UnmanagedMemoryStreamCreator(IntPtr data)
			: this(data, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Pointer to the data</param>
		/// <param name="dataLength">Length of data</param>
		/// <exception cref="ArgumentOutOfRangeException">If one of the args is invalid</exception>
		public UnmanagedMemoryStreamCreator(IntPtr data, long dataLength) {
			if (dataLength < 0)
				throw new ArgumentOutOfRangeException("dataLength");
			this.data = data;
			this.dataLength = dataLength;
		}

		/// <inheritdoc/>
		public unsafe Stream Create(FileOffset offset, long length) {
			if (offset.Value < 0)
				throw new ArgumentOutOfRangeException("offset");
			if (length < 0)
				throw new ArgumentOutOfRangeException("length");
			ulong offs = (ulong)offset.Value;
			if (offs + (ulong)length < offs || offs + (ulong)length > (ulong)dataLength)
				throw new ArgumentOutOfRangeException("length");
			return new UnmanagedMemoryStream((byte*)data.ToPointer() + offset.Value, length);
		}

		/// <inheritdoc/>
		public unsafe Stream CreateFull() {
			return new UnmanagedMemoryStream((byte*)data.ToPointer(), dataLength);
		}
	}
}
