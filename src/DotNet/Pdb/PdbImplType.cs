// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// PDB implementation type
	/// </summary>
	public enum PdbImplType {
		/// <summary>
		/// Use Microsoft's COM DLL (diasymreader.dll)
		/// </summary>
		MicrosoftCOM,

		/// <summary>
		/// Use the managed PDB reader
		/// </summary>
		Managed,

		/// <summary>
		/// Use the default PDB reader
		/// </summary>
		Default = Managed,
	}
}
