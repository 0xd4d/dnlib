// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.DotNet {
	/// <summary>
	/// Method impl attributes, see CorHdr.h/CorMethodImpl
	/// </summary>
	[Flags]
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
}
