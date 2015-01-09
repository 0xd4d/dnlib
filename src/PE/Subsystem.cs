// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.PE {
	/// <summary>
	/// IMAGE_OPTIONAL_HEADER.Subsystem
	/// </summary>
	public enum Subsystem : ushort {
		/// <summary>Unknown subsystem.</summary>
		Unknown = 0,
		/// <summary>Image doesn't require a subsystem.</summary>
		Native = 1,
		/// <summary>Image runs in the Windows GUI subsystem.</summary>
		WindowsGui = 2,
		/// <summary>Image runs in the Windows character subsystem.</summary>
		WindowsCui = 3,
		/// <summary>image runs in the OS/2 character subsystem.</summary>
		Os2Cui = 5,
		/// <summary>image runs in the Posix character subsystem.</summary>
		PosixCui = 7,
		/// <summary>image is a native Win9x driver.</summary>
		NativeWindows = 8,
		/// <summary>Image runs in the Windows CE subsystem.</summary>
		WindowsCeGui = 9,
		/// <summary/>
		EfiApplication = 10,
		/// <summary/>
		EfiBootServiceDriver = 11,
		/// <summary/>
		EfiRuntimeDriver = 12,
		/// <summary/>
		EfiRom = 13,
		/// <summary/>
		Xbox = 14,
		/// <summary/>
		WindowsBootApplication = 16,
	}
}
