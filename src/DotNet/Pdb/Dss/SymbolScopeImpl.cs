// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolScopeImpl : SymbolScope {
		readonly ISymUnmanagedScope scope;
		readonly SymbolMethod method;
		readonly SymbolScope parent;

		public SymbolScopeImpl(ISymUnmanagedScope scope, SymbolMethod method, SymbolScope parent) {
			this.scope = scope;
			this.method = method;
			this.parent = parent;
		}

		public override SymbolMethod Method {
			get { return method; }
		}

		public override SymbolScope Parent {
			get { return parent; }
		}

		public override int StartOffset {
			get {
				uint result;
				scope.GetStartOffset(out result);
				return (int)result;
			}
		}

		public override int EndOffset {
			get {
				uint result;
				scope.GetEndOffset(out result);
				return (int)result;
			}
		}

		public override IList<SymbolScope> Children {
			get {
				if (children == null) {
					uint numScopes;
					scope.GetChildren(0, out numScopes, null);
					var unScopes = new ISymUnmanagedScope[numScopes];
					scope.GetChildren((uint)unScopes.Length, out numScopes, unScopes);
					var scopes = new SymbolScope[numScopes];
					for (uint i = 0; i < numScopes; i++)
						scopes[i] = new SymbolScopeImpl(unScopes[i], method, this);
					children = scopes;
				}
				return children;
			}
		}
		volatile SymbolScope[] children;

		public override IList<SymbolVariable> Locals {
			get {
				if (locals == null) {
					uint numVars;
					scope.GetLocals(0, out numVars, null);
					var unVars = new ISymUnmanagedVariable[numVars];
					scope.GetLocals((uint)unVars.Length, out numVars, unVars);
					var vars = new SymbolVariable[numVars];
					for (uint i = 0; i < numVars; i++)
						vars[i] = new SymbolVariableImpl(unVars[i]);
					locals = vars;
				}
				return locals;
			}
		}
		volatile SymbolVariable[] locals;

		public override IList<SymbolNamespace> Namespaces {
			get {
				if (namespaces == null) {
					uint numNss;
					scope.GetNamespaces(0, out numNss, null);
					var unNss = new ISymUnmanagedNamespace[numNss];
					scope.GetNamespaces((uint)unNss.Length, out numNss, unNss);
					var nss = new SymbolNamespace[numNss];
					for (uint i = 0; i < numNss; i++)
						nss[i] = new SymbolNamespaceImpl(unNss[i]);
					namespaces = nss;
				}
				return namespaces;
			}
		}
		volatile SymbolNamespace[] namespaces;

		public override IList<PdbCustomDebugInfo> CustomDebugInfos {
			get { return emptyPdbCustomDebugInfos; }
		}
		static readonly PdbCustomDebugInfo[] emptyPdbCustomDebugInfos = new PdbCustomDebugInfo[0];

		public override PdbImportScope ImportScope {
			get { return null; }
		}

		public override IList<PdbConstant> GetConstants(ModuleDef module, GenericParamContext gpContext) {
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
