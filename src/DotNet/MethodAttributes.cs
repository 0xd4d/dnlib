// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// Method attributes, see CorHdr.h/CorMethodAttr
	/// </summary>
	[Flags, DebuggerDisplay("{Extensions.ToString(this),nq}")]
	public enum MethodAttributes : ushort {
		/// <summary>member access mask - Use this mask to retrieve accessibility information.</summary>
		MemberAccessMask	= 0x0007,
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
		/// <summary>Method may not be overridden.</summary>
		Final				= 0x0020,
		/// <summary>Method virtual.</summary>
		Virtual				= 0x0040,
		/// <summary>Method hides by name+sig, else just by name.</summary>
		HideBySig			= 0x0080,

		/// <summary>vtable layout mask - Use this mask to retrieve vtable attributes.</summary>
		VtableLayoutMask	= 0x0100,
		/// <summary>The default.</summary>
		ReuseSlot			= 0x0000,
		/// <summary>Method always gets a new slot in the vtable.</summary>
		NewSlot				= 0x0100,

		/// <summary>Overridability is the same as the visibility.</summary>
		CheckAccessOnOverride = 0x0200,
		/// <summary>Method does not provide an implementation.</summary>
		Abstract			= 0x0400,
		/// <summary>Method is special.  Name describes how.</summary>
		SpecialName			= 0x0800,

		/// <summary>Implementation is forwarded through pinvoke.</summary>
		PinvokeImpl			= 0x2000,
		/// <summary>Implementation is forwarded through pinvoke.</summary>
		PInvokeImpl			= PinvokeImpl,
		/// <summary>Managed method exported via thunk to unmanaged code.</summary>
		UnmanagedExport		= 0x0008,

		/// <summary>Runtime should check name encoding.</summary>
		RTSpecialName		= 0x1000,
		/// <summary>Method has security associate with it.</summary>
		HasSecurity			= 0x4000,
		/// <summary>Method calls another method containing security code.</summary>
		RequireSecObject	= 0x8000,
	}

	public static partial class Extensions {
		internal static string ToString(MethodAttributes flags) {
			var sb = new StringBuilder();

			switch ((flags & MethodAttributes.MemberAccessMask)) {
			case MethodAttributes.PrivateScope: sb.Append("PrivateScope"); break;
			case MethodAttributes.Private: sb.Append("Private"); break;
			case MethodAttributes.FamANDAssem: sb.Append("FamANDAssem"); break;
			case MethodAttributes.Assembly: sb.Append("Assembly"); break;
			case MethodAttributes.Family: sb.Append("Family"); break;
			case MethodAttributes.FamORAssem: sb.Append("FamORAssem"); break;
			case MethodAttributes.Public: sb.Append("Public"); break;
			default: sb.Append("FieldAccess_UNKNOWN"); break;
			}

			if ((flags & MethodAttributes.Static) != 0)
				sb.Append(" | Static");

			if ((flags & MethodAttributes.Final) != 0)
				sb.Append(" | Final");

			if ((flags & MethodAttributes.Virtual) != 0)
				sb.Append(" | Virtual");

			if ((flags & MethodAttributes.HideBySig) != 0)
				sb.Append(" | HideBySig");

			if ((flags & MethodAttributes.NewSlot) != 0)
				sb.Append(" | NewSlot");
			else
				sb.Append(" | ReuseSlot");

			if ((flags & MethodAttributes.CheckAccessOnOverride) != 0)
				sb.Append(" | CheckAccessOnOverride");

			if ((flags & MethodAttributes.Abstract) != 0)
				sb.Append(" | Abstract");

			if ((flags & MethodAttributes.SpecialName) != 0)
				sb.Append(" | SpecialName");

			if ((flags & MethodAttributes.PinvokeImpl) != 0)
				sb.Append(" | PinvokeImpl");

			if ((flags & MethodAttributes.UnmanagedExport) != 0)
				sb.Append(" | UnmanagedExport");

			if ((flags & MethodAttributes.RTSpecialName) != 0)
				sb.Append(" | RTSpecialName");

			if ((flags & MethodAttributes.HasSecurity) != 0)
				sb.Append(" | HasSecurity");

			if ((flags & MethodAttributes.RequireSecObject) != 0)
				sb.Append(" | RequireSecObject");

			return sb.ToString();
		}
	}
}
