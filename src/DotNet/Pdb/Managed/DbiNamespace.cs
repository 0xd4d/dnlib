// dnlib: See LICENSE.txt for more info

using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Managed {
	sealed class DbiNamespace : SymbolNamespace {
		public override string Name {
			get { return name; }
		}
		readonly string name;

		public DbiNamespace(string ns) {
			name = ns;
		}
	}
}
