// dnlib: See LICENSE.txt for more info

using System.Diagnostics;
using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolScope : ISymbolScope2 {
		readonly ISymUnmanagedScope scope;

		public SymbolScope(ISymUnmanagedScope scope) {
			this.scope = scope;
		}

		public int EndOffset {
			get {
				uint result;
				scope.GetEndOffset(out result);
				return (int)result;
			}
		}

		public ISymbolMethod Method {
			get {
				ISymUnmanagedMethod method;
				scope.GetMethod(out method);
				return method == null ? null : new SymbolMethod(method);
			}
		}

		public ISymbolScope Parent {
			get {
				ISymUnmanagedScope parentScope;
				scope.GetParent(out parentScope);
				return parentScope == null ? null : new SymbolScope(parentScope);
			}
		}

		public int StartOffset {
			get {
				uint result;
				scope.GetStartOffset(out result);
				return (int)result;
			}
		}

		public ISymbolScope[] GetChildren() {
			uint numScopes;
			scope.GetChildren(0, out numScopes, null);
			var unScopes = new ISymUnmanagedScope[numScopes];
			scope.GetChildren((uint)unScopes.Length, out numScopes, unScopes);
			var scopes = new ISymbolScope[numScopes];
			for (uint i = 0; i < numScopes; i++)
				scopes[i] = new SymbolScope(unScopes[i]);
			return scopes;
		}

		public ISymbolVariable[] GetLocals() {
			uint numVars;
			scope.GetLocals(0, out numVars, null);
			var unVars = new ISymUnmanagedVariable[numVars];
			scope.GetLocals((uint)unVars.Length, out numVars, unVars);
			var vars = new ISymbolVariable[numVars];
			for (uint i = 0; i < numVars; i++)
				vars[i] = new SymbolVariable(unVars[i]);
			return vars;
		}

		public ISymbolNamespace[] GetNamespaces() {
			uint numNss;
			scope.GetNamespaces(0, out numNss, null);
			var unNss = new ISymUnmanagedNamespace[numNss];
			scope.GetNamespaces((uint)unNss.Length, out numNss, unNss);
			var nss = new ISymbolNamespace[numNss];
			for (uint i = 0; i < numNss; i++)
				nss[i] = new SymbolNamespace(unNss[i]);
			return nss;
		}

		public PdbConstant[] GetConstants(ModuleDefMD module, GenericParamContext gpContext) {
			var scope2 = scope as ISymUnmanagedScope2;
			if (scope2 == null)
				return emptySymbolConstants;
			uint numCs;
			scope2.GetConstants(0, out numCs, null);
			if (numCs == 0)
				return emptySymbolConstants;
			var unCs = new ISymUnmanagedConstant[numCs];
			scope2.GetConstants((uint)unCs.Length, out numCs, unCs);
			var nss = new PdbConstant[numCs];
			for (uint i = 0; i < numCs; i++) {
				var unc = unCs[i];
				var name = GetName(unc);
				object value;
				unc.GetValue(out value);
				var sigBytes = GetSignatureBytes(unc);
				TypeSig signature;
				if (sigBytes.Length == 0)
					signature = null;
				else
					signature = SignatureReader.ReadTypeSig(module, module.CorLibTypes, sigBytes, gpContext);
				nss[i] = new PdbConstant(name, signature, value);
			}
			return nss;
		}
		static readonly PdbConstant[] emptySymbolConstants = new PdbConstant[0];

		string GetName(ISymUnmanagedConstant unc) {
			uint count;
			unc.GetName(0, out count, null);
			var chars = new char[count];
			unc.GetName((uint)chars.Length, out count, chars);
			if (chars.Length == 0)
				return string.Empty;
			return new string(chars, 0, chars.Length - 1);
		}

		byte[] GetSignatureBytes(ISymUnmanagedConstant unc) {
			const int E_FAIL = unchecked((int)0x80004005);
			const int E_NOTIMPL = unchecked((int)0x80004001);
			uint bufSize;
			int hr = unc.GetSignature(0, out bufSize, null);
			if (bufSize == 0 || (hr < 0 && hr != E_FAIL && hr != E_NOTIMPL))
				return emptyByteArray;
			var buffer = new byte[bufSize];
			hr = unc.GetSignature((uint)buffer.Length, out bufSize, buffer);
			Debug.Assert(hr == 0);
			if (hr != 0)
				return emptyByteArray;
			return buffer;
		}
		static readonly byte[] emptyByteArray = new byte[0];
	}
}
