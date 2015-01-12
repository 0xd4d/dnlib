// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet {
	/// <summary>
	/// Security action. See CorHdr.h/CorDeclSecurity
	/// </summary>
	public enum SecurityAction : short {
		/// <summary>Mask allows growth of enum.</summary>
		ActionMask			= 0x001F,
		/// <summary/>
		ActionNil			= 0x0000,
		/// <summary/>
		Request				= 0x0001,
		/// <summary/>
		Demand				= 0x0002,
		/// <summary/>
		Assert				= 0x0003,
		/// <summary/>
		Deny				= 0x0004,
		/// <summary/>
		PermitOnly			= 0x0005,
		/// <summary/>
		LinktimeCheck		= 0x0006,
		/// <summary/>
		LinkDemand			= LinktimeCheck,
		/// <summary/>
		InheritanceCheck	= 0x0007,
		/// <summary/>
		InheritDemand		= InheritanceCheck,
		/// <summary/>
		RequestMinimum		= 0x0008,
		/// <summary/>
		RequestOptional		= 0x0009,
		/// <summary/>
		RequestRefuse		= 0x000A,
		/// <summary>Persisted grant set at prejit time</summary>
		PrejitGrant			= 0x000B,
		/// <summary>Persisted grant set at prejit time</summary>
		PreJitGrant			= PrejitGrant,
		/// <summary>Persisted denied set at prejit time</summary>
		PrejitDenied		= 0x000C,
		/// <summary>Persisted denied set at prejit time</summary>
		PreJitDeny			= PrejitDenied,
		/// <summary/>
		NonCasDemand		= 0x000D,
		/// <summary/>
		NonCasLinkDemand	= 0x000E,
		/// <summary/>
		NonCasInheritance	= 0x000F,
		/// <summary>Maximum legal value</summary>
		MaximumValue		= 0x000F,
	}
}
