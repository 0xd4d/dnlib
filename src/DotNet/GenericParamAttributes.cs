// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// Generic parameter flags. See CorHdr.h/CorGenericParamAttr
	/// </summary>
	[Flags, DebuggerDisplay("{Extensions.ToString(this),nq}")]
	public enum GenericParamAttributes : ushort {
		/// <summary/>
		VarianceMask			= 0x0003,
		/// <summary/>
		NonVariant				= 0x0000,
		/// <summary/>
		Covariant				= 0x0001,
		/// <summary/>
		Contravariant			= 0x0002,

		/// <summary/>
		SpecialConstraintMask	= 0x001C,
		/// <summary/>
		NoSpecialConstraint		= 0x0000,
		/// <summary>type argument must be a reference type</summary>
		ReferenceTypeConstraint = 0x0004,
		/// <summary>type argument must be a value type but not Nullable</summary>
		NotNullableValueTypeConstraint = 0x0008,
		/// <summary>type argument must have a public default constructor</summary>
		DefaultConstructorConstraint = 0x0010,
	}

	public static partial class Extensions {
		internal static string ToString(GenericParamAttributes flags) {
			var sb = new StringBuilder();

			switch ((flags & GenericParamAttributes.VarianceMask)) {
			case GenericParamAttributes.NonVariant: sb.Append("NonVariant"); break;
			case GenericParamAttributes.Covariant: sb.Append("Covariant"); break;
			case GenericParamAttributes.Contravariant: sb.Append("Contravariant"); break;
			default: sb.Append("Variance_UNKNOWN"); break;
			}

			switch ((flags & GenericParamAttributes.SpecialConstraintMask)) {
			case GenericParamAttributes.NoSpecialConstraint: sb.Append(" | NoSpecialConstraint"); break;
			case GenericParamAttributes.ReferenceTypeConstraint: sb.Append(" | ReferenceTypeConstraint"); break;
			case GenericParamAttributes.NotNullableValueTypeConstraint: sb.Append(" | NotNullableValueTypeConstraint"); break;
			case GenericParamAttributes.DefaultConstructorConstraint: sb.Append(" | DefaultConstructorConstraint"); break;
			default: sb.Append(" | SpecialConstraint_UNKNOWN"); break;
			}

			return sb.ToString();
		}
	}
}
