// dnlib: See LICENSE.txt for more info

// https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Portable {
	sealed class PortablePdbReader : SymbolReader {
		readonly IMetaData moduleMetaData;
		readonly IMetaData pdbMetaData;
		readonly ReadOnlyCollection<SymbolDocument> documents;

		readonly Guid pdbId;
		readonly uint timestamp;
		readonly uint entryPointToken;

		public override int UserEntryPoint {
			get { return (int)entryPointToken; }
		}

		public override ReadOnlyCollection<SymbolDocument> Documents {
			get { return documents; }
		}

		public PortablePdbReader(IMetaData moduleMetaData, IImageStream pdbStream) {
			this.moduleMetaData = moduleMetaData;
			pdbMetaData = MetaDataCreator.Create(pdbStream, true);
			var pdbHeap = GetPdbStream(pdbMetaData.AllStreams);
			Debug.Assert(pdbHeap != null);
			if (pdbHeap != null) {
				using (var stream = pdbHeap.GetClonedImageStream()) {
					pdbId = new Guid(stream.ReadBytes(16));
					timestamp = stream.ReadUInt32();
					entryPointToken = stream.ReadUInt32();
				}
			}

			documents = ReadDocuments();
		}

		static DotNetStream GetPdbStream(IList<DotNetStream> streams) {
			foreach (var stream in streams) {
				if (stream.Name == "#Pdb")
					return stream;
			}
			return null;
		}

		static Guid GetLanguageVendor(Guid language) {
			if (language == Constants.LanguageCSharp || language == Constants.LanguageVisualBasic || language == Constants.LanguageFSharp)
				return Constants.LanguageVendorMicrosoft;
			return Guid.Empty;
		}

		ReadOnlyCollection<SymbolDocument> ReadDocuments() {
			var docTbl = pdbMetaData.TablesStream.DocumentTable;
			var docs = new SymbolDocument[docTbl.Rows];
			var nameReader = new DocumentNameReader(pdbMetaData.BlobStream);
			for (int i = 0; i < docs.Length; i++) {
				uint nameOffset, hashAlgorithmIndex, hashOffset;
				uint languageIndex = pdbMetaData.TablesStream.ReadDocumentRow2((uint)i + 1, out nameOffset, out hashAlgorithmIndex, out hashOffset);
				var url = nameReader.ReadDocumentName(nameOffset);
				var language = pdbMetaData.GuidStream.Read(languageIndex) ?? Guid.Empty;
				var languageVendor = GetLanguageVendor(language);
				var documentType = Constants.DocumentTypeText;
				var checkSumAlgorithmId = pdbMetaData.GuidStream.Read(hashAlgorithmIndex) ?? Guid.Empty;
				var checkSum = pdbMetaData.BlobStream.ReadNoNull(hashOffset);
				docs[i] = new SymbolDocumentImpl(url, language, languageVendor, documentType, checkSumAlgorithmId, checkSum);
			}
			return new ReadOnlyCollection<SymbolDocument>(docs);
		}

		bool TryGetSymbolDocument(uint rid, out SymbolDocument document) {
			int index = (int)rid - 1;
			if ((uint)index >= (uint)documents.Count) {
				Debug.Fail("Couldn't find document with rid 0x" + rid.ToString("X6"));
				document = null;
				return false;
			}
			document = documents[index];
			return true;
		}

		public override SymbolMethod GetMethod(ModuleDef module, MethodDef method, int version) {
			var mdTable = pdbMetaData.TablesStream.MethodDebugInformationTable;
			uint methodRid = method.Rid;
			if (!mdTable.IsValidRID(methodRid))
				return null;

			var sequencePoints = ReadSequencePoints(methodRid) ?? emptySymbolSequencePoints;
			var rootScope = ReadScope(module, methodRid);

			var kickoffMethod = GetKickoffMethod(methodRid);
			bool isAsyncMethod = kickoffMethod != 0 && IsAsyncMethod(method);
			bool isIteratorMethod = kickoffMethod != 0 && !isAsyncMethod;
			int iteratorKickoffMethod = isIteratorMethod ? kickoffMethod : 0;
			int asyncKickoffMethod = isAsyncMethod ? kickoffMethod : 0;
			uint? asyncCatchHandlerILOffset = null;
			var asyncStepInfos = emptySymbolAsyncStepInfos;
			var symbolMethod = new SymbolMethodImpl(method.MDToken.ToInt32(), rootScope, sequencePoints, iteratorKickoffMethod, asyncKickoffMethod, asyncCatchHandlerILOffset, asyncStepInfos);
			rootScope.method = symbolMethod;
			return symbolMethod;
		}
		static readonly ReadOnlyCollection<SymbolAsyncStepInfo> emptySymbolAsyncStepInfos = new ReadOnlyCollection<SymbolAsyncStepInfo>(new SymbolAsyncStepInfo[0]);

		static bool IsAsyncMethod(MethodDef method) {
			foreach (var iface in method.DeclaringType.Interfaces) {
				if (iface.Interface.Name != stringIAsyncStateMachine)
					continue;
				if (iface.Interface.FullName == "System.Runtime.CompilerServices.IAsyncStateMachine")
					return true;
			}
			return false;
		}
		static readonly UTF8String stringIAsyncStateMachine = new UTF8String("IAsyncStateMachine");

		int GetKickoffMethod(uint methodRid) {
			uint rid = pdbMetaData.GetStateMachineMethodRid(methodRid);
			if (rid == 0)
				return 0;
			return 0x06000000 + (int)pdbMetaData.TablesStream.ReadStateMachineMethodRow2(rid);
		}

		ReadOnlyCollection<SymbolSequencePoint> ReadSequencePoints(uint methodRid) {
			uint documentRid;
			uint sequencePointsOffset = pdbMetaData.TablesStream.ReadMethodDebugInformationRow2(methodRid, out documentRid);
			if (sequencePointsOffset == 0)
				return null;

			var seqPointsBuilder = ListCache<SymbolSequencePoint>.AllocList();
			using (var seqPointsStream = pdbMetaData.BlobStream.CreateStream(sequencePointsOffset)) {
				uint localSig = seqPointsStream.ReadCompressedUInt32();
				if (documentRid == 0)
					documentRid = seqPointsStream.ReadCompressedUInt32();

				SymbolDocument document;
				TryGetSymbolDocument(documentRid, out document);

				uint ilOffset = uint.MaxValue;
				int line = -1, column = 0;
				bool canReadDocumentRecord = false;
				while (seqPointsStream.Position < seqPointsStream.Length) {
					uint data = seqPointsStream.ReadCompressedUInt32();
					if (data == 0 && canReadDocumentRecord) {
						// document-record

						documentRid = seqPointsStream.ReadCompressedUInt32();
						TryGetSymbolDocument(documentRid, out document);
					}
					else {
						// SequencePointRecord

						Debug.Assert(document != null);
						if (document == null)
							return null;

						var symSeqPoint = new SymbolSequencePoint {
							Document = document,
						};

						if (ilOffset == uint.MaxValue)
							ilOffset = data;
						else {
							Debug.Assert(data != 0);
							if (data == 0)
								return null;
							ilOffset += data;
						}
						symSeqPoint.Offset = (int)ilOffset;

						uint dlines = seqPointsStream.ReadCompressedUInt32();
						int dcolumns = dlines == 0 ? (int)seqPointsStream.ReadCompressedUInt32() : seqPointsStream.ReadCompressedInt32();

						if (dlines == 0 && dcolumns == 0) {
							// hidden-sequence-point-record

							const int HIDDEN_LINE = 0xFEEFEE;
							const int HIDDEN_COLUMN = 0;
							symSeqPoint.Line = HIDDEN_LINE;
							symSeqPoint.EndLine = HIDDEN_LINE;
							symSeqPoint.Column = HIDDEN_COLUMN;
							symSeqPoint.EndColumn = HIDDEN_COLUMN;
						}
						else {
							// sequence-point-record

							if (line < 0) {
								line = (int)seqPointsStream.ReadCompressedUInt32();
								column = (int)seqPointsStream.ReadCompressedUInt32();
							}
							else {
								line += seqPointsStream.ReadCompressedInt32();
								column += seqPointsStream.ReadCompressedInt32();
							}

							symSeqPoint.Line = line;
							symSeqPoint.EndLine = line + (int)dlines;
							symSeqPoint.Column = column;
							symSeqPoint.EndColumn = column + dcolumns;
						}

						seqPointsBuilder.Add(symSeqPoint);
					}

					canReadDocumentRecord = true;
				}
				Debug.Assert(seqPointsStream.Position == seqPointsStream.Length);
			}

			return new ReadOnlyCollection<SymbolSequencePoint>(ListCache<SymbolSequencePoint>.FreeAndToArray(ref seqPointsBuilder));
		}
		static readonly ReadOnlyCollection<SymbolSequencePoint> emptySymbolSequencePoints = new ReadOnlyCollection<SymbolSequencePoint>(new SymbolSequencePoint[0]);

		SymbolScopeImpl ReadScope(ModuleDef module, uint methodRid) {
			var scopesRidList = pdbMetaData.GetLocalScopeRidList(methodRid);
			SymbolScopeImpl rootScopeOrNull = null;
			if (scopesRidList.Count != 0) {
				var stack = ListCache<SymbolScopeImpl>.AllocList();
				var importScopeBlobReader = new ImportScopeBlobReader(module, pdbMetaData.BlobStream);
				for (int i = 0; i < scopesRidList.Count; i++) {
					var rid = scopesRidList[i];
					uint importScope, variableList, constantList, startOffset;
					uint length = pdbMetaData.TablesStream.ReadLocalScopeRow2(rid, out importScope, out variableList, out constantList, out startOffset);
					uint endOffset = startOffset + length;

					SymbolScopeImpl parent = null;
					while (stack.Count > 0) {
						var nextParent = stack[stack.Count - 1];
						if (startOffset >= nextParent.StartOffset && endOffset <= nextParent.EndOffset) {
							parent = nextParent;
							break;
						}
						stack.RemoveAt(stack.Count - 1);
					}

					Debug.Assert(parent != null || rootScopeOrNull == null);
					var scope = new SymbolScopeImpl(parent, (int)startOffset, (int)endOffset);
					if (rootScopeOrNull == null)
						rootScopeOrNull = scope;
					stack.Add(scope);
					if (parent != null)
						parent.childrenList.Add(scope);

					scope.importScope = ReadPdbImportScope(ref importScopeBlobReader, importScope);
					uint variableListEnd, constantListEnd;
					GetEndOfLists(rid, out variableListEnd, out constantListEnd);
					ReadVariables(scope, variableList, variableListEnd);
					ReadConstants(scope, constantList, constantListEnd);
				}

				ListCache<SymbolScopeImpl>.Free(ref stack);
			}
			return rootScopeOrNull ?? new SymbolScopeImpl(null, 0, int.MaxValue);
		}

		void GetEndOfLists(uint scopeRid, out uint variableListEnd, out uint constantListEnd) {
			var localScopeTable = pdbMetaData.TablesStream.LocalScopeTable;
			var nextRid = scopeRid + 1;
			if (!localScopeTable.IsValidRID(nextRid)) {
				variableListEnd = pdbMetaData.TablesStream.LocalVariableTable.Rows + 1;
				constantListEnd = pdbMetaData.TablesStream.LocalConstantTable.Rows + 1;
			}
			else {
				uint nextImportScope, nextVariableList, nextConstantList, nextStartOffset;
				pdbMetaData.TablesStream.ReadLocalScopeRow2(nextRid, out nextImportScope, out nextVariableList, out nextConstantList, out nextStartOffset);
				variableListEnd = nextVariableList;
				constantListEnd = nextConstantList;
			}
		}

		PdbImportScope ReadPdbImportScope(ref ImportScopeBlobReader importScopeBlobReader, uint importScope) {
			if (importScope == 0)
				return null;
			const int MAX = 1000;
			PdbImportScope result = null;
			PdbImportScope prevScope = null;
			for (int i = 0; importScope != 0; i++) {
				Debug.Assert(i < MAX);
				if (i >= MAX)
					return null;
				uint imports = pdbMetaData.TablesStream.ReadImportScopeRow2(importScope, out importScope);
				var scope = new PdbImportScope();
				if (result == null)
					result = scope;
				if (prevScope != null)
					prevScope.Parent = scope;
				importScopeBlobReader.Read(imports, scope.Imports);
				prevScope = scope;
			}

			return result;
		}

		void ReadVariables(SymbolScopeImpl scope, uint variableList, uint variableListEnd) {
			if (variableList == 0)
				return;
			Debug.Assert(variableList <= variableListEnd);
			if (variableList >= variableListEnd)
				return;
			var table = pdbMetaData.TablesStream.LocalVariableTable;
			Debug.Assert(table.IsValidRID(variableListEnd - 1));
			if (!table.IsValidRID(variableListEnd - 1))
				return;
			Debug.Assert(table.IsValidRID(variableList));
			if (!table.IsValidRID(variableList))
				return;
			for (uint rid = variableList; rid < variableListEnd; rid++) {
				ushort attributes, index;
				var nameOffset = pdbMetaData.TablesStream.ReadLocalVariableRow2(rid, out attributes, out index);
				var name = pdbMetaData.StringsStream.Read(nameOffset);
				scope.localsList.Add(new SymbolVariableImpl(name, ToSymbolVariableAttributes(attributes), index));
			}
		}

		static SymbolVariableAttributes ToSymbolVariableAttributes(ushort attributes) {
			var res = SymbolVariableAttributes.None;
			const ushort DebuggerHidden = 0x0001;
			if ((attributes & DebuggerHidden) != 0)
				res |= SymbolVariableAttributes.CompilerGenerated;
			return res;
		}

		void ReadConstants(SymbolScopeImpl scope, uint constantList, uint constantListEnd) {
			if (constantList == 0)
				return;
			Debug.Assert(constantList <= constantListEnd);
			if (constantList >= constantListEnd)
				return;
			var table = pdbMetaData.TablesStream.LocalConstantTable;
			Debug.Assert(table.IsValidRID(constantListEnd - 1));
			if (!table.IsValidRID(constantListEnd - 1))
				return;
			Debug.Assert(table.IsValidRID(constantList));
			if (!table.IsValidRID(constantList))
				return;
			scope.SetConstants(pdbMetaData, constantList, constantListEnd);
		}

		public override void GetCustomDebugInfo(MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result) {
			//TODO: CustomDebugInformation table
		}

		public override void Dispose() {
			pdbMetaData.Dispose();
		}
	}
}
