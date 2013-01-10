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

namespace dnlib.DotNet.MD {
	/// <summary>
	/// MDStream flags
	/// </summary>
	[Flags]
	public enum MDStreamFlags : byte {
		/// <summary>#Strings stream is big and requires 4 byte offsets</summary>
		BigStrings = 1,
		/// <summary>#GUID stream is big and requires 4 byte offsets</summary>
		BigGUID = 2,
		/// <summary>#Blob stream is big and requires 4 byte offsets</summary>
		BigBlob = 4,
		/// <summary/>
		Padding = 8,
		/// <summary/>
		DeltaOnly = 0x20,
		/// <summary>Extra data follows the row counts</summary>
		ExtraData = 0x40,
		/// <summary>Set if certain tables can contain deleted rows. The name column (if present) is set to "_Deleted"</summary>
		HasDelete = 0x80,
	}
}
