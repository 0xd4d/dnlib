// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.DotNet.Pdb.WindowsPdb;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolReaderImpl : SymbolReader {
		ModuleDef module;
		readonly ISymUnmanagedReader reader;

		const int E_FAIL = unchecked((int)0x80004005);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">An unmanaged symbol reader</param>
		public SymbolReaderImpl(ISymUnmanagedReader reader) {
			if (reader == null)
				throw new ArgumentNullException("reader");
			this.reader = reader;
		}

		public override PdbFileKind PdbFileKind {
			get { return PdbFileKind.WindowsPDB; }
		}

		public override int UserEntryPoint {
			get {
				uint token;
				int hr = reader.GetUserEntryPoint(out token);
				if (hr == E_FAIL)
					token = 0;
				else
					Marshal.ThrowExceptionForHR(hr);
				return (int)token;
			}
		}

		public override IList<SymbolDocument> Documents {
			get {
				if (documents == null) {
					uint numDocs;
					reader.GetDocuments(0, out numDocs, null);
					var unDocs = new ISymUnmanagedDocument[numDocs];
					reader.GetDocuments((uint)unDocs.Length, out numDocs, unDocs);
					var docs = new SymbolDocument[numDocs];
					for (uint i = 0; i < numDocs; i++)
						docs[i] = new SymbolDocumentImpl(unDocs[i]);
					documents = docs;
				}
				return documents;
			}
		}
		volatile SymbolDocument[] documents;

		public override void Initialize(ModuleDef module) {
			this.module = module;
		}

		public override SymbolMethod GetMethod(MethodDef method, int version) {
			ISymUnmanagedMethod unMethod;
			int hr = reader.GetMethodByVersion(method.MDToken.Raw, version, out unMethod);
			if (hr == E_FAIL)
				return null;
			Marshal.ThrowExceptionForHR(hr);
			return unMethod == null ? null : new SymbolMethodImpl(this, unMethod);
		}

		internal void GetCustomDebugInfos(SymbolMethodImpl symMethod, MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result) {
			var asyncMethod = PseudoCustomDebugInfoFactory.TryCreateAsyncMethod(method.Module, method, body, symMethod.AsyncKickoffMethod, symMethod.AsyncStepInfos, symMethod.AsyncCatchHandlerILOffset);
			if (asyncMethod != null)
				result.Add(asyncMethod);

			const string CDI_NAME = "MD2";
			uint bufSize;
			reader.GetSymAttribute(method.MDToken.Raw, CDI_NAME, 0, out bufSize, null);
			if (bufSize == 0)
				return;
			var cdiData = new byte[bufSize];
			reader.GetSymAttribute(method.MDToken.Raw, CDI_NAME, (uint)cdiData.Length, out bufSize, cdiData);
			PdbCustomDebugInfoReader.Read(method, body, result, cdiData);
		}

		public override void GetCustomDebugInfos(int token, GenericParamContext gpContext, IList<PdbCustomDebugInfo> result) {
		}
	}
}
