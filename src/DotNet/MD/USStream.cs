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
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Represents the #US stream
	/// </summary>
	public sealed class USStream : DotNetStream {
		/// <inheritdoc/>
		public USStream() {
		}

		/// <inheritdoc/>
		public USStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}

		/// <summary>
		/// Reads a unicode string
		/// </summary>
		/// <param name="offset">Offset of unicode string</param>
		/// <returns>A string or <c>null</c> if <paramref name="offset"/> is invalid</returns>
		public string Read(uint offset) {
			if (offset == 0)
				return string.Empty;
			if (!IsValidOffset(offset))
				return null;
			imageStream.Position = offset;
			uint length;
			if (!imageStream.ReadCompressedUInt32(out length))
				return null;
			if (imageStream.Position + length < length || imageStream.Position + length > imageStream.Length)
				return null;
			try {
				return imageStream.ReadString((int)(length / 2));
			}
			catch (OutOfMemoryException) {
				throw;
			}
			catch {
				// It's possible that an exception is thrown when converting a char* to
				// a string. If so, return an empty string.
				return string.Empty;
			}
		}

		/// <summary>
		/// Reads data just like <see cref="Read"/>, but returns an empty string if
		/// offset is invalid
		/// </summary>
		/// <param name="offset">Offset of unicode string</param>
		/// <returns>The string</returns>
		public string ReadNoNull(uint offset) {
			return Read(offset) ?? string.Empty;
		}
	}
}
