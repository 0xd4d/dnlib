/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using System.Diagnostics;
using System.IO;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// .NET metadata stream
	/// </summary>
	[DebuggerDisplay("{imageStream.Length} {streamHeader.Name}")]
	public class DotNetStream : IFileSection, IDisposable {
		/// <summary>
		/// Reader that can access the whole stream
		/// </summary>
		protected IImageStream imageStream;

		/// <summary>
		/// <c>null</c> if it wasn't present in the file
		/// </summary>
		StreamHeader streamHeader;

		/// <inheritdoc/>
		public FileOffset StartOffset {
			get { return imageStream.FileOffset; }
		}

		/// <inheritdoc/>
		public FileOffset EndOffset {
			get { return imageStream.FileOffset + imageStream.Length; }
		}

		/// <summary>
		/// Returns the internal image stream
		/// </summary>
		public IImageStream ImageStream {
			get { return imageStream; }
		}

		/// <summary>
		/// Gets the stream header
		/// </summary>
		public StreamHeader StreamHeader {
			get { return streamHeader; }
		}

		/// <summary>
		/// Gets the name of the stream
		/// </summary>
		public string Name {
			get { return streamHeader.Name; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public DotNetStream() {
			this.imageStream = MemoryImageStream.CreateEmpty();
			this.streamHeader = null;
		}

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
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing"><c>true</c> if called by <see cref="Dispose()"/></param>
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
		/// <returns><c>true</c> if the index is valid</returns>
		public virtual bool IsValidIndex(uint index) {
			return IsValidOffset(index);
		}

		/// <summary>
		/// Check whether an offset is within the stream
		/// </summary>
		/// <param name="offset">Stream offset</param>
		/// <returns><c>true</c> if the offset is valid</returns>
		public bool IsValidOffset(uint offset) {
			return offset == 0 || offset < imageStream.Length;
		}

		/// <summary>
		/// Check whether an offset is within the stream
		/// </summary>
		/// <param name="offset">Stream offset</param>
		/// <param name="size">Size of data</param>
		/// <returns><c>true</c> if the offset is valid</returns>
		public bool IsValidOffset(uint offset, int size) {
			if (size == 0)
				return IsValidOffset(offset);
			return size > 0 && (long)offset + (uint)size <= imageStream.Length;
		}
	}
}
