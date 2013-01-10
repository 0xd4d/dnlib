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

﻿namespace dnlib.PE {
	/// <summary>
	/// IMAGE_OPTIONAL_HEADER.Subsystem
	/// </summary>
	public enum Subsystem : ushort {
		/// <summary>Unknown subsystem.</summary>
		Unknown = 0,
		/// <summary>Image doesn't require a subsystem.</summary>
		Native = 1,
		/// <summary>Image runs in the Windows GUI subsystem.</summary>
		WindowsGui = 2,
		/// <summary>Image runs in the Windows character subsystem.</summary>
		WindowsCui = 3,
		/// <summary>image runs in the OS/2 character subsystem.</summary>
		Os2Cui = 5,
		/// <summary>image runs in the Posix character subsystem.</summary>
		PosixCui = 7,
		/// <summary>image is a native Win9x driver.</summary>
		NativeWindows = 8,
		/// <summary>Image runs in the Windows CE subsystem.</summary>
		WindowsCeGui = 9,
		/// <summary/>
		EfiApplication = 10,
		/// <summary/>
		EfiBootServiceDriver = 11,
		/// <summary/>
		EfiRuntimeDriver = 12,
		/// <summary/>
		EfiRom = 13,
		/// <summary/>
		Xbox = 14,
		/// <summary/>
		WindowsBootApplication = 16,
	}
}
