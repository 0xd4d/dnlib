// dnlib: See LICENSE.txt for more info

namespace dnlib.TestUtils.Dumping {
	/// <summary>
	/// What <see cref="ModuleDumper"/> writes
	/// </summary>
	public sealed class ModuleDumpOptions {
		/// <summary>
		/// <c>true</c> to write the max stack value of method bodies. The writer calculates a new value unless
		/// it's told to keep the old one, so it can only be compared in that case.
		/// </summary>
		public bool MaxStack { get; set; }

		/// <summary>
		/// <c>true</c> to write the info of exported methods (name, ordinal, options)
		/// </summary>
		public bool MethodExportInfo { get; set; } = true;

		/// <summary>
		/// <c>true</c> to write the debug info that was read from a PDB file: documents, sequence points,
		/// scopes, local variable names, constants, imports and custom debug info
		/// </summary>
		public bool Pdb { get; set; }
	}
}
