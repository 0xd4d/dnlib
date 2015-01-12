// dnlib: See LICENSE.txt for more info

﻿using System;

namespace dnlib.DotNet {
	/// <summary>
	/// File row flags. See CorHdr.h/CorFileFlags
	/// </summary>
	[Flags]
	public enum FileAttributes : uint {
		/// <summary>This is not a resource file</summary>
		ContainsMetaData	= 0x0000,
		/// <summary>This is a resource file or other non-metadata-containing file</summary>
		ContainsNoMetaData	= 0x0001,
	}
}
