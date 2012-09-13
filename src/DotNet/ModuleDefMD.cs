using System;
using System.Collections.Generic;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// Created from a row in the Module table
	/// </summary>
	public sealed class ModuleDefMD : ModuleDefMD2, ICorLibTypes {
		/// <summary>The file that contains all .NET metadata</summary>
		DotNetFile dnFile;

		SimpleLazyList<ModuleDefMD2> listModuleDefMD;
		SimpleLazyList<TypeRefMD> listTypeRefMD;
		SimpleLazyList<TypeDefMD> listTypeDefMD;
		SimpleLazyList<FieldPtrMD> listFieldPtrMD;
		SimpleLazyList<FieldDefMD> listFieldDefMD;
		SimpleLazyList<MethodPtrMD> listMethodPtrMD;
		SimpleLazyList<MethodDefMD> listMethodDefMD;
		SimpleLazyList<ParamPtrMD> listParamPtrMD;
		SimpleLazyList<ParamDefMD> listParamDefMD;
		SimpleLazyList<InterfaceImplMD> listInterfaceImplMD;
		SimpleLazyList<MemberRefMD> listMemberRefMD;
		SimpleLazyList<ConstantMD> listConstantMD;
		SimpleLazyList<CustomAttributeMD> listCustomAttributeMD;
		SimpleLazyList<FieldMarshalMD> listFieldMarshalMD;
		SimpleLazyList<DeclSecurityMD> listDeclSecurityMD;
		SimpleLazyList<ClassLayoutMD> listClassLayoutMD;
		SimpleLazyList<FieldLayoutMD> listFieldLayoutMD;
		SimpleLazyList<StandAloneSigMD> listStandAloneSigMD;
		SimpleLazyList<EventMapMD> listEventMapMD;
		SimpleLazyList<EventPtrMD> listEventPtrMD;
		SimpleLazyList<EventDefMD> listEventDefMD;
		SimpleLazyList<PropertyMapMD> listPropertyMapMD;
		SimpleLazyList<PropertyPtrMD> listPropertyPtrMD;
		SimpleLazyList<PropertyDefMD> listPropertyDefMD;
		SimpleLazyList<MethodSemanticsMD> listMethodSemanticsMD;
		SimpleLazyList<MethodImplMD> listMethodImplMD;
		SimpleLazyList<ModuleRefMD> listModuleRefMD;
		SimpleLazyList<TypeSpecMD> listTypeSpecMD;
		SimpleLazyList<ImplMapMD> listImplMapMD;
		SimpleLazyList<FieldRVAMD> listFieldRVAMD;
		SimpleLazyList<ENCLogMD> listENCLogMD;
		SimpleLazyList<ENCMapMD> listENCMapMD;
		SimpleLazyList<AssemblyDefMD> listAssemblyDefMD;
		SimpleLazyList<AssemblyProcessorMD> listAssemblyProcessorMD;
		SimpleLazyList<AssemblyOSMD> listAssemblyOSMD;
		SimpleLazyList<AssemblyRefMD> listAssemblyRefMD;
		SimpleLazyList<AssemblyRefProcessorMD> listAssemblyRefProcessorMD;
		SimpleLazyList<AssemblyRefOSMD> listAssemblyRefOSMD;
		SimpleLazyList<FileDefMD> listFileDefMD;
		SimpleLazyList<ExportedTypeMD> listExportedTypeMD;
		SimpleLazyList<ManifestResourceMD> listManifestResourceMD;
		SimpleLazyList<NestedClassMD> listNestedClassMD;
		SimpleLazyList<GenericParamMD> listGenericParamMD;
		SimpleLazyList<MethodSpecMD> listMethodSpecMD;
		SimpleLazyList<GenericParamConstraintMD> listGenericParamConstraintMD;

		LazyList<TypeDef> types;

		CorLibTypeSig typeVoid;
		CorLibTypeSig typeBoolean;
		CorLibTypeSig typeChar;
		CorLibTypeSig typeSByte;
		CorLibTypeSig typeByte;
		CorLibTypeSig typeInt16;
		CorLibTypeSig typeUInt16;
		CorLibTypeSig typeInt32;
		CorLibTypeSig typeUInt32;
		CorLibTypeSig typeInt64;
		CorLibTypeSig typeUInt64;
		CorLibTypeSig typeSingle;
		CorLibTypeSig typeDouble;
		CorLibTypeSig typeString;
		CorLibTypeSig typeTypedReference;
		CorLibTypeSig typeIntPtr;
		CorLibTypeSig typeUIntPtr;
		CorLibTypeSig typeObject;
		AssemblyRef corLibAssemblyRef;

		/// <summary>
		/// Returns the .NET file
		/// </summary>
		public DotNetFile DotNetFile {
			get { return dnFile; }
		}

		/// <summary>
		/// Returns the .NET metadata interface
		/// </summary>
		public IMetaData MetaData {
			get { return dnFile.MetaData; }
		}

		/// <summary>
		/// Returns the #~ or #- tables stream
		/// </summary>
		public TablesStream TablesStream {
			get { return dnFile.MetaData.TablesStream; }
		}

		/// <summary>
		/// Returns the #Strings stream
		/// </summary>
		public StringsStream StringsStream {
			get { return dnFile.MetaData.StringsStream; }
		}

		/// <summary>
		/// Returns the #Blob stream
		/// </summary>
		public BlobStream BlobStream {
			get { return dnFile.MetaData.BlobStream; }
		}

		/// <summary>
		/// Returns the #GUID stream
		/// </summary>
		public GuidStream GuidStream {
			get { return dnFile.MetaData.GuidStream; }
		}

		/// <summary>
		/// Returns the #US stream
		/// </summary>
		public USStream USStream {
			get { return dnFile.MetaData.USStream; }
		}

		/// <inheritdoc/>
		public override IList<TypeDef> Types {
			get { return types; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.Void {
			get { return typeVoid; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.Boolean {
			get { return typeBoolean; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.Char {
			get { return typeChar; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.SByte {
			get { return typeSByte; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.Byte {
			get { return typeByte; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.Int16 {
			get { return typeInt16; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.UInt16 {
			get { return typeUInt16; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.Int32 {
			get { return typeInt32; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.UInt32 {
			get { return typeUInt32; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.Int64 {
			get { return typeInt64; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.UInt64 {
			get { return typeUInt64; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.Single {
			get { return typeSingle; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.Double {
			get { return typeDouble; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.String {
			get { return typeString; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.TypedReference {
			get { return typeTypedReference; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.IntPtr {
			get { return typeIntPtr; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.UIntPtr {
			get { return typeUIntPtr; }
		}

		/// <inheritdoc/>
		CorLibTypeSig ICorLibTypes.Object {
			get { return typeObject; }
		}

		/// <inheritdoc/>
		AssemblyRef ICorLibTypes.AssemblyRef {
			get { return corLibAssemblyRef; }
		}

		/// <summary>
		/// Gets the <see cref="ICorLibTypes"/>
		/// </summary>
		public ICorLibTypes CorLibTypes {
			get { return this; }
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a file
		/// </summary>
		/// <param name="fileName">File name of an existing .NET module/assembly</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public new static ModuleDefMD Load(string fileName) {
			DotNetFile dnFile = null;
			try {
				return Load(dnFile = DotNetFile.Load(fileName));
			}
			catch {
				if (dnFile != null)
					dnFile.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a byte[]
		/// </summary>
		/// <param name="data">Contents of a .NET module/assembly</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public new static ModuleDefMD Load(byte[] data) {
			DotNetFile dnFile = null;
			try {
				return Load(dnFile = DotNetFile.Load(data));
			}
			catch {
				if (dnFile != null)
					dnFile.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a memory location
		/// </summary>
		/// <param name="addr">Address of a .NET module/assembly</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public new static ModuleDefMD Load(IntPtr addr) {
			DotNetFile dnFile = null;
			try {
				return Load(dnFile = DotNetFile.Load(addr));
			}
			catch {
				if (dnFile != null)
					dnFile.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a <see cref="DotNetFile"/>
		/// </summary>
		/// <param name="dnFile">The loaded .NET file</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance that now owns <paramref name="dnFile"/></returns>
		public new static ModuleDefMD Load(DotNetFile dnFile) {
			return new ModuleDefMD(dnFile);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dnFile">The loaded .NET file</param>
		/// <exception cref="ArgumentNullException">If <paramref name="dnFile"/> is null</exception>
		ModuleDefMD(DotNetFile dnFile)
			: base(null, 1) {
#if DEBUG
			if (dnFile == null)
				throw new ArgumentNullException("dnFile");
#endif
			this.dnFile = dnFile;
			Initialize();
			InitializeCorLibTypes();
		}

		void Initialize() {
			var ts = dnFile.MetaData.TablesStream;
			listModuleDefMD = new SimpleLazyList<ModuleDefMD2>(ts.Get(Table.Module).Rows, rid2 => rid2 == rid ? this : new ModuleDefMD2(this, rid2));
			listTypeRefMD = new SimpleLazyList<TypeRefMD>(ts.Get(Table.TypeRef).Rows, rid2 => new TypeRefMD(this, rid2));
			listTypeDefMD = new SimpleLazyList<TypeDefMD>(ts.Get(Table.TypeDef).Rows, rid2 => new TypeDefMD(this, rid2));
			listFieldPtrMD = new SimpleLazyList<FieldPtrMD>(ts.Get(Table.FieldPtr).Rows, rid2 => new FieldPtrMD(this, rid2));
			listFieldDefMD = new SimpleLazyList<FieldDefMD>(ts.Get(Table.Field).Rows, rid2 => new FieldDefMD(this, rid2));
			listMethodPtrMD = new SimpleLazyList<MethodPtrMD>(ts.Get(Table.MethodPtr).Rows, rid2 => new MethodPtrMD(this, rid2));
			listMethodDefMD = new SimpleLazyList<MethodDefMD>(ts.Get(Table.Method).Rows, rid2 => new MethodDefMD(this, rid2));
			listParamPtrMD = new SimpleLazyList<ParamPtrMD>(ts.Get(Table.ParamPtr).Rows, rid2 => new ParamPtrMD(this, rid2));
			listParamDefMD = new SimpleLazyList<ParamDefMD>(ts.Get(Table.Param).Rows, rid2 => new ParamDefMD(this, rid2));
			listInterfaceImplMD = new SimpleLazyList<InterfaceImplMD>(ts.Get(Table.InterfaceImpl).Rows, rid2 => new InterfaceImplMD(this, rid2));
			listMemberRefMD = new SimpleLazyList<MemberRefMD>(ts.Get(Table.MemberRef).Rows, rid2 => new MemberRefMD(this, rid2));
			listConstantMD = new SimpleLazyList<ConstantMD>(ts.Get(Table.Constant).Rows, rid2 => new ConstantMD(this, rid2));
			listCustomAttributeMD = new SimpleLazyList<CustomAttributeMD>(ts.Get(Table.CustomAttribute).Rows, rid2 => new CustomAttributeMD(this, rid2));
			listFieldMarshalMD = new SimpleLazyList<FieldMarshalMD>(ts.Get(Table.FieldMarshal).Rows, rid2 => new FieldMarshalMD(this, rid2));
			listDeclSecurityMD = new SimpleLazyList<DeclSecurityMD>(ts.Get(Table.DeclSecurity).Rows, rid2 => new DeclSecurityMD(this, rid2));
			listClassLayoutMD = new SimpleLazyList<ClassLayoutMD>(ts.Get(Table.ClassLayout).Rows, rid2 => new ClassLayoutMD(this, rid2));
			listFieldLayoutMD = new SimpleLazyList<FieldLayoutMD>(ts.Get(Table.FieldLayout).Rows, rid2 => new FieldLayoutMD(this, rid2));
			listStandAloneSigMD = new SimpleLazyList<StandAloneSigMD>(ts.Get(Table.StandAloneSig).Rows, rid2 => new StandAloneSigMD(this, rid2));
			listEventMapMD = new SimpleLazyList<EventMapMD>(ts.Get(Table.EventMap).Rows, rid2 => new EventMapMD(this, rid2));
			listEventPtrMD = new SimpleLazyList<EventPtrMD>(ts.Get(Table.EventPtr).Rows, rid2 => new EventPtrMD(this, rid2));
			listEventDefMD = new SimpleLazyList<EventDefMD>(ts.Get(Table.Event).Rows, rid2 => new EventDefMD(this, rid2));
			listPropertyMapMD = new SimpleLazyList<PropertyMapMD>(ts.Get(Table.PropertyMap).Rows, rid2 => new PropertyMapMD(this, rid2));
			listPropertyPtrMD = new SimpleLazyList<PropertyPtrMD>(ts.Get(Table.PropertyPtr).Rows, rid2 => new PropertyPtrMD(this, rid2));
			listPropertyDefMD = new SimpleLazyList<PropertyDefMD>(ts.Get(Table.Property).Rows, rid2 => new PropertyDefMD(this, rid2));
			listMethodSemanticsMD = new SimpleLazyList<MethodSemanticsMD>(ts.Get(Table.MethodSemantics).Rows, rid2 => new MethodSemanticsMD(this, rid2));
			listMethodImplMD = new SimpleLazyList<MethodImplMD>(ts.Get(Table.MethodImpl).Rows, rid2 => new MethodImplMD(this, rid2));
			listModuleRefMD = new SimpleLazyList<ModuleRefMD>(ts.Get(Table.ModuleRef).Rows, rid2 => new ModuleRefMD(this, rid2));
			listTypeSpecMD = new SimpleLazyList<TypeSpecMD>(ts.Get(Table.TypeSpec).Rows, rid2 => new TypeSpecMD(this, rid2));
			listImplMapMD = new SimpleLazyList<ImplMapMD>(ts.Get(Table.ImplMap).Rows, rid2 => new ImplMapMD(this, rid2));
			listFieldRVAMD = new SimpleLazyList<FieldRVAMD>(ts.Get(Table.FieldRVA).Rows, rid2 => new FieldRVAMD(this, rid2));
			listENCLogMD = new SimpleLazyList<ENCLogMD>(ts.Get(Table.ENCLog).Rows, rid2 => new ENCLogMD(this, rid2));
			listENCMapMD = new SimpleLazyList<ENCMapMD>(ts.Get(Table.ENCMap).Rows, rid2 => new ENCMapMD(this, rid2));
			listAssemblyDefMD = new SimpleLazyList<AssemblyDefMD>(ts.Get(Table.Assembly).Rows, rid2 => {
				var asm = new AssemblyDefMD(this, rid2);
				if (rid2 == 1)
					asm.ManifestModule = this;
				return asm;
			});
			listAssemblyProcessorMD = new SimpleLazyList<AssemblyProcessorMD>(ts.Get(Table.AssemblyProcessor).Rows, rid2 => new AssemblyProcessorMD(this, rid2));
			listAssemblyOSMD = new SimpleLazyList<AssemblyOSMD>(ts.Get(Table.AssemblyOS).Rows, rid2 => new AssemblyOSMD(this, rid2));
			listAssemblyRefMD = new SimpleLazyList<AssemblyRefMD>(ts.Get(Table.AssemblyRef).Rows, rid2 => new AssemblyRefMD(this, rid2));
			listAssemblyRefProcessorMD = new SimpleLazyList<AssemblyRefProcessorMD>(ts.Get(Table.AssemblyRefProcessor).Rows, rid2 => new AssemblyRefProcessorMD(this, rid2));
			listAssemblyRefOSMD = new SimpleLazyList<AssemblyRefOSMD>(ts.Get(Table.AssemblyRefOS).Rows, rid2 => new AssemblyRefOSMD(this, rid2));
			listFileDefMD = new SimpleLazyList<FileDefMD>(ts.Get(Table.File).Rows, rid2 => new FileDefMD(this, rid2));
			listExportedTypeMD = new SimpleLazyList<ExportedTypeMD>(ts.Get(Table.ExportedType).Rows, rid2 => new ExportedTypeMD(this, rid2));
			listManifestResourceMD = new SimpleLazyList<ManifestResourceMD>(ts.Get(Table.ManifestResource).Rows, rid2 => new ManifestResourceMD(this, rid2));
			listNestedClassMD = new SimpleLazyList<NestedClassMD>(ts.Get(Table.NestedClass).Rows, rid2 => new NestedClassMD(this, rid2));
			if (ts.Get(Table.GenericParam) != null) {
				listGenericParamMD = new SimpleLazyList<GenericParamMD>(ts.Get(Table.GenericParam).Rows, rid2 => new GenericParamMD(this, rid2));
				listMethodSpecMD = new SimpleLazyList<MethodSpecMD>(ts.Get(Table.MethodSpec).Rows, rid2 => new MethodSpecMD(this, rid2));
				listGenericParamConstraintMD = new SimpleLazyList<GenericParamConstraintMD>(ts.Get(Table.GenericParamConstraint).Rows, rid2 => new GenericParamConstraintMD(this, rid2));
			}

			var list = MetaData.GetTypeDefRidList();
			types = new LazyList<TypeDef>((int)list.Length, list, (list2, i) => ResolveTypeDef(((RidList)list2)[i]));
		}

		void InitializeCorLibTypes() {
			corLibAssemblyRef = FindCorLibAssemblyRef();
			typeVoid = new CorLibTypeSig(CreateCorLibTypeRef("Void"), ElementType.Void);
			typeBoolean = new CorLibTypeSig(CreateCorLibTypeRef("Boolean"), ElementType.Boolean);
			typeChar = new CorLibTypeSig(CreateCorLibTypeRef("Char"), ElementType.Char);
			typeSByte = new CorLibTypeSig(CreateCorLibTypeRef("SByte"), ElementType.I1);
			typeByte = new CorLibTypeSig(CreateCorLibTypeRef("Byte"), ElementType.U1);
			typeInt16 = new CorLibTypeSig(CreateCorLibTypeRef("Int16"), ElementType.I2);
			typeUInt16 = new CorLibTypeSig(CreateCorLibTypeRef("UInt16"), ElementType.U2);
			typeInt32 = new CorLibTypeSig(CreateCorLibTypeRef("Int32"), ElementType.I4);
			typeUInt32 = new CorLibTypeSig(CreateCorLibTypeRef("UInt32"), ElementType.U4);
			typeInt64 = new CorLibTypeSig(CreateCorLibTypeRef("Int64"), ElementType.I8);
			typeUInt64 = new CorLibTypeSig(CreateCorLibTypeRef("UInt64"), ElementType.U8);
			typeSingle = new CorLibTypeSig(CreateCorLibTypeRef("Single"), ElementType.R4);
			typeDouble = new CorLibTypeSig(CreateCorLibTypeRef("Double"), ElementType.R8);
			typeString = new CorLibTypeSig(CreateCorLibTypeRef("String"), ElementType.String);
			typeTypedReference = new CorLibTypeSig(CreateCorLibTypeRef("TypedReference"), ElementType.TypedByRef);
			typeIntPtr = new CorLibTypeSig(CreateCorLibTypeRef("IntPtr"), ElementType.I);
			typeUIntPtr = new CorLibTypeSig(CreateCorLibTypeRef("UIntPtr"), ElementType.U);
			typeObject = new CorLibTypeSig(CreateCorLibTypeRef("Object"), ElementType.Object);
		}

		TypeRef CreateCorLibTypeRef(string name) {
			return new TypeRefUser(this, "System", name, corLibAssemblyRef);
		}

		/// <summary>
		/// Finds or creates a mscorlib <see cref="AssemblyRef"/>
		/// </summary>
		/// <returns>An existing or new <see cref="AssemblyRef"/></returns>
		AssemblyRef FindCorLibAssemblyRef() {
			var numAsmRefs = TablesStream.Get(Table.AssemblyRef).Rows;
			AssemblyRef corLibAsmRef = null;
			for (uint i = 1; i <= numAsmRefs; i++) {
				var asmRef = ResolveAssemblyRef(i);
				if (UTF8String.IsNullOrEmpty(asmRef.Name))
					continue;
				if (asmRef.Name != "mscorlib")
					continue;
				if (corLibAsmRef == null || corLibAsmRef.Version == null || (asmRef.Version != null && asmRef.Version >= corLibAsmRef.Version))
					corLibAsmRef = asmRef;
			}
			if (corLibAsmRef != null)
				return corLibAsmRef;
			return new AssemblyRefUser("mscorlib", new Version(2, 0, 0, 0), new PublicKeyToken("b77a5c561934e089"));
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				if (dnFile != null)
					dnFile.Dispose();
				dnFile = null;
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="mdToken">The metadata token</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or null if <paramref name="mdToken"/> is invalid</returns>
		public IMDTokenProvider ResolveToken(MDToken mdToken) {
			return ResolveToken(mdToken.Raw);
		}

		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="token">The metadata token</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or null if <paramref name="token"/> is invalid</returns>
		public IMDTokenProvider ResolveToken(int token) {
			return ResolveToken((uint)token);
		}

		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="token">The metadata token</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or null if <paramref name="token"/> is invalid</returns>
		public IMDTokenProvider ResolveToken(uint token) {
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Module: return ResolveModule(rid);
			case Table.TypeRef: return ResolveTypeRef(rid);
			case Table.TypeDef: return ResolveTypeDef(rid);
			case Table.FieldPtr: return ResolveFieldPtr(rid);
			case Table.Field: return ResolveField(rid);
			case Table.MethodPtr: return ResolveMethodPtr(rid);
			case Table.Method: return ResolveMethod(rid);
			case Table.ParamPtr: return ResolveParamPtr(rid);
			case Table.Param: return ResolveParam(rid);
			case Table.InterfaceImpl: return ResolveInterfaceImpl(rid);
			case Table.MemberRef: return ResolveMemberRef(rid);
			case Table.Constant: return ResolveConstant(rid);
			case Table.CustomAttribute: return ResolveCustomAttribute(rid);
			case Table.FieldMarshal: return ResolveFieldMarshal(rid);
			case Table.DeclSecurity: return ResolveDeclSecurity(rid);
			case Table.ClassLayout: return ResolveClassLayout(rid);
			case Table.FieldLayout: return ResolveFieldLayout(rid);
			case Table.StandAloneSig: return ResolveStandAloneSig(rid);
			case Table.EventMap: return ResolveEventMap(rid);
			case Table.EventPtr: return ResolveEventPtr(rid);
			case Table.Event: return ResolveEvent(rid);
			case Table.PropertyMap: return ResolvePropertyMap(rid);
			case Table.PropertyPtr: return ResolvePropertyPtr(rid);
			case Table.Property: return ResolveProperty(rid);
			case Table.MethodSemantics: return ResolveMethodSemantics(rid);
			case Table.MethodImpl: return ResolveMethodImpl(rid);
			case Table.ModuleRef: return ResolveModuleRef(rid);
			case Table.TypeSpec: return ResolveTypeSpec(rid);
			case Table.ImplMap: return ResolveImplMap(rid);
			case Table.FieldRVA: return ResolveFieldRVA(rid);
			case Table.ENCLog: return ResolveENCLog(rid);
			case Table.ENCMap: return ResolveENCMap(rid);
			case Table.Assembly: return ResolveAssembly(rid);
			case Table.AssemblyProcessor: return ResolveAssemblyProcessor(rid);
			case Table.AssemblyOS: return ResolveAssemblyOS(rid);
			case Table.AssemblyRef: return ResolveAssemblyRef(rid);
			case Table.AssemblyRefProcessor: return ResolveAssemblyRefProcessor(rid);
			case Table.AssemblyRefOS: return ResolveAssemblyRefOS(rid);
			case Table.File: return ResolveFile(rid);
			case Table.ExportedType: return ResolveExportedType(rid);
			case Table.ManifestResource: return ResolveManifestResource(rid);
			case Table.NestedClass: return ResolveNestedClass(rid);
			case Table.GenericParam: return ResolveGenericParam(rid);
			case Table.MethodSpec: return ResolveMethodSpec(rid);
			case Table.GenericParamConstraint: return ResolveGenericParamConstraint(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="ModuleDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ModuleDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ModuleDef ResolveModule(uint rid) {
			return listModuleDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="TypeRef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public TypeRef ResolveTypeRef(uint rid) {
			return listTypeRefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="TypeDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public TypeDef ResolveTypeDef(uint rid) {
			return listTypeDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="FieldPtr"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="FieldPtr"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public FieldPtr ResolveFieldPtr(uint rid) {
			return listFieldPtrMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="FieldDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="FieldDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public FieldDef ResolveField(uint rid) {
			return listFieldDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="MethodPtr"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MethodPtr"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public MethodPtr ResolveMethodPtr(uint rid) {
			return listMethodPtrMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="MethodDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MethodDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public MethodDef ResolveMethod(uint rid) {
			return listMethodDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ParamPtr"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ParamPtr"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ParamPtr ResolveParamPtr(uint rid) {
			return listParamPtrMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ParamDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ParamDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ParamDef ResolveParam(uint rid) {
			return listParamDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="InterfaceImpl"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="InterfaceImpl"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public InterfaceImpl ResolveInterfaceImpl(uint rid) {
			return listInterfaceImplMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MemberRef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public MemberRef ResolveMemberRef(uint rid) {
			return listMemberRefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="Constant"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="Constant"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public Constant ResolveConstant(uint rid) {
			return listConstantMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="CustomAttribute"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="CustomAttribute"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public CustomAttribute ResolveCustomAttribute(uint rid) {
			return listCustomAttributeMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="FieldMarshal"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="FieldMarshal"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public FieldMarshal ResolveFieldMarshal(uint rid) {
			return listFieldMarshalMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="DeclSecurity"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="DeclSecurity"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public DeclSecurity ResolveDeclSecurity(uint rid) {
			return listDeclSecurityMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ClassLayout"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ClassLayout"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ClassLayout ResolveClassLayout(uint rid) {
			return listClassLayoutMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="FieldLayout"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="FieldLayout"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public FieldLayout ResolveFieldLayout(uint rid) {
			return listFieldLayoutMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="StandAloneSig"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="StandAloneSig"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public StandAloneSig ResolveStandAloneSig(uint rid) {
			return listStandAloneSigMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="EventMap"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="EventMap"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public EventMap ResolveEventMap(uint rid) {
			return listEventMapMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="EventPtr"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="EventPtr"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public EventPtr ResolveEventPtr(uint rid) {
			return listEventPtrMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="EventDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="EventDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public EventDef ResolveEvent(uint rid) {
			return listEventDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="PropertyMap"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="PropertyMap"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public PropertyMap ResolvePropertyMap(uint rid) {
			return listPropertyMapMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="PropertyPtr"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="PropertyPtr"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public PropertyPtr ResolvePropertyPtr(uint rid) {
			return listPropertyPtrMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="PropertyDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="PropertyDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public PropertyDef ResolveProperty(uint rid) {
			return listPropertyDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="MethodSemantics"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MethodSemantics"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public MethodSemantics ResolveMethodSemantics(uint rid) {
			return listMethodSemanticsMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="MethodImpl"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MethodImpl"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public MethodImpl ResolveMethodImpl(uint rid) {
			return listMethodImplMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ModuleRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ModuleRef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ModuleRef ResolveModuleRef(uint rid) {
			return listModuleRefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="TypeSpec"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public TypeSpec ResolveTypeSpec(uint rid) {
			return listTypeSpecMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ImplMap"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ImplMap"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ImplMap ResolveImplMap(uint rid) {
			return listImplMapMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="FieldRVA"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="FieldRVA"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public FieldRVA ResolveFieldRVA(uint rid) {
			return listFieldRVAMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ENCLog"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ENCLog"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ENCLog ResolveENCLog(uint rid) {
			return listENCLogMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ENCMap"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ENCMap"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ENCMap ResolveENCMap(uint rid) {
			return listENCMapMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="AssemblyDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="AssemblyDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public AssemblyDef ResolveAssembly(uint rid) {
			return listAssemblyDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="AssemblyProcessor"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="AssemblyProcessor"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public AssemblyProcessor ResolveAssemblyProcessor(uint rid) {
			return listAssemblyProcessorMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="AssemblyOS"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="AssemblyOS"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public AssemblyOS ResolveAssemblyOS(uint rid) {
			return listAssemblyOSMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="AssemblyRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="AssemblyRef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public AssemblyRef ResolveAssemblyRef(uint rid) {
			return listAssemblyRefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="AssemblyRefProcessor"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="AssemblyRefProcessor"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public AssemblyRefProcessor ResolveAssemblyRefProcessor(uint rid) {
			return listAssemblyRefProcessorMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="AssemblyRefOS"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="AssemblyRefOS"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public AssemblyRefOS ResolveAssemblyRefOS(uint rid) {
			return listAssemblyRefOSMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="FileDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="FileDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public FileDef ResolveFile(uint rid) {
			return listFileDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ExportedType"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ExportedType"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ExportedType ResolveExportedType(uint rid) {
			return listExportedTypeMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ManifestResource"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ManifestResource"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ManifestResource ResolveManifestResource(uint rid) {
			return listManifestResourceMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="NestedClass"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="NestedClass"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public NestedClass ResolveNestedClass(uint rid) {
			return listNestedClassMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="GenericParam"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="GenericParam"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public GenericParam ResolveGenericParam(uint rid) {
			if (listGenericParamMD == null)
				return null;
			return listGenericParamMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="MethodSpec"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MethodSpec"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public MethodSpec ResolveMethodSpec(uint rid) {
			if (listMethodSpecMD == null)
				return null;
			return listMethodSpecMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="GenericParamConstraint"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="GenericParamConstraint"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public GenericParamConstraint ResolveGenericParamConstraint(uint rid) {
			if (listGenericParamConstraintMD == null)
				return null;
			return listGenericParamConstraintMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="codedToken">A <c>TypeDefOrRef</c> coded token</param>
		/// <returns>A <see cref="ITypeDefOrRef"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public ITypeDefOrRef ResolveTypeDefOrRef(uint codedToken) {
			uint token;
			if (!CodedToken.TypeDefOrRef.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.TypeDef: return ResolveTypeDef(rid);
			case Table.TypeRef: return ResolveTypeRef(rid);
			case Table.TypeSpec: return ResolveTypeSpec(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IHasConstant"/>
		/// </summary>
		/// <param name="codedToken">A <c>HasConstant</c> coded token</param>
		/// <returns>A <see cref="IHasConstant"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public IHasConstant ResolveHasConstant(uint codedToken) {
			uint token;
			if (!CodedToken.HasConstant.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Field: return ResolveField(rid);
			case Table.Param: return ResolveParam(rid);
			case Table.Property: return ResolveProperty(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IHasCustomAttribute"/>
		/// </summary>
		/// <param name="codedToken">A <c>HasCustomAttribute</c> coded token</param>
		/// <returns>A <see cref="IHasCustomAttribute"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public IHasCustomAttribute ResolveHasCustomAttribute(uint codedToken) {
			uint token;
			if (!CodedToken.HasCustomAttribute.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Method: return ResolveMethod(rid);
			case Table.Field: return ResolveField(rid);
			case Table.TypeRef: return ResolveTypeRef(rid);
			case Table.TypeDef: return ResolveTypeDef(rid);
			case Table.Param: return ResolveParam(rid);
			case Table.InterfaceImpl: return ResolveInterfaceImpl(rid);
			case Table.MemberRef: return ResolveMemberRef(rid);
			case Table.Module: return ResolveModule(rid);
			case Table.DeclSecurity: return ResolveDeclSecurity(rid);
			case Table.Property: return ResolveProperty(rid);
			case Table.Event: return ResolveEvent(rid);
			case Table.StandAloneSig: return ResolveStandAloneSig(rid);
			case Table.ModuleRef: return ResolveModuleRef(rid);
			case Table.TypeSpec: return ResolveTypeSpec(rid);
			case Table.Assembly: return ResolveAssembly(rid);
			case Table.AssemblyRef: return ResolveAssemblyRef(rid);
			case Table.File: return ResolveFile(rid);
			case Table.ExportedType: return ResolveExportedType(rid);
			case Table.ManifestResource: return ResolveManifestResource(rid);
			case Table.GenericParam: return ResolveGenericParam(rid);
			case Table.GenericParamConstraint: return ResolveGenericParamConstraint(rid);
			case Table.MethodSpec: return ResolveMethodSpec(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IHasFieldMarshal"/>
		/// </summary>
		/// <param name="codedToken">A <c>HasFieldMarshal</c> coded token</param>
		/// <returns>A <see cref="IHasFieldMarshal"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public IHasFieldMarshal ResolveHasFieldMarshal(uint codedToken) {
			uint token;
			if (!CodedToken.HasFieldMarshal.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Field: return ResolveField(rid);
			case Table.Param: return ResolveParam(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IHasDeclSecurity"/>
		/// </summary>
		/// <param name="codedToken">A <c>HasDeclSecurity</c> coded token</param>
		/// <returns>A <see cref="IHasDeclSecurity"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public IHasDeclSecurity ResolveHasDeclSecurity(uint codedToken) {
			uint token;
			if (!CodedToken.HasDeclSecurity.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.TypeDef: return ResolveTypeDef(rid);
			case Table.Method: return ResolveMethod(rid);
			case Table.Assembly: return ResolveAssembly(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IMemberRefParent"/>
		/// </summary>
		/// <param name="codedToken">A <c>MemberRefParent</c> coded token</param>
		/// <returns>A <see cref="IMemberRefParent"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public IMemberRefParent ResolveMemberRefParent(uint codedToken) {
			uint token;
			if (!CodedToken.MemberRefParent.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.TypeDef: return ResolveTypeDef(rid);
			case Table.TypeRef: return ResolveTypeRef(rid);
			case Table.ModuleRef: return ResolveModuleRef(rid);
			case Table.Method: return ResolveMethod(rid);
			case Table.TypeSpec: return ResolveTypeSpec(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IHasSemantic"/>
		/// </summary>
		/// <param name="codedToken">A <c>HasSemantic</c> coded token</param>
		/// <returns>A <see cref="IHasSemantic"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public IHasSemantic ResolveHasSemantic(uint codedToken) {
			uint token;
			if (!CodedToken.HasSemantic.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Event: return ResolveEvent(rid);
			case Table.Property: return ResolveProperty(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IMethodDefOrRef"/>
		/// </summary>
		/// <param name="codedToken">A <c>MethodDefOrRef</c> coded token</param>
		/// <returns>A <see cref="IMethodDefOrRef"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public IMethodDefOrRef ResolveMethodDefOrRef(uint codedToken) {
			uint token;
			if (!CodedToken.MethodDefOrRef.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Method: return ResolveMethod(rid);
			case Table.MemberRef: return ResolveMemberRef(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IMemberForwarded"/>
		/// </summary>
		/// <param name="codedToken">A <c>MemberForwarded</c> coded token</param>
		/// <returns>A <see cref="IMemberForwarded"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public IMemberForwarded ResolveMemberForwarded(uint codedToken) {
			uint token;
			if (!CodedToken.MemberForwarded.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Field: return ResolveField(rid);
			case Table.Method: return ResolveMethod(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IImplementation"/>
		/// </summary>
		/// <param name="codedToken">An <c>Implementation</c> coded token</param>
		/// <returns>A <see cref="IImplementation"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public IImplementation ResolveImplementation(uint codedToken) {
			uint token;
			if (!CodedToken.Implementation.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.File: return ResolveFile(rid);
			case Table.AssemblyRef: return ResolveAssemblyRef(rid);
			case Table.ExportedType: return ResolveExportedType(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="ICustomAttributeType"/>
		/// </summary>
		/// <param name="codedToken">A <c>CustomAttributeType</c> coded token</param>
		/// <returns>A <see cref="ICustomAttributeType"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public ICustomAttributeType ResolveCustomAttributeType(uint codedToken) {
			uint token;
			if (!CodedToken.CustomAttributeType.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Method: return ResolveMethod(rid);
			case Table.MemberRef: return ResolveMemberRef(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IResolutionScope"/>
		/// </summary>
		/// <param name="codedToken">A <c>ResolutionScope</c> coded token</param>
		/// <returns>A <see cref="IResolutionScope"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public IResolutionScope ResolveResolutionScope(uint codedToken) {
			uint token;
			if (!CodedToken.ResolutionScope.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Module: return ResolveModule(rid);
			case Table.ModuleRef: return ResolveModuleRef(rid);
			case Table.AssemblyRef: return ResolveAssemblyRef(rid);
			case Table.TypeRef: return ResolveTypeRef(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="ITypeOrMethodDef"/>
		/// </summary>
		/// <param name="codedToken">A <c>TypeOrMethodDef</c>> coded token</param>
		/// <returns>A <see cref="ITypeOrMethodDef"/> or null if <paramref name="codedToken"/> is invalid</returns>
		public ITypeOrMethodDef ResolveTypeOrMethodDef(uint codedToken) {
			uint token;
			if (!CodedToken.TypeOrMethodDef.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.TypeDef: return ResolveTypeDef(rid);
			case Table.Method: return ResolveMethod(rid);
			}
			return null;
		}

		/// <summary>
		/// Reads a signature from the #Blob stream
		/// </summary>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public CallingConventionSig ReadSignature(uint sig) {
			return SignatureReader.ReadSig(this, sig);
		}

		/// <summary>
		/// Reads a type signature from the #Blob stream
		/// </summary>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <returns>A new <see cref="ITypeSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public ITypeSig ReadTypeSignature(uint sig) {
			return SignatureReader.ReadTypeSig(this, sig);
		}
	}
}
