// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Dss {
#if NETCOREAPP
	[SupportedOSPlatform("windows")]
#endif
	sealed class SymbolScopeImpl : SymbolScope {
		readonly ISymUnmanagedScope scope;
		readonly SymbolMethod method;
		readonly SymbolScope parent;

		public SymbolScopeImpl(ISymUnmanagedScope scope, SymbolMethod method, SymbolScope parent) {
			this.scope = scope;
			this.method = method;
			this.parent = parent;
		}

		public override SymbolMethod Method => method;
		public override SymbolScope Parent => parent;

		public override int StartOffset {
			get {
				scope.GetStartOffset(out uint result);
				return (int)result;
			}
		}

		public override int EndOffset {
			get {
				scope.GetEndOffset(out uint result);
				return (int)result;
			}
		}

		public override IList<SymbolScope> Children {
			get {
				if (children is null) {
					scope.GetChildren(0, out uint numScopes, null);
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
				if (locals is null) {
					scope.GetLocals(0, out uint numVars, null);
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
				if (namespaces is null) {
					scope.GetNamespaces(0, out uint numNss, null);
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

		public override IList<PdbCustomDebugInfo> CustomDebugInfos => Array2.Empty<PdbCustomDebugInfo>();
		public override PdbImportScope ImportScope => null;

		public override IList<PdbConstant> GetConstants(ModuleDef module, GenericParamContext gpContext) {
			var scope2 = scope as ISymUnmanagedScope2;
			if (scope2 is null)
				return Array2.Empty<PdbConstant>();
			scope2.GetConstants(0, out uint numCs, null);
			if (numCs == 0)
				return Array2.Empty<PdbConstant>();
			var unCs = new ISymUnmanagedConstant[numCs];
			scope2.GetConstants((uint)unCs.Length, out numCs, unCs);
			var nss = new PdbConstant[numCs];
			for (uint i = 0; i < numCs; i++) {
				var unc = unCs[i];
				var name = GetName(unc);
				var value = GetValue(unc);
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

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("oleaut32.dll")]
		static extern int VariantClear(IntPtr pvarg);

		const ushort VT_ERROR = 10;

		static unsafe object GetValue(ISymUnmanagedConstant unc) {
			// VARIANT is 16 bytes (32-bit) or 24 bytes (64-bit). It must be initialized (VT_EMPTY) since
			// GetValue() clears it first.
			var variant = stackalloc byte[24];
			for (int i = 0; i < 24; i++)
				variant[i] = 0;
			try {
				// Clear it even if it fails in case the reader stored something in it
				if (unc.GetValue((IntPtr)variant) < 0)
					return null;
				// The readers return VT_ERROR (eg. HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)) if they
				// can't return the value, eg. "" (Microsoft.DiaSymReader.Native) or any string (diasymreader.dll).
				// It's not the constant's value, and the marshaler would return it as an Int32.
				if (*(ushort*)variant == VT_ERROR)
					return null;
				return Marshal.GetObjectForNativeVariant((IntPtr)variant);
			}
			finally {
				VariantClear((IntPtr)variant);
			}
		}

		string GetName(ISymUnmanagedConstant unc) {
			unc.GetName(0, out uint count, null);
			var chars = new char[count];
			unc.GetName((uint)chars.Length, out count, chars);
			if (chars.Length == 0)
				return string.Empty;
			return new string(chars, 0, chars.Length - 1);
		}

		byte[] GetSignatureBytes(ISymUnmanagedConstant unc) {
			const int E_FAIL = unchecked((int)0x80004005);
			const int E_NOTIMPL = unchecked((int)0x80004001);
			int hr = unc.GetSignature(0, out uint bufSize, null);
			if (bufSize == 0 || (hr < 0 && hr != E_FAIL && hr != E_NOTIMPL))
				return Array2.Empty<byte>();
			var buffer = new byte[bufSize];
			hr = unc.GetSignature((uint)buffer.Length, out bufSize, buffer);
			Debug.Assert(hr == 0);
			if (hr != 0)
				return Array2.Empty<byte>();
			return buffer;
		}
	}
}
