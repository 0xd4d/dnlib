// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.DotNet {
	/// <summary>
	/// ManifestResource flags. See CorHdr.h/CorManifestResourceFlags
	/// </summary>
	[Flags]
	public enum ManifestResourceAttributes : uint {
		/// <summary/>
		VisibilityMask	= 0x0007,
		/// <summary>The Resource is exported from the Assembly.</summary>
		Public			= 0x0001,
		/// <summary>The Resource is private to the Assembly.</summary>
		Private			= 0x0002,
	}
}
