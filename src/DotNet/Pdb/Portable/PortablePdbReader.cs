// dnlib: See LICENSE.txt for more info

// https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
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
			CheckCustomDebugInfoSize();
			CheckImportScopeSize();
			documents = ReadDocuments();
		}

		// Each row parses its own copy of its blob, and many rows can use the same blob, so memory
		// could grow with rows * blob size. Blobs that don't overlap can't add up to more than the
		// heap. Compilers share identical blobs, such as several embedded copies of one file, so a
		// fixed amount is allowed too, to keep those files loading.
		const long MinBlobSizeLimit = 16 * 1024 * 1024;

		long GetBlobSizeLimit() => Math.Max((long)pdbMetadata.BlobStream.StreamLength * 2, MinBlobSizeLimit);

		void CheckCustomDebugInfoSize() {
			var tablesStream = pdbMetadata.TablesStream;
			var blobStream = pdbMetadata.BlobStream;
			long maxSize = GetBlobSizeLimit();
			long size = 0;
			uint rows = tablesStream.CustomDebugInformationTable.Rows;
			for (uint rid = 1; rid <= rows; rid++) {
				if (!tablesStream.TryReadCustomDebugInformationRow(rid, out var row))
					continue;
				if (!blobStream.TryCreateReader(row.Value, out var reader))
					continue;
				size += reader.Length;
				if (size > maxSize)
					throw new IOException("Custom debug info rows share too much blob data");
			}
		}

		// The same problem as above: each ImportScope row parses its own copy of its imports blob, and
		// each import decodes its own copy of its strings, so rows can share an imports blob and
		// imports can share a string blob. Rows are counted even if no method uses them, so the result
		// doesn't depend on which method bodies are read.
		void CheckImportScopeSize() {
			var tablesStream = pdbMetadata.TablesStream;
			var importScopeBlobReader = new ImportScopeBlobReader(module, pdbMetadata.BlobStream);
			long maxSize = GetBlobSizeLimit();
			long size = 0;
			uint rows = tablesStream.ImportScopeTable.Rows;
			for (uint rid = 1; rid <= rows; rid++) {
				if (!tablesStream.TryReadImportScopeRow(rid, out var row))
					continue;
				size += importScopeBlobReader.GetSize(row.Imports);
				if (size > maxSize)
					throw new IOException("Import scope rows share too much blob data");
			}
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
				Debug.Fail($"Couldn't find document with rid 0x{rid:X6}");
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
			try {
				return ReadSequencePoints(ref seqPointsReader, documentRid);
			}
			catch (IOException) {
				// A corrupt blob loses this method's sequence points, like the invalid data checks below
				return null;
			}
		}

		SymbolSequencePoint[] ReadSequencePoints(ref DataReader seqPointsReader, uint documentRid) {
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

					scope.importScope = GetPdbImportScope(row.ImportScope);
					ReadVariables(scope, gpContext, pdbMetadata.GetLocalVariableRidList(rid));
					ReadConstants(scope, pdbMetadata.GetLocalConstantRidList(rid));
				}

				ListCache<SymbolScopeImpl>.Free(ref stack);
				ListCache<PdbCustomDebugInfo>.Free(ref custInfos);
			}
			return rootScopeOrNull ?? new SymbolScopeImpl(this, null, 0, int.MaxValue, Array2.Empty<PdbCustomDebugInfo>());
		}

		// Many LocalScopes, in one method or in many, can use the same ImportScope rows, and each row
		// can have a long Parent chain. Each row is read once, and every scope that uses it gets the
		// same PdbImportScope. Reading the whole chain again for each LocalScope used memory and time
		// proportional to LocalScopes * chain length * blob sizes.
		// importScopes[rid] is the row's scope once created. importScopeChainLengths[rid] is the
		// number of rows in its chain, InvalidImportScopeChainLength if the chain is too long, has a
		// loop or has a row that can't be read, or 0 if not known yet. Both only ever change from
		// their default to a final value that doesn't depend on the order rows are read in, so
		// threads reading different methods can share them without a lock.
		const int MaxImportScopeChainLength = 1000;
		const int InvalidImportScopeChainLength = MaxImportScopeChainLength + 1;
		PdbImportScope[] importScopes;
		int[] importScopeChainLengths;

		static T[] GetOrCreateArray<T>(ref T[] array, uint length) =>
			array ?? Interlocked.CompareExchange(ref array, new T[length], null) ?? array;

		PdbImportScope GetPdbImportScope(uint importScope) {
			if (importScope == 0)
				return null;
			uint rows = pdbMetadata.TablesStream.ImportScopeTable.Rows;
			if (importScope > rows)
				return null;
			if (GetImportScopeChainLength(importScope, rows) > MaxImportScopeChainLength)
				return null;
			return CreatePdbImportScope(importScope, rows);
		}

		int GetImportScopeChainLength(uint importScope, uint rows) {
			var lengths = GetOrCreateArray(ref importScopeChainLengths, rows + 1);
			int length = lengths[importScope];
			if (length != 0)
				return length;

			// Usually the parent is 0 or its length is already known, so don't allocate a path
			if (pdbMetadata.TablesStream.TryReadImportScopeRow(importScope, out var firstRow)) {
				uint parent = firstRow.Parent;
				int parentLength = parent == 0 ? 0 :
					parent > rows || parent == importScope ? InvalidImportScopeChainLength : lengths[parent];
				if (parent == 0 || parentLength != 0) {
					length = parentLength < InvalidImportScopeChainLength ? parentLength + 1 : InvalidImportScopeChainLength;
					lengths[importScope] = length;
					return length;
				}
			}

			// Find the rows whose length isn't known yet. Each row is added to a path at most once
			// (besides threads racing), so all calls together take time proportional to the rows.
			// A row that's already in the path is a loop, and all rows in the path lead to it.
			var path = new List<uint>();
			var inPath = new HashSet<uint>();
			uint rid = importScope;
			while (true) {
				if (rid == 0) {
					length = 0;
					break;
				}
				if (rid > rows) {
					length = InvalidImportScopeChainLength;
					break;
				}
				length = lengths[rid];
				if (length != 0)
					break;
				if (!inPath.Add(rid) || !pdbMetadata.TablesStream.TryReadImportScopeRow(rid, out var row)) {
					length = InvalidImportScopeChainLength;
					break;
				}
				path.Add(rid);
				rid = row.Parent;
			}

			for (int i = path.Count - 1; i >= 0; i--) {
				if (length < InvalidImportScopeChainLength)
					length++;
				lengths[path[i]] = length;
			}
			return lengths[importScope];
		}

		// The caller has checked that the chain is valid, so it has at most MaxImportScopeChainLength rows
		PdbImportScope CreatePdbImportScope(uint importScope, uint rows) {
			var scopes = GetOrCreateArray(ref importScopes, rows + 1);
			var result = scopes[importScope];
			if (result is not null)
				return result;

			var path = new List<uint>();
			PdbImportScope parent = null;
			for (uint rid = importScope; rid != 0;) {
				parent = scopes[rid];
				if (parent is not null)
					break;
				path.Add(rid);
				bool b = pdbMetadata.TablesStream.TryReadImportScopeRow(rid, out var row);
				Debug.Assert(b);
				rid = row.Parent;
			}

			// The scopes are shared by all methods, so no method's generic context is used. Import scope
			// custom debug info doesn't use it.
			var gpContext = new GenericParamContext();
			var importScopeBlobReader = new ImportScopeBlobReader(module, pdbMetadata.BlobStream);
			for (int i = path.Count - 1; i >= 0; i--) {
				uint rid = path[i];
				bool b = pdbMetadata.TablesStream.TryReadImportScopeRow(rid, out var row);
				Debug.Assert(b);
				var scope = new PdbImportScope();
				GetCustomDebugInfos(new MDToken(Table.ImportScope, rid).ToInt32(), gpContext, scope.CustomDebugInfos);
				importScopeBlobReader.Read(row.Imports, scope.Imports);
				scope.Parent = parent;
				// Another thread could have created it first. Use one object so all scopes share it.
				parent = Interlocked.CompareExchange(ref scopes[rid], scope, null) ?? scope;
			}
			return parent;
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
