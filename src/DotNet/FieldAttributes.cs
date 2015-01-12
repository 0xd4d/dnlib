// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// Field flags, see CorHdr.h/CorFieldAttr
	/// </summary>
	[Flags, DebuggerDisplay("{Extensions.ToString(this),nq}")]
	public enum FieldAttributes : ushort {
		/// <summary>member access mask - Use this mask to retrieve accessibility information.</summary>
		FieldAccessMask		= 0x0007,
		/// <summary>Member not referenceable.</summary>
		PrivateScope		= 0x0000,
		/// <summary>Member not referenceable.</summary>
		CompilerControlled	= PrivateScope,
		/// <summary>Accessible only by the parent type.</summary>
		Private				= 0x0001,
		/// <summary>Accessible by sub-types only in this Assembly.</summary>
		FamANDAssem			= 0x0002,
		/// <summary>Accessibly by anyone in the Assembly.</summary>
		Assembly			= 0x0003,
		/// <summary>Accessible only by type and sub-types.</summary>
		Family				= 0x0004,
		/// <summary>Accessibly by sub-types anywhere, plus anyone in assembly.</summary>
		FamORAssem			= 0x0005,
		/// <summary>Accessibly by anyone who has visibility to this scope.</summary>
		Public				= 0x0006,

		/// <summary>Defined on type, else per instance.</summary>
		Static				= 0x0010,
		/// <summary>Field may only be initialized, not written to after init.</summary>
		InitOnly			= 0x0020,
		/// <summary>Value is compile time constant.</summary>
		Literal				= 0x0040,
		/// <summary>Field does not have to be serialized when type is remoted.</summary>
		NotSerialized		= 0x0080,

		/// <summary>field is special.  Name describes how.</summary>
		SpecialName			= 0x0200,

		/// <summary>Implementation is forwarded through pinvoke.</summary>
		PinvokeImpl			= 0x2000,

		/// <summary>Runtime(metadata internal APIs) should check name encoding.</summary>
		RTSpecialName		= 0x0400,
		/// <summary>Field has marshalling information.</summary>
		HasFieldMarshal		= 0x1000,
		/// <summary>Field has default.</summary>
		HasDefault			= 0x8000,
		/// <summary>Field has RVA.</summary>
		HasFieldRVA			= 0x0100,
	}

	public static partial class Extensions {
		internal static string ToString(FieldAttributes flags) {
			var sb = new StringBuilder();

			switch ((flags & FieldAttributes.FieldAccessMask)) {
			case FieldAttributes.PrivateScope: sb.Append("PrivateScope"); break;
			case FieldAttributes.Private: sb.Append("Private"); break;
			case FieldAttributes.FamANDAssem: sb.Append("FamANDAssem"); break;
			case FieldAttributes.Assembly: sb.Append("Assembly"); break;
			case FieldAttributes.Family: sb.Append("Family"); break;
			case FieldAttributes.FamORAssem: sb.Append("FamORAssem"); break;
			case FieldAttributes.Public: sb.Append("Public"); break;
			default: sb.Append("FieldAccess_UNKNOWN"); break;
			}

			if ((flags & FieldAttributes.Static) != 0)
				sb.Append(" | Static");

			if ((flags & FieldAttributes.InitOnly) != 0)
				sb.Append(" | InitOnly");

			if ((flags & FieldAttributes.Literal) != 0)
				sb.Append(" | Literal");

			if ((flags & FieldAttributes.NotSerialized) != 0)
				sb.Append(" | NotSerialized");

			if ((flags & FieldAttributes.SpecialName) != 0)
				sb.Append(" | SpecialName");

			if ((flags & FieldAttributes.PinvokeImpl) != 0)
				sb.Append(" | PinvokeImpl");

			if ((flags & FieldAttributes.RTSpecialName) != 0)
				sb.Append(" | RTSpecialName");

			if ((flags & FieldAttributes.HasFieldMarshal) != 0)
				sb.Append(" | HasFieldMarshal");

			if ((flags & FieldAttributes.HasDefault) != 0)
				sb.Append(" | HasDefault");

			if ((flags & FieldAttributes.HasFieldRVA) != 0)
				sb.Append(" | HasFieldRVA");

			return sb.ToString();
		}
	}
}
