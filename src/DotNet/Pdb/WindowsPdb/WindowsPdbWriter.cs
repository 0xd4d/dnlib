// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.WindowsPdb {
	sealed class WindowsPdbWriter : IDisposable {
		SymbolWriter writer;
		readonly PdbState pdbState;
		readonly ModuleDef module;
		readonly Metadata metadata;
		readonly Dictionary<PdbDocument, ISymbolDocumentWriter> pdbDocs = new Dictionary<PdbDocument, ISymbolDocumentWriter>();
		readonly SequencePointHelper seqPointsHelper = new SequencePointHelper();
		readonly Dictionary<Instruction, uint> instrToOffset;
		InstructionOffsetCache externalInstructionOffsets;
		readonly PdbCustomDebugInfoWriterContext customDebugInfoWriterContext;
		readonly int localsEndScopeIncValue;

		public ILogger Logger { get; set; }

		public WindowsPdbWriter(SymbolWriter writer, PdbState pdbState, Metadata metadata)
			: this(pdbState, metadata) {
			if (pdbState is null)
				throw new ArgumentNullException(nameof(pdbState));
			if (metadata is null)
				throw new ArgumentNullException(nameof(metadata));
			this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
			writer.Initialize(metadata);
		}

		WindowsPdbWriter(PdbState pdbState, Metadata metadata) {
			this.pdbState = pdbState;
			this.metadata = metadata;
			module = metadata.Module;
			instrToOffset = new Dictionary<Instruction, uint>();
			customDebugInfoWriterContext = new PdbCustomDebugInfoWriterContext();
			localsEndScopeIncValue = PdbUtils.IsEndInclusive(PdbFileKind.WindowsPDB, pdbState.Compiler) ? 1 : 0;
		}

		ISymbolDocumentWriter Add(PdbDocument pdbDoc) {
			if (pdbDocs.TryGetValue(pdbDoc, out var docWriter))
				return docWriter;
			docWriter = writer.DefineDocument(pdbDoc.Url, pdbDoc.Language, pdbDoc.LanguageVendor, pdbDoc.DocumentType);
			docWriter.SetCheckSum(pdbDoc.CheckSumAlgorithmId, pdbDoc.CheckSum);
			if (TryGetCustomDebugInfo(pdbDoc, out PdbEmbeddedSourceCustomDebugInfo sourceCdi))
				docWriter.SetSource(sourceCdi.SourceCodeBlob);
			pdbDocs.Add(pdbDoc, docWriter);
			return docWriter;
		}

		static bool TryGetCustomDebugInfo<TCDI>(IHasCustomDebugInformation hci, out TCDI cdi) where TCDI : PdbCustomDebugInfo {
			var cdis = hci.CustomDebugInfos;
			int count = cdis.Count;
			for (int i = 0; i < count; i++) {
				if (cdis[i] is TCDI cdi2) {
					cdi = cdi2;
					return true;
				}
			}
			cdi = null;
			return false;
		}

		public void Write() {
			writer.SetUserEntryPoint(GetUserEntryPointToken());

			var cdiBuilder = new List<PdbCustomDebugInfo>();
			foreach (var type in module.GetTypes()) {
				if (type is null)
					continue;
				var typeMethods = type.Methods;
				int count = typeMethods.Count;
				for (int i = 0; i < count; i++) {
					var method = typeMethods[i];
					if (method is null)
						continue;
					if (!ShouldAddMethod(method))
						continue;
					Write(method, cdiBuilder);
				}
			}

			if (TryGetCustomDebugInfo(module, out PdbSourceLinkCustomDebugInfo sourceLinkCdi))
				writer.SetSourceLinkData(sourceLinkCdi.FileBlob);
			if (TryGetCustomDebugInfo(module, out PdbSourceServerCustomDebugInfo sourceServerCdi))
				writer.SetSourceServerData(sourceServerCdi.FileBlob);
		}

		bool ShouldAddMethod(MethodDef method) {
			var body = method.Body;
			if (body is null)
				return false;

			if (body.HasPdbMethod)
				return true;

			var bodyVariables = body.Variables;
			int count = bodyVariables.Count;
			for (int i = 0; i < count; i++) {
				var local = bodyVariables[i];
				// Don't check whether it's the empty string. Only check for null.
				if (local.Name is not null)
					return true;
				if (local.Attributes != 0)
					return true;
			}

			var bodyInstructions = body.Instructions;
			count = bodyInstructions.Count;
			for (int i = 0; i < count; i++) {
				if (bodyInstructions[i].SequencePoint is not null)
					return true;
			}

			return false;
		}

		sealed class SequencePointHelper {
			// Documents in the order they're first used. Equal PdbDocument instances (Equals()) share an index
			// and are written with the first instance.
			readonly List<PdbDocument> docs = new List<PdbDocument>();
			readonly Dictionary<PdbDocument, int> docToIndex = new Dictionary<PdbDocument, int>();
			int[] docStarts = Array2.Empty<int>();
			// Document index of each sequence point
			int[] seqPointDocs = Array2.Empty<int>();
			int[] instrOffsets = Array2.Empty<int>();
			int[] startLines = Array2.Empty<int>();
			int[] startColumns = Array2.Empty<int>();
			int[] endLines = Array2.Empty<int>();
			int[] endColumns = Array2.Empty<int>();
			// Sequence points sorted by document, only used if a method uses more than one document
			int[] sortedInstrOffsets = Array2.Empty<int>();
			int[] sortedStartLines = Array2.Empty<int>();
			int[] sortedStartColumns = Array2.Empty<int>();
			int[] sortedEndLines = Array2.Empty<int>();
			int[] sortedEndColumns = Array2.Empty<int>();

			public void Write(WindowsPdbWriter pdbWriter, IList<Instruction> instrs) {
				try {
					Write2(pdbWriter, instrs);
				}
				finally {
					// Don't keep the method's documents alive
					docs.Clear();
					docToIndex.Clear();
				}
			}

			void Write2(WindowsPdbWriter pdbWriter, IList<Instruction> instrs) {
				// One pass over the instructions. The points are saved in instruction order.
				int count = 0, instrOffset = 0;
				PdbDocument lastDoc = null;
				int lastDocIndex = -1;
				Instruction instr = null;
				for (int i = 0; i < instrs.Count; i++, instrOffset += instr.GetSize()) {
					instr = instrs[i];
					var seqp = instr.SequencePoint;
					if (seqp is null)
						continue;
					var doc = seqp.Document;
					if (doc is null)
						continue;

					int docIndex;
					if ((object)doc == lastDoc)
						docIndex = lastDocIndex;
					else {
						if (!docToIndex.TryGetValue(doc, out docIndex)) {
							docIndex = docs.Count;
							docs.Add(doc);
							docToIndex.Add(doc, docIndex);
						}
						lastDoc = doc;
						lastDocIndex = docIndex;
					}

					if (count >= instrOffsets.Length) {
						int newSize = count * 2;
						if (newSize < 64)
							newSize = 64;
						Array.Resize(ref seqPointDocs, newSize);
						Array.Resize(ref instrOffsets, newSize);
						Array.Resize(ref startLines, newSize);
						Array.Resize(ref startColumns, newSize);
						Array.Resize(ref endLines, newSize);
						Array.Resize(ref endColumns, newSize);
					}

					seqPointDocs[count]	= docIndex;
					instrOffsets[count]	= instrOffset;
					startLines[count]	= seqp.StartLine;
					startColumns[count]	= seqp.StartColumn;
					endLines[count]		= seqp.EndLine;
					endColumns[count]	= seqp.EndColumn;
					count++;
				}

				if (docs.Count == 0)
					return;
				if (docs.Count == 1) {
					pdbWriter.writer.DefineSequencePoints(pdbWriter.Add(docs[0]), (uint)count, instrOffsets, startLines, startColumns, endLines, endColumns);
					return;
				}

				// Stable counting sort by document
				int docCount = docs.Count;
				if (docStarts.Length < docCount)
					docStarts = new int[Math.Max(docCount, docStarts.Length * 2)];
				Array.Clear(docStarts, 0, docCount);
				for (int i = 0; i < count; i++)
					docStarts[seqPointDocs[i]]++;
				int start = 0;
				for (int d = 0; d < docCount; d++) {
					int n = docStarts[d];
					docStarts[d] = start;
					start += n;
				}
				if (sortedInstrOffsets.Length < count) {
					int newSize = instrOffsets.Length;
					sortedInstrOffsets = new int[newSize];
					sortedStartLines = new int[newSize];
					sortedStartColumns = new int[newSize];
					sortedEndLines = new int[newSize];
					sortedEndColumns = new int[newSize];
				}
				for (int i = 0; i < count; i++) {
					int j = docStarts[seqPointDocs[i]]++;
					sortedInstrOffsets[j]	= instrOffsets[i];
					sortedStartLines[j]		= startLines[i];
					sortedStartColumns[j]	= startColumns[i];
					sortedEndLines[j]		= endLines[i];
					sortedEndColumns[j]		= endColumns[i];
				}

				// docStarts[d] is now the end of document d's points. The arrays must start at index 0, so
				// copy each document's points to the start of the unsorted arrays.
				int begin = 0;
				for (int d = 0; d < docCount; d++) {
					int end = docStarts[d];
					int n = end - begin;
					Array.Copy(sortedInstrOffsets, begin, instrOffsets, 0, n);
					Array.Copy(sortedStartLines, begin, startLines, 0, n);
					Array.Copy(sortedStartColumns, begin, startColumns, 0, n);
					Array.Copy(sortedEndLines, begin, endLines, 0, n);
					Array.Copy(sortedEndColumns, begin, endColumns, 0, n);
					pdbWriter.writer.DefineSequencePoints(pdbWriter.Add(docs[d]), (uint)n, instrOffsets, startLines, startColumns, endLines, endColumns);
					begin = end;
				}
			}
		}

		struct CurrentMethod {
			readonly WindowsPdbWriter pdbWriter;
			public readonly MethodDef Method;
			readonly Dictionary<Instruction, uint> toOffset;
			public readonly uint BodySize;

			public CurrentMethod(WindowsPdbWriter pdbWriter, MethodDef method, Dictionary<Instruction, uint> toOffset) {
				this.pdbWriter = pdbWriter;
				Method = method;
				this.toOffset = toOffset;
				toOffset.Clear();
				uint offset = 0;
				var instructions = method.Body.Instructions;
				int count = instructions.Count;
				for (int i = 0; i < count; i++) {
					var instr = instructions[i];
					toOffset[instr] = offset;
					offset += (uint)instr.GetSize();
				}
				BodySize = offset;
			}

			public readonly int GetOffset(Instruction instr) {
				if (instr is null)
					return (int)BodySize;
				if (toOffset.TryGetValue(instr, out uint offset))
					return (int)offset;
				pdbWriter.Error("Instruction was removed from the body but is referenced from PdbScope: {0}", instr);
				return (int)BodySize;
			}
		}

		void Write(MethodDef method, List<PdbCustomDebugInfo> cdiBuilder) {
			uint rid = metadata.GetRid(method);
			if (rid == 0) {
				Error("Method {0} ({1:X8}) is not defined in this module ({2})", method, method.MDToken.Raw, module);
				return;
			}

			var info = new CurrentMethod(this, method, instrToOffset);
			var body = method.Body;
			var symbolToken = new MDToken(MD.Table.Method, rid);
			writer.OpenMethod(symbolToken);
			seqPointsHelper.Write(this, info.Method.Body.Instructions);

			// Don't modify the module: it may be read by another thread while it's being written
			var scope = body.PdbMethod?.Scope;
			if (scope is null || (scope.Namespaces.Count == 0 && scope.Variables.Count == 0 && scope.Constants.Count == 0)) {
				if (scope is null || scope.Scopes.Count == 0) {
					// We must open at least one sub scope (the sym writer creates the 'method' scope
					// which covers the whole method) or the native PDB reader will fail to read all
					// sequence points.
					writer.OpenScope(0);
					writer.CloseScope((int)info.BodySize);
				}
				else {
					var scopes = scope.Scopes;
					int count = scopes.Count;
					for (int i = 0; i < count; i++)
						WriteScope(ref info, scopes[i], 0);
				}
			}
			else {
				// C++/.NET (some methods)
				WriteScope(ref info, scope, 0);
			}

			GetPseudoCustomDebugInfos(method.CustomDebugInfos, cdiBuilder, out var asyncMethod);
			if (cdiBuilder.Count != 0) {
				customDebugInfoWriterContext.Logger = GetLogger();
				var cdiData = PdbCustomDebugInfoWriter.Write(metadata, method, customDebugInfoWriterContext, cdiBuilder);
				if (cdiData is not null)
					writer.SetSymAttribute(symbolToken, "MD2", cdiData);
			}

			if (asyncMethod is not null) {
				if (!writer.SupportsAsyncMethods)
					Error("PDB symbol writer doesn't support writing async methods");
				else
					WriteAsyncMethod(ref info, asyncMethod);
			}

			writer.CloseMethod();
		}

		void GetPseudoCustomDebugInfos(IList<PdbCustomDebugInfo> customDebugInfos, List<PdbCustomDebugInfo> cdiBuilder, out PdbAsyncMethodCustomDebugInfo asyncMethod) {
			cdiBuilder.Clear();
			asyncMethod = null;
			int count = customDebugInfos.Count;
			for (int i = 0; i < count; i++) {
				var cdi = customDebugInfos[i];
				switch (cdi.Kind) {
				case PdbCustomDebugInfoKind.AsyncMethod:
					if (asyncMethod is not null)
						Error("Duplicate async method custom debug info");
					else
						asyncMethod = (PdbAsyncMethodCustomDebugInfo)cdi;
					break;

				default:
					if ((uint)cdi.Kind > byte.MaxValue)
						Error("Custom debug info {0} isn't supported by Windows PDB files", cdi.Kind);
					else
						cdiBuilder.Add(cdi);
					break;
				}
			}
		}

		uint GetMethodToken(MethodDef method) {
			uint rid = metadata.GetRid(method);
			if (rid == 0)
				Error("Method {0} ({1:X8}) is not defined in this module ({2})", method, method.MDToken.Raw, module);
			return new MDToken(MD.Table.Method, rid).Raw;
		}

		void WriteAsyncMethod(ref CurrentMethod info, PdbAsyncMethodCustomDebugInfo asyncMethod) {
			if (asyncMethod.KickoffMethod is null) {
				Error("KickoffMethod is null");
				return;
			}

			uint kickoffMethod = GetMethodToken(asyncMethod.KickoffMethod);
			writer.DefineKickoffMethod(kickoffMethod);

			if (asyncMethod.CatchHandlerInstruction is not null) {
				int catchHandlerILOffset = info.GetOffset(asyncMethod.CatchHandlerInstruction);
				writer.DefineCatchHandlerILOffset((uint)catchHandlerILOffset);
			}

			var stepInfos = asyncMethod.StepInfos;
			var yieldOffsets = new uint[stepInfos.Count];
			var breakpointOffset = new uint[stepInfos.Count];
			var breakpointMethods = new uint[stepInfos.Count];
			for (int i = 0; i < yieldOffsets.Length; i++) {
				var stepInfo = stepInfos[i];
				if (stepInfo.YieldInstruction is null) {
					Error("YieldInstruction is null");
					return;
				}
				if (stepInfo.BreakpointMethod is null) {
					Error("BreakpointMethod is null");
					return;
				}
				if (stepInfo.BreakpointInstruction is null) {
					Error("BreakpointInstruction is null");
					return;
				}
				yieldOffsets[i] = (uint)info.GetOffset(stepInfo.YieldInstruction);
				breakpointOffset[i] = (uint)GetExternalInstructionOffset(ref info, stepInfo.BreakpointMethod, stepInfo.BreakpointInstruction);
				breakpointMethods[i] = GetMethodToken(stepInfo.BreakpointMethod);
			}
			writer.DefineAsyncStepInfo(yieldOffsets, breakpointOffset, breakpointMethods);
		}

		int GetExternalInstructionOffset(ref CurrentMethod info, MethodDef method, Instruction instr) {
			if (info.Method == method)
				return info.GetOffset(instr);
			var body = method.Body;
			if (body is null) {
				Error("Method body is null");
				return 0;
			}
			externalInstructionOffsets ??= new InstructionOffsetCache();
			if (externalInstructionOffsets.TryGetOffset(method, body, instr, out uint offset))
				return (int)offset;
			Error("Async method instruction has been removed but it's still being referenced by PDB info: BP Instruction: {0}, BP Method: {1} (0x{2:X8}), Current Method: {3} (0x{4:X8})", instr, method, method.MDToken.Raw, info.Method, info.Method.MDToken.Raw);
			return 0;
		}

		void WriteScope(ref CurrentMethod info, PdbScope scope, int recursionCounter) {
			if (recursionCounter >= 1000) {
				Error("Too many PdbScopes");
				return;
			}

			int startOffset = info.GetOffset(scope.Start);
			int endOffset = info.GetOffset(scope.End);
			if (endOffset < startOffset) {
				Error("PDB scope's end instruction is before its start instruction. Method {0} ({1:X8})", info.Method, info.Method.MDToken.Raw);
				endOffset = startOffset;
			}
			writer.OpenScope(startOffset);
			AddLocals(info.Method, scope.Variables, (uint)startOffset, (uint)endOffset);
			if (scope.Constants.Count > 0) {
				var constants = scope.Constants;
				var sig = new FieldSig();
				for (int i = 0; i < constants.Count; i++) {
					var constant = constants[i];
					sig.Type = constant.Type;
					var token = metadata.GetToken(sig);
					writer.DefineConstant(constant.Name, constant.Value ?? boxedZeroInt32, token.Raw);
				}
			}
			var scopeNamespaces = scope.Namespaces;
			int count = scopeNamespaces.Count;
			for (int i = 0; i < count; i++)
				writer.UsingNamespace(scopeNamespaces[i]);
			var scopes = scope.Scopes;
			count = scopes.Count;
			for (int i = 0; i < count; i++)
				WriteScope(ref info, scopes[i], recursionCounter + 1);
			writer.CloseScope(startOffset == 0 && endOffset == info.BodySize ? endOffset : endOffset - localsEndScopeIncValue);
		}
		static readonly object boxedZeroInt32 = 0;

		void AddLocals(MethodDef method, IList<PdbLocal> locals, uint startOffset, uint endOffset) {
			if (locals.Count == 0)
				return;
			uint token = metadata.GetLocalVarSigToken(method);
			if (token == 0) {
				Error("Method {0} ({1:X8}) has no local signature token", method, method.MDToken.Raw);
				return;
			}
			int count = locals.Count;
			for (int i = 0; i < count; i++) {
				var local = locals[i];
				if (local is null) {
					Error("PDB local is null. Method {0} ({1:X8})", method, method.MDToken.Raw);
					continue;
				}
				if (!PdbUtils.IsValidLocal(local, method.Body, out var errorMessage)) {
					Error(errorMessage + ". Method {1} ({2:X8})", local.Name, method, method.MDToken.Raw);
					continue;
				}
				uint attrs = GetPdbLocalFlags(local.Attributes);
				if (attrs == 0 && local.Name is null)
					continue;
				writer.DefineLocalVariable(local.Name ?? string.Empty, attrs,
								token, 1, (uint)local.Index, 0, 0, startOffset, endOffset);
			}
		}

		static uint GetPdbLocalFlags(PdbLocalAttributes attributes) {
			if ((attributes & PdbLocalAttributes.DebuggerHidden) != 0)
				return (uint)CorSymVarFlag.VAR_IS_COMP_GEN;
			return 0;
		}

		MDToken GetUserEntryPointToken() {
			var ep = pdbState.UserEntryPoint;
			if (ep is null)
				return default;
			uint rid = metadata.GetRid(ep);
			if (rid == 0) {
				Error("PDB user entry point method {0} ({1:X8}) is not defined in this module ({2})", ep, ep.MDToken.Raw, module);
				return default;
			}
			return new MDToken(MD.Table.Method, rid);
		}

		public bool GetDebugInfo(ChecksumAlgorithm pdbChecksumAlgorithm, ref uint pdbAge, out Guid guid, out uint stamp, out IMAGE_DEBUG_DIRECTORY idd, out byte[] codeViewData) =>
			writer.GetDebugInfo(pdbChecksumAlgorithm, ref pdbAge, out guid, out stamp, out idd, out codeViewData);

		public void Close() => writer.Close();
		ILogger GetLogger() => Logger ?? DummyLogger.ThrowModuleWriterExceptionOnErrorInstance;
		void Error(string message, params object[] args) => GetLogger().Log(this, LoggerEvent.Error, message, args);

		/// <inheritdoc/>
		public void Dispose() {
			if (writer is not null)
				Close();
			writer?.Dispose();
			writer = null;
		}
	}
}
