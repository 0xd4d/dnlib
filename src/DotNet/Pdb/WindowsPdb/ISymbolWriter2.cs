// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.WindowsPdb {
	interface ISymbolWriter2 : ISymbolWriter, IDisposable {
		bool IsDeterministic { get; }
		bool SupportsAsyncMethods { get; }

		void DefineSequencePoints(ISymbolDocumentWriter document, uint arraySize, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns);
		bool GetDebugInfo(ChecksumAlgorithm pdbChecksumAlgorithm, ref uint pdbAge, out Guid guid, out uint stamp, out IMAGE_DEBUG_DIRECTORY pIDD, out byte[] codeViewData);
		void DefineLocalVariable2(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3, uint startOffset, uint endOffset);
		void Initialize(Metadata metadata);
		void DefineConstant2(string name, object value, uint sigToken);
		void DefineKickoffMethod(uint kickoffMethod);
		void DefineCatchHandlerILOffset(uint catchHandlerOffset);
		void DefineAsyncStepInfo(uint[] yieldOffsets, uint[] breakpointOffset, uint[] breakpointMethod);
	}
}
