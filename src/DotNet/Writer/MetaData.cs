using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dot10.IO;
using dot10.PE;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// <see cref="MetaData"/> options
	/// </summary>
	[Flags]
	enum MetaDataOptions {
		/// <summary>
		/// Preserves all rids in the following tables: <c>TypeRef</c>, <c>TypeDef</c>,
		/// <c>Field</c>, <c>Method</c>, <c>Param</c>, <c>MemberRef</c>, <c>StandAloneSig</c>,
		/// <c>Event</c>, <c>Property</c>, <c>TypeSpec</c>, <c>MethodSpec</c>
		/// </summary>
		PreserveTokens = 1,

		/// <summary>
		/// Preserves all offsets in the #Strings heap (the original #Strings heap will be saved
		/// in the new file). Type names, field names, and other non-user strings are stored
		/// in the #Strings heap.
		/// </summary>
		PreserveStringsOffsets = 2,

		/// <summary>
		/// Preserves all offsets in the #US heap (the original #US heap will be saved
		/// in the new file). User strings (referenced by the ldstr instruction) are stored in
		/// the #US heap.
		/// </summary>
		PreserveUSOffsets = 4,

		/// <summary>
		/// Preserves all offsets in the #Blob heap (the original #Blob heap will be saved
		/// in the new file). Custom attributes, signatures and other blobs are stored in the
		/// #Blob heap.
		/// </summary>
		PreserveBlobOffsets = 8,
	}

	/// <summary>
	/// .NET meta data
	/// </summary>
	class MetaData : IChunk {
		ModuleDef module;
		UniqueChunkList<ByteArrayChunk> constants;
		MethodBodyChunks methodBodies;
		NetResources netResources;
		TablesHeap tablesHeap;
		StringsHeap stringsHeap;
		USHeap usHeap;
		GuidHeap guidHeap;
		BlobHeap blobHeap;
		FileOffset offset;
		RVA rva;
		MetaDataOptions options;
		ITablesCreator tablesCreator;

		interface ITablesCreator {
			void Create();
		}

		class NormalTablesCreator : ITablesCreator {
			MetaData metaData;
			List<TypeDef> sortedTypes;
			Rows<ModuleDef> moduleDefInfos = new Rows<ModuleDef>();
			Rows<TypeRef> typeRefInfos = new Rows<TypeRef>();
			Rows<TypeDef> typeDefInfos = new Rows<TypeDef>();
			Rows<TypeSpec> typeSpecInfos = new Rows<TypeSpec>();
			Rows<ModuleRef> moduleRefInfos = new Rows<ModuleRef>();
			Rows<AssemblyRef> assemblyRefInfos = new Rows<AssemblyRef>();
			Rows<FieldDef> fieldDefInfos = new Rows<FieldDef>();
			Rows<MethodDef> methodDefInfos = new Rows<MethodDef>();
			Rows<EventDef> eventDefInfos = new Rows<EventDef>();
			Rows<PropertyDef> propertyDefInfos = new Rows<PropertyDef>();
			Rows<MemberRef> memberRefInfos = new Rows<MemberRef>();
			Rows<FileDef> fileDefInfos = new Rows<FileDef>();

			class Rows<T> where T : IMDTokenProvider {
				Dictionary<T, uint> dict = new Dictionary<T, uint>();

				public bool TryGetRid(T value, out uint rid) {
					if (value == null) {
						rid = 0;
						return false;
					}
					return dict.TryGetValue(value, out rid);
				}

				public void Add(T value, uint rid) {
					dict.Add(value, rid);
				}

				public uint Rid(T value) {
					return dict[value];
				}

				public void SetRid(T value, uint rid) {
					dict[value] = rid;
				}
			}

			public NormalTablesCreator(MetaData metaData) {
				this.metaData = metaData;
			}

			public void Create() {
				sortedTypes = GetSortedTypes();

				// Do what the VC# compiler does. It first creates the <Module> type
				// followed by the module row.
				if (sortedTypes.Count > 0)
					AddTypeDef(sortedTypes[0]);
				AddModule(metaData.module);
				for (int i = 1; i < sortedTypes.Count; i++)
					AddTypeDef(sortedTypes[i]);

				uint fieldListRid = 1, methodListRid = 1;
				uint eventListRid = 1, propertyListRid = 1;
				foreach (var type in sortedTypes) {
					if (type == null)
						continue;
					uint typeRid = typeDefInfos.Rid(type);
					var typeRow = metaData.tablesHeap.TypeDefTable[typeRid];
					AddGenericParams(new MDToken(Table.TypeDef, typeRid), type.GenericParams);
					typeRow.Extends = AddTypeDefOrRef(type.BaseType);	//TODO: null is allowed here if <Module> or iface so don't warn user
					typeRow.FieldList = fieldListRid;
					typeRow.MethodList = methodListRid;
					AddDeclSecurities(new MDToken(Table.TypeDef, typeRid), type.DeclSecurities);
					AddInterfaceImpls(typeRid, type.InterfaceImpls);
					AddClassLayout(typeRid, type.ClassLayout);

					foreach (var field in type.Fields) {
						if (field == null)
							continue;
						uint rid = fieldListRid++;
						var row = new RawFieldRow((ushort)field.Flags,
									metaData.stringsHeap.Add(field.Name),
									GetSignature(field.Signature));
						fieldDefInfos.Add(field, rid);
						AddFieldLayout(rid, field.FieldLayout);
						AddFieldMarshal(new MDToken(Table.Field, rid), field.FieldMarshal);
						AddFieldRVA(rid, field.FieldRVA);
						AddImplMap(new MDToken(Table.Field, rid), field.ImplMap);
						AddConstant(new MDToken(Table.Field, rid), field.Constant);
					}

					foreach (var method in type.Methods) {
						if (method == null)
							continue;
						uint rid = methodListRid++;
						var row = new RawMethodRow(0,
									(ushort)method.ImplFlags,
									(ushort)method.Flags,
									metaData.stringsHeap.Add(method.Name),
									GetSignature(method.Signature),
									0);
						methodDefInfos.Add(method, rid);
						AddGenericParams(new MDToken(Table.Method, rid), method.GenericParams);
						AddDeclSecurities(new MDToken(Table.Method, rid), method.DeclSecurities);
						AddImplMap(new MDToken(Table.Method, rid), method.ImplMap);
					}

					if (!IsEmpty(type.Events)) {
						metaData.tablesHeap.EventMapTable.Create(new RawEventMapRow(typeRid, eventListRid));
						foreach (var evt in type.Events) {
							if (evt == null)
								continue;
							uint rid = eventListRid++;
							var row = new RawEventRow((ushort)evt.Flags,
										metaData.stringsHeap.Add(evt.Name),
										AddTypeDefOrRef(evt.Type));
							eventDefInfos.Add(evt, rid);
						}
					}

					if (!IsEmpty(type.Properties)) {
						metaData.tablesHeap.PropertyMapTable.Create(new RawPropertyMapRow(typeRid, propertyListRid));
						foreach (var prop in type.Properties) {
							if (prop == null)
								continue;
							uint rid = propertyListRid++;
							var row = new RawPropertyRow((ushort)prop.Flags,
										metaData.stringsHeap.Add(prop.Name),
										GetSignature(prop.Type));
							propertyDefInfos.Add(prop, rid);
							AddConstant(new MDToken(Table.Property, rid), prop.Constant);
						}
					}
				}

				//TODO: Write all params

				AddAssembly(metaData.module.Assembly);

				foreach (var type in sortedTypes) {
					AddNestedType(type, type.DeclaringType);

					// Second method pass
					foreach (var method in type.Methods) {
						//TODO:
						uint rid = methodDefInfos.Rid(method);
						AddMethodImpls(rid, method.Overrides);
					}
					foreach (var evt in type.Events)
						AddMethodSemantics(evt);
					foreach (var prop in type.Properties)
						AddMethodSemantics(prop);
				}

				//TODO: Write module cust attr
				//TODO: Write assembly cust attr
				//TODO: Write assembly decl security
				//TODO: Sort the tables that must be sorted

				AddResources(metaData.module.Resources);
			}

			static bool IsEmpty<T>(IList<T> list) where T : class {
				if (list == null)
					return true;
				foreach (var e in list) {
					if (e != null)
						return false;
				}
				return true;
			}

			void AddNestedType(TypeDef nestedType, TypeDef declaringType) {
				if (nestedType == null || declaringType == null)
					return;
				uint nestedRid, dtRid;
				if (!typeDefInfos.TryGetRid(nestedType, out nestedRid))
					return;	//TODO: Warn user
				if (!typeDefInfos.TryGetRid(declaringType, out dtRid))
					return;	//TODO: Warn user
				var row = new RawNestedClassRow(nestedRid, dtRid);
				metaData.tablesHeap.NestedClassTable.Add(row);
			}

			uint AddModule(ModuleDef module) {
				if (module == null)
					return 0;	//TODO: Warn user
				var row = new RawModuleRow(module.Generation,
									metaData.stringsHeap.Add(module.Name),
									metaData.guidHeap.Add(module.Mvid),
									metaData.guidHeap.Add(module.EncId),
									metaData.guidHeap.Add(module.EncBaseId));
				uint rid = metaData.tablesHeap.ModuleTable.Create(row);
				moduleDefInfos.Add(module, rid);
				return rid;
			}

			/// <summary>
			/// Creates a <c>TypeDef</c> row and initializes its Flags, Name, and Namespace columns.
			/// </summary>
			/// <param name="td">The type</param>
			/// <returns>The new <c>rid</c></returns>
			uint AddTypeDef(TypeDef td) {
				if (td == null)
					return 0;	//TODO: Warn user
				var row = new RawTypeDefRow((uint)td.Flags,
							metaData.stringsHeap.Add(td.Name),
							metaData.stringsHeap.Add(td.Namespace),
							0, 0, 0);
				uint rid = metaData.tablesHeap.TypeDefTable.Create(row);
				typeDefInfos.Add(td, rid);
				return rid;
			}

			uint GetTypeDefRid(TypeDef td) {
				uint rid;
				if (typeDefInfos.TryGetRid(td, out rid))
					return rid;
				return 0;	//TODO: Warn user
			}

			uint AddTypeDefOrRef(ITypeDefOrRef tdr) {
				uint encodedToken;

				var td = tdr as TypeDef;
				if (td != null) {
					var token = new MDToken(Table.TypeDef, GetTypeDefRid(td));
					if (!CodedToken.TypeDefOrRef.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a TypeDefOrRef token");	//TODO: Instead of throwing here and elsewhere, warn user
					return encodedToken;
				}

				var tr = tdr as TypeRef;
				if (tr != null) {
					var token = new MDToken(Table.TypeRef, AddTypeRef(tr));
					if (!CodedToken.TypeDefOrRef.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a TypeDefOrRef token");
					return encodedToken;
				}

				var ts = tdr as TypeSpec;
				if (ts != null) {
					var token = new MDToken(Table.TypeSpec, AddTypeSpec(ts));
					if (!CodedToken.TypeDefOrRef.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a TypeDefOrRef token");
					return encodedToken;
				}

				return 0;	//TODO: Warn user
			}

			uint AddTypeRef(TypeRef tr) {
				if (tr == null)
					return 0;	//TODO: Warn user
				uint rid;
				if (typeRefInfos.TryGetRid(tr, out rid))
					return rid;	//TODO: If rid == 0, warn user
				typeRefInfos.Add(tr, 0);	// Prevent inf recursion
				var row = new RawTypeRefRow(AddResolutionScope(tr.ResolutionScope),
							metaData.stringsHeap.Add(tr.Name),
							metaData.stringsHeap.Add(tr.Namespace));
				rid = metaData.tablesHeap.TypeRefTable.Add(row);
				typeRefInfos.SetRid(tr, rid);
				return rid;
			}

			uint AddResolutionScope(IResolutionScope rs) {
				uint encodedToken;

				var asmRef = rs as AssemblyRef;
				if (asmRef != null) {
					var token = new MDToken(Table.AssemblyRef, AddAssemblyRef(asmRef));
					if (!CodedToken.ResolutionScope.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a ResolutionScope token");
					return encodedToken;
				}

				var tr = rs as TypeRef;
				if (tr != null) {
					var token = new MDToken(Table.TypeRef, AddTypeRef(tr));
					if (!CodedToken.ResolutionScope.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a ResolutionScope token");
					return encodedToken;
				}

				var modRef = rs as ModuleRef;
				if (modRef != null) {
					var token = new MDToken(Table.ModuleRef, AddModuleRef(modRef));
					if (!CodedToken.ResolutionScope.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a ResolutionScope token");
					return encodedToken;
				}

				//TODO: Error if mod is not metaData.module
				var mod = rs as ModuleDef;
				if (mod != null) {
					var token = new MDToken(Table.Module, AddModule(mod));
					if (!CodedToken.ResolutionScope.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a ResolutionScope token");
					return encodedToken;
				}

				return 0;	//TODO: Warn user
			}

			uint AddTypeSpec(TypeSpec ts) {
				if (ts == null)
					return 0;	//TODO: Warn user
				uint rid;
				if (typeSpecInfos.TryGetRid(ts, out rid))
					return rid;	//TODO: If rid == 0, warn user
				typeSpecInfos.Add(ts, 0);	// Prevent inf recursion
				var row = new RawTypeSpecRow(GetSignature(ts.TypeSig));
				rid = metaData.tablesHeap.TypeSpecTable.Add(row);
				typeSpecInfos.SetRid(ts, rid);
				return rid;
			}

			uint AddModuleRef(ModuleRef modRef) {
				if (modRef == null)
					return 0;	//TODO: Warn user
				uint rid;
				if (moduleRefInfos.TryGetRid(modRef, out rid))
					return rid;
				var row = new RawModuleRefRow(metaData.stringsHeap.Add(modRef.Name));
				rid = metaData.tablesHeap.ModuleRefTable.Add(row);
				moduleRefInfos.Add(modRef, rid);
				return rid;
			}

			uint AddAssemblyRef(AssemblyRef asmRef) {
				if (asmRef == null)
					return 0;	//TODO: Warn user
				uint rid;
				if (assemblyRefInfos.TryGetRid(asmRef, out rid))
					return rid;
				var version = Utils.CreateVersionWithNoUndefinedValues(asmRef.Version);
				var row = new RawAssemblyRefRow((ushort)version.Major,
								(ushort)version.Minor,
								(ushort)version.Build,
								(ushort)version.Revision,
								(uint)asmRef.Flags,
								metaData.blobHeap.Add(GetPublicKeyOrTokenData(asmRef.PublicKeyOrToken)),
								metaData.stringsHeap.Add(asmRef.Name),
								metaData.stringsHeap.Add(asmRef.Locale),
								metaData.blobHeap.Add(asmRef.HashValue));
				rid = metaData.tablesHeap.AssemblyRefTable.Add(row);
				assemblyRefInfos.Add(asmRef, rid);
				return rid;
			}

			void AddAssembly(AssemblyDef asm) {
				if (asm == null)
					return;
				var version = Utils.CreateVersionWithNoUndefinedValues(asm.Version);
				var row = new RawAssemblyRow((uint)asm.HashAlgId,
								(ushort)version.Major,
								(ushort)version.Minor,
								(ushort)version.Build,
								(ushort)version.Revision,
								(uint)asm.Flags,
								metaData.blobHeap.Add(GetPublicKeyOrTokenData(asm.PublicKeyOrToken)),
								metaData.stringsHeap.Add(asm.Name),
								metaData.stringsHeap.Add(asm.Locale));
				metaData.tablesHeap.AssemblyTable.Add(row);
			}

			static byte[] GetPublicKeyOrTokenData(PublicKeyBase pkb) {
				if (pkb == null)
					return null;
				return pkb.Data;
			}

			void AddGenericParams(MDToken token, IList<GenericParam> gps) {
				if (gps == null)
					return;
				foreach (var gp in gps)
					AddGenericParam(token, gp);
			}

			void AddGenericParam(MDToken owner, GenericParam gp) {
				if (gp == null)
					return;	//TODO: Warn user
				uint encodedOwner;
				if (!CodedToken.TypeOrMethodDef.Encode(owner, out encodedOwner))
					throw new ModuleWriterException("Can't encode GenericParam owner token");
				var row = new RawGenericParamRow(gp.Number,
								(ushort)gp.Flags,
								encodedOwner,
								metaData.stringsHeap.Add(gp.Name),
								gp.Kind == null ? 0 : AddTypeDefOrRef(gp.Kind));
				uint rid = metaData.tablesHeap.GenericParamTable.Create(row);
				AddGenericParamConstraints(rid, gp.GenericParamConstraints);
			}

			void AddGenericParamConstraints(uint gpRid, IList<GenericParamConstraint> constraints) {
				if (constraints == null)
					return;
				foreach (var gpc in constraints)
					AddGenericParamConstraint(gpRid, gpc);
			}

			void AddGenericParamConstraint(uint gpRid, GenericParamConstraint gpc) {
				if (gpc == null)
					return;
				var row = new RawGenericParamConstraintRow(gpRid, AddTypeDefOrRef(gpc.Constraint));
				metaData.tablesHeap.GenericParamConstraintTable.Add(row);
			}

			void AddInterfaceImpls(uint typeDefRid, IList<InterfaceImpl> ifaces) {
				foreach (var iface in ifaces) {
					if (iface == null)
						continue;
					var row = new RawInterfaceImplRow(typeDefRid,
								AddTypeDefOrRef(iface.Interface));
					uint rid = metaData.tablesHeap.InterfaceImplTable.Create(row);
					//TODO: Write custom attrs
				}
			}

			void AddFieldLayout(uint fieldRid, FieldLayout fieldLayout) {
				if (fieldLayout == null)
					return;
				var row = new RawFieldLayoutRow(fieldLayout.Offset, fieldRid);
				metaData.tablesHeap.FieldLayoutTable.Add(row);
			}

			void AddFieldMarshal(MDToken parent, FieldMarshal fieldMarshal) {
				if (fieldMarshal == null)
					return;
				uint encodedParent;
				if (!CodedToken.HasFieldMarshal.Encode(parent, out encodedParent))
					throw new ModuleWriterException("Can't encode a HasFieldMarshal token");
				var row = new RawFieldMarshalRow(encodedParent,
							metaData.blobHeap.Add(fieldMarshal.NativeType));
				metaData.tablesHeap.FieldMarshalTable.Add(row);
			}

			void AddFieldRVA(uint fieldRid, FieldRVA fieldRVA) {
				if (fieldRVA == null)
					return;
				var row = new RawFieldRVARow((uint)fieldRVA.RVA, fieldRid);
				metaData.tablesHeap.FieldRVATable.Add(row);
			}

			void AddImplMap(MDToken parent, ImplMap implMap) {
				if (implMap == null)
					return;
				uint encodedParent;
				if (!CodedToken.MemberForwarded.Encode(parent, out encodedParent))
					throw new ModuleWriterException("Can't encode a MemberForwarded token");
				var row = new RawImplMapRow((ushort)implMap.Flags,
							encodedParent,
							metaData.stringsHeap.Add(implMap.Name),
							AddModuleRef(implMap.Scope));
				metaData.tablesHeap.ImplMapTable.Add(row);
			}

			void AddConstant(MDToken parent, Constant constant) {
				if (constant == null)
					return;
				uint encodedParent;
				if (!CodedToken.HasConstant.Encode(parent, out encodedParent))
					throw new ModuleWriterException("Can't encode a HasConstant token");
				var row = new RawConstantRow((byte)constant.Type, 0,
							encodedParent,
							metaData.blobHeap.Add(GetConstantValueAsByteArray(constant.Type, constant.Value)));
				metaData.tablesHeap.ConstantTable.Add(row);
			}

			static readonly byte[] constantClassByteArray = new byte[4];
			static readonly byte[] constantDefaultByteArray = new byte[8];
			byte[] GetConstantValueAsByteArray(ElementType etype, object o) {
				if (o == null) {
					if (etype == ElementType.Class)
						return constantClassByteArray;
					return constantDefaultByteArray;	//TODO: Warn user
				}

				switch (Type.GetTypeCode(o.GetType())) {
				case TypeCode.Boolean:
					//TODO: if etype is not same type, warn user
					return BitConverter.GetBytes((bool)o);

				case TypeCode.Char:
					//TODO: if etype is not same type, warn user
					return BitConverter.GetBytes((char)o);

				case TypeCode.SByte:
					//TODO: if etype is not same type, warn user
					return BitConverter.GetBytes((sbyte)o);

				case TypeCode.Byte:
					//TODO: if etype is not same type, warn user
					return BitConverter.GetBytes((byte)o);

				case TypeCode.Int16:
					//TODO: if etype is not same type, warn user
					return BitConverter.GetBytes((short)o);

				case TypeCode.UInt16:
					//TODO: if etype is not same type, warn user
					return BitConverter.GetBytes((ushort)o);

				case TypeCode.Int32:
					//TODO: if etype is not same type, warn user
					return BitConverter.GetBytes((int)o);

				case TypeCode.UInt32:
					//TODO: if etype is not same type, warn user
					return BitConverter.GetBytes((uint)o);

				case TypeCode.Int64:
					//TODO: if etype is not same type, warn user
					return BitConverter.GetBytes((long)o);

				case TypeCode.UInt64:
					//TODO: if etype is not same type, warn user
					return BitConverter.GetBytes((ulong)o);

				case TypeCode.Single:
					//TODO: if etype is not same type, warn user
					return BitConverter.GetBytes((float)o);

				case TypeCode.Double:
					//TODO: if etype is not same type, warn user
					return BitConverter.GetBytes((double)o);

				case TypeCode.String:
					//TODO: if etype is not same type, warn user
					return Encoding.Unicode.GetBytes((string)o);

				default:
					return constantDefaultByteArray;	//TODO: warn user
				}
			}

			void AddDeclSecurities(MDToken parent, IList<DeclSecurity> declSecurities) {
				if (declSecurities == null)
					return;
				uint encodedParent;
				if (!CodedToken.HasDeclSecurity.Encode(parent, out encodedParent))
					throw new ModuleWriterException("Can't encode a HasDeclSecurity token");
				foreach (var decl in declSecurities) {
					if (decl == null)
						continue;
					var row = new RawDeclSecurityRow((short)decl.Action,
								encodedParent,
								metaData.blobHeap.Add(decl.PermissionSet));
					metaData.tablesHeap.DeclSecurityTable.Create(row);
				}
			}

			void AddMethodSemantics(EventDef evt) {
				if (evt == null)
					return;	//TODO: Warn user
				uint rid;
				if (!eventDefInfos.TryGetRid(evt, out rid))
					return;	//TODO: Warn user
				var token = new MDToken(Table.Event, rid);
				AddMethodSemantics(token, evt.AddMethod, MethodSemanticsAttributes.AddOn);
				AddMethodSemantics(token, evt.RemoveMethod, MethodSemanticsAttributes.RemoveOn);
				AddMethodSemantics(token, evt.InvokeMethod, MethodSemanticsAttributes.Fire);
				AddMethodSemantics(token, evt.OtherMethods);
			}

			void AddMethodSemantics(PropertyDef prop) {
				if (prop == null)
					return;	//TODO: Warn user
				uint rid;
				if (!propertyDefInfos.TryGetRid(prop, out rid))
					return;	//TODO: Warn user
				var token = new MDToken(Table.Property, rid);
				AddMethodSemantics(token, prop.GetMethod, MethodSemanticsAttributes.Getter);
				AddMethodSemantics(token, prop.SetMethod, MethodSemanticsAttributes.Setter);
				AddMethodSemantics(token, prop.OtherMethods);
			}

			void AddMethodSemantics(MDToken owner, IList<MethodDef> otherMethods) {
				if (otherMethods == null)
					return;
				foreach (var method in otherMethods)
					AddMethodSemantics(owner, method, MethodSemanticsAttributes.Other);
			}

			void AddMethodSemantics(MDToken owner, MethodDef method, MethodSemanticsAttributes flags) {
				uint methodRid;
				if (!methodDefInfos.TryGetRid(method, out methodRid))
					return;	//TODO: Warn user
				uint encodedOwner;
				if (!CodedToken.HasSemantic.Encode(owner, out encodedOwner))
					throw new ModuleWriterException("Can't encode a HasSemantic token");
				var row = new RawMethodSemanticsRow((ushort)flags, methodRid, encodedOwner);
				metaData.tablesHeap.MethodSemanticsTable.Add(row);
			}

			void AddMethodImpls(uint rid, IList<MethodOverride> overrides) {
				if (overrides == null)
					return;
				foreach (var ovr in overrides) {
					var row = new RawMethodImplRow(rid,
								AddMethodDefOrRef(ovr.MethodBody),
								AddMethodDefOrRef(ovr.MethodDeclaration));
					metaData.tablesHeap.MethodImplTable.Add(row);
				}
			}

			void AddClassLayout(uint typeRid, ClassLayout classLayout) {
				if (classLayout == null)
					return;
				var row = new RawClassLayoutRow(classLayout.PackingSize, classLayout.ClassSize, typeRid);
				metaData.tablesHeap.ClassLayoutTable.Add(row);
			}

			uint AddMethodDefOrRef(IMethodDefOrRef mdr) {
				uint encodedToken;

				var md = mdr as MethodDef;
				if (md != null) {
					uint rid;
					if (!methodDefInfos.TryGetRid(md, out rid)) {
						//TODO: method is not in this module. warn user.
					}
					if (!CodedToken.MethodDefOrRef.Encode(new MDToken(Table.Method, rid), out encodedToken))
						throw new ModuleWriterException("Can't encode a MethodDefOrRef token");
					return encodedToken;
				}

				var mr = mdr as MemberRef;
				if (mr != null) {
					if (!CodedToken.MethodDefOrRef.Encode(new MDToken(Table.MemberRef, AddMemberRef(mr)), out encodedToken))
						throw new ModuleWriterException("Can't encode a MethodDefOrRef token");
					return encodedToken;
				}

				return 0;	//TODO: Warn user
			}

			uint AddMemberRef(MemberRef mr) {
				if (mr == null)
					return 0;	//TODO: Warn user
				uint rid;
				if (memberRefInfos.TryGetRid(mr, out rid))
					return rid;
				var row = new RawMemberRefRow(AddMemberRefParent(mr.Class),
								metaData.stringsHeap.Add(mr.Name),
								GetSignature(mr.Signature));
				rid = metaData.tablesHeap.MemberRefTable.Add(row);
				memberRefInfos.Add(mr, rid);
				return rid;
			}

			uint AddMemberRefParent(IMemberRefParent parent) {
				if (parent == null)
					return 0;	//TODO: Warn user

				return 0;	//TODO:
			}

			void AddResources(IList<Resource> resources) {
				if (resources == null)
					return;
				foreach (var resource in resources)
					AddResource(resource);
			}

			void AddResource(Resource resource) {
				var er = resource as EmbeddedResource;
				if (er != null) {
					AddEmbeddedResource(er);
					return;
				}

				var alr = resource as AssemblyLinkedResource;
				if (alr != null) {
					AddAssemblyLinkedResource(alr);
					return;
				}

				var lr = resource as LinkedResource;
				if (lr != null) {
					AddLinkedResource(lr);
					return;
				}

				//TODO: Warn user
			}

			void AddEmbeddedResource(EmbeddedResource er) {
				if (er == null)
					return;
				var row = new RawManifestResourceRow(metaData.netResources.NextOffset,
							(uint)er.Flags,
							metaData.stringsHeap.Add(er.Name),
							0);
				metaData.tablesHeap.ManifestResourceTable.Create(row);
				metaData.netResources.Add(er.Data);
			}

			void AddAssemblyLinkedResource(AssemblyLinkedResource alr) {
				if (alr == null)
					return;
				var row = new RawManifestResourceRow(0,
							(uint)alr.Flags,
							metaData.stringsHeap.Add(alr.Name),
							AddAssemblyRef(alr.Assembly));
				metaData.tablesHeap.ManifestResourceTable.Create(row);
			}

			void AddLinkedResource(LinkedResource lr) {
				if (lr == null)
					return;
				var row = new RawManifestResourceRow(0,
							(uint)lr.Flags,
							metaData.stringsHeap.Add(lr.Name),
							AddFile(lr.File));
				metaData.tablesHeap.ManifestResourceTable.Create(row);
			}

			uint AddFile(FileDef file) {
				if (file == null)
					return 0;	//TODO: Warn user
				uint rid;
				if (fileDefInfos.TryGetRid(file, out rid))
					return rid;
				var row = new RawFileRow((uint)file.Flags,
							metaData.stringsHeap.Add(file.Name),
							metaData.blobHeap.Add(file.HashValue));	//TODO: Re-calculate the hash value if possible
				fileDefInfos.Add(file, rid);
				return rid;
			}

			uint GetSignature(TypeSig ts) {
				if (ts == null)
					return 0;	//TODO: Warn user

				return 0;	//TODO:
			}

			uint GetSignature(CallingConventionSig sig) {
				if (sig == null)
					return 0;	//TODO: Warn user

				return 0;	//TODO:
			}

			/// <summary>
			/// Gets all <see cref="TypeDef"/>s in the order that they'll be saved in the
			/// <c>TypeDef</c> table.
			/// </summary>
			List<TypeDef> GetSortedTypes() {
				// All nested types must be after their enclosing type. This is exactly
				// what module.GetTypes() does.
				return new List<TypeDef>(metaData.module.GetTypes());
			}
		}

		class PreserveTokensTablesWriter : ITablesCreator {
			MetaData metaData;

			public PreserveTokensTablesWriter(MetaData metaData) {
				this.metaData = metaData;
			}

			public void Create() {
				throw new NotImplementedException();	//TODO:
			}
		}

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Gets/sets the options
		/// </summary>
		public MetaDataOptions Options {
			get { return options; }
			set { options = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataOptions.PreserveTokens"/> bit
		/// </summary>
		public bool PreserveTokens {
			get { return (options & MetaDataOptions.PreserveTokens) != 0; }
			set {
				if (value)
					options |= MetaDataOptions.PreserveTokens;
				else
					options &= ~MetaDataOptions.PreserveTokens;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataOptions.PreserveStringsOffsets"/> bit
		/// </summary>
		public bool PreserveStringsOffsets {
			get { return (options & MetaDataOptions.PreserveStringsOffsets) != 0; }
			set {
				if (value)
					options |= MetaDataOptions.PreserveStringsOffsets;
				else
					options &= ~MetaDataOptions.PreserveStringsOffsets;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataOptions.PreserveUSOffsets"/> bit
		/// </summary>
		public bool PreserveUSOffsets {
			get { return (options & MetaDataOptions.PreserveUSOffsets) != 0; }
			set {
				if (value)
					options |= MetaDataOptions.PreserveUSOffsets;
				else
					options &= ~MetaDataOptions.PreserveUSOffsets;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataOptions.PreserveBlobOffsets"/> bit
		/// </summary>
		public bool PreserveBlobOffsets {
			get { return (options & MetaDataOptions.PreserveBlobOffsets) != 0; }
			set {
				if (value)
					options |= MetaDataOptions.PreserveBlobOffsets;
				else
					options &= ~MetaDataOptions.PreserveBlobOffsets;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="constants">Constants list</param>
		/// <param name="methodBodies">Method bodies list</param>
		/// <param name="netResources">.NET resources list</param>
		public MetaData(ModuleDef module, UniqueChunkList<ByteArrayChunk> constants, MethodBodyChunks methodBodies, NetResources netResources) {
			this.module = module;
			this.constants = constants;
			this.methodBodies = methodBodies;
			this.netResources = netResources;
			this.tablesHeap = new TablesHeap();
			this.stringsHeap = new StringsHeap();
			this.usHeap = new USHeap();
			this.guidHeap = new GuidHeap();
			this.blobHeap = new BlobHeap();
		}

		/// <summary>
		/// Creates the .NET metadata tables
		/// </summary>
		public void CreateTables() {
			if (module.Types.Count == 0 || module.Types[0] == null)
				throw new ModuleWriterException("Missing <Module> type");

			var moduleDefMD = module as ModuleDefMD;
			if (moduleDefMD != null) {
				if (PreserveStringsOffsets)
					stringsHeap.Populate(moduleDefMD.StringsStream);
				if (PreserveUSOffsets)
					usHeap.Populate(moduleDefMD.USStream);
				if (PreserveBlobOffsets)
					blobHeap.Populate(moduleDefMD.BlobStream);
			}

			if (PreserveTokens)
				tablesCreator = new PreserveTokensTablesWriter(this);
			else
				tablesCreator = new NormalTablesCreator(this);

			tablesCreator.Create();
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
			throw new System.NotImplementedException();
		}

		/// <inheritdoc/>
		public uint GetLength() {
			throw new System.NotImplementedException();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			throw new System.NotImplementedException();
		}
	}
}
