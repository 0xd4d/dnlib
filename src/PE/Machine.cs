// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.PE {
	/// <summary>
	/// IMAGE_FILE_HEADER.Machine enum
	/// </summary>
	public enum Machine : ushort {
		/// <summary>Unknown machine</summary>
		Unknown		= 0,
		/// <summary>x86</summary>
		I386		= 0x014C,
		/// <summary>MIPS little-endian, 0x160 big-endian</summary>
		R3000		= 0x0162,
		/// <summary>MIPS little-endian</summary>
		R4000		= 0x0166,
		/// <summary>MIPS little-endian</summary>
		R10000		= 0x0168,
		/// <summary>MIPS little-endian WCE v2</summary>
		WCEMIPSV2	= 0x0169,
		/// <summary>Alpha_AXP</summary>
		ALPHA		= 0x0184,
		/// <summary>SH3 little-endian</summary>
		SH3			= 0x01A2,
		/// <summary></summary>
		SH3DSP		= 0x01A3,
		/// <summary>SH3E little-endian</summary>
		SH3E		= 0x01A4,
		/// <summary>SH4 little-endian</summary>
		SH4			= 0x01A6,
		/// <summary>SH5</summary>
		SH5			= 0x01A8,
		/// <summary>ARM Little-Endian</summary>
		ARM			= 0x01C0,
		/// <summary>ARM Thumb/Thumb-2 Little-Endian</summary>
		THUMB		= 0x01C2,
		/// <summary>ARM Thumb-2 Little-Endian</summary>
		ARMNT		= 0x01C4,
		/// <summary></summary>
		AM33		= 0x01D3,
		/// <summary>IBM PowerPC Little-Endian</summary>
		POWERPC		= 0x01F0,
		/// <summary></summary>
		POWERPCFP	= 0x01F1,
		/// <summary>IA-64</summary>
		IA64		= 0x0200,
		/// <summary></summary>
		MIPS16		= 0x0266,
		/// <summary></summary>
		ALPHA64		= 0x0284,
		/// <summary></summary>
		MIPSFPU		= 0x0366,
		/// <summary></summary>
		MIPSFPU16	= 0x0466,
		/// <summary>Infineon</summary>
		TRICORE		= 0x0520,
		/// <summary></summary>
		CEF			= 0x0CEF,
		/// <summary>EFI Byte Code</summary>
		EBC			= 0x0EBC,
		/// <summary>x64</summary>
		AMD64		= 0x8664,
		/// <summary>M32R little-endian</summary>
		M32R		= 0x9041,
		/// <summary></summary>
		ARM64		= 0xAA64,
		/// <summary></summary>
		CEE			= 0xC0EE,
	}
}
