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
	/// IMAGE_FILE_HEADER.Characteristics flags
	/// </summary>
	[Flags]
	public enum Characteristics : ushort {
		/// <summary>Relocation info stripped from file.</summary>
		RelocsStripped		= 0x0001,
		/// <summary>File is executable  (i.e. no unresolved externel references).</summary>
		ExecutableImage		= 0x0002,
		/// <summary>Line nunbers stripped from file.</summary>
		LineNumsStripped	= 0x0004,
		/// <summary>Local symbols stripped from file.</summary>
		LocalsSymsStripped	= 0x0008,
		/// <summary>Agressively trim working set</summary>
		AggressiveWsTrim	= 0x0010,
		/// <summary>App can handle >2gb addresses</summary>
		LargeAddressAware	= 0x0020,
		/// <summary>Bytes of machine word are reversed.</summary>
		BytesReversedLo		= 0x0080,
		/// <summary>32 bit word machine.</summary>
		_32BitMachine		= 0x0100,
		/// <summary>Debugging info stripped from file in .DBG file</summary>
		DebugStripped		= 0x0200,
		/// <summary>If Image is on removable media, copy and run from the swap file.</summary>
		RemovableRunFromSwap= 0x0400,
		/// <summary>If Image is on Net, copy and run from the swap file.</summary>
		NetRunFromSwap		= 0x0800,
		/// <summary>System File.</summary>
		System				= 0x1000,
		/// <summary>File is a DLL.</summary>
		Dll					= 0x2000,
		/// <summary>File should only be run on a UP machine</summary>
		UpSystemOnly		= 0x4000,
		/// <summary>Bytes of machine word are reversed.</summary>
		BytesReversedHi		= 0x8000,
	}
}
