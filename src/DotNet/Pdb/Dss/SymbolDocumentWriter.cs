// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolDocumentWriter : ISymbolDocumentWriter {
		readonly ISymUnmanagedDocumentWriter writer;
		public ISymUnmanagedDocumentWriter SymUnmanagedDocumentWriter => writer;
		public SymbolDocumentWriter(ISymUnmanagedDocumentWriter writer) => this.writer = writer;
		public void SetCheckSum(Guid algorithmId, byte[] checkSum) {
			if (!(checkSum is null) && checkSum.Length != 0 && algorithmId != Guid.Empty)
				writer.SetCheckSum(algorithmId, (uint)checkSum.Length, checkSum);
		}
		public void SetSource(byte[] source) => writer.SetSource((uint)source.Length, source);
	}
}
