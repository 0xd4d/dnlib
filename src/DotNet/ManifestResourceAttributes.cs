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
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// ManifestResource flags. See CorHdr.h/CorManifestResourceFlags
	/// </summary>
	[Flags, DebuggerDisplay("{Extensions.ToString(this),nq}")]
	public enum ManifestResourceAttributes : uint {
		/// <summary/>
		VisibilityMask	= 0x0007,
		/// <summary>The Resource is exported from the Assembly.</summary>
		Public			= 0x0001,
		/// <summary>The Resource is private to the Assembly.</summary>
		Private			= 0x0002,
	}

	public static partial class Extensions {
		internal static string ToString(ManifestResourceAttributes flags) {
			var sb = new StringBuilder();

			switch ((flags & ManifestResourceAttributes.VisibilityMask)) {
			case ManifestResourceAttributes.Public: sb.Append("Public"); break;
			case ManifestResourceAttributes.Private: sb.Append("Private"); break;
			default: sb.Append("Visibility_UNKNOWN"); break;
			}

			return sb.ToString();
		}
	}
}
