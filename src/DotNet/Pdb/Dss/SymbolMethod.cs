// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolMethod : ISymbolMethod {
		readonly ISymUnmanagedMethod method;

		public SymbolMethod(ISymUnmanagedMethod method) {
			this.method = method;
		}

		public ISymbolScope RootScope {
			get {
				ISymUnmanagedScope scope;
				method.GetRootScope(out scope);
				return scope == null ? null : new SymbolScope(scope);
			}
		}

		public int SequencePointCount {
			get {
				uint result;
				method.GetSequencePointCount(out result);
				return (int)result;
			}
		}

		public SymbolToken Token {
			get {
				uint result;
				method.GetToken(out result);
				return new SymbolToken((int)result);
			}
		}

		public ISymbolNamespace GetNamespace() {
			ISymUnmanagedNamespace ns;
			method.GetNamespace(out ns);
			return ns == null ? null : new SymbolNamespace(ns);
		}

		public int GetOffset(ISymbolDocument document, int line, int column) {
			var symDoc = document as SymbolDocument;
			if (symDoc == null)
				throw new ArgumentException("document is not a non-null SymbolDocument instance");
			uint result;
			method.GetOffset(symDoc.SymUnmanagedDocument, (uint)line, (uint)column, out result);
			return (int)result;
		}

		public ISymbolVariable[] GetParameters() {
			uint numVars;
			method.GetParameters(0, out numVars, null);
			var unVars = new ISymUnmanagedVariable[numVars];
			method.GetParameters((uint)unVars.Length, out numVars, unVars);
			var vars = new ISymbolVariable[numVars];
			for (uint i = 0; i < numVars; i++)
				vars[i] = new SymbolVariable(unVars[i]);
			return vars;
		}

		public int[] GetRanges(ISymbolDocument document, int line, int column) {
			var symDoc = document as SymbolDocument;
			if (symDoc == null)
				throw new ArgumentException("document is not a non-null SymbolDocument instance");
			uint arySize;
			method.GetRanges(symDoc.SymUnmanagedDocument, (uint)line, (uint)column, 0, out arySize, null);
			var ary = new int[arySize];
			method.GetRanges(symDoc.SymUnmanagedDocument, (uint)line, (uint)column, (uint)ary.Length, out arySize, ary);
			return ary;
		}

		public ISymbolScope GetScope(int offset) {
			ISymUnmanagedScope scope;
			method.GetScopeFromOffset((uint)offset, out scope);
			return scope == null ? null : new SymbolScope(scope);
		}

		public void GetSequencePoints(int[] offsets, ISymbolDocument[] documents, int[] lines, int[] columns, int[] endLines, int[] endColumns) {
			// Any array can be null, and the documentation says we must verify the sizes.

			int arySize = -1;
			if (offsets != null)		arySize = offsets.Length;
			else if (documents != null)	arySize = documents.Length;
			else if (lines != null)		arySize = lines.Length;
			else if (columns != null)	arySize = columns.Length;
			else if (endLines != null)	arySize = endLines.Length;
			else if (endColumns != null)arySize = endColumns.Length;

			if (offsets    != null && offsets.Length    != arySize) throw new ArgumentException("Invalid array length: offsets");
			if (documents  != null && documents.Length  != arySize) throw new ArgumentException("Invalid array length: documents");
			if (lines      != null && lines.Length      != arySize) throw new ArgumentException("Invalid array length: lines");
			if (columns    != null && columns.Length    != arySize) throw new ArgumentException("Invalid array length: columns");
			if (endLines   != null && endLines.Length   != arySize) throw new ArgumentException("Invalid array length: endLines");
			if (endColumns != null && endColumns.Length != arySize) throw new ArgumentException("Invalid array length: endColumns");

			if (arySize <= 0)
				return;

			var unDocs = documents == null ? null : new ISymUnmanagedDocument[documents.Length];
			uint size;
			method.GetSequencePoints((uint)arySize, out size, offsets, unDocs, lines, columns, endLines, endColumns);

			if (unDocs != null) {
				for (int i = 0; i < unDocs.Length; i++)
					documents[i] = unDocs[i] == null ? null : new SymbolDocument(unDocs[i]);
			}
		}

		public bool GetSourceStartEnd(ISymbolDocument[] docs, int[] lines, int[] columns) {
			if (docs    != null && docs.Length    < 2) throw new ArgumentException("Invalid array: length < 2: docs");
			if (lines   != null && lines.Length   < 2) throw new ArgumentException("Invalid array: length < 2: lines");
			if (columns != null && columns.Length < 2) throw new ArgumentException("Invalid array: length < 2: columns");

			var unDocs = docs == null ? null : new ISymUnmanagedDocument[docs.Length];
			bool result;
			method.GetSourceStartEnd(unDocs, lines, columns, out result);

			if (unDocs != null) {
				for (int i = 0; i < unDocs.Length; i++)
					docs[i] = unDocs[i] == null ? null : new SymbolDocument(unDocs[i]);
			}

			return result;
		}
	}
}
