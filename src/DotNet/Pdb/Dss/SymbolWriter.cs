// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using dnlib.DotNet.Pdb.WindowsPdb;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolWriter : ISymbolWriter2 {
		readonly ISymUnmanagedWriter2 writer;
		readonly ISymUnmanagedAsyncMethodPropertiesWriter asyncMethodWriter;
		readonly string pdbFileName;
		readonly Stream pdbStream;
		readonly bool ownsStream;
		readonly bool isDeterministic;
		bool closeCalled;

		public bool IsDeterministic => isDeterministic;
		public bool SupportsAsyncMethods => asyncMethodWriter != null;

		public SymbolWriter(ISymUnmanagedWriter2 writer, string pdbFileName, Stream pdbStream, PdbWriterOptions options, bool ownsStream) {
			this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
			asyncMethodWriter = writer as ISymUnmanagedAsyncMethodPropertiesWriter;
			this.pdbStream = pdbStream ?? throw new ArgumentNullException(nameof(pdbStream));
			this.pdbFileName = pdbFileName;
			this.ownsStream = ownsStream;
			// If it's deterministic, we should call UpdateSignatureByHashingContent() or UpdateSignature(),
			// but that requires v7 or v8. InitializeDeterministic() is v6.
			isDeterministic = (options & PdbWriterOptions.Deterministic) != 0 && writer is ISymUnmanagedWriter7;
		}

		public void Abort() => writer.Abort();

		public void Close() {
			if (closeCalled)
				return;
			closeCalled = true;
			writer.Close();
		}

		public void CloseMethod() => writer.CloseMethod();
		public void CloseNamespace() => writer.CloseNamespace();
		public void CloseScope(int endOffset) => writer.CloseScope((uint)endOffset);

		public void DefineAsyncStepInfo(uint[] yieldOffsets, uint[] breakpointOffset, uint[] breakpointMethod) {
			if (asyncMethodWriter == null)
				throw new InvalidOperationException();
			if (yieldOffsets.Length != breakpointOffset.Length || yieldOffsets.Length != breakpointMethod.Length)
				throw new ArgumentException();
			asyncMethodWriter.DefineAsyncStepInfo((uint)yieldOffsets.Length, yieldOffsets, breakpointOffset, breakpointMethod);
		}

		public void DefineCatchHandlerILOffset(uint catchHandlerOffset) {
			if (asyncMethodWriter == null)
				throw new InvalidOperationException();
			asyncMethodWriter.DefineCatchHandlerILOffset(catchHandlerOffset);
		}

		public void DefineConstant(string name, object value, byte[] signature) => writer.DefineConstant(name, value, (uint)signature.Length, signature);
		public void DefineConstant2(string name, object value, uint sigToken) => writer.DefineConstant2(name, value, sigToken);

		public ISymbolDocumentWriter DefineDocument(string url, Guid language, Guid languageVendor, Guid documentType) {
			writer.DefineDocument(url, ref language, ref languageVendor, ref documentType, out var unDocWriter);
			return unDocWriter == null ? null : new SymbolDocumentWriter(unDocWriter);
		}

		public void DefineField(SymbolToken parent, string name, System.Reflection.FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3) =>
			writer.DefineField((uint)parent.GetToken(), name, (uint)attributes, (uint)signature.Length, signature, (uint)addrKind, (uint)addr1, (uint)addr2, (uint)addr3);

		public void DefineGlobalVariable(string name, System.Reflection.FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3) =>
			writer.DefineGlobalVariable(name, (uint)attributes, (uint)signature.Length, signature, (uint)addrKind, (uint)addr1, (uint)addr2, (uint)addr3);

		public void DefineGlobalVariable2(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3) =>
			writer.DefineGlobalVariable2(name, attributes, sigToken, addrKind, addr1, addr2, addr3);

		public void DefineKickoffMethod(uint kickoffMethod) {
			if (asyncMethodWriter == null)
				throw new InvalidOperationException();
			asyncMethodWriter.DefineKickoffMethod(kickoffMethod);
		}

		public void DefineLocalVariable(string name, System.Reflection.FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset) =>
			writer.DefineLocalVariable(name, (uint)attributes, (uint)signature.Length, signature, (uint)addrKind, (uint)addr1, (uint)addr2, (uint)addr3, (uint)startOffset, (uint)endOffset);

		public void DefineParameter(string name, ParameterAttributes attributes, int sequence, SymAddressKind addrKind, int addr1, int addr2, int addr3) =>
			writer.DefineParameter(name, (uint)attributes, (uint)sequence, (uint)addrKind, (uint)addr1, (uint)addr2, (uint)addr3);

		public void DefineSequencePoints(ISymbolDocumentWriter document, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns) {
			var doc = document as SymbolDocumentWriter;
			if (doc == null)
				throw new ArgumentException("document isn't a non-null SymbolDocumentWriter instance");
			if (offsets == null || lines == null || columns == null ||
				endLines == null || endColumns == null ||
				offsets.Length != lines.Length ||
				offsets.Length != columns.Length ||
				offsets.Length != endLines.Length ||
				offsets.Length != endColumns.Length)
				throw new ArgumentException("Invalid arrays");
			writer.DefineSequencePoints(doc.SymUnmanagedDocumentWriter, (uint)offsets.Length, offsets, lines, columns, endLines, endColumns);
		}

		public void DefineSequencePoints(ISymbolDocumentWriter document, uint arraySize, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns) {
			var doc = document as SymbolDocumentWriter;
			if (doc == null)
				throw new ArgumentException("document isn't a non-null SymbolDocumentWriter instance");
			writer.DefineSequencePoints(doc.SymUnmanagedDocumentWriter, arraySize, offsets, lines, columns, endLines, endColumns);
		}

		public void Initialize(IntPtr emitter, string filename, bool fFullBuild) => writer.Initialize(emitter, filename, null, fFullBuild);
		public void OpenMethod(SymbolToken method) => writer.OpenMethod((uint)method.GetToken());
		public void OpenNamespace(string name) => writer.OpenNamespace(name);

		public int OpenScope(int startOffset) {
			writer.OpenScope((uint)startOffset, out uint result);
			return (int)result;
		}

		public void RemapToken(uint oldToken, uint newToken) => writer.RemapToken(oldToken, newToken);

		public void SetMethodSourceRange(ISymbolDocumentWriter startDoc, int startLine, int startColumn, ISymbolDocumentWriter endDoc, int endLine, int endColumn) {
			var sdoc = startDoc as SymbolDocumentWriter;
			if (sdoc == null)
				throw new ArgumentException("startDoc isn't a non-null SymbolDocumentWriter instance");
			var edoc = endDoc as SymbolDocumentWriter;
			if (edoc == null)
				throw new ArgumentException("endDoc isn't a non-null SymbolDocumentWriter instance");
			writer.SetMethodSourceRange(sdoc.SymUnmanagedDocumentWriter, (uint)startLine, (uint)startColumn, edoc.SymUnmanagedDocumentWriter, (uint)endLine, (uint)endColumn);
		}

		public void SetScopeRange(int scopeID, int startOffset, int endOffset) => writer.SetScopeRange((uint)scopeID, (uint)startOffset, (uint)endOffset);
		public void SetSymAttribute(SymbolToken parent, string name, byte[] data) => writer.SetSymAttribute((uint)parent.GetToken(), name, (uint)data.Length, data);
		public void SetUnderlyingWriter(IntPtr underlyingWriter) => throw new NotSupportedException();
		public void SetUserEntryPoint(SymbolToken entryMethod) => writer.SetUserEntryPoint((uint)entryMethod.GetToken());
		public void UsingNamespace(string fullName) => writer.UsingNamespace(fullName);

		public unsafe bool GetDebugInfo(ChecksumAlgorithm pdbChecksumAlgorithm, ref uint pdbAge, out Guid guid, out uint stamp, out IMAGE_DEBUG_DIRECTORY pIDD, out byte[] codeViewData) {
			pIDD = default;
			codeViewData = null;

			if (isDeterministic) {
				((ISymUnmanagedWriter3)writer).Commit();
				pdbStream.Position = 0;
				var checksumBytes = Hasher.Hash(pdbChecksumAlgorithm, pdbStream, pdbStream.Length);
				if (writer is ISymUnmanagedWriter8 writer8) {
					RoslynContentIdProvider.GetContentId(checksumBytes, out guid, out stamp);
					writer8.UpdateSignature(guid, stamp, pdbAge);
					return true;
				}
				else {
					var writer7 = (ISymUnmanagedWriter7)writer;
					fixed (byte* p = checksumBytes)
						writer7.UpdateSignatureByHashingContent(new IntPtr(p), (uint)checksumBytes.Length);
				}
			}

			writer.GetDebugInfo(out pIDD, 0, out uint size, null);
			codeViewData = new byte[size];
			writer.GetDebugInfo(out pIDD, size, out size, codeViewData);

			if (writer is IPdbWriter comPdbWriter) {
				var guidBytes = new byte[16];
				Array.Copy(codeViewData, 4, guidBytes, 0, 16);
				guid = new Guid(guidBytes);
				comPdbWriter.GetSignatureAge(out stamp, out uint age);
				Debug.Assert(age == pdbAge);
				pdbAge = age;
				return true;
			}

			Debug.Fail($"COM PDB writer doesn't impl {nameof(IPdbWriter)}");
			guid = default;
			stamp = 0;
			return false;
		}

		public void DefineLocalVariable2(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3, uint startOffset, uint endOffset) =>
			writer.DefineLocalVariable2(name, attributes, sigToken, addrKind, addr1, addr2, addr3, startOffset, endOffset);

		public void Initialize(Metadata metadata) {
			if (isDeterministic)
				((ISymUnmanagedWriter6)writer).InitializeDeterministic(new MDEmitter(metadata), new StreamIStream(pdbStream));
			else
				writer.Initialize(new MDEmitter(metadata), pdbFileName, new StreamIStream(pdbStream), true);
		}

		public void Dispose() {
			Marshal.FinalReleaseComObject(writer);
			if (ownsStream)
				pdbStream.Dispose();
		}
	}
}
