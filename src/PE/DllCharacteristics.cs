// dnlib: See LICENSE.txt for more info

﻿using System;

namespace dnlib.PE {
	/// <summary>
	/// IMAGE_OPTIONAL_HEADER.DllCharacteristics
	/// </summary>
	[Flags]
	public enum DllCharacteristics : ushort {
		/// <summary/>
		Reserved1			= 0x0001,
		/// <summary/>
		Reserved2			= 0x0002,
		/// <summary/>
		Reserved3			= 0x0004,
		/// <summary/>
		Reserved4			= 0x0008,
		/// <summary/>
		Reserved5			= 0x0010,
		/// <summary>Image can handle a high entropy 64-bit virtual address space.</summary>
		HighEntropyVA		= 0x0020,
		/// <summary>DLL can move.</summary>
		DynamicBase			= 0x0040,
		/// <summary>Code Integrity Image</summary>
		ForceIntegrity		= 0x0080,
		/// <summary>Image is NX compatible</summary>
		NxCompat			= 0x0100,
		/// <summary>Image understands isolation and doesn't want it</summary>
		NoIsolation			= 0x0200,
		/// <summary>Image does not use SEH.  No SE handler may reside in this image</summary>
		NoSeh				= 0x0400,
		/// <summary>Do not bind this image.</summary>
		NoBind				= 0x0800,
		/// <summary>Image should execute in an AppContainer</summary>
		AppContainer		= 0x1000,
		/// <summary>Driver uses WDM model</summary>
		WdmDriver			= 0x2000,
		/// <summary>Image supports Control Flow Guard.</summary>
		GuardCf				= 0x4000,
		/// <summary/>
		TerminalServerAware	= 0x8000,
	}
}
