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

﻿using System.Diagnostics;
using System.IO;
using dnlib.IO;

namespace dnlib.IO {
	/// <summary>
	/// Base class for classes needing to implement IFileSection
	/// </summary>
	[DebuggerDisplay("O:{startOffset} L:{size} {GetType().Name}")]
	public class FileSection : IFileSection {
		/// <summary>
		/// The start file offset of this section
		/// </summary>
		protected FileOffset startOffset;

		/// <summary>
		/// Size of the section
		/// </summary>
		protected uint size;

		/// <inheritdoc/>
		public FileOffset StartOffset {
			get { return startOffset; }
		}

		/// <inheritdoc/>
		public FileOffset EndOffset {
			get { return startOffset + size; }
		}

		/// <summary>
		/// Set <see cref="startOffset"/> to <paramref name="reader"/>'s current position
		/// </summary>
		/// <param name="reader">The reader</param>
		protected void SetStartOffset(IImageStream reader) {
			startOffset = (FileOffset)reader.Position;
		}

		/// <summary>
		/// Set <see cref="size"/> according to <paramref name="reader"/>'s current position
		/// </summary>
		/// <param name="reader">The reader</param>
		protected void SetEndoffset(IImageStream reader) {
			size = (uint)(reader.Position - startOffset);
			startOffset = reader.FileOffset + (long)startOffset;
		}
	}
}
