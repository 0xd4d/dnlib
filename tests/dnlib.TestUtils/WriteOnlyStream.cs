// dnlib: See LICENSE.txt for more info

using System;
using System.IO;

namespace dnlib.TestUtils {
	/// <summary>
	/// A stream that can only be written to, like a pipe: it can't be read and has no position
	/// </summary>
	public sealed class WriteOnlyStream : Stream {
		readonly MemoryStream written = new MemoryStream();

		/// <summary>
		/// Gets everything that was written to the stream
		/// </summary>
		public byte[] GetWrittenData() => written.ToArray();

		public override bool CanRead => false;
		public override bool CanSeek => false;
		public override bool CanWrite => true;
		public override long Length => throw new NotSupportedException();
		public override long Position {
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}
		public override void Flush() { }
		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => written.Write(buffer, offset, count);
	}
}
