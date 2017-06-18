// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet {
	/// <summary>
	/// Native types used by field marshals. See CorHdr.h/CorNativeType
	/// </summary>
	public enum NativeType : uint {
		/// <summary>Deprecated</summary>
		End					= 0x00,
		/// <summary>void</summary>
		Void				= 0x01,
		/// <summary>bool</summary>
		Boolean				= 0x02,
		/// <summary>int8</summary>
		I1					= 0x03,
		/// <summary>unsigned int8</summary>
		U1					= 0x04,
		/// <summary>int16</summary>
		I2					= 0x05,
		/// <summary>unsigned int16</summary>
		U2					= 0x06,
		/// <summary>int32</summary>
		I4					= 0x07,
		/// <summary>unsigned int32</summary>
		U4					= 0x08,
		/// <summary>int64</summary>
		I8					= 0x09,
		/// <summary>unsigned int64</summary>
		U8					= 0x0A,
		/// <summary>float32</summary>
		R4					= 0x0B,
		/// <summary>float64</summary>
		R8					= 0x0C,
		/// <summary>syschar</summary>
		SysChar				= 0x0D,
		/// <summary>variant</summary>
		Variant				= 0x0E,
		/// <summary>currency</summary>
		Currency			= 0x0F,
		/// <summary>ptr</summary>
		Ptr					= 0x10,
		/// <summary>decimal</summary>
		Decimal				= 0x11,
		/// <summary>date</summary>
		Date				= 0x12,
		/// <summary>bstr</summary>
		BStr				= 0x13,
		/// <summary>lpstr</summary>
		LPStr				= 0x14,
		/// <summary>lpwstr</summary>
		LPWStr				= 0x15,
		/// <summary>lptstr</summary>
		LPTStr				= 0x16,
		/// <summary>fixed sysstring</summary>
		FixedSysString		= 0x17,
		/// <summary>objectref</summary>
		ObjectRef			= 0x18,
		/// <summary>iunknown</summary>
		IUnknown			= 0x19,
		/// <summary>idispatch</summary>
		IDispatch			= 0x1A,
		/// <summary>struct</summary>
		Struct				= 0x1B,
		/// <summary>interface</summary>
		IntF				= 0x1C,
		/// <summary>safearray</summary>
		SafeArray			= 0x1D,
		/// <summary>fixed array</summary>
		FixedArray			= 0x1E,
		/// <summary>int</summary>
		Int					= 0x1F,
		/// <summary>uint</summary>
		UInt				= 0x20,
		/// <summary>nested struct</summary>
		NestedStruct		= 0x21,
		/// <summary>byvalstr</summary>
		ByValStr			= 0x22,
		/// <summary>ansi bstr</summary>
		ANSIBStr			= 0x23,
		/// <summary>tbstr</summary>
		TBStr				= 0x24,
		/// <summary>variant bool</summary>
		VariantBool			= 0x25,
		/// <summary>func</summary>
		Func				= 0x26,
		/// <summary>as any</summary>
		ASAny				= 0x28,
		/// <summary>array</summary>
		Array				= 0x2A,
		/// <summary>lpstruct</summary>
		LPStruct			= 0x2B,
		/// <summary>custom marshaler</summary>
		CustomMarshaler		= 0x2C,
		/// <summary>error</summary>
		Error				= 0x2D,
		/// <summary>iinspectable</summary>
		IInspectable		= 0x2E,
		/// <summary>hstring</summary>
		HString				= 0x2F,
		/// <summary>UTF-8 encoded string</summary>
		LPUTF8Str			= 0x30,
		/// <summary>first invalid element type</summary>
		Max					= 0x50,
		/// <summary>Value wasn't present in the blob</summary>
		NotInitialized		= 0xFFFFFFFE,
		/// <summary>Raw marshal blob type</summary>
		RawBlob				= 0xFFFFFFFF,
	}
}
