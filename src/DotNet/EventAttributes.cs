using System;

namespace dot10.DotNet {
	/// <summary>
	/// Event attributes, see CorHdr.h/CorEventAttr
	/// </summary>
	[Flags]
	public enum EventAttributes : ushort {
		/// <summary>event is special.  Name describes how.</summary>
		SpecialName			= 0x0200,
		/// <summary>Runtime(metadata internal APIs) should check name encoding.</summary>
		RTSpecialName		= 0x0400,
	}
}
