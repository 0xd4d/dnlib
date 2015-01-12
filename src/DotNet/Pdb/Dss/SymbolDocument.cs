// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolDocument : ISymbolDocument {
		readonly ISymUnmanagedDocument document;

		public ISymUnmanagedDocument SymUnmanagedDocument {
			get { return document; }
		}

		public SymbolDocument(ISymUnmanagedDocument document) {
			this.document = document;
		}

		public Guid CheckSumAlgorithmId {
			get {
				Guid guid;
				document.GetCheckSumAlgorithmId(out guid);
				return guid;
			}
		}

		public Guid DocumentType {
			get {
				Guid guid;
				document.GetDocumentType(out guid);
				return guid;
			}
		}

		public bool HasEmbeddedSource {
			get {
				bool result;
				document.HasEmbeddedSource(out result);
				return result;
			}
		}

		public Guid Language {
			get {
				Guid guid;
				document.GetLanguage(out guid);
				return guid;
			}
		}

		public Guid LanguageVendor {
			get {
				Guid guid;
				document.GetLanguageVendor(out guid);
				return guid;
			}
		}

		public int SourceLength {
			get {
				uint result;
				document.GetSourceLength(out result);
				return (int)result;
			}
		}

		public string URL {
			get {
				uint count;
				document.GetURL(0, out count, null);
				var chars = new char[count];
				document.GetURL((uint)chars.Length, out count, chars);
				if (chars.Length == 0)
					return string.Empty;
				return new string(chars, 0, chars.Length - 1);
			}
		}

		public int FindClosestLine(int line) {
			uint result;
			document.FindClosestLine((uint)line, out result);
			return (int)result;
		}

		public byte[] GetCheckSum() {
			uint bufSize;
			document.GetCheckSum(0, out bufSize, null);
			var buffer = new byte[bufSize];
			document.GetCheckSum((uint)buffer.Length, out bufSize, buffer);
			return buffer;
		}

		public byte[] GetSourceRange(int startLine, int startColumn, int endLine, int endColumn) {
			uint bufSize;
			document.GetSourceRange((uint)startLine, (uint)startColumn, (uint)endLine, (uint)endColumn, 0, out bufSize, null);
			var buffer = new byte[bufSize];
			document.GetSourceRange((uint)startLine, (uint)startColumn, (uint)endLine, (uint)endColumn, (uint)buffer.Length, out bufSize, buffer);
			return buffer;
		}
	}
}
