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

		public override string URL {
			get { return url; }
		}
		public override Guid Language {
			get { return language; }
		}
		public override Guid LanguageVendor {
			get { return languageVendor; }
		}
		public override Guid DocumentType {
			get { return documentType; }
		}
		public override Guid CheckSumAlgorithmId {
			get { return checkSumAlgorithmId; }
		}
		public override byte[] CheckSum {
			get { return checkSum; }
		}
		public override PdbCustomDebugInfo[] CustomDebugInfos {
			get { return emptyPdbCustomDebugInfos; }
		}
		static readonly PdbCustomDebugInfo[] emptyPdbCustomDebugInfos = new PdbCustomDebugInfo[0];

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
