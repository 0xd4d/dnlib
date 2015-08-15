// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.DotNet {
	/// <summary>
	/// P/Invoke attributes, see CorHdr.h/CorPinvokeMap
	/// </summary>
	[Flags]
	public enum PInvokeAttributes : ushort {
		/// <summary>Pinvoke is to use the member name as specified.</summary>
		NoMangle			= 0x0001,

		/// <summary>Use this mask to retrieve the CharSet information.</summary>
		CharSetMask			= 0x0006,
		/// <summary/>
		CharSetNotSpec		= 0x0000,
		/// <summary/>
		CharSetAnsi			= 0x0002,
		/// <summary/>
		CharSetUnicode		= 0x0004,
		/// <summary/>
		CharSetAuto			= 0x0006,

		/// <summary/>
		BestFitUseAssem		= 0x0000,
		/// <summary/>
		BestFitEnabled		= 0x0010,
		/// <summary/>
		BestFitDisabled		= 0x0020,
		/// <summary/>
		BestFitMask			= 0x0030,

		/// <summary/>
		ThrowOnUnmappableCharUseAssem	= 0x0000,
		/// <summary/>
		ThrowOnUnmappableCharEnabled	= 0x1000,
		/// <summary/>
		ThrowOnUnmappableCharDisabled	= 0x2000,
		/// <summary/>
		ThrowOnUnmappableCharMask		= 0x3000,

		/// <summary>Information about target function. Not relevant for fields.</summary>
		SupportsLastError	= 0x0040,

		/// <summary/>
		CallConvMask		= 0x0700,
		/// <summary>Pinvoke will use native callconv appropriate to target windows platform.</summary>
		CallConvWinapi		= 0x0100,
		/// <summary/>
		CallConvCdecl		= 0x0200,
		/// <summary/>
		CallConvStdcall		= 0x0300,
		/// <summary/>
		CallConvStdCall		= CallConvStdcall,
		/// <summary>In M9, pinvoke will raise exception.</summary>
		CallConvThiscall	= 0x0400,
		/// <summary/>
		CallConvFastcall	= 0x0500,
	}
}
