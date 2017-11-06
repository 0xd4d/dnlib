// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolDocumentImpl : SymbolDocument {
		readonly ISymUnmanagedDocument document;

		public ISymUnmanagedDocument SymUnmanagedDocument {
			get { return document; }
		}

		public SymbolDocumentImpl(ISymUnmanagedDocument document) {
			this.document = document;
		}

		public override Guid CheckSumAlgorithmId {
			get {
				Guid guid;
				document.GetCheckSumAlgorithmId(out guid);
				return guid;
			}
		}

		public override Guid DocumentType {
			get {
				Guid guid;
				document.GetDocumentType(out guid);
				return guid;
			}
		}

		public override Guid Language {
			get {
				Guid guid;
				document.GetLanguage(out guid);
				return guid;
			}
		}

		public override Guid LanguageVendor {
			get {
				Guid guid;
				document.GetLanguageVendor(out guid);
				return guid;
			}
		}

		public override string URL {
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

		public override byte[] CheckSum {
			get {
				uint bufSize;
				document.GetCheckSum(0, out bufSize, null);
				var buffer = new byte[bufSize];
				document.GetCheckSum((uint)buffer.Length, out bufSize, buffer);
				return buffer;
			}
		}

		public override PdbCustomDebugInfo[] CustomDebugInfos {
			get { return emptyPdbCustomDebugInfos; }
		}
		static readonly PdbCustomDebugInfo[] emptyPdbCustomDebugInfos = new PdbCustomDebugInfo[0];
	}
}
