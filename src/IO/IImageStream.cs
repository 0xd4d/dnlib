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
using System.IO;

namespace dnlib.IO {
	/// <summary>
	/// Interface to access part of some data
	/// </summary>
	public interface IImageStream : IBinaryReader {
		/// <summary>
		/// Returns the file offset of the stream
		/// </summary>
		FileOffset FileOffset { get; }

		/// <summary>
		/// Creates a sub stream that can access parts of this stream
		/// </summary>
		/// <param name="offset">File offset relative to the start of this stream</param>
		/// <param name="length">Length</param>
		/// <returns>A new stream</returns>
		IImageStream Create(FileOffset offset, long length);
	}

	static partial class IOExtensions {
		/// <summary>
		/// Creates a stream that can access all data starting from <paramref name="offset"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="offset">Offset relative to the beginning of the stream</param>
		/// <returns>A new stream</returns>
		public static IImageStream Create(this IImageStream self, FileOffset offset) {
			return self.Create(offset, long.MaxValue);
		}

		/// <summary>
		/// Clones this <see cref="IImageStream"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <returns>A new <see cref="IImageStream"/> instance</returns>
		public static IImageStream Clone(this IImageStream self) {
			return self.Create(0, long.MaxValue);
		}
	}
}
