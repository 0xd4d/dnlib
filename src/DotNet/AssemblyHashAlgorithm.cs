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

﻿namespace dnlib.DotNet {
	/// <summary>
	/// Any ALG_CLASS_HASH type in WinCrypt.h can be used by Microsoft's CLI implementation
	/// </summary>
	public enum AssemblyHashAlgorithm : uint {
		/// <summary/>
		None				= 0,
		/// <summary/>
		MD2					= 0x8001,
		/// <summary/>
		MD4					= 0x8002,
		/// <summary>This is a reserved value in the CLI</summary>
		MD5					= 0x8003,
		/// <summary>The only algorithm supported by the CLI</summary>
		SHA1				= 0x8004,
		/// <summary/>
		MAC					= 0x8005,
		/// <summary/>
		SSL3_SHAMD5			= 0x8008,
		/// <summary/>
		HMAC				= 0x8009,
		/// <summary/>
		TLS1PRF				= 0x800A,
		/// <summary/>
		HASH_REPLACE_OWF	= 0x800B,
		/// <summary/>
		SHA_256				= 0x800C,
		/// <summary/>
		SHA_384				= 0x800D,
		/// <summary/>
		SHA_512				= 0x800E,
	}

	public static partial class Extensions {
		internal static string GetName(this AssemblyHashAlgorithm hashAlg) {
			switch (hashAlg) {
			case AssemblyHashAlgorithm.MD2:		return null;
			case AssemblyHashAlgorithm.MD4:		return null;
			case AssemblyHashAlgorithm.MD5:		return "MD5";
			case AssemblyHashAlgorithm.SHA1:	return "SHA1";
			case AssemblyHashAlgorithm.MAC:		return null;
			case AssemblyHashAlgorithm.SSL3_SHAMD5: return null;
			case AssemblyHashAlgorithm.HMAC:	return null;
			case AssemblyHashAlgorithm.TLS1PRF:	return null;
			case AssemblyHashAlgorithm.HASH_REPLACE_OWF: return null;
			case AssemblyHashAlgorithm.SHA_256:	return "SHA256";
			case AssemblyHashAlgorithm.SHA_384:	return "SHA384";
			case AssemblyHashAlgorithm.SHA_512:	return "SHA512";
			default: return null;
			}
		}
	}
}
