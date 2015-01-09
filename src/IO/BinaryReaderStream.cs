// dnlib: See LICENSE.txt for more info

﻿using System;
using System.IO;

namespace dnlib.IO {
	/// <summary>
	/// A <see cref="Stream"/> class that can be used when you have a <see cref="IBinaryReader"/>
	/// but must use a <see cref="Stream"/>
	/// </summary>
	sealed class BinaryReaderStream : Stream {
		IBinaryReader reader;
		readonly bool ownsReader;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">Reader. This instance does <c>NOT</c> own this reader.</param>
		public BinaryReaderStream(IBinaryReader reader)
			: this(reader, false) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <param name="ownsReader"><c>true</c> if this instance owns <paramref name="reader"/></param>
		public BinaryReaderStream(IBinaryReader reader, bool ownsReader) {
			this.reader = reader;
			this.ownsReader = ownsReader;
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
			case SeekOrigin.Begin:	Position = offset; break;
			case SeekOrigin.Current:Position += offset; break;
			case SeekOrigin.End:	Position = Length + offset; break;
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

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				var r = reader;
				if (ownsReader && r != null)
					r.Dispose();
				reader = null;
			}
			base.Dispose(disposing);
		}
	}
}
