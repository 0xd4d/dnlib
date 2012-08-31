using System;
using System.IO;

namespace dot10.IO {
	/// <summary>
	/// Creates <see cref="UnmanagedMemoryStream"/>s to partially access an
	/// unmanaged memory range
	/// </summary>
	/// <seealso cref="MemoryStreamCreator"/>
	public class UnmanagedMemoryStreamCreator : IImageStreamCreator {
		/// <summary>
		/// Address of data
		/// </summary>
		protected IntPtr data;

		/// <summary>
		/// Length of data
		/// </summary>
		protected long dataLength;

		/// <summary>
		/// Name of file
		/// </summary>
		protected string theFileName;

		/// <summary>
		/// The file name
		/// </summary>
		public string FileName {
			get { return theFileName; }
			set { theFileName = value; }
		}

		/// <summary>
		/// Size of the data
		/// </summary>
		public long Length {
			get { return dataLength; }
			set { dataLength = value; }
		}

		/// <summary>
		/// Returns the base address of the data
		/// </summary>
		public IntPtr Address {
			get { return data; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		protected UnmanagedMemoryStreamCreator() {
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
		public unsafe IImageStream Create(FileOffset offset, long length) {
			if (offset.Value < 0)
				throw new ArgumentOutOfRangeException("offset");
			if (length < 0)
				throw new ArgumentOutOfRangeException("length");
			ulong offs = (ulong)offset.Value;
			if (offs + (ulong)length < offs || offs + (ulong)length > (ulong)dataLength)
				throw new ArgumentOutOfRangeException("length");
			return new UnmanagedMemoryImageStream(offset, (byte*)data.ToPointer() + offset.Value, length);
		}

		/// <inheritdoc/>
		public IImageStream CreateFull() {
			return new UnmanagedMemoryImageStream(FileOffset.Zero, data, dataLength);
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing">true if called by Dispose()</param>
		protected virtual void Dispose(bool disposing) {
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("mem: D:{0:X8} L:{1:X8} {2}", data, dataLength, theFileName);
		}
	}
}
