namespace dot10.PE {
	/// <summary>
	/// IMAGE_FILE_HEADER.Machine enum
	/// </summary>
	public enum Machine : ushort {
		/// <summary>
		/// x86
		/// </summary>
		I386 = 0x014C,

		/// <summary>
		/// IA-64
		/// </summary>
		IA64 = 0x0200,

		/// <summary>
		/// x64
		/// </summary>
		AMD64 = 0x8664,
	}
}
