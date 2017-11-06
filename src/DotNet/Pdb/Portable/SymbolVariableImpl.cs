// dnlib: See LICENSE.txt for more info

using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Portable {
	sealed class SymbolVariableImpl : SymbolVariable {
		readonly string name;
		readonly PdbLocalAttributes attributes;
		readonly int index;
		readonly PdbCustomDebugInfo[] customDebugInfos;

		public override string Name {
			get { return name; }
		}

		public override PdbLocalAttributes Attributes {
			get { return attributes; }
		}

		public override int Index {
			get { return index; }
		}

		public override PdbCustomDebugInfo[] CustomDebugInfos {
			get { return customDebugInfos; }
		}

		public SymbolVariableImpl(string name, PdbLocalAttributes attributes, int index, PdbCustomDebugInfo[] customDebugInfos) {
			this.name = name;
			this.attributes = attributes;
			this.index = index;
			this.customDebugInfos = customDebugInfos;
		}
	}
}
