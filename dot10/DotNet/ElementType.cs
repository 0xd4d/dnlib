namespace dot10.DotNet {
	/// <summary>
	/// See CorHdr.h/CorElementType
	/// </summary>
	public enum ElementType : byte {
		/// <summary></summary>
		End			= 0x00,
		/// <summary>System.Void</summary>
		Void		= 0x01,
		/// <summary>System.Boolean</summary>
		Boolean		= 0x02,
		/// <summary>System.Char</summary>
		Char		= 0x03,
		/// <summary>System.SByte</summary>
		I1			= 0x04,
		/// <summary>System.Byte</summary>
		U1 			= 0x05,
		/// <summary>System.Int16</summary>
		I2 			= 0x06,
		/// <summary>System.UInt16</summary>
		U2 			= 0x07,
		/// <summary>System.Int32</summary>
		I4 			= 0x08,
		/// <summary>System.UInt32</summary>
		U4			= 0x09,
		/// <summary>System.Int64</summary>
		I8			= 0x0A,
		/// <summary>System.UInt64</summary>
		U8			= 0x0B,
		/// <summary>System.Single</summary>
		R4			= 0x0C,
		/// <summary>System.Double</summary>
		R8			= 0x0D,
		/// <summary>System.String</summary>
		String		= 0x0E,
		/// <summary></summary>
		Ptr			= 0x0F,
		/// <summary></summary>
		ByRef		= 0x10,
		/// <summary></summary>
		ValueType	= 0x11,
		/// <summary></summary>
		Class		= 0x12,
		/// <summary>Type generic parameter</summary>
		Var			= 0x13,
		/// <summary></summary>
		Array		= 0x14,
		/// <summary></summary>
		GenericInst	= 0x15,
		/// <summary></summary>
		TypedByRef	= 0x16,
		/// <summary></summary>
		ValueArray	= 0x17,
		/// <summary>System.IntPtr</summary>
		I			= 0x18,
		/// <summary>System.UIntPtr</summary>
		U			= 0x19,
		/// <summary>native real</summary>
		R			= 0x1A,
		/// <summary></summary>
		FnPtr		= 0x1B,
		/// <summary>System.Object</summary>
		Object		= 0x1C,
		/// <summary></summary>
		SZArray		= 0x1D,
		/// <summary>Method generic parameter</summary>
		MVar		= 0x1E,
		/// <summary></summary>
		CModReqd	= 0x1F,
		/// <summary></summary>
		CModOpt		= 0x20,
		/// <summary></summary>
		Internal	= 0x21,
		/// <summary></summary>
		Module		= 0x3F,
		/// <summary></summary>
		Sentinel	= 0x41,
		/// <summary></summary>
		Pinned		= 0x45,
	}
}
