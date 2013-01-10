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

﻿namespace dnlib.DotNet {
	/// <summary>
	/// See CorSerializationType/CorHdr.h
	/// </summary>
	enum SerializationType : byte {
		/// <summary/>
		Undefined	= 0,
		/// <summary>System.Boolean</summary>
		Boolean		= ElementType.Boolean,
		/// <summary>System.Char</summary>
		Char		= ElementType.Char,
		/// <summary>System.SByte</summary>
		I1			= ElementType.I1,
		/// <summary>System.Byte</summary>
		U1			= ElementType.U1,
		/// <summary>System.Int16</summary>
		I2			= ElementType.I2,
		/// <summary>System.UInt16</summary>
		U2			= ElementType.U2,
		/// <summary>System.Int32</summary>
		I4			= ElementType.I4,
		/// <summary>System.UInt32</summary>
		U4			= ElementType.U4,
		/// <summary>System.Int64</summary>
		I8			= ElementType.I8,
		/// <summary>System.UInt64</summary>
		U8			= ElementType.U8,
		/// <summary>System.Single</summary>
		R4			= ElementType.R4,
		/// <summary>System.Double</summary>
		R8			= ElementType.R8,
		/// <summary>System.String</summary>
		String		= ElementType.String,
		/// <summary>Single-dimension, zero lower bound array ([])</summary>
		SZArray		= ElementType.SZArray,
		/// <summary>System.Type</summary>
		Type		= 0x50,
		/// <summary>Boxed value type</summary>
		TaggedObject= 0x51,
		/// <summary>A field</summary>
		Field		= 0x53,
		/// <summary>A property</summary>
		Property	= 0x54,
		/// <summary>An enum</summary>
		Enum		= 0x55,
	}
}
