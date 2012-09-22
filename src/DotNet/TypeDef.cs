using System;
using System.Collections.Generic;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the TypeDef table
	/// </summary>
	public abstract class TypeDef : ITypeDefOrRef, IHasCustomAttribute, IHasDeclSecurity, IMemberRefParent, ITypeOrMethodDef, IListListener<FieldDef>, IListListener<MethodDef>, IListListener<TypeDef> {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// The owner module
		/// </summary>
		protected ModuleDef ownerModule;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.TypeDef, rid); }
		}

		/// <inheritdoc/>
		public int TypeDefOrRefTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 3; }
		}

		/// <inheritdoc/>
		public int HasDeclSecurityTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int TypeOrMethodDefTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		string IType.Name {
			get { return FullNameCreator.Name(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get { return FullNameCreator.Name(this, true); }
		}

		/// <inheritdoc/>
		string IType.Namespace {
			get { return FullNameCreator.Namespace(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionNamespace {
			get { return FullNameCreator.Namespace(this, true); }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return FullNameCreator.FullName(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionFullName {
			get { return FullNameCreator.FullName(this, true); }
		}

		/// <inheritdoc/>
		public string AssemblyQualifiedName {
			get { return FullNameCreator.AssemblyQualifiedName(this); }
		}

		/// <inheritdoc/>
		public IAssembly DefinitionAssembly {
			get { return FullNameCreator.DefinitionAssembly(this); }
		}

		/// <inheritdoc/>
		public ModuleDef OwnerModule {
			get { return FullNameCreator.OwnerModule(this); }
		}

		/// <summary>
		/// Gets/sets the owner module
		/// </summary>
		internal ModuleDef OwnerModule2 {
			get { return ownerModule; }
			set { ownerModule = value; }
		}

		/// <summary>
		/// From column TypeDef.Flags
		/// </summary>
		public abstract TypeAttributes Flags { get; set; }

		/// <summary>
		/// From column TypeDef.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column TypeDef.Namespace
		/// </summary>
		public abstract UTF8String Namespace { get; set; }

		/// <summary>
		/// From column TypeDef.Extends
		/// </summary>
		public abstract ITypeDefOrRef Extends { get; set; }

		/// <summary>
		/// From column TypeDef.FieldList
		/// </summary>
		public abstract IList<FieldDef> Fields { get; }

		/// <summary>
		/// From column TypeDef.MethodList
		/// </summary>
		public abstract IList<MethodDef> Methods { get; }

		/// <inheritdoc/>
		public abstract IList<GenericParam> GenericParams { get; }

		/// <summary>
		/// Gets the interfaces
		/// </summary>
		public abstract IList<InterfaceImpl> InterfaceImpls { get; }

		/// <inheritdoc/>
		public abstract IList<DeclSecurity> DeclSecurities { get; }

		/// <summary>
		/// Gets/sets the class layout
		/// </summary>
		public abstract ClassLayout ClassLayout { get; set; }

		/// <summary>
		/// Gets/sets the enclosing type. It's <c>null</c> if this isn't a nested class.
		/// </summary>
		public TypeDef DeclaringType {
			get { return DeclaringType2; }
			set {
				var currentDeclaringType = DeclaringType2;
				if (currentDeclaringType != null)
					currentDeclaringType.NestedTypes.Remove(this);	// Will set DeclaringType2 = null
				if (value != null)
					value.NestedTypes.Add(this);		// Will set DeclaringType2 = value
			}
		}

		/// <summary>
		/// Called by <see cref="DeclaringType"/>
		/// </summary>
		protected abstract TypeDef DeclaringType2 { get; set; }

		/// <summary>
		/// Gets all the nested types
		/// </summary>
		public abstract IList<TypeDef> NestedTypes { get; }

		/// <summary>
		/// Gets/sets the event map
		/// </summary>
		public abstract EventMap EventMap { get; set; }

		/// <summary>
		/// Gets/sets the property map
		/// </summary>
		public abstract PropertyMap PropertyMap { get; set; }

		/// <summary>
		/// Gets/sets the visibility
		/// </summary>
		public TypeAttributes Visibility {
			get { return Flags & TypeAttributes.VisibilityMask; }
			set { Flags = (Flags & ~TypeAttributes.VisibilityMask) | (value & TypeAttributes.VisibilityMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NotPublic"/> is set
		/// </summary>
		public bool IsNotPublic {
			get { return (Flags & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.Public"/> is set
		/// </summary>
		public bool IsPublic {
			get { return (Flags & TypeAttributes.VisibilityMask) == TypeAttributes.Public; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedPublic"/> is set
		/// </summary>
		public bool IsNestedPublic {
			get { return (Flags & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedPrivate"/> is set
		/// </summary>
		public bool IsNestedPrivate {
			get { return (Flags & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedFamily"/> is set
		/// </summary>
		public bool IsNestedFamily {
			get { return (Flags & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedAssembly"/> is set
		/// </summary>
		public bool IsNestedAssembly {
			get { return (Flags & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedFamANDAssem"/> is set
		/// </summary>
		public bool IsNestedFamANDAssem {
			get { return (Flags & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedFamORAssem"/> is set
		/// </summary>
		public bool IsNestedFamORAssem {
			get { return (Flags & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem; }
		}

		/// <summary>
		/// Gets/sets the layout
		/// </summary>
		public TypeAttributes Layout {
			get { return Flags & TypeAttributes.LayoutMask; }
			set { Flags = (Flags & ~TypeAttributes.LayoutMask) | (value & TypeAttributes.LayoutMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.AutoLayout"/> is set
		/// </summary>
		public bool IsAutoLayout {
			get { return (Flags & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.SequentialLayout"/> is set
		/// </summary>
		public bool IsSequentialLayout {
			get { return (Flags & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.ExplicitLayout"/> is set
		/// </summary>
		public bool IsExplicitLayout {
			get { return (Flags & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout; }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Interface"/> bit
		/// </summary>
		public bool IsInterface {
			get { return (Flags & TypeAttributes.Interface) != 0; }
			set {
				if (value)
					Flags |= TypeAttributes.Interface;
				else
					Flags &= ~TypeAttributes.Interface;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Class"/> bit
		/// </summary>
		public bool IsClass {
			get { return (Flags & TypeAttributes.Interface) == 0; }
			set {
				if (value)
					Flags &= ~TypeAttributes.Interface;
				else
					Flags |= TypeAttributes.Interface;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Abstract"/> bit
		/// </summary>
		public bool IsAbstract {
			get { return (Flags & TypeAttributes.Abstract) != 0; }
			set {
				if (value)
					Flags |= TypeAttributes.Abstract;
				else
					Flags &= ~TypeAttributes.Abstract;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Sealed"/> bit
		/// </summary>
		public bool IsSealed {
			get { return (Flags & TypeAttributes.Sealed) != 0; }
			set {
				if (value)
					Flags |= TypeAttributes.Sealed;
				else
					Flags &= ~TypeAttributes.Sealed;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get { return (Flags & TypeAttributes.SpecialName) != 0; }
			set {
				if (value)
					Flags |= TypeAttributes.SpecialName;
				else
					Flags &= ~TypeAttributes.SpecialName;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Import"/> bit
		/// </summary>
		public bool IsImport {
			get { return (Flags & TypeAttributes.Import) != 0; }
			set {
				if (value)
					Flags |= TypeAttributes.Import;
				else
					Flags &= ~TypeAttributes.Import;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Serializable"/> bit
		/// </summary>
		public bool IsSerializable {
			get { return (Flags & TypeAttributes.Serializable) != 0; }
			set {
				if (value)
					Flags |= TypeAttributes.Serializable;
				else
					Flags &= ~TypeAttributes.Serializable;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.WindowsRuntime"/> bit
		/// </summary>
		public bool IsWindowsRuntime {
			get { return (Flags & TypeAttributes.WindowsRuntime) != 0; }
			set {
				if (value)
					Flags |= TypeAttributes.WindowsRuntime;
				else
					Flags &= ~TypeAttributes.WindowsRuntime;
			}
		}

		/// <summary>
		/// Gets/sets the string format
		/// </summary>
		public TypeAttributes StringFormat {
			get { return Flags & TypeAttributes.StringFormatMask; }
			set { Flags = (Flags & ~TypeAttributes.StringFormatMask) | (value & TypeAttributes.StringFormatMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.AnsiClass"/> is set
		/// </summary>
		public bool IsAnsiClass {
			get { return (Flags & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.UnicodeClass"/> is set
		/// </summary>
		public bool IsUnicodeClass {
			get { return (Flags & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.AutoClass"/> is set
		/// </summary>
		public bool IsAutoClass {
			get { return (Flags & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.CustomFormatClass"/> is set
		/// </summary>
		public bool IsCustomFormatClass {
			get { return (Flags & TypeAttributes.StringFormatMask) == TypeAttributes.CustomFormatClass; }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.BeforeFieldInit"/> bit
		/// </summary>
		public bool IsBeforeFieldInit {
			get { return (Flags & TypeAttributes.BeforeFieldInit) != 0; }
			set {
				if (value)
					Flags |= TypeAttributes.BeforeFieldInit;
				else
					Flags &= ~TypeAttributes.BeforeFieldInit;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Forwarder"/> bit
		/// </summary>
		public bool IsForwarder {
			get { return (Flags & TypeAttributes.Forwarder) != 0; }
			set {
				if (value)
					Flags |= TypeAttributes.Forwarder;
				else
					Flags &= ~TypeAttributes.Forwarder;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRTSpecialName {
			get { return (Flags & TypeAttributes.RTSpecialName) != 0; }
			set {
				if (value)
					Flags |= TypeAttributes.RTSpecialName;
				else
					Flags &= ~TypeAttributes.RTSpecialName;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.HasSecurity"/> bit
		/// </summary>
		public bool HasSecurity {
			get { return (Flags & TypeAttributes.HasSecurity) != 0; }
			set {
				if (value)
					Flags |= TypeAttributes.HasSecurity;
				else
					Flags &= ~TypeAttributes.HasSecurity;
			}
		}

		/// <summary>
		/// <c>true</c> if this is the global (aka. &lt;Module&gt;) type
		/// </summary>
		public bool IsGlobalModuleType {
			get {
				var mod = OwnerModule;
				return mod != null && mod.Types.Count != 0 && mod.Types[0] == this;
			}
		}

		/// <summary>
		/// Gets a list of all nested types and all their nested types
		/// </summary>
		public IEnumerable<TypeDef> GetTypes() {
			return AllTypesHelper.Types(NestedTypes);
		}

		/// <inheritdoc/>
		void IListListener<FieldDef>.OnAdd(int index, FieldDef value, bool isLazyAdd) {
			if (isLazyAdd) {
#if DEBUG
				if (value.DeclaringType != this)
					throw new InvalidOperationException("Added field's DeclaringType != this");
#endif
				return;
			}
			if (value.DeclaringType != null)
				throw new InvalidOperationException("Field is already owned by another type. Set DeclaringType to null first.");
			value.SetDeclaringType(this);
		}

		/// <inheritdoc/>
		void IListListener<FieldDef>.OnRemove(int index, FieldDef value) {
			value.SetDeclaringType(null);
		}

		/// <inheritdoc/>
		void IListListener<FieldDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<FieldDef>.OnClear() {
			foreach (var field in Fields)
				field.SetDeclaringType(null);
		}

		/// <inheritdoc/>
		void IListListener<MethodDef>.OnAdd(int index, MethodDef value, bool isLazyAdd) {
			if (isLazyAdd) {
#if DEBUG
				if (value.DeclaringType != this)
					throw new InvalidOperationException("Added method's DeclaringType != this");
#endif
				return;
			}
			if (value.DeclaringType != null)
				throw new InvalidOperationException("Method is already owned by another type. Set DeclaringType to null first.");
			value.SetDeclaringType(this);
		}

		/// <inheritdoc/>
		void IListListener<MethodDef>.OnRemove(int index, MethodDef value) {
			value.SetDeclaringType(null);
		}

		/// <inheritdoc/>
		void IListListener<MethodDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<MethodDef>.OnClear() {
			foreach (var method in Methods)
				method.SetDeclaringType(null);
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnAdd(int index, TypeDef value, bool isLazyAdd) {
			if (isLazyAdd) {
#if DEBUG
				if (value.OwnerModule2 != null)
					throw new InvalidOperationException("Added nested type's OwnerModule != null");
				if (value.DeclaringType != this)
					throw new InvalidOperationException("Added nested type's DeclaringType != this");
#endif
				return;
			}
			if (value.DeclaringType != null)
				throw new InvalidOperationException("Nested type is already owned by another type. Set DeclaringType to null first.");
			if (value.OwnerModule != null)
				throw new InvalidOperationException("Type is already owned by another module. Remove it from that module's type list.");
			value.DeclaringType2 = this;
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnRemove(int index, TypeDef value) {
			value.DeclaringType2 = null;
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnClear() {
			foreach (var type in NestedTypes)
				type.DeclaringType2 = null;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A TypeDef row created by the user and not present in the original .NET file
	/// </summary>
	public class TypeDefUser : TypeDef {
		TypeAttributes flags;
		UTF8String name;
		UTF8String @namespace;
		ITypeDefOrRef extends;
		LazyList<FieldDef> fields;
		LazyList<MethodDef> methods;
		IList<GenericParam> genericParams = new List<GenericParam>();
		IList<InterfaceImpl> interfaceImpls = new List<InterfaceImpl>();
		IList<DeclSecurity> declSecurities = new List<DeclSecurity>();
		ClassLayout classLayout;
		TypeDef declaringType;
		EventMap eventMap;
		PropertyMap propertyMap;
		LazyList<TypeDef> nestedTypes;

		/// <inheritdoc/>
		public override TypeAttributes Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Namespace {
			get { return @namespace; }
			set { @namespace = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef Extends {
			get { return extends; }
			set { extends = value; }
		}

		/// <inheritdoc/>
		public override IList<FieldDef> Fields {
			get { return fields; }
		}

		/// <inheritdoc/>
		public override IList<MethodDef> Methods {
			get { return methods; }
		}

		/// <inheritdoc/>
		public override IList<GenericParam> GenericParams {
			get { return genericParams; }
		}

		/// <inheritdoc/>
		public override IList<InterfaceImpl> InterfaceImpls {
			get { return interfaceImpls; }
		}

		/// <inheritdoc/>
		public override IList<DeclSecurity> DeclSecurities {
			get { return declSecurities; }
		}

		/// <inheritdoc/>
		public override ClassLayout ClassLayout {
			get { return classLayout; }
			set { classLayout = value; }
		}

		/// <inheritdoc/>
		protected override TypeDef DeclaringType2 {
			get { return declaringType; }
			set { declaringType = value; }
		}

		/// <inheritdoc/>
		public override EventMap EventMap {
			get { return eventMap; }
			set { eventMap = value; }
		}

		/// <inheritdoc/>
		public override PropertyMap PropertyMap {
			get { return propertyMap; }
			set { propertyMap = value; }
		}

		/// <inheritdoc/>
		public override IList<TypeDef> NestedTypes {
			get { return nestedTypes; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public TypeDefUser(UTF8String name)
			: this(null, name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="namespace">Namespace</param>
		/// <param name="name">Name</param>
		public TypeDefUser(UTF8String @namespace, UTF8String name)
			: this(@namespace, name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="extends">Base class or <c>null</c> if it's an interface</param>
		public TypeDefUser(UTF8String name, ITypeDefOrRef extends)
			: this(null, name, extends) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="namespace">Namespace</param>
		/// <param name="name">Name</param>
		/// <param name="extends">Base class or <c>null</c> if it's an interface</param>
		public TypeDefUser(UTF8String @namespace, UTF8String name, ITypeDefOrRef extends) {
			this.fields = new LazyList<FieldDef>(this);
			this.methods = new LazyList<MethodDef>(this);
			this.nestedTypes = new LazyList<TypeDef>(this);
			this.@namespace = @namespace;
			this.name = name;
			this.extends = extends;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public TypeDefUser(string name)
			: this(null, name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="namespace">Namespace</param>
		/// <param name="name">Name</param>
		public TypeDefUser(string @namespace, string name)
			: this(@namespace, name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="extends">Base class or <c>null</c> if it's an interface</param>
		public TypeDefUser(string name, ITypeDefOrRef extends)
			: this(null, name, extends) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="namespace">Namespace</param>
		/// <param name="name">Name</param>
		/// <param name="extends">Base class or <c>null</c> if it's an interface</param>
		public TypeDefUser(string @namespace, string name, ITypeDefOrRef extends)
			: this(new UTF8String(@namespace), new UTF8String(name), extends) {
		}
	}

	/// <summary>
	/// Created from a row in the TypeDef table
	/// </summary>
	sealed class TypeDefMD : TypeDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawTypeDefRow rawRow;

		UserValue<TypeAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<UTF8String> @namespace;
		UserValue<ITypeDefOrRef> extends;
		LazyList<FieldDef> fields;
		LazyList<MethodDef> methods;
		LazyList<GenericParam> genericParams;
		LazyList<InterfaceImpl> interfaceImpls;
		LazyList<DeclSecurity> declSecurities;
		UserValue<ClassLayout> classLayout;
		UserValue<TypeDef> declaringType;
		UserValue<EventMap> eventMap;
		UserValue<PropertyMap> propertyMap;
		LazyList<TypeDef> nestedTypes;

		/// <inheritdoc/>
		public override TypeAttributes Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Namespace {
			get { return @namespace.Value; }
			set { @namespace.Value = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef Extends {
			get { return extends.Value; }
			set { extends.Value = value; }
		}

		/// <inheritdoc/>
		public override IList<FieldDef> Fields {
			get {
				if (fields == null) {
					var list = readerModule.MetaData.GetFieldRidList(rid);
					fields = new LazyList<FieldDef>((int)list.Length, this, list, (list2, index) => readerModule.ResolveField(((RidList)list2)[index]));
				}
				return fields;
			}
		}

		/// <inheritdoc/>
		public override IList<MethodDef> Methods {
			get {
				if (methods == null) {
					var list = readerModule.MetaData.GetMethodRidList(rid);
					methods = new LazyList<MethodDef>((int)list.Length, this, list, (list2, index) => readerModule.ResolveMethod(((RidList)list2)[index]));
				}
				return methods;
			}
		}

		/// <inheritdoc/>
		public override IList<GenericParam> GenericParams {
			get {
				if (genericParams == null) {
					var list = readerModule.MetaData.GetGenericParamRidList(Table.TypeDef, rid);
					genericParams = new LazyList<GenericParam>((int)list.Length, list, (list2, index) => readerModule.ResolveGenericParam(((RidList)list2)[index]));
				}
				return genericParams;
			}
		}

		/// <inheritdoc/>
		public override IList<InterfaceImpl> InterfaceImpls {
			get {
				if (interfaceImpls == null) {
					var list = readerModule.MetaData.GetInterfaceImplRidList(rid);
					interfaceImpls = new LazyList<InterfaceImpl>((int)list.Length, list, (list2, index) => readerModule.ResolveInterfaceImpl(((RidList)list2)[index]));
				}
				return interfaceImpls;
			}
		}

		/// <inheritdoc/>
		public override IList<DeclSecurity> DeclSecurities {
			get {
				if (declSecurities == null) {
					var list = readerModule.MetaData.GetDeclSecurityRidList(Table.TypeDef, rid);
					declSecurities = new LazyList<DeclSecurity>((int)list.Length, list, (list2, index) => readerModule.ResolveDeclSecurity(((RidList)list2)[index]));
				}
				return declSecurities;
			}
		}

		/// <inheritdoc/>
		public override ClassLayout ClassLayout {
			get { return classLayout.Value; }
			set { classLayout.Value = value; }
		}

		/// <inheritdoc/>
		protected override TypeDef DeclaringType2 {
			get { return declaringType.Value; }
			set { declaringType.Value = value; }
		}

		/// <inheritdoc/>
		public override EventMap EventMap {
			get { return eventMap.Value; }
			set { eventMap.Value = value; }
		}

		/// <inheritdoc/>
		public override PropertyMap PropertyMap {
			get { return propertyMap.Value; }
			set { propertyMap.Value = value; }
		}

		/// <inheritdoc/>
		public override IList<TypeDef> NestedTypes {
			get {
				if (nestedTypes == null) {
					var list = readerModule.MetaData.GetNestedClassRidList(rid);
					nestedTypes = new LazyList<TypeDef>((int)list.Length, this, list, (list2, index) => readerModule.ResolveTypeDef(((RidList)list2)[index]));
				}
				return nestedTypes;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>TypeDef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public TypeDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.TypeDef).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("TypeDef rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (TypeAttributes)rawRow.Flags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			@namespace.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Namespace);
			};
			extends.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveTypeDefOrRef(rawRow.Extends);
			};
			classLayout.ReadOriginalValue = () => {
				return readerModule.ResolveClassLayout(readerModule.MetaData.GetClassLayoutRid(rid));
			};
			declaringType.ReadOriginalValue = () => {
				var nestedClass = readerModule.ResolveNestedClass(readerModule.MetaData.GetNestedClassRid(rid));
				return nestedClass == null ? null : nestedClass.EnclosingType;
			};
			eventMap.ReadOriginalValue = () => {
				return readerModule.ResolveEventMap(readerModule.MetaData.GetEventMapRid(rid));
			};
			propertyMap.ReadOriginalValue = () => {
				return readerModule.ResolvePropertyMap(readerModule.MetaData.GetPropertyMapRid(rid));
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadTypeDefRow(rid);
		}
	}
}
