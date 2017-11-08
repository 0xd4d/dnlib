// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using dnlib.DotNet.MD;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Does not preserve metadata tokens
	/// </summary>
	sealed class NormalMetaData : MetaData {
		readonly Rows<TypeRef> typeRefInfos = new Rows<TypeRef>();
		readonly Rows<TypeDef> typeDefInfos = new Rows<TypeDef>();
		readonly Rows<FieldDef> fieldDefInfos = new Rows<FieldDef>();
		readonly Rows<MethodDef> methodDefInfos = new Rows<MethodDef>();
		readonly Rows<ParamDef> paramDefInfos = new Rows<ParamDef>();
		readonly Rows<MemberRef> memberRefInfos = new Rows<MemberRef>();
		readonly Rows<StandAloneSig> standAloneSigInfos = new Rows<StandAloneSig>();
		readonly Rows<EventDef> eventDefInfos = new Rows<EventDef>();
		readonly Rows<PropertyDef> propertyDefInfos = new Rows<PropertyDef>();
		readonly Rows<TypeSpec> typeSpecInfos = new Rows<TypeSpec>();
		readonly Rows<MethodSpec> methodSpecInfos = new Rows<MethodSpec>();

		protected override int NumberOfMethods {
			get { return methodDefInfos.Count; }
		}

		public NormalMetaData(ModuleDef module, UniqueChunkList<ByteArrayChunk> constants, MethodBodyChunks methodBodies, NetResources netResources, MetaDataOptions options, DebugMetaDataKind debugKind, bool isStandaloneDebugMetadata)
			: base(module, constants, methodBodies, netResources, options, debugKind, isStandaloneDebugMetadata) {
		}

		/// <inheritdoc/>
		protected override List<TypeDef> GetAllTypeDefs() {
			// All nested types must be after their enclosing type. This is exactly
			// what module.GetTypes() does.
			return new List<TypeDef>(module.GetTypes());
		}

		/// <inheritdoc/>
		protected override void AllocateTypeDefRids() {
			foreach (var type in allTypeDefs) {
				if (type == null)
					continue;
				uint rid = tablesHeap.TypeDefTable.Create(new RawTypeDefRow());
				typeDefInfos.Add(type, rid);
			}
		}

		/// <inheritdoc/>
		protected override void AllocateMemberDefRids() {
			int numTypes = allTypeDefs.Count;
			int typeNum = 0;
			int notifyNum = 0;
			const int numNotifyEvents = 5; // AllocateMemberDefRids0 - AllocateMemberDefRids4
			int notifyAfter = numTypes / numNotifyEvents;

			uint fieldListRid = 1, methodListRid = 1;
			uint eventListRid = 1, propertyListRid = 1;
			uint paramListRid = 1;
			foreach (var type in allTypeDefs) {
				if (typeNum++ == notifyAfter && notifyNum < numNotifyEvents) {
					Listener.OnMetaDataEvent(this, MetaDataEvent.AllocateMemberDefRids0 + notifyNum++);
					notifyAfter += numTypes / numNotifyEvents;
				}

				if (type == null)
					continue;
				uint typeRid = GetRid(type);
				var typeRow = tablesHeap.TypeDefTable[typeRid];
				typeRow.FieldList = fieldListRid;
				typeRow.MethodList = methodListRid;

				foreach (var field in type.Fields) {
					if (field == null)
						continue;
					uint rid = fieldListRid++;
					if (rid != tablesHeap.FieldTable.Create(new RawFieldRow()))
						throw new ModuleWriterException("Invalid field rid");
					fieldDefInfos.Add(field, rid);
				}

				foreach (var method in type.Methods) {
					if (method == null)
						continue;
					uint rid = methodListRid++;
					var row = new RawMethodRow(0, 0, 0, 0, 0, paramListRid);
					if (rid != tablesHeap.MethodTable.Create(row))
						throw new ModuleWriterException("Invalid method rid");
					methodDefInfos.Add(method, rid);
					foreach (var pd in Sort(method.ParamDefs)) {
						if (pd == null)
							continue;
						uint pdRid = paramListRid++;
						if (pdRid != tablesHeap.ParamTable.Create(new RawParamRow()))
							throw new ModuleWriterException("Invalid param rid");
						paramDefInfos.Add(pd, pdRid);
					}
				}

				if (!IsEmpty(type.Events)) {
					uint eventMapRid = tablesHeap.EventMapTable.Create(new RawEventMapRow(typeRid, eventListRid));
					eventMapInfos.Add(type, eventMapRid);
					foreach (var evt in type.Events) {
						if (evt == null)
							continue;
						uint rid = eventListRid++;
						if (rid != tablesHeap.EventTable.Create(new RawEventRow()))
							throw new ModuleWriterException("Invalid event rid");
						eventDefInfos.Add(evt, rid);
					}
				}

				if (!IsEmpty(type.Properties)) {
					uint propertyMapRid = tablesHeap.PropertyMapTable.Create(new RawPropertyMapRow(typeRid, propertyListRid));
					propertyMapInfos.Add(type, propertyMapRid);
					foreach (var prop in type.Properties) {
						if (prop == null)
							continue;
						uint rid = propertyListRid++;
						if (rid != tablesHeap.PropertyTable.Create(new RawPropertyRow()))
							throw new ModuleWriterException("Invalid property rid");
						propertyDefInfos.Add(prop, rid);
					}
				}
			}
			while (notifyNum < numNotifyEvents)
				Listener.OnMetaDataEvent(this, MetaDataEvent.AllocateMemberDefRids0 + notifyNum++);
		}

		/// <inheritdoc/>
		public override uint GetRid(TypeRef tr) {
			uint rid;
			typeRefInfos.TryGetRid(tr, out rid);
			return rid;
		}

		/// <inheritdoc/>
		public override uint GetRid(TypeDef td) {
			uint rid;
			if (typeDefInfos.TryGetRid(td, out rid))
				return rid;
			if (td == null)
				Error("TypeDef is null");
			else
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
			var row = new RawTypeRefRow(AddResolutionScope(tr.ResolutionScope),
						stringsHeap.Add(tr.Name),
						stringsHeap.Add(tr.Namespace));
			rid = tablesHeap.TypeRefTable.Add(row);
			typeRefInfos.SetRid(tr, rid);
			AddCustomAttributes(Table.TypeRef, rid, tr);
			AddCustomDebugInformationList(Table.TypeRef, rid, tr);
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
					Error("TypeSpec {0:X8} has an infinite TypeSig loop", ts.MDToken.Raw);
				return rid;
			}
			typeSpecInfos.Add(ts, 0);	// Prevent inf recursion
			var row = new RawTypeSpecRow(GetSignature(ts.TypeSig, ts.ExtraData));
			rid = tablesHeap.TypeSpecTable.Add(row);
			typeSpecInfos.SetRid(ts, rid);
			AddCustomAttributes(Table.TypeSpec, rid, ts);
			AddCustomDebugInformationList(Table.TypeSpec, rid, ts);
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
			var row = new RawMemberRefRow(AddMemberRefParent(mr.Class),
							stringsHeap.Add(mr.Name),
							GetSignature(mr.Signature));
			rid = tablesHeap.MemberRefTable.Add(row);
			memberRefInfos.Add(mr, rid);
			AddCustomAttributes(Table.MemberRef, rid, mr);
			AddCustomDebugInformationList(Table.MemberRef, rid, mr);
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
			var row = new RawStandAloneSigRow(GetSignature(sas.Signature));
			rid = tablesHeap.StandAloneSigTable.Add(row);
			standAloneSigInfos.Add(sas, rid);
			AddCustomAttributes(Table.StandAloneSig, rid, sas);
			AddCustomDebugInformationList(Table.StandAloneSig, rid, sas);
			return rid;
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
			var row = new RawMethodSpecRow(AddMethodDefOrRef(ms.Method),
						GetSignature(ms.Instantiation));
			rid = tablesHeap.MethodSpecTable.Add(row);
			methodSpecInfos.Add(ms, rid);
			AddCustomAttributes(Table.MethodSpec, rid, ms);
			AddCustomDebugInformationList(Table.MethodSpec, rid, ms);
			return rid;
		}
	}
}
