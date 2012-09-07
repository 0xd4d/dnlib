namespace dot10.dotNET.Hi {
	/// <summary>
	/// See CorHdr.h/CorCallingConvention
	/// </summary>
	public enum CallingConvention : byte {
		/// <summary></summary>
		Default			= 0x0,
		/// <summary></summary>
		C				= 0x1,
		/// <summary></summary>
		StdCall			= 0x2,
		/// <summary></summary>
		ThisCall		= 0x3,
		/// <summary></summary>
		FastCall		= 0x4,
		/// <summary></summary>
		VarArg			= 0x5,
		/// <summary></summary>
		Field			= 0x6,
		/// <summary></summary>
		LocalSig		= 0x7,
		/// <summary></summary>
		Property		= 0x8,
		/// <summary></summary>
		Unmanaged		= 0x9,
		/// <summary>generic method instantiation</summary>
		GenericInst		= 0xA,
		/// <summary>used ONLY for 64bit vararg PInvoke calls</summary>
		NativeVarArg	= 0xB,

		/// <summary>Calling convention is bottom 4 bits</summary>
		Mask			= 0x0F,

		/// <summary>Generic method</summary>
		Generic			= 0x10,
		/// <summary>Method needs a 'this' parameter</summary>
		HasThis			= 0x20,
		/// <summary>'this' parameter is the first arg if set (else it's hidden)</summary>
		ExplicitThis	= 0x40,
		/// <summary>Used internally by the CLR</summary>
		ReservedByCLR	= 0x80,
	}
}
