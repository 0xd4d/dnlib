// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// PDB reader options
	/// </summary>
	[Flags]
	public enum PdbReaderOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Use the COM Windows PDB reader instead of the managed Windows PDB reader.
		/// 
		/// This is NOT recommended since the COM reader can only be called on the same
		/// thread it was created on. It also requires a Windows OS.
		/// 
		/// If this is not set, the managed PDB reader will be used.
		/// 
		/// This option is only used if it's a Windows PDB file, not if it's a Portable PDB file.
		/// </summary>
		MicrosoftComReader		= 0x00000001,

		/// <summary>
		/// Don't use Microsoft.DiaSymReader.Native. This is a NuGet package with an updated Windows PDB reader/writer implementation,
		/// and if it's available at runtime, dnlib will try to use it. If this option is set, dnlib won't use it.
		/// You have to add a reference to the NuGet package if you want to use it, dnlib has no reference to the NuGet package.
		/// 
		/// Only used if <see cref="MicrosoftComReader"/> is set and if it's a Windows PDB file
		/// </summary>
		NoDiaSymReader			= 0x00000002,

		/// <summary>
		/// Don't use diasymreader.dll's PDB reader that is shipped with .NET Framework.
		/// 
		/// Only used if <see cref="MicrosoftComReader"/> is set and if it's a Windows PDB file
		/// </summary>
		NoOldDiaSymReader		= 0x00000004,
	}
}
