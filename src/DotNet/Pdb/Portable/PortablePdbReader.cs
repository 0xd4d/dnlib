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
		readonly Metadata pdbMetadata;
		SymbolDocument[] documents;

		public override PdbFileKind PdbFileKind => pdbFileKind;
		public override int UserEntryPoint => pdbMetadata.PdbStream.EntryPoint.ToInt32();
		public override IList<SymbolDocument> Documents => documents;

		public PortablePdbReader(DataReaderFactory pdbStream, PdbFileKind pdbFileKind) {
			this.pdbFileKind = pdbFileKind;
			pdbMetadata = MetadataFactory.CreateStandalonePortablePDB(pdbStream, true);
		}

		internal bool MatchesModule(Guid pdbGuid, uint timestamp, uint age) {
			if (pdbMetadata.PdbStream is PdbStream pdbStream) {
				var pdbGuidArray = pdbStream.Id;
				Array.Resize(ref pdbGuidArray, 16);
				if (new Guid(pdbGuidArray) != pdbGuid)
					return false;
				if (BitConverter.ToUInt32(pdbStream.Id, 16) != timestamp)
					return false;
				if (age != 1)
					return false;

				return true;
			}
			return false;
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
			Debug.Assert(module is not null);
			var docTbl = pdbMetadata.TablesStream.DocumentTable;
			var docs = new SymbolDocument[docTbl.Rows];
			var nameReader = new DocumentNameReader(pdbMetadata.BlobStream);
			var custInfos = ListCache<PdbCustomDebugInfo>.AllocList();
			var gpContext = new GenericParamContext();
			for (int i = 0; i < docs.Length; i++) {
				uint rid = (uint)i + 1;
				bool b = pdbMetadata.TablesStream.TryReadDocumentRow(rid, out var row);
				Debug.Assert(b);
				var url = nameReader.ReadDocumentName(row.Name);
				var language = pdbMetadata.GuidStream.Read(row.Language) ?? Guid.Empty;
				var languageVendor = GetLanguageVendor(language);
				var documentType = PdbDocumentConstants.DocumentTypeText;
				var checkSumAlgorithmId = pdbMetadata.GuidStream.Read(row.HashAlgorithm) ?? Guid.Empty;
				var checkSum = pdbMetadata.BlobStream.ReadNoNull(row.Hash);

				var mdToken = new MDToken(Table.Document, rid);
				var token = mdToken.ToInt32();
				custInfos.Clear();
				GetCustomDebugInfos(token, gpContext, custInfos);
				var custInfosArray = custInfos.Count == 0 ? Array2.Empty<PdbCustomDebugInfo>() : custInfos.ToArray();

				docs[i] = new SymbolDocumentImpl(url, language, languageVendor, documentType, checkSumAlgorithmId, checkSum, custInfosArray, mdToken);
			}
			ListCache<PdbCustomDebugInfo>.Free(ref custInfos);
			return docs;
		}

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
			if (version != 1)
				return null;
			var mdTable = pdbMetadata.TablesStream.MethodDebugInformationTable;
			uint methodRid = method.Rid;
			if (!mdTable.IsValidRID(methodRid))
				return null;

			var sequencePoints = ReadSequencePoints(methodRid) ?? Array2.Empty<SymbolSequencePoint>();
			var gpContext = GenericParamContext.Create(method);
			var rootScope = ReadScope(methodRid, gpContext);

			var kickoffMethod = GetKickoffMethod(methodRid);
			var symbolMethod = new SymbolMethodImpl(this, method.MDToken.ToInt32(), rootScope, sequencePoints, kickoffMethod);
			rootScope.method = symbolMethod;
			return symbolMethod;
		}

		int GetKickoffMethod(uint methodRid) {
			uint rid = pdbMetadata.GetStateMachineMethodRid(methodRid);
			if (rid == 0)
				return 0;
			if (!pdbMetadata.TablesStream.TryReadStateMachineMethodRow(rid, out var row))
				return 0;
			return 0x06000000 + (int)row.KickoffMethod;
		}

		SymbolSequencePoint[] ReadSequencePoints(uint methodRid) {
			if (!pdbMetadata.TablesStream.MethodDebugInformationTable.IsValidRID(methodRid))
				return null;
			if (!pdbMetadata.TablesStream.TryReadMethodDebugInformationRow(methodRid, out var row))
				return null;
			if (row.SequencePoints == 0)
				return null;
			uint documentRid = row.Document;

			if (!pdbMetadata.BlobStream.TryCreateReader(row.SequencePoints, out var seqPointsReader))
				return null;
			var seqPointsBuilder = ListCache<SymbolSequencePoint>.AllocList();
			uint localSig = seqPointsReader.ReadCompressedUInt32();
			if (documentRid == 0)
				documentRid = seqPointsReader.ReadCompressedUInt32();

			TryGetSymbolDocument(documentRid, out var document);

			uint ilOffset = uint.MaxValue;
			int line = -1, column = 0;
			bool canReadDocumentRecord = false;
			while (seqPointsReader.Position < seqPointsReader.Length) {
				uint data = seqPointsReader.ReadCompressedUInt32();
				if (data == 0 && canReadDocumentRecord) {
					// document-record

					documentRid = seqPointsReader.ReadCompressedUInt32();
					TryGetSymbolDocument(documentRid, out document);
				}
				else {
					// SequencePointRecord

					Debug.Assert(document is not null);
					if (document is null)
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

					uint dlines = seqPointsReader.ReadCompressedUInt32();
					int dcolumns = dlines == 0 ? (int)seqPointsReader.ReadCompressedUInt32() : seqPointsReader.ReadCompressedInt32();

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
							line = (int)seqPointsReader.ReadCompressedUInt32();
							column = (int)seqPointsReader.ReadCompressedUInt32();
						}
						else {
							line += seqPointsReader.ReadCompressedInt32();
							column += seqPointsReader.ReadCompressedInt32();
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
			Debug.Assert(seqPointsReader.Position == seqPointsReader.Length);

			return ListCache<SymbolSequencePoint>.FreeAndToArray(ref seqPointsBuilder);
		}

		SymbolScopeImpl ReadScope(uint methodRid, GenericParamContext gpContext) {
			var scopesRidList = pdbMetadata.GetLocalScopeRidList(methodRid);
			SymbolScopeImpl rootScopeOrNull = null;
			if (scopesRidList.Count != 0) {
				var custInfos = ListCache<PdbCustomDebugInfo>.AllocList();
				var stack = ListCache<SymbolScopeImpl>.AllocList();
				var importScopeBlobReader = new ImportScopeBlobReader(module, pdbMetadata.BlobStream);
				for (int i = 0; i < scopesRidList.Count; i++) {
					var rid = scopesRidList[i];
					int token = new MDToken(Table.LocalScope, rid).ToInt32();
					bool b = pdbMetadata.TablesStream.TryReadLocalScopeRow(rid, out var row);
					Debug.Assert(b);
					uint startOffset = row.StartOffset;
					uint endOffset = startOffset + row.Length;

					SymbolScopeImpl parent = null;
					while (stack.Count > 0) {
						var nextParent = stack[stack.Count - 1];
						if (startOffset >= nextParent.StartOffset && endOffset <= nextParent.EndOffset) {
							parent = nextParent;
							break;
						}
						stack.RemoveAt(stack.Count - 1);
					}

					Debug.Assert(parent is not null || rootScopeOrNull is null);
					custInfos.Clear();
					GetCustomDebugInfos(token, gpContext, custInfos);
					var customDebugInfos = custInfos.Count == 0 ? Array2.Empty<PdbCustomDebugInfo>() : custInfos.ToArray();
					var scope = new SymbolScopeImpl(this, parent, (int)startOffset, (int)endOffset, customDebugInfos);
					if (rootScopeOrNull is null)
						rootScopeOrNull = scope;
					stack.Add(scope);
					if (parent is not null)
						parent.childrenList.Add(scope);

					scope.importScope = ReadPdbImportScope(ref importScopeBlobReader, row.ImportScope, gpContext);
					ReadVariables(scope, gpContext, pdbMetadata.GetLocalVariableRidList(rid));
					ReadConstants(scope, pdbMetadata.GetLocalConstantRidList(rid));
				}

				ListCache<SymbolScopeImpl>.Free(ref stack);
				ListCache<PdbCustomDebugInfo>.Free(ref custInfos);
			}
			return rootScopeOrNull ?? new SymbolScopeImpl(this, null, 0, int.MaxValue, Array2.Empty<PdbCustomDebugInfo>());
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
				if (!pdbMetadata.TablesStream.TryReadImportScopeRow(importScope, out var row))
					return null;
				var scope = new PdbImportScope();
				GetCustomDebugInfos(token, gpContext, scope.CustomDebugInfos);
				if (result is null)
					result = scope;
				if (prevScope is not null)
					prevScope.Parent = scope;
				importScopeBlobReader.Read(row.Imports, scope.Imports);
				prevScope = scope;
				importScope = row.Parent;
			}

			return result;
		}

		void ReadVariables(SymbolScopeImpl scope, GenericParamContext gpContext, RidList rids) {
			if (rids.Count == 0)
				return;
			var table = pdbMetadata.TablesStream.LocalVariableTable;
			var custInfos = ListCache<PdbCustomDebugInfo>.AllocList();
			for (int i = 0; i < rids.Count; i++) {
				var rid = rids[i];
				int token = new MDToken(Table.LocalVariable, rid).ToInt32();
				custInfos.Clear();
				GetCustomDebugInfos(token, gpContext, custInfos);
				var customDebugInfos = custInfos.Count == 0 ? Array2.Empty<PdbCustomDebugInfo>() : custInfos.ToArray();
				bool b = pdbMetadata.TablesStream.TryReadLocalVariableRow(rid, out var row);
				Debug.Assert(b);
				var name = pdbMetadata.StringsStream.Read(row.Name);
				scope.localsList.Add(new SymbolVariableImpl(name, ToSymbolVariableAttributes(row.Attributes), row.Index, customDebugInfos));
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

		void ReadConstants(SymbolScopeImpl scope, RidList rids) {
			if (rids.Count == 0)
				return;
			scope.SetConstants(pdbMetadata, rids);
		}

		internal void GetCustomDebugInfos(SymbolMethodImpl symMethod, MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result) {
			Debug.Assert(method.Module == module);
			GetCustomDebugInfos(method.MDToken.ToInt32(), GenericParamContext.Create(method), result, method, body, out var asyncStepInfo);
			if (asyncStepInfo is not null) {
				var asyncMethod = TryCreateAsyncMethod(module, symMethod.KickoffMethod, asyncStepInfo.AsyncStepInfos, asyncStepInfo.CatchHandler);
				Debug.Assert(asyncMethod is not null);
				if (asyncMethod is not null)
					result.Add(asyncMethod);
			}
			else if (symMethod.KickoffMethod != 0) {
				var iteratorMethod = TryCreateIteratorMethod(module, symMethod.KickoffMethod);
				Debug.Assert(iteratorMethod is not null);
				if (iteratorMethod is not null)
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
			int count = asyncStepInfos.Count;
			for (int i = 0; i < count; i++)
				asyncMethod.StepInfos.Add(asyncStepInfos[i]);
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
			GetCustomDebugInfos(token, gpContext, result, null, null, out var asyncStepInfo);
			Debug.Assert(asyncStepInfo is null);
		}

		void GetCustomDebugInfos(int token, GenericParamContext gpContext, IList<PdbCustomDebugInfo> result, MethodDef methodOpt, CilBody bodyOpt, out PdbAsyncMethodSteppingInformationCustomDebugInfo asyncStepInfo) {
			asyncStepInfo = null;
			var mdToken = new MDToken(token);
			var ridList = pdbMetadata.GetCustomDebugInformationRidList(mdToken.Table, mdToken.Rid);
			if (ridList.Count == 0)
				return;
			var typeOpt = methodOpt?.DeclaringType;
			for (int i = 0; i < ridList.Count; i++) {
				var rid = ridList[i];
				if (!pdbMetadata.TablesStream.TryReadCustomDebugInformationRow(rid, out var row))
					continue;
				var guid = pdbMetadata.GuidStream.Read(row.Kind);
				if (!pdbMetadata.BlobStream.TryCreateReader(row.Value, out var reader))
					continue;
				Debug.Assert(guid is not null);
				if (guid is null)
					continue;
				var cdi = PortablePdbCustomDebugInfoReader.Read(module, typeOpt, bodyOpt, gpContext, guid.Value, ref reader);
				Debug.Assert(cdi is not null);
				if (cdi is not null) {
					if (cdi is PdbAsyncMethodSteppingInformationCustomDebugInfo asyncStepInfoTmp) {
						Debug.Assert(asyncStepInfo is null);
						asyncStepInfo = asyncStepInfoTmp;
					}
					else
						result.Add(cdi);
				}
			}
		}

		public override void Dispose() => pdbMetadata.Dispose();
	}
}
