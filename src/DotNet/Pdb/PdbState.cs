// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.Threading;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// PDB state for a <see cref="ModuleDef"/>
	/// </summary>
	public sealed class PdbState {
		readonly SymbolReader reader;
		readonly Dictionary<PdbDocument, PdbDocument> docDict = new Dictionary<PdbDocument, PdbDocument>();
		MethodDef userEntryPoint;
		readonly Compiler compiler;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <summary>
		/// Gets/sets the PDB file kind. You can change it from portable PDB to embedded portable PDB
		/// and vice versa. Converting a Windows PDB to a portable PDB isn't supported.
		/// </summary>
		public PdbFileKind PdbFileKind { get; set; }

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
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="pdbFileKind">PDB file kind</param>
		public PdbState(ModuleDef module, PdbFileKind pdbFileKind) {
			if (module == null)
				throw new ArgumentNullException("module");
			this.compiler = CalculateCompiler(module);
			PdbFileKind = pdbFileKind;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">A <see cref="SymbolReader"/> instance</param>
		/// <param name="module">Owner module</param>
		public PdbState(SymbolReader reader, ModuleDefMD module) {
			if (reader == null)
				throw new ArgumentNullException("reader");
			if (module == null)
				throw new ArgumentNullException("module");
			this.reader = reader;
			reader.Initialize(module);
			PdbFileKind = reader.PdbFileKind;
			this.compiler = CalculateCompiler(module);

			this.userEntryPoint = module.ResolveToken(reader.UserEntryPoint) as MethodDef;

			foreach (var doc in reader.Documents)
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

		internal Compiler Compiler {
			get { return compiler; }
		}

		internal void InitializeMethodBody(ModuleDefMD module, MethodDef ownerMethod, CilBody body) {
			if (reader == null)
				return;

			var method = reader.GetMethod(ownerMethod, 1);
			if (method != null) {
				var pdbMethod = new PdbMethod();
				pdbMethod.Scope = CreateScope(module, GenericParamContext.Create(ownerMethod), body, method.RootScope);
				AddSequencePoints(body, method);
				body.PdbMethod = pdbMethod;
			}
		}

		internal void InitializeCustomDebugInfos(MethodDef ownerMethod, CilBody body, IList<PdbCustomDebugInfo> customDebugInfos) {
			if (reader == null)
				return;

			var method = reader.GetMethod(ownerMethod, 1);
			if (method != null)
				method.GetCustomDebugInfos(ownerMethod, body, customDebugInfos);
		}

		static Compiler CalculateCompiler(ModuleDef module) {
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

		void AddSequencePoints(CilBody body, SymbolMethod method) {
			int instrIndex = 0;
			foreach (var sp in method.SequencePoints) {
				var instr = GetInstruction(body.Instructions, sp.Offset, ref instrIndex);
				if (instr == null)
					continue;
				var seqPoint = new SequencePoint() {
					Document = Add_NoLock(new PdbDocument(sp.Document)),
					StartLine = sp.Line,
					StartColumn = sp.Column,
					EndLine = sp.EndLine,
					EndColumn = sp.EndColumn,
				};
				instr.SequencePoint = seqPoint;
			}
		}

		struct CreateScopeState {
			public SymbolScope SymScope;
			public PdbScope PdbScope;
			public IList<SymbolScope> Children;
			public int ChildrenIndex;
		}

		PdbScope CreateScope(ModuleDefMD module, GenericParamContext gpContext, CilBody body, SymbolScope symScope) {
			if (symScope == null)
				return null;

			// Don't use recursive calls
			var stack = new Stack<CreateScopeState>();
			var state = new CreateScopeState() { SymScope = symScope };
recursive_call:
			int instrIndex = 0;
			int endIsInclusiveValue = Compiler == Compiler.VisualBasic ? 1 : 0;
			state.PdbScope = new PdbScope() {
				Start = GetInstruction(body.Instructions, state.SymScope.StartOffset, ref instrIndex),
				End   = GetInstruction(body.Instructions, state.SymScope.EndOffset + endIsInclusiveValue, ref instrIndex),
			};
			foreach (var cdi in state.SymScope.CustomDebugInfos)
				state.PdbScope.CustomDebugInfos.Add(cdi);

			foreach (var symLocal in state.SymScope.Locals) {
				int localIndex = symLocal.Index;
				if ((uint)localIndex >= (uint)body.Variables.Count) {
					// VB sometimes creates a PDB local without a metadata local
					continue;
				}
				var local = body.Variables[localIndex];
				var name = symLocal.Name;
				local.SetName(name);
				var attributes = symLocal.Attributes;
				local.SetAttributes(attributes);
				var pdbLocal = new PdbLocal(local, name, attributes);
				foreach (var cdi in symLocal.CustomDebugInfos)
					pdbLocal.CustomDebugInfos.Add(cdi);
				state.PdbScope.Variables.Add(pdbLocal);
			}

			foreach (var ns in state.SymScope.Namespaces)
				state.PdbScope.Namespaces.Add(ns.Name);
			state.PdbScope.ImportScope = state.SymScope.ImportScope;

			var constants = state.SymScope.GetConstants(module, gpContext);
			for (int i = 0; i < constants.Count; i++) {
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
						if (PdbFileKind == PdbFileKind.WindowsPDB) {
							// "" is stored as null, and null is stored as (int)0
							if (constant.Value is int && (int)constant.Value == 0)
								constant.Value = null;
							else if (constant.Value == null)
								constant.Value = string.Empty;
						}
						else
							Debug.Assert(PdbFileKind == PdbFileKind.PortablePDB || PdbFileKind == PdbFileKind.EmbeddedPortablePDB);
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

			// Here's the now somewhat obfuscated for loop
			state.ChildrenIndex = 0;
			state.Children = state.SymScope.Children;
do_return:
			if (state.ChildrenIndex < state.Children.Count) {
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

		internal void InitializeCustomDebugInfos(MDToken token, GenericParamContext gpContext, IList<PdbCustomDebugInfo> result) {
			Debug.Assert(token.Table != Table.Method, "Methods get initialized when reading the method bodies");
			if (reader != null)
				reader.GetCustomDebugInfos(token.ToInt32(), gpContext, result);
		}

		internal void Dispose() {
			if (reader != null)
				reader.Dispose();
		}
	}

	enum Compiler {
		Other,
		VisualBasic,
	}
}
