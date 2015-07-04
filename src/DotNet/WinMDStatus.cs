// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet {
	/// <summary>
	/// WinMD status
	/// </summary>
	public enum WinMDStatus {
		/// <summary>
		/// This is not a WinMD file
		/// </summary>
		None,

		/// <summary>
		/// This is a pure WinMD file (not managed)
		/// </summary>
		Pure,

		/// <summary>
		/// This is a managed WinMD file (created by eg. winmdexp.exe)
		/// </summary>
		Managed,
	}
}
