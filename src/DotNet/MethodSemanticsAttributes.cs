// dnlib: See LICENSE.txt for more info

﻿using System;

namespace dnlib.DotNet {
	/// <summary>
	/// Method semantics flags, see CorHdr.h/CorMethodSemanticsAttr
	/// </summary>
	[Flags]
	public enum MethodSemanticsAttributes : ushort {
		/// <summary>No bit is set</summary>
		None		= 0,
		/// <summary>Setter for property</summary>
		Setter		= 0x0001,
		/// <summary>Getter for property</summary>
		Getter		= 0x0002,
		/// <summary>other method for property or event</summary>
		Other		= 0x0004,
		/// <summary>AddOn method for event</summary>
		AddOn		= 0x0008,
		/// <summary>RemoveOn method for event</summary>
		RemoveOn	= 0x0010,
		/// <summary>Fire method for event</summary>
		Fire		= 0x0020,
	}
}
