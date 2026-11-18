// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.CodeAnalysis;
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
		readonly SymbolReader? reader;
		// PdbDocument's Equals()/GetHashCode() use properties the user can change at any time, so a
		// PdbDocument is never used as a dictionary key by value here:
		//	- docs: the inserted documents, compared by reference. Add(), Remove() and GetExisting()
		//	  always find an inserted instance, no matter how it's been edited.
		//	- valueIndex: finds an inserted document that's equal to some other instance. The keys are
		//	  immutable copies of the compared values. An entry can be stale if the document was
		//	  edited after it was indexed, so a hit is always checked against the current values.
		//	- readerIndex: finds the document that was created from a SymbolDocument. The keys are
		//	  the original values in the PDB file and don't change when the document is edited.
		readonly Dictionary<PdbDocument, DocumentInfo> docs = new Dictionary<PdbDocument, DocumentInfo>(ReferenceComparer.Instance);
		readonly Dictionary<DocumentKey, PdbDocument> valueIndex = new Dictionary<DocumentKey, PdbDocument>();
		readonly Dictionary<DocumentKey, PdbDocument> readerIndex = new Dictionary<DocumentKey, PdbDocument>();
		internal readonly Dictionary<MDToken, PdbDocument> tokenToDocument = new Dictionary<MDToken, PdbDocument>();
		MethodDef? userEntryPoint;
		readonly Compiler compiler;
		readonly PdbFileKind originalPdbFileKind;
		readonly object readerOriginOwner = new object();

		sealed class ReferenceComparer : IEqualityComparer<PdbDocument> {
			public static readonly ReferenceComparer Instance = new ReferenceComparer();
			public bool Equals(PdbDocument? x, PdbDocument? y) => (object?)x == y;
			public int GetHashCode(PdbDocument obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
		}

		sealed class DocumentInfo {
			public DocumentKey ValueKey;
			public bool HasValueKey;
			public DocumentKey ReaderKey;
			public bool HasReaderKey;
		}

		// Stored in a PdbDocument that was created from the PDB file. Neither field ever changes.
		// Owner is the PdbState's readerOriginOwner and not the PdbState itself, so a document that's
		// moved to another module doesn't keep this PdbState, its reader and its module alive.
		sealed class ReaderOrigin {
			public readonly object Owner;
			public readonly DocumentKey Key;
			public ReaderOrigin(object owner, DocumentKey key) {
				Owner = owner;
				Key = key;
			}
		}

		/// <summary>
		/// The values that <see cref="PdbDocument.Equals(object)"/> compares. It must match that method.
		/// </summary>
		readonly struct DocumentKey : IEquatable<DocumentKey> {
			readonly string url;
			readonly Guid language;
			readonly Guid checkSumAlgorithmId;
			readonly byte[]? checkSum;
			readonly int hashCode;

			public DocumentKey(PdbDocument doc)
				: this(doc.Url, doc.Language, doc.CheckSumAlgorithmId, doc.CheckSum) {
			}

			public DocumentKey(SymbolDocument symDoc)
				: this(symDoc.URL, symDoc.Language, symDoc.CheckSumAlgorithmId, symDoc.CheckSum) {
			}

			DocumentKey(string? url, Guid language, Guid checkSumAlgorithmId, byte[]? checkSum) {
				// null and empty are equal, see PdbDocument.Equals()
				this.url = url ?? string.Empty;
				this.language = language;
				this.checkSumAlgorithmId = checkSumAlgorithmId;
				this.checkSum = checkSum is null || checkSum.Length == 0 ? null : checkSum;
				unchecked {
					hashCode = StringComparer.Ordinal.GetHashCode(this.url) + checkSumAlgorithmId.GetHashCode() +
							Utils.GetHashCode(this.checkSum) + language.GetHashCode();
				}
			}

			/// <summary>
			/// Returns a key that doesn't change if the caller writes to the checksum array. Keys
			/// stored in a dictionary must be created by this method.
			/// </summary>
			public DocumentKey ToImmutable() =>
				checkSum is null ? this : new DocumentKey(url, language, checkSumAlgorithmId, (byte[])checkSum.Clone());

			public bool Equals(DocumentKey other) =>
				hashCode == other.hashCode &&
				StringComparer.Ordinal.Equals(url, other.url) &&
				checkSumAlgorithmId == other.checkSumAlgorithmId &&
				Utils.Equals(checkSum, other.checkSum) &&
				language == other.language;

			public override bool Equals(object? obj) => obj is DocumentKey other && Equals(other);
			public override int GetHashCode() => hashCode;
		}

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
		public MethodDef? UserEntryPoint {
			get => userEntryPoint;
			set => userEntryPoint = value;
		}

		/// <summary>
		/// Gets all PDB documents
		/// </summary>
		public IEnumerable<PdbDocument> Documents {
			get {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
					return new List<PdbDocument>(docs.Keys);
				}
				finally { theLock.ExitWriteLock(); }
#else
				return docs.Keys;
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
					return docs.Count > 0;
#if THREAD_SAFE
				}
				finally { theLock.ExitWriteLock(); }
#endif

			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="pdbFileKind">PDB file kind</param>
		public PdbState(ModuleDef module, PdbFileKind pdbFileKind) {
			if (module is null)
				throw new ArgumentNullException(nameof(module));
			compiler = CalculateCompiler(module);
			PdbFileKind = pdbFileKind;
			originalPdbFileKind = pdbFileKind;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">A <see cref="SymbolReader"/> instance</param>
		/// <param name="module">Owner module</param>
		public PdbState(SymbolReader reader, ModuleDefMD module) {
			if (module is null)
				throw new ArgumentNullException(nameof(module));
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			reader.Initialize(module);
			PdbFileKind = reader.PdbFileKind;
			originalPdbFileKind = reader.PdbFileKind;
			compiler = CalculateCompiler(module);

			userEntryPoint = module.ResolveToken(reader.UserEntryPoint) as MethodDef;

			var documents = reader.Documents;
			int count = documents.Count;
			for (int i = 0; i < count; i++)
				Add_NoLock(documents[i]);
		}

		/// <summary>
		/// Adds <paramref name="doc"/>
		/// </summary>
		/// <param name="doc">New document</param>
		/// <returns><paramref name="doc"/> if it wasn't inserted, or the already existing document
		/// if it was already inserted.</returns>
		/// <remarks>An equal document (see <see cref="PdbDocument.Equals(object)"/>) that was changed
		/// after it was inserted isn't always found. <paramref name="doc"/> is then inserted too, and
		/// equal documents are written as one document when the PDB file is saved.</remarks>
		public PdbDocument Add(PdbDocument doc) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
				return Add_NoLock(doc);
#if THREAD_SAFE
			}
			finally { theLock.ExitWriteLock(); }
#endif
		}

		PdbDocument Add_NoLock(PdbDocument doc) {
			if (doc is null)
				throw new ArgumentNullException(nameof(doc));
			if (docs.ContainsKey(doc))
				return doc;
			var key = new DocumentKey(doc);
			var existing = FindEqual_NoLock(key);
			if (existing is not null)
				return existing;
			Insert_NoLock(doc, key);
			return doc;
		}

		PdbDocument Add_NoLock(SymbolDocument symDoc) {
			// A document read from a Portable PDB is identified by its Document row. Look it up by
			// token first, so that editing its Url after loading doesn't make the lazy lookup from
			// AddSequencePoints miss it and add the same row a second time (see #566).
			if (symDoc.MDToken.HasValue && tokenToDocument.TryGetValue(symDoc.MDToken.Value, out var existing))
				return existing;
			// Windows PDB documents don't have a token and the reader can return a new instance every
			// time, so they're found by their original values. Editing the document doesn't change this key.
			var key = new DocumentKey(symDoc);
			if (readerIndex.TryGetValue(key, out existing))
				return existing;
			// The document was removed, or it's the first time we see it. Use an equal document if the
			// user has added one.
			existing = FindEqual_NoLock(key);
			if (existing is not null)
				return existing;
			// Expensive part, can read source code etc
			var doc = new PdbDocument(symDoc);
			doc.readerOrigin = new ReaderOrigin(readerOriginOwner, key.ToImmutable());
			Insert_NoLock(doc, key);
			return doc;
		}

		void Insert_NoLock(PdbDocument doc, DocumentKey key) {
			var info = new DocumentInfo();
			docs.Add(doc, info);
			AddToValueIndex_NoLock(doc, info, key.ToImmutable());
			// Only documents created from this module's PDB are found by their original values and
			// by token, including when they're removed and added back. Any other document can be from
			// another module, and its token is then some other document in this PDB.
			if (doc.readerOrigin is ReaderOrigin origin && origin.Owner == readerOriginOwner) {
				// Not free if the method bodies were read while the document was removed
				if (!readerIndex.ContainsKey(origin.Key)) {
					readerIndex.Add(origin.Key, doc);
					info.ReaderKey = origin.Key;
					info.HasReaderKey = true;
				}
				if (doc.MDToken.HasValue && !tokenToDocument.ContainsKey(doc.MDToken.Value))
					tokenToDocument.Add(doc.MDToken.Value, doc);
			}
		}

		void AddToValueIndex_NoLock(PdbDocument doc, DocumentInfo info, DocumentKey immutableKey) {
			// If another inserted document has the same values, that one stays in the index
			if (valueIndex.ContainsKey(immutableKey))
				return;
			valueIndex.Add(immutableKey, doc);
			info.ValueKey = immutableKey;
			info.HasValueKey = true;
		}

		void RemoveFromValueIndex_NoLock(DocumentInfo info) {
			if (!info.HasValueKey)
				return;
			valueIndex.Remove(info.ValueKey);
			info.ValueKey = default;
			info.HasValueKey = false;
		}

		/// <summary>
		/// Finds an inserted document whose current values equal <paramref name="key"/>
		/// </summary>
		PdbDocument? FindEqual_NoLock(DocumentKey key) {
			if (!valueIndex.TryGetValue(key, out var doc))
				return null;
			var currentKey = new DocumentKey(doc);
			if (currentKey.Equals(key))
				return doc;
			// The document was edited after it was indexed. Index its current values instead.
			var info = docs[doc];
			RemoveFromValueIndex_NoLock(info);
			AddToValueIndex_NoLock(doc, info, currentKey.ToImmutable());
			return null;
		}

		/// <summary>
		/// Removes <paramref name="doc"/>
		/// </summary>
		/// <param name="doc">Document</param>
		/// <returns><c>true</c> if it was removed, <c>false</c> if it wasn't inserted.</returns>
		/// <remarks>This doesn't remove the document from the PDB file. A document that's still used by
		/// a sequence point is written anyway, including one that's created again from the PDB file when
		/// a method body is read after this call. To remove a document from the PDB file, also remove the
		/// sequence points that use it.</remarks>
		public bool Remove(PdbDocument doc) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
				// doc may be an equal copy of the stored document
				var stored = GetExisting_NoLock(doc);
				if (stored is null)
					return false;
				var info = docs[stored];
				docs.Remove(stored);
				RemoveFromValueIndex_NoLock(info);
				if (info.HasReaderKey)
					readerIndex.Remove(info.ReaderKey);
				if (stored.MDToken.HasValue && tokenToDocument.TryGetValue(stored.MDToken.Value, out var tokenDoc) && tokenDoc == (object)stored)
					tokenToDocument.Remove(stored.MDToken.Value);
				return true;
#if THREAD_SAFE
			}
			finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Returns an inserted <see cref="PdbDocument"/> instance or <c>null</c> if it's not been
		/// inserted yet.
		/// </summary>
		/// <param name="doc">A PDB document</param>
		/// <returns>The existing <see cref="PdbDocument"/> or <c>null</c> if it doesn't exist.</returns>
		/// <remarks>An inserted instance is always found. An equal document (see
		/// <see cref="PdbDocument.Equals(object)"/>) that was changed after it was inserted isn't
		/// always found.</remarks>
		public PdbDocument? GetExisting(PdbDocument doc) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
				return GetExisting_NoLock(doc);
#if THREAD_SAFE
			}
			finally { theLock.ExitWriteLock(); }
#endif
		}

		PdbDocument? GetExisting_NoLock(PdbDocument doc) {
			if (doc is null)
				throw new ArgumentNullException(nameof(doc));
			if (docs.ContainsKey(doc))
				return doc;
			return FindEqual_NoLock(new DocumentKey(doc));
		}

		/// <summary>
		/// Removes all documents
		/// </summary>
		/// <returns></returns>
		/// <remarks>This doesn't remove the documents from the PDB file. A document that's still used by
		/// a sequence point is written anyway, including one that's created again from the PDB file when
		/// a method body is read after this call. To remove a document from the PDB file, also remove the
		/// sequence points that use it.</remarks>
		public void RemoveAllDocuments() => RemoveAllDocuments(false);

		/// <summary>
		/// Removes all documents and optionally returns them
		/// </summary>
		/// <param name="returnDocs"><c>true</c> if all the original <see cref="PdbDocument"/>s
		/// should be returned.</param>
		/// <returns>All <see cref="PdbDocument"/>s if <paramref name="returnDocs"/> is <c>true</c>
		/// or <c>null</c> if <paramref name="returnDocs"/> is <c>false</c>.</returns>
		/// <remarks>This doesn't remove the documents from the PDB file. A document that's still used by
		/// a sequence point is written anyway, including one that's created again from the PDB file when
		/// a method body is read after this call. To remove a document from the PDB file, also remove the
		/// sequence points that use it.</remarks>
		public List<PdbDocument>? RemoveAllDocuments(bool returnDocs) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
				var result = returnDocs ? new List<PdbDocument>(docs.Keys) : null;
				tokenToDocument.Clear();
				valueIndex.Clear();
				readerIndex.Clear();
				docs.Clear();
				return result;
#if THREAD_SAFE
			}
			finally { theLock.ExitWriteLock(); }
#endif
		}

		internal Compiler Compiler => compiler;

		internal void InitializeMethodBody(ModuleDefMD module, MethodDef ownerMethod, CilBody body) {
			if (reader is null)
				return;

			var method = reader.GetMethod(ownerMethod, 1);
			if (method is not null) {
				var pdbMethod = new PdbMethod();
				bool sorted = AreOffsetsSorted(body.Instructions);
				pdbMethod.Scope = CreateScope(module, GenericParamContext.Create(ownerMethod), body, sorted, method.RootScope);
				AddSequencePoints(body, sorted, method);
				body.PdbMethod = pdbMethod;
			}
		}

		internal void InitializeCustomDebugInfos(MethodDef ownerMethod, CilBody body, IList<PdbCustomDebugInfo> customDebugInfos) {
			if (reader is null)
				return;

			var method = reader.GetMethod(ownerMethod, 1);
			if (method is not null)
				method.GetCustomDebugInfos(ownerMethod, body, customDebugInfos);
		}

		static Compiler CalculateCompiler(ModuleDef module) {
			if (module is null)
				return Compiler.Other;

			foreach (var asmRef in module.GetAssemblyRefs()) {
				if (asmRef.Name == nameAssemblyVisualBasic || asmRef.Name == nameAssemblyVisualBasicCore)
					return Compiler.VisualBasic;
			}

			// Disable this for now, we shouldn't be resolving types this early since we could be called by the ModuleDefMD ctor
#if false
			// The VB runtime can also be embedded, and if so, it seems that "Microsoft.VisualBasic.Embedded"
			// attribute is added to the assembly's custom attributes.
			var asm = module.Assembly;
			if (asm is not null && asm.CustomAttributes.IsDefined("Microsoft.VisualBasic.Embedded"))
				return Compiler.VisualBasic;
#endif

			return Compiler.Other;
		}
		static readonly UTF8String nameAssemblyVisualBasic = new UTF8String("Microsoft.VisualBasic");
		// .NET Core 3.0 has this assembly because Microsoft.VisualBasic contains WinForms refs
		static readonly UTF8String nameAssemblyVisualBasicCore = new UTF8String("Microsoft.VisualBasic.Core");

		void AddSequencePoints(CilBody body, bool sorted, SymbolMethod method) {
			int instrIndex = 0;
			var sequencePoints = method.SequencePoints;
			int count = sequencePoints.Count;
			for (int i = 0; i < count; i++) {
				var sp = sequencePoints[i];
				var instr = GetInstruction(body.Instructions, sorted, sp.Offset, ref instrIndex);
				if (instr is null)
					continue;
				var seqPoint = new SequencePoint(Add_NoLock(sp.Document), sp.Line, sp.Column, sp.EndLine, sp.EndColumn);
				instr.SequencePoint = seqPoint;
			}
		}

		struct CreateScopeState {
			public SymbolScope SymScope;
			public PdbScope PdbScope;
			public IList<SymbolScope> Children;
			public int ChildrenIndex;
		}

		PdbScope? CreateScope(ModuleDefMD module, GenericParamContext gpContext, CilBody body, bool sorted, SymbolScope? symScope) {
			if (symScope is null)
				return null;

			// Don't use recursive calls
			var stack = new Stack<CreateScopeState>();
			var state = new CreateScopeState() { SymScope = symScope };
			int endIsInclusiveValue = PdbUtils.IsEndInclusive(originalPdbFileKind, Compiler) ? 1 : 0;
recursive_call:
			int instrIndex = 0;
			state.PdbScope = new PdbScope() {
				Start = GetInstruction(body.Instructions, sorted, state.SymScope.StartOffset, ref instrIndex),
				End = GetInstruction(body.Instructions, sorted, state.SymScope.EndOffset + endIsInclusiveValue, ref instrIndex),
			};
			var cdis = state.SymScope.CustomDebugInfos;
			int count = cdis.Count;
			for (int i = 0; i < count; i++)
				state.PdbScope.CustomDebugInfos.Add(cdis[i]);

			var locals = state.SymScope.Locals;
			count = locals.Count;
			for (int i = 0; i < count; i++) {
				var symLocal = locals[i];
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
				cdis = symLocal.CustomDebugInfos;
				int count2 = cdis.Count;
				for (int j = 0; j < count2; j++)
					pdbLocal.CustomDebugInfos.Add(cdis[j]);
				state.PdbScope.Variables.Add(pdbLocal);
			}

			var namespaces = state.SymScope.Namespaces;
			count = namespaces.Count;
			for (int i = 0; i < count; i++)
				state.PdbScope.Namespaces.Add(namespaces[i].Name);
			state.PdbScope.ImportScope = state.SymScope.ImportScope;

			var constants = state.SymScope.GetConstants(module, gpContext);
			for (int i = 0; i < constants.Count; i++) {
				var constant = constants[i];
				var type = constant.Type.RemovePinnedAndModifiers();
				if (type is not null) {
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
						break;
					case ElementType.ValueType:
						// Microsoft.DiaSymReader.Native returns a DateTime constant as a VT_DATE, whose 8 bytes are
						// the tick count (written by Roslyn). Our COM reader returns those bytes as a double.
						if (constant.Value is double && IsSystemDateTime(((ValueTypeSig)type).TypeDefOrRef)) {
							long ticks = BitConverter.DoubleToInt64Bits((double)constant.Value);
							if ((ulong)ticks <= (ulong)DateTime.MaxValue.Ticks)
								constant.Value = new DateTime(ticks);
						}
						break;
					case ElementType.String:
						// Use the kind of the PDB that was read, PdbFileKind can be changed to write another kind
						if (originalPdbFileKind == PdbFileKind.WindowsPDB) {
							// "" is stored as null, and null is stored as (int)0
							if (constant.Value is int && (int)constant.Value == 0)
								constant.Value = null;
							else if (constant.Value is null)
								constant.Value = string.Empty;
						}
						else
							Debug.Assert(originalPdbFileKind == PdbFileKind.PortablePDB || originalPdbFileKind == PdbFileKind.EmbeddedPortablePDB);
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
						if (gp is not null) {
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

		static bool AreOffsetsSorted(IList<Instruction> instrs) {
			int count = instrs.Count;
			for (int i = 1; i < count; i++) {
				if (instrs[i - 1].Offset >= instrs[i].Offset)
					return false;
			}
			return true;
		}

		static Instruction? GetInstruction(IList<Instruction> instrs, bool sorted, int offset, ref int index) {
			// A body read from the file has sorted offsets, but a body from an IMethodDecrypter might not.
			// Use a binary search when they're sorted so a method with many scopes or out of order
			// sequence points doesn't scan the whole body for each one.
			if (sorted) {
				int lo = 0, hi = instrs.Count - 1;
				while (lo <= hi) {
					int i = lo + (hi - lo) / 2;
					var instr = instrs[i];
					if (instr.Offset == offset)
						return instr;
					if (offset < instr.Offset)
						hi = i - 1;
					else
						lo = i + 1;
				}
				return null;
			}

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
			reader?.GetCustomDebugInfos(token.ToInt32(), gpContext, result);
		}

		internal void Dispose() => reader?.Dispose();

		static bool IsSystemDateTime(ITypeDefOrRef? tdr) =>
			tdr is not null && tdr.Name == "DateTime" && tdr.Namespace == "System" && tdr.DefinitionAssembly.IsCorLib();
	}

	enum Compiler {
		Other,
		VisualBasic,
	}
}
