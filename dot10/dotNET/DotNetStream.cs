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
		/// <param name="disposing">true if called by <see cref="Dispose()"/></param>
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
			return size > 0 && (long)offset + (uint)size <= imageStream.Length;
		}

		/// <summary>
		/// Reads a compressed <see cref="uint"/> from the current position in <see cref="imageStream"/>
		/// </summary>
		/// <remarks>Max value it can return is <c>0x1FFFFFFF</c></remarks>
		/// <param name="val">Decompressed value</param>
		/// <returns><c>true</c> if successful, <c>false</c> on failure</returns>
		protected bool ReadCompressedUInt32(out uint val) {
			var pos = imageStream.Position;
			var len = imageStream.Length;
			if (pos >= len) {
				val = 0;
				return false;
			}

			byte b = imageStream.ReadByte();
			if ((b & 0x80) == 0) {
				val = b;
				return true;
			}

			if ((b & 0xC0) == 0x80) {
				if (pos + 1 < pos || pos + 1 >= len) {
					val = 0;
					return false;
				}
				val = (uint)(((b & 0x3F) << 8) | imageStream.ReadByte());
				return true;
			}

			if ((b & 0xE0) == 0xC0) {
				if (pos + 3 < pos || pos + 3 >= len) {
					val = 0;
					return false;
				}
				val = (uint)(((b & 0x1F) << 24) | (imageStream.ReadByte() << 16) |
						(imageStream.ReadByte() << 8) | imageStream.ReadByte());
				return true;
			}

			val = 0;
			return false;
		}
	}
}
