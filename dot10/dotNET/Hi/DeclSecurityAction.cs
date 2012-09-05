namespace dot10.dotNET.Hi {
	/// <summary>
	/// Security action. See CorHdr.h/CorDeclSecurity
	/// </summary>
	public enum DeclSecurityAction : short {
		/// <summary>Mask allows growth of enum.</summary>
		ActionMask			= 0x001F,
		/// <summary></summary>
		ActionNil			= 0x0000,
		/// <summary></summary>
		Request				= 0x0001,
		/// <summary></summary>
		Demand				= 0x0002,
		/// <summary></summary>
		Assert				= 0x0003,
		/// <summary></summary>
		Deny				= 0x0004,
		/// <summary></summary>
		PermitOnly			= 0x0005,
		/// <summary></summary>
		LinktimeCheck		= 0x0006,
		/// <summary></summary>
		InheritanceCheck	= 0x0007,
		/// <summary></summary>
		RequestMinimum		= 0x0008,
		/// <summary></summary>
		RequestOptional		= 0x0009,
		/// <summary></summary>
		RequestRefuse		= 0x000A,
		/// <summary>Persisted grant set at prejit time</summary>
		PrejitGrant			= 0x000B,
		/// <summary>Persisted denied set at prejit time</summary>
		PrejitDenied		= 0x000C,
		/// <summary></summary>
		NonCasDemand		= 0x000D,
		/// <summary></summary>
		NonCasLinkDemand	= 0x000E,
		/// <summary></summary>
		NonCasInheritance	= 0x000F,
		/// <summary>Maximum legal value</summary>
		MaximumValue		= 0x000F,
	}
}
