// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	sealed class DbiDocument : SymbolDocument {
		readonly string url;
		Guid language;
		Guid languageVendor;
		Guid documentType;
		Guid checkSumAlgorithmId;
		byte[] checkSum;
		byte[] sourceCode;

		public override string URL => url;
		public override Guid Language => language;
		public override Guid LanguageVendor => languageVendor;
		public override Guid DocumentType => documentType;
		public override Guid CheckSumAlgorithmId => checkSumAlgorithmId;
		public override byte[] CheckSum => checkSum;
		byte[] SourceCode => sourceCode;

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

		public DbiDocument(string url) {
			this.url = url;
			documentType = SymDocumentType.Text;
		}

		public void Read(ref DataReader reader) {
			reader.Position = 0;
			language = reader.ReadGuid();
			languageVendor = reader.ReadGuid();
			documentType = reader.ReadGuid();
			checkSumAlgorithmId = reader.ReadGuid();
			int checkSumLen = reader.ReadInt32();
			int sourceLen = reader.ReadInt32();
			checkSum = reader.ReadBytes(checkSumLen);
			sourceCode = sourceLen == 0 ? null : reader.ReadBytes(sourceLen);
			Debug.Assert(reader.BytesLeft == 0);
		}
	}
}
