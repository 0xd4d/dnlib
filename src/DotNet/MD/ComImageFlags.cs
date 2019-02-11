// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// See COMIMAGE_FLAGS_XXX in CorHdr.h in the Windows SDK
	/// </summary>
	[Flags]
	public enum ComImageFlags : uint {
		/// <summary>
		/// See COMIMAGE_FLAGS_ILONLY in the Windows SDK
		/// </summary>
		ILOnly = 1,

		/// <summary>
		/// See COMIMAGE_FLAGS_32BITREQUIRED in the Windows SDK
		/// </summary>
		Bit32Required = 2,

		/// <summary>
		/// Set if a native header exists (COMIMAGE_FLAGS_IL_LIBRARY)
		/// </summary>
		ILLibrary = 4,

		/// <summary>
		/// See COMIMAGE_FLAGS_STRONGNAMESIGNED in the Windows SDK
		/// </summary>
		StrongNameSigned = 8,

		/// <summary>
		/// See COMIMAGE_FLAGS_NATIVE_ENTRYPOINT in the Windows SDK
		/// </summary>
		NativeEntryPoint = 0x10,

		/// <summary>
		/// See COMIMAGE_FLAGS_TRACKDEBUGDATA in the Windows SDK
		/// </summary>
		TrackDebugData = 0x10000,

		/// <summary>
		/// See COMIMAGE_FLAGS_32BITPREFERRED in the Windows SDK
		/// </summary>
		Bit32Preferred = 0x20000,
	}
}
