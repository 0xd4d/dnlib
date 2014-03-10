/*
    Copyright (C) 2012-2014 de4dot@gmail.com

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

namespace dnlib.IO {
	/// <summary>
	/// Creates a new stream that accesses part of some data
	/// </summary>
	public interface IImageStreamCreator : IDisposable {
		/// <summary>
		/// The file name or <c>null</c> if data is not from a file
		/// </summary>
		string FileName { get; }

		/// <summary>
		/// Returns the total length of the original data
		/// </summary>
		long Length { get; }

		/// <summary>
		/// Creates a stream that can access only part of the data
		/// </summary>
		/// <param name="offset">Offset within the original data</param>
		/// <param name="length">Length of section within the original data</param>
		/// <returns>A new stream</returns>
		IImageStream Create(FileOffset offset, long length);

		/// <summary>
		/// Creates a stream that can access all data
		/// </summary>
		/// <returns>A new stream</returns>
		IImageStream CreateFull();
	}

	static partial class IOExtensions {
		/// <summary>
		/// Creates a stream that can access all data starting from <paramref name="offset"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="offset">Offset within the original data</param>
		/// <returns>A new stream</returns>
		public static IImageStream Create(this IImageStreamCreator self, FileOffset offset) {
			return self.Create(offset, long.MaxValue);
		}
	}
}
