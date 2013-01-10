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

﻿using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Represents the #Strings stream
	/// </summary>
	public sealed class StringsStream : DotNetStream {
		/// <inheritdoc/>
		public StringsStream() {
		}

		/// <inheritdoc/>
		public StringsStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}

		/// <summary>
		/// Reads a <see cref="UTF8String"/>
		/// </summary>
		/// <param name="offset">Offset of string</param>
		/// <returns>A <see cref="UTF8String"/> instance or <c>null</c> if invalid offset</returns>
		public UTF8String Read(uint offset) {
			if (offset >= imageStream.Length)
				return null;
			imageStream.Position = offset;
			var data = imageStream.ReadBytesUntilByte(0);
			if (data == null)
				return null;
			return new UTF8String(data);
		}

		/// <summary>
		/// Reads a <see cref="UTF8String"/>. The empty string is returned if <paramref name="offset"/>
		/// is invalid.
		/// </summary>
		/// <param name="offset">Offset of string</param>
		/// <returns>A <see cref="UTF8String"/> instance</returns>
		public UTF8String ReadNoNull(uint offset) {
			return Read(offset) ?? UTF8String.Empty;
		}
	}
}
