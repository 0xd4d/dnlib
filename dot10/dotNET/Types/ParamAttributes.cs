namespace dot10.dotNET.Types {
	/// <summary>
	/// Parameter flags. See CorHdr.h/CorParamAttr
	/// </summary>
	public enum ParamAttributes : ushort {
		/// <summary>Param is [In]</summary>
		In					= 0x0001,
		/// <summary>Param is [out]</summary>
		Out					= 0x0002,
		/// <summary>Param is optional</summary>
		Optional			= 0x0010,

		/// <summary>Reserved flags for Runtime use only.</summary>
		ReservedMask		= 0xf000,
		/// <summary>Param has default value.</summary>
		HasDefault			= 0x1000,
		/// <summary>Param has FieldMarshal.</summary>
		HasFieldMarshal		= 0x2000,
	}
}
