namespace dot10.DotNet {
	/// <summary>
	/// Property attributes, see CorHdr.h/CorPropertyAttr
	/// </summary>
	public enum PropertyAttributes : ushort {
		/// <summary>property is special.  Name describes how.</summary>
		SpecialName			= 0x0200,
		/// <summary>Runtime(metadata internal APIs) should check name encoding.</summary>
		RTSpecialName		= 0x0400,
		/// <summary>Property has default</summary>
		HasDefault			= 0x1000,
	}
}
