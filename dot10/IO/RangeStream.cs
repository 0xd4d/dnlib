using System;
using System.IO;

namespace dot10.IO {
	/// <summary>
	/// Encapsulates another stream, forcing all accesses to a certain range
	/// in the original stream.
	/// </summary>
	class RangeStream : Stream {
		Stream stream;
		long baseOffset;
		long length;

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
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public override long Length {
			get { return length; }
		}

		/// <inheritdoc/>
		public override long Position {
			get { return GetPosition(); }
			set { SetPosition(value); }
		}

		public RangeStream(Stream stream, long baseOffset, long length) {
			if (stream == null)
				throw new ArgumentException("stream");
			if (baseOffset < 0)
				throw new ArgumentOutOfRangeException("baseOffset");
			if (length < 0)
				throw new ArgumentOutOfRangeException("length");
			if (baseOffset + length < baseOffset)
				throw new ArgumentOutOfRangeException("length");
			if (baseOffset > stream.Length)
				throw new ArgumentOutOfRangeException("baseOffset");
			if (baseOffset + length > stream.Length)
				throw new ArgumentOutOfRangeException("length");
			this.stream = stream;
			this.baseOffset = baseOffset;
			this.length = length;
		}

		/// <inheritdoc/>
		public override int Read(byte[] buffer, int offset, int count) {
			int maxLen = (int)Math.Min((long)int.MaxValue, length - GetPosition());
			if (maxLen < 0)
				return 0;
			if (count > maxLen)
				count = maxLen;
			return stream.Read(buffer, offset, count);
		}

		/// <inheritdoc/>
		public override long Seek(long offset, SeekOrigin origin) {
			switch (origin) {
			case SeekOrigin.Begin: SetPosition(offset); break;
			case SeekOrigin.Current: SetPosition(GetPosition() + offset); break;
			case SeekOrigin.End: SetPosition(length - offset); break;
			default: throw new ArgumentException("Invalid origin");
			}
			return GetPosition();
		}

		long GetPosition() {
			long pos = stream.Position - baseOffset;
			if (pos < 0)
				SetPosition(pos = 0);
			if (pos > length)
				SetPosition(pos = length);
			return pos;
		}

		void SetPosition(long newPos) {
			if (newPos < 0)
				newPos = 0;
			if (newPos > length)
				newPos = length;
			stream.Position = baseOffset + newPos;
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
