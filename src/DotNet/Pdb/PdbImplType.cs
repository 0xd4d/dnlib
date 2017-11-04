// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// PDB implementation type
	/// </summary>
	public enum PdbImplType {
		/// <summary>
		/// Use Microsoft's COM DLL (diasymreader.dll). It's not recommended to use this reader since it can only be accessed on the COM thread.
		/// 
		/// This reader can only read the old PDB files (aka Windows PDB files). It does not support portable PDB files.
		/// </summary>
		MicrosoftCOM,

		/// <summary>
		/// Use the managed PDB reader. It supports Windows PDB files and portable PDB files and is the default PDB reader.
		/// </summary>
		Managed,

		/// <summary>
		/// Use the default PDB reader
		/// </summary>
		Default = Managed,
	}
}
