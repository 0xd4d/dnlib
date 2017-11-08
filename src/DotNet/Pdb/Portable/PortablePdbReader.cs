// dnlib: See LICENSE.txt for more info

// https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Portable {
	sealed class PortablePdbReader : SymbolReader {
		readonly PdbFileKind pdbFileKind;
		ModuleDef module;
		readonly IMetaData pdbMetaData;
		SymbolDocument[] documents;

		public override PdbFileKind PdbFileKind {
			get { return pdbFileKind; }
		}

		public override int UserEntryPoint {
			get { return pdbMetaData.PdbStream.EntryPoint.ToInt32(); }
		}

		public override IList<SymbolDocument> Documents {
			get { return documents; }
		}

		public PortablePdbReader(IImageStream pdbStream, PdbFileKind pdbFileKind) {
			this.pdbFileKind = pdbFileKind;
			pdbMetaData = MetaDataCreator.CreateStandalonePortablePDB(pdbStream, true);
		}

		public override void Initialize(ModuleDef module) {
			this.module = module;
			documents = ReadDocuments();
		}

		static Guid GetLanguageVendor(Guid language) {
			if (language == PdbDocumentConstants.LanguageCSharp || language == PdbDocumentConstants.LanguageVisualBasic || language == PdbDocumentConstants.LanguageFSharp)
				return PdbDocumentConstants.LanguageVendorMicrosoft;
			return Guid.Empty;
		}

		SymbolDocument[] ReadDocuments() {
			Debug.Assert(module != null);
			var docTbl = pdbMetaData.TablesStream.DocumentTable;
			var docs = new SymbolDocument[docTbl.Rows];
			var nameReader = new DocumentNameReader(pdbMetaData.BlobStream);
			var custInfos = ListCache<PdbCustomDebugInfo>.AllocList();
			var gpContext = new GenericParamContext();
			for (int i = 0; i < docs.Length; i++) {
				uint nameOffset, hashAlgorithmIndex, hashOffset;
				uint languageIndex = pdbMetaData.TablesStream.ReadDocumentRow2((uint)i + 1, out nameOffset, out hashAlgorithmIndex, out hashOffset);
				var url = nameReader.ReadDocumentName(nameOffset);
				var language = pdbMetaData.GuidStream.Read(languageIndex) ?? Guid.Empty;
				var languageVendor = GetLanguageVendor(language);
				var documentType = PdbDocumentConstants.DocumentTypeText;
				var checkSumAlgorithmId = pdbMetaData.GuidStream.Read(hashAlgorithmIndex) ?? Guid.Empty;
				var checkSum = pdbMetaData.BlobStream.ReadNoNull(hashOffset);

				var token = new MDToken(Table.Document, i + 1).ToInt32();
				custInfos.Clear();
				GetCustomDebugInfos(token, gpContext, custInfos);
				var custInfosArray = custInfos.Count == 0 ? emptyPdbCustomDebugInfos : custInfos.ToArray();

				docs[i] = new SymbolDocumentImpl(url, language, languageVendor, documentType, checkSumAlgorithmId, checkSum, custInfosArray);
			}
			ListCache<PdbCustomDebugInfo>.Free(ref custInfos);
			return docs;
		}
		static readonly PdbCustomDebugInfo[] emptyPdbCustomDebugInfos = new PdbCustomDebugInfo[0];

		bool TryGetSymbolDocument(uint rid, out SymbolDocument document) {
			int index = (int)rid - 1;
			if ((uint)index >= (uint)documents.Length) {
				Debug.Fail("Couldn't find document with rid 0x" + rid.ToString("X6"));
				document = null;
				return false;
			}
			document = documents[index];
			return true;
		}

		public override SymbolMethod GetMethod(MethodDef method, int version) {
			var mdTable = pdbMetaData.TablesStream.MethodDebugInformationTable;
			uint methodRid = method.Rid;
			if (!mdTable.IsValidRID(methodRid))
				return null;

			var sequencePoints = ReadSequencePoints(methodRid) ?? emptySymbolSequencePoints;
			var gpContext = GenericParamContext.Create(method);
			var rootScope = ReadScope(methodRid, gpContext);

			var kickoffMethod = GetKickoffMethod(methodRid);
			var symbolMethod = new SymbolMethodImpl(this, method.MDToken.ToInt32(), rootScope, sequencePoints, kickoffMethod);
			rootScope.method = symbolMethod;
			return symbolMethod;
		}
		static readonly SymbolAsyncStepInfo[] emptySymbolAsyncStepInfos = new SymbolAsyncStepInfo[0];

		int GetKickoffMethod(uint methodRid) {
			uint rid = pdbMetaData.GetStateMachineMethodRid(methodRid);
			if (rid == 0)
				return 0;
			if (!pdbMetaData.TablesStream.StateMachineMethodTable.IsValidRID(rid))
				return 0;
			return 0x06000000 + (int)pdbMetaData.TablesStream.ReadStateMachineMethodRow2(rid);
		}

		SymbolSequencePoint[] ReadSequencePoints(uint methodRid) {
			if (!pdbMetaData.TablesStream.MethodDebugInformationTable.IsValidRID(methodRid))
				return null;
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

							symSeqPoint.Line = SequencePointConstants.HIDDEN_LINE;
							symSeqPoint.EndLine = SequencePointConstants.HIDDEN_LINE;
							symSeqPoint.Column = SequencePointConstants.HIDDEN_COLUMN;
							symSeqPoint.EndColumn = SequencePointConstants.HIDDEN_COLUMN;
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

			return ListCache<SymbolSequencePoint>.FreeAndToArray(ref seqPointsBuilder);
		}
		static readonly SymbolSequencePoint[] emptySymbolSequencePoints = new SymbolSequencePoint[0];

		SymbolScopeImpl ReadScope(uint methodRid, GenericParamContext gpContext) {
			var scopesRidList = pdbMetaData.GetLocalScopeRidList(methodRid);
			SymbolScopeImpl rootScopeOrNull = null;
			if (scopesRidList.Count != 0) {
				var custInfos = ListCache<PdbCustomDebugInfo>.AllocList();
				var stack = ListCache<SymbolScopeImpl>.AllocList();
				var importScopeBlobReader = new ImportScopeBlobReader(module, pdbMetaData.BlobStream);
				for (int i = 0; i < scopesRidList.Count; i++) {
					var rid = scopesRidList[i];
					uint importScope, variableList, constantList, startOffset;
					int token = new MDToken(Table.LocalScope, rid).ToInt32();
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
					custInfos.Clear();
					GetCustomDebugInfos(token, gpContext, custInfos);
					var customDebugInfos = custInfos.Count == 0 ? emptyPdbCustomDebugInfos : custInfos.ToArray();
					var scope = new SymbolScopeImpl(this, parent, (int)startOffset, (int)endOffset, customDebugInfos);
					if (rootScopeOrNull == null)
						rootScopeOrNull = scope;
					stack.Add(scope);
					if (parent != null)
						parent.childrenList.Add(scope);

					scope.importScope = ReadPdbImportScope(ref importScopeBlobReader, importScope, gpContext);
					uint variableListEnd, constantListEnd;
					GetEndOfLists(rid, out variableListEnd, out constantListEnd);
					ReadVariables(scope, gpContext, variableList, variableListEnd);
					ReadConstants(scope, constantList, constantListEnd);
				}

				ListCache<SymbolScopeImpl>.Free(ref stack);
				ListCache<PdbCustomDebugInfo>.Free(ref custInfos);
			}
			return rootScopeOrNull ?? new SymbolScopeImpl(this, null, 0, int.MaxValue, emptyPdbCustomDebugInfos);
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

		PdbImportScope ReadPdbImportScope(ref ImportScopeBlobReader importScopeBlobReader, uint importScope, GenericParamContext gpContext) {
			if (importScope == 0)
				return null;
			const int MAX = 1000;
			PdbImportScope result = null;
			PdbImportScope prevScope = null;
			for (int i = 0; importScope != 0; i++) {
				Debug.Assert(i < MAX);
				if (i >= MAX)
					return null;
				int token = new MDToken(Table.ImportScope, importScope).ToInt32();
				if (!pdbMetaData.TablesStream.ImportScopeTable.IsValidRID(importScope))
					return null;
				uint imports = pdbMetaData.TablesStream.ReadImportScopeRow2(importScope, out importScope);
				var scope = new PdbImportScope();
				GetCustomDebugInfos(token, gpContext, scope.CustomDebugInfos);
				if (result == null)
					result = scope;
				if (prevScope != null)
					prevScope.Parent = scope;
				importScopeBlobReader.Read(imports, scope.Imports);
				prevScope = scope;
			}

			return result;
		}

		void ReadVariables(SymbolScopeImpl scope, GenericParamContext gpContext, uint variableList, uint variableListEnd) {
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
			var custInfos = ListCache<PdbCustomDebugInfo>.AllocList();
			for (uint rid = variableList; rid < variableListEnd; rid++) {
				int token = new MDToken(Table.LocalVariable, rid).ToInt32();
				custInfos.Clear();
				GetCustomDebugInfos(token, gpContext, custInfos);
				var customDebugInfos = custInfos.Count == 0 ? emptyPdbCustomDebugInfos : custInfos.ToArray();
				ushort attributes, index;
				var nameOffset = pdbMetaData.TablesStream.ReadLocalVariableRow2(rid, out attributes, out index);
				var name = pdbMetaData.StringsStream.Read(nameOffset);
				scope.localsList.Add(new SymbolVariableImpl(name, ToSymbolVariableAttributes(attributes), index, customDebugInfos));
			}
			ListCache<PdbCustomDebugInfo>.Free(ref custInfos);
		}

		static PdbLocalAttributes ToSymbolVariableAttributes(ushort attributes) {
			var res = PdbLocalAttributes.None;
			const ushort DebuggerHidden = 0x0001;
			if ((attributes & DebuggerHidden) != 0)
				res |= PdbLocalAttributes.DebuggerHidden;
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

		internal void GetCustomDebugInfos(SymbolMethodImpl symMethod, MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result) {
			Debug.Assert(method.Module == module);
			PdbAsyncMethodSteppingInformationCustomDebugInfo asyncStepInfo;
			GetCustomDebugInfos(method.MDToken.ToInt32(), GenericParamContext.Create(method), result, method, body, out asyncStepInfo);
			if (asyncStepInfo != null) {
				var asyncMethod = TryCreateAsyncMethod(module, symMethod.KickoffMethod, asyncStepInfo.AsyncStepInfos, asyncStepInfo.CatchHandler);
				Debug.Assert(asyncMethod != null);
				if (asyncMethod != null)
					result.Add(asyncMethod);
			}
			else if (symMethod.KickoffMethod != 0) {
				var iteratorMethod = TryCreateIteratorMethod(module, symMethod.KickoffMethod);
				Debug.Assert(iteratorMethod != null);
				if (iteratorMethod != null)
					result.Add(iteratorMethod);
			}
		}

		PdbAsyncMethodCustomDebugInfo TryCreateAsyncMethod(ModuleDef module, int asyncKickoffMethod, IList<PdbAsyncStepInfo> asyncStepInfos, Instruction asyncCatchHandler) {
			var kickoffToken = new MDToken(asyncKickoffMethod);
			if (kickoffToken.Table != Table.Method)
				return null;

			var asyncMethod = new PdbAsyncMethodCustomDebugInfo(asyncStepInfos.Count);
			asyncMethod.KickoffMethod = module.ResolveToken(kickoffToken) as MethodDef;
			asyncMethod.CatchHandlerInstruction = asyncCatchHandler;
			foreach (var info in asyncStepInfos)
				asyncMethod.StepInfos.Add(info);
			return asyncMethod;
		}

		PdbIteratorMethodCustomDebugInfo TryCreateIteratorMethod(ModuleDef module, int iteratorKickoffMethod) {
			var kickoffToken = new MDToken(iteratorKickoffMethod);
			if (kickoffToken.Table != Table.Method)
				return null;
			var kickoffMethod = module.ResolveToken(kickoffToken) as MethodDef;
			return new PdbIteratorMethodCustomDebugInfo(kickoffMethod);
		}

		public override void GetCustomDebugInfos(int token, GenericParamContext gpContext, IList<PdbCustomDebugInfo> result) {
			PdbAsyncMethodSteppingInformationCustomDebugInfo asyncStepInfo;
			GetCustomDebugInfos(token, gpContext, result, null, null, out asyncStepInfo);
			Debug.Assert(asyncStepInfo == null);
		}

		void GetCustomDebugInfos(int token, GenericParamContext gpContext, IList<PdbCustomDebugInfo> result, MethodDef methodOpt, CilBody bodyOpt, out PdbAsyncMethodSteppingInformationCustomDebugInfo asyncStepInfo) {
			asyncStepInfo = null;
			var mdToken = new MDToken(token);
			var ridList = pdbMetaData.GetCustomDebugInformationRidList(mdToken.Table, mdToken.Rid);
			if (ridList.Count == 0)
				return;
			var typeOpt = methodOpt == null ? null : methodOpt.DeclaringType;
			for (int i = 0; i < ridList.Count; i++) {
				var rid = ridList[i];
				uint kind;
				uint value = pdbMetaData.TablesStream.ReadCustomDebugInformationRow2(rid, out kind);
				var guid = pdbMetaData.GuidStream.Read(kind);
				var data = pdbMetaData.BlobStream.Read(value);
				Debug.Assert(guid != null && data != null);
				if (guid == null || data == null)
					continue;
				var cdi = PortablePdbCustomDebugInfoReader.Read(module, typeOpt, bodyOpt, gpContext, guid.Value, data);
				Debug.Assert(cdi != null);
				if (cdi != null) {
					var asyncStepInfoTmp = cdi as PdbAsyncMethodSteppingInformationCustomDebugInfo;
					if (asyncStepInfoTmp != null) {
						Debug.Assert(asyncStepInfo == null);
						asyncStepInfo = asyncStepInfoTmp;
					}
					else
						result.Add(cdi);
				}
			}
		}

		public override void Dispose() {
			pdbMetaData.Dispose();
		}
	}
}
