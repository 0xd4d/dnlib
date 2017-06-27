// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using dnlib.DotNet.Emit;
using dnlib.Threading;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// PDB state for a <see cref="ModuleDef"/>
	/// </summary>
	public sealed class PdbState {
		readonly ISymbolReader reader;
		readonly Dictionary<PdbDocument, PdbDocument> docDict = new Dictionary<PdbDocument, PdbDocument>();
		MethodDef userEntryPoint;
		Compiler compiler;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <summary>
		/// Gets/sets the user entry point method.
		/// </summary>
		public MethodDef UserEntryPoint {
			get { return userEntryPoint; }
			set { userEntryPoint = value; }
		}

		/// <summary>
		/// Gets all PDB documents
		/// </summary>
		public IEnumerable<PdbDocument> Documents {
			get {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
					return new List<PdbDocument>(docDict.Values);
				} finally { theLock.ExitWriteLock(); }
#else
				return docDict.Values;
#endif
			}
		}

		/// <summary>
		/// <c>true</c> if <see cref="Documents"/> is not empty
		/// </summary>
		public bool HasDocuments {
			get {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				return docDict.Count > 0;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
		
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		[Obsolete("Use PdbState(ModuleDef) constructor")]
		public PdbState() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		public PdbState(ModuleDef module) {
			if (module == null)
				throw new ArgumentNullException("module");
			this.compiler = CalculateCompiler(module);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">A <see cref="ISymbolReader"/> instance</param>
		/// <param name="module">Owner module</param>
		public PdbState(ISymbolReader reader, ModuleDefMD module) {
			if (reader == null)
				throw new ArgumentNullException("reader");
			if (module == null)
				throw new ArgumentNullException("module");
			this.reader = reader;
			this.compiler = CalculateCompiler(module);

			this.userEntryPoint = module.ResolveToken(reader.UserEntryPoint.GetToken()) as MethodDef;

			foreach (var doc in reader.GetDocuments())
				Add_NoLock(new PdbDocument(doc));
		}

		/// <summary>
		/// Adds <paramref name="doc"/>
		/// </summary>
		/// <param name="doc">New document</param>
		/// <returns><paramref name="doc"/> if it wasn't inserted, or the already existing document
		/// if it was already inserted.</returns>
		public PdbDocument Add(PdbDocument doc) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			return Add_NoLock(doc);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		PdbDocument Add_NoLock(PdbDocument doc) {
			PdbDocument orig;
			if (docDict.TryGetValue(doc, out orig))
				return orig;
			docDict.Add(doc, doc);
			return doc;
		}

		/// <summary>
		/// Removes <paramref name="doc"/>
		/// </summary>
		/// <param name="doc">Document</param>
		/// <returns><c>true</c> if it was removed, <c>false</c> if it wasn't inserted.</returns>
		public bool Remove(PdbDocument doc) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			return docDict.Remove(doc);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Returns an inserted <see cref="PdbDocument"/> instance or <c>null</c> if it's not been
		/// inserted yet.
		/// </summary>
		/// <param name="doc">A PDB document</param>
		/// <returns>The existing <see cref="PdbDocument"/> or <c>null</c> if it doesn't exist.</returns>
		public PdbDocument GetExisting(PdbDocument doc) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			PdbDocument orig;
			docDict.TryGetValue(doc, out orig);
			return orig;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Removes all documents
		/// </summary>
		/// <returns></returns>
		public void RemoveAllDocuments() {
			RemoveAllDocuments(false);
		}

		/// <summary>
		/// Removes all documents and optionally returns them
		/// </summary>
		/// <param name="returnDocs"><c>true</c> if all the original <see cref="PdbDocument"/>s
		/// should be returned.</param>
		/// <returns>All <see cref="PdbDocument"/>s if <paramref name="returnDocs"/> is <c>true</c>
		/// or <c>null</c> if <paramref name="returnDocs"/> is <c>false</c>.</returns>
		public List<PdbDocument> RemoveAllDocuments(bool returnDocs) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var docs = returnDocs ? new List<PdbDocument>(docDict.Values) : null;
			docDict.Clear();
			return docs;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Initializes a <see cref="CilBody"/> with information found in the PDB file. The
		/// instructions in <paramref name="body"/> must have valid offsets. This method is
		/// automatically called by <see cref="ModuleDefMD"/> and you don't need to explicitly call
		/// it.
		/// </summary>
		/// <param name="body">Method body</param>
		/// <param name="methodRid">Method row ID</param>
		[Obsolete("Don't use this method, the body gets initialied by dnlib", true)]
		public void InitializeDontCall(CilBody body, uint methodRid) {
			InitializeMethodBody(null, null, body, methodRid);
		}

		internal Compiler GetCompiler(ModuleDef module) {
			if (compiler == Compiler.Unknown)
				compiler = CalculateCompiler(module);
			return compiler;
		}

		internal void InitializeMethodBody(ModuleDefMD module, MethodDef ownerMethod, CilBody body, uint methodRid) {
			Debug.Assert((module == null) == (ownerMethod == null));
			if (reader == null || body == null)
				return;

			var token = new SymbolToken((int)(0x06000000 + methodRid));
			ISymbolMethod method;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			method = reader.GetMethod(token);
			if (method != null) {
				var pdbMethod = new PdbMethod();
				pdbMethod.Scope = CreateScope(module, ownerMethod == null ? new GenericParamContext() : GenericParamContext.Create(ownerMethod), body, method.RootScope);
				AddSequencePoints(body, method);

				var method2 = method as ISymbolMethod2;
				Debug.Assert(method2 != null);
				if (module != null && method2 != null && method2.IsAsyncMethod)
					pdbMethod.AsyncMethod = CreateAsyncMethod(module, ownerMethod, body, method2);

				if (ownerMethod != null) {
					// Read the custom debug info last so eg. local names have been initialized
					var cdiData = reader.GetSymAttribute(token, "MD2");
					if (cdiData != null && cdiData.Length != 0)
						PdbCustomDebugInfoReader.Read(ownerMethod, body, pdbMethod.CustomDebugInfos, cdiData);
				}

				body.PdbMethod = pdbMethod;
			}
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		Compiler CalculateCompiler(ModuleDef module) {
			if (module == null)
				return Compiler.Other;

			foreach (var asmRef in module.GetAssemblyRefs()) {
				if (asmRef.Name == nameAssemblyVisualBasic)
					return Compiler.VisualBasic;
			}

			// The VB runtime can also be embedded, and if so, it seems that "Microsoft.VisualBasic.Embedded"
			// attribute is added to the assembly's custom attributes.
			var asm = module.Assembly;
			if (asm != null && asm.CustomAttributes.IsDefined("Microsoft.VisualBasic.Embedded"))
				return Compiler.VisualBasic;

			return Compiler.Other;
		}
		static readonly UTF8String nameAssemblyVisualBasic = new UTF8String("Microsoft.VisualBasic");

		PdbAsyncMethod CreateAsyncMethod(ModuleDefMD module, MethodDef method, CilBody body, ISymbolMethod2 symMethod) {
			var kickoffToken = new MDToken(symMethod.KickoffMethod);
			if (kickoffToken.Table != MD.Table.Method)
				return null;
			var kickoffMethod = module.ResolveMethod(kickoffToken.Rid);

			var rawStepInfos = symMethod.GetAsyncStepInfos();

			var asyncMethod = new PdbAsyncMethod(rawStepInfos.Length);
			asyncMethod.KickoffMethod = kickoffMethod;

			var catchHandlerILOffset = symMethod.CatchHandlerILOffset;
			if (catchHandlerILOffset != null) {
				asyncMethod.CatchHandlerInstruction = GetInstruction(body, catchHandlerILOffset.Value);
				Debug.Assert(asyncMethod.CatchHandlerInstruction != null);
			}

			foreach (var rawInfo in rawStepInfos) {
				var yieldInstruction = GetInstruction(body, rawInfo.YieldOffset);
				Debug.Assert(yieldInstruction != null);
				if (yieldInstruction == null)
					continue;
				MethodDef breakpointMethod;
				Instruction breakpointInstruction;
				if (method.MDToken.Raw == rawInfo.BreakpointMethod) {
					breakpointMethod = method;
					breakpointInstruction = GetInstruction(body, rawInfo.BreakpointOffset);
				}
				else {
					var breakpointMethodToken = new MDToken(rawInfo.BreakpointMethod);
					Debug.Assert(breakpointMethodToken.Table == MD.Table.Method);
					if (breakpointMethodToken.Table != MD.Table.Method)
						continue;
					breakpointMethod = module.ResolveMethod(breakpointMethodToken.Rid);
					Debug.Assert(breakpointMethod != null);
					if (breakpointMethod == null)
						continue;
					breakpointInstruction = GetInstruction(breakpointMethod.Body, rawInfo.BreakpointOffset);
				}
				Debug.Assert(breakpointInstruction != null);
				if (breakpointInstruction == null)
					continue;

				asyncMethod.StepInfos.Add(new PdbAsyncStepInfo(yieldInstruction, breakpointMethod, breakpointInstruction));
			}

			return asyncMethod;
		}

		Instruction GetInstruction(CilBody body, uint offset) {
			if (body == null)
				return null;
			var instructions = body.Instructions;
			int lo = 0, hi = instructions.Count - 1;
			while (lo <= hi && hi != -1) {
				int i = (lo + hi) / 2;
				var instr = instructions[i];
				if (instr.Offset == offset)
					return instr;
				if (offset < instr.Offset)
					hi = i - 1;
				else
					lo = i + 1;
			}
			return null;
		}

		void AddSequencePoints(CilBody body, ISymbolMethod method) {
			int numSeqs = method.SequencePointCount;
			var offsets = new int[numSeqs];
			var documents = new ISymbolDocument[numSeqs];
			var lines = new int[numSeqs];
			var columns = new int[numSeqs];
			var endLines = new int[numSeqs];
			var endColumns = new int[numSeqs];
			method.GetSequencePoints(offsets, documents, lines, columns, endLines, endColumns);

			int instrIndex = 0;
			for (int i = 0; i < numSeqs; i++) {
				var instr = GetInstruction(body.Instructions, offsets[i], ref instrIndex);
				if (instr == null)
					continue;
				var seqPoint = new SequencePoint() {
					Document = Add_NoLock(new PdbDocument(documents[i])),
					StartLine = lines[i],
					StartColumn = columns[i],
					EndLine = endLines[i],
					EndColumn = endColumns[i],
				};
				instr.SequencePoint = seqPoint;
			}
		}

		struct CreateScopeState {
			public ISymbolScope SymScope;
			public PdbScope PdbScope;
			public ISymbolScope[] Children;
			public int ChildrenIndex;
		}

		PdbScope CreateScope(ModuleDefMD module, GenericParamContext gpContext, CilBody body, ISymbolScope symScope) {
			if (symScope == null)
				return null;

			// Don't use recursive calls
			var stack = new Stack<CreateScopeState>();
			var state = new CreateScopeState() { SymScope = symScope };
recursive_call:
			int instrIndex = 0;
			int endIsInclusiveValue = GetCompiler(module) == Compiler.VisualBasic ? 1 : 0;
			state.PdbScope = new PdbScope() {
				Start = GetInstruction(body.Instructions, state.SymScope.StartOffset, ref instrIndex),
				End   = GetInstruction(body.Instructions, state.SymScope.EndOffset + endIsInclusiveValue, ref instrIndex),
			};

			foreach (var symLocal in state.SymScope.GetLocals()) {
				if (symLocal.AddressKind != SymAddressKind.ILOffset)
					continue;

				int localIndex = symLocal.AddressField1;
				if ((uint)localIndex >= (uint)body.Variables.Count) {
					// VB sometimes creates a PDB local without a metadata local
					continue;
				}
				var local = body.Variables[localIndex];
				local.Name = symLocal.Name;
				var attributes = symLocal.Attributes;
				if (attributes is int)
					local.PdbAttributes = (int)attributes;
				state.PdbScope.Variables.Add(local);
			}

			foreach (var ns in state.SymScope.GetNamespaces())
				state.PdbScope.Namespaces.Add(ns.Name);

			var scope2 = state.SymScope as ISymbolScope2;
			Debug.Assert(scope2 != null);
			if (scope2 != null && module != null) {
				var constants = scope2.GetConstants(module, gpContext);
				for (int i = 0; i < constants.Length; i++) {
					var constant = constants[i];
					var type = constant.Type.RemovePinnedAndModifiers();
					if (type != null) {
						// Fix a few values since they're stored as some other type in the PDB
						switch (type.ElementType) {
						case ElementType.Boolean:
							if (constant.Value is short)
								constant.Value = (short)constant.Value != 0;
							break;
						case ElementType.Char:
							if (constant.Value is ushort)
								constant.Value = (char)(ushort)constant.Value;
							break;
						case ElementType.I1:
							if (constant.Value is short)
								constant.Value = (sbyte)(short)constant.Value;
							break;
						case ElementType.U1:
							if (constant.Value is short)
								constant.Value = (byte)(short)constant.Value;
							break;
						case ElementType.I2:
						case ElementType.U2:
						case ElementType.I4:
						case ElementType.U4:
						case ElementType.I8:
						case ElementType.U8:
						case ElementType.R4:
						case ElementType.R8:
						case ElementType.Void:
						case ElementType.Ptr:
						case ElementType.ByRef:
						case ElementType.TypedByRef:
						case ElementType.I:
						case ElementType.U:
						case ElementType.FnPtr:
						case ElementType.ValueType:
							break;
						case ElementType.String:
							// "" is stored as null, and null is stored as (int)0
							if (constant.Value is int && (int)constant.Value == 0)
								constant.Value = null;
							else if (constant.Value == null)
								constant.Value = string.Empty;
							break;
						case ElementType.Object:
						case ElementType.Class:
						case ElementType.SZArray:
						case ElementType.Array:
						default:
							if (constant.Value is int && (int)constant.Value == 0)
								constant.Value = null;
							break;
						case ElementType.GenericInst:
							var gis = (GenericInstSig)type;
							if (gis.GenericType is ValueTypeSig)
								break;
							goto case ElementType.Class;
						case ElementType.Var:
						case ElementType.MVar:
							var gp = ((GenericSig)type).GenericParam;
							if (gp != null) {
								if (gp.HasNotNullableValueTypeConstraint)
									break;
								if (gp.HasReferenceTypeConstraint)
									goto case ElementType.Class;
							}
							break;
						}
					}
					state.PdbScope.Constants.Add(constant);
				}
			}

			// Here's the now somewhat obfuscated for loop
			state.ChildrenIndex = 0;
			state.Children = state.SymScope.GetChildren();
do_return:
			if (state.ChildrenIndex < state.Children.Length) {
				var child = state.Children[state.ChildrenIndex];
				stack.Push(state);
				state = new CreateScopeState() { SymScope = child };
				goto recursive_call;
			}

			if (stack.Count == 0)
				return state.PdbScope;

			// Return from recursive call, and execute the last part of the for loop
			var newPdbScope = state.PdbScope;
			state = stack.Pop();
			state.PdbScope.Scopes.Add(newPdbScope);
			state.ChildrenIndex++;
			goto do_return;
		}

		static Instruction GetInstruction(IList<Instruction> instrs, int offset, ref int index) {
			if (instrs.Count > 0 && offset > instrs[instrs.Count - 1].Offset)
				return null;
			for (int i = index; i < instrs.Count; i++) {
				var instr = instrs[i];
				if (instr.Offset < offset)
					continue;
				if (instr.Offset == offset) {
					index = i;
					return instr;
				}
				break;
			}
			for (int i = 0; i < index; i++) {
				var instr = instrs[i];
				if (instr.Offset < offset)
					continue;
				if (instr.Offset == offset) {
					index = i;
					return instr;
				}
				break;
			}
			return null;
		}
	}

	enum Compiler {
		Unknown,
		Other,
		VisualBasic,
	}
}
