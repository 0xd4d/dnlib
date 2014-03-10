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

namespace dnlib.DotNet.MD {
	/// <summary>
	/// See COMIMAGE_FLAGS_XXX in CorHdr.h in the Windows SDK
	/// </summary>
	[Flags]
	public enum ComImageFlags : uint {
		/// <summary>
		/// See COMIMAGE_FLAGS_ILONLY in the Windows SDK
		/// </summary>
		ILOnly = 1,

		/// <summary>
		/// See COMIMAGE_FLAGS_32BITREQUIRED in the Windows SDK
		/// </summary>
		_32BitRequired = 2,

		/// <summary>
		/// Set if a native header exists (COMIMAGE_FLAGS_IL_LIBRARY)
		/// </summary>
		ILLibrary = 4,

		/// <summary>
		/// See COMIMAGE_FLAGS_STRONGNAMESIGNED in the Windows SDK
		/// </summary>
		StrongNameSigned = 8,

		/// <summary>
		/// See COMIMAGE_FLAGS_NATIVE_ENTRYPOINT in the Windows SDK
		/// </summary>
		NativeEntryPoint = 0x10,

		/// <summary>
		/// See COMIMAGE_FLAGS_TRACKDEBUGDATA in the Windows SDK
		/// </summary>
		TrackDebugData = 0x10000,

		/// <summary>
		/// See COMIMAGE_FLAGS_32BITPREFERRED in the Windows SDK
		/// </summary>
		_32BitPreferred = 0x20000,
	}
}
