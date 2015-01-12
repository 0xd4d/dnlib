// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// ManifestResource flags. See CorHdr.h/CorManifestResourceFlags
	/// </summary>
	[Flags, DebuggerDisplay("{Extensions.ToString(this),nq}")]
	public enum ManifestResourceAttributes : uint {
		/// <summary/>
		VisibilityMask	= 0x0007,
		/// <summary>The Resource is exported from the Assembly.</summary>
		Public			= 0x0001,
		/// <summary>The Resource is private to the Assembly.</summary>
		Private			= 0x0002,
	}

	public static partial class Extensions {
		internal static string ToString(ManifestResourceAttributes flags) {
			var sb = new StringBuilder();

			switch ((flags & ManifestResourceAttributes.VisibilityMask)) {
			case ManifestResourceAttributes.Public: sb.Append("Public"); break;
			case ManifestResourceAttributes.Private: sb.Append("Private"); break;
			default: sb.Append("Visibility_UNKNOWN"); break;
			}

			return sb.ToString();
		}
	}
}
