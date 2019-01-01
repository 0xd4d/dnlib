// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb.Portable {
	[DebuggerDisplay("{GetDebuggerString(),nq}")]
	sealed class SymbolDocumentImpl : SymbolDocument {
		readonly string url;
		/*readonly*/ Guid language;
		/*readonly*/ Guid languageVendor;
		/*readonly*/ Guid documentType;
		/*readonly*/ Guid checkSumAlgorithmId;
		readonly byte[] checkSum;
		readonly PdbCustomDebugInfo[] customDebugInfos;

		string GetDebuggerString() {
			var sb = new StringBuilder();
			if (language == PdbDocumentConstants.LanguageCSharp)
				sb.Append("C#");
			else if (language == PdbDocumentConstants.LanguageVisualBasic)
				sb.Append("VB");
			else if (language == PdbDocumentConstants.LanguageFSharp)
				sb.Append("F#");
			else
				sb.Append(language.ToString());
			sb.Append(", ");
			if (checkSumAlgorithmId == PdbDocumentConstants.HashSHA1)
				sb.Append("SHA-1");
			else if (checkSumAlgorithmId == PdbDocumentConstants.HashSHA256)
				sb.Append("SHA-256");
			else
				sb.Append(checkSumAlgorithmId.ToString());
			sb.Append(": ");
			sb.Append(url);
			return sb.ToString();
		}

		public override string URL => url;
		public override Guid Language => language;
		public override Guid LanguageVendor => languageVendor;
		public override Guid DocumentType => documentType;
		public override Guid CheckSumAlgorithmId => checkSumAlgorithmId;
		public override byte[] CheckSum => checkSum;
		public override PdbCustomDebugInfo[] CustomDebugInfos => customDebugInfos;

		public SymbolDocumentImpl(string url, Guid language, Guid languageVendor, Guid documentType, Guid checkSumAlgorithmId, byte[] checkSum, PdbCustomDebugInfo[] customDebugInfos) {
			this.url = url;
			this.language = language;
			this.languageVendor = languageVendor;
			this.documentType = documentType;
			this.checkSumAlgorithmId = checkSumAlgorithmId;
			this.checkSum = checkSum;
			this.customDebugInfos = customDebugInfos;
		}
	}
}
