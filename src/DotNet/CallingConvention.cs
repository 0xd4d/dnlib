// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.DotNet {
	/// <summary>
	/// See CorHdr.h/CorCallingConvention
	/// </summary>
	[Flags]
	public enum CallingConvention : byte {
		/// <summary>The managed calling convention</summary>
		Default			= 0x0,
		/// <summary/>
		C				= 0x1,
		/// <summary/>
		StdCall			= 0x2,
		/// <summary/>
		ThisCall		= 0x3,
		/// <summary/>
		FastCall		= 0x4,
		/// <summary/>
		VarArg			= 0x5,
		/// <summary/>
		Field			= 0x6,
		/// <summary/>
		LocalSig		= 0x7,
		/// <summary/>
		Property		= 0x8,
		/// <summary/>
		Unmanaged		= 0x9,
		/// <summary>generic method instantiation</summary>
		GenericInst		= 0xA,
		/// <summary>used ONLY for 64bit vararg PInvoke calls</summary>
		NativeVarArg	= 0xB,

		/// <summary>Calling convention is bottom 4 bits</summary>
		Mask			= 0x0F,

		/// <summary>Generic method</summary>
		Generic			= 0x10,
		/// <summary>Method needs a 'this' parameter</summary>
		HasThis			= 0x20,
		/// <summary>'this' parameter is the first arg if set (else it's hidden)</summary>
		ExplicitThis	= 0x40,
		/// <summary>Used internally by the CLR</summary>
		ReservedByCLR	= 0x80,
	}
}
