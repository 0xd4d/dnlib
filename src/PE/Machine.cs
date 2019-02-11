// dnlib: See LICENSE.txt for more info

namespace dnlib.PE {
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

		// Search for IMAGE_FILE_MACHINE_NATIVE and IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE here:
		//		https://github.com/dotnet/coreclr/blob/master/src/inc/pedecoder.h
		// Note that IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE == 0 if it's Windows

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		I386_Native_Apple = I386 ^ 0x4644,
		AMD64_Native_Apple = AMD64 ^ 0x4644,
		ARMNT_Native_Apple = ARMNT ^ 0x4644,
		ARM64_Native_Apple = ARM64 ^ 0x4644,

		I386_Native_FreeBSD = I386 ^ 0xADC4,
		AMD64_Native_FreeBSD = AMD64 ^ 0xADC4,
		ARMNT_Native_FreeBSD = ARMNT ^ 0xADC4,
		ARM64_Native_FreeBSD = ARM64 ^ 0xADC4,

		I386_Native_Linux = I386 ^ 0x7B79,
		AMD64_Native_Linux = AMD64 ^ 0x7B79,
		ARMNT_Native_Linux = ARMNT ^ 0x7B79,
		ARM64_Native_Linux = ARM64 ^ 0x7B79,

		I386_Native_NetBSD = I386 ^ 0x1993,
		AMD64_Native_NetBSD = AMD64 ^ 0x1993,
		ARMNT_Native_NetBSD = ARMNT ^ 0x1993,
		ARM64_Native_NetBSD = ARM64 ^ 0x1993,
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// Extensions
	/// </summary>
	public static class MachineExtensions {
		/// <summary>
		/// Checks if <paramref name="machine"/> is a 64-bit machine
		/// </summary>
		/// <param name="machine">Machine</param>
		/// <returns></returns>
		public static bool Is64Bit(this Machine machine) {
			switch (machine) {
			case Machine.IA64:

			case Machine.AMD64:
			case Machine.AMD64_Native_Apple:
			case Machine.AMD64_Native_FreeBSD:
			case Machine.AMD64_Native_Linux:
			case Machine.AMD64_Native_NetBSD:

			case Machine.ARM64:
			case Machine.ARM64_Native_Apple:
			case Machine.ARM64_Native_FreeBSD:
			case Machine.ARM64_Native_Linux:
			case Machine.ARM64_Native_NetBSD:
				return true;

			default:
				return false;
			}
		}

		/// <summary>
		/// Checks if <paramref name="machine"/> is <see cref="Machine.I386"/>, <see cref="Machine.I386_Native_Apple"/>, etc...
		/// </summary>
		/// <param name="machine">Machine</param>
		/// <returns></returns>
		public static bool IsI386(this Machine machine) {
			switch (machine) {
			case Machine.I386:
			case Machine.I386_Native_Apple:
			case Machine.I386_Native_FreeBSD:
			case Machine.I386_Native_Linux:
			case Machine.I386_Native_NetBSD:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Checks if <paramref name="machine"/> is <see cref="Machine.AMD64"/>, <see cref="Machine.AMD64_Native_Apple"/>, etc...
		/// </summary>
		/// <param name="machine">Machine</param>
		/// <returns></returns>
		public static bool IsAMD64(this Machine machine) {
			switch (machine) {
			case Machine.AMD64:
			case Machine.AMD64_Native_Apple:
			case Machine.AMD64_Native_FreeBSD:
			case Machine.AMD64_Native_Linux:
			case Machine.AMD64_Native_NetBSD:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Checks if <paramref name="machine"/> is <see cref="Machine.ARMNT"/>, <see cref="Machine.ARMNT_Native_Apple"/>, etc...
		/// </summary>
		/// <param name="machine">Machine</param>
		/// <returns></returns>
		public static bool IsARMNT(this Machine machine) {
			switch (machine) {
			case Machine.ARMNT:
			case Machine.ARMNT_Native_Apple:
			case Machine.ARMNT_Native_FreeBSD:
			case Machine.ARMNT_Native_Linux:
			case Machine.ARMNT_Native_NetBSD:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Checks if <paramref name="machine"/> is <see cref="Machine.ARM64"/>, <see cref="Machine.ARM64_Native_Apple"/>, etc...
		/// </summary>
		/// <param name="machine">Machine</param>
		/// <returns></returns>
		public static bool IsARM64(this Machine machine) {
			switch (machine) {
			case Machine.ARM64:
			case Machine.ARM64_Native_Apple:
			case Machine.ARM64_Native_FreeBSD:
			case Machine.ARM64_Native_Linux:
			case Machine.ARM64_Native_NetBSD:
				return true;
			default:
				return false;
			}
		}
	}
}
