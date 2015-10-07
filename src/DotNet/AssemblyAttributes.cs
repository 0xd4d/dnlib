// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.DotNet {
	/// <summary>
	/// Assembly flags from Assembly.Flags column.
	/// </summary>
	/// <remarks>See CorHdr.h/CorAssemblyFlags</remarks>
	[Flags]
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
}
