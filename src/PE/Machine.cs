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
	/// IMAGE_FILE_HEADER.Machine enum
	/// </summary>
	public enum Machine : ushort {
		/// <summary>Unknown machine</summary>
		Unknown		= 0,
		/// <summary>x86</summary>
		I386		= 0x014C,
		/// <summary>MIPS little-endian, 0x160 big-endian</summary>
		R3000		= 0x0162,
		/// <summary>MIPS little-endian</summary>
		R4000		= 0x0166,
		/// <summary>MIPS little-endian</summary>
		R10000		= 0x0168,
		/// <summary>MIPS little-endian WCE v2</summary>
		WCEMIPSV2	= 0x0169,
		/// <summary>Alpha_AXP</summary>
		ALPHA		= 0x0184,
		/// <summary>SH3 little-endian</summary>
		SH3			= 0x01A2,
		/// <summary></summary>
		SH3DSP		= 0x01A3,
		/// <summary>SH3E little-endian</summary>
		SH3E		= 0x01A4,
		/// <summary>SH4 little-endian</summary>
		SH4			= 0x01A6,
		/// <summary>SH5</summary>
		SH5			= 0x01A8,
		/// <summary>ARM Little-Endian</summary>
		ARM			= 0x01C0,
		/// <summary>ARM Thumb/Thumb-2 Little-Endian</summary>
		THUMB		= 0x01C2,
		/// <summary>ARM Thumb-2 Little-Endian</summary>
		ARMNT		= 0x01C4,
		/// <summary></summary>
		AM33		= 0x01D3,
		/// <summary>IBM PowerPC Little-Endian</summary>
		POWERPC		= 0x01F0,
		/// <summary></summary>
		POWERPCFP	= 0x01F1,
		/// <summary>IA-64</summary>
		IA64		= 0x0200,
		/// <summary></summary>
		MIPS16		= 0x0266,
		/// <summary></summary>
		ALPHA64		= 0x0284,
		/// <summary></summary>
		MIPSFPU		= 0x0366,
		/// <summary></summary>
		MIPSFPU16	= 0x0466,
		/// <summary>Infineon</summary>
		TRICORE		= 0x0520,
		/// <summary></summary>
		CEF			= 0x0CEF,
		/// <summary>EFI Byte Code</summary>
		EBC			= 0x0EBC,
		/// <summary>x64</summary>
		AMD64		= 0x8664,
		/// <summary>M32R little-endian</summary>
		M32R		= 0x9041,
		/// <summary></summary>
		CEE			= 0xC0EE,
	}
}
