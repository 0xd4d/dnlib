/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
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
