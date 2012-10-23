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
	public enum MetaDataOptions {
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
	public abstract class MetaData : IChunk, ISignatureWriterHelper, ITokenCreator, ICustomAttributeWriterHelper {
		internal ModuleDef module;
		internal UniqueChunkList<ByteArrayChunk> constants;
		internal MethodBodyChunks methodBodies;
		internal NetResources netResources;
		internal TablesHeap tablesHeap;
		internal StringsHeap stringsHeap;
		internal USHeap usHeap;
		internal GuidHeap guidHeap;
		internal BlobHeap blobHeap;
		FileOffset offset;
		RVA rva;
		MetaDataOptions options;

		internal List<TypeDef> allTypeDefs;
		internal Rows<ModuleDef> moduleDefInfos = new Rows<ModuleDef>();
		internal SortedRows<InterfaceImpl, RawInterfaceImplRow> interfaceImplInfos = new SortedRows<InterfaceImpl, RawInterfaceImplRow>();
		internal SortedRows<IHasConstant, RawConstantRow> hasConstantInfos = new SortedRows<IHasConstant, RawConstantRow>();
		internal SortedRows<CustomAttribute, RawCustomAttributeRow> customAttributeInfos = new SortedRows<CustomAttribute, RawCustomAttributeRow>();
		internal SortedRows<IHasFieldMarshal, RawFieldMarshalRow> fieldMarshalInfos = new SortedRows<IHasFieldMarshal, RawFieldMarshalRow>();
		internal SortedRows<DeclSecurity, RawDeclSecurityRow> declSecurityInfos = new SortedRows<DeclSecurity, RawDeclSecurityRow>();
		internal SortedRows<TypeDef, RawClassLayoutRow> classLayoutInfos = new SortedRows<TypeDef, RawClassLayoutRow>();
		internal SortedRows<FieldDef, RawFieldLayoutRow> fieldLayoutInfos = new SortedRows<FieldDef, RawFieldLayoutRow>();
		internal Rows<TypeDef> eventMapInfos = new Rows<TypeDef>();
		internal Rows<TypeDef> propertyMapInfos = new Rows<TypeDef>();
		internal SortedRows<MethodDef, RawMethodSemanticsRow> methodSemanticsInfos = new SortedRows<MethodDef, RawMethodSemanticsRow>();
		internal SortedRows<MethodDef, RawMethodImplRow> methodImplInfos = new SortedRows<MethodDef, RawMethodImplRow>();
		internal Rows<ModuleRef> moduleRefInfos = new Rows<ModuleRef>();
		internal SortedRows<IMemberForwarded, RawImplMapRow> implMapInfos = new SortedRows<IMemberForwarded, RawImplMapRow>();
		internal SortedRows<FieldDef, RawFieldRVARow> fieldRVAInfos = new SortedRows<FieldDef, RawFieldRVARow>();
		internal Rows<AssemblyDef> assemblyInfos = new Rows<AssemblyDef>();
		internal Rows<AssemblyRef> assemblyRefInfos = new Rows<AssemblyRef>();
		internal Rows<FileDef> fileDefInfos = new Rows<FileDef>();
		internal Rows<ExportedType> exportedTypeInfos = new Rows<ExportedType>();
		internal Rows<Resource> manifestResourceInfos = new Rows<Resource>();
		internal SortedRows<TypeDef, RawNestedClassRow> nestedClassInfos = new SortedRows<TypeDef, RawNestedClassRow>();
		internal SortedRows<GenericParam, RawGenericParamRow> genericParamInfos = new SortedRows<GenericParam, RawGenericParamRow>();
		internal SortedRows<GenericParamConstraint, RawGenericParamConstraintRow> genericParamConstraintInfos = new SortedRows<GenericParamConstraint, RawGenericParamConstraintRow>();

		internal class SortedRows<T, TRow>
			where T : class
			where TRow : class {
			public List<Info> infos = new List<Info>();

			public struct Info {
				public uint owner;
				public T data;
				public TRow row;
				public Info(uint owner, T data, TRow row) {
					this.owner = owner;
					this.data = data;
					this.row = row;
				}
			}

			public void Add(uint owner, T data, TRow row) {
				infos.Add(new Info(owner, data, row));
			}
		}

		internal class Rows<T> where T : class {
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

		/// <summary>
		/// Creates a <see cref="MetaData"/> instance
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="constants">Constants list</param>
		/// <param name="methodBodies">Method bodies list</param>
		/// <param name="netResources">.NET resources list</param>
		/// <returns>A new <see cref="MetaData"/> instance</returns>
		public static MetaData Create(ModuleDef module, UniqueChunkList<ByteArrayChunk> constants, MethodBodyChunks methodBodies, NetResources netResources) {
			return Create(module, constants, methodBodies, netResources, 0);
		}

		/// <summary>
		/// Creates a <see cref="MetaData"/> instance
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="constants">Constants list</param>
		/// <param name="methodBodies">Method bodies list</param>
		/// <param name="netResources">.NET resources list</param>
		/// <param name="options">Options</param>
		/// <returns>A new <see cref="MetaData"/> instance</returns>
		public static MetaData Create(ModuleDef module, UniqueChunkList<ByteArrayChunk> constants, MethodBodyChunks methodBodies, NetResources netResources, MetaDataOptions options) {
			if ((options & MetaDataOptions.PreserveTokens) != 0)
				return new PreserveTokensMetaData(module, constants, methodBodies, netResources, options);
			return new NormalMetaData(module, constants, methodBodies, netResources, options);
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
		/// Gets the <see cref="MetaDataOptions.PreserveTokens"/> bit
		/// </summary>
		public bool PreserveTokens {
			get { return (options & MetaDataOptions.PreserveTokens) != 0; }
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
		/// <param name="options">Options</param>
		internal MetaData(ModuleDef module, UniqueChunkList<ByteArrayChunk> constants, MethodBodyChunks methodBodies, NetResources netResources, MetaDataOptions options) {
			this.module = module;
			this.constants = constants;
			this.methodBodies = methodBodies;
			this.netResources = netResources;
			this.options = options;
			this.tablesHeap = new TablesHeap();
			this.stringsHeap = new StringsHeap();
			this.usHeap = new USHeap();
			this.guidHeap = new GuidHeap();
			this.blobHeap = new BlobHeap();
		}

		/// <summary>
		/// Called when an error is detected
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="args">Optional message arguments</param>
		protected void Error(string message, params object[] args) {
			//TODO:
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

			Create();
		}

		void Create() {
			allTypeDefs = new List<TypeDef>(GetAllTypeDefs());
			AddModule(module);
			AllocateTypeDefRids();
			AllocateMemberDefRids();

			foreach (var type in allTypeDefs) {
				if (type == null)
					continue;
				uint typeRid = GetTypeDefRid(type);
				var typeRow = tablesHeap.TypeDefTable[typeRid];
				typeRow.Flags = (uint)type.Flags;
				typeRow.Name = stringsHeap.Add(type.Name);
				typeRow.Namespace = stringsHeap.Add(type.Namespace);
				typeRow.Extends = AddTypeDefOrRef(type.BaseType);	//TODO: null is allowed here if <Module> or iface so don't warn user
				AddGenericParams(new MDToken(Table.TypeDef, typeRid), type.GenericParams);
				AddDeclSecurities(new MDToken(Table.TypeDef, typeRid), type.DeclSecurities);
				AddInterfaceImpls(typeRid, type.InterfaceImpls);
				AddClassLayout(type);

				foreach (var field in type.Fields) {
					if (field == null)
						continue;
					uint rid = GetFieldRid(field);
					var row = tablesHeap.FieldTable[rid];
					row.Flags = (ushort)field.Flags;
					row.Name = stringsHeap.Add(field.Name);
					row.Signature = GetSignature(field.Signature);
					AddFieldLayout(field);
					AddFieldMarshal(new MDToken(Table.Field, rid), field);
					AddFieldRVA(field);
					AddImplMap(new MDToken(Table.Field, rid), field);
					AddConstant(new MDToken(Table.Field, rid), field);
				}

				foreach (var method in type.Methods) {
					if (method == null)
						continue;
					uint rid = GetMethodRid(method);
					var row = tablesHeap.MethodTable[rid];
					row.ImplFlags = (ushort)method.ImplFlags;
					row.Flags = (ushort)method.Flags;
					row.Name = stringsHeap.Add(method.Name);
					row.Signature = GetSignature(method.Signature);
					AddGenericParams(new MDToken(Table.Method, rid), method.GenericParams);
					AddDeclSecurities(new MDToken(Table.Method, rid), method.DeclSecurities);
					AddImplMap(new MDToken(Table.Method, rid), method);
				}

				if (!IsEmpty(type.Events)) {
					foreach (var evt in type.Events) {
						if (evt == null)
							continue;
						uint rid = GetEventRid(evt);
						var row = tablesHeap.EventTable[rid];
						row.EventFlags = (ushort)evt.Flags;
						row.Name = stringsHeap.Add(evt.Name);
						row.EventType = AddTypeDefOrRef(evt.Type);
					}
				}

				if (!IsEmpty(type.Properties)) {
					foreach (var prop in type.Properties) {
						if (prop == null)
							continue;
						uint rid = GetPropertyRid(prop);
						var row = tablesHeap.PropertyTable[rid];
						row.PropFlags = (ushort)prop.Flags;
						row.Name = stringsHeap.Add(prop.Name);
						row.Type = GetSignature(prop.Type);
						AddConstant(new MDToken(Table.Property, rid), prop);
					}
				}
			}

			//TODO: Sort more tables

			//TODO: Write all params

			AddAssembly(module.Assembly);

			// Second pass now that we know their rids
			foreach (var type in allTypeDefs) {
				if (type == null)
					continue;
				AddCustomAttributes(Table.TypeDef, GetTypeDefRid(type), type);
				AddNestedType(type, type.DeclaringType);

				foreach (var field in type.Fields) {
					if (field == null)
						continue;
					AddCustomAttributes(Table.Field, GetFieldRid(field), field);
				}

				foreach (var method in type.Methods) {
					if (method == null)
						continue;
					AddCustomAttributes(Table.Method, GetMethodRid(method), method);
					AddMethodImpls(method, method.Overrides);

					//TODO:
					var cilBody = method.CilBody;
					if (cilBody != null) {
						var writer = new MethodBodyWriter(this, cilBody);
						writer.Write();
						var code = writer.Code;
						var ehs = writer.ExtraSections;
					}
				}
				foreach (var evt in type.Events) {
					if (evt == null)
						continue;
					AddCustomAttributes(Table.Event, GetEventRid(evt), evt);
					AddMethodSemantics(evt);
				}
				foreach (var prop in type.Properties) {
					if (prop == null)
						continue;
					AddCustomAttributes(Table.Property, GetPropertyRid(prop), prop);
					AddMethodSemantics(prop);
				}
			}

			//TODO: Write module cust attr
			//TODO: Write assembly cust attr
			//TODO: Write assembly decl security
			//TODO: Sort the tables that must be sorted

			AddResources(module.Resources);
		}

		/// <summary>
		/// Checks whether a list is empty or whether it contains only <c>null</c>s
		/// </summary>
		/// <typeparam name="T">Any type</typeparam>
		/// <param name="list">The list</param>
		/// <returns><c>true</c> if the list is empty or if it contains only <c>null</c>s, <c>false</c> otherwise</returns>
		protected static bool IsEmpty<T>(IList<T> list) where T : class {
			if (list == null)
				return true;
			foreach (var e in list) {
				if (e != null)
					return false;
			}
			return true;
		}

		/// <inheritdoc/>
		public MDToken GetToken(object o) {
			var tp = o as IMDTokenProvider;
			if (tp != null)
				return new MDToken(tp.MDToken.Table, AddMDTokenProvider(tp));

			var s = o as string;
			if (s != null)
				return new MDToken((Table)0x70, usHeap.Add(s));

			if (o == null)
				Error("Instruction operand is null");
			else
				Error("Invalid instruction operand");
			return new MDToken((Table)0xFF, 0x00FFFFFF);
		}

		/// <inheritdoc/>
		public MDToken GetToken(IList<TypeSig> locals, uint origToken) {
			if (locals == null || locals.Count == 0)
				return new MDToken((Table)0, 0);

			var row = new RawStandAloneSigRow(GetSignature(new LocalSig(locals, false)));
			uint rid = tablesHeap.StandAloneSigTable.Add(row);
			//TODO: Add custom attributes
			return new MDToken(Table.StandAloneSig, rid);
		}

		uint AddMDTokenProvider(IMDTokenProvider tp) {
			if (tp != null) {
				switch (tp.MDToken.Table) {
				case Table.Module:
					return AddModule((ModuleDef)tp);

				case Table.TypeRef:
					return AddTypeRef((TypeRef)tp);

				case Table.TypeDef:
					return GetTypeDefRid((TypeDef)tp);

				case Table.Field:
					return GetFieldRid((FieldDef)tp);

				case Table.Method:
					return GetMethodRid((MethodDef)tp);

				case Table.Param:
					return GetParamRid((ParamDef)tp);

				case Table.MemberRef:
					return AddMemberRef((MemberRef)tp);

				case Table.StandAloneSig:
					return AddStandAloneSig((StandAloneSig)tp);

				case Table.Event:
					return GetEventRid((EventDef)tp);

				case Table.Property:
					return GetPropertyRid((PropertyDef)tp);

				case Table.ModuleRef:
					return AddModuleRef((ModuleRef)tp);

				case Table.TypeSpec:
					return AddTypeSpec((TypeSpec)tp);

				case Table.Assembly:
					return AddAssembly((AssemblyDef)tp);

				case Table.AssemblyRef:
					return AddAssemblyRef((AssemblyRef)tp);

				case Table.File:
					return AddFile((FileDef)tp);

				case Table.ExportedType:
					return AddExportedType((ExportedType)tp);

				case Table.MethodSpec:
					return AddMethodSpec((MethodSpec)tp);

				case Table.FieldPtr:
				case Table.MethodPtr:
				case Table.ParamPtr:
				case Table.InterfaceImpl:
				case Table.Constant:
				case Table.CustomAttribute:
				case Table.FieldMarshal:
				case Table.DeclSecurity:
				case Table.ClassLayout:
				case Table.FieldLayout:
				case Table.EventMap:
				case Table.EventPtr:
				case Table.PropertyMap:
				case Table.PropertyPtr:
				case Table.MethodSemantics:
				case Table.MethodImpl:
				case Table.ImplMap:
				case Table.FieldRVA:
				case Table.ENCLog:
				case Table.ENCMap:
				case Table.AssemblyProcessor:
				case Table.AssemblyOS:
				case Table.AssemblyRefProcessor:
				case Table.AssemblyRefOS:
				case Table.ManifestResource:
				case Table.NestedClass:
				case Table.GenericParam:
				case Table.GenericParamConstraint:
				default:
					break;
				}
			}

			if (tp == null)
				Error("IMDTokenProvider is null");
			else
				Error("Invalid IMDTokenProvider");
			return 0;
		}

		/// <summary>
		/// Adds a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="tdr">Value</param>
		/// <returns>Its encoded token</returns>
		protected uint AddTypeDefOrRef(ITypeDefOrRef tdr) {
			if (tdr == null) {
				Error("TypeDefOrRef is null");
				return 0;
			}

			var token = new MDToken(tdr.MDToken.Table, AddMDTokenProvider(tdr));
			uint encodedToken;
			if (!CodedToken.TypeDefOrRef.Encode(token, out encodedToken)) {
				Error("Can't encode TypeDefOrRef token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			return encodedToken;
		}

		/// <summary>
		/// Adds a <see cref="IResolutionScope"/>
		/// </summary>
		/// <param name="rs">Value</param>
		/// <returns>Its encoded token</returns>
		protected uint AddResolutionScope(IResolutionScope rs) {
			if (rs == null) {
				Error("ResolutionScope is null");
				return 0;
			}

			var token = new MDToken(rs.MDToken.Table, AddMDTokenProvider(rs));
			uint encodedToken;
			if (!CodedToken.ResolutionScope.Encode(token, out encodedToken)) {
				Error("Can't encode ResolutionScope token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			return encodedToken;
		}

		/// <summary>
		/// Adds a <see cref="IMethodDefOrRef"/>
		/// </summary>
		/// <param name="mdr">Value</param>
		/// <returns>Its encoded token</returns>
		protected uint AddMethodDefOrRef(IMethodDefOrRef mdr) {
			if (mdr == null) {
				Error("MethodDefOrRef is null");
				return 0;
			}

			var token = new MDToken(mdr.MDToken.Table, AddMDTokenProvider(mdr));
			uint encodedToken;
			if (!CodedToken.MethodDefOrRef.Encode(token, out encodedToken)) {
				Error("Can't encode MethodDefOrRef token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			return encodedToken;
		}

		/// <summary>
		/// Adds a <see cref="IMemberRefParent"/>
		/// </summary>
		/// <param name="parent">Value</param>
		/// <returns>Its encoded token</returns>
		protected uint AddMemberRefParent(IMemberRefParent parent) {
			if (parent == null) {
				Error("MemberRefParent is null");
				return 0;
			}

			var token = new MDToken(parent.MDToken.Table, AddMDTokenProvider(parent));
			uint encodedToken;
			if (!CodedToken.MemberRefParent.Encode(token, out encodedToken)) {
				Error("Can't encode MemberRefParent token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			return encodedToken;
		}

		/// <summary>
		/// Adds a <see cref="IImplementation"/>
		/// </summary>
		/// <param name="impl">Value</param>
		/// <returns>Its encoded token</returns>
		protected uint AddImplementation(IImplementation impl) {
			if (impl == null) {
				Error("Implementation is null");
				return 0;
			}

			var token = new MDToken(impl.MDToken.Table, AddMDTokenProvider(impl));
			uint encodedToken;
			if (!CodedToken.Implementation.Encode(token, out encodedToken)) {
				Error("Can't encode Implementation token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			return encodedToken;
		}

		/// <summary>
		/// Adds a <see cref="ICustomAttributeType"/>
		/// </summary>
		/// <param name="cat">Value</param>
		/// <returns>Its encoded token</returns>
		protected uint AddCustomAttributeType(ICustomAttributeType cat) {
			if (cat == null) {
				Error("CustomAttributeType is null");
				return 0;
			}

			var token = new MDToken(cat.MDToken.Table, AddMDTokenProvider(cat));
			uint encodedToken;
			if (!CodedToken.CustomAttributeType.Encode(token, out encodedToken)) {
				Error("Can't encode CustomAttributeType token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			return encodedToken;
		}

		/// <summary>
		/// Adds a <c>NestedType</c> row
		/// </summary>
		/// <param name="nestedType">Nested type</param>
		/// <param name="declaringType">Declaring type</param>
		protected void AddNestedType(TypeDef nestedType, TypeDef declaringType) {
			if (nestedType == null || declaringType == null)
				return;
			uint nestedRid = GetTypeDefRid(nestedType);
			uint dtRid = GetTypeDefRid(declaringType);
			if (nestedRid == 0 || dtRid == 0)
				return;
			var row = new RawNestedClassRow(nestedRid, dtRid);
			nestedClassInfos.Add(nestedRid, declaringType, row);
		}

		/// <summary>
		/// Adds a <c>Module</c> row
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns>Its new rid</returns>
		protected uint AddModule(ModuleDef module) {
			if (module == null) {
				Error("Module is null");
				return 0;
			}
			if (this.module != module)
				Error("Module {0} must be referenced with a ModuleRef, not a ModuleDef", module);
			var row = new RawModuleRow(module.Generation,
								stringsHeap.Add(module.Name),
								guidHeap.Add(module.Mvid),
								guidHeap.Add(module.EncId),
								guidHeap.Add(module.EncBaseId));
			uint rid = tablesHeap.ModuleTable.Create(row);
			moduleDefInfos.Add(module, rid);
			AddCustomAttributes(Table.Module, rid, module);
			return rid;
		}

		/// <summary>
		/// Adds a <c>ModuleRef</c> row
		/// </summary>
		/// <param name="modRef">Module ref</param>
		/// <returns>Its new rid</returns>
		protected uint AddModuleRef(ModuleRef modRef) {
			if (modRef == null) {
				Error("ModuleRef is null");
				return 0;
			}
			uint rid;
			if (moduleRefInfos.TryGetRid(modRef, out rid))
				return rid;
			var row = new RawModuleRefRow(stringsHeap.Add(modRef.Name));
			rid = tablesHeap.ModuleRefTable.Add(row);
			moduleRefInfos.Add(modRef, rid);
			AddCustomAttributes(Table.ModuleRef, rid, modRef);
			return rid;
		}

		/// <summary>
		/// Adds an <c>AssemblyRef</c> row
		/// </summary>
		/// <param name="asmRef">Assembly ref</param>
		/// <returns>Its new rid</returns>
		protected uint AddAssemblyRef(AssemblyRef asmRef) {
			if (asmRef == null) {
				Error("AssemblyRef is null");
				return 0;
			}
			uint rid;
			if (assemblyRefInfos.TryGetRid(asmRef, out rid))
				return rid;
			var version = Utils.CreateVersionWithNoUndefinedValues(asmRef.Version);
			var row = new RawAssemblyRefRow((ushort)version.Major,
							(ushort)version.Minor,
							(ushort)version.Build,
							(ushort)version.Revision,
							(uint)asmRef.Flags,
							blobHeap.Add(GetPublicKeyOrTokenData(asmRef.PublicKeyOrToken)),
							stringsHeap.Add(asmRef.Name),
							stringsHeap.Add(asmRef.Locale),
							blobHeap.Add(asmRef.HashValue));
			rid = tablesHeap.AssemblyRefTable.Add(row);
			assemblyRefInfos.Add(asmRef, rid);
			AddCustomAttributes(Table.AssemblyRef, rid, asmRef);
			return rid;
		}

		/// <summary>
		/// Adds an <c>Assembly</c> row
		/// </summary>
		/// <param name="asm">Assembly</param>
		/// <returns>Its new rid</returns>
		protected uint AddAssembly(AssemblyDef asm) {
			if (asm == null) {
				Error("Assembly is null");
				return 0;
			}
			var version = Utils.CreateVersionWithNoUndefinedValues(asm.Version);
			var row = new RawAssemblyRow((uint)asm.HashAlgId,
							(ushort)version.Major,
							(ushort)version.Minor,
							(ushort)version.Build,
							(ushort)version.Revision,
							(uint)asm.Flags,
							blobHeap.Add(GetPublicKeyOrTokenData(asm.PublicKeyOrToken)),
							stringsHeap.Add(asm.Name),
							stringsHeap.Add(asm.Locale));
			uint rid = tablesHeap.AssemblyTable.Add(row);
			assemblyInfos.Add(asm, rid);
			AddCustomAttributes(Table.Assembly, rid, asm);
			return rid;
		}

		static byte[] GetPublicKeyOrTokenData(PublicKeyBase pkb) {
			if (pkb == null)
				return null;
			return pkb.Data;
		}

		/// <summary>
		/// Adds generic paramters
		/// </summary>
		/// <param name="token">New token of owner</param>
		/// <param name="gps">All generic params</param>
		protected void AddGenericParams(MDToken token, IList<GenericParam> gps) {
			if (gps == null)
				return;
			foreach (var gp in gps)
				AddGenericParam(token, gp);
		}

		/// <summary>
		/// Adds a generic param
		/// </summary>
		/// <param name="owner">New token of owner</param>
		/// <param name="gp">Generic paramater</param>
		protected void AddGenericParam(MDToken owner, GenericParam gp) {
			if (gp == null) {
				Error("GenericParam is null");
				return;
			}
			uint encodedOwner;
			if (!CodedToken.TypeOrMethodDef.Encode(owner, out encodedOwner)) {
				Error("Can't encode TypeOrMethodDef token {0:X8}", owner.Raw);
				encodedOwner = 0;
			}
			var row = new RawGenericParamRow(gp.Number,
							(ushort)gp.Flags,
							encodedOwner,
							stringsHeap.Add(gp.Name),
							gp.Kind == null ? 0 : AddTypeDefOrRef(gp.Kind));
			genericParamInfos.Add(owner.Raw, gp, row);
		}

		/// <summary>
		/// Adds generic parameter constraints
		/// </summary>
		/// <param name="gpRid">New rid of owner generic param</param>
		/// <param name="constraints">Its constraints</param>
		protected void AddGenericParamConstraints(uint gpRid, IList<GenericParamConstraint> constraints) {
			if (constraints == null)
				return;
			foreach (var gpc in constraints)
				AddGenericParamConstraint(gpRid, gpc);
		}

		/// <summary>
		/// Adds a generic parameter constraint
		/// </summary>
		/// <param name="gpRid">New rid of owner generic param</param>
		/// <param name="gpc">Generic paramter constraint</param>
		protected void AddGenericParamConstraint(uint gpRid, GenericParamConstraint gpc) {
			if (gpc == null) {
				Error("GenericParamConstraint is null");
				return;
			}
			var row = new RawGenericParamConstraintRow(gpRid, AddTypeDefOrRef(gpc.Constraint));
			tablesHeap.GenericParamConstraintTable.Add(row);
		}

		/// <summary>
		/// Adds a <c>InterfaceImpl</c> row
		/// </summary>
		/// <param name="typeDefRid">New rid of owner</param>
		/// <param name="ifaces">All interfaces</param>
		protected void AddInterfaceImpls(uint typeDefRid, IList<InterfaceImpl> ifaces) {
			foreach (var iface in ifaces) {
				if (iface == null)
					continue;
				var row = new RawInterfaceImplRow(typeDefRid,
							AddTypeDefOrRef(iface.Interface));
				interfaceImplInfos.Add(typeDefRid, iface, row);
			}
		}

		/// <summary>
		/// Adds a <c>FieldLayout</c> row
		/// </summary>
		/// <param name="field">Owner field</param>
		protected void AddFieldLayout(FieldDef field) {
			if (field == null || field.FieldLayout == null)
				return;
			var rid = GetFieldRid(field);
			var row = new RawFieldLayoutRow(field.FieldLayout.Offset, rid);
			fieldLayoutInfos.Add(rid, field, row);
		}

		/// <summary>
		/// Adds a <c>FieldMarshal</c> row
		/// </summary>
		/// <param name="parent">New owner token</param>
		/// <param name="hfm">Owner</param>
		protected void AddFieldMarshal(MDToken parent, IHasFieldMarshal hfm) {
			if (hfm == null || hfm.FieldMarshal == null)
				return;
			var fieldMarshal = hfm.FieldMarshal;
			uint encodedParent;
			if (!CodedToken.HasFieldMarshal.Encode(parent, out encodedParent)) {
				Error("Can't encode HasFieldMarshal token {0:X8}", parent.Raw);
				encodedParent = 0;
			}
			var row = new RawFieldMarshalRow(encodedParent,
						blobHeap.Add(fieldMarshal.NativeType));
			fieldMarshalInfos.Add(encodedParent, hfm, row);
		}

		/// <summary>
		/// Adds a <c>FieldRVA</c> row
		/// </summary>
		/// <param name="field">The field</param>
		protected void AddFieldRVA(FieldDef field) {
			if (field == null || field.FieldRVA == null)
				return;
			uint rid = GetFieldRid(field);
			var fieldRVA = field.FieldRVA;
			var row = new RawFieldRVARow((uint)fieldRVA.RVA, rid);
			fieldRVAInfos.Add(rid, field, row);
		}

		/// <summary>
		/// Adds a <c>ImplMap</c> row
		/// </summary>
		/// <param name="parent">New owner token</param>
		/// <param name="mf">Owner</param>
		protected void AddImplMap(MDToken parent, IMemberForwarded mf) {
			if (mf == null || mf.ImplMap == null)
				return;
			var implMap = mf.ImplMap;
			uint encodedParent;
			if (!CodedToken.MemberForwarded.Encode(parent, out encodedParent)) {
				Error("Can't encode MemberForwarded token {0:X8}", parent.Raw);
				encodedParent = 0;
			}
			var row = new RawImplMapRow((ushort)implMap.Flags,
						encodedParent,
						stringsHeap.Add(implMap.Name),
						AddModuleRef(implMap.Scope));
			implMapInfos.Add(encodedParent, mf, row);
		}

		/// <summary>
		/// Adds a <c>Constant</c> row
		/// </summary>
		/// <param name="parent">New owner token</param>
		/// <param name="hc">Owner</param>
		protected void AddConstant(MDToken parent, IHasConstant hc) {
			if (hc == null || hc.Constant == null)
				return;
			var constant = hc.Constant;
			uint encodedParent;
			if (!CodedToken.HasConstant.Encode(parent, out encodedParent)) {
				Error("Can't encode HasConstant token {0:X8}", parent.Raw);
				encodedParent = 0;
			}
			var row = new RawConstantRow((byte)constant.Type, 0,
						encodedParent,
						blobHeap.Add(GetConstantValueAsByteArray(constant.Type, constant.Value)));
			hasConstantInfos.Add(encodedParent, hc, row);
		}

		static readonly byte[] constantClassByteArray = new byte[4];
		static readonly byte[] constantDefaultByteArray = new byte[8];
		byte[] GetConstantValueAsByteArray(ElementType etype, object o) {
			if (o == null) {
				if (etype == ElementType.Class)
					return constantClassByteArray;
				Error("Constant is null");
				return constantDefaultByteArray;
			}

			var typeCode = Type.GetTypeCode(o.GetType());
			switch (typeCode) {
			case TypeCode.Boolean:
				VerifyConstantType(etype, ElementType.Boolean);
				return BitConverter.GetBytes((bool)o);

			case TypeCode.Char:
				VerifyConstantType(etype, ElementType.Char);
				return BitConverter.GetBytes((char)o);

			case TypeCode.SByte:
				VerifyConstantType(etype, ElementType.I1);
				return BitConverter.GetBytes((sbyte)o);

			case TypeCode.Byte:
				VerifyConstantType(etype, ElementType.U1);
				return BitConverter.GetBytes((byte)o);

			case TypeCode.Int16:
				VerifyConstantType(etype, ElementType.I2);
				return BitConverter.GetBytes((short)o);

			case TypeCode.UInt16:
				VerifyConstantType(etype, ElementType.U2);
				return BitConverter.GetBytes((ushort)o);

			case TypeCode.Int32:
				VerifyConstantType(etype, ElementType.I4);
				return BitConverter.GetBytes((int)o);

			case TypeCode.UInt32:
				VerifyConstantType(etype, ElementType.U4);
				return BitConverter.GetBytes((uint)o);

			case TypeCode.Int64:
				VerifyConstantType(etype, ElementType.I8);
				return BitConverter.GetBytes((long)o);

			case TypeCode.UInt64:
				VerifyConstantType(etype, ElementType.U8);
				return BitConverter.GetBytes((ulong)o);

			case TypeCode.Single:
				VerifyConstantType(etype, ElementType.R4);
				return BitConverter.GetBytes((float)o);

			case TypeCode.Double:
				VerifyConstantType(etype, ElementType.R8);
				return BitConverter.GetBytes((double)o);

			case TypeCode.String:
				VerifyConstantType(etype, ElementType.String);
				return Encoding.Unicode.GetBytes((string)o);

			default:
				Error("Invalid constant type: {0}", typeCode);
				return constantDefaultByteArray;
			}
		}

		void VerifyConstantType(ElementType realType, ElementType expectedType) {
			if (realType != expectedType)
				Error("Constant value's type is the wrong type");
		}

		/// <summary>
		/// Adds a <c>DeclSecurity</c> row
		/// </summary>
		/// <param name="parent">New owner token</param>
		/// <param name="declSecurities">All <c>DeclSecurity</c> rows</param>
		protected void AddDeclSecurities(MDToken parent, IList<DeclSecurity> declSecurities) {
			if (declSecurities == null)
				return;
			uint encodedParent;
			if (!CodedToken.HasDeclSecurity.Encode(parent, out encodedParent)) {
				Error("Can't encode HasDeclSecurity token {0:X8}", parent.Raw);
				encodedParent = 0;
			}
			foreach (var decl in declSecurities) {
				if (decl == null)
					continue;
				var row = new RawDeclSecurityRow((short)decl.Action,
							encodedParent,
							blobHeap.Add(decl.PermissionSet));
				declSecurityInfos.Add(encodedParent, decl, row);
			}
		}

		/// <summary>
		/// Adds <c>MethodSemantics</c> rows
		/// </summary>
		/// <param name="evt">Event</param>
		protected void AddMethodSemantics(EventDef evt) {
			if (evt == null) {
				Error("Event is null");
				return;
			}
			uint rid = GetEventRid(evt);
			if (rid == 0)
				return;
			var token = new MDToken(Table.Event, rid);
			AddMethodSemantics(token, evt.AddMethod, MethodSemanticsAttributes.AddOn);
			AddMethodSemantics(token, evt.RemoveMethod, MethodSemanticsAttributes.RemoveOn);
			AddMethodSemantics(token, evt.InvokeMethod, MethodSemanticsAttributes.Fire);
			AddMethodSemantics(token, evt.OtherMethods);
		}

		/// <summary>
		/// Adds <c>MethodSemantics</c> rows
		/// </summary>
		/// <param name="prop">Property</param>
		protected void AddMethodSemantics(PropertyDef prop) {
			if (prop == null) {
				Error("Property is null");
				return;
			}
			uint rid = GetPropertyRid(prop);
			if (rid == 0)
				return;
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
			uint methodRid = GetMethodRid(method);
			if (methodRid == 0)
				return;
			uint encodedOwner;
			if (!CodedToken.HasSemantic.Encode(owner, out encodedOwner)) {
				Error("Can't encode HasSemantic token {0:X8}", owner.Raw);
				encodedOwner = 0;
			}
			var row = new RawMethodSemanticsRow((ushort)flags, methodRid, encodedOwner);
			methodSemanticsInfos.Add(encodedOwner, method, row);
		}

		void AddMethodImpls(MethodDef method, IList<MethodOverride> overrides) {
			if (overrides == null)
				return;
			uint rid = GetMethodRid(method);
			foreach (var ovr in overrides) {
				var row = new RawMethodImplRow(rid,
							AddMethodDefOrRef(ovr.MethodBody),
							AddMethodDefOrRef(ovr.MethodDeclaration));
				methodImplInfos.Add(rid, method, row);
			}
		}

		/// <summary>
		/// Adds a <c>ClassLayout</c> row
		/// </summary>
		/// <param name="type">Type</param>
		protected void AddClassLayout(TypeDef type) {
			if (type == null || type.ClassLayout == null)
				return;
			var rid = GetTypeDefRid(type);
			var classLayout = type.ClassLayout;
			var row = new RawClassLayoutRow(classLayout.PackingSize, classLayout.ClassSize, rid);
			classLayoutInfos.Add(rid, type, row);
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

			if (resource == null)
				Error("Resource is null");
			else
				Error("Invalid resource type: {0}", resource.GetType());
		}

		uint AddEmbeddedResource(EmbeddedResource er) {
			if (er == null) {
				Error("EmbeddedResource is null");
				return 0;
			}
			var row = new RawManifestResourceRow(netResources.NextOffset,
						(uint)er.Flags,
						stringsHeap.Add(er.Name),
						0);
			uint rid = tablesHeap.ManifestResourceTable.Create(row);
			manifestResourceInfos.Add(er, rid);
			netResources.Add(er.Data);
			//TODO: Add custom attributes
			return rid;
		}

		uint AddAssemblyLinkedResource(AssemblyLinkedResource alr) {
			if (alr == null) {
				Error("AssemblyLinkedResource is null");
				return 0;
			}
			var row = new RawManifestResourceRow(0,
						(uint)alr.Flags,
						stringsHeap.Add(alr.Name),
						AddAssemblyRef(alr.Assembly));
			uint rid = tablesHeap.ManifestResourceTable.Create(row);
			manifestResourceInfos.Add(alr, rid);
			//TODO: Add custom attributes
			return rid;
		}

		uint AddLinkedResource(LinkedResource lr) {
			if (lr == null) {
				Error("LinkedResource is null");
				return 0;
			}
			var row = new RawManifestResourceRow(0,
						(uint)lr.Flags,
						stringsHeap.Add(lr.Name),
						AddFile(lr.File));
			uint rid = tablesHeap.ManifestResourceTable.Create(row);
			manifestResourceInfos.Add(lr, rid);
			//TODO: Add custom attributes
			return rid;
		}

		/// <summary>
		/// Adds a <c>File</c> row
		/// </summary>
		/// <param name="file">File</param>
		/// <returns>Its new rid</returns>
		protected uint AddFile(FileDef file) {
			if (file == null) {
				Error("FileDef is null");
				return 0;
			}
			uint rid;
			if (fileDefInfos.TryGetRid(file, out rid))
				return rid;
			var row = new RawFileRow((uint)file.Flags,
						stringsHeap.Add(file.Name),
						blobHeap.Add(file.HashValue));	//TODO: Re-calculate the hash value if possible
			fileDefInfos.Add(file, rid);
			AddCustomAttributes(Table.File, rid, file);
			return rid;
		}

		/// <summary>
		/// Adds a <c>ExportedType</c> row
		/// </summary>
		/// <param name="et">Exported type</param>
		/// <returns>Its new rid</returns>
		protected uint AddExportedType(ExportedType et) {
			if (et == null) {
				Error("ExportedType is null");
				return 0;
			}
			uint rid;
			if (exportedTypeInfos.TryGetRid(et, out rid))
				return rid;
			exportedTypeInfos.Add(et, 0);	// Prevent inf recursion
			var row = new RawExportedTypeRow((uint)et.Flags,
						et.TypeDefId,
						stringsHeap.Add(et.TypeName),
						stringsHeap.Add(et.TypeNamespace),
						AddImplementation(et));
			rid = tablesHeap.ExportedTypeTable.Add(row);
			exportedTypeInfos.SetRid(et, rid);
			AddCustomAttributes(Table.ExportedType, rid, et);
			return rid;
		}

		/// <summary>
		/// Gets a #Blob offset of a type signature
		/// </summary>
		/// <param name="ts">Type sig</param>
		/// <returns>#Blob offset</returns>
		protected uint GetSignature(TypeSig ts) {
			if (ts == null) {
				Error("TypeSig is null");
				return 0;
			}

			var blob = SignatureWriter.Write(this, ts);
			return blobHeap.Add(blob);
		}

		/// <summary>
		/// Gets a #Blob offset of a calling convention signature
		/// </summary>
		/// <param name="sig">Signature</param>
		/// <returns>#Blob offset</returns>
		protected uint GetSignature(CallingConventionSig sig) {
			if (sig == null) {
				Error("CallingConventionSig is null");
				return 0;
			}

			var blob = SignatureWriter.Write(this, sig);
			return blobHeap.Add(blob);
		}

		/// <summary>
		/// Adds a <c>CustomAttribute</c> row
		/// </summary>
		/// <param name="table">Owner table</param>
		/// <param name="rid">New owner rid</param>
		/// <param name="hca">Onwer</param>
		protected void AddCustomAttributes(Table table, uint rid, IHasCustomAttribute hca) {
			AddCustomAttributes(table, rid, hca.CustomAttributes);
		}

		void AddCustomAttributes(Table table, uint rid, CustomAttributeCollection caList) {
			var token = new MDToken(table, rid);
			foreach (var ca in caList)
				AddCustomAttribute(token, ca);
		}

		void AddCustomAttribute(MDToken token, CustomAttribute ca) {
			uint encodedToken;
			if (!CodedToken.HasCustomAttribute.Encode(token, out encodedToken)) {
				Error("Can't encode HasCustomAttribute token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			var caBlob = CustomAttributeWriter.Write(this, ca);
			var row = new RawCustomAttributeRow(encodedToken,
						AddCustomAttributeType(ca.Ctor),
						blobHeap.Add(caBlob));
			customAttributeInfos.Add(encodedToken, ca, row);
		}

		/// <inheritdoc/>
		uint ISignatureWriterHelper.ToEncodedToken(ITypeDefOrRef typeDefOrRef) {
			return AddTypeDefOrRef(typeDefOrRef);
		}

		/// <inheritdoc/>
		void ISignatureWriterHelper.Error(string message) {
			Error(message);
		}

		/// <inheritdoc/>
		void ITokenCreator.Error(string message) {
			Error(message);
		}

		/// <inheritdoc/>
		void ICustomAttributeWriterHelper.Error(string message) {
			Error(message);
		}

		/// <inheritdoc/>
		bool IFullNameCreatorHelper.MustUseAssemblyName(IType type) {
			//TODO: If type is in this asm, return false. If there's no type with
			//		this name in this assembly and it exists in mscorlib, return false.
			//		else return true. When comparing assemblies, compare the full
			//		asm name when comparing this assembly, but only part of it
			//		when checking whether it's in mscorlib.
			return true;
		}

		/// <summary>
		/// Gets all <see cref="TypeDef"/>s that should be saved in the meta data
		/// </summary>
		protected abstract IEnumerable<TypeDef> GetAllTypeDefs();

		/// <summary>
		/// Initializes <c>TypeDef</c> rids and creates raw rows, but does not initialize
		/// any columns.
		/// </summary>
		protected abstract void AllocateTypeDefRids();

		/// <summary>
		/// Allocates <c>Field</c>, <c>Method</c>, <c>Property</c>, <c>Event</c>, <c>Param</c>:
		/// rid and raw row, but doesn't initialize the raw row.
		/// Initializes <c>TypeDef</c> columns: <c>FieldList</c>, <c>MethodList</c>.
		/// Initialized <c>Method</c> column: <c>ParamList</c>.
		/// Initializes <see cref="MetaData.eventMapInfos"/> and <see cref="MetaData.propertyMapInfos"/>.
		/// </summary>
		protected abstract void AllocateMemberDefRids();

		/// <summary>
		/// Gets the new <see cref="TypeDef"/> rid
		/// </summary>
		/// <param name="td">Type</param>
		/// <returns>Its new rid</returns>
		protected abstract uint GetTypeDefRid(TypeDef td);

		/// <summary>
		/// Gets the new <see cref="FieldDef"/> rid
		/// </summary>
		/// <param name="fd">Type</param>
		/// <returns>Its new rid</returns>
		protected abstract uint GetFieldRid(FieldDef fd);

		/// <summary>
		/// Gets the new <see cref="MethodDef"/> rid
		/// </summary>
		/// <param name="md">Method</param>
		/// <returns>Its new rid</returns>
		protected abstract uint GetMethodRid(MethodDef md);

		/// <summary>
		/// Gets the new <see cref="ParamDef"/> rid
		/// </summary>
		/// <param name="pd">Parameter</param>
		/// <returns>Its new rid</returns>
		protected abstract uint GetParamRid(ParamDef pd);

		/// <summary>
		/// Gets the new <see cref="EventDef"/> rid
		/// </summary>
		/// <param name="ed">Event</param>
		/// <returns>Its new rid</returns>
		protected abstract uint GetEventRid(EventDef ed);

		/// <summary>
		/// Gets the new <see cref="PropertyDef"/> rid
		/// </summary>
		/// <param name="pd">Property</param>
		/// <returns>Its new rid</returns>
		protected abstract uint GetPropertyRid(PropertyDef pd);

		/// <summary>
		/// Adds a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="tr">Type reference</param>
		/// <returns>Its new rid</returns>
		protected abstract uint AddTypeRef(TypeRef tr);

		/// <summary>
		/// Adds a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="ts">Type spec</param>
		/// <returns>Its new rid</returns>
		protected abstract uint AddTypeSpec(TypeSpec ts);

		/// <summary>
		/// Adds a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="mr">Member ref</param>
		/// <returns>Its new rid</returns>
		protected abstract uint AddMemberRef(MemberRef mr);

		/// <summary>
		/// Adds a <see cref="StandAloneSig"/>
		/// </summary>
		/// <param name="sas">Stand alone sig</param>
		/// <returns>Its new rid</returns>
		protected abstract uint AddStandAloneSig(StandAloneSig sas);

		/// <summary>
		/// Adds a <see cref="MethodSpec"/>
		/// </summary>
		/// <param name="ms">Method spec</param>
		/// <returns>Its new rid</returns>
		protected abstract uint AddMethodSpec(MethodSpec ms);

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
