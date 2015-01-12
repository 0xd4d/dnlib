// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb.Managed {
	sealed class DbiNamespace : ISymbolNamespace {
		public string Namespace { get; private set; }

		public DbiNamespace(string ns) {
			Namespace = ns;
		}

		#region ISymbolNamespace

		public string Name {
			get { return Namespace; }
		}

		public ISymbolNamespace[] GetNamespaces() {
			throw new NotImplementedException();
		}

		public ISymbolVariable[] GetVariables() {
			throw new NotImplementedException();
		}

		#endregion
	}
}