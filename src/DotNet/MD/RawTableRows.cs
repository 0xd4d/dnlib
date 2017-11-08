// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet.MD {
	/// <summary>
	/// A raw table row
	/// </summary>
	public interface IRawRow {
		/// <summary>
		/// Reads a column
		/// </summary>
		/// <param name="index">Column index</param>
		/// <returns>Column value</returns>
		uint Read(int index);

		/// <summary>
		/// Writes a column
		/// </summary>
		/// <param name="index">Column index</param>
		/// <param name="value">New value</param>
		void Write(int index, uint value);
	}

	/// <summary>
	/// Raw contents of an uncompressed Module table row
	/// </summary>
	public sealed class RawModuleRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Generation;
			case 1: return Name;
			case 2: return Mvid;
			case 3: return EncId;
			case 4: return EncBaseId;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Generation = (ushort)value; break;
			case 1: Name = value; break;
			case 2: Mvid = value; break;
			case 3: EncId = value; break;
			case 4: EncBaseId = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed TypeRef table row
	/// </summary>
	public sealed class RawTypeRefRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return ResolutionScope;
			case 1: return Name;
			case 2: return Namespace;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: ResolutionScope = value; break;
			case 1: Name = value; break;
			case 2: Namespace = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed TypeDef table row
	/// </summary>
	public sealed class RawTypeDefRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
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

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Flags = value; break;
			case 1: Name = value; break;
			case 2: Namespace = value; break;
			case 3: Extends = value; break;
			case 4: FieldList = value; break;
			case 5: MethodList = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed FieldPtr table row
	/// </summary>
	public sealed class RawFieldPtrRow : IRawRow {
		/// <summary/>
		public uint Field;

		/// <summary>Default constructor</summary>
		public RawFieldPtrRow() {
		}

		/// <summary>Constructor</summary>
		public RawFieldPtrRow(uint Field) {
			this.Field = Field;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Field;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Field = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Field table row
	/// </summary>
	public sealed class RawFieldRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Flags;
			case 1: return Name;
			case 2: return Signature;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Flags = (ushort)value; break;
			case 1: Name = value; break;
			case 2: Signature = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodPtr table row
	/// </summary>
	public sealed class RawMethodPtrRow : IRawRow {
		/// <summary/>
		public uint Method;

		/// <summary>Default constructor</summary>
		public RawMethodPtrRow() {
		}

		/// <summary>Constructor</summary>
		public RawMethodPtrRow(uint Method) {
			this.Method = Method;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Method;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Method = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Method table row
	/// </summary>
	public sealed class RawMethodRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
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

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: RVA = value; break;
			case 1: ImplFlags = (ushort)value; break;
			case 2: Flags = (ushort)value; break;
			case 3: Name = value; break;
			case 4: Signature = value; break;
			case 5: ParamList = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ParamPtr table row
	/// </summary>
	public sealed class RawParamPtrRow : IRawRow {
		/// <summary/>
		public uint Param;

		/// <summary>Default constructor</summary>
		public RawParamPtrRow() {
		}

		/// <summary>Constructor</summary>
		public RawParamPtrRow(uint Param) {
			this.Param = Param;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Param;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Param = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Param table row
	/// </summary>
	public sealed class RawParamRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Flags;
			case 1: return Sequence;
			case 2: return Name;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Flags = (ushort)value; break;
			case 1: Sequence = (ushort)value; break;
			case 2: Name = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed InterfaceImpl table row
	/// </summary>
	public sealed class RawInterfaceImplRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Class;
			case 1: return Interface;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Class = value; break;
			case 1: Interface = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MemberRef table row
	/// </summary>
	public sealed class RawMemberRefRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Class;
			case 1: return Name;
			case 2: return Signature;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Class = value; break;
			case 1: Name = value; break;
			case 2: Signature = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Constant table row
	/// </summary>
	public sealed class RawConstantRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Type;
			case 1: return Parent;
			case 2: return Value;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Type = (byte)value; break;
			case 1: Parent = value; break;
			case 2: Value = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed CustomAttribute table row
	/// </summary>
	public sealed class RawCustomAttributeRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Parent;
			case 1: return Type;
			case 2: return Value;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Parent = value; break;
			case 1: Type = value; break;
			case 2: Value = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed FieldMarshal table row
	/// </summary>
	public sealed class RawFieldMarshalRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Parent;
			case 1: return NativeType;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Parent = value; break;
			case 1: NativeType = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed DeclSecurity table row
	/// </summary>
	public sealed class RawDeclSecurityRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return (uint)(int)Action;
			case 1: return Parent;
			case 2: return PermissionSet;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Action = (short)value; break;
			case 1: Parent = value; break;
			case 2: PermissionSet = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ClassLayout table row
	/// </summary>
	public sealed class RawClassLayoutRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return PackingSize;
			case 1: return ClassSize;
			case 2: return Parent;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: PackingSize = (ushort)value; break;
			case 1: ClassSize = value; break;
			case 2: Parent = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed FieldLayout table row
	/// </summary>
	public sealed class RawFieldLayoutRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return OffSet;
			case 1: return Field;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: OffSet = value; break;
			case 1: Field = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed StandAloneSig table row
	/// </summary>
	public sealed class RawStandAloneSigRow : IRawRow {
		/// <summary/>
		public uint Signature;

		/// <summary>Default constructor</summary>
		public RawStandAloneSigRow() {
		}

		/// <summary>Constructor</summary>
		public RawStandAloneSigRow(uint Signature) {
			this.Signature = Signature;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Signature;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Signature = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed EventMap table row
	/// </summary>
	public sealed class RawEventMapRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Parent;
			case 1: return EventList;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Parent = value; break;
			case 1: EventList = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed EventPtr table row
	/// </summary>
	public sealed class RawEventPtrRow : IRawRow {
		/// <summary/>
		public uint Event;

		/// <summary>Default constructor</summary>
		public RawEventPtrRow() {
		}

		/// <summary>Constructor</summary>
		public RawEventPtrRow(uint Event) {
			this.Event = Event;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Event;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Event = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Event table row
	/// </summary>
	public sealed class RawEventRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return EventFlags;
			case 1: return Name;
			case 2: return EventType;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: EventFlags = (ushort)value; break;
			case 1: Name = value; break;
			case 2: EventType = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed PropertyMap table row
	/// </summary>
	public sealed class RawPropertyMapRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Parent;
			case 1: return PropertyList;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Parent = value; break;
			case 1: PropertyList = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed PropertyPtr table row
	/// </summary>
	public sealed class RawPropertyPtrRow : IRawRow {
		/// <summary/>
		public uint Property;

		/// <summary>Default constructor</summary>
		public RawPropertyPtrRow() {
		}

		/// <summary>Constructor</summary>
		public RawPropertyPtrRow(uint Property) {
			this.Property = Property;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Property;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Property = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Property table row
	/// </summary>
	public sealed class RawPropertyRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return PropFlags;
			case 1: return Name;
			case 2: return Type;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: PropFlags = (ushort)value; break;
			case 1: Name = value; break;
			case 2: Type = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodSemantics table row
	/// </summary>
	public sealed class RawMethodSemanticsRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Semantic;
			case 1: return Method;
			case 2: return Association;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Semantic = (ushort)value; break;
			case 1: Method = value; break;
			case 2: Association = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodImpl table row
	/// </summary>
	public sealed class RawMethodImplRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Class;
			case 1: return MethodBody;
			case 2: return MethodDeclaration;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Class = value; break;
			case 1: MethodBody = value; break;
			case 2: MethodDeclaration = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ModuleRef table row
	/// </summary>
	public sealed class RawModuleRefRow : IRawRow {
		/// <summary/>
		public uint Name;

		/// <summary>Default constructor</summary>
		public RawModuleRefRow() {
		}

		/// <summary>Constructor</summary>
		public RawModuleRefRow(uint Name) {
			this.Name = Name;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Name;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Name = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed TypeSpec table row
	/// </summary>
	public sealed class RawTypeSpecRow : IRawRow {
		/// <summary/>
		public uint Signature;

		/// <summary>Default constructor</summary>
		public RawTypeSpecRow() {
		}

		/// <summary>Constructor</summary>
		public RawTypeSpecRow(uint Signature) {
			this.Signature = Signature;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Signature;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Signature = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ImplMap table row
	/// </summary>
	public sealed class RawImplMapRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return MappingFlags;
			case 1: return MemberForwarded;
			case 2: return ImportName;
			case 3: return ImportScope;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: MappingFlags = (ushort)value; break;
			case 1: MemberForwarded = value; break;
			case 2: ImportName = value; break;
			case 3: ImportScope = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed FieldRVA table row
	/// </summary>
	public sealed class RawFieldRVARow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return RVA;
			case 1: return Field;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: RVA = value; break;
			case 1: Field = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ENCLog table row
	/// </summary>
	public sealed class RawENCLogRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Token;
			case 1: return FuncCode;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Token = value; break;
			case 1: FuncCode = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ENCMap table row
	/// </summary>
	public sealed class RawENCMapRow : IRawRow {
		/// <summary/>
		public uint Token;

		/// <summary>Default constructor</summary>
		public RawENCMapRow() {
		}

		/// <summary>Constructor</summary>
		public RawENCMapRow(uint Token) {
			this.Token = Token;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Token;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Token = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Assembly table row
	/// </summary>
	public sealed class RawAssemblyRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
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

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: HashAlgId = value; break;
			case 1: MajorVersion = (ushort)value; break;
			case 2: MinorVersion = (ushort)value; break;
			case 3: BuildNumber = (ushort)value; break;
			case 4: RevisionNumber = (ushort)value; break;
			case 5: Flags = value; break;
			case 6: PublicKey = value; break;
			case 7: Name = value; break;
			case 8: Locale = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyProcessor table row
	/// </summary>
	public sealed class RawAssemblyProcessorRow : IRawRow {
		/// <summary/>
		public uint Processor;

		/// <summary>Default constructor</summary>
		public RawAssemblyProcessorRow() {
		}

		/// <summary>Constructor</summary>
		public RawAssemblyProcessorRow(uint Processor) {
			this.Processor = Processor;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Processor;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Processor = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyOS table row
	/// </summary>
	public sealed class RawAssemblyOSRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return OSPlatformId;
			case 1: return OSMajorVersion;
			case 2: return OSMinorVersion;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: OSPlatformId = value; break;
			case 1: OSMajorVersion = value; break;
			case 2: OSMinorVersion = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyRef table row
	/// </summary>
	public sealed class RawAssemblyRefRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
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

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: MajorVersion = (ushort)value; break;
			case 1: MinorVersion = (ushort)value; break;
			case 2: BuildNumber = (ushort)value; break;
			case 3: RevisionNumber = (ushort)value; break;
			case 4: Flags = value; break;
			case 5: PublicKeyOrToken = value; break;
			case 6: Name = value; break;
			case 7: Locale = value; break;
			case 8: HashValue = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyRefProcessor table row
	/// </summary>
	public sealed class RawAssemblyRefProcessorRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Processor;
			case 1: return AssemblyRef;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Processor = value; break;
			case 1: AssemblyRef = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed AssemblyRefOS table row
	/// </summary>
	public sealed class RawAssemblyRefOSRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return OSPlatformId;
			case 1: return OSMajorVersion;
			case 2: return OSMinorVersion;
			case 3: return AssemblyRef;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: OSPlatformId = value; break;
			case 1: OSMajorVersion = value; break;
			case 2: OSMinorVersion = value; break;
			case 3: AssemblyRef = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed File table row
	/// </summary>
	public sealed class RawFileRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Flags;
			case 1: return Name;
			case 2: return HashValue;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Flags = value; break;
			case 1: Name = value; break;
			case 2: HashValue = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ExportedType table row
	/// </summary>
	public sealed class RawExportedTypeRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Flags;
			case 1: return TypeDefId;
			case 2: return TypeName;
			case 3: return TypeNamespace;
			case 4: return Implementation;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Flags = value; break;
			case 1: TypeDefId = value; break;
			case 2: TypeName = value; break;
			case 3: TypeNamespace = value; break;
			case 4: Implementation = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ManifestResource table row
	/// </summary>
	public sealed class RawManifestResourceRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Offset;
			case 1: return Flags;
			case 2: return Name;
			case 3: return Implementation;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Offset = value; break;
			case 1: Flags = value; break;
			case 2: Name = value; break;
			case 3: Implementation = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed NestedClass table row
	/// </summary>
	public sealed class RawNestedClassRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return NestedClass;
			case 1: return EnclosingClass;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: NestedClass = value; break;
			case 1: EnclosingClass = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed GenericParam table row
	/// </summary>
	public sealed class RawGenericParamRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Number;
			case 1: return Flags;
			case 2: return Owner;
			case 3: return Name;
			case 4: return Kind;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Number = (ushort)value; break;
			case 1: Flags = (ushort)value; break;
			case 2: Owner = value; break;
			case 3: Name = value; break;
			case 4: Kind = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodSpec table row
	/// </summary>
	public sealed class RawMethodSpecRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Method;
			case 1: return Instantiation;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Method = value; break;
			case 1: Instantiation = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed GenericParamConstraint table row
	/// </summary>
	public sealed class RawGenericParamConstraintRow : IRawRow {
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

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Owner;
			case 1: return Constraint;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Owner = value; break;
			case 1: Constraint = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed Document table row
	/// </summary>
	public sealed class RawDocumentRow : IRawRow {
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint HashAlgorithm;
		/// <summary/>
		public uint Hash;
		/// <summary/>
		public uint Language;

		/// <summary>Default constructor</summary>
		public RawDocumentRow() {
		}

		/// <summary>Constructor</summary>
		public RawDocumentRow(uint Name, uint HashAlgorithm, uint Hash, uint Language) {
			this.Name = Name;
			this.HashAlgorithm = HashAlgorithm;
			this.Hash = Hash;
			this.Language = Language;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Name;
			case 1: return HashAlgorithm;
			case 2: return Hash;
			case 3: return Language;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Name = value; break;
			case 1: HashAlgorithm = value; break;
			case 2: Hash = value; break;
			case 3: Language = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed MethodDebugInformation table row
	/// </summary>
	public sealed class RawMethodDebugInformationRow : IRawRow {
		/// <summary/>
		public uint Document;
		/// <summary/>
		public uint SequencePoints;

		/// <summary>Default constructor</summary>
		public RawMethodDebugInformationRow() {
		}

		/// <summary>Constructor</summary>
		public RawMethodDebugInformationRow(uint Document, uint SequencePoints) {
			this.Document = Document;
			this.SequencePoints = SequencePoints;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Document;
			case 1: return SequencePoints;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Document = value; break;
			case 1: SequencePoints = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed LocalScope table row
	/// </summary>
	public sealed class RawLocalScopeRow : IRawRow {
		/// <summary/>
		public uint Method;
		/// <summary/>
		public uint ImportScope;
		/// <summary/>
		public uint VariableList;
		/// <summary/>
		public uint ConstantList;
		/// <summary/>
		public uint StartOffset;
		/// <summary/>
		public uint Length;

		/// <summary>Default constructor</summary>
		public RawLocalScopeRow() {
		}

		/// <summary>Constructor</summary>
		public RawLocalScopeRow(uint Method, uint ImportScope, uint VariableList, uint ConstantList, uint StartOffset, uint Length) {
			this.Method = Method;
			this.ImportScope = ImportScope;
			this.VariableList = VariableList;
			this.ConstantList = ConstantList;
			this.StartOffset = StartOffset;
			this.Length = Length;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
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

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Method = value; break;
			case 1: ImportScope = value; break;
			case 2: VariableList = value; break;
			case 3: ConstantList = value; break;
			case 4: StartOffset = value; break;
			case 5: Length = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed LocalVariable table row
	/// </summary>
	public sealed class RawLocalVariableRow : IRawRow {
		/// <summary/>
		public ushort Attributes;
		/// <summary/>
		public ushort Index;
		/// <summary/>
		public uint Name;

		/// <summary>Default constructor</summary>
		public RawLocalVariableRow() {
		}

		/// <summary>Constructor</summary>
		public RawLocalVariableRow(ushort Attributes, ushort Index, uint Name) {
			this.Attributes = Attributes;
			this.Index = Index;
			this.Name = Name;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Attributes;
			case 1: return Index;
			case 2: return Name;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Attributes = (ushort)value; break;
			case 1: Index = (ushort)value; break;
			case 2: Name = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed LocalConstant table row
	/// </summary>
	public sealed class RawLocalConstantRow : IRawRow {
		/// <summary/>
		public uint Name;
		/// <summary/>
		public uint Signature;

		/// <summary>Default constructor</summary>
		public RawLocalConstantRow() {
		}

		/// <summary>Constructor</summary>
		public RawLocalConstantRow(uint Name, uint Signature) {
			this.Name = Name;
			this.Signature = Signature;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Name;
			case 1: return Signature;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Name = value; break;
			case 1: Signature = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed ImportScope table row
	/// </summary>
	public sealed class RawImportScopeRow : IRawRow {
		/// <summary/>
		public uint Parent;
		/// <summary/>
		public uint Imports;

		/// <summary>Default constructor</summary>
		public RawImportScopeRow() {
		}

		/// <summary>Constructor</summary>
		public RawImportScopeRow(uint Parent, uint Imports) {
			this.Parent = Parent;
			this.Imports = Imports;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Parent;
			case 1: return Imports;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Parent = value; break;
			case 1: Imports = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed StateMachineMethod table row
	/// </summary>
	public sealed class RawStateMachineMethodRow : IRawRow {
		/// <summary/>
		public uint MoveNextMethod;
		/// <summary/>
		public uint KickoffMethod;

		/// <summary>Default constructor</summary>
		public RawStateMachineMethodRow() {
		}

		/// <summary>Constructor</summary>
		public RawStateMachineMethodRow(uint MoveNextMethod, uint KickoffMethod) {
			this.MoveNextMethod = MoveNextMethod;
			this.KickoffMethod = KickoffMethod;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return MoveNextMethod;
			case 1: return KickoffMethod;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: MoveNextMethod = value; break;
			case 1: KickoffMethod = value; break;
			default: break;
			}
		}
	}

	/// <summary>
	/// Raw contents of an uncompressed CustomDebugInformation table row
	/// </summary>
	public sealed class RawCustomDebugInformationRow : IRawRow {
		/// <summary/>
		public uint Parent;
		/// <summary/>
		public uint Kind;
		/// <summary/>
		public uint Value;

		/// <summary>Default constructor</summary>
		public RawCustomDebugInformationRow() {
		}

		/// <summary>Constructor</summary>
		public RawCustomDebugInformationRow(uint Parent, uint Kind, uint Value) {
			this.Parent = Parent;
			this.Kind = Kind;
			this.Value = Value;
		}

		/// <inheritdoc/>
		public uint Read(int index) {
			switch (index) {
			case 0: return Parent;
			case 1: return Kind;
			case 2: return Value;
			default: return 0;
			}
		}

		/// <inheritdoc/>
		public void Write(int index, uint value) {
			switch (index) {
			case 0: Parent = value; break;
			case 1: Kind = value; break;
			case 2: Value = value; break;
			default: break;
			}
		}
	}
}
