// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// P/Invoke attributes, see CorHdr.h/CorPinvokeMap
	/// </summary>
	[Flags, DebuggerDisplay("{Extensions.ToString(this),nq}")]
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

	public static partial class Extensions {
		internal static string ToString(PInvokeAttributes flags) {
			var sb = new StringBuilder();

			switch ((flags & PInvokeAttributes.CharSetMask)) {
			case PInvokeAttributes.CharSetNotSpec: sb.Append("CharSetNotSpec"); break;
			case PInvokeAttributes.CharSetAnsi: sb.Append("CharSetAnsi"); break;
			case PInvokeAttributes.CharSetUnicode: sb.Append("CharSetUnicode"); break;
			case PInvokeAttributes.CharSetAuto: sb.Append("CharSetAuto"); break;
			}

			if ((flags & PInvokeAttributes.NoMangle) != 0)
				sb.Append(" | NoMangle");

			switch ((flags & PInvokeAttributes.BestFitMask)) {
			case PInvokeAttributes.BestFitUseAssem: sb.Append(" | BestFitUseAssem"); break;
			case PInvokeAttributes.BestFitEnabled: sb.Append(" | BestFitEnabled"); break;
			case PInvokeAttributes.BestFitDisabled: sb.Append(" | BestFitDisabled"); break;
			default: sb.Append(" | BestFit_UNKNOWN"); break;
			}

			switch ((flags & PInvokeAttributes.ThrowOnUnmappableCharMask)) {
			case PInvokeAttributes.ThrowOnUnmappableCharUseAssem: sb.Append(" | ThrowOnUnmappableCharUseAssem"); break;
			case PInvokeAttributes.ThrowOnUnmappableCharEnabled: sb.Append(" | ThrowOnUnmappableCharEnabled"); break;
			case PInvokeAttributes.ThrowOnUnmappableCharDisabled: sb.Append(" | ThrowOnUnmappableCharDisabled"); break;
			default: sb.Append(" | ThrowOnUnmappableChar_UNKNOWN"); break;
			}

			if ((flags & PInvokeAttributes.SupportsLastError) != 0)
				sb.Append(" | SupportsLastError");

			switch ((flags & PInvokeAttributes.CallConvMask)) {
			case PInvokeAttributes.CallConvWinapi: sb.Append(" | CallConvWinapi"); break;
			case PInvokeAttributes.CallConvCdecl: sb.Append(" | CallConvCdecl"); break;
			case PInvokeAttributes.CallConvStdcall: sb.Append(" | CallConvStdcall"); break;
			case PInvokeAttributes.CallConvThiscall: sb.Append(" | CallConvThiscall"); break;
			case PInvokeAttributes.CallConvFastcall: sb.Append(" | CallConvFastcall"); break;
			default: sb.Append(" | CallConv_UNKNOWN"); break;
			}

			return sb.ToString();
		}
	}
}
