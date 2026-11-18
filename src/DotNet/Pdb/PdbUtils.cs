// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		public static bool IsValidLocal(PdbLocal pdbLocal, CilBody? body, [NotNullWhen(false)] out string? errorMessage) {
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

	/// <summary>
	/// Offsets of the instructions of methods other than the method being written, eg. async step info
	/// breakpoint methods. One instance is used for the whole PDB write. The first lookup in a method scans
	/// its instructions, the second one creates a dictionary that all later lookups use.
	/// </summary>
	sealed class InstructionOffsetCache {
		// The value is null if the method has only been scanned once
		readonly Dictionary<MethodDef, Dictionary<Instruction, uint>?> cache = new Dictionary<MethodDef, Dictionary<Instruction, uint>?>();

		/// <summary>
		/// Gets the offset of <paramref name="instr"/> in <paramref name="body"/>. If the instruction is in the
		/// body more than once, its first offset is returned.
		/// </summary>
		/// <param name="method">Method, not null</param>
		/// <param name="body">Body of <paramref name="method"/>, not null</param>
		/// <param name="instr">Instruction, not null</param>
		/// <param name="offset">Updated with the offset of <paramref name="instr"/></param>
		/// <returns><c>false</c> if <paramref name="instr"/> isn't in the body</returns>
		public bool TryGetOffset(MethodDef method, CilBody body, Instruction instr, out uint offset) {
			var instrs = body.Instructions;
			if (cache.TryGetValue(method, out var offsets)) {
				if (offsets is null) {
					offsets = new Dictionary<Instruction, uint>(instrs.Count);
					uint currOffset = 0;
					for (int i = 0; i < instrs.Count; i++) {
						var currInstr = instrs[i];
						if (currInstr is null)
							continue;
						if (!offsets.ContainsKey(currInstr))
							offsets.Add(currInstr, currOffset);
						currOffset += (uint)currInstr.GetSize();
					}
					cache[method] = offsets;
				}
				return offsets.TryGetValue(instr, out offset);
			}

			cache.Add(method, null);
			offset = 0;
			for (int i = 0; i < instrs.Count; i++) {
				var currInstr = instrs[i];
				if (currInstr is null)
					continue;
				if (currInstr == instr)
					return true;
				offset += (uint)currInstr.GetSize();
			}
			offset = 0;
			return false;
		}
	}
}
