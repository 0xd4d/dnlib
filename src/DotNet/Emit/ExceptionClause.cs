using System;

namespace dot10.DotNet.Emit {
	/// <summary>
	/// Type of exception handler. See CorHdr.h/CorExceptionFlag
	/// </summary>
	[Flags]
	public enum ExceptionClause {
		/// <summary></summary>
		Catch		= 0x0000,
		/// <summary></summary>
		Filter		= 0x0001,
		/// <summary></summary>
		Finally		= 0x0002,
		/// <summary></summary>
		Fault		= 0x0004,
		/// <summary></summary>
		Duplicated	= 0x0008,
	}
}
