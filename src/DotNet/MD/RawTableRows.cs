namespace dot10.DotNet.MD {
	/// <summary>
	/// Raw contents of an uncompressed Module table row
	/// </summary>
	public sealed class RawModuleRow {
		/// <summary/>
		public ushort Generation;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint Mvid;
		/// <summary/>
		public uint EncId;
		/// <summary/>
		public uint EncBaseId;

		/// <summary>Default constructor</summary>
		public RawModuleRow() {
		}

		/// <summary>Constructor</summary>
		public RawModuleRow(ushort Generation, uint Name, uint Mvid, uint EncId, uint EncBaseId) {
			this.Generation = Generation;
			this.Name = Name;
			this.Mvid = Mvid;
			this.EncId = EncId;
			this.EncBaseId = EncBaseId;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed TypeRef table row
	/// </summary>
	public sealed class RawTypeRefRow {
		/// <summary/>
		public uint ResolutionScope;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint Namespace;

		/// <summary>Default constructor</summary>
		public RawTypeRefRow() {
		}

		/// <summary>Constructor</summary>
		public RawTypeRefRow(uint ResolutionScope, uint Name, uint Namespace) {
			this.ResolutionScope = ResolutionScope;
			this.Name = Name;
			this.Namespace = Namespace;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed TypeDef table row
	/// </summary>
	public sealed class RawTypeDefRow {
		/// <summary/>
		public uint Flags;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint Namespace;
		/// <summary/>
		public uint Extends;
		/// <summary/>
		public uint FieldList;
		/// <summary/>
		public uint MethodList;

		/// <summary>Default constructor</summary>
		public RawTypeDefRow() {
		}

		/// <summary>Constructor</summary>
		public RawTypeDefRow(uint Flags, uint Name, uint Namespace, uint Extends, uint FieldList, uint MethodList) {
			this.Flags = Flags;
			this.Name = Name;
			this.Namespace = Namespace;
			this.Extends = Extends;
			this.FieldList = FieldList;
			this.MethodList = MethodList;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed FieldPtr table row
	/// </summary>
	public sealed class RawFieldPtrRow {
		/// <summary/>
		public uint Field;

		/// <summary>Default constructor</summary>
		public RawFieldPtrRow() {
		}

		/// <summary>Constructor</summary>
		public RawFieldPtrRow(uint Field) {
			this.Field = Field;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Field table row
	/// </summary>
	public sealed class RawFieldRow {
		/// <summary/>
		public ushort Flags;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint Signature;

		/// <summary>Default constructor</summary>
		public RawFieldRow() {
		}

		/// <summary>Constructor</summary>
		public RawFieldRow(ushort Flags, uint Name, uint Signature) {
			this.Flags = Flags;
			this.Name = Name;
			this.Signature = Signature;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodPtr table row
	/// </summary>
	public sealed class RawMethodPtrRow {
		/// <summary/>
		public uint Method;

		/// <summary>Default constructor</summary>
		public RawMethodPtrRow() {
		}

		/// <summary>Constructor</summary>
		public RawMethodPtrRow(uint Method) {
			this.Method = Method;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Method table row
	/// </summary>
	public sealed class RawMethodRow {
		/// <summary/>
		public uint RVA;
		/// <summary/>
		public ushort ImplFlags;
		/// <summary/>
		public ushort Flags;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint Signature;
		/// <summary/>
		public uint ParamList;

		/// <summary>Default constructor</summary>
		public RawMethodRow() {
		}

		/// <summary>Constructor</summary>
		public RawMethodRow(uint RVA, ushort ImplFlags, ushort Flags, uint Name, uint Signature, uint ParamList) {
			this.RVA = RVA;
			this.ImplFlags = ImplFlags;
			this.Flags = Flags;
			this.Name = Name;
			this.Signature = Signature;
			this.ParamList = ParamList;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ParamPtr table row
	/// </summary>
	public sealed class RawParamPtrRow {
		/// <summary/>
		public uint Param;

		/// <summary>Default constructor</summary>
		public RawParamPtrRow() {
		}

		/// <summary>Constructor</summary>
		public RawParamPtrRow(uint Param) {
			this.Param = Param;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Param table row
	/// </summary>
	public sealed class RawParamRow {
		/// <summary/>
		public ushort Flags;
		/// <summary/>
		public ushort Sequence;
		/// <summary/>
		public uint Name;

		/// <summary>Default constructor</summary>
		public RawParamRow() {
		}

		/// <summary>Constructor</summary>
		public RawParamRow(ushort Flags, ushort Sequence, uint Name) {
			this.Flags = Flags;
			this.Sequence = Sequence;
			this.Name = Name;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed InterfaceImpl table row
	/// </summary>
	public sealed class RawInterfaceImplRow {
		/// <summary/>
		public uint Class;
		/// <summary/>
		public uint Interface;

		/// <summary>Default constructor</summary>
		public RawInterfaceImplRow() {
		}

		/// <summary>Constructor</summary>
		public RawInterfaceImplRow(uint Class, uint Interface) {
			this.Class = Class;
			this.Interface = Interface;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MemberRef table row
	/// </summary>
	public sealed class RawMemberRefRow {
		/// <summary/>
		public uint Class;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint Signature;

		/// <summary>Default constructor</summary>
		public RawMemberRefRow() {
		}

		/// <summary>Constructor</summary>
		public RawMemberRefRow(uint Class, uint Name, uint Signature) {
			this.Class = Class;
			this.Name = Name;
			this.Signature = Signature;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Constant table row
	/// </summary>
	public sealed class RawConstantRow {
		/// <summary/>
		public byte Type;
		/// <summary/>
		public byte Padding;
		/// <summary/>
		public uint Parent;
		/// <summary/>
		public uint Value;

		/// <summary>Default constructor</summary>
		public RawConstantRow() {
		}

		/// <summary>Constructor</summary>
		public RawConstantRow(byte Type, byte Padding, uint Parent, uint Value) {
			this.Type = Type;
			this.Padding = Padding;
			this.Parent = Parent;
			this.Value = Value;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed CustomAttribute table row
	/// </summary>
	public sealed class RawCustomAttributeRow {
		/// <summary/>
		public uint Parent;
		/// <summary/>
		public uint Type;
		/// <summary/>
		public uint Value;

		/// <summary>Default constructor</summary>
		public RawCustomAttributeRow() {
		}

		/// <summary>Constructor</summary>
		public RawCustomAttributeRow(uint Parent, uint Type, uint Value) {
			this.Parent = Parent;
			this.Type = Type;
			this.Value = Value;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed FieldMarshal table row
	/// </summary>
	public sealed class RawFieldMarshalRow {
		/// <summary/>
		public uint Parent;
		/// <summary/>
		public uint NativeType;

		/// <summary>Default constructor</summary>
		public RawFieldMarshalRow() {
		}

		/// <summary>Constructor</summary>
		public RawFieldMarshalRow(uint Parent, uint NativeType) {
			this.Parent = Parent;
			this.NativeType = NativeType;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed DeclSecurity table row
	/// </summary>
	public sealed class RawDeclSecurityRow {
		/// <summary/>
		public short Action;
		/// <summary/>
		public uint Parent;
		/// <summary/>
		public uint PermissionSet;

		/// <summary>Default constructor</summary>
		public RawDeclSecurityRow() {
		}

		/// <summary>Constructor</summary>
		public RawDeclSecurityRow(short Action, uint Parent, uint PermissionSet) {
			this.Action = Action;
			this.Parent = Parent;
			this.PermissionSet = PermissionSet;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ClassLayout table row
	/// </summary>
	public sealed class RawClassLayoutRow {
		/// <summary/>
		public ushort PackingSize;
		/// <summary/>
		public uint ClassSize;
		/// <summary/>
		public uint Parent;

		/// <summary>Default constructor</summary>
		public RawClassLayoutRow() {
		}

		/// <summary>Constructor</summary>
		public RawClassLayoutRow(ushort PackingSize, uint ClassSize, uint Parent) {
			this.PackingSize = PackingSize;
			this.ClassSize = ClassSize;
			this.Parent = Parent;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed FieldLayout table row
	/// </summary>
	public sealed class RawFieldLayoutRow {
		/// <summary/>
		public uint OffSet;
		/// <summary/>
		public uint Field;

		/// <summary>Default constructor</summary>
		public RawFieldLayoutRow() {
		}

		/// <summary>Constructor</summary>
		public RawFieldLayoutRow(uint OffSet, uint Field) {
			this.OffSet = OffSet;
			this.Field = Field;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed StandAloneSig table row
	/// </summary>
	public sealed class RawStandAloneSigRow {
		/// <summary/>
		public uint Signature;

		/// <summary>Default constructor</summary>
		public RawStandAloneSigRow() {
		}

		/// <summary>Constructor</summary>
		public RawStandAloneSigRow(uint Signature) {
			this.Signature = Signature;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed EventMap table row
	/// </summary>
	public sealed class RawEventMapRow {
		/// <summary/>
		public uint Parent;
		/// <summary/>
		public uint EventList;

		/// <summary>Default constructor</summary>
		public RawEventMapRow() {
		}

		/// <summary>Constructor</summary>
		public RawEventMapRow(uint Parent, uint EventList) {
			this.Parent = Parent;
			this.EventList = EventList;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed EventPtr table row
	/// </summary>
	public sealed class RawEventPtrRow {
		/// <summary/>
		public uint Event;

		/// <summary>Default constructor</summary>
		public RawEventPtrRow() {
		}

		/// <summary>Constructor</summary>
		public RawEventPtrRow(uint Event) {
			this.Event = Event;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Event table row
	/// </summary>
	public sealed class RawEventRow {
		/// <summary/>
		public ushort EventFlags;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint EventType;

		/// <summary>Default constructor</summary>
		public RawEventRow() {
		}

		/// <summary>Constructor</summary>
		public RawEventRow(ushort EventFlags, uint Name, uint EventType) {
			this.EventFlags = EventFlags;
			this.Name = Name;
			this.EventType = EventType;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed PropertyMap table row
	/// </summary>
	public sealed class RawPropertyMapRow {
		/// <summary/>
		public uint Parent;
		/// <summary/>
		public uint PropertyList;

		/// <summary>Default constructor</summary>
		public RawPropertyMapRow() {
		}

		/// <summary>Constructor</summary>
		public RawPropertyMapRow(uint Parent, uint PropertyList) {
			this.Parent = Parent;
			this.PropertyList = PropertyList;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed PropertyPtr table row
	/// </summary>
	public sealed class RawPropertyPtrRow {
		/// <summary/>
		public uint Property;

		/// <summary>Default constructor</summary>
		public RawPropertyPtrRow() {
		}

		/// <summary>Constructor</summary>
		public RawPropertyPtrRow(uint Property) {
			this.Property = Property;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Property table row
	/// </summary>
	public sealed class RawPropertyRow {
		/// <summary/>
		public ushort PropFlags;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint Type;

		/// <summary>Default constructor</summary>
		public RawPropertyRow() {
		}

		/// <summary>Constructor</summary>
		public RawPropertyRow(ushort PropFlags, uint Name, uint Type) {
			this.PropFlags = PropFlags;
			this.Name = Name;
			this.Type = Type;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodSemantics table row
	/// </summary>
	public sealed class RawMethodSemanticsRow {
		/// <summary/>
		public ushort Semantic;
		/// <summary/>
		public uint Method;
		/// <summary/>
		public uint Association;

		/// <summary>Default constructor</summary>
		public RawMethodSemanticsRow() {
		}

		/// <summary>Constructor</summary>
		public RawMethodSemanticsRow(ushort Semantic, uint Method, uint Association) {
			this.Semantic = Semantic;
			this.Method = Method;
			this.Association = Association;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodImpl table row
	/// </summary>
	public sealed class RawMethodImplRow {
		/// <summary/>
		public uint Class;
		/// <summary/>
		public uint MethodBody;
		/// <summary/>
		public uint MethodDeclaration;

		/// <summary>Default constructor</summary>
		public RawMethodImplRow() {
		}

		/// <summary>Constructor</summary>
		public RawMethodImplRow(uint Class, uint MethodBody, uint MethodDeclaration) {
			this.Class = Class;
			this.MethodBody = MethodBody;
			this.MethodDeclaration = MethodDeclaration;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ModuleRef table row
	/// </summary>
	public sealed class RawModuleRefRow {
		/// <summary/>
		public uint Name;

		/// <summary>Default constructor</summary>
		public RawModuleRefRow() {
		}

		/// <summary>Constructor</summary>
		public RawModuleRefRow(uint Name) {
			this.Name = Name;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed TypeSpec table row
	/// </summary>
	public sealed class RawTypeSpecRow {
		/// <summary/>
		public uint Signature;

		/// <summary>Default constructor</summary>
		public RawTypeSpecRow() {
		}

		/// <summary>Constructor</summary>
		public RawTypeSpecRow(uint Signature) {
			this.Signature = Signature;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ImplMap table row
	/// </summary>
	public sealed class RawImplMapRow {
		/// <summary/>
		public ushort MappingFlags;
		/// <summary/>
		public uint MemberForwarded;
		/// <summary/>
		public uint ImportName;
		/// <summary/>
		public uint ImportScope;

		/// <summary>Default constructor</summary>
		public RawImplMapRow() {
		}

		/// <summary>Constructor</summary>
		public RawImplMapRow(ushort MappingFlags, uint MemberForwarded, uint ImportName, uint ImportScope) {
			this.MappingFlags = MappingFlags;
			this.MemberForwarded = MemberForwarded;
			this.ImportName = ImportName;
			this.ImportScope = ImportScope;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed FieldRVA table row
	/// </summary>
	public sealed class RawFieldRVARow {
		/// <summary/>
		public uint RVA;
		/// <summary/>
		public uint Field;

		/// <summary>Default constructor</summary>
		public RawFieldRVARow() {
		}

		/// <summary>Constructor</summary>
		public RawFieldRVARow(uint RVA, uint Field) {
			this.RVA = RVA;
			this.Field = Field;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ENCLog table row
	/// </summary>
	public sealed class RawENCLogRow {
		/// <summary/>
		public uint Token;
		/// <summary/>
		public uint FuncCode;

		/// <summary>Default constructor</summary>
		public RawENCLogRow() {
		}

		/// <summary>Constructor</summary>
		public RawENCLogRow(uint Token, uint FuncCode) {
			this.Token = Token;
			this.FuncCode = FuncCode;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ENCMap table row
	/// </summary>
	public sealed class RawENCMapRow {
		/// <summary/>
		public uint Token;

		/// <summary>Default constructor</summary>
		public RawENCMapRow() {
		}

		/// <summary>Constructor</summary>
		public RawENCMapRow(uint Token) {
			this.Token = Token;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Assembly table row
	/// </summary>
	public sealed class RawAssemblyRow {
		/// <summary/>
		public uint HashAlgId;
		/// <summary/>
		public ushort MajorVersion;
		/// <summary/>
		public ushort MinorVersion;
		/// <summary/>
		public ushort BuildNumber;
		/// <summary/>
		public ushort RevisionNumber;
		/// <summary/>
		public uint Flags;
		/// <summary/>
		public uint PublicKey;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint Locale;

		/// <summary>Default constructor</summary>
		public RawAssemblyRow() {
		}

		/// <summary>Constructor</summary>
		public RawAssemblyRow(uint HashAlgId, ushort MajorVersion, ushort MinorVersion, ushort BuildNumber, ushort RevisionNumber, uint Flags, uint PublicKey, uint Name, uint Locale) {
			this.HashAlgId = HashAlgId;
			this.MajorVersion = MajorVersion;
			this.MinorVersion = MinorVersion;
			this.BuildNumber = BuildNumber;
			this.RevisionNumber = RevisionNumber;
			this.Flags = Flags;
			this.PublicKey = PublicKey;
			this.Name = Name;
			this.Locale = Locale;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyProcessor table row
	/// </summary>
	public sealed class RawAssemblyProcessorRow {
		/// <summary/>
		public uint Processor;

		/// <summary>Default constructor</summary>
		public RawAssemblyProcessorRow() {
		}

		/// <summary>Constructor</summary>
		public RawAssemblyProcessorRow(uint Processor) {
			this.Processor = Processor;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyOS table row
	/// </summary>
	public sealed class RawAssemblyOSRow {
		/// <summary/>
		public uint OSPlatformId;
		/// <summary/>
		public uint OSMajorVersion;
		/// <summary/>
		public uint OSMinorVersion;

		/// <summary>Default constructor</summary>
		public RawAssemblyOSRow() {
		}

		/// <summary>Constructor</summary>
		public RawAssemblyOSRow(uint OSPlatformId, uint OSMajorVersion, uint OSMinorVersion) {
			this.OSPlatformId = OSPlatformId;
			this.OSMajorVersion = OSMajorVersion;
			this.OSMinorVersion = OSMinorVersion;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyRef table row
	/// </summary>
	public sealed class RawAssemblyRefRow {
		/// <summary/>
		public ushort MajorVersion;
		/// <summary/>
		public ushort MinorVersion;
		/// <summary/>
		public ushort BuildNumber;
		/// <summary/>
		public ushort RevisionNumber;
		/// <summary/>
		public uint Flags;
		/// <summary/>
		public uint PublicKeyOrToken;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint Locale;
		/// <summary/>
		public uint HashValue;

		/// <summary>Default constructor</summary>
		public RawAssemblyRefRow() {
		}

		/// <summary>Constructor</summary>
		public RawAssemblyRefRow(ushort MajorVersion, ushort MinorVersion, ushort BuildNumber, ushort RevisionNumber, uint Flags, uint PublicKeyOrToken, uint Name, uint Locale, uint HashValue) {
			this.MajorVersion = MajorVersion;
			this.MinorVersion = MinorVersion;
			this.BuildNumber = BuildNumber;
			this.RevisionNumber = RevisionNumber;
			this.Flags = Flags;
			this.PublicKeyOrToken = PublicKeyOrToken;
			this.Name = Name;
			this.Locale = Locale;
			this.HashValue = HashValue;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyRefProcessor table row
	/// </summary>
	public sealed class RawAssemblyRefProcessorRow {
		/// <summary/>
		public uint Processor;
		/// <summary/>
		public uint AssemblyRef;

		/// <summary>Default constructor</summary>
		public RawAssemblyRefProcessorRow() {
		}

		/// <summary>Constructor</summary>
		public RawAssemblyRefProcessorRow(uint Processor, uint AssemblyRef) {
			this.Processor = Processor;
			this.AssemblyRef = AssemblyRef;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyRefOS table row
	/// </summary>
	public sealed class RawAssemblyRefOSRow {
		/// <summary/>
		public uint OSPlatformId;
		/// <summary/>
		public uint OSMajorVersion;
		/// <summary/>
		public uint OSMinorVersion;
		/// <summary/>
		public uint AssemblyRef;

		/// <summary>Default constructor</summary>
		public RawAssemblyRefOSRow() {
		}

		/// <summary>Constructor</summary>
		public RawAssemblyRefOSRow(uint OSPlatformId, uint OSMajorVersion, uint OSMinorVersion, uint AssemblyRef) {
			this.OSPlatformId = OSPlatformId;
			this.OSMajorVersion = OSMajorVersion;
			this.OSMinorVersion = OSMinorVersion;
			this.AssemblyRef = AssemblyRef;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed File table row
	/// </summary>
	public sealed class RawFileRow {
		/// <summary/>
		public uint Flags;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint HashValue;

		/// <summary>Default constructor</summary>
		public RawFileRow() {
		}

		/// <summary>Constructor</summary>
		public RawFileRow(uint Flags, uint Name, uint HashValue) {
			this.Flags = Flags;
			this.Name = Name;
			this.HashValue = HashValue;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ExportedType table row
	/// </summary>
	public sealed class RawExportedTypeRow {
		/// <summary/>
		public uint Flags;
		/// <summary/>
		public uint TypeDefId;
		/// <summary/>
		public uint TypeName;
		/// <summary/>
		public uint TypeNamespace;
		/// <summary/>
		public uint Implementation;

		/// <summary>Default constructor</summary>
		public RawExportedTypeRow() {
		}

		/// <summary>Constructor</summary>
		public RawExportedTypeRow(uint Flags, uint TypeDefId, uint TypeName, uint TypeNamespace, uint Implementation) {
			this.Flags = Flags;
			this.TypeDefId = TypeDefId;
			this.TypeName = TypeName;
			this.TypeNamespace = TypeNamespace;
			this.Implementation = Implementation;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ManifestResource table row
	/// </summary>
	public sealed class RawManifestResourceRow {
		/// <summary/>
		public uint Offset;
		/// <summary/>
		public uint Flags;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint Implementation;

		/// <summary>Default constructor</summary>
		public RawManifestResourceRow() {
		}

		/// <summary>Constructor</summary>
		public RawManifestResourceRow(uint Offset, uint Flags, uint Name, uint Implementation) {
			this.Offset = Offset;
			this.Flags = Flags;
			this.Name = Name;
			this.Implementation = Implementation;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed NestedClass table row
	/// </summary>
	public sealed class RawNestedClassRow {
		/// <summary/>
		public uint NestedClass;
		/// <summary/>
		public uint EnclosingClass;

		/// <summary>Default constructor</summary>
		public RawNestedClassRow() {
		}

		/// <summary>Constructor</summary>
		public RawNestedClassRow(uint NestedClass, uint EnclosingClass) {
			this.NestedClass = NestedClass;
			this.EnclosingClass = EnclosingClass;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed GenericParam table row
	/// </summary>
	public sealed class RawGenericParamRow {
		/// <summary/>
		public ushort Number;
		/// <summary/>
		public ushort Flags;
		/// <summary/>
		public uint Owner;
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint Kind;

		/// <summary>Default constructor</summary>
		public RawGenericParamRow() {
		}

		/// <summary>Constructor</summary>
		public RawGenericParamRow(ushort Number, ushort Flags, uint Owner, uint Name, uint Kind) {
			this.Number = Number;
			this.Flags = Flags;
			this.Owner = Owner;
			this.Name = Name;
			this.Kind = Kind;
		}

		/// <summary>Constructor</summary>
		public RawGenericParamRow(ushort Number, ushort Flags, uint Owner, uint Name) {
			this.Number = Number;
			this.Flags = Flags;
			this.Owner = Owner;
			this.Name = Name;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodSpec table row
	/// </summary>
	public sealed class RawMethodSpecRow {
		/// <summary/>
		public uint Method;
		/// <summary/>
		public uint Instantiation;

		/// <summary>Default constructor</summary>
		public RawMethodSpecRow() {
		}

		/// <summary>Constructor</summary>
		public RawMethodSpecRow(uint Method, uint Instantiation) {
			this.Method = Method;
			this.Instantiation = Instantiation;
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed GenericParamConstraint table row
	/// </summary>
	public sealed class RawGenericParamConstraintRow {
		/// <summary/>
		public uint Owner;
		/// <summary/>
		public uint Constraint;

		/// <summary>Default constructor</summary>
		public RawGenericParamConstraintRow() {
		}

		/// <summary>Constructor</summary>
		public RawGenericParamConstraintRow(uint Owner, uint Constraint) {
			this.Owner = Owner;
			this.Constraint = Constraint;
		}
	}
}
