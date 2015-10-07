// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.MD;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Preserves metadata tokens
	/// </summary>
	sealed class PreserveTokensMetaData : MetaData {
		readonly ModuleDefMD mod;
		readonly Rows<TypeRef> typeRefInfos = new Rows<TypeRef>();
		readonly Dictionary<TypeDef, uint> typeToRid = new Dictionary<TypeDef, uint>();
		MemberDefDict<FieldDef> fieldDefInfos;
		MemberDefDict<MethodDef> methodDefInfos;
		MemberDefDict<ParamDef> paramDefInfos;
		readonly Rows<MemberRef> memberRefInfos = new Rows<MemberRef>();
		readonly Rows<StandAloneSig> standAloneSigInfos = new Rows<StandAloneSig>();
		MemberDefDict<EventDef> eventDefInfos;
		MemberDefDict<PropertyDef> propertyDefInfos;
		readonly Rows<TypeSpec> typeSpecInfos = new Rows<TypeSpec>();
		readonly Rows<MethodSpec> methodSpecInfos = new Rows<MethodSpec>();
		readonly Dictionary<uint, uint> callConvTokenToSignature = new Dictionary<uint, uint>();

		[DebuggerDisplay("{Rid} -> {NewRid} {Def}")]
		sealed class MemberDefInfo<T> where T : IMDTokenProvider {
			public readonly T Def;

			/// <summary>
			/// Its real rid
			/// </summary>
			public uint Rid;

			/// <summary>
			/// Its logical rid or real rid. If the ptr table exists (eg. MethodPtr), then it's
			/// an index into it, else it's the real rid.
			/// </summary>
			public uint NewRid;

			public MemberDefInfo(T def, uint rid) {
				this.Def = def;
				this.Rid = rid;
				this.NewRid = rid;
			}
		}

		[DebuggerDisplay("Count = {Count}")]
		sealed class MemberDefDict<T> where T : IMDTokenProvider {
			readonly Type defMDType;
			uint userRid = 0x01000000;
			uint newRid = 1;
			int numDefMDs;
			int numDefUsers;
			int tableSize;
			bool wasSorted;
			readonly bool preserveRids;
			readonly bool enableRidToInfo;
			readonly Dictionary<T, MemberDefInfo<T>> defToInfo = new Dictionary<T, MemberDefInfo<T>>();
			Dictionary<uint, MemberDefInfo<T>> ridToInfo;
			readonly List<MemberDefInfo<T>> defs = new List<MemberDefInfo<T>>();
			List<MemberDefInfo<T>> sortedDefs;
			readonly Dictionary<T, int> collectionPositions = new Dictionary<T, int>();

			/// <summary>
			/// Gets total number of defs in the list. It does <c>not</c> necessarily return
			/// the table size. Use <see cref="TableSize"/> for that.
			/// </summary>
			public int Count {
				get { return defs.Count; }
			}

			/// <summary>
			/// Gets the number of rows that need to be created in the table
			/// </summary>
			public int TableSize {
				get { return tableSize; }
			}

			/// <summary>
			/// Returns <c>true</c> if the ptr table (eg. <c>MethodPtr</c>) is needed
			/// </summary>
			public bool NeedPtrTable {
				get { return preserveRids && !wasSorted; }
			}

			public MemberDefDict(Type defMDType, bool preserveRids)
				: this(defMDType, preserveRids, false) {
			}

			public MemberDefDict(Type defMDType, bool preserveRids, bool enableRidToInfo) {
				this.defMDType = defMDType;
				this.preserveRids = preserveRids;
				this.enableRidToInfo = enableRidToInfo;
			}

			public uint Rid(T def) {
				return defToInfo[def].Rid;
			}

			public bool TryGetRid(T def, out uint rid) {
				MemberDefInfo<T> info;
				if (def == null || !defToInfo.TryGetValue(def, out info)) {
					rid = 0;
					return false;
				}
				rid = info.Rid;
				return true;
			}

			/// <summary>
			/// Sorts the table
			/// </summary>
			/// <param name="comparer">Comparer</param>
			public void Sort(Comparison<MemberDefInfo<T>> comparer) {
				if (!preserveRids) {
					// It's already sorted
					sortedDefs = defs;
					return;
				}

				sortedDefs = new List<MemberDefInfo<T>>(defs);
				sortedDefs.Sort(comparer);
				wasSorted = true;
				for (int i = 0; i < sortedDefs.Count; i++) {
					var def = sortedDefs[i];
					uint newRid = (uint)i + 1;
					def.NewRid = newRid;
					if (def.Rid != newRid)
						wasSorted = false;
				}
			}

			public MemberDefInfo<T> Get(int i) {
				return defs[i];
			}

			public MemberDefInfo<T> GetSorted(int i) {
				return sortedDefs[i];
			}

			public MemberDefInfo<T> GetByRid(uint rid) {
				MemberDefInfo<T> info;
				ridToInfo.TryGetValue(rid, out info);
				return info;
			}

			/// <summary>
			/// Adds a def. <see cref="SortDefs()"/> must be called after adding the last def.
			/// </summary>
			/// <param name="def">The def</param>
			/// <param name="collPos">Collection position</param>
			public void Add(T def, int collPos) {
				uint rid;
				if (def.GetType() == defMDType) {
					numDefMDs++;
					rid = preserveRids ? def.Rid : newRid++;
				}
				else {
					numDefUsers++;
					rid = preserveRids ? userRid++ : newRid++;
				}

				var info = new MemberDefInfo<T>(def, rid);
				defToInfo[def] = info;
				defs.Add(info);
				collectionPositions.Add(def, collPos);
			}

			/// <summary>
			/// Must be called after <see cref="Add"/>'ing the last def
			/// </summary>
			public void SortDefs() {
				// It's already sorted if we don't preserve rids
				if (preserveRids) {
					// Sort all def MDs before user defs
					defs.Sort((a, b) => a.Rid.CompareTo(b.Rid));

					// Fix user created defs' rids
					uint newRid = numDefMDs == 0 ? 1 : defs[numDefMDs - 1].Rid + 1;
					for (int i = numDefMDs; i < defs.Count; i++)
						defs[i].Rid = newRid++;

					// Now we know total table size
					tableSize = (int)newRid - 1;
				}
				else
					tableSize = defs.Count;

				if (enableRidToInfo) {
					ridToInfo = new Dictionary<uint, MemberDefInfo<T>>(defs.Count);
					foreach (var info in defs)
						ridToInfo.Add(info.Rid, info);
				}

				if ((uint)tableSize > 0x00FFFFFF)
					throw new ModuleWriterException("Table is too big");
			}

			public int GetCollectionPosition(T def) {
				return collectionPositions[def];
			}
		}

		protected override int NumberOfMethods {
			get { return methodDefInfos.Count; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="constants">Constants list</param>
		/// <param name="methodBodies">Method bodies list</param>
		/// <param name="netResources">.NET resources list</param>
		/// <param name="options">Options</param>
		public PreserveTokensMetaData(ModuleDef module, UniqueChunkList<ByteArrayChunk> constants, MethodBodyChunks methodBodies, NetResources netResources, MetaDataOptions options)
			: base(module, constants, methodBodies, netResources, options) {
			mod = module as ModuleDefMD;
			if (mod == null)
				throw new ModuleWriterException("Not a ModuleDefMD");
		}

		/// <inheritdoc/>
		public override uint GetRid(TypeRef tr) {
			uint rid;
			typeRefInfos.TryGetRid(tr, out rid);
			return rid;
		}

		/// <inheritdoc/>
		public override uint GetRid(TypeDef td) {
			if (td == null) {
				Error("TypeDef is null");
				return 0;
			}
			uint rid;
			if (typeToRid.TryGetValue(td, out rid))
				return rid;
			Error("TypeDef {0} ({1:X8}) is not defined in this module ({2}). A type was removed that is still referenced by this module.", td, td.MDToken.Raw, module);
			return 0;
		}

		/// <inheritdoc/>
		public override uint GetRid(FieldDef fd) {
			uint rid;
			if (fieldDefInfos.TryGetRid(fd, out rid))
				return rid;
			if (fd == null)
				Error("Field is null");
			else
				Error("Field {0} ({1:X8}) is not defined in this module ({2}). A field was removed that is still referenced by this module.", fd, fd.MDToken.Raw, module);
			return 0;
		}

		/// <inheritdoc/>
		public override uint GetRid(MethodDef md) {
			uint rid;
			if (methodDefInfos.TryGetRid(md, out rid))
				return rid;
			if (md == null)
				Error("Method is null");
			else
				Error("Method {0} ({1:X8}) is not defined in this module ({2}). A method was removed that is still referenced by this module.", md, md.MDToken.Raw, module);
			return 0;
		}

		/// <inheritdoc/>
		public override uint GetRid(ParamDef pd) {
			uint rid;
			if (paramDefInfos.TryGetRid(pd, out rid))
				return rid;
			if (pd == null)
				Error("Param is null");
			else
				Error("Param {0} ({1:X8}) is not defined in this module ({2}). A parameter was removed that is still referenced by this module.", pd, pd.MDToken.Raw, module);
			return 0;
		}

		/// <inheritdoc/>
		public override uint GetRid(MemberRef mr) {
			uint rid;
			memberRefInfos.TryGetRid(mr, out rid);
			return rid;
		}

		/// <inheritdoc/>
		public override uint GetRid(StandAloneSig sas) {
			uint rid;
			standAloneSigInfos.TryGetRid(sas, out rid);
			return rid;
		}

		/// <inheritdoc/>
		public override uint GetRid(EventDef ed) {
			uint rid;
			if (eventDefInfos.TryGetRid(ed, out rid))
				return rid;
			if (ed == null)
				Error("Event is null");
			else
				Error("Event {0} ({1:X8}) is not defined in this module ({2}). An event was removed that is still referenced by this module.", ed, ed.MDToken.Raw, module);
			return 0;
		}

		/// <inheritdoc/>
		public override uint GetRid(PropertyDef pd) {
			uint rid;
			if (propertyDefInfos.TryGetRid(pd, out rid))
				return rid;
			if (pd == null)
				Error("Property is null");
			else
				Error("Property {0} ({1:X8}) is not defined in this module ({2}). A property was removed that is still referenced by this module.", pd, pd.MDToken.Raw, module);
			return 0;
		}

		/// <inheritdoc/>
		public override uint GetRid(TypeSpec ts) {
			uint rid;
			typeSpecInfos.TryGetRid(ts, out rid);
			return rid;
		}

		/// <inheritdoc/>
		public override uint GetRid(MethodSpec ms) {
			uint rid;
			methodSpecInfos.TryGetRid(ms, out rid);
			return rid;
		}

		/// <inheritdoc/>
		protected override void Initialize() {
			fieldDefInfos = new MemberDefDict<FieldDef>(typeof(FieldDefMD), PreserveFieldRids);
			methodDefInfos = new MemberDefDict<MethodDef>(typeof(MethodDefMD), PreserveMethodRids, true);
			paramDefInfos = new MemberDefDict<ParamDef>(typeof(ParamDefMD), PreserveParamRids);
			eventDefInfos = new MemberDefDict<EventDef>(typeof(EventDefMD), PreserveEventRids);
			propertyDefInfos = new MemberDefDict<PropertyDef>(typeof(PropertyDefMD), PreservePropertyRids);

			CreateEmptyTableRows();
		}

		/// <inheritdoc/>
		protected override List<TypeDef> GetAllTypeDefs() {
			if (!PreserveTypeDefRids) {
				var types2 = new List<TypeDef>(module.GetTypes());
				InitializeTypeToRid(types2);
				return types2;
			}

			var typeToIndex = new Dictionary<TypeDef, uint>();
			var types = new List<TypeDef>();
			uint index = 0;
			const uint IS_TYPEDEFMD = 0x80000000;
			const uint INDEX_BITS = 0x00FFFFFF;
			foreach (var type in module.GetTypes()) {
				if (type == null)
					continue;
				types.Add(type);
				uint val = (uint)index++;
				if (type.GetType() == typeof(TypeDefMD))
					val |= IS_TYPEDEFMD;
				typeToIndex[type] = val;
			}

			var globalType = types[0];
			types.Sort((a, b) => {
				if (a == b)
					return 0;
				// Make sure the global <Module> type is always sorted first, even if it's
				// a TypeDefUser
				if (a == globalType)
					return -1;
				if (b == globalType)
					return 1;

				// Sort all TypeDefMDs before all TypeDefUsers
				uint ai = typeToIndex[a];
				uint bi = typeToIndex[b];
				bool amd = (ai & IS_TYPEDEFMD) != 0;
				bool bmd = (bi & IS_TYPEDEFMD) != 0;
				if (amd == bmd) {	// Both are TypeDefMDs or both are TypeDefUsers
					// If TypeDefMDs, only compare rids since rids are preserved
					if (amd)
						return a.Rid.CompareTo(b.Rid);

					// If TypeDefUsers, rids aren't preserved so compare by index
					return (ai & INDEX_BITS).CompareTo(bi & INDEX_BITS);
				}
				if (amd)
					return -1;
				return 1;
			});

			// Some of the original types may have been removed. Create dummy types
			// so TypeDef rids can be preserved.
			var newTypes = new List<TypeDef>(types.Count);
			uint prevRid = 1;
			newTypes.Add(globalType);
			for (int i = 1; i < types.Count; i++) {
				var type = types[i];

				// TypeDefUsers were sorted last so when we reach one, we can stop
				if (type.GetType() != typeof(TypeDefMD)) {
					while (i < types.Count)
						newTypes.Add(types[i++]);
					break;
				}

				uint currRid = type.Rid;
				int extraTypes = (int)(currRid - prevRid - 1);
				if (extraTypes != 0) { // always >= 0 since currRid > prevRid
					// At least one type has been removed. Create dummy types.
					for (int j = 0; j < extraTypes; j++)
						newTypes.Add(new TypeDefUser("dummy", Guid.NewGuid().ToString("B"), module.CorLibTypes.Object.TypeDefOrRef));
				}
				newTypes.Add(type);
				prevRid = currRid;
			}

			InitializeTypeToRid(newTypes);
			return newTypes;
		}

		void InitializeTypeToRid(IEnumerable<TypeDef> types) {
			uint rid = 1;
			foreach (var type in types) {
				if (type == null)
					continue;
				if (typeToRid.ContainsKey(type))
					continue;
				typeToRid[type] = rid++;
			}
		}

		/// <inheritdoc/>
		protected override void AllocateTypeDefRids() {
			foreach (var type in allTypeDefs) {
				uint rid = tablesHeap.TypeDefTable.Create(new RawTypeDefRow());
				if (typeToRid[type] != rid)
					throw new ModuleWriterException("Got a different rid than expected");
			}
		}

		/// <summary>
		/// Reserves rows in <c>TypeRef</c>, <c>MemberRef</c>, <c>StandAloneSig</c>,
		/// <c>TypeSpec</c> and <c>MethodSpec</c> where we will store the original rows
		/// to make sure they get the same rid. Any user created rows will be stored at
		/// the end of each table.
		/// </summary>
		void CreateEmptyTableRows() {
			uint rows;

			if (PreserveTypeRefRids) {
				rows = mod.TablesStream.TypeRefTable.Rows;
				for (uint i = 0; i < rows; i++)
					tablesHeap.TypeRefTable.Create(new RawTypeRefRow());
			}

			if (PreserveMemberRefRids) {
				rows = mod.TablesStream.MemberRefTable.Rows;
				for (uint i = 0; i < rows; i++)
					tablesHeap.MemberRefTable.Create(new RawMemberRefRow());
			}

			if (PreserveStandAloneSigRids) {
				rows = mod.TablesStream.StandAloneSigTable.Rows;
				for (uint i = 0; i < rows; i++)
					tablesHeap.StandAloneSigTable.Create(new RawStandAloneSigRow());
			}

			if (PreserveTypeSpecRids) {
				rows = mod.TablesStream.TypeSpecTable.Rows;
				for (uint i = 0; i < rows; i++)
					tablesHeap.TypeSpecTable.Create(new RawTypeSpecRow());
			}

			if (PreserveMethodSpecRids) {
				rows = mod.TablesStream.MethodSpecTable.Rows;
				for (uint i = 0; i < rows; i++)
					tablesHeap.MethodSpecTable.Create(new RawMethodSpecRow());
			}
		}

		/// <summary>
		/// Adds any non-referenced rows that haven't been added yet but are present in
		/// the original file. If there are any non-referenced rows, it's usually a sign
		/// that an obfuscator has encrypted one or more methods or that it has added
		/// some rows it uses to decrypt something.
		/// </summary>
		void InitializeUninitializedTableRows() {
			InitializeTypeRefTableRows();
			InitializeMemberRefTableRows();
			InitializeStandAloneSigTableRows();
			InitializeTypeSpecTableRows();
			InitializeMethodSpecTableRows();
		}

		bool initdTypeRef = false;
		void InitializeTypeRefTableRows() {
			if (!PreserveTypeRefRids || initdTypeRef)
				return;
			initdTypeRef = true;

			uint rows = mod.TablesStream.TypeRefTable.Rows;
			for (uint rid = 1; rid <= rows; rid++)
				AddTypeRef(mod.ResolveTypeRef(rid));
			tablesHeap.TypeRefTable.ReAddRows();
		}

		bool initdMemberRef = false;
		void InitializeMemberRefTableRows() {
			if (!PreserveMemberRefRids || initdMemberRef)
				return;
			initdMemberRef = true;

			uint rows = mod.TablesStream.MemberRefTable.Rows;
			for (uint rid = 1; rid <= rows; rid++)
				AddMemberRef(mod.ResolveMemberRef(rid), true);
			tablesHeap.MemberRefTable.ReAddRows();
		}

		bool initdStandAloneSig = false;
		void InitializeStandAloneSigTableRows() {
			if (!PreserveStandAloneSigRids || initdStandAloneSig)
				return;
			initdStandAloneSig = true;

			uint rows = mod.TablesStream.StandAloneSigTable.Rows;
			for (uint rid = 1; rid <= rows; rid++)
				AddStandAloneSig(mod.ResolveStandAloneSig(rid), true);
			tablesHeap.StandAloneSigTable.ReAddRows();
		}

		bool initdTypeSpec = false;
		void InitializeTypeSpecTableRows() {
			if (!PreserveTypeSpecRids || initdTypeSpec)
				return;
			initdTypeSpec = true;

			uint rows = mod.TablesStream.TypeSpecTable.Rows;
			for (uint rid = 1; rid <= rows; rid++)
				AddTypeSpec(mod.ResolveTypeSpec(rid), true);
			tablesHeap.TypeSpecTable.ReAddRows();
		}

		bool initdMethodSpec = false;
		void InitializeMethodSpecTableRows() {
			if (!PreserveMethodSpecRids || initdMethodSpec)
				return;
			initdMethodSpec = true;

			uint rows = mod.TablesStream.MethodSpecTable.Rows;
			for (uint rid = 1; rid <= rows; rid++)
				AddMethodSpec(mod.ResolveMethodSpec(rid), true);
			tablesHeap.MethodSpecTable.ReAddRows();
		}

		/// <inheritdoc/>
		protected override void AllocateMemberDefRids() {
			FindMemberDefs();

			Listener.OnMetaDataEvent(this, MetaDataEvent.AllocateMemberDefRids0);

			for (int i = 1; i <= fieldDefInfos.TableSize; i++) {
				if ((uint)i != tablesHeap.FieldTable.Create(new RawFieldRow()))
					throw new ModuleWriterException("Invalid field rid");
			}

			for (int i = 1; i <= methodDefInfos.TableSize; i++) {
				if ((uint)i != tablesHeap.MethodTable.Create(new RawMethodRow()))
					throw new ModuleWriterException("Invalid method rid");
			}

			for (int i = 1; i <= paramDefInfos.TableSize; i++) {
				if ((uint)i != tablesHeap.ParamTable.Create(new RawParamRow()))
					throw new ModuleWriterException("Invalid param rid");
			}

			for (int i = 1; i <= eventDefInfos.TableSize; i++) {
				if ((uint)i != tablesHeap.EventTable.Create(new RawEventRow()))
					throw new ModuleWriterException("Invalid event rid");
			}

			for (int i = 1; i <= propertyDefInfos.TableSize; i++) {
				if ((uint)i != tablesHeap.PropertyTable.Create(new RawPropertyRow()))
					throw new ModuleWriterException("Invalid property rid");
			}

			SortFields();
			SortMethods();
			SortParameters();
			SortEvents();
			SortProperties();

			Listener.OnMetaDataEvent(this, MetaDataEvent.AllocateMemberDefRids1);

			if (fieldDefInfos.NeedPtrTable) {
				for (int i = 0; i < fieldDefInfos.Count; i++) {
					var info = fieldDefInfos.GetSorted(i);
					if ((uint)i + 1 != tablesHeap.FieldPtrTable.Add(new RawFieldPtrRow(info.Rid)))
						throw new ModuleWriterException("Invalid field ptr rid");
				}
				ReUseDeletedFieldRows();
			}

			if (methodDefInfos.NeedPtrTable) {
				for (int i = 0; i < methodDefInfos.Count; i++) {
					var info = methodDefInfos.GetSorted(i);
					if ((uint)i + 1 != tablesHeap.MethodPtrTable.Add(new RawMethodPtrRow(info.Rid)))
						throw new ModuleWriterException("Invalid method ptr rid");
				}
				ReUseDeletedMethodRows();
			}

			if (paramDefInfos.NeedPtrTable) {
				// NOTE: peverify does not support the ParamPtr table. It's a bug.
				for (int i = 0; i < paramDefInfos.Count; i++) {
					var info = paramDefInfos.GetSorted(i);
					if ((uint)i + 1 != tablesHeap.ParamPtrTable.Add(new RawParamPtrRow(info.Rid)))
						throw new ModuleWriterException("Invalid param ptr rid");
				}
				ReUseDeletedParamRows();
			}

			if (eventDefInfos.NeedPtrTable) {
				for (int i = 0; i < eventDefInfos.Count; i++) {
					var info = eventDefInfos.GetSorted(i);
					if ((uint)i + 1 != tablesHeap.EventPtrTable.Add(new RawEventPtrRow(info.Rid)))
						throw new ModuleWriterException("Invalid event ptr rid");
				}
			}

			if (propertyDefInfos.NeedPtrTable) {
				for (int i = 0; i < propertyDefInfos.Count; i++) {
					var info = propertyDefInfos.GetSorted(i);
					if ((uint)i + 1 != tablesHeap.PropertyPtrTable.Add(new RawPropertyPtrRow(info.Rid)))
						throw new ModuleWriterException("Invalid property ptr rid");
				}
			}

			Listener.OnMetaDataEvent(this, MetaDataEvent.AllocateMemberDefRids2);

			InitializeMethodAndFieldList();
			InitializeParamList();
			InitializeEventMap();
			InitializePropertyMap();

			Listener.OnMetaDataEvent(this, MetaDataEvent.AllocateMemberDefRids3);

			// We must re-use deleted event/property rows after we've initialized
			// the event/prop map tables.
			if (eventDefInfos.NeedPtrTable)
				ReUseDeletedEventRows();
			if (propertyDefInfos.NeedPtrTable)
				ReUseDeletedPropertyRows();

			Listener.OnMetaDataEvent(this, MetaDataEvent.AllocateMemberDefRids4);

			InitializeTypeRefTableRows();
			InitializeTypeSpecTableRows();
			InitializeMemberRefTableRows();
			InitializeMethodSpecTableRows();
		}

		/// <summary>
		/// Re-uses all <c>Field</c> rows which aren't owned by any type due to the fields
		/// having been deleted by the user. The reason we must do this is that the
		/// <c>FieldPtr</c> and <c>Field</c> tables must be the same size.
		/// </summary>
		void ReUseDeletedFieldRows() {
			if (tablesHeap.FieldPtrTable.IsEmpty)
				return;
			if (fieldDefInfos.TableSize == tablesHeap.FieldPtrTable.Rows)
				return;

			var hasOwner = new bool[fieldDefInfos.TableSize];
			for (int i = 0; i < fieldDefInfos.Count; i++)
				hasOwner[(int)fieldDefInfos.Get(i).Rid - 1] = true;

			CreateDummyPtrTableType();

			uint fieldSig = GetSignature(new FieldSig(module.CorLibTypes.Byte));
			for (int i = 0; i < hasOwner.Length; i++) {
				if (hasOwner[i])
					continue;
				uint frid = (uint)i + 1;

				var frow = tablesHeap.FieldTable[frid];
				frow.Flags = (ushort)(FieldAttributes.Public | FieldAttributes.Static);
				frow.Name = stringsHeap.Add(string.Format("f{0:X6}", frid));
				frow.Signature = fieldSig;
				tablesHeap.FieldPtrTable.Create(new RawFieldPtrRow(frid));
			}

			if (fieldDefInfos.TableSize != tablesHeap.FieldPtrTable.Rows)
				throw new ModuleWriterException("Didn't create all dummy fields");
		}

		/// <summary>
		/// Re-uses all <c>Method</c> rows which aren't owned by any type due to the methods
		/// having been deleted by the user. The reason we must do this is that the
		/// <c>MethodPtr</c> and <c>Method</c> tables must be the same size.
		/// </summary>
		void ReUseDeletedMethodRows() {
			if (tablesHeap.MethodPtrTable.IsEmpty)
				return;
			if (methodDefInfos.TableSize == tablesHeap.MethodPtrTable.Rows)
				return;

			var hasOwner = new bool[methodDefInfos.TableSize];
			for (int i = 0; i < methodDefInfos.Count; i++)
				hasOwner[(int)methodDefInfos.Get(i).Rid - 1] = true;

			CreateDummyPtrTableType();

			uint methodSig = GetSignature(MethodSig.CreateInstance(module.CorLibTypes.Void));
			for (int i = 0; i < hasOwner.Length; i++) {
				if (hasOwner[i])
					continue;
				uint mrid = (uint)i + 1;

				var mrow = tablesHeap.MethodTable[mrid];
				mrow.RVA = 0;
				mrow.ImplFlags = (ushort)(MethodImplAttributes.IL | MethodImplAttributes.Managed);
				mrow.Flags = (ushort)(MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Abstract);
				mrow.Name = stringsHeap.Add(string.Format("m{0:X6}", mrid));
				mrow.Signature = methodSig;
				mrow.ParamList = (uint)paramDefInfos.Count;
				tablesHeap.MethodPtrTable.Create(new RawMethodPtrRow(mrid));
			}

			if (methodDefInfos.TableSize != tablesHeap.MethodPtrTable.Rows)
				throw new ModuleWriterException("Didn't create all dummy methods");
		}

		/// <summary>
		/// Re-uses all <c>Param</c> rows which aren't owned by any type due to the params
		/// having been deleted by the user. The reason we must do this is that the
		/// <c>ParamPtr</c> and <c>Param</c> tables must be the same size.
		/// This method must be called after <see cref="ReUseDeletedMethodRows()"/> since
		/// this method will create more methods at the end of the <c>Method</c> table.
		/// </summary>
		void ReUseDeletedParamRows() {
			if (tablesHeap.ParamPtrTable.IsEmpty)
				return;
			if (paramDefInfos.TableSize == tablesHeap.ParamPtrTable.Rows)
				return;

			var hasOwner = new bool[paramDefInfos.TableSize];
			for (int i = 0; i < paramDefInfos.Count; i++)
				hasOwner[(int)paramDefInfos.Get(i).Rid - 1] = true;

			CreateDummyPtrTableType();

			// For each param, attach it to a new method. Another alternative would be to create
			// one (or a few) methods with tons of parameters.
			uint methodSig = GetSignature(MethodSig.CreateInstance(module.CorLibTypes.Void));
			for (int i = 0; i < hasOwner.Length; i++) {
				if (hasOwner[i])
					continue;
				uint prid = (uint)i + 1;

				var prow = tablesHeap.ParamTable[prid];
				prow.Flags = 0;
				prow.Sequence = 0;	// Return type parameter
				prow.Name = stringsHeap.Add(string.Format("p{0:X6}", prid));
				uint ptrRid = tablesHeap.ParamPtrTable.Create(new RawParamPtrRow(prid));

				var mrow = new RawMethodRow(0,
					(ushort)(MethodImplAttributes.IL | MethodImplAttributes.Managed),
					(ushort)(MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Abstract),
					stringsHeap.Add(string.Format("mp{0:X6}", prid)),
					methodSig,
					ptrRid);
				uint mrid = tablesHeap.MethodTable.Create(mrow);
				if (tablesHeap.MethodPtrTable.Rows > 0)
					tablesHeap.MethodPtrTable.Create(new RawMethodPtrRow(mrid));
			}

			if (paramDefInfos.TableSize != tablesHeap.ParamPtrTable.Rows)
				throw new ModuleWriterException("Didn't create all dummy params");
		}

		/// <summary>
		/// Re-uses all <c>Event</c> rows which aren't owned by any type due to the events
		/// having been deleted by the user. The reason we must do this is that the
		/// <c>EventPtr</c> and <c>Event</c> tables must be the same size.
		/// </summary>
		void ReUseDeletedEventRows() {
			if (tablesHeap.EventPtrTable.IsEmpty)
				return;
			if (eventDefInfos.TableSize == tablesHeap.EventPtrTable.Rows)
				return;

			var hasOwner = new bool[eventDefInfos.TableSize];
			for (int i = 0; i < eventDefInfos.Count; i++)
				hasOwner[(int)eventDefInfos.Get(i).Rid - 1] = true;

			uint typeRid = CreateDummyPtrTableType();
			tablesHeap.EventMapTable.Create(new RawEventMapRow(typeRid, (uint)tablesHeap.EventPtrTable.Rows + 1));

			uint eventType = AddTypeDefOrRef(module.CorLibTypes.Object.TypeDefOrRef);
			for (int i = 0; i < hasOwner.Length; i++) {
				if (hasOwner[i])
					continue;
				uint erid = (uint)i + 1;

				var frow = tablesHeap.EventTable[erid];
				frow.EventFlags = 0;
				frow.Name = stringsHeap.Add(string.Format("E{0:X6}", erid));
				frow.EventType = eventType;
				tablesHeap.EventPtrTable.Create(new RawEventPtrRow(erid));
			}

			if (eventDefInfos.TableSize != tablesHeap.EventPtrTable.Rows)
				throw new ModuleWriterException("Didn't create all dummy events");
		}

		/// <summary>
		/// Re-uses all <c>Property</c> rows which aren't owned by any type due to the properties
		/// having been deleted by the user. The reason we must do this is that the
		/// <c>PropertyPtr</c> and <c>Property</c> tables must be the same size.
		/// </summary>
		void ReUseDeletedPropertyRows() {
			if (tablesHeap.PropertyPtrTable.IsEmpty)
				return;
			if (propertyDefInfos.TableSize == tablesHeap.PropertyPtrTable.Rows)
				return;

			var hasOwner = new bool[propertyDefInfos.TableSize];
			for (int i = 0; i < propertyDefInfos.Count; i++)
				hasOwner[(int)propertyDefInfos.Get(i).Rid - 1] = true;

			uint typeRid = CreateDummyPtrTableType();
			tablesHeap.PropertyMapTable.Create(new RawPropertyMapRow(typeRid, (uint)tablesHeap.PropertyPtrTable.Rows + 1));

			uint propertySig = GetSignature(PropertySig.CreateStatic(module.CorLibTypes.Object));
			for (int i = 0; i < hasOwner.Length; i++) {
				if (hasOwner[i])
					continue;
				uint prid = (uint)i + 1;

				var frow = tablesHeap.PropertyTable[prid];
				frow.PropFlags = 0;
				frow.Name = stringsHeap.Add(string.Format("P{0:X6}", prid));
				frow.Type = propertySig;
				tablesHeap.PropertyPtrTable.Create(new RawPropertyPtrRow(prid));
			}

			if (propertyDefInfos.TableSize != tablesHeap.PropertyPtrTable.Rows)
				throw new ModuleWriterException("Didn't create all dummy properties");
		}

		/// <summary>
		/// Creates a dummy <c>TypeDef</c> at the end of the <c>TypeDef</c> table that will own
		/// dummy methods and fields. These dummy methods and fields are only created if the size
		/// of the ptr table is less than the size of the non-ptr table (eg. size MethodPtr table
		/// is less than size Method table). The only reason the ptr table would be smaller than
		/// the non-ptr table is when some field/method has been deleted and we must preserve
		/// all method/field rids.
		/// </summary>
		uint CreateDummyPtrTableType() {
			if (dummyPtrTableTypeRid != 0)
				return dummyPtrTableTypeRid;

			var flags = TypeAttributes.NotPublic | TypeAttributes.AutoLayout |
				TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.AnsiClass;
			int numFields = fieldDefInfos.NeedPtrTable ? fieldDefInfos.Count : fieldDefInfos.TableSize;
			int numMethods = methodDefInfos.NeedPtrTable ? methodDefInfos.Count : methodDefInfos.TableSize;
			var row = new RawTypeDefRow((uint)flags,
						stringsHeap.Add(Guid.NewGuid().ToString("B")),
						stringsHeap.Add("dummy_ptr"),
						AddTypeDefOrRef(module.CorLibTypes.Object.TypeDefOrRef),
						(uint)numFields + 1,
						(uint)numMethods + 1);
			dummyPtrTableTypeRid = tablesHeap.TypeDefTable.Create(row);
			if (dummyPtrTableTypeRid == 1)
				throw new ModuleWriterException("Dummy ptr type is the first type");
			return dummyPtrTableTypeRid;
		}
		uint dummyPtrTableTypeRid;

		void FindMemberDefs() {
			int pos;
			foreach (var type in allTypeDefs) {
				if (type == null)
					continue;

				pos = 0;
				foreach (var field in type.Fields) {
					if (field == null)
						continue;
					fieldDefInfos.Add(field, pos++);
				}

				pos = 0;
				foreach (var method in type.Methods) {
					if (method == null)
						continue;
					methodDefInfos.Add(method, pos++);
				}

				pos = 0;
				foreach (var evt in type.Events) {
					if (evt == null)
						continue;
					eventDefInfos.Add(evt, pos++);
				}

				pos = 0;
				foreach (var prop in type.Properties) {
					if (prop == null)
						continue;
					propertyDefInfos.Add(prop, pos++);
				}
			}

			fieldDefInfos.SortDefs();
			methodDefInfos.SortDefs();
			eventDefInfos.SortDefs();
			propertyDefInfos.SortDefs();

			for (int i = 0; i < methodDefInfos.Count; i++) {
				var method = methodDefInfos.Get(i).Def;
				pos = 0;
				foreach (var param in Sort(method.ParamDefs)) {
					if (param == null)
						continue;
					paramDefInfos.Add(param, pos++);
				}
			}
			paramDefInfos.SortDefs();
		}

		void SortFields() {
			fieldDefInfos.Sort((a, b) => {
				var dta = a.Def.DeclaringType == null ? 0 : typeToRid[a.Def.DeclaringType];
				var dtb = b.Def.DeclaringType == null ? 0 : typeToRid[b.Def.DeclaringType];
				if (dta == 0 || dtb == 0)
					return a.Rid.CompareTo(b.Rid);
				if (dta != dtb)
					return dta.CompareTo(dtb);
				return fieldDefInfos.GetCollectionPosition(a.Def).CompareTo(fieldDefInfos.GetCollectionPosition(b.Def));
			});
		}

		void SortMethods() {
			methodDefInfos.Sort((a, b) => {
				var dta = a.Def.DeclaringType == null ? 0 : typeToRid[a.Def.DeclaringType];
				var dtb = b.Def.DeclaringType == null ? 0 : typeToRid[b.Def.DeclaringType];
				if (dta == 0 || dtb == 0)
					return a.Rid.CompareTo(b.Rid);
				if (dta != dtb)
					return dta.CompareTo(dtb);
				return methodDefInfos.GetCollectionPosition(a.Def).CompareTo(methodDefInfos.GetCollectionPosition(b.Def));
			});
		}

		void SortParameters() {
			paramDefInfos.Sort((a, b) => {
				var dma = a.Def.DeclaringMethod == null ? 0 : methodDefInfos.Rid(a.Def.DeclaringMethod);
				var dmb = b.Def.DeclaringMethod == null ? 0 : methodDefInfos.Rid(b.Def.DeclaringMethod);
				if (dma == 0 || dmb == 0)
					return a.Rid.CompareTo(b.Rid);
				if (dma != dmb)
					return dma.CompareTo(dmb);
				return paramDefInfos.GetCollectionPosition(a.Def).CompareTo(paramDefInfos.GetCollectionPosition(b.Def));
			});
		}

		void SortEvents() {
			eventDefInfos.Sort((a, b) => {
				var dta = a.Def.DeclaringType == null ? 0 : typeToRid[a.Def.DeclaringType];
				var dtb = b.Def.DeclaringType == null ? 0 : typeToRid[b.Def.DeclaringType];
				if (dta == 0 || dtb == 0)
					return a.Rid.CompareTo(b.Rid);
				if (dta != dtb)
					return dta.CompareTo(dtb);
				return eventDefInfos.GetCollectionPosition(a.Def).CompareTo(eventDefInfos.GetCollectionPosition(b.Def));
			});
		}

		void SortProperties() {
			propertyDefInfos.Sort((a, b) => {
				var dta = a.Def.DeclaringType == null ? 0 : typeToRid[a.Def.DeclaringType];
				var dtb = b.Def.DeclaringType == null ? 0 : typeToRid[b.Def.DeclaringType];
				if (dta == 0 || dtb == 0)
					return a.Rid.CompareTo(b.Rid);
				if (dta != dtb)
					return dta.CompareTo(dtb);
				return propertyDefInfos.GetCollectionPosition(a.Def).CompareTo(propertyDefInfos.GetCollectionPosition(b.Def));
			});
		}

		void InitializeMethodAndFieldList() {
			uint fieldList = 1, methodList = 1;
			foreach (var type in allTypeDefs) {
				var typeRow = tablesHeap.TypeDefTable[typeToRid[type]];
				typeRow.FieldList = fieldList;
				typeRow.MethodList = methodList;
				fieldList += (uint)type.Fields.Count;
				methodList += (uint)type.Methods.Count;
			}
		}

		void InitializeParamList() {
			uint ridList = 1;
			for (uint methodRid = 1; methodRid <= methodDefInfos.TableSize; methodRid++) {
				var methodInfo = methodDefInfos.GetByRid(methodRid);
				var row = tablesHeap.MethodTable[methodRid];
				row.ParamList = ridList;
				if (methodInfo != null)
					ridList += (uint)methodInfo.Def.ParamDefs.Count;
			}
		}

		void InitializeEventMap() {
			if (!tablesHeap.EventMapTable.IsEmpty)
				throw new ModuleWriterException("EventMap table isn't empty");
			TypeDef type = null;
			for (int i = 0; i < eventDefInfos.Count; i++) {
				var info = eventDefInfos.GetSorted(i);
				if (type == info.Def.DeclaringType)
					continue;
				type = info.Def.DeclaringType;
				var row = new RawEventMapRow(typeToRid[type], info.NewRid);
				uint eventMapRid = tablesHeap.EventMapTable.Create(row);
				eventMapInfos.Add(type, eventMapRid);
			}
		}

		void InitializePropertyMap() {
			if (!tablesHeap.PropertyMapTable.IsEmpty)
				throw new ModuleWriterException("PropertyMap table isn't empty");
			TypeDef type = null;
			for (int i = 0; i < propertyDefInfos.Count; i++) {
				var info = propertyDefInfos.GetSorted(i);
				if (type == info.Def.DeclaringType)
					continue;
				type = info.Def.DeclaringType;
				var row = new RawPropertyMapRow(typeToRid[type], info.NewRid);
				uint propertyMapRid = tablesHeap.PropertyMapTable.Create(row);
				propertyMapInfos.Add(type, propertyMapRid);
			}
		}

		/// <inheritdoc/>
		protected override uint AddTypeRef(TypeRef tr) {
			if (tr == null) {
				Error("TypeRef is null");
				return 0;
			}
			uint rid;
			if (typeRefInfos.TryGetRid(tr, out rid)) {
				if (rid == 0)
					Error("TypeRef {0:X8} has an infinite ResolutionScope loop", tr.MDToken.Raw);
				return rid;
			}
			typeRefInfos.Add(tr, 0);	// Prevent inf recursion

			bool isOld = PreserveTypeRefRids && mod.ResolveTypeRef(tr.Rid) == tr;
			var row = isOld ? tablesHeap.TypeRefTable[tr.Rid] : new RawTypeRefRow();
			row.ResolutionScope = AddResolutionScope(tr.ResolutionScope);
			row.Name = stringsHeap.Add(tr.Name);
			row.Namespace = stringsHeap.Add(tr.Namespace);

			rid = isOld ? tr.Rid : tablesHeap.TypeRefTable.Add(row);
			typeRefInfos.SetRid(tr, rid);
			AddCustomAttributes(Table.TypeRef, rid, tr);
			return rid;
		}

		/// <inheritdoc/>
		protected override uint AddTypeSpec(TypeSpec ts) {
			return AddTypeSpec(ts, false);
		}

		uint AddTypeSpec(TypeSpec ts, bool forceIsOld) {
			if (ts == null) {
				Error("TypeSpec is null");
				return 0;
			}
			uint rid;
			if (typeSpecInfos.TryGetRid(ts, out rid)) {
				if (rid == 0)
					Error("TypeSpec {0:X8} has an infinite TypeSig loop", ts.MDToken.Raw);
				return rid;
			}
			typeSpecInfos.Add(ts, 0);	// Prevent inf recursion

			bool isOld = forceIsOld || (PreserveTypeSpecRids && mod.ResolveTypeSpec(ts.Rid) == ts);
			var row = isOld ? tablesHeap.TypeSpecTable[ts.Rid] : new RawTypeSpecRow();
			row.Signature = GetSignature(ts.TypeSig, ts.ExtraData);

			rid = isOld ? ts.Rid : tablesHeap.TypeSpecTable.Add(row);
			typeSpecInfos.SetRid(ts, rid);
			AddCustomAttributes(Table.TypeSpec, rid, ts);
			return rid;
		}

		/// <inheritdoc/>
		protected override uint AddMemberRef(MemberRef mr) {
			return AddMemberRef(mr, false);
		}

		uint AddMemberRef(MemberRef mr, bool forceIsOld) {
			if (mr == null) {
				Error("MemberRef is null");
				return 0;
			}
			uint rid;
			if (memberRefInfos.TryGetRid(mr, out rid))
				return rid;

			bool isOld = forceIsOld || (PreserveMemberRefRids && mod.ResolveMemberRef(mr.Rid) == mr);
			var row = isOld ? tablesHeap.MemberRefTable[mr.Rid] : new RawMemberRefRow();
			row.Class = AddMemberRefParent(mr.Class);
			row.Name = stringsHeap.Add(mr.Name);
			row.Signature = GetSignature(mr.Signature);

			rid = isOld ? mr.Rid : tablesHeap.MemberRefTable.Add(row);
			memberRefInfos.Add(mr, rid);
			AddCustomAttributes(Table.MemberRef, rid, mr);
			return rid;
		}

		/// <inheritdoc/>
		protected override uint AddStandAloneSig(StandAloneSig sas) {
			return AddStandAloneSig(sas, false);
		}

		uint AddStandAloneSig(StandAloneSig sas, bool forceIsOld) {
			if (sas == null) {
				Error("StandAloneSig is null");
				return 0;
			}
			uint rid;
			if (standAloneSigInfos.TryGetRid(sas, out rid))
				return rid;

			bool isOld = forceIsOld || (PreserveStandAloneSigRids && mod.ResolveStandAloneSig(sas.Rid) == sas);
			var row = isOld ? tablesHeap.StandAloneSigTable[sas.Rid] : new RawStandAloneSigRow();
			row.Signature = GetSignature(sas.Signature);

			rid = isOld ? sas.Rid : tablesHeap.StandAloneSigTable.Add(row);
			standAloneSigInfos.Add(sas, rid);
			AddCustomAttributes(Table.StandAloneSig, rid, sas);
			return rid;
		}

		/// <inheritdoc/>
		public override MDToken GetToken(IList<TypeSig> locals, uint origToken) {
			if (!PreserveStandAloneSigRids || !IsValidStandAloneSigToken(origToken))
				return base.GetToken(locals, origToken);

			uint rid = AddStandAloneSig(new LocalSig(locals, false), origToken);
			if (rid == 0)
				return base.GetToken(locals, origToken);
			return new MDToken(Table.StandAloneSig, rid);
		}

		/// <inheritdoc/>
		protected override uint AddStandAloneSig(MethodSig methodSig, uint origToken) {
			if (!PreserveStandAloneSigRids || !IsValidStandAloneSigToken(origToken))
				return base.AddStandAloneSig(methodSig, origToken);

			uint rid = AddStandAloneSig(methodSig, origToken);
			if (rid == 0)
				return base.AddStandAloneSig(methodSig, origToken);
			return rid;
		}

		uint AddStandAloneSig(CallingConventionSig callConvSig, uint origToken) {
			uint sig = GetSignature(callConvSig);
			uint otherSig;
			if (callConvTokenToSignature.TryGetValue(origToken, out otherSig)) {
				if (sig == otherSig)
					return MDToken.ToRID(origToken);
				Warning("Could not preserve StandAloneSig token {0:X8}", origToken);
				return 0;
			}

			uint rid = MDToken.ToRID(origToken);
			var sas = mod.ResolveStandAloneSig(rid);
			if (standAloneSigInfos.Exists(sas)) {
				Warning("StandAloneSig {0:X8} already exists", origToken);
				return 0;
			}

			// Make sure it uses the updated sig
			var oldSig = sas.Signature;
			try {
				sas.Signature = callConvSig;
				AddStandAloneSig(sas, true);
			}
			finally {
				sas.Signature = oldSig;
			}

			callConvTokenToSignature.Add(origToken, sig);
			return MDToken.ToRID(origToken);
		}

		bool IsValidStandAloneSigToken(uint token) {
			if (MDToken.ToTable(token) != Table.StandAloneSig)
				return false;
			uint rid = MDToken.ToRID(token);
			return mod.TablesStream.StandAloneSigTable.IsValidRID(rid);
		}

		/// <inheritdoc/>
		protected override uint AddMethodSpec(MethodSpec ms) {
			return AddMethodSpec(ms, false);
		}

		uint AddMethodSpec(MethodSpec ms, bool forceIsOld) {
			if (ms == null) {
				Error("MethodSpec is null");
				return 0;
			}
			uint rid;
			if (methodSpecInfos.TryGetRid(ms, out rid))
				return rid;

			bool isOld = forceIsOld || (PreserveMethodSpecRids && mod.ResolveMethodSpec(ms.Rid) == ms);
			var row = isOld ? tablesHeap.MethodSpecTable[ms.Rid] : new RawMethodSpecRow();
			row.Method = AddMethodDefOrRef(ms.Method);
			row.Instantiation = GetSignature(ms.Instantiation);

			rid = isOld ? ms.Rid : tablesHeap.MethodSpecTable.Add(row);
			methodSpecInfos.Add(ms, rid);
			AddCustomAttributes(Table.MethodSpec, rid, ms);
			return rid;
		}

		/// <inheritdoc/>
		protected override void BeforeSortingCustomAttributes() {
			InitializeUninitializedTableRows();
		}
	}
}
