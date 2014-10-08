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

namespace dnlib.DotNet {
	/// <summary>
	/// Variant type (<c>VT_XXX</c> in the Windows SDK)
	/// </summary>
	public enum VariantType : uint {
		/// <summary/>
		Empty			= 0,
		/// <summary/>
		None			= 0,
		/// <summary/>
		Null			= 1,
		/// <summary/>
		I2				= 2,
		/// <summary/>
		I4				= 3,
		/// <summary/>
		R4				= 4,
		/// <summary/>
		R8				= 5,
		/// <summary/>
		CY				= 6,
		/// <summary/>
		Date			= 7,
		/// <summary/>
		BStr			= 8,
		/// <summary/>
		Dispatch		= 9,
		/// <summary/>
		Error			= 10,
		/// <summary/>
		Bool			= 11,
		/// <summary/>
		Variant			= 12,
		/// <summary/>
		Unknown			= 13,
		/// <summary/>
		Decimal			= 14,
		/// <summary/>
		I1				= 16,
		/// <summary/>
		UI1				= 17,
		/// <summary/>
		UI2				= 18,
		/// <summary/>
		UI4				= 19,
		/// <summary/>
		I8				= 20,
		/// <summary/>
		UI8				= 21,
		/// <summary/>
		Int				= 22,
		/// <summary/>
		UInt			= 23,
		/// <summary/>
		Void			= 24,
		/// <summary/>
		HResult			= 25,
		/// <summary/>
		Ptr				= 26,
		/// <summary/>
		SafeArray		= 27,
		/// <summary/>
		CArray			= 28,
		/// <summary/>
		UserDefined		= 29,
		/// <summary/>
		LPStr			= 30,
		/// <summary/>
		LPWStr			= 31,
		/// <summary/>
		Record			= 36,
		/// <summary/>
		IntPtr			= 37,
		/// <summary/>
		UIntPtr			= 38,
		/// <summary/>
		FileTime		= 64,
		/// <summary/>
		Blob			= 65,
		/// <summary/>
		Stream			= 66,
		/// <summary/>
		Storage			= 67,
		/// <summary/>
		StreamedObject	= 68,
		/// <summary/>
		StoredObject	= 69,
		/// <summary/>
		BlobObject		= 70,
		/// <summary/>
		CF				= 71,
		/// <summary/>
		CLSID			= 72,
		/// <summary/>
		VersionedStream	= 73,
		/// <summary/>
		BStrBlob		= 0x0FFF,
		/// <summary/>
		Vector			= 0x1000,
		/// <summary/>
		Array			= 0x2000,
		/// <summary/>
		ByRef			= 0x4000,
		/// <summary/>
		Reserved		= 0x8000,
		/// <summary/>
		Illegal			= 0xFFFF,
		/// <summary/>
		IllegalMasked	= 0x0FFF,
		/// <summary/>
		TypeMask		= 0x0FFF,
		/// <summary>This wasn't present in the blob</summary>
		NotInitialized	= 0xFFFFFFFF,
	}
}
