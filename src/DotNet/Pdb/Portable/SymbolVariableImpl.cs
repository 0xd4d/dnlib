// dnlib: See LICENSE.txt for more info

using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Portable {
	sealed class SymbolVariableImpl : SymbolVariable {
		readonly string name;
		readonly SymbolVariableAttributes attributes;
		readonly int index;

		public override string Name {
			get { return name; }
		}

		public override SymbolVariableAttributes Attributes {
			get { return attributes; }
		}

		public override int Index {
			get { return index; }
		}

		public SymbolVariableImpl(string name, SymbolVariableAttributes attributes, int index) {
			this.name = name;
			this.attributes = attributes;
			this.index = index;
		}
	}
}
