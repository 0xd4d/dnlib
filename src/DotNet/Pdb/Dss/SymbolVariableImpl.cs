// dnlib: See LICENSE.txt for more info

using dnlib.DotNet.Pdb.Symbols;

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

		public override SymbolVariableAttributes Attributes {
			get {
				uint result;
				variable.GetAttributes(out result);
				const int VAR_IS_COMP_GEN = 1;
				if ((result & VAR_IS_COMP_GEN) != 0)
					return SymbolVariableAttributes.CompilerGenerated;
				return SymbolVariableAttributes.None;
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
	}
}
