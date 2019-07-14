// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Runtime.InteropServices;
using dnlib.DotNet.Pdb.WindowsPdb;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolWriterImpl : SymbolWriter {
		readonly ISymUnmanagedWriter2 writer;
		readonly ISymUnmanagedAsyncMethodPropertiesWriter asyncMethodWriter;
		readonly string pdbFileName;
		readonly Stream pdbStream;
		readonly bool ownsStream;
		readonly bool isDeterministic;
		bool closeCalled;

		public override bool IsDeterministic => isDeterministic;
		public override bool SupportsAsyncMethods => !(asyncMethodWriter is null);

		public SymbolWriterImpl(ISymUnmanagedWriter2 writer, string pdbFileName, Stream pdbStream, PdbWriterOptions options, bool ownsStream) {
			this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
			asyncMethodWriter = writer as ISymUnmanagedAsyncMethodPropertiesWriter;
			this.pdbStream = pdbStream ?? throw new ArgumentNullException(nameof(pdbStream));
			this.pdbFileName = pdbFileName;
			this.ownsStream = ownsStream;
			isDeterministic = (options & PdbWriterOptions.Deterministic) != 0 && writer is ISymUnmanagedWriter6;
		}

		public override void Close() {
			if (closeCalled)
				return;
			closeCalled = true;
			writer.Close();
		}

		public override void CloseMethod() => writer.CloseMethod();
		public override void CloseScope(int endOffset) => writer.CloseScope((uint)endOffset);

		public override void DefineAsyncStepInfo(uint[] yieldOffsets, uint[] breakpointOffset, uint[] breakpointMethod) {
			if (asyncMethodWriter is null)
				throw new InvalidOperationException();
			if (yieldOffsets.Length != breakpointOffset.Length || yieldOffsets.Length != breakpointMethod.Length)
				throw new ArgumentException();
			asyncMethodWriter.DefineAsyncStepInfo((uint)yieldOffsets.Length, yieldOffsets, breakpointOffset, breakpointMethod);
		}

		public override void DefineCatchHandlerILOffset(uint catchHandlerOffset) {
			if (asyncMethodWriter is null)
				throw new InvalidOperationException();
			asyncMethodWriter.DefineCatchHandlerILOffset(catchHandlerOffset);
		}

		public override void DefineConstant(string name, object value, uint sigToken) => writer.DefineConstant2(name, value, sigToken);

		public override ISymbolDocumentWriter DefineDocument(string url, Guid language, Guid languageVendor, Guid documentType) {
			writer.DefineDocument(url, ref language, ref languageVendor, ref documentType, out var unDocWriter);
			return unDocWriter is null ? null : new SymbolDocumentWriter(unDocWriter);
		}

		public override void DefineKickoffMethod(uint kickoffMethod) {
			if (asyncMethodWriter is null)
				throw new InvalidOperationException();
			asyncMethodWriter.DefineKickoffMethod(kickoffMethod);
		}

		public override void DefineSequencePoints(ISymbolDocumentWriter document, uint arraySize, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns) {
			var doc = document as SymbolDocumentWriter;
			if (doc is null)
				throw new ArgumentException("document isn't a non-null SymbolDocumentWriter instance");
			writer.DefineSequencePoints(doc.SymUnmanagedDocumentWriter, arraySize, offsets, lines, columns, endLines, endColumns);
		}

		public override void OpenMethod(MDToken method) => writer.OpenMethod(method.Raw);

		public override int OpenScope(int startOffset) {
			writer.OpenScope((uint)startOffset, out uint result);
			return (int)result;
		}

		public override void SetSymAttribute(MDToken parent, string name, byte[] data) => writer.SetSymAttribute(parent.Raw, name, (uint)data.Length, data);
		public override void SetUserEntryPoint(MDToken entryMethod) => writer.SetUserEntryPoint(entryMethod.Raw);
		public override void UsingNamespace(string fullName) => writer.UsingNamespace(fullName);

		public override unsafe bool GetDebugInfo(ChecksumAlgorithm pdbChecksumAlgorithm, ref uint pdbAge, out Guid guid, out uint stamp, out IMAGE_DEBUG_DIRECTORY pIDD, out byte[] codeViewData) {
			pIDD = default;
			codeViewData = null;

			if (isDeterministic) {
				((ISymUnmanagedWriter3)writer).Commit();
				var oldPos = pdbStream.Position;
				pdbStream.Position = 0;
				var checksumBytes = Hasher.Hash(pdbChecksumAlgorithm, pdbStream, pdbStream.Length);
				pdbStream.Position = oldPos;
				if (writer is ISymUnmanagedWriter8 writer8) {
					RoslynContentIdProvider.GetContentId(checksumBytes, out guid, out stamp);
					writer8.UpdateSignature(guid, stamp, pdbAge);
					return true;
				}
				else if (writer is ISymUnmanagedWriter7 writer7) {
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

		public override void DefineLocalVariable(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3, uint startOffset, uint endOffset) =>
			writer.DefineLocalVariable2(name, attributes, sigToken, addrKind, addr1, addr2, addr3, startOffset, endOffset);

		public override void Initialize(Metadata metadata) {
			if (isDeterministic)
				((ISymUnmanagedWriter6)writer).InitializeDeterministic(new MDEmitter(metadata), new StreamIStream(pdbStream));
			else
				writer.Initialize(new MDEmitter(metadata), pdbFileName, new StreamIStream(pdbStream), true);
		}

		public override unsafe void SetSourceServerData(byte[] data) {
			if (data is null)
				return;
			if (writer is ISymUnmanagedWriter8 writer8) {
				fixed (void* p = data)
					writer8.SetSourceServerData(new IntPtr(p), (uint)data.Length);
			}
		}

		public override unsafe void SetSourceLinkData(byte[] data) {
			if (data is null)
				return;
			if (writer is ISymUnmanagedWriter8 writer8) {
				fixed (void* p = data)
					writer8.SetSourceLinkData(new IntPtr(p), (uint)data.Length);
			}
		}

		public override void Dispose() {
			Marshal.FinalReleaseComObject(writer);
			if (ownsStream)
				pdbStream.Dispose();
		}
	}
}
