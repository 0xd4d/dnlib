// dnlib: See LICENSE.txt for more info

﻿using System;
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

		public override string URL => url;
		public override Guid Language => language;
		public override Guid LanguageVendor => languageVendor;
		public override Guid DocumentType => documentType;
		public override Guid CheckSumAlgorithmId => checkSumAlgorithmId;
		public override byte[] CheckSum => checkSum;
		public override PdbCustomDebugInfo[] CustomDebugInfos => Array2.Empty<PdbCustomDebugInfo>();

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

			var len = reader.ReadInt32();
			if (reader.ReadUInt32() != 0)
				throw new PdbException("Unexpected value");

			checkSum = reader.ReadBytes(len);
		}
	}
}
