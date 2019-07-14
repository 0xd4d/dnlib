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

		byte[] SourceCode {
			get {
				int hr = document.GetSourceLength(out int size);
				if (hr < 0)
					return null;
				if (size <= 0)
					return null;
				var sourceCode = new byte[size];
				hr = document.GetSourceRange(0, 0, int.MaxValue, int.MaxValue, size, out var bytesRead, sourceCode);
				if (hr < 0)
					return null;
				if (bytesRead <= 0)
					return null;
				if (bytesRead != sourceCode.Length)
					Array.Resize(ref sourceCode, bytesRead);
				return sourceCode;
			}
		}

		public override PdbCustomDebugInfo[] CustomDebugInfos {
			get {
				if (customDebugInfos is null) {
					var sourceCode = SourceCode;
					if (!(sourceCode is null))
						customDebugInfos = new PdbCustomDebugInfo[1] { new PdbEmbeddedSourceCustomDebugInfo(sourceCode) };
					else
						customDebugInfos = Array2.Empty<PdbCustomDebugInfo>();
				}
				return customDebugInfos;
			}
		}
		PdbCustomDebugInfo[] customDebugInfos;
	}
}
