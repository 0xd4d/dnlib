// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.WindowsPdb {
	/// <summary>
	/// PDB writer
	/// </summary>
	/// <remarks>This class is not thread safe because it's a writer class</remarks>
	public sealed class WindowsPdbWriter : IDisposable {
		ISymbolWriter2 writer;
		ISymbolWriter3 writer3;
		readonly PdbState pdbState;
		readonly ModuleDef module;
		readonly MetaData metaData;
		readonly Dictionary<PdbDocument, ISymbolDocumentWriter> pdbDocs = new Dictionary<PdbDocument, ISymbolDocumentWriter>();
		readonly SequencePointHelper seqPointsHelper = new SequencePointHelper();
		readonly Dictionary<Instruction, uint> instrToOffset;
		readonly PdbCustomDebugInfoWriterContext customDebugInfoWriterContext;
		readonly int localsEndScopeIncValue;

		/// <summary>
		/// Gets/sets the logger
		/// </summary>
		public ILogger Logger { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="writer">Symbol writer, it should implement <see cref="ISymbolWriter3"/></param>
		/// <param name="pdbState">PDB state</param>
		/// <param name="metaData">Meta data</param>
		public WindowsPdbWriter(ISymbolWriter2 writer, PdbState pdbState, MetaData metaData)
			: this(pdbState, metaData) {
			if (writer == null)
				throw new ArgumentNullException("writer");
			if (pdbState == null)
				throw new ArgumentNullException("pdbState");
			if (metaData == null)
				throw new ArgumentNullException("metaData");
			this.writer = writer;
			this.writer3 = writer as ISymbolWriter3;
			Debug.Assert(writer3 != null, "Symbol writer doesn't implement interface ISymbolWriter3");
			writer.Initialize(metaData);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="writer">Symbol writer</param>
		/// <param name="pdbState">PDB state</param>
		/// <param name="metaData">Meta data</param>
		public WindowsPdbWriter(ISymbolWriter3 writer, PdbState pdbState, MetaData metaData)
			: this(pdbState, metaData) {
			if (writer == null)
				throw new ArgumentNullException("writer");
			if (pdbState == null)
				throw new ArgumentNullException("pdbState");
			if (metaData == null)
				throw new ArgumentNullException("metaData");
			this.writer = writer;
			this.writer3 = writer;
			writer.Initialize(metaData);
		}

		WindowsPdbWriter(PdbState pdbState, MetaData metaData) {
			this.pdbState = pdbState;
			this.metaData = metaData;
			this.module = metaData.Module;
			this.instrToOffset = new Dictionary<Instruction, uint>();
			this.customDebugInfoWriterContext = new PdbCustomDebugInfoWriterContext();
			this.localsEndScopeIncValue = pdbState.Compiler == Compiler.VisualBasic ? 1 : 0;
		}

		/// <summary>
		/// Adds <paramref name="pdbDoc"/> if it doesn't exist
		/// </summary>
		/// <param name="pdbDoc">PDB document</param>
		/// <returns>A <see cref="ISymbolDocumentWriter"/> instance</returns>
		ISymbolDocumentWriter Add(PdbDocument pdbDoc) {
			ISymbolDocumentWriter docWriter;
			if (pdbDocs.TryGetValue(pdbDoc, out docWriter))
				return docWriter;
			docWriter = writer.DefineDocument(pdbDoc.Url, pdbDoc.Language, pdbDoc.LanguageVendor, pdbDoc.DocumentType);
			docWriter.SetCheckSum(pdbDoc.CheckSumAlgorithmId, pdbDoc.CheckSum);
			pdbDocs.Add(pdbDoc, docWriter);
			return docWriter;
		}

		/// <summary>
		/// Writes the PDB file
		/// </summary>
		public void Write() {
			writer.SetUserEntryPoint(new SymbolToken(GetUserEntryPointToken()));

			var cdiBuilder = new List<PdbCustomDebugInfo>();
			foreach (var type in module.GetTypes()) {
				if (type == null)
					continue;
				foreach (var method in type.Methods) {
					if (method == null)
						continue;
					if (!ShouldAddMethod(method))
						continue;
					Write(method, cdiBuilder);
				}
			}
		}

		bool ShouldAddMethod(MethodDef method) {
			var body = method.Body;
			if (body == null)
				return false;

			if (body.HasPdbMethod)
				return true;

			foreach (var local in body.Variables) {
				// Don't check whether it's the empty string. Only check for null.
				if (local.Name != null)
					return true;
				if (local.Attributes != 0)
					return true;
			}

			foreach (var instr in body.Instructions) {
				if (instr.SequencePoint != null)
					return true;
			}

			return false;
		}

		sealed class SequencePointHelper {
			readonly Dictionary<PdbDocument, bool> checkedPdbDocs = new Dictionary<PdbDocument, bool>();
			int[] instrOffsets = new int[0];
			int[] startLines;
			int[] startColumns;
			int[] endLines;
			int[] endColumns;

			public void Write(WindowsPdbWriter pdbWriter, IList<Instruction> instrs) {
				checkedPdbDocs.Clear();
				while (true) {
					PdbDocument currPdbDoc = null;
					bool otherDocsAvailable = false;
					int index = 0, instrOffset = 0;
					Instruction instr = null;
					for (int i = 0; i < instrs.Count; i++, instrOffset += instr.GetSize()) {
						instr = instrs[i];
						var seqp = instr.SequencePoint;
						if (seqp == null || seqp.Document == null)
							continue;
						if (checkedPdbDocs.ContainsKey(seqp.Document))
							continue;
						if (currPdbDoc == null)
							currPdbDoc = seqp.Document;
						else if (currPdbDoc != seqp.Document) {
							otherDocsAvailable = true;
							continue;
						}

						if (index >= instrOffsets.Length) {
							int newSize = index * 2;
							if (newSize < 64)
								newSize = 64;
							Array.Resize(ref instrOffsets, newSize);
							Array.Resize(ref startLines, newSize);
							Array.Resize(ref startColumns, newSize);
							Array.Resize(ref endLines, newSize);
							Array.Resize(ref endColumns, newSize);
						}

						instrOffsets[index]	= instrOffset;
						startLines[index]	= seqp.StartLine;
						startColumns[index]	= seqp.StartColumn;
						endLines[index]		= seqp.EndLine;
						endColumns[index]	= seqp.EndColumn;
						index++;
					}
					if (index != 0)
						pdbWriter.writer.DefineSequencePoints(pdbWriter.Add(currPdbDoc), (uint)index, instrOffsets, startLines, startColumns, endLines, endColumns);

					if (!otherDocsAvailable)
						break;
					if (currPdbDoc != null)
						checkedPdbDocs.Add(currPdbDoc, true);
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
				foreach (var instr in method.Body.Instructions) {
					toOffset[instr] = offset;
					offset += (uint)instr.GetSize();
				}
				BodySize = offset;
			}

			public int GetOffset(Instruction instr) {
				if (instr == null)
					return (int)BodySize;
				uint offset;
				if (toOffset.TryGetValue(instr, out offset))
					return (int)offset;
				pdbWriter.Error("Instruction was removed from the body but is referenced from PdbScope: {0}", instr);
				return (int)BodySize;
			}
		}

		void Write(MethodDef method, List<PdbCustomDebugInfo> cdiBuilder) {
			uint rid = metaData.GetRid(method);
			if (rid == 0) {
				Error("Method {0} ({1:X8}) is not defined in this module ({2})", method, method.MDToken.Raw, module);
				return;
			}

			var info = new CurrentMethod(this, method, instrToOffset);
			var body = method.Body;
			var symbolToken = new SymbolToken((int)new MDToken(MD.Table.Method, metaData.GetRid(method)).Raw);
			writer.OpenMethod(symbolToken);
			seqPointsHelper.Write(this, info.Method.Body.Instructions);

			var pdbMethod = body.PdbMethod;
			if (pdbMethod == null)
				body.PdbMethod = pdbMethod = new PdbMethod();
			var scope = pdbMethod.Scope;
			if (scope == null)
				pdbMethod.Scope = scope = new PdbScope();
			if (scope.Namespaces.Count == 0 && scope.Variables.Count == 0 && scope.Constants.Count == 0) {
				if (scope.Scopes.Count == 0) {
					// We must open at least one sub scope (the sym writer creates the 'method' scope
					// which covers the whole method) or the native PDB reader will fail to read all
					// sequence points.
					writer.OpenScope(0);
					writer.CloseScope((int)info.BodySize);
				}
				else {
					foreach (var childScope in scope.Scopes)
						WriteScope(ref info, childScope, 0);
				}
			}
			else {
				Debug.Fail("Root scope isn't empty");
				WriteScope(ref info, scope, 0);
			}

			PdbAsyncMethodCustomDebugInfo asyncMethod;
			GetPseudoCustomDebugInfos(method.CustomDebugInfos, cdiBuilder, out asyncMethod);
			if (cdiBuilder.Count != 0) {
				customDebugInfoWriterContext.Logger = GetLogger();
				var cdiData = PdbCustomDebugInfoWriter.Write(metaData, method, customDebugInfoWriterContext, cdiBuilder);
				if (cdiData != null)
					writer.SetSymAttribute(symbolToken, "MD2", cdiData);
			}

			if (asyncMethod != null) {
				if (writer3 == null || !writer3.SupportsAsyncMethods)
					Error("PDB symbol writer doesn't support writing async methods");
				else
					WriteAsyncMethod(ref info, asyncMethod);
			}

			writer.CloseMethod();
		}

		void GetPseudoCustomDebugInfos(IList<PdbCustomDebugInfo> customDebugInfos, List<PdbCustomDebugInfo> cdiBuilder, out PdbAsyncMethodCustomDebugInfo asyncMethod) {
			cdiBuilder.Clear();
			asyncMethod = null;
			foreach (var cdi in customDebugInfos) {
				switch (cdi.Kind) {
				case PdbCustomDebugInfoKind.AsyncMethod:
					if (asyncMethod != null)
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
			uint rid = metaData.GetRid(method);
			if (rid == 0)
				Error("Method {0} ({1:X8}) is not defined in this module ({2})", method, method.MDToken.Raw, module);
			return new MDToken(MD.Table.Method, rid).Raw;
		}

		void WriteAsyncMethod(ref CurrentMethod info, PdbAsyncMethodCustomDebugInfo asyncMethod) {
			if (asyncMethod.KickoffMethod == null) {
				Error("KickoffMethod is null");
				return;
			}

			uint kickoffMethod = GetMethodToken(asyncMethod.KickoffMethod);
			writer3.DefineKickoffMethod(kickoffMethod);

			if (asyncMethod.CatchHandlerInstruction != null) {
				int catchHandlerILOffset = info.GetOffset(asyncMethod.CatchHandlerInstruction);
				writer3.DefineCatchHandlerILOffset((uint)catchHandlerILOffset);
			}

			var stepInfos = asyncMethod.StepInfos;
			var yieldOffsets = new uint[stepInfos.Count];
			var breakpointOffset = new uint[stepInfos.Count];
			var breakpointMethods = new uint[stepInfos.Count];
			for (int i = 0; i < yieldOffsets.Length; i++) {
				var stepInfo = stepInfos[i];
				if (stepInfo.YieldInstruction == null) {
					Error("YieldInstruction is null");
					return;
				}
				if (stepInfo.BreakpointMethod == null) {
					Error("BreakpointInstruction is null");
					return;
				}
				if (stepInfo.BreakpointInstruction == null) {
					Error("BreakpointInstruction is null");
					return;
				}
				yieldOffsets[i] = (uint)info.GetOffset(stepInfo.YieldInstruction);
				breakpointOffset[i] = (uint)GetExternalInstructionOffset(ref info, stepInfo.BreakpointMethod, stepInfo.BreakpointInstruction);
				breakpointMethods[i] = GetMethodToken(stepInfo.BreakpointMethod);
			}
			writer3.DefineAsyncStepInfo(yieldOffsets, breakpointOffset, breakpointMethods);
		}

		int GetExternalInstructionOffset(ref CurrentMethod info, MethodDef method, Instruction instr) {
			if (info.Method == method)
				return info.GetOffset(instr);
			var body = method.Body;
			if (body == null) {
				Error("Method body is null");
				return 0;
			}

			var instrs = body.Instructions;
			int offset = 0;
			for (int i = 0; i < instrs.Count; i++) {
				var currInstr = instrs[i];
				if (currInstr == instr)
					return offset;
				offset += currInstr.GetSize();
			}
			if (instr == null)
				return offset;
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
			writer.OpenScope(startOffset);
			AddLocals(info.Method, scope.Variables, (uint)startOffset, (uint)endOffset);
			if (scope.Constants.Count > 0) {
				if (writer3 == null)
					Error("Symbol writer doesn't implement ISymbolWriter3: no constants can be written to the PDB file");
				else {
					var constants = scope.Constants;
					var sig = new FieldSig();
					for (int i = 0; i < constants.Count; i++) {
						var constant = constants[i];
						sig.Type = constant.Type;
						var token = metaData.GetToken(sig);
						writer3.DefineConstant2(constant.Name, constant.Value ?? 0, token.Raw);
					}
				}
			}
			foreach (var ns in scope.Namespaces)
				writer.UsingNamespace(ns);
			foreach (var childScope in scope.Scopes)
				WriteScope(ref info, childScope, recursionCounter + 1);
			writer.CloseScope(startOffset == 0 && endOffset == info.BodySize ? endOffset : endOffset - localsEndScopeIncValue);
		}

		void AddLocals(MethodDef method, IList<PdbLocal> locals, uint startOffset, uint endOffset) {
			if (locals.Count == 0)
				return;
			uint token = metaData.GetLocalVarSigToken(method);
			if (token == 0) {
				Error("Method {0} ({1:X8}) has no local signature token", method, method.MDToken.Raw);
				return;
			}
			foreach (var local in locals) {
				uint attrs = GetPdbLocalFlags(local.Attributes);
				if (attrs == 0 && local.Name == null)
					continue;
				writer.DefineLocalVariable2(local.Name ?? string.Empty, attrs,
								token, 1, (uint)local.Index, 0, 0, startOffset, endOffset);
			}
		}

		static uint GetPdbLocalFlags(PdbLocalAttributes attributes) {
			if ((attributes & PdbLocalAttributes.DebuggerHidden) != 0)
				return (uint)CorSymVarFlag.VAR_IS_COMP_GEN;
			return 0;
		}

		int GetUserEntryPointToken() {
			var ep = pdbState.UserEntryPoint;
			if (ep == null)
				return 0;
			uint rid = metaData.GetRid(ep);
			if (rid == 0) {
				Error("PDB user entry point method {0} ({1:X8}) is not defined in this module ({2})", ep, ep.MDToken.Raw, module);
				return 0;
			}
			return new MDToken(MD.Table.Method, rid).ToInt32();
		}

		/// <summary>
		/// Gets the <see cref="IMAGE_DEBUG_DIRECTORY"/> and debug data that should be written to
		/// the PE file.
		/// </summary>
		/// <param name="idd">Updated with new values</param>
		/// <returns>Debug data</returns>
		public byte[] GetDebugInfo(out IMAGE_DEBUG_DIRECTORY idd) {
			return writer.GetDebugInfo(out idd);
		}

		/// <summary>
		/// Closes the PDB writer
		/// </summary>
		public void Close() {
			writer.Close();
		}

		ILogger GetLogger() {
			return Logger ?? DummyLogger.ThrowModuleWriterExceptionOnErrorInstance;
		}

		void Error(string message, params object[] args) {
			GetLogger().Log(this, LoggerEvent.Error, message, args);
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (writer != null)
				Close();
			writer = null;
		}
	}
}
