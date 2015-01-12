// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolReader : ISymbolReader {
		readonly ISymUnmanagedReader reader;

		const int E_FAIL = unchecked((int)0x80004005);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">An unmanaged symbol reader</param>
		public SymbolReader(ISymUnmanagedReader reader) {
			if (reader == null)
				throw new ArgumentNullException("reader");
			this.reader = reader;
		}

		public SymbolToken UserEntryPoint {
			get {
				uint token;
				int hr = reader.GetUserEntryPoint(out token);
				if (hr == E_FAIL)
					token = 0;
				else
					Marshal.ThrowExceptionForHR(hr);
				return new SymbolToken((int)token);
			}
		}

		public ISymbolDocument GetDocument(string url, Guid language, Guid languageVendor, Guid documentType) {
			ISymUnmanagedDocument document;
			reader.GetDocument(url, language, languageVendor, documentType, out document);
			return document == null ? null : new SymbolDocument(document);
		}

		public ISymbolDocument[] GetDocuments() {
			uint numDocs;
			reader.GetDocuments(0, out numDocs, null);
			var unDocs = new ISymUnmanagedDocument[numDocs];
			reader.GetDocuments((uint)unDocs.Length, out numDocs, unDocs);
			var docs = new ISymbolDocument[numDocs];
			for (uint i = 0; i < numDocs; i++)
				docs[i] = new SymbolDocument(unDocs[i]);
			return docs;
		}

		public ISymbolVariable[] GetGlobalVariables() {
			uint numVars;
			reader.GetGlobalVariables(0, out numVars, null);
			var unVars = new ISymUnmanagedVariable[numVars];
			reader.GetGlobalVariables((uint)unVars.Length, out numVars, unVars);
			var vars = new ISymbolVariable[numVars];
			for (uint i = 0; i < numVars; i++)
				vars[i] = new SymbolVariable(unVars[i]);
			return vars;
		}

		public ISymbolMethod GetMethod(SymbolToken method) {
			ISymUnmanagedMethod unMethod;
			int hr = reader.GetMethod((uint)method.GetToken(), out unMethod);
			if (hr == E_FAIL)
				return null;
			Marshal.ThrowExceptionForHR(hr);
			return unMethod == null ? null : new SymbolMethod(unMethod);
		}

		public ISymbolMethod GetMethod(SymbolToken method, int version) {
			ISymUnmanagedMethod unMethod;
			int hr = reader.GetMethodByVersion((uint)method.GetToken(), version, out unMethod);
			if (hr == E_FAIL)
				return null;
			Marshal.ThrowExceptionForHR(hr);
			return unMethod == null ? null : new SymbolMethod(unMethod);
		}

		public ISymbolMethod GetMethodFromDocumentPosition(ISymbolDocument document, int line, int column) {
			var symDoc = document as SymbolDocument;
			if (symDoc == null)
				throw new ArgumentException("document is not a non-null SymbolDocument instance");
			ISymUnmanagedMethod unMethod;
			int hr = reader.GetMethodFromDocumentPosition(symDoc.SymUnmanagedDocument, (uint)line, (uint)column, out unMethod);
			if (hr == E_FAIL)
				return null;
			Marshal.ThrowExceptionForHR(hr);
			return unMethod == null ? null : new SymbolMethod(unMethod);
		}

		public ISymbolNamespace[] GetNamespaces() {
			uint numNss;
			reader.GetNamespaces(0, out numNss, null);
			var unNss = new ISymUnmanagedNamespace[numNss];
			reader.GetNamespaces((uint)unNss.Length, out numNss, unNss);
			var nss = new ISymbolNamespace[numNss];
			for (uint i = 0; i < numNss; i++)
				nss[i] = new SymbolNamespace(unNss[i]);
			return nss;
		}

		public byte[] GetSymAttribute(SymbolToken parent, string name) {
			uint bufSize;
			reader.GetSymAttribute((uint)parent.GetToken(), name, 0, out bufSize, null);
			var buffer = new byte[bufSize];
			reader.GetSymAttribute((uint)parent.GetToken(), name, (uint)buffer.Length, out bufSize, buffer);
			return buffer;
		}

		public ISymbolVariable[] GetVariables(SymbolToken parent) {
			uint numVars;
			reader.GetVariables((uint)parent.GetToken(), 0, out numVars, null);
			var unVars = new ISymUnmanagedVariable[numVars];
			reader.GetVariables((uint)parent.GetToken(), (uint)unVars.Length, out numVars, unVars);
			var vars = new ISymbolVariable[numVars];
			for (uint i = 0; i < numVars; i++)
				vars[i] = new SymbolVariable(unVars[i]);
			return vars;
		}
	}
}
