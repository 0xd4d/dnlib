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
	/// Security action. See CorHdr.h/CorDeclSecurity
	/// </summary>
	public enum DeclSecurityAction : short {
		/// <summary>Mask allows growth of enum.</summary>
		ActionMask			= 0x001F,
		/// <summary/>
		ActionNil			= 0x0000,
		/// <summary/>
		Request				= 0x0001,
		/// <summary/>
		Demand				= 0x0002,
		/// <summary/>
		Assert				= 0x0003,
		/// <summary/>
		Deny				= 0x0004,
		/// <summary/>
		PermitOnly			= 0x0005,
		/// <summary/>
		LinktimeCheck		= 0x0006,
		/// <summary/>
		InheritanceCheck	= 0x0007,
		/// <summary/>
		RequestMinimum		= 0x0008,
		/// <summary/>
		RequestOptional		= 0x0009,
		/// <summary/>
		RequestRefuse		= 0x000A,
		/// <summary>Persisted grant set at prejit time</summary>
		PrejitGrant			= 0x000B,
		/// <summary>Persisted denied set at prejit time</summary>
		PrejitDenied		= 0x000C,
		/// <summary/>
		NonCasDemand		= 0x000D,
		/// <summary/>
		NonCasLinkDemand	= 0x000E,
		/// <summary/>
		NonCasInheritance	= 0x000F,
		/// <summary>Maximum legal value</summary>
		MaximumValue		= 0x000F,
	}
}
