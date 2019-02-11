// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.WindowsPdb {
	abstract class SymbolWriter : IDisposable {
		public abstract bool IsDeterministic { get; }
		public abstract bool SupportsAsyncMethods { get; }

		public abstract void Initialize(Metadata metadata);
		public abstract void Close();
		public abstract bool GetDebugInfo(ChecksumAlgorithm pdbChecksumAlgorithm, ref uint pdbAge, out Guid guid, out uint stamp, out IMAGE_DEBUG_DIRECTORY pIDD, out byte[] codeViewData);

		public abstract void SetUserEntryPoint(MDToken entryMethod);
		public abstract ISymbolDocumentWriter DefineDocument(string url, Guid language, Guid languageVendor, Guid documentType);
		public abstract void SetSourceServerData(byte[] data);
		public abstract void SetSourceLinkData(byte[] data);

		public abstract void OpenMethod(MDToken method);
		public abstract void CloseMethod();
		public abstract int OpenScope(int startOffset);
		public abstract void CloseScope(int endOffset);
		public abstract void SetSymAttribute(MDToken parent, string name, byte[] data);
		public abstract void UsingNamespace(string fullName);
		public abstract void DefineSequencePoints(ISymbolDocumentWriter document, uint arraySize, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns);
		public abstract void DefineLocalVariable(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3, uint startOffset, uint endOffset);
		public abstract void DefineConstant(string name, object value, uint sigToken);
		public abstract void DefineKickoffMethod(uint kickoffMethod);
		public abstract void DefineCatchHandlerILOffset(uint catchHandlerOffset);
		public abstract void DefineAsyncStepInfo(uint[] yieldOffsets, uint[] breakpointOffset, uint[] breakpointMethod);

		public abstract void Dispose();
	}
}
