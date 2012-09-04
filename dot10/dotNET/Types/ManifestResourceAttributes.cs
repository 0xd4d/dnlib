using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// ManifestResource flags. See CorHdr.h/CorManifestResourceFlags
	/// </summary>
	[Flags]
	public enum ManifestResourceAttributes : uint {
		/// <summary></summary>
		VisibilityMask	= 0x0007,
		/// <summary>The Resource is exported from the Assembly.</summary>
		Public			= 0x0001,
		/// <summary>The Resource is private to the Assembly.</summary>
		Private			= 0x0002,
	}
}
