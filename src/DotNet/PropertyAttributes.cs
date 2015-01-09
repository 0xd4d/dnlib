// dnlib: See LICENSE.txt for more info

﻿using System;

namespace dnlib.DotNet {
	/// <summary>
	/// Property attributes, see CorHdr.h/CorPropertyAttr
	/// </summary>
	[Flags]
	public enum PropertyAttributes : ushort {
		/// <summary>property is special.  Name describes how.</summary>
		SpecialName			= 0x0200,
		/// <summary>Runtime(metadata internal APIs) should check name encoding.</summary>
		RTSpecialName		= 0x0400,
		/// <summary>Property has default</summary>
		HasDefault			= 0x1000,
	}
}
