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

namespace dnlib.PE {
	/// <summary>
	/// IMAGE_OPTIONAL_HEADER.DllCharacteristics
	/// </summary>
	[Flags]
	public enum DllCharacteristics {
		/// <summary>Image can handle a high entropy 64-bit virtual address space.</summary>
		HighEntrypyVA		= 0x0020,
		/// <summary>DLL can move.</summary>
		DynamicBase			= 0x0040,
		/// <summary>Code Integrity Image</summary>
		ForceIntegrity		= 0x0080,
		/// <summary>Image is NX compatible</summary>
		NxCompat			= 0x0100,
		/// <summary>Image understands isolation and doesn't want it</summary>
		NoIsolation			= 0x0200,
		/// <summary>Image does not use SEH.  No SE handler may reside in this image</summary>
		NoSeh				= 0x0400,
		/// <summary>Do not bind this image.</summary>
		NoBind				= 0x0800,
		/// <summary>Image should execute in an AppContainer</summary>
		AppContainer		= 0x1000,
		/// <summary>Driver uses WDM model</summary>
		WdmDriver			= 0x2000,
		/// <summary/>
		TerminalServerAware	= 0x8000,
	}
}
