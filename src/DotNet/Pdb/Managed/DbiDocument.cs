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

		public void Read(IImageStream stream) {
			stream.Position = 0;
			language = new Guid(stream.ReadBytes(0x10));
			languageVendor = new Guid(stream.ReadBytes(0x10));
			documentType = new Guid(stream.ReadBytes(0x10));
			checkSumAlgorithmId = new Guid(stream.ReadBytes(0x10));

			var len = stream.ReadInt32();
			if (stream.ReadUInt32() != 0)
				throw new PdbException("Unexpected value");

			checkSum = stream.ReadBytes(len);
		}
	}
}
