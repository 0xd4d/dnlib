using System;
using System.Collections.Generic;
using System.Diagnostics;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Preserves metadata tokens
	/// </summary>
	sealed class PreserveTokensMetaData : MetaData {
		ModuleDefMD mod;
		Rows<TypeRef> typeRefInfos = new Rows<TypeRef>();
		Dictionary<TypeDef, uint> typeToRid = new Dictionary<TypeDef, uint>();
		MemberDefDict<FieldDef> fieldDefInfos = new MemberDefDict<FieldDef>();
		MemberDefDict<MethodDef> methodDefInfos = new MemberDefDict<MethodDef>();
		MemberDefDict<ParamDef> paramDefInfos = new MemberDefDict<ParamDef>();
		Rows<MemberRef> memberRefInfos = new Rows<MemberRef>();
		Rows<StandAloneSig> standAloneSigInfos = new Rows<StandAloneSig>();
		MemberDefDict<EventDef> eventDefInfos = new MemberDefDict<EventDef>();
		MemberDefDict<PropertyDef> propertyDefInfos = new MemberDefDict<PropertyDef>();
		Rows<TypeSpec> typeSpecInfos = new Rows<TypeSpec>();
		Rows<MethodSpec> methodSpecInfos = new Rows<MethodSpec>();
		Dictionary<uint, uint> localsTokenToSignature = new Dictionary<uint, uint>();

		[DebuggerDisplay("{Rid} -> {NewRid} {Def}")]
		sealed class MemberDefInfo<T> where T : IMDTokenProvider {
			public T Def;
			public uint Rid;
			public uint NewRid;

			public MemberDefInfo(T def, uint rid) {
				this.Def = def;
				this.Rid = rid;
				this.NewRid = rid;
			}
		}

		[DebuggerDisplay("Count = {Count}")]
		sealed class MemberDefDict<T> where T : IMDTokenProvider {
			Dictionary<T, MemberDefInfo<T>> defToInfo = new Dictionary<T, MemberDefInfo<T>>();
			List<MemberDefInfo<T>> defs = new List<MemberDefInfo<T>>();
			List<MemberDefInfo<T>> sortedDefs;
			Dictionary<T, int> collectionPosition = new Dictionary<T, int>();

			public int Count {
				get { return defs.Count; }
			}

			public void FindDefs(IEnumerable<TypeDef> allTypeDefs, MFunc<uint, T> resolve, MFunc<TypeDef, IEnumerable<T>> getDefs) {
				uint rid;

				for (rid = 1; ; rid++) {
					var def = resolve(rid);
					if (def == null)
						break;
					var info = new MemberDefInfo<T>(def, rid);
					defToInfo[def] = info;
					defs.Add(info);
				}

				foreach (var type in allTypeDefs) {
					foreach (var def in getDefs(type)) {
						if (def == null)
							continue;
						if (defToInfo.ContainsKey(def))
							continue;
						var info = new MemberDefInfo<T>(def, rid++);
						defToInfo[def] = info;
						defs.Add(info);
					}
				}
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

			public void Sort(Comparison<MemberDefInfo<T>> comparer) {
				sortedDefs = new List<MemberDefInfo<T>>(defs);
				sortedDefs.Sort(comparer);
				for (int i = 0; i < sortedDefs.Count; i++)
					sortedDefs[i].NewRid = (uint)i + 1;
			}

			public MemberDefInfo<T> Get(int i) {
				return defs[i];
			}

			public MemberDefInfo<T> GetSorted(int i) {
				return sortedDefs[i];
			}

			public bool WasSorted() {
				for (int i = 0; i < defs.Count; i++) {
					if (defs[i] != sortedDefs[i])
						return false;
				}
				return true;
			}

			public void SetCollectionPosition(T def, int position) {
				collectionPosition.Add(def, position);
			}

			public int GetCollectionPosition(T def) {
				return collectionPosition[def];
			}
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

			// We'll resurrect every single type, field, method, param, event and property,
			// so make sure that whatever was deleted stays deleted.
			if (mod.MetaData.TablesStream.HasDelete)
				tablesHeap.HasDeletedRows = true;
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
			Error("TypeDef {0} ({1:X8}) is not defined in this module", td, td.MDToken.Raw);
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
				Error("Field {0} ({1:X8}) is not defined in this module", fd, fd.MDToken.Raw);
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
				Error("Method {0} ({1:X8}) is not defined in this module", md, md.MDToken.Raw);
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
				Error("Param {0} ({1:X8}) is not defined in this module", pd, pd.MDToken.Raw);
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
				Error("Event {0} ({1:X8}) is not defined in this module", ed, ed.MDToken.Raw);
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
				Error("Property {0} ({1:X8}) is not defined in this module", pd, pd.MDToken.Raw);
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
			CreateEmptyTableRows();
		}

		/// <inheritdoc/>
		protected override IEnumerable<TypeDef> GetAllTypeDefs() {
			var types = new List<TypeDef>();
			uint rid;

			for (rid = 1; ; rid++) {
				var type = mod.ResolveTypeDef(rid);
				if (type == null)
					break;
				typeToRid[type] = rid;
				types.Add(type);
			}

			foreach (var type in mod.GetTypes()) {
				if (type == null)
					continue;
				if (typeToRid.ContainsKey(type))
					continue;
				typeToRid[type] = rid++;
				types.Add(type);
			}

			return types;
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

			rows = mod.TablesStream.Get(Table.TypeRef).Rows;
			for (uint i = 0; i < rows; i++)
				tablesHeap.TypeRefTable.Create(new RawTypeRefRow());

			rows = mod.TablesStream.Get(Table.MemberRef).Rows;
			for (uint i = 0; i < rows; i++)
				tablesHeap.MemberRefTable.Create(new RawMemberRefRow());

			rows = mod.TablesStream.Get(Table.StandAloneSig).Rows;
			for (uint i = 0; i < rows; i++)
				tablesHeap.StandAloneSigTable.Create(new RawStandAloneSigRow());

			rows = mod.TablesStream.Get(Table.TypeSpec).Rows;
			for (uint i = 0; i < rows; i++)
				tablesHeap.TypeSpecTable.Create(new RawTypeSpecRow());

			rows = mod.TablesStream.Get(Table.MethodSpec).Rows;
			for (uint i = 0; i < rows; i++)
				tablesHeap.MethodSpecTable.Create(new RawMethodSpecRow());
		}

		/// <summary>
		/// Adds any non-referenced rows that haven't been added yet but are present in
		/// the original file. If there are any non-referenced rows, it's usually a sign
		/// that an obfuscator has encrypted one or more methods or that it has added
		/// some rows it uses to decrypt something.
		/// </summary>
		void InitializeUninitializedTableRows() {
			uint rows;

			rows = mod.TablesStream.Get(Table.TypeRef).Rows;
			for (uint rid = 1; rid <= rows; rid++)
				AddTypeRef(mod.ResolveTypeRef(rid));

			rows = mod.TablesStream.Get(Table.MemberRef).Rows;
			for (uint rid = 1; rid <= rows; rid++)
				AddMemberRef(mod.ResolveMemberRef(rid));

			rows = mod.TablesStream.Get(Table.StandAloneSig).Rows;
			for (uint rid = 1; rid <= rows; rid++)
				AddStandAloneSig(mod.ResolveStandAloneSig(rid));

			rows = mod.TablesStream.Get(Table.TypeSpec).Rows;
			for (uint rid = 1; rid <= rows; rid++)
				AddTypeSpec(mod.ResolveTypeSpec(rid));

			rows = mod.TablesStream.Get(Table.MethodSpec).Rows;
			for (uint rid = 1; rid <= rows; rid++)
				AddMethodSpec(mod.ResolveMethodSpec(rid));
		}

		/// <inheritdoc/>
		protected override void AllocateMemberDefRids() {
			FindMemberDefs();
			InitializeCollectionPositions();

			for (int i = 0; i < fieldDefInfos.Count; i++) {
				var info = fieldDefInfos.Get(i);
				if (info.Rid != tablesHeap.FieldTable.Create(new RawFieldRow()))
					throw new ModuleWriterException("Invalid field rid");
			}

			for (int i = 0; i < methodDefInfos.Count; i++) {
				var info = methodDefInfos.Get(i);
				if (info.Rid != tablesHeap.MethodTable.Create(new RawMethodRow()))
					throw new ModuleWriterException("Invalid method rid");
			}

			for (int i = 0; i < paramDefInfos.Count; i++) {
				var info = paramDefInfos.Get(i);
				if (info.Rid != tablesHeap.ParamTable.Create(new RawParamRow()))
					throw new ModuleWriterException("Invalid param rid");
			}

			for (int i = 0; i < eventDefInfos.Count; i++) {
				var info = eventDefInfos.Get(i);
				if (info.Rid != tablesHeap.EventTable.Create(new RawEventRow()))
					throw new ModuleWriterException("Invalid event rid");
			}

			for (int i = 0; i < propertyDefInfos.Count; i++) {
				var info = propertyDefInfos.Get(i);
				if (info.Rid != tablesHeap.PropertyTable.Create(new RawPropertyRow()))
					throw new ModuleWriterException("Invalid property rid");
			}

			// ParamDef has currently no DeclaringMethod property but we need it. Use this dict.
			var toMethod = new Dictionary<ParamDef, MethodDef>();
			for (int i = 0; i < methodDefInfos.Count; i++) {
				var info = methodDefInfos.Get(i);
				foreach (var param in info.Def.ParamList)
					toMethod.Add(param, info.Def);
			}

			SortFields();
			SortMethods();
			SortParameters(toMethod);
			SortEvents();
			SortProperties();

			if (!fieldDefInfos.WasSorted()) {
				for (int i = 0; i < fieldDefInfos.Count; i++) {
					var info = fieldDefInfos.GetSorted(i);
					if ((uint)i + 1 != tablesHeap.FieldPtrTable.Add(new RawFieldPtrRow(info.Rid)))
						throw new ModuleWriterException("Invalid field ptr rid");
				}
			}

			if (!methodDefInfos.WasSorted()) {
				for (int i = 0; i < methodDefInfos.Count; i++) {
					var info = methodDefInfos.GetSorted(i);
					if ((uint)i + 1 != tablesHeap.MethodPtrTable.Add(new RawMethodPtrRow(info.Rid)))
						throw new ModuleWriterException("Invalid method ptr rid");
				}
			}

			if (!paramDefInfos.WasSorted()) {
				for (int i = 0; i < paramDefInfos.Count; i++) {
					var info = paramDefInfos.GetSorted(i);
					if ((uint)i + 1 != tablesHeap.ParamPtrTable.Add(new RawParamPtrRow(info.Rid)))
						throw new ModuleWriterException("Invalid param ptr rid");
				}
			}

			if (!eventDefInfos.WasSorted()) {
				for (int i = 0; i < eventDefInfos.Count; i++) {
					var info = eventDefInfos.GetSorted(i);
					if ((uint)i + 1 != tablesHeap.EventPtrTable.Add(new RawEventPtrRow(info.Rid)))
						throw new ModuleWriterException("Invalid event ptr rid");
				}
			}

			if (!propertyDefInfos.WasSorted()) {
				for (int i = 0; i < propertyDefInfos.Count; i++) {
					var info = propertyDefInfos.GetSorted(i);
					if ((uint)i + 1 != tablesHeap.PropertyPtrTable.Add(new RawPropertyPtrRow(info.Rid)))
						throw new ModuleWriterException("Invalid property ptr rid");
				}
			}

			InitializeFieldList();
			InitializeMethodList();
			InitializeParamList(toMethod);
			InitializeEventMap();
			InitializePropertyMap();
		}

		void FindMemberDefs() {
			fieldDefInfos.FindDefs(allTypeDefs, rid => mod.ResolveField(rid), type => type.Fields);
			methodDefInfos.FindDefs(allTypeDefs, rid => mod.ResolveMethod(rid), type => type.Methods);
			paramDefInfos.FindDefs(allTypeDefs, rid => mod.ResolveParam(rid), type => FindParamDefs(type));
			eventDefInfos.FindDefs(allTypeDefs, rid => mod.ResolveEvent(rid), type => type.Events);
			propertyDefInfos.FindDefs(allTypeDefs, rid => mod.ResolveProperty(rid), type => type.Properties);
		}

		void InitializeCollectionPositions() {
			foreach (var type in allTypeDefs) {
				int pos;

				pos = 0;
				foreach (var field in type.Fields) {
					if (field == null)
						continue;
					fieldDefInfos.SetCollectionPosition(field, pos++);
				}

				pos = 0;
				foreach (var method in type.Methods) {
					if (method == null)
						continue;
					methodDefInfos.SetCollectionPosition(method, pos++);

					int pos2 = 0;
					foreach (var param in method.ParamList) {
						if (param == null)
							continue;
						paramDefInfos.SetCollectionPosition(param, pos2++);
					}
				}

				pos = 0;
				foreach (var evt in type.Events) {
					if (evt == null)
						continue;
					eventDefInfos.SetCollectionPosition(evt, pos++);
				}

				pos = 0;
				foreach (var prop in type.Properties) {
					if (prop == null)
						continue;
					propertyDefInfos.SetCollectionPosition(prop, pos++);
				}
			}
		}

		static IEnumerable<ParamDef> FindParamDefs(TypeDef type) {
			foreach (var method in type.Methods) {
				foreach (var paramDef in method.ParamList)
					yield return paramDef;
			}
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

		void SortParameters(Dictionary<ParamDef, MethodDef> toMethod) {
			paramDefInfos.Sort((a, b) => {
				MethodDef method;
				var dma = toMethod.TryGetValue(a.Def, out method) ? methodDefInfos.Rid(method) : 0;
				var dmb = toMethod.TryGetValue(b.Def, out method) ? methodDefInfos.Rid(method) : 0;
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

		void InitializeFieldList() {
			TypeDef type = null;
			for (int i = 0; i < fieldDefInfos.Count; i++) {
				var info = fieldDefInfos.GetSorted(i);
				if (info.Def.DeclaringType == type)
					continue;
				type = info.Def.DeclaringType;
				var row = tablesHeap.TypeDefTable[typeToRid[type]];
				if (row.FieldList != 0)
					throw new ModuleWriterException("row.FieldList has already been initialized");
				row.FieldList = (uint)i + 1;
			}

			uint rid = 1;
			for (int i = 0; i < allTypeDefs.Count; i++) {
				var row = tablesHeap.TypeDefTable[(uint)i + 1];
				if (row.FieldList == 0)
					row.FieldList = rid;
				else {
					if (rid != row.FieldList)
						throw new ModuleWriterException("Invalid field list rid");
					foreach (var field in allTypeDefs[i].Fields) {
						if (field != null)
							rid++;
					}
				}
			}
		}

		void InitializeMethodList() {
			TypeDef type = null;
			for (int i = 0; i < methodDefInfos.Count; i++) {
				var info = methodDefInfos.GetSorted(i);
				if (info.Def.DeclaringType == type)
					continue;
				type = info.Def.DeclaringType;
				var row = tablesHeap.TypeDefTable[typeToRid[type]];
				if (row.MethodList != 0)
					throw new ModuleWriterException("row.MethodList has already been initialized");
				row.MethodList = (uint)i + 1;
			}

			uint rid = 1;
			for (int i = 0; i < allTypeDefs.Count; i++) {
				var row = tablesHeap.TypeDefTable[(uint)i + 1];
				if (row.MethodList == 0)
					row.MethodList = rid;
				else {
					if (rid != row.MethodList)
						throw new ModuleWriterException("Invalid method list rid");
					foreach (var method in allTypeDefs[i].Methods) {
						if (method != null)
							rid++;
					}
				}
			}
		}

		void InitializeParamList(Dictionary<ParamDef, MethodDef> toMethod) {
			MethodDef method = null;
			for (int i = 0; i < paramDefInfos.Count; i++) {
				var info = paramDefInfos.GetSorted(i);
				MethodDef declaringMethod;
				toMethod.TryGetValue(info.Def, out declaringMethod);
				if (declaringMethod == method)
					continue;
				method = declaringMethod;
				var row = tablesHeap.MethodTable[methodDefInfos.Rid(method)];
				if (row.ParamList != 0)
					throw new ModuleWriterException("row.ParamList has already been initialized");
				row.ParamList = (uint)i + 1;
			}

			uint rid = 1;
			for (int i = 0; i < methodDefInfos.Count; i++) {
				var row = tablesHeap.MethodTable[(uint)i + 1];
				if (row.ParamList == 0)
					row.ParamList = rid;
				else {
					if (rid != row.ParamList)
						throw new ModuleWriterException("Invalid param list rid");
					foreach (var param in methodDefInfos.Get(i).Def.ParamList) {
						if (param != null)
							rid++;
					}
				}
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
					Error("TypeRef {0} ({1:X8}) has an infinite ResolutionScope loop", tr, tr.MDToken.Raw);
				return rid;
			}
			typeRefInfos.Add(tr, 0);	// Prevent inf recursion

			bool isOld = mod.ResolveTypeRef(tr.Rid) == tr;
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
			if (ts == null) {
				Error("TypeSpec is null");
				return 0;
			}
			uint rid;
			if (typeSpecInfos.TryGetRid(ts, out rid)) {
				if (rid == 0)
					Error("TypeSpec {0} ({1:X8}) has an infinite TypeSig loop", ts, ts.MDToken.Raw);
				return rid;
			}
			typeSpecInfos.Add(ts, 0);	// Prevent inf recursion

			bool isOld = mod.ResolveTypeSpec(ts.Rid) == ts;
			var row = isOld ? tablesHeap.TypeSpecTable[ts.Rid] : new RawTypeSpecRow();
			row.Signature = GetSignature(ts.TypeSig, ts.ExtraData);

			rid = isOld ? ts.Rid : tablesHeap.TypeSpecTable.Add(row);
			typeSpecInfos.SetRid(ts, rid);
			AddCustomAttributes(Table.TypeSpec, rid, ts);
			return rid;
		}

		/// <inheritdoc/>
		protected override uint AddMemberRef(MemberRef mr) {
			if (mr == null) {
				Error("MemberRef is null");
				return 0;
			}
			uint rid;
			if (memberRefInfos.TryGetRid(mr, out rid))
				return rid;

			bool isOld = mod.ResolveMemberRef(mr.Rid) == mr;
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
			if (sas == null) {
				Error("StandAloneSig is null");
				return 0;
			}
			uint rid;
			if (standAloneSigInfos.TryGetRid(sas, out rid))
				return rid;

			bool isOld = mod.ResolveStandAloneSig(sas.Rid) == sas;
			var row = isOld ? tablesHeap.StandAloneSigTable[sas.Rid] : new RawStandAloneSigRow();
			row.Signature = GetSignature(sas.Signature);

			rid = isOld ? sas.Rid : tablesHeap.StandAloneSigTable.Add(row);
			standAloneSigInfos.Add(sas, rid);
			AddCustomAttributes(Table.StandAloneSig, rid, sas);
			return rid;
		}

		/// <inheritdoc/>
		public override MDToken GetToken(IList<TypeSig> locals, uint origToken) {
			if (!IsValidStandAloneSigToken(origToken))
				return base.GetToken(locals, origToken);

			uint sig = GetSignature(new LocalSig(locals, false));
			uint otherSig;
			if (localsTokenToSignature.TryGetValue(origToken, out otherSig)) {
				if (sig == otherSig)
					return new MDToken(origToken);
				Error("Could not preserve StandAloneSig token {0:X8}", origToken);
				return base.GetToken(locals, origToken);
			}

			uint rid = MDToken.ToRID(origToken);
			var sas = mod.ResolveStandAloneSig(rid);
			if (standAloneSigInfos.Exists(sas)) {
				Error("StandAloneSig {0:X8} already exists", origToken);
				return base.GetToken(locals, origToken);
			}

			AddStandAloneSig(sas);
			localsTokenToSignature.Add(origToken, sig);
			return new MDToken(origToken);
		}

		bool IsValidStandAloneSigToken(uint token) {
			if (MDToken.ToTable(token) != Table.StandAloneSig)
				return false;
			uint rid = MDToken.ToRID(token);
			return mod.TablesStream.Get(Table.StandAloneSig).IsValidRID(rid);
		}

		/// <inheritdoc/>
		protected override uint AddMethodSpec(MethodSpec ms) {
			if (ms == null) {
				Error("MethodSpec is null");
				return 0;
			}
			uint rid;
			if (methodSpecInfos.TryGetRid(ms, out rid))
				return rid;

			bool isOld = mod.ResolveMethodSpec(ms.Rid) == ms;
			var row = isOld ? tablesHeap.MethodSpecTable[ms.Rid] : new RawMethodSpecRow();
			row.Method = AddMethodDefOrRef(ms.Method);
			row.Instantiation = GetSignature(ms.Instantiation);

			rid = isOld ? ms.Rid : tablesHeap.MethodSpecTable.Add(row);
			methodSpecInfos.Add(ms, rid);
			AddCustomAttributes(Table.MethodSpec, rid, ms);
			return rid;
		}

		/// <inheritdoc/>
		protected override void EverythingInitialized() {
			InitializeUninitializedTableRows();
		}
	}
}
