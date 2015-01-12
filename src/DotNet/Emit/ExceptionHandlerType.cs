// dnlib: See LICENSE.txt for more info

﻿using System;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Type of exception handler. See CorHdr.h/CorExceptionFlag
	/// </summary>
	[Flags]
	public enum ExceptionHandlerType {
		/// <summary/>
		Catch		= 0x0000,
		/// <summary/>
		Filter		= 0x0001,
		/// <summary/>
		Finally		= 0x0002,
		/// <summary/>
		Fault		= 0x0004,
		/// <summary/>
		Duplicated	= 0x0008,
	}
}
