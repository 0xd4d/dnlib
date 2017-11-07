// dnlib: See LICENSE.txt for more info

using dnlib.DotNet.Pdb.Symbols;
using dnlib.DotNet.Pdb.WindowsPdb;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolVariableImpl : SymbolVariable {
		readonly ISymUnmanagedVariable variable;

		public SymbolVariableImpl(ISymUnmanagedVariable variable) {
			this.variable = variable;
		}

		public override int Index {
			get {
				uint result;
				variable.GetAddressField1(out result);
				return (int)result;
			}
		}

		public override PdbLocalAttributes Attributes {
			get {
				uint result;
				variable.GetAttributes(out result);
				if ((result & (uint)CorSymVarFlag.VAR_IS_COMP_GEN) != 0)
					return PdbLocalAttributes.DebuggerHidden;
				return PdbLocalAttributes.None;
			}
		}

		public override string Name {
			get {
				uint count;
				variable.GetName(0, out count, null);
				var chars = new char[count];
				variable.GetName((uint)chars.Length, out count, chars);
				if (chars.Length == 0)
					return string.Empty;
				return new string(chars, 0, chars.Length - 1);
			}
		}

		public override PdbCustomDebugInfo[] CustomDebugInfos {
			get { return emptyPdbCustomDebugInfos; }
		}
		static readonly PdbCustomDebugInfo[] emptyPdbCustomDebugInfos = new PdbCustomDebugInfo[0];
	}
}
