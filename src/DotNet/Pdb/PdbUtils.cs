// dnlib: See LICENSE.txt for more info

using dnlib.DotNet.Emit;

namespace dnlib.DotNet.Pdb {
	static class PdbUtils {
		public static bool IsEndInclusive(PdbFileKind pdbFileKind, Compiler compiler) =>
			pdbFileKind == PdbFileKind.WindowsPDB && compiler == Compiler.VisualBasic;

		/// <summary>
		/// Checks whether a PDB writer can write <paramref name="pdbLocal"/>: its <see cref="PdbLocal.Local"/>
		/// must be a local of <paramref name="body"/>
		/// </summary>
		/// <param name="pdbLocal">PDB local, not null</param>
		/// <param name="body">Body of the method that owns the PDB local</param>
		/// <param name="errorMessage">Error message if it's not valid. <c>{0}</c> is the name of the PDB local</param>
		/// <returns><c>true</c> if it can be written</returns>
		public static bool IsValidLocal(PdbLocal pdbLocal, CilBody body, out string errorMessage) {
			var local = pdbLocal.Local;
			if (local is null) {
				errorMessage = "PDB local '{0}' has no local (PdbLocal.Local is null)";
				return false;
			}
			var variables = body?.Variables;
			int index = local.Index;
			if (variables is null || (uint)index >= (uint)variables.Count || variables[index] != local) {
				errorMessage = "The local of PDB local '{0}' isn't a local of the method body, maybe it was removed";
				return false;
			}
			errorMessage = null;
			return true;
		}
	}
}
