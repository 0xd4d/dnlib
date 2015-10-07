// dnlib: See LICENSE.txt for more info

using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolNamespace : ISymbolNamespace {
		readonly ISymUnmanagedNamespace ns;

		public SymbolNamespace(ISymUnmanagedNamespace @namespace) {
			this.ns = @namespace;
		}

		public string Name {
			get {
				uint count;
				ns.GetName(0, out count, null);
				var chars = new char[count];
				ns.GetName((uint)chars.Length, out count, chars);
				if (chars.Length == 0)
					return string.Empty;
				return new string(chars, 0, chars.Length - 1);
			}
		}

		public ISymbolNamespace[] GetNamespaces() {
			uint numNss;
			ns.GetNamespaces(0, out numNss, null);
			var unNss = new ISymUnmanagedNamespace[numNss];
			ns.GetNamespaces((uint)unNss.Length, out numNss, unNss);
			var nss = new ISymbolNamespace[numNss];
			for (uint i = 0; i < numNss; i++)
				nss[i] = new SymbolNamespace(unNss[i]);
			return nss;
		}

		public ISymbolVariable[] GetVariables() {
			uint numVars;
			ns.GetVariables(0, out numVars, null);
			var unVars = new ISymUnmanagedVariable[numVars];
			ns.GetVariables((uint)unVars.Length, out numVars, unVars);
			var vars = new ISymbolVariable[numVars];
			for (uint i = 0; i < numVars; i++)
				vars[i] = new SymbolVariable(unVars[i]);
			return vars;
		}
	}
}
