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
