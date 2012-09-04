using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Generic parameter flags. See CorHdr.h/CorGenericParamAttr
	/// </summary>
	[Flags]
	public enum GenericParamAttributes : ushort {
		/// <summary></summary>
		VarianceMask			= 0x0003,
		/// <summary></summary>
		NonVariant				= 0x0000,
		/// <summary></summary>
		Covariant				= 0x0001,
		/// <summary></summary>
		Contravariant			= 0x0002,

		/// <summary></summary>
		SpecialConstraintMask	= 0x001C,
		/// <summary></summary>
		NoSpecialConstraint		= 0x0000,
		/// <summary>type argument must be a reference type</summary>
		ReferenceTypeConstraint = 0x0004,
		/// <summary>type argument must be a value type but not Nullable</summary>
		NotNullableValueTypeConstraint = 0x0008,
		/// <summary>type argument must have a public default constructor</summary>
		DefaultConstructorConstraint = 0x0010,
	}
}
