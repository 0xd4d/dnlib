// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// Assembly flags from Assembly.Flags column.
	/// </summary>
	/// <remarks>See CorHdr.h/CorAssemblyFlags</remarks>
	[Flags, DebuggerDisplay("{Extensions.ToString(this),nq}")]
	public enum AssemblyAttributes : uint {
		/// <summary>No flags set</summary>
		None						= 0,

		/// <summary>The assembly ref holds the full (unhashed) public key.</summary>
		PublicKey					= 1,

		/// <summary>Processor Architecture unspecified</summary>
		PA_None						= 0x0000,
		/// <summary>Processor Architecture: neutral (PE32)</summary>
		PA_MSIL						= 0x0010,
		/// <summary>Processor Architecture: x86 (PE32)</summary>
		PA_x86						= 0x0020,
		/// <summary>Processor Architecture: Itanium (PE32+)</summary>
		PA_IA64						= 0x0030,
		/// <summary>Processor Architecture: AMD X64 (PE32+)</summary>
		PA_AMD64					= 0x0040,
		/// <summary>Processor Architecture: ARM (PE32)</summary>
		PA_ARM						= 0x0050,
		/// <summary>applies to any platform but cannot run on any (e.g. reference assembly), should not have "specified" set</summary>
		PA_NoPlatform				= 0x0070,
		/// <summary>Propagate PA flags to AssemblyRef record</summary>
		PA_Specified				= 0x0080,
		/// <summary>Bits describing the processor architecture</summary>
		PA_Mask						= 0x0070,
		/// <summary>Bits describing the PA incl. Specified</summary>
		PA_FullMask					= 0x00F0,
		/// <summary>NOT A FLAG, shift count in PA flags &lt;--&gt; index conversion</summary>
		PA_Shift					= 0x0004,

		/// <summary>From "DebuggableAttribute".</summary>
		EnableJITcompileTracking	= 0x8000,
		/// <summary>From "DebuggableAttribute".</summary>
		DisableJITcompileOptimizer	= 0x4000,

		/// <summary>The assembly can be retargeted (at runtime) to an assembly from a different publisher.</summary>
		Retargetable				= 0x0100,

		/// <summary/>
		ContentType_Default			= 0x0000,
		/// <summary/>
		ContentType_WindowsRuntime	= 0x0200,
		/// <summary>Bits describing ContentType</summary>
		ContentType_Mask			= 0x0E00,
	}

	public static partial class Extensions {
		internal static string ToString(AssemblyAttributes flags) {
			if (flags == AssemblyAttributes.None)
				return "None";

			var sb = new StringBuilder();

			switch ((flags & AssemblyAttributes.PA_FullMask)) {
			case AssemblyAttributes.PA_None: sb.Append("PA_None"); break;
			case AssemblyAttributes.PA_MSIL: sb.Append("PA_MSIL"); break;
			case AssemblyAttributes.PA_x86: sb.Append("PA_x86"); break;
			case AssemblyAttributes.PA_IA64: sb.Append("PA_IA64"); break;
			case AssemblyAttributes.PA_AMD64: sb.Append("PA_AMD64"); break;
			case AssemblyAttributes.PA_ARM: sb.Append("PA_ARM"); break;
			case AssemblyAttributes.PA_NoPlatform: sb.Append("PA_NoPlatform"); break;
			case AssemblyAttributes.PA_Specified: sb.Append("PA_Specified"); break;
			default: sb.Append("PA_UNKNOWN"); break;
			}

			if ((flags & AssemblyAttributes.PublicKey) != 0)
				sb.Append(" | PublicKey");

			if ((flags & AssemblyAttributes.EnableJITcompileTracking) != 0)
				sb.Append(" | EnableJITcompileTracking");

			if ((flags & AssemblyAttributes.DisableJITcompileOptimizer) != 0)
				sb.Append(" | DisableJITcompileOptimizer");

			if ((flags & AssemblyAttributes.Retargetable) != 0)
				sb.Append(" | Retargetable");

			switch ((flags & AssemblyAttributes.ContentType_Mask)) {
			case AssemblyAttributes.ContentType_Default: sb.Append(" | ContentType_Default"); break;
			case AssemblyAttributes.ContentType_WindowsRuntime: sb.Append(" | ContentType_WindowsRuntime"); break;
			default: sb.Append(" | ContentType_UNKNOWN"); break;
			}

			return sb.ToString();
		}
	}
}
