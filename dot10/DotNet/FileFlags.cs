using System;

namespace dot10.DotNet {
	/// <summary>
	/// File row flags. See CorHdr.h/CorFileFlags
	/// </summary>
	[Flags]
	public enum FileFlags : uint {
		/// <summary>This is not a resource file</summary>
		ContainsMetaData	= 0x0000,
		/// <summary>This is a resource file or other non-metadata-containing file</summary>
		ContainsNoMetaData	= 0x0001,
	}
}
