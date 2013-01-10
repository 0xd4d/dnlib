/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

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
