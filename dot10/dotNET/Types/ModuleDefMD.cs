using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the Module table
	/// </summary>
	sealed class ModuleDefMD : ModuleDef {
		/// <summary>The file that contains all .NET metadata</summary>
		DotNetFile dnFile;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawModuleRow rawRow;

		UserValue<ushort> generation;
		UserValue<UTF8String> name;
		UserValue<Guid?> mvid;
		UserValue<Guid?> encId;
		UserValue<Guid?> encBaseId;
		UserValue<AssemblyDef> assembly;

		LazyList<ModuleDefMD> listModuleDefMD;
		LazyList<TypeRefMD> listTypeRefMD;
		LazyList<TypeDefMD> listTypeDefMD;
		LazyList<FieldDefMD> listFieldDefMD;
		LazyList<MethodDefMD> listMethodDefMD;
		LazyList<ParamDefMD> listParamDefMD;
		LazyList<InterfaceImplMD> listInterfaceImplMD;
		LazyList<MemberRefMD> listMemberRefMD;
		LazyList<DeclSecurityMD> listDeclSecurityMD;
		LazyList<StandAloneSigMD> listStandAloneSigMD;
		LazyList<EventDefMD> listEventDefMD;
		LazyList<PropertyDefMD> listPropertyDefMD;
		LazyList<ModuleRefMD> listModuleRefMD;
		LazyList<TypeSpecMD> listTypeSpecMD;
		LazyList<AssemblyDefMD> listAssemblyDefMD;
		LazyList<AssemblyRefMD> listAssemblyRefMD;
		LazyList<FileDefMD> listFileDefMD;
		LazyList<ExportedTypeMD> listExportedTypeMD;
		LazyList<ManifestResourceMD> listManifestResourceMD;
		LazyList<GenericParamMD> listGenericParamMD;
		LazyList<MethodSpecMD> listMethodSpecMD;
		LazyList<GenericParamConstraintMD> listGenericParamConstraintMD;

		/// <summary>
		/// Returns the .NET file
		/// </summary>
		public DotNetFile DotNetFile {
			get { return dnFile; }
		}

		/// <inheritdoc/>
		public override ushort Generation {
			get { return generation.Value; }
			set { generation.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override Guid? Mvid {
			get { return mvid.Value; }
			set { mvid.Value = value; }
		}

		/// <inheritdoc/>
		public override Guid? EncId {
			get { return encId.Value; }
			set { encId.Value = value; }
		}

		/// <inheritdoc/>
		public override Guid? EncBaseId {
			get { return encBaseId.Value; }
			set { encBaseId.Value = value; }
		}

		/// <inheritdoc/>
		public override AssemblyDef Assembly {
			get { return assembly.Value; }
			set { assembly.Value = value; }
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
			: this(dnFile, 1) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dnFile">The loaded .NET file</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="dnFile"/> is null</exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> &gt; <c>0x00FFFFFF</c></exception>
		ModuleDefMD(DotNetFile dnFile, uint rid) {
			if (rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (dnFile == null)
				throw new ArgumentNullException("dnFile");

			this.dnFile = dnFile;
			this.rid = rid;
			Initialize();
		}

		void Initialize() {
			generation.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.Generation;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return dnFile.MetaData.StringsStream.Read(rawRow.Name);
			};
			mvid.ReadOriginalValue = () => {
				InitializeRawRow();
				return dnFile.MetaData.GuidStream.Read(rawRow.Mvid);
			};
			encId.ReadOriginalValue = () => {
				InitializeRawRow();
				return dnFile.MetaData.GuidStream.Read(rawRow.EncId);
			};
			encBaseId.ReadOriginalValue = () => {
				InitializeRawRow();
				return dnFile.MetaData.GuidStream.Read(rawRow.EncBaseId);
			};
			assembly.ReadOriginalValue = () => {
				if (rid != 1)
					return null;
				//TODO: Call ResolveAssembly(1) instead.
				if (dnFile.MetaData.TablesStream.Get(Table.Assembly).Rows >= 1) {
					var asm = new AssemblyDefMD(dnFile.MetaData, 1);
					asm.ManifestModule = this;
					return asm;
				}
				return null;
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = dnFile.MetaData.TablesStream.ReadModuleRow(rid) ?? new RawModuleRow();
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
		/// Resolves a <see cref="ModuleDefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ModuleDefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ModuleDefMD ReadModule(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="TypeRefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="TypeRefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public TypeRefMD ReadTypeRef(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="TypeDefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="TypeDefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public TypeDefMD ReadTypeDef(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="FieldDefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="FieldDefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public FieldDefMD ReadField(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="MethodDefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MethodDefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public MethodDefMD ReadMethod(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="ParamDefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ParamDefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ParamDefMD ReadParam(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="InterfaceImplMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="InterfaceImplMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public InterfaceImplMD ReadInterfaceImpl(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="MemberRefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MemberRefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public MemberRefMD ReadMemberRef(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="DeclSecurityMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="DeclSecurityMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public DeclSecurityMD ReadDeclSecurity(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="StandAloneSigMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="StandAloneSigMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public StandAloneSigMD ReadStandAloneSig(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="EventDefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="EventDefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public EventDefMD ReadEvent(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="PropertyDefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="PropertyDefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public PropertyDefMD ReadProperty(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="ModuleRefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ModuleRefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ModuleRefMD ReadModuleRef(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="TypeSpecMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="TypeSpecMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public TypeSpecMD ReadTypeSpec(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="AssemblyDefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="AssemblyDefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public AssemblyDefMD ReadAssembly(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="AssemblyRefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="AssemblyRefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public AssemblyRefMD ReadAssemblyRef(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="FileDefMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="FileDefMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public FileDefMD ReadFile(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="ExportedTypeMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ExportedTypeMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ExportedTypeMD ReadExportedType(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="ManifestResourceMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ManifestResourceMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ManifestResourceMD ReadManifestResource(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="GenericParamMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="GenericParamMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public GenericParamMD ReadGenericParam(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="MethodSpecMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MethodSpecMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public MethodSpecMD ReadMethodSpec(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolves a <see cref="GenericParamConstraintMD"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="GenericParamConstraintMD"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public GenericParamConstraintMD ReadGenericParamConstraint(uint rid) {
			throw new NotImplementedException();	//TODO:
		}

		/// <summary>
		/// Resolve a token
		/// </summary>
		/// <param name="mdToken">The metadata token</param>
		/// <returns>A <see cref="ICodedToken"/> or null if <paramref name="mdToken"/> is invalid</returns>
		public ICodedToken ResolveToken(MDToken mdToken) {
			return ResolveToken(mdToken.Raw);
		}

		/// <summary>
		/// Resolve a token
		/// </summary>
		/// <param name="token">The metadata token</param>
		/// <returns>A <see cref="ICodedToken"/> or null if <paramref name="token"/> is invalid</returns>
		public ICodedToken ResolveToken(int token) {
			return ResolveToken((uint)token);
		}

		/// <summary>
		/// Resolve a token
		/// </summary>
		/// <param name="token">The metadata token</param>
		/// <returns>A <see cref="ICodedToken"/> or null if <paramref name="token"/> is invalid</returns>
		public ICodedToken ResolveToken(uint token) {
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
			case Table.Module: return ResolveModule(rid);
			case Table.TypeRef: return ResolveTypeRef(rid);
			case Table.TypeDef: return ResolveTypeDef(rid);
			case Table.Field: return ResolveField(rid);
			case Table.Method: return ResolveMethod(rid);
			case Table.Param: return ResolveParam(rid);
			case Table.InterfaceImpl: return ResolveInterfaceImpl(rid);
			case Table.MemberRef: return ResolveMemberRef(rid);
			case Table.DeclSecurity: return ResolveDeclSecurity(rid);
			case Table.StandAloneSig: return ResolveStandAloneSig(rid);
			case Table.Event: return ResolveEvent(rid);
			case Table.Property: return ResolveProperty(rid);
			case Table.ModuleRef: return ResolveModuleRef(rid);
			case Table.TypeSpec: return ResolveTypeSpec(rid);
			case Table.Assembly: return ResolveAssembly(rid);
			case Table.AssemblyRef: return ResolveAssemblyRef(rid);
			case Table.File: return ResolveFile(rid);
			case Table.ExportedType: return ResolveExportedType(rid);
			case Table.ManifestResource: return ResolveManifestResource(rid);
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
			if (listModuleDefMD == null)
				listModuleDefMD = new LazyList<ModuleDefMD>(dnFile.MetaData.TablesStream.Get(Table.Module).Rows, ReadModule);
			return listModuleDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="TypeRef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public TypeRef ResolveTypeRef(uint rid) {
			if (listTypeRefMD == null)
				listTypeRefMD = new LazyList<TypeRefMD>(dnFile.MetaData.TablesStream.Get(Table.TypeRef).Rows, ReadTypeRef);
			return listTypeRefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="TypeDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public TypeDef ResolveTypeDef(uint rid) {
			if (listTypeDefMD == null)
				listTypeDefMD = new LazyList<TypeDefMD>(dnFile.MetaData.TablesStream.Get(Table.TypeDef).Rows, ReadTypeDef);
			return listTypeDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="FieldDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="FieldDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public FieldDef ResolveField(uint rid) {
			if (listFieldDefMD == null)
				listFieldDefMD = new LazyList<FieldDefMD>(dnFile.MetaData.TablesStream.Get(Table.Field).Rows, ReadField);
			return listFieldDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="MethodDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MethodDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public MethodDef ResolveMethod(uint rid) {
			if (listMethodDefMD == null)
				listMethodDefMD = new LazyList<MethodDefMD>(dnFile.MetaData.TablesStream.Get(Table.Method).Rows, ReadMethod);
			return listMethodDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ParamDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ParamDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ParamDef ResolveParam(uint rid) {
			if (listParamDefMD == null)
				listParamDefMD = new LazyList<ParamDefMD>(dnFile.MetaData.TablesStream.Get(Table.Param).Rows, ReadParam);
			return listParamDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="InterfaceImpl"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="InterfaceImpl"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public InterfaceImpl ResolveInterfaceImpl(uint rid) {
			if (listInterfaceImplMD == null)
				listInterfaceImplMD = new LazyList<InterfaceImplMD>(dnFile.MetaData.TablesStream.Get(Table.InterfaceImpl).Rows, ReadInterfaceImpl);
			return listInterfaceImplMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MemberRef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public MemberRef ResolveMemberRef(uint rid) {
			if (listMemberRefMD == null)
				listMemberRefMD = new LazyList<MemberRefMD>(dnFile.MetaData.TablesStream.Get(Table.MemberRef).Rows, ReadMemberRef);
			return listMemberRefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="DeclSecurity"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="DeclSecurity"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public DeclSecurity ResolveDeclSecurity(uint rid) {
			if (listDeclSecurityMD == null)
				listDeclSecurityMD = new LazyList<DeclSecurityMD>(dnFile.MetaData.TablesStream.Get(Table.DeclSecurity).Rows, ReadDeclSecurity);
			return listDeclSecurityMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="StandAloneSig"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="StandAloneSig"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public StandAloneSig ResolveStandAloneSig(uint rid) {
			if (listStandAloneSigMD == null)
				listStandAloneSigMD = new LazyList<StandAloneSigMD>(dnFile.MetaData.TablesStream.Get(Table.StandAloneSig).Rows, ReadStandAloneSig);
			return listStandAloneSigMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="EventDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="EventDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public EventDef ResolveEvent(uint rid) {
			if (listEventDefMD == null)
				listEventDefMD = new LazyList<EventDefMD>(dnFile.MetaData.TablesStream.Get(Table.Event).Rows, ReadEvent);
			return listEventDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="PropertyDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="PropertyDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public PropertyDef ResolveProperty(uint rid) {
			if (listPropertyDefMD == null)
				listPropertyDefMD = new LazyList<PropertyDefMD>(dnFile.MetaData.TablesStream.Get(Table.Property).Rows, ReadProperty);
			return listPropertyDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ModuleRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ModuleRef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ModuleRef ResolveModuleRef(uint rid) {
			if (listModuleRefMD == null)
				listModuleRefMD = new LazyList<ModuleRefMD>(dnFile.MetaData.TablesStream.Get(Table.ModuleRef).Rows, ReadModuleRef);
			return listModuleRefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="TypeSpec"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public TypeSpec ResolveTypeSpec(uint rid) {
			if (listTypeSpecMD == null)
				listTypeSpecMD = new LazyList<TypeSpecMD>(dnFile.MetaData.TablesStream.Get(Table.TypeSpec).Rows, ReadTypeSpec);
			return listTypeSpecMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="AssemblyDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="AssemblyDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public AssemblyDef ResolveAssembly(uint rid) {
			if (listAssemblyDefMD == null)
				listAssemblyDefMD = new LazyList<AssemblyDefMD>(dnFile.MetaData.TablesStream.Get(Table.Assembly).Rows, ReadAssembly);
			return listAssemblyDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="AssemblyRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="AssemblyRef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public AssemblyRef ResolveAssemblyRef(uint rid) {
			if (listAssemblyRefMD == null)
				listAssemblyRefMD = new LazyList<AssemblyRefMD>(dnFile.MetaData.TablesStream.Get(Table.AssemblyRef).Rows, ReadAssemblyRef);
			return listAssemblyRefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="FileDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="FileDef"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public FileDef ResolveFile(uint rid) {
			if (listFileDefMD == null)
				listFileDefMD = new LazyList<FileDefMD>(dnFile.MetaData.TablesStream.Get(Table.File).Rows, ReadFile);
			return listFileDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ExportedType"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ExportedType"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ExportedType ResolveExportedType(uint rid) {
			if (listExportedTypeMD == null)
				listExportedTypeMD = new LazyList<ExportedTypeMD>(dnFile.MetaData.TablesStream.Get(Table.ExportedType).Rows, ReadExportedType);
			return listExportedTypeMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ManifestResource"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ManifestResource"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public ManifestResource ResolveManifestResource(uint rid) {
			if (listManifestResourceMD == null)
				listManifestResourceMD = new LazyList<ManifestResourceMD>(dnFile.MetaData.TablesStream.Get(Table.ManifestResource).Rows, ReadManifestResource);
			return listManifestResourceMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="GenericParam"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="GenericParam"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public GenericParam ResolveGenericParam(uint rid) {
			if (listGenericParamMD == null)
				listGenericParamMD = new LazyList<GenericParamMD>(dnFile.MetaData.TablesStream.Get(Table.GenericParam).Rows, ReadGenericParam);
			return listGenericParamMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="MethodSpec"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MethodSpec"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public MethodSpec ResolveMethodSpec(uint rid) {
			if (listMethodSpecMD == null)
				listMethodSpecMD = new LazyList<MethodSpecMD>(dnFile.MetaData.TablesStream.Get(Table.MethodSpec).Rows, ReadMethodSpec);
			return listMethodSpecMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="GenericParamConstraint"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="GenericParamConstraint"/> instance or null if <paramref name="rid"/> is invalid</returns>
		public GenericParamConstraint ResolveGenericParamConstraint(uint rid) {
			if (listGenericParamConstraintMD == null)
				listGenericParamConstraintMD = new LazyList<GenericParamConstraintMD>(dnFile.MetaData.TablesStream.Get(Table.GenericParamConstraint).Rows, ReadGenericParamConstraint);
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
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
			uint rid = token & 0x00FFFFFF;
			switch ((Table)(token >> 24)) {
			case Table.TypeDef: return ResolveTypeDef(rid);
			case Table.Method: return ResolveMethod(rid);
			}
			return null;
		}
	}
}
