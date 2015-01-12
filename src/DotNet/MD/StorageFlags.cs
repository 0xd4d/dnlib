// dnlib: See LICENSE.txt for more info

﻿using System;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Storage flags found in the MD header
	/// </summary>
	[Flags]
	public enum StorageFlags : byte {
		/// <summary>
		/// Normal flags
		/// </summary>
		Normal = 0,

		/// <summary>
		/// More data after the header but before the streams.
		/// </summary>
		/// <remarks>The CLR will fail to load the file if this flag (or any other bits) is set.</remarks>
		ExtraData = 1,
	}
}
