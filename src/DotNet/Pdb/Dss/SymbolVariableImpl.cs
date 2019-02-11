// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.DotNet.Pdb.WindowsPdb;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolVariableImpl : SymbolVariable {
		readonly ISymUnmanagedVariable variable;

		public SymbolVariableImpl(ISymUnmanagedVariable variable) => this.variable = variable;

		public override int Index {
			get {
				variable.GetAddressField1(out uint result);
				return (int)result;
			}
		}

		public override PdbLocalAttributes Attributes {
			get {
				variable.GetAttributes(out uint result);
				if ((result & (uint)CorSymVarFlag.VAR_IS_COMP_GEN) != 0)
					return PdbLocalAttributes.DebuggerHidden;
				return PdbLocalAttributes.None;
			}
		}

		public override string Name {
			get {
				variable.GetName(0, out uint count, null);
				var chars = new char[count];
				variable.GetName((uint)chars.Length, out count, chars);
				if (chars.Length == 0)
					return string.Empty;
				return new string(chars, 0, chars.Length - 1);
			}
		}

		public override PdbCustomDebugInfo[] CustomDebugInfos => Array2.Empty<PdbCustomDebugInfo>();
	}
}
