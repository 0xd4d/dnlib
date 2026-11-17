// dnlib: See LICENSE.txt for more info

using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolNamespaceImpl : SymbolNamespace {
		readonly ISymUnmanagedNamespace ns;

		public SymbolNamespaceImpl(ISymUnmanagedNamespace @namespace) => ns = @namespace;

		public override string Name {
			get {
				ns.GetName(0, out uint count, null);
				var chars = new char[count];
				ns.GetName((uint)chars.Length, out count, chars);
				if (chars.Length == 0)
					return string.Empty;
				return new string(chars, 0, chars.Length - 1);
			}
		}
	}
}
