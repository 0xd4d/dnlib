// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.MD {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
	/// <summary>
	/// Raw contents of an uncompressed Module table row
	/// </summary>
	public readonly struct RawModuleRow {
		public readonly ushort Generation;
		public readonly uint Name;
		public readonly uint Mvid;
		public readonly uint EncId;
		public readonly uint EncBaseId;

		public RawModuleRow(ushort Generation, uint Name, uint Mvid, uint EncId, uint EncBaseId) {
			this.Generation = Generation;
			this.Name = Name;
			this.Mvid = Mvid;
			this.EncId = EncId;
			this.EncBaseId = EncBaseId;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Generation;
				case 1: return Name;
				case 2: return Mvid;
				case 3: return EncId;
				case 4: return EncBaseId;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed TypeRef table row
	/// </summary>
	public readonly struct RawTypeRefRow {
		public readonly uint ResolutionScope;
		public readonly uint Name;
		public readonly uint Namespace;

		public RawTypeRefRow(uint ResolutionScope, uint Name, uint Namespace) {
			this.ResolutionScope = ResolutionScope;
			this.Name = Name;
			this.Namespace = Namespace;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return ResolutionScope;
				case 1: return Name;
				case 2: return Namespace;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed TypeDef table row
	/// </summary>
	public readonly struct RawTypeDefRow {
		public readonly uint Flags;
		public readonly uint Name;
		public readonly uint Namespace;
		public readonly uint Extends;
		public readonly uint FieldList;
		public readonly uint MethodList;

		public RawTypeDefRow(uint Flags, uint Name, uint Namespace, uint Extends, uint FieldList, uint MethodList) {
			this.Flags = Flags;
			this.Name = Name;
			this.Namespace = Namespace;
			this.Extends = Extends;
			this.FieldList = FieldList;
			this.MethodList = MethodList;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Flags;
				case 1: return Name;
				case 2: return Namespace;
				case 3: return Extends;
				case 4: return FieldList;
				case 5: return MethodList;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed FieldPtr table row
	/// </summary>
	public readonly struct RawFieldPtrRow {
		public readonly uint Field;

		public RawFieldPtrRow(uint Field) => this.Field = Field;

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Field;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Field table row
	/// </summary>
	public readonly struct RawFieldRow {
		public readonly ushort Flags;
		public readonly uint Name;
		public readonly uint Signature;

		public RawFieldRow(ushort Flags, uint Name, uint Signature) {
			this.Flags = Flags;
			this.Name = Name;
			this.Signature = Signature;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Flags;
				case 1: return Name;
				case 2: return Signature;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodPtr table row
	/// </summary>
	public readonly struct RawMethodPtrRow {
		public readonly uint Method;

		public RawMethodPtrRow(uint Method) => this.Method = Method;

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Method;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Method table row
	/// </summary>
	public readonly struct RawMethodRow {
		public readonly uint RVA;
		public readonly ushort ImplFlags;
		public readonly ushort Flags;
		public readonly uint Name;
		public readonly uint Signature;
		public readonly uint ParamList;

		public RawMethodRow(uint RVA, ushort ImplFlags, ushort Flags, uint Name, uint Signature, uint ParamList) {
			this.RVA = RVA;
			this.ImplFlags = ImplFlags;
			this.Flags = Flags;
			this.Name = Name;
			this.Signature = Signature;
			this.ParamList = ParamList;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return RVA;
				case 1: return ImplFlags;
				case 2: return Flags;
				case 3: return Name;
				case 4: return Signature;
				case 5: return ParamList;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ParamPtr table row
	/// </summary>
	public readonly struct RawParamPtrRow {
		public readonly uint Param;

		public RawParamPtrRow(uint Param) => this.Param = Param;

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Param;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Param table row
	/// </summary>
	public readonly struct RawParamRow {
		public readonly ushort Flags;
		public readonly ushort Sequence;
		public readonly uint Name;

		public RawParamRow(ushort Flags, ushort Sequence, uint Name) {
			this.Flags = Flags;
			this.Sequence = Sequence;
			this.Name = Name;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Flags;
				case 1: return Sequence;
				case 2: return Name;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed InterfaceImpl table row
	/// </summary>
	public readonly struct RawInterfaceImplRow {
		public readonly uint Class;
		public readonly uint Interface;

		public RawInterfaceImplRow(uint Class, uint Interface) {
			this.Class = Class;
			this.Interface = Interface;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Class;
				case 1: return Interface;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MemberRef table row
	/// </summary>
	public readonly struct RawMemberRefRow {
		public readonly uint Class;
		public readonly uint Name;
		public readonly uint Signature;

		public RawMemberRefRow(uint Class, uint Name, uint Signature) {
			this.Class = Class;
			this.Name = Name;
			this.Signature = Signature;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Class;
				case 1: return Name;
				case 2: return Signature;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Constant table row
	/// </summary>
	public readonly struct RawConstantRow {
		public readonly byte Type;
		public readonly byte Padding;
		public readonly uint Parent;
		public readonly uint Value;

		public RawConstantRow(byte Type, byte Padding, uint Parent, uint Value) {
			this.Type = Type;
			this.Padding = Padding;
			this.Parent = Parent;
			this.Value = Value;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Type;
				case 1: return Padding;
				case 2: return Parent;
				case 3: return Value;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed CustomAttribute table row
	/// </summary>
	public readonly struct RawCustomAttributeRow {
		public readonly uint Parent;
		public readonly uint Type;
		public readonly uint Value;

		public RawCustomAttributeRow(uint Parent, uint Type, uint Value) {
			this.Parent = Parent;
			this.Type = Type;
			this.Value = Value;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Parent;
				case 1: return Type;
				case 2: return Value;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed FieldMarshal table row
	/// </summary>
	public readonly struct RawFieldMarshalRow {
		public readonly uint Parent;
		public readonly uint NativeType;

		public RawFieldMarshalRow(uint Parent, uint NativeType) {
			this.Parent = Parent;
			this.NativeType = NativeType;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Parent;
				case 1: return NativeType;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed DeclSecurity table row
	/// </summary>
	public readonly struct RawDeclSecurityRow {
		public readonly short Action;
		public readonly uint Parent;
		public readonly uint PermissionSet;

		public RawDeclSecurityRow(short Action, uint Parent, uint PermissionSet) {
			this.Action = Action;
			this.Parent = Parent;
			this.PermissionSet = PermissionSet;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return (uint)(int)Action;
				case 1: return Parent;
				case 2: return PermissionSet;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ClassLayout table row
	/// </summary>
	public readonly struct RawClassLayoutRow {
		public readonly ushort PackingSize;
		public readonly uint ClassSize;
		public readonly uint Parent;

		public RawClassLayoutRow(ushort PackingSize, uint ClassSize, uint Parent) {
			this.PackingSize = PackingSize;
			this.ClassSize = ClassSize;
			this.Parent = Parent;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return PackingSize;
				case 1: return ClassSize;
				case 2: return Parent;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed FieldLayout table row
	/// </summary>
	public readonly struct RawFieldLayoutRow {
		public readonly uint OffSet;
		public readonly uint Field;

		public RawFieldLayoutRow(uint OffSet, uint Field) {
			this.OffSet = OffSet;
			this.Field = Field;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return OffSet;
				case 1: return Field;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed StandAloneSig table row
	/// </summary>
	public readonly struct RawStandAloneSigRow {
		public readonly uint Signature;

		public RawStandAloneSigRow(uint Signature) => this.Signature = Signature;

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Signature;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed EventMap table row
	/// </summary>
	public readonly struct RawEventMapRow {
		public readonly uint Parent;
		public readonly uint EventList;

		public RawEventMapRow(uint Parent, uint EventList) {
			this.Parent = Parent;
			this.EventList = EventList;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Parent;
				case 1: return EventList;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed EventPtr table row
	/// </summary>
	public readonly struct RawEventPtrRow {
		public readonly uint Event;

		public RawEventPtrRow(uint Event) => this.Event = Event;

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Event;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Event table row
	/// </summary>
	public readonly struct RawEventRow {
		public readonly ushort EventFlags;
		public readonly uint Name;
		public readonly uint EventType;

		public RawEventRow(ushort EventFlags, uint Name, uint EventType) {
			this.EventFlags = EventFlags;
			this.Name = Name;
			this.EventType = EventType;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return EventFlags;
				case 1: return Name;
				case 2: return EventType;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed PropertyMap table row
	/// </summary>
	public readonly struct RawPropertyMapRow {
		public readonly uint Parent;
		public readonly uint PropertyList;

		public RawPropertyMapRow(uint Parent, uint PropertyList) {
			this.Parent = Parent;
			this.PropertyList = PropertyList;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Parent;
				case 1: return PropertyList;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed PropertyPtr table row
	/// </summary>
	public readonly struct RawPropertyPtrRow {
		public readonly uint Property;

		public RawPropertyPtrRow(uint Property) => this.Property = Property;

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Property;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Property table row
	/// </summary>
	public readonly struct RawPropertyRow {
		public readonly ushort PropFlags;
		public readonly uint Name;
		public readonly uint Type;

		public RawPropertyRow(ushort PropFlags, uint Name, uint Type) {
			this.PropFlags = PropFlags;
			this.Name = Name;
			this.Type = Type;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return PropFlags;
				case 1: return Name;
				case 2: return Type;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodSemantics table row
	/// </summary>
	public readonly struct RawMethodSemanticsRow {
		public readonly ushort Semantic;
		public readonly uint Method;
		public readonly uint Association;

		public RawMethodSemanticsRow(ushort Semantic, uint Method, uint Association) {
			this.Semantic = Semantic;
			this.Method = Method;
			this.Association = Association;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Semantic;
				case 1: return Method;
				case 2: return Association;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodImpl table row
	/// </summary>
	public readonly struct RawMethodImplRow {
		public readonly uint Class;
		public readonly uint MethodBody;
		public readonly uint MethodDeclaration;

		public RawMethodImplRow(uint Class, uint MethodBody, uint MethodDeclaration) {
			this.Class = Class;
			this.MethodBody = MethodBody;
			this.MethodDeclaration = MethodDeclaration;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Class;
				case 1: return MethodBody;
				case 2: return MethodDeclaration;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ModuleRef table row
	/// </summary>
	public readonly struct RawModuleRefRow {
		public readonly uint Name;

		public RawModuleRefRow(uint Name) => this.Name = Name;

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Name;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed TypeSpec table row
	/// </summary>
	public readonly struct RawTypeSpecRow {
		public readonly uint Signature;

		public RawTypeSpecRow(uint Signature) => this.Signature = Signature;

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Signature;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ImplMap table row
	/// </summary>
	public readonly struct RawImplMapRow {
		public readonly ushort MappingFlags;
		public readonly uint MemberForwarded;
		public readonly uint ImportName;
		public readonly uint ImportScope;

		public RawImplMapRow(ushort MappingFlags, uint MemberForwarded, uint ImportName, uint ImportScope) {
			this.MappingFlags = MappingFlags;
			this.MemberForwarded = MemberForwarded;
			this.ImportName = ImportName;
			this.ImportScope = ImportScope;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return MappingFlags;
				case 1: return MemberForwarded;
				case 2: return ImportName;
				case 3: return ImportScope;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed FieldRVA table row
	/// </summary>
	public readonly struct RawFieldRVARow {
		public readonly uint RVA;
		public readonly uint Field;

		public RawFieldRVARow(uint RVA, uint Field) {
			this.RVA = RVA;
			this.Field = Field;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return RVA;
				case 1: return Field;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ENCLog table row
	/// </summary>
	public readonly struct RawENCLogRow {
		public readonly uint Token;
		public readonly uint FuncCode;

		public RawENCLogRow(uint Token, uint FuncCode) {
			this.Token = Token;
			this.FuncCode = FuncCode;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Token;
				case 1: return FuncCode;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ENCMap table row
	/// </summary>
	public readonly struct RawENCMapRow {
		public readonly uint Token;

		public RawENCMapRow(uint Token) => this.Token = Token;

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Token;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Assembly table row
	/// </summary>
	public readonly struct RawAssemblyRow {
		public readonly uint HashAlgId;
		public readonly ushort MajorVersion;
		public readonly ushort MinorVersion;
		public readonly ushort BuildNumber;
		public readonly ushort RevisionNumber;
		public readonly uint Flags;
		public readonly uint PublicKey;
		public readonly uint Name;
		public readonly uint Locale;

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

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return HashAlgId;
				case 1: return MajorVersion;
				case 2: return MinorVersion;
				case 3: return BuildNumber;
				case 4: return RevisionNumber;
				case 5: return Flags;
				case 6: return PublicKey;
				case 7: return Name;
				case 8: return Locale;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyProcessor table row
	/// </summary>
	public readonly struct RawAssemblyProcessorRow {
		public readonly uint Processor;

		public RawAssemblyProcessorRow(uint Processor) => this.Processor = Processor;

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Processor;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyOS table row
	/// </summary>
	public readonly struct RawAssemblyOSRow {
		public readonly uint OSPlatformId;
		public readonly uint OSMajorVersion;
		public readonly uint OSMinorVersion;

		public RawAssemblyOSRow(uint OSPlatformId, uint OSMajorVersion, uint OSMinorVersion) {
			this.OSPlatformId = OSPlatformId;
			this.OSMajorVersion = OSMajorVersion;
			this.OSMinorVersion = OSMinorVersion;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return OSPlatformId;
				case 1: return OSMajorVersion;
				case 2: return OSMinorVersion;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyRef table row
	/// </summary>
	public readonly struct RawAssemblyRefRow {
		public readonly ushort MajorVersion;
		public readonly ushort MinorVersion;
		public readonly ushort BuildNumber;
		public readonly ushort RevisionNumber;
		public readonly uint Flags;
		public readonly uint PublicKeyOrToken;
		public readonly uint Name;
		public readonly uint Locale;
		public readonly uint HashValue;

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

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return MajorVersion;
				case 1: return MinorVersion;
				case 2: return BuildNumber;
				case 3: return RevisionNumber;
				case 4: return Flags;
				case 5: return PublicKeyOrToken;
				case 6: return Name;
				case 7: return Locale;
				case 8: return HashValue;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyRefProcessor table row
	/// </summary>
	public readonly struct RawAssemblyRefProcessorRow {
		public readonly uint Processor;
		public readonly uint AssemblyRef;

		public RawAssemblyRefProcessorRow(uint Processor, uint AssemblyRef) {
			this.Processor = Processor;
			this.AssemblyRef = AssemblyRef;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Processor;
				case 1: return AssemblyRef;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyRefOS table row
	/// </summary>
	public readonly struct RawAssemblyRefOSRow {
		public readonly uint OSPlatformId;
		public readonly uint OSMajorVersion;
		public readonly uint OSMinorVersion;
		public readonly uint AssemblyRef;

		public RawAssemblyRefOSRow(uint OSPlatformId, uint OSMajorVersion, uint OSMinorVersion, uint AssemblyRef) {
			this.OSPlatformId = OSPlatformId;
			this.OSMajorVersion = OSMajorVersion;
			this.OSMinorVersion = OSMinorVersion;
			this.AssemblyRef = AssemblyRef;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return OSPlatformId;
				case 1: return OSMajorVersion;
				case 2: return OSMinorVersion;
				case 3: return AssemblyRef;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed File table row
	/// </summary>
	public readonly struct RawFileRow {
		public readonly uint Flags;
		public readonly uint Name;
		public readonly uint HashValue;

		public RawFileRow(uint Flags, uint Name, uint HashValue) {
			this.Flags = Flags;
			this.Name = Name;
			this.HashValue = HashValue;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Flags;
				case 1: return Name;
				case 2: return HashValue;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ExportedType table row
	/// </summary>
	public readonly struct RawExportedTypeRow {
		public readonly uint Flags;
		public readonly uint TypeDefId;
		public readonly uint TypeName;
		public readonly uint TypeNamespace;
		public readonly uint Implementation;

		public RawExportedTypeRow(uint Flags, uint TypeDefId, uint TypeName, uint TypeNamespace, uint Implementation) {
			this.Flags = Flags;
			this.TypeDefId = TypeDefId;
			this.TypeName = TypeName;
			this.TypeNamespace = TypeNamespace;
			this.Implementation = Implementation;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Flags;
				case 1: return TypeDefId;
				case 2: return TypeName;
				case 3: return TypeNamespace;
				case 4: return Implementation;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ManifestResource table row
	/// </summary>
	public readonly struct RawManifestResourceRow {
		public readonly uint Offset;
		public readonly uint Flags;
		public readonly uint Name;
		public readonly uint Implementation;

		public RawManifestResourceRow(uint Offset, uint Flags, uint Name, uint Implementation) {
			this.Offset = Offset;
			this.Flags = Flags;
			this.Name = Name;
			this.Implementation = Implementation;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Offset;
				case 1: return Flags;
				case 2: return Name;
				case 3: return Implementation;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed NestedClass table row
	/// </summary>
	public readonly struct RawNestedClassRow {
		public readonly uint NestedClass;
		public readonly uint EnclosingClass;

		public RawNestedClassRow(uint NestedClass, uint EnclosingClass) {
			this.NestedClass = NestedClass;
			this.EnclosingClass = EnclosingClass;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return NestedClass;
				case 1: return EnclosingClass;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed GenericParam table row
	/// </summary>
	public readonly struct RawGenericParamRow {
		public readonly ushort Number;
		public readonly ushort Flags;
		public readonly uint Owner;
		public readonly uint Name;
		public readonly uint Kind;

		public RawGenericParamRow(ushort Number, ushort Flags, uint Owner, uint Name, uint Kind) {
			this.Number = Number;
			this.Flags = Flags;
			this.Owner = Owner;
			this.Name = Name;
			this.Kind = Kind;
		}

		public RawGenericParamRow(ushort Number, ushort Flags, uint Owner, uint Name) {
			this.Number = Number;
			this.Flags = Flags;
			this.Owner = Owner;
			this.Name = Name;
			Kind = 0;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Number;
				case 1: return Flags;
				case 2: return Owner;
				case 3: return Name;
				case 4: return Kind;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodSpec table row
	/// </summary>
	public readonly struct RawMethodSpecRow {
		public readonly uint Method;
		public readonly uint Instantiation;

		public RawMethodSpecRow(uint Method, uint Instantiation) {
			this.Method = Method;
			this.Instantiation = Instantiation;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Method;
				case 1: return Instantiation;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed GenericParamConstraint table row
	/// </summary>
	public readonly struct RawGenericParamConstraintRow {
		public readonly uint Owner;
		public readonly uint Constraint;

		public RawGenericParamConstraintRow(uint Owner, uint Constraint) {
			this.Owner = Owner;
			this.Constraint = Constraint;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Owner;
				case 1: return Constraint;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Document table row
	/// </summary>
	public readonly struct RawDocumentRow {
		public readonly uint Name;
		public readonly uint HashAlgorithm;
		public readonly uint Hash;
		public readonly uint Language;

		public RawDocumentRow(uint Name, uint HashAlgorithm, uint Hash, uint Language) {
			this.Name = Name;
			this.HashAlgorithm = HashAlgorithm;
			this.Hash = Hash;
			this.Language = Language;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Name;
				case 1: return HashAlgorithm;
				case 2: return Hash;
				case 3: return Language;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodDebugInformation table row
	/// </summary>
	public readonly struct RawMethodDebugInformationRow {
		public readonly uint Document;
		public readonly uint SequencePoints;

		public RawMethodDebugInformationRow(uint Document, uint SequencePoints) {
			this.Document = Document;
			this.SequencePoints = SequencePoints;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Document;
				case 1: return SequencePoints;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed LocalScope table row
	/// </summary>
	public readonly struct RawLocalScopeRow {
		public readonly uint Method;
		public readonly uint ImportScope;
		public readonly uint VariableList;
		public readonly uint ConstantList;
		public readonly uint StartOffset;
		public readonly uint Length;

		public RawLocalScopeRow(uint Method, uint ImportScope, uint VariableList, uint ConstantList, uint StartOffset, uint Length) {
			this.Method = Method;
			this.ImportScope = ImportScope;
			this.VariableList = VariableList;
			this.ConstantList = ConstantList;
			this.StartOffset = StartOffset;
			this.Length = Length;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Method;
				case 1: return ImportScope;
				case 2: return VariableList;
				case 3: return ConstantList;
				case 4: return StartOffset;
				case 5: return Length;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed LocalVariable table row
	/// </summary>
	public readonly struct RawLocalVariableRow {
		public readonly ushort Attributes;
		public readonly ushort Index;
		public readonly uint Name;

		public RawLocalVariableRow(ushort Attributes, ushort Index, uint Name) {
			this.Attributes = Attributes;
			this.Index = Index;
			this.Name = Name;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Attributes;
				case 1: return Index;
				case 2: return Name;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed LocalConstant table row
	/// </summary>
	public readonly struct RawLocalConstantRow {
		public readonly uint Name;
		public readonly uint Signature;

		public RawLocalConstantRow(uint Name, uint Signature) {
			this.Name = Name;
			this.Signature = Signature;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Name;
				case 1: return Signature;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ImportScope table row
	/// </summary>
	public readonly struct RawImportScopeRow {
		public readonly uint Parent;
		public readonly uint Imports;

		public RawImportScopeRow(uint Parent, uint Imports) {
			this.Parent = Parent;
			this.Imports = Imports;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Parent;
				case 1: return Imports;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed StateMachineMethod table row
	/// </summary>
	public readonly struct RawStateMachineMethodRow {
		public readonly uint MoveNextMethod;
		public readonly uint KickoffMethod;

		public RawStateMachineMethodRow(uint MoveNextMethod, uint KickoffMethod) {
			this.MoveNextMethod = MoveNextMethod;
			this.KickoffMethod = KickoffMethod;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return MoveNextMethod;
				case 1: return KickoffMethod;
				default: return 0;
				}
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed CustomDebugInformation table row
	/// </summary>
	public readonly struct RawCustomDebugInformationRow {
		public readonly uint Parent;
		public readonly uint Kind;
		public readonly uint Value;

		public RawCustomDebugInformationRow(uint Parent, uint Kind, uint Value) {
			this.Parent = Parent;
			this.Kind = Kind;
			this.Value = Value;
		}

		/// <summary>
		/// Gets a column
		/// </summary>
		/// <param name="index">Index of column</param>
		/// <returns></returns>
		public uint this[int index] {
			get {
				switch (index) {
				case 0: return Parent;
				case 1: return Kind;
				case 2: return Value;
				default: return 0;
				}
			}
		}
	}
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
}
