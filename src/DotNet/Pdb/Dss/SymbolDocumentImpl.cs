// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolDocumentImpl : SymbolDocument {
		readonly ISymUnmanagedDocument document;
		public ISymUnmanagedDocument SymUnmanagedDocument => document;
		public SymbolDocumentImpl(ISymUnmanagedDocument document) => this.document = document;

		public override Guid CheckSumAlgorithmId {
			get {
				document.GetCheckSumAlgorithmId(out var guid);
				return guid;
			}
		}

		public override Guid DocumentType {
			get {
				document.GetDocumentType(out var guid);
				return guid;
			}
		}

		public override Guid Language {
			get {
				document.GetLanguage(out var guid);
				return guid;
			}
		}

		public override Guid LanguageVendor {
			get {
				document.GetLanguageVendor(out var guid);
				return guid;
			}
		}

		public override string URL {
			get {
				document.GetURL(0, out uint count, null);
				var chars = new char[count];
				document.GetURL((uint)chars.Length, out count, chars);
				if (chars.Length == 0)
					return string.Empty;
				return new string(chars, 0, chars.Length - 1);
			}
		}

		public override byte[] CheckSum {
			get {
				document.GetCheckSum(0, out uint bufSize, null);
				var buffer = new byte[bufSize];
				document.GetCheckSum((uint)buffer.Length, out bufSize, buffer);
				return buffer;
			}
		}

		public override PdbCustomDebugInfo[] CustomDebugInfos => Array2.Empty<PdbCustomDebugInfo>();
	}
}
