// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// Method impl attributes, see CorHdr.h/CorMethodImpl
	/// </summary>
	[Flags, DebuggerDisplay("{Extensions.ToString(this),nq}")]
	public enum MethodImplAttributes : ushort {
		/// <summary>Flags about code type.</summary>
		CodeTypeMask		= 0x0003,
		/// <summary>Method impl is IL.</summary>
		IL					= 0x0000,
		/// <summary>Method impl is native.</summary>
		Native				= 0x0001,
		/// <summary>Method impl is OPTIL</summary>
		OPTIL				= 0x0002,
		/// <summary>Method impl is provided by the runtime.</summary>
		Runtime				= 0x0003,

		/// <summary>Flags specifying whether the code is managed or unmanaged.</summary>
		ManagedMask			= 0x0004,
		/// <summary>Method impl is unmanaged, otherwise managed.</summary>
		Unmanaged			= 0x0004,
		/// <summary>Method impl is managed.</summary>
		Managed				= 0x0000,

		/// <summary>Indicates method is defined; used primarily in merge scenarios.</summary>
		ForwardRef			= 0x0010,
		/// <summary>Indicates method sig is not to be mangled to do HRESULT conversion.</summary>
		PreserveSig			= 0x0080,

		/// <summary>Reserved for internal use.</summary>
		InternalCall		= 0x1000,

		/// <summary>Method is single threaded through the body.</summary>
		Synchronized		= 0x0020,
		/// <summary>Method may not be inlined.</summary>
		NoInlining			= 0x0008,
		/// <summary>Method should be inlined if possible.</summary>
		AggressiveInlining	= 0x0100,
		/// <summary>Method may not be optimized.</summary>
		NoOptimization		= 0x0040,
	}

	public static partial class Extensions {
		internal static string ToString(MethodImplAttributes flags) {
			var sb = new StringBuilder();

			switch ((flags & MethodImplAttributes.CodeTypeMask)) {
			case MethodImplAttributes.IL: sb.Append("IL"); break;
			case MethodImplAttributes.Native: sb.Append("Native"); break;
			case MethodImplAttributes.OPTIL: sb.Append("OPTIL"); break;
			case MethodImplAttributes.Runtime: sb.Append("Runtime"); break;
			}

			if ((flags & MethodImplAttributes.Unmanaged) != 0)
				sb.Append(" | Unmanaged");
			else
				sb.Append(" | Managed");

			if ((flags & MethodImplAttributes.ForwardRef) != 0)
				sb.Append(" | ForwardRef");

			if ((flags & MethodImplAttributes.PreserveSig) != 0)
				sb.Append(" | PreserveSig");

			if ((flags & MethodImplAttributes.InternalCall) != 0)
				sb.Append(" | InternalCall");

			if ((flags & MethodImplAttributes.Synchronized) != 0)
				sb.Append(" | Synchronized");

			if ((flags & MethodImplAttributes.NoInlining) != 0)
				sb.Append(" | NoInlining");

			if ((flags & MethodImplAttributes.AggressiveInlining) != 0)
				sb.Append(" | AggressiveInlining");

			if ((flags & MethodImplAttributes.NoOptimization) != 0)
				sb.Append(" | NoOptimization");

			return sb.ToString();
		}
	}
}
