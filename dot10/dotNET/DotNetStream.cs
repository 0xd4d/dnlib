using System;
using System.IO;
using dot10.IO;

namespace dot10.dotNET {
	/// <summary>
	/// .NET metadata stream
	/// </summary>
	public class DotNetStream : IDisposable {
		/// <summary>
		/// Reader that can access the whole stream
		/// </summary>
		protected IImageStream imageStream;
		StreamHeader streamHeader;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="imageStream">Stream data</param>
		/// <param name="streamHeader">The stream header</param>
		public DotNetStream(IImageStream imageStream, StreamHeader streamHeader) {
			this.imageStream = imageStream;
			this.streamHeader = streamHeader;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("{0:X8} {1}", imageStream.Length, streamHeader.Name);
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
			if (disposing) {
				if (imageStream != null)
					imageStream.Dispose();
				imageStream = null;
				streamHeader = null;
			}
		}

		/// <summary>
		/// Checks whether an index is valid
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns>true if the index is valid</returns>
		public virtual bool IsValidIndex(uint index) {
			return IsValidOffset(index);
		}

		/// <summary>
		/// Check whether an offset is within the stream
		/// </summary>
		/// <param name="offset">Stream offset</param>
		/// <returns>true if the offset is valid</returns>
		public bool IsValidOffset(uint offset) {
			return offset == 0 || offset < imageStream.Length;
		}

		/// <summary>
		/// Check whether an offset is within the stream
		/// </summary>
		/// <param name="offset">Stream offset</param>
		/// <param name="size">Size of data</param>
		/// <returns>true if the offset is valid</returns>
		public bool IsValidOffset(uint offset, int size) {
			if (size == 0)
				return IsValidOffset(offset);
			return offset + (uint)size >= offset && offset + (uint)size <= imageStream.Length;
		}
	}
}
