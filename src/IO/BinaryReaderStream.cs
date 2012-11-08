using System;
using System.IO;

namespace dot10.IO {
	/// <summary>
	/// A <see cref="Stream"/> class that can be used when you have a <see cref="IBinaryReader"/>
	/// but must use a <see cref="Stream"/>
	/// </summary>
	sealed class BinaryReaderStream : Stream {
		readonly IBinaryReader reader;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">Reader</param>
		public BinaryReaderStream(IBinaryReader reader) {
			this.reader = reader;
		}

		/// <inheritdoc/>
		public override bool CanRead {
			get { return true; }
		}

		/// <inheritdoc/>
		public override bool CanSeek {
			get { return true; }
		}

		/// <inheritdoc/>
		public override bool CanWrite {
			get { return false; }
		}

		/// <inheritdoc/>
		public override void Flush() {
		}

		/// <inheritdoc/>
		public override long Length {
			get { return reader.Length; }
		}

		/// <inheritdoc/>
		public override long Position {
			get { return reader.Position; }
			set { reader.Position = value; }
		}

		/// <inheritdoc/>
		public override int Read(byte[] buffer, int offset, int count) {
			return reader.Read(buffer, offset, count);
		}

		/// <inheritdoc/>
		public override int ReadByte() {
			try {
				return reader.ReadByte();
			}
			catch (IOException) {
				return -1;
			}
		}

		/// <inheritdoc/>
		public override long Seek(long offset, SeekOrigin origin) {
			switch (origin) {
			case SeekOrigin.Begin: Position = offset; break;
			case SeekOrigin.Current: Position += offset; break;
			case SeekOrigin.End: Position = Length - offset; break;
			}
			return Position;
		}

		/// <inheritdoc/>
		public override void SetLength(long value) {
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public override void Write(byte[] buffer, int offset, int count) {
			throw new NotImplementedException();
		}
	}
}
