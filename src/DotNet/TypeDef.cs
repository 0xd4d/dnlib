// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Threading;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.DotNet.Emit;
using dnlib.Threading;
using dnlib.DotNet.Pdb;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the TypeDef table
	/// </summary>
	public abstract class TypeDef : ITypeDefOrRef, IHasCustomAttribute, IHasDeclSecurity, IMemberRefParent, ITypeOrMethodDef, IHasCustomDebugInformation, IListListener<FieldDef>, IListListener<MethodDef>, IListListener<TypeDef>, IListListener<EventDef>, IListListener<PropertyDef>, IListListener<GenericParam>, IMemberRefResolver, IMemberDef {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.TypeDef, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
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
		int IGenericParameterProvider.NumberOfGenericParameters {
			get { return GenericParameters.Count; }
		}

		/// <inheritdoc/>
		string IType.TypeName {
			get { return FullNameCreator.Name(this, false, null); }
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get { return FullNameCreator.Name(this, true, null); }
		}

		/// <inheritdoc/>
		string IType.Namespace {
			get { return FullNameCreator.Namespace(this, false, null); }
		}

		/// <inheritdoc/>
		public string ReflectionNamespace {
			get { return FullNameCreator.Namespace(this, true, null); }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return FullNameCreator.FullName(this, false, null, null); }
		}

		/// <inheritdoc/>
		public string ReflectionFullName {
			get { return FullNameCreator.FullName(this, true, null, null); }
		}

		/// <inheritdoc/>
		public string AssemblyQualifiedName {
			get { return FullNameCreator.AssemblyQualifiedName(this, null, null); }
		}

		/// <inheritdoc/>
		public IAssembly DefinitionAssembly {
			get { return FullNameCreator.DefinitionAssembly(this); }
		}

		/// <inheritdoc/>
		public IScope Scope {
			get { return Module; }
		}

		/// <inheritdoc/>
		public ITypeDefOrRef ScopeType {
			get { return this; }
		}

		/// <summary>
		/// Always returns <c>false</c> since a <see cref="TypeDef"/> does not contain any
		/// <see cref="GenericVar"/> or <see cref="GenericMVar"/>.
		/// </summary>
		public bool ContainsGenericParameter {
			get { return false; }
		}

		/// <inheritdoc/>
		public ModuleDef Module {
			get { return FullNameCreator.OwnerModule(this); }
		}

		/// <summary>
		/// Gets/sets the owner module
		/// </summary>
		internal ModuleDef Module2 {
			get {
				if (!module2_isInitialized)
					InitializeModule2();
				return module2;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				module2 = value;
				module2_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected ModuleDef module2;
		/// <summary/>
		protected bool module2_isInitialized;

		void InitializeModule2() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (module2_isInitialized)
				return;
			module2 = GetModule2_NoLock();
			module2_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="module2"/></summary>
		protected virtual ModuleDef GetModule2_NoLock() {
			return null;
		}

		bool IIsTypeOrMethod.IsType {
			get { return true; }
		}

		bool IIsTypeOrMethod.IsMethod {
			get { return false; }
		}

		bool IMemberRef.IsField {
			get { return false; }
		}

		bool IMemberRef.IsTypeSpec {
			get { return false; }
		}

		bool IMemberRef.IsTypeRef {
			get { return false; }
		}

		bool IMemberRef.IsTypeDef {
			get { return true; }
		}

		bool IMemberRef.IsMethodSpec {
			get { return false; }
		}

		bool IMemberRef.IsMethodDef {
			get { return false; }
		}

		bool IMemberRef.IsMemberRef {
			get { return false; }
		}

		bool IMemberRef.IsFieldDef {
			get { return false; }
		}

		bool IMemberRef.IsPropertyDef {
			get { return false; }
		}

		bool IMemberRef.IsEventDef {
			get { return false; }
		}

		bool IMemberRef.IsGenericParam {
			get { return false; }
		}

		/// <summary>
		/// From column TypeDef.Flags
		/// </summary>
		public TypeAttributes Attributes {
			get { return (TypeAttributes)attributes; }
			set { attributes = (int)value; }
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column TypeDef.Name
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value; }
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column TypeDef.Namespace
		/// </summary>
		public UTF8String Namespace {
			get { return @namespace; }
			set { @namespace = value; }
		}
		/// <summary>Name</summary>
		protected UTF8String @namespace;

		/// <summary>
		/// From column TypeDef.Extends
		/// </summary>
		public ITypeDefOrRef BaseType {
			get {
				if (!baseType_isInitialized)
					InitializeBaseType();
				return baseType;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				baseType = value;
				baseType_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected ITypeDefOrRef baseType;
		/// <summary/>
		protected bool baseType_isInitialized;

		void InitializeBaseType() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (baseType_isInitialized)
				return;
			baseType = GetBaseType_NoLock();
			baseType_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="baseType"/></summary>
		protected virtual ITypeDefOrRef GetBaseType_NoLock() {
			return null;
		}

		/// <summary>Reset <see cref="BaseType"/></summary>
		protected void ResetBaseType() {
			baseType_isInitialized = false;
		}

		/// <summary>
		/// From column TypeDef.FieldList
		/// </summary>
		public ThreadSafe.IList<FieldDef> Fields {
			get {
				if (fields == null)
					InitializeFields();
				return fields;
			}
		}
		/// <summary/>
		protected LazyList<FieldDef> fields;
		/// <summary>Initializes <see cref="fields"/></summary>
		protected virtual void InitializeFields() {
			Interlocked.CompareExchange(ref fields, new LazyList<FieldDef>(this), null);
		}

		/// <summary>
		/// From column TypeDef.MethodList
		/// </summary>
		public ThreadSafe.IList<MethodDef> Methods {
			get {
				if (methods == null)
					InitializeMethods();
				return methods;
			}
		}
		/// <summary/>
		protected LazyList<MethodDef> methods;
		/// <summary>Initializes <see cref="methods"/></summary>
		protected virtual void InitializeMethods() {
			Interlocked.CompareExchange(ref methods, new LazyList<MethodDef>(this), null);
		}

		/// <inheritdoc/>
		public ThreadSafe.IList<GenericParam> GenericParameters {
			get {
				if (genericParameters == null)
					InitializeGenericParameters();
				return genericParameters;
			}
		}
		/// <summary/>
		protected LazyList<GenericParam> genericParameters;
		/// <summary>Initializes <see cref="genericParameters"/></summary>
		protected virtual void InitializeGenericParameters() {
			Interlocked.CompareExchange(ref genericParameters, new LazyList<GenericParam>(this), null);
		}

		/// <summary>
		/// Gets the interfaces
		/// </summary>
		public ThreadSafe.IList<InterfaceImpl> Interfaces {
			get {
				if (interfaces == null)
					InitializeInterfaces();
				return interfaces;
			}
		}
		/// <summary/>
		protected ThreadSafe.IList<InterfaceImpl> interfaces;
		/// <summary>Initializes <see cref="interfaces"/></summary>
		protected virtual void InitializeInterfaces() {
			Interlocked.CompareExchange(ref interfaces, ThreadSafeListCreator.Create<InterfaceImpl>(), null);
		}

		/// <inheritdoc/>
		public ThreadSafe.IList<DeclSecurity> DeclSecurities {
			get {
				if (declSecurities == null)
					InitializeDeclSecurities();
				return declSecurities;
			}
		}
		/// <summary/>
		protected ThreadSafe.IList<DeclSecurity> declSecurities;
		/// <summary>Initializes <see cref="declSecurities"/></summary>
		protected virtual void InitializeDeclSecurities() {
			Interlocked.CompareExchange(ref declSecurities, ThreadSafeListCreator.Create<DeclSecurity>(), null);
		}

		/// <summary>
		/// Gets/sets the class layout
		/// </summary>
		public ClassLayout ClassLayout {
			get {
				if (!classLayout_isInitialized)
					InitializeClassLayout();
				return classLayout;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				classLayout = value;
				classLayout_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected ClassLayout classLayout;
		/// <summary/>
		protected bool classLayout_isInitialized;

		void InitializeClassLayout() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (classLayout_isInitialized)
				return;
			classLayout = GetClassLayout_NoLock();
			classLayout_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}
		ClassLayout GetOrCreateClassLayout() {
			var cl = ClassLayout;
			if (cl != null)
				return cl;
			Interlocked.CompareExchange(ref classLayout, new ClassLayoutUser(0, 0), null);
			return classLayout;
		}

		/// <summary>Called to initialize <see cref="classLayout"/></summary>
		protected virtual ClassLayout GetClassLayout_NoLock() {
			return null;
		}

		/// <inheritdoc/>
		public bool HasDeclSecurities {
			get { return DeclSecurities.Count > 0; }
		}

		/// <summary>
		/// Gets/sets the enclosing type. It's <c>null</c> if this isn't a nested class.
		/// </summary>
		public TypeDef DeclaringType {
			get {
				if (!declaringType2_isInitialized)
					InitializeDeclaringType2();
				return declaringType2;
			}
			set {
				var currentDeclaringType = DeclaringType2;
				if (currentDeclaringType == value)
					return;
				if (currentDeclaringType != null)
					currentDeclaringType.NestedTypes.Remove(this);	// Will set DeclaringType2 = null
				if (value != null)
					value.NestedTypes.Add(this);		// Will set DeclaringType2 = value

				// Make sure this is clear. Will be set whenever it's inserted into ModulDef.Types
				Module2 = null;
			}
		}

		/// <inheritdoc/>
		ITypeDefOrRef IMemberRef.DeclaringType {
			get { return DeclaringType; }
		}

		/// <summary>
		/// Called by <see cref="DeclaringType"/> and should normally not be called by any user
		/// code. Use <see cref="DeclaringType"/> instead. Only call this if you must set the
		/// declaring type without inserting it in the declaring type's method list.
		/// </summary>
		public TypeDef DeclaringType2 {
			get {
				if (!declaringType2_isInitialized)
					InitializeDeclaringType2();
				return declaringType2;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				declaringType2 = value;
				declaringType2_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected TypeDef declaringType2;
		/// <summary/>
		protected bool declaringType2_isInitialized;

		void InitializeDeclaringType2() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (declaringType2_isInitialized)
				return;
			declaringType2 = GetDeclaringType2_NoLock();
			declaringType2_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="declaringType2"/></summary>
		protected virtual TypeDef GetDeclaringType2_NoLock() {
			return null;
		}

		/// <summary>
		/// Gets all the nested types
		/// </summary>
		public ThreadSafe.IList<TypeDef> NestedTypes {
			get {
				if (nestedTypes == null)
					InitializeNestedTypes();
				return nestedTypes;
			}
		}
		/// <summary/>
		protected LazyList<TypeDef> nestedTypes;
		/// <summary>Initializes <see cref="nestedTypes"/></summary>
		protected virtual void InitializeNestedTypes() {
			Interlocked.CompareExchange(ref nestedTypes, new LazyList<TypeDef>(this), null);
		}

		/// <summary>
		/// Gets all events
		/// </summary>
		public ThreadSafe.IList<EventDef> Events {
			get {
				if (events == null)
					InitializeEvents();
				return events;
			}
		}
		/// <summary/>
		protected LazyList<EventDef> events;
		/// <summary>Initializes <see cref="events"/></summary>
		protected virtual void InitializeEvents() {
			Interlocked.CompareExchange(ref events, new LazyList<EventDef>(this), null);
		}

		/// <summary>
		/// Gets all properties
		/// </summary>
		public ThreadSafe.IList<PropertyDef> Properties {
			get {
				if (properties == null)
					InitializeProperties();
				return properties;
			}
		}
		/// <summary/>
		protected LazyList<PropertyDef> properties;
		/// <summary>Initializes <see cref="properties"/></summary>
		protected virtual void InitializeProperties() {
			Interlocked.CompareExchange(ref properties, new LazyList<PropertyDef>(this), null);
		}

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributes == null)
					InitializeCustomAttributes();
				return customAttributes;
			}
		}
		/// <summary/>
		protected CustomAttributeCollection customAttributes;
		/// <summary>Initializes <see cref="customAttributes"/></summary>
		protected virtual void InitializeCustomAttributes() {
			Interlocked.CompareExchange(ref customAttributes, new CustomAttributeCollection(), null);
		}

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <inheritdoc/>
		public int HasCustomDebugInformationTag {
			get { return 3; }
		}

		/// <inheritdoc/>
		public bool HasCustomDebugInfos {
			get { return CustomDebugInfos.Count > 0; }
		}

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public ThreadSafe.IList<PdbCustomDebugInfo> CustomDebugInfos {
			get {
				if (customDebugInfos == null)
					InitializeCustomDebugInfos();
				return customDebugInfos;
			}
		}
		/// <summary/>
		protected ThreadSafe.IList<PdbCustomDebugInfo> customDebugInfos;
		/// <summary>Initializes <see cref="customDebugInfos"/></summary>
		protected virtual void InitializeCustomDebugInfos() {
			Interlocked.CompareExchange(ref customDebugInfos, ThreadSafeListCreator.Create<PdbCustomDebugInfo>(), null);
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="FieldDef"/> in <see cref="Fields"/>
		/// </summary>
		public bool HasFields {
			get { return Fields.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="MethodDef"/> in <see cref="Methods"/>
		/// </summary>
		public bool HasMethods {
			get { return Methods.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="GenericParam"/> in <see cref="GenericParameters"/>
		/// </summary>
		public bool HasGenericParameters {
			get { return GenericParameters.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="EventDef"/> in <see cref="Events"/>
		/// </summary>
		public bool HasEvents {
			get { return Events.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="PropertyDef"/> in <see cref="Properties"/>
		/// </summary>
		public bool HasProperties {
			get { return Properties.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="TypeDef"/> in <see cref="NestedTypes"/>
		/// </summary>
		public bool HasNestedTypes {
			get { return NestedTypes.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="InterfaceImpl"/> in <see cref="Interfaces"/>
		/// </summary>
		public bool HasInterfaces {
			get { return Interfaces.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="ClassLayout"/> is not <c>null</c>
		/// </summary>
		public bool HasClassLayout {
			get { return ClassLayout != null; }
		}

		/// <summary>
		/// Gets/sets the packing size. If you write to this property but <see cref="ClassLayout"/>
		/// is <c>null</c>, it will be created. The value <see cref="ushort.MaxValue"/> is returned
		/// if <see cref="ClassLayout"/> is <c>null</c>.
		/// </summary>
		public ushort PackingSize {
			get {
				var cl = ClassLayout;
				return cl == null ? ushort.MaxValue : cl.PackingSize;
			}
			set {
				var cl = GetOrCreateClassLayout();
				cl.PackingSize = value;
			}
		}

		/// <summary>
		/// Gets/sets the class size. If you write to this property but <see cref="ClassLayout"/>
		/// is <c>null</c>, it will be created. The value <see cref="uint.MaxValue"/> is returned
		/// if <see cref="ClassLayout"/> is <c>null</c>.
		/// </summary>
		public uint ClassSize {
			get {
				var cl = ClassLayout;
				return cl == null ? uint.MaxValue : cl.ClassSize;
			}
			set {
				var cl = GetOrCreateClassLayout();
				cl.ClassSize = value;
			}
		}

		/// <inheritdoc/>
		public bool IsValueType {
			get {
				// Don't include abstract since value types can be abstract without throwing at runtime
				if ((Attributes & (TypeAttributes.Sealed | TypeAttributes.ClassSemanticsMask)) != (TypeAttributes.Sealed | TypeAttributes.Class))
					return false;
				var baseType = BaseType;
				if (baseType == null)
					return false;
				if (!baseType.DefinitionAssembly.IsCorLib())
					return false;

				// PERF: Don't allocate a System.String by calling FullName etc.
				UTF8String baseName, baseNamespace;
				var baseTr = baseType as TypeRef;
				if (baseTr != null) {
					baseName = baseTr.Name;
					baseNamespace = baseTr.Namespace;
				}
				else {
					var baseTd = baseType as TypeDef;
					if (baseTd == null)
						return false;
					baseName = baseTd.Name;
					baseNamespace = baseTd.Namespace;
				}

				if (baseNamespace != systemString)
					return false;
				if (baseName != valueTypeString && baseName != enumString)
					return false;

				if (!DefinitionAssembly.IsCorLib())
					return true;
				return !(Name == enumString && Namespace == systemString);
			}
		}
		static readonly UTF8String systemString = new UTF8String("System");
		static readonly UTF8String enumString = new UTF8String("Enum");
		static readonly UTF8String valueTypeString = new UTF8String("ValueType");
		static readonly UTF8String multicastDelegateString = new UTF8String("MulticastDelegate");

		/// <summary>
		/// <c>true</c> if it's an enum
		/// </summary>
		public bool IsEnum {
			get {
				// Don't include abstract since value types can be abstract without throwing at runtime
				if ((Attributes & (TypeAttributes.Sealed | TypeAttributes.ClassSemanticsMask)) != (TypeAttributes.Sealed | TypeAttributes.Class))
					return false;
				var baseType = BaseType;
				if (baseType == null)
					return false;
				if (!baseType.DefinitionAssembly.IsCorLib())
					return false;

				// PERF: Don't allocate a System.String by calling FullName etc.
				var baseTr = baseType as TypeRef;
				if (baseTr != null)
					return baseTr.Namespace == systemString && baseTr.Name == enumString;
				var baseTd = baseType as TypeDef;
				if (baseTd != null)
					return baseTd.Namespace == systemString && baseTd.Name == enumString;
				return false;
			}
		}

		/// <summary>
		/// <c>true</c> if it's a delegate (it derives from <see cref="System.MulticastDelegate"/>)
		/// </summary>
		public bool IsDelegate {
			get {
				if ((Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.ClassSemanticsMask)) != (TypeAttributes.Sealed | TypeAttributes.Class))
					return false;
				var baseType = BaseType;
				if (baseType == null)
					return false;
				if (!baseType.DefinitionAssembly.IsCorLib())
					return false;

				// PERF: Don't allocate a System.String by calling FullName etc.
				var baseTr = baseType as TypeRef;
				if (baseTr != null)
					return baseTr.Namespace == systemString && baseTr.Name == multicastDelegateString;
				var baseTd = baseType as TypeDef;
				if (baseTd != null)
					return baseTd.Namespace == systemString && baseTd.Name == multicastDelegateString;
				return false;
			}
		}

		/// <summary>
		/// <c>true</c> if this is a nested type (it has a declaring type)
		/// </summary>
		public bool IsNested {
			get { return DeclaringType != null; }
		}

		/// <inheritdoc/>
		public bool IsPrimitive {
			get { return this.IsPrimitive(); }
		}

		/// <summary>
		/// Modify <see cref="attributes"/> property: <see cref="attributes"/> =
		/// (<see cref="attributes"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(TypeAttributes andMask, TypeAttributes orMask) {
#if THREAD_SAFE
			int origVal, newVal;
			do {
				origVal = attributes;
				newVal = (origVal & (int)andMask) | (int)orMask;
			} while (Interlocked.CompareExchange(ref attributes, newVal, origVal) != origVal);
#else
			attributes = (attributes & (int)andMask) | (int)orMask;
#endif
		}

		/// <summary>
		/// Set or clear flags in <see cref="attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, TypeAttributes flags) {
#if THREAD_SAFE
			int origVal, newVal;
			do {
				origVal = attributes;
				if (set)
					newVal = origVal | (int)flags;
				else
					newVal = origVal & ~(int)flags;
			} while (Interlocked.CompareExchange(ref attributes, newVal, origVal) != origVal);
#else
			if (set)
				attributes |= (int)flags;
			else
				attributes &= ~(int)flags;
#endif
		}

		/// <summary>
		/// Gets/sets the visibility
		/// </summary>
		public TypeAttributes Visibility {
			get { return (TypeAttributes)attributes & TypeAttributes.VisibilityMask; }
			set { ModifyAttributes(~TypeAttributes.VisibilityMask, value & TypeAttributes.VisibilityMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NotPublic"/> is set
		/// </summary>
		public bool IsNotPublic {
			get { return ((TypeAttributes)attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.Public"/> is set
		/// </summary>
		public bool IsPublic {
			get { return ((TypeAttributes)attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedPublic"/> is set
		/// </summary>
		public bool IsNestedPublic {
			get { return ((TypeAttributes)attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedPrivate"/> is set
		/// </summary>
		public bool IsNestedPrivate {
			get { return ((TypeAttributes)attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedFamily"/> is set
		/// </summary>
		public bool IsNestedFamily {
			get { return ((TypeAttributes)attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedAssembly"/> is set
		/// </summary>
		public bool IsNestedAssembly {
			get { return ((TypeAttributes)attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedFamANDAssem"/> is set
		/// </summary>
		public bool IsNestedFamilyAndAssembly {
			get { return ((TypeAttributes)attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedFamORAssem"/> is set
		/// </summary>
		public bool IsNestedFamilyOrAssembly {
			get { return ((TypeAttributes)attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem; }
		}

		/// <summary>
		/// Gets/sets the layout
		/// </summary>
		public TypeAttributes Layout {
			get { return (TypeAttributes)attributes & TypeAttributes.LayoutMask; }
			set { ModifyAttributes(~TypeAttributes.LayoutMask, value & TypeAttributes.LayoutMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.AutoLayout"/> is set
		/// </summary>
		public bool IsAutoLayout {
			get { return ((TypeAttributes)attributes & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.SequentialLayout"/> is set
		/// </summary>
		public bool IsSequentialLayout {
			get { return ((TypeAttributes)attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.ExplicitLayout"/> is set
		/// </summary>
		public bool IsExplicitLayout {
			get { return ((TypeAttributes)attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout; }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Interface"/> bit
		/// </summary>
		public bool IsInterface {
			get { return ((TypeAttributes)attributes & TypeAttributes.Interface) != 0; }
			set { ModifyAttributes(value, TypeAttributes.Interface); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Class"/> bit
		/// </summary>
		public bool IsClass {
			get { return ((TypeAttributes)attributes & TypeAttributes.Interface) == 0; }
			set { ModifyAttributes(!value, TypeAttributes.Interface); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Abstract"/> bit
		/// </summary>
		public bool IsAbstract {
			get { return ((TypeAttributes)attributes & TypeAttributes.Abstract) != 0; }
			set { ModifyAttributes(value, TypeAttributes.Abstract); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Sealed"/> bit
		/// </summary>
		public bool IsSealed {
			get { return ((TypeAttributes)attributes & TypeAttributes.Sealed) != 0; }
			set { ModifyAttributes(value, TypeAttributes.Sealed); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get { return ((TypeAttributes)attributes & TypeAttributes.SpecialName) != 0; }
			set { ModifyAttributes(value, TypeAttributes.SpecialName); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Import"/> bit
		/// </summary>
		public bool IsImport {
			get { return ((TypeAttributes)attributes & TypeAttributes.Import) != 0; }
			set { ModifyAttributes(value, TypeAttributes.Import); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Serializable"/> bit
		/// </summary>
		public bool IsSerializable {
			get { return ((TypeAttributes)attributes & TypeAttributes.Serializable) != 0; }
			set { ModifyAttributes(value, TypeAttributes.Serializable); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.WindowsRuntime"/> bit
		/// </summary>
		public bool IsWindowsRuntime {
			get { return ((TypeAttributes)attributes & TypeAttributes.WindowsRuntime) != 0; }
			set { ModifyAttributes(value, TypeAttributes.WindowsRuntime); }
		}

		/// <summary>
		/// Gets/sets the string format
		/// </summary>
		public TypeAttributes StringFormat {
			get { return (TypeAttributes)attributes & TypeAttributes.StringFormatMask; }
			set { ModifyAttributes(~TypeAttributes.StringFormatMask, value & TypeAttributes.StringFormatMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.AnsiClass"/> is set
		/// </summary>
		public bool IsAnsiClass {
			get { return ((TypeAttributes)attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.UnicodeClass"/> is set
		/// </summary>
		public bool IsUnicodeClass {
			get { return ((TypeAttributes)attributes & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.AutoClass"/> is set
		/// </summary>
		public bool IsAutoClass {
			get { return ((TypeAttributes)attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.CustomFormatClass"/> is set
		/// </summary>
		public bool IsCustomFormatClass {
			get { return ((TypeAttributes)attributes & TypeAttributes.StringFormatMask) == TypeAttributes.CustomFormatClass; }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.BeforeFieldInit"/> bit
		/// </summary>
		public bool IsBeforeFieldInit {
			get { return ((TypeAttributes)attributes & TypeAttributes.BeforeFieldInit) != 0; }
			set { ModifyAttributes(value, TypeAttributes.BeforeFieldInit); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Forwarder"/> bit
		/// </summary>
		public bool IsForwarder {
			get { return ((TypeAttributes)attributes & TypeAttributes.Forwarder) != 0; }
			set { ModifyAttributes(value, TypeAttributes.Forwarder); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRuntimeSpecialName {
			get { return ((TypeAttributes)attributes & TypeAttributes.RTSpecialName) != 0; }
			set { ModifyAttributes(value, TypeAttributes.RTSpecialName); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.HasSecurity"/> bit
		/// </summary>
		public bool HasSecurity {
			get { return ((TypeAttributes)attributes & TypeAttributes.HasSecurity) != 0; }
			set { ModifyAttributes(value, TypeAttributes.HasSecurity); }
		}

		/// <summary>
		/// <c>true</c> if this is the global (aka. &lt;Module&gt;) type
		/// </summary>
		public bool IsGlobalModuleType {
			get {
				var mod = Module;
				return mod != null && mod.GlobalType == this;
			}
		}

		/// <summary>
		/// Gets a list of all nested types and all their nested types
		/// </summary>
		public IEnumerable<TypeDef> GetTypes() {
			return AllTypesHelper.Types(NestedTypes);
		}

		/// <summary>
		/// Gets an enum's underlying type or <c>null</c> if none. Should only be called
		/// if this is an enum.
		/// </summary>
		public TypeSig GetEnumUnderlyingType() {
			foreach (var field in Fields.GetSafeEnumerable()) {
				if (!field.IsLiteral && !field.IsStatic) {
					var fieldSig = field.FieldSig;
					if (fieldSig != null)
						return fieldSig.Type;
				}
			}
			return null;
		}

		/// <summary>
		/// Resolves a method or a field. <see cref="MemberRef.Class"/> (owner type) is ignored when
		/// resolving the method/field. Private scope methods/fields are not returned.
		/// </summary>
		/// <param name="memberRef">A method/field reference</param>
		/// <returns>A <see cref="MethodDef"/> or a <see cref="FieldDef"/> instance or <c>null</c>
		/// if it couldn't be resolved.</returns>
		public IMemberForwarded Resolve(MemberRef memberRef) {
			return Resolve(memberRef, 0);
		}

		/// <summary>
		/// Resolves a method or a field. <see cref="MemberRef.Class"/> (owner type) is ignored when
		/// resolving the method/field.
		/// </summary>
		/// <param name="memberRef">A method/field reference</param>
		/// <param name="options">Method/field signature comparison options</param>
		/// <returns>A <see cref="MethodDef"/> or a <see cref="FieldDef"/> instance or <c>null</c>
		/// if it couldn't be resolved.</returns>
		public IMemberForwarded Resolve(MemberRef memberRef, SigComparerOptions options) {
			if (memberRef == null)
				return null;

			var methodSig = memberRef.MethodSig;
			if (methodSig != null)
				return FindMethodCheckBaseType(memberRef.Name, methodSig, options, memberRef.Module);

			var fieldSig = memberRef.FieldSig;
			if (fieldSig != null)
				return FindFieldCheckBaseType(memberRef.Name, fieldSig, options, memberRef.Module);

			return null;
		}

		/// <summary>
		/// Finds a method. Private scope methods are not returned.
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="sig">Method signature</param>
		/// <returns>The first method that matches or <c>null</c> if none found</returns>
		public MethodDef FindMethod(UTF8String name, MethodSig sig) {
			return FindMethod(name, sig, 0, null);
		}

		/// <summary>
		/// Finds a method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="sig">Method signature</param>
		/// <param name="options">Method signature comparison options</param>
		/// <returns>The first method that matches or <c>null</c> if none found</returns>
		public MethodDef FindMethod(UTF8String name, MethodSig sig, SigComparerOptions options) {
			return FindMethod(name, sig, options, null);
		}

		/// <summary>
		/// Finds a method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="sig">Method signature</param>
		/// <param name="options">Method signature comparison options</param>
		/// <param name="sourceModule">The module that needs to find the method or <c>null</c></param>
		/// <returns>The first method that matches or <c>null</c> if none found</returns>
		public MethodDef FindMethod(UTF8String name, MethodSig sig, SigComparerOptions options, ModuleDef sourceModule) {
			if (UTF8String.IsNull(name) || sig == null)
				return null;
			var comparer = new SigComparer(options, sourceModule);
			bool allowPrivateScope = (options & SigComparerOptions.PrivateScopeMethodIsComparable) != 0;
			foreach (var method in Methods.GetSafeEnumerable()) {
				if (!allowPrivateScope && method.IsPrivateScope)
					continue;
				if (!UTF8String.Equals(method.Name, name))
					continue;
				if (comparer.Equals(method.MethodSig, sig))
					return method;
			}
			return null;
		}

		/// <summary>
		/// Finds a method by name
		/// </summary>
		/// <param name="name">Name of method</param>
		/// <returns>The <see cref="MethodDef"/> or <c>null</c> if not found</returns>
		public MethodDef FindMethod(UTF8String name) {
			foreach (var method in Methods.GetSafeEnumerable()) {
				if (UTF8String.Equals(method.Name, name))
					return method;
			}
			return null;
		}

		/// <summary>
		/// Finds all methods by name
		/// </summary>
		/// <param name="name">Name of method</param>
		/// <returns>All methods with that name</returns>
		public IEnumerable<MethodDef> FindMethods(UTF8String name) {
			foreach (var method in Methods.GetSafeEnumerable()) {
				if (UTF8String.Equals(method.Name, name))
					yield return method;
			}
		}

		/// <summary>
		/// Finds the class constructor (aka type initializer). It's the method named .cctor
		/// </summary>
		/// <returns>The class constructor or <c>null</c> if none found</returns>
		public MethodDef FindStaticConstructor() {
			return Methods.ExecuteLocked<MethodDef, object, MethodDef>(null, (tsList, arg) => FindStaticConstructor_NoMethodsLock());
		}

		MethodDef FindStaticConstructor_NoMethodsLock() {
			foreach (var method in Methods.GetEnumerable_NoLock()) {
				if (method.IsStaticConstructor)
					return method;
			}
			return null;
		}

		/// <summary>
		/// Finds the class constructor (aka type initializer). It's the method named .cctor.
		/// If it doesn't exist, it is created, inserted into <see cref="Methods"/> and returned.
		/// The created .cctor will have just one RET instruction.
		/// </summary>
		/// <returns>The class constructor</returns>
		public MethodDef FindOrCreateStaticConstructor() {
			var cctor = FindStaticConstructor();
			if (cctor != null)
				return cctor;

			var implFlags = MethodImplAttributes.IL | MethodImplAttributes.Managed;
			var flags = MethodAttributes.Private | MethodAttributes.Static |
						MethodAttributes.HideBySig | MethodAttributes.ReuseSlot |
						MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
			var module = Module;
			cctor = module.UpdateRowId(new MethodDefUser(MethodDef.StaticConstructorName,
						MethodSig.CreateStatic(module.CorLibTypes.Void), implFlags, flags));
			var body = new CilBody();
			body.InitLocals = true;
			body.MaxStack = 8;
			body.Instructions.Add(OpCodes.Ret.ToInstruction());
			cctor.Body = body;
			return Methods.ExecuteLocked<MethodDef, MethodDef, MethodDef>(cctor, (tsList, cctor2) => {
				var cctor3 = FindStaticConstructor_NoMethodsLock();
				if (cctor3 != null)
					return cctor3;
				tsList.Add_NoLock(cctor2);
				return cctor2;
			});
		}

		/// <summary>
		/// Finds all instance constructors (not class constructors)
		/// </summary>
		/// <returns>All instance constructors</returns>
		public IEnumerable<MethodDef> FindInstanceConstructors() {
			foreach (var method in Methods.GetSafeEnumerable()) {
				if (method.IsInstanceConstructor)
					yield return method;
			}
		}

		/// <summary>
		/// Finds all static and instance constructors
		/// </summary>
		/// <returns>All static and instance constructors</returns>
		public IEnumerable<MethodDef> FindConstructors() {
			foreach (var method in Methods.GetSafeEnumerable()) {
				if (method.IsConstructor)
					yield return method;
			}
		}

		/// <summary>
		/// Finds the default instance constructor (the one with no arguments)
		/// </summary>
		/// <returns>The default instance constructor or <c>null</c> if none</returns>
		public MethodDef FindDefaultConstructor() {
			foreach (var method in Methods.GetSafeEnumerable()) {
				if (!method.IsInstanceConstructor)
					continue;
				var sig = method.MethodSig;
				if (sig != null && sig.Params.Count == 0)
					return method;
			}
			return null;
		}

		/// <summary>
		/// Finds a field. Private scope fields are not returned.
		/// </summary>
		/// <param name="name">Field name</param>
		/// <param name="sig">Field signature</param>
		/// <returns>The first field that matches or <c>null</c> if none found</returns>
		public FieldDef FindField(UTF8String name, FieldSig sig) {
			return FindField(name, sig, 0, null);
		}

		/// <summary>
		/// Finds a field
		/// </summary>
		/// <param name="name">Field name</param>
		/// <param name="sig">Field signature</param>
		/// <param name="options">Field signature comparison options</param>
		/// <returns>The first field that matches or <c>null</c> if none found</returns>
		public FieldDef FindField(UTF8String name, FieldSig sig, SigComparerOptions options) {
			return FindField(name, sig, options, null);
		}

		/// <summary>
		/// Finds a field
		/// </summary>
		/// <param name="name">Field name</param>
		/// <param name="sig">Field signature</param>
		/// <param name="options">Field signature comparison options</param>
		/// <param name="sourceModule">The module that needs to find the field or <c>null</c></param>
		/// <returns>The first field that matches or <c>null</c> if none found</returns>
		public FieldDef FindField(UTF8String name, FieldSig sig, SigComparerOptions options, ModuleDef sourceModule) {
			if (UTF8String.IsNull(name) || sig == null)
				return null;
			var comparer = new SigComparer(options, sourceModule);
			bool allowPrivateScope = (options & SigComparerOptions.PrivateScopeFieldIsComparable) != 0;
			foreach (var field in Fields.GetSafeEnumerable()) {
				if (!allowPrivateScope && field.IsPrivateScope)
					continue;
				if (!UTF8String.Equals(field.Name, name))
					continue;
				if (comparer.Equals(field.FieldSig, sig))
					return field;
			}
			return null;
		}

		/// <summary>
		/// Finds a field by name
		/// </summary>
		/// <param name="name">Name of field</param>
		/// <returns>The <see cref="FieldDef"/> or <c>null</c> if not found</returns>
		public FieldDef FindField(UTF8String name) {
			foreach (var field in Fields.GetSafeEnumerable()) {
				if (UTF8String.Equals(field.Name, name))
					return field;
			}
			return null;
		}

		/// <summary>
		/// Finds all fields by name
		/// </summary>
		/// <param name="name">Name of field</param>
		/// <returns>All fields with that name</returns>
		public IEnumerable<FieldDef> FindFields(UTF8String name) {
			foreach (var field in Fields.GetSafeEnumerable()) {
				if (UTF8String.Equals(field.Name, name))
					yield return field;
			}
		}

		/// <summary>
		/// Finds an event
		/// </summary>
		/// <param name="name">Name of event</param>
		/// <param name="type">Type of event</param>
		/// <returns>A <see cref="EventDef"/> or <c>null</c> if not found</returns>
		public EventDef FindEvent(UTF8String name, IType type) {
			return FindEvent(name, type, 0, null);
		}

		/// <summary>
		/// Finds an event
		/// </summary>
		/// <param name="name">Name of event</param>
		/// <param name="type">Type of event</param>
		/// <param name="options">Event type comparison options</param>
		/// <returns>A <see cref="EventDef"/> or <c>null</c> if not found</returns>
		public EventDef FindEvent(UTF8String name, IType type, SigComparerOptions options) {
			return FindEvent(name, type, options, null);
		}

		/// <summary>
		/// Finds an event
		/// </summary>
		/// <param name="name">Name of event</param>
		/// <param name="type">Type of event</param>
		/// <param name="options">Event type comparison options</param>
		/// <param name="sourceModule">The module that needs to find the event or <c>null</c></param>
		/// <returns>A <see cref="EventDef"/> or <c>null</c> if not found</returns>
		public EventDef FindEvent(UTF8String name, IType type, SigComparerOptions options, ModuleDef sourceModule) {
			if (UTF8String.IsNull(name) || type == null)
				return null;
			var comparer = new SigComparer(options, sourceModule);
			foreach (var @event in Events.GetSafeEnumerable()) {
				if (!UTF8String.Equals(@event.Name, name))
					continue;
				if (comparer.Equals(@event.EventType, type))
					return @event;
			}
			return null;
		}

		/// <summary>
		/// Finds an event by name
		/// </summary>
		/// <param name="name">Name of event</param>
		/// <returns>The <see cref="EventDef"/> or <c>null</c> if not found</returns>
		public EventDef FindEvent(UTF8String name) {
			foreach (var @event in Events.GetSafeEnumerable()) {
				if (UTF8String.Equals(@event.Name, name))
					return @event;
			}
			return null;
		}

		/// <summary>
		/// Finds all events by name
		/// </summary>
		/// <param name="name">Name of event</param>
		/// <returns>All events with that name</returns>
		public IEnumerable<EventDef> FindEvents(UTF8String name) {
			foreach (var @event in Events.GetSafeEnumerable()) {
				if (UTF8String.Equals(@event.Name, name))
					yield return @event;
			}
		}

		/// <summary>
		/// Finds a property
		/// </summary>
		/// <param name="name">Name of property</param>
		/// <param name="propSig">Property signature</param>
		/// <returns>A <see cref="PropertyDef"/> or <c>null</c> if not found</returns>
		public PropertyDef FindProperty(UTF8String name, CallingConventionSig propSig) {
			return FindProperty(name, propSig, 0, null);
		}

		/// <summary>
		/// Finds a property
		/// </summary>
		/// <param name="name">Name of property</param>
		/// <param name="propSig">Property signature</param>
		/// <param name="options">Property signature comparison options</param>
		/// <returns>A <see cref="PropertyDef"/> or <c>null</c> if not found</returns>
		public PropertyDef FindProperty(UTF8String name, CallingConventionSig propSig, SigComparerOptions options) {
			return FindProperty(name, propSig, options, null);
		}

		/// <summary>
		/// Finds a property
		/// </summary>
		/// <param name="name">Name of property</param>
		/// <param name="propSig">Property signature</param>
		/// <param name="options">Property signature comparison options</param>
		/// <param name="sourceModule">The module that needs to find the property or <c>null</c></param>
		/// <returns>A <see cref="PropertyDef"/> or <c>null</c> if not found</returns>
		public PropertyDef FindProperty(UTF8String name, CallingConventionSig propSig, SigComparerOptions options, ModuleDef sourceModule) {
			if (UTF8String.IsNull(name) || propSig == null)
				return null;
			var comparer = new SigComparer(options, sourceModule);
			foreach (var prop in Properties.GetSafeEnumerable()) {
				if (!UTF8String.Equals(prop.Name, name))
					continue;
				if (comparer.Equals(prop.Type, propSig))
					return prop;
			}
			return null;
		}

		/// <summary>
		/// Finds a prop by name
		/// </summary>
		/// <param name="name">Name of prop</param>
		/// <returns>The <see cref="PropertyDef"/> or <c>null</c> if not found</returns>
		public PropertyDef FindProperty(UTF8String name) {
			foreach (var prop in Properties.GetSafeEnumerable()) {
				if (UTF8String.Equals(prop.Name, name))
					return prop;
			}
			return null;
		}

		/// <summary>
		/// Finds all props by name
		/// </summary>
		/// <param name="name">Name of prop</param>
		/// <returns>All props with that name</returns>
		public IEnumerable<PropertyDef> FindProperties(UTF8String name) {
			foreach (var prop in Properties.GetSafeEnumerable()) {
				if (UTF8String.Equals(prop.Name, name))
					yield return prop;
			}
		}

		/// <summary>
		/// Finds a method by checking this type or any of its base types
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="sig">Method signature</param>
		/// <returns>The method or <c>null</c> if it wasn't found</returns>
		public MethodDef FindMethodCheckBaseType(UTF8String name, MethodSig sig) {
			return FindMethodCheckBaseType(name, sig, 0, null);
		}

		/// <summary>
		/// Finds a method by checking this type or any of its base types
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="sig">Method signature</param>
		/// <param name="options">Method signature comparison options</param>
		/// <returns>The method or <c>null</c> if it wasn't found</returns>
		public MethodDef FindMethodCheckBaseType(UTF8String name, MethodSig sig, SigComparerOptions options) {
			return FindMethodCheckBaseType(name, sig, options, null);
		}

		/// <summary>
		/// Finds a method by checking this type or any of its base types
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="sig">Method signature</param>
		/// <param name="options">Method signature comparison options</param>
		/// <param name="sourceModule">The module that needs to find the method or <c>null</c></param>
		/// <returns>The method or <c>null</c> if it wasn't found</returns>
		public MethodDef FindMethodCheckBaseType(UTF8String name, MethodSig sig, SigComparerOptions options, ModuleDef sourceModule) {
			var td = this;
			while (td != null) {
				var md = td.FindMethod(name, sig, options, sourceModule);
				if (md != null)
					return md;
				td = td.BaseType.ResolveTypeDef();
			}
			return null;
		}

		/// <summary>
		/// Finds a method by checking this type or any of its base types
		/// </summary>
		/// <param name="name">Method name</param>
		/// <returns>The method or <c>null</c> if it wasn't found</returns>
		public MethodDef FindMethodCheckBaseType(UTF8String name) {
			var td = this;
			while (td != null) {
				var md = td.FindMethod(name);
				if (md != null)
					return md;
				td = td.BaseType.ResolveTypeDef();
			}
			return null;
		}

		/// <summary>
		/// Finds a field by checking this type or any of its base types
		/// </summary>
		/// <param name="name">Field name</param>
		/// <param name="sig">Field signature</param>
		/// <returns>The field or <c>null</c> if it wasn't found</returns>
		public FieldDef FindFieldCheckBaseType(UTF8String name, FieldSig sig) {
			return FindFieldCheckBaseType(name, sig, 0, null);
		}

		/// <summary>
		/// Finds a field by checking this type or any of its base types
		/// </summary>
		/// <param name="name">Field name</param>
		/// <param name="sig">Field signature</param>
		/// <param name="options">Field signature comparison options</param>
		/// <returns>The field or <c>null</c> if it wasn't found</returns>
		public FieldDef FindFieldCheckBaseType(UTF8String name, FieldSig sig, SigComparerOptions options) {
			return FindFieldCheckBaseType(name, sig, options, null);
		}

		/// <summary>
		/// Finds a field by checking this type or any of its base types
		/// </summary>
		/// <param name="name">Field name</param>
		/// <param name="sig">Field signature</param>
		/// <param name="options">Field signature comparison options</param>
		/// <param name="sourceModule">The module that needs to find the field or <c>null</c></param>
		/// <returns>The field or <c>null</c> if it wasn't found</returns>
		public FieldDef FindFieldCheckBaseType(UTF8String name, FieldSig sig, SigComparerOptions options, ModuleDef sourceModule) {
			var td = this;
			while (td != null) {
				var fd = td.FindField(name, sig, options, sourceModule);
				if (fd != null)
					return fd;
				td = td.BaseType.ResolveTypeDef();
			}
			return null;
		}

		/// <summary>
		/// Finds a field by checking this type or any of its base types
		/// </summary>
		/// <param name="name">Field name</param>
		/// <returns>The field or <c>null</c> if it wasn't found</returns>
		public FieldDef FindFieldCheckBaseType(UTF8String name) {
			var td = this;
			while (td != null) {
				var fd = td.FindField(name);
				if (fd != null)
					return fd;
				td = td.BaseType.ResolveTypeDef();
			}
			return null;
		}

		/// <summary>
		/// Finds an event by checking this type or any of its base types
		/// </summary>
		/// <param name="name">Event name</param>
		/// <param name="eventType">Event type</param>
		/// <returns>The event or <c>null</c> if it wasn't found</returns>
		public EventDef FindEventCheckBaseType(UTF8String name, ITypeDefOrRef eventType) {
			var td = this;
			while (td != null) {
				var ed = td.FindEvent(name, eventType);
				if (ed != null)
					return ed;
				td = td.BaseType.ResolveTypeDef();
			}
			return null;
		}

		/// <summary>
		/// Finds an event by checking this type or any of its base types
		/// </summary>
		/// <param name="name">Event name</param>
		/// <returns>The event or <c>null</c> if it wasn't found</returns>
		public EventDef FindEventCheckBaseType(UTF8String name) {
			var td = this;
			while (td != null) {
				var ed = td.FindEvent(name);
				if (ed != null)
					return ed;
				td = td.BaseType.ResolveTypeDef();
			}
			return null;
		}

		/// <summary>
		/// Finds a property by checking this type or any of its base types
		/// </summary>
		/// <param name="name">Property name</param>
		/// <param name="sig">Property signature</param>
		/// <returns>The property or <c>null</c> if it wasn't found</returns>
		public PropertyDef FindPropertyCheckBaseType(UTF8String name, FieldSig sig) {
			var td = this;
			while (td != null) {
				var pd = td.FindProperty(name, sig);
				if (pd != null)
					return pd;
				td = td.BaseType.ResolveTypeDef();
			}
			return null;
		}

		/// <summary>
		/// Finds a property by checking this type or any of its base types
		/// </summary>
		/// <param name="name">Property name</param>
		/// <returns>The property or <c>null</c> if it wasn't found</returns>
		public PropertyDef FindPropertyCheckBaseType(UTF8String name) {
			var td = this;
			while (td != null) {
				var pd = td.FindProperty(name);
				if (pd != null)
					return pd;
				td = td.BaseType.ResolveTypeDef();
			}
			return null;
		}

		/// <summary>
		/// Removes a method from this type. It also removes it from any properties and events.
		/// </summary>
		/// <param name="method">The method to remove</param>
		public void Remove(MethodDef method) {
			Remove(method, false);
		}

		/// <summary>
		/// Removes a method from this type. It also removes it from any properties and events.
		/// </summary>
		/// <param name="method">The method to remove</param>
		/// <param name="removeEmptyPropertiesEvents"><c>true</c> if we should remove all
		/// empty properties and events.</param>
		public void Remove(MethodDef method, bool removeEmptyPropertiesEvents) {
			if (method == null)
				return;

			foreach (var prop in Properties.GetSafeEnumerable()) {
				prop.GetMethods.Remove(method);
				prop.SetMethods.Remove(method);
				prop.OtherMethods.Remove(method);
			}

			foreach (var evt in Events.GetSafeEnumerable()) {
				if (evt.AddMethod == method)
					evt.AddMethod = null;
				if (evt.RemoveMethod == method)
					evt.RemoveMethod = null;
				if (evt.InvokeMethod == method)
					evt.InvokeMethod = null;
				evt.OtherMethods.Remove(method);
			}

			if (removeEmptyPropertiesEvents) {
				RemoveEmptyProperties();
				RemoveEmptyEvents();
			}

			Methods.Remove(method);
		}

		void RemoveEmptyProperties() {
			Properties.IterateAllReverse((tsList, index, value) => {
				if (value.IsEmpty)
					tsList.RemoveAt_NoLock(index);
			});
		}

		void RemoveEmptyEvents() {
			Events.IterateAllReverse((tsList, index, value) => {
				if (value.IsEmpty)
					tsList.RemoveAt_NoLock(index);
			});
		}

		/// <inheritdoc/>
		void IListListener<FieldDef>.OnLazyAdd(int index, ref FieldDef value) {
			OnLazyAdd2(index, ref value);
		}

		internal virtual void OnLazyAdd2(int index, ref FieldDef value) {
#if DEBUG
			if (value.DeclaringType != this)
				throw new InvalidOperationException("Added field's DeclaringType != this");
#endif
		}

		/// <inheritdoc/>
		void IListListener<FieldDef>.OnAdd(int index, FieldDef value) {
			if (value.DeclaringType != null)
				throw new InvalidOperationException("Field is already owned by another type. Set DeclaringType to null first.");
			value.DeclaringType2 = this;
		}

		/// <inheritdoc/>
		void IListListener<FieldDef>.OnRemove(int index, FieldDef value) {
			value.DeclaringType2 = null;
		}

		/// <inheritdoc/>
		void IListListener<FieldDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<FieldDef>.OnClear() {
			foreach (var field in Fields.GetEnumerable_NoLock())
				field.DeclaringType2 = null;
		}

		/// <inheritdoc/>
		void IListListener<MethodDef>.OnLazyAdd(int index, ref MethodDef value) {
			OnLazyAdd2(index, ref value);
		}

		internal virtual void OnLazyAdd2(int index, ref MethodDef value) {
#if DEBUG
			if (value.DeclaringType != this)
				throw new InvalidOperationException("Added method's DeclaringType != this");
#endif
		}

		/// <inheritdoc/>
		void IListListener<MethodDef>.OnAdd(int index, MethodDef value) {
			if (value.DeclaringType != null)
				throw new InvalidOperationException("Method is already owned by another type. Set DeclaringType to null first.");
			value.DeclaringType2 = this;
			value.Parameters.UpdateThisParameterType(this);
		}

		/// <inheritdoc/>
		void IListListener<MethodDef>.OnRemove(int index, MethodDef value) {
			value.DeclaringType2 = null;
			value.Parameters.UpdateThisParameterType(null);
		}

		/// <inheritdoc/>
		void IListListener<MethodDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<MethodDef>.OnClear() {
			foreach (var method in Methods.GetEnumerable_NoLock()) {
				method.DeclaringType2 = null;
				method.Parameters.UpdateThisParameterType(null);
			}
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnLazyAdd(int index, ref TypeDef value) {
#if DEBUG
			if (value.Module2 != null)
				throw new InvalidOperationException("Added nested type's Module != null");
			if (value.DeclaringType != this)
				throw new InvalidOperationException("Added nested type's DeclaringType != this");
#endif
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnAdd(int index, TypeDef value) {
			if (value.DeclaringType != null)
				throw new InvalidOperationException("Nested type is already owned by another type. Set DeclaringType to null first.");
			if (value.Module != null)
				throw new InvalidOperationException("Type is already owned by another module. Remove it from that module's type list.");
			value.DeclaringType2 = this;
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnRemove(int index, TypeDef value) {
			value.DeclaringType2 = null;
			value.Module2 = null;
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnClear() {
			foreach (var type in NestedTypes.GetEnumerable_NoLock())
				type.DeclaringType2 = null;
		}

		/// <inheritdoc/>
		void IListListener<EventDef>.OnLazyAdd(int index, ref EventDef value) {
			OnLazyAdd2(index, ref value);
		}

		internal virtual void OnLazyAdd2(int index, ref EventDef value) {
#if DEBUG
			if (value.DeclaringType != this)
				throw new InvalidOperationException("Added event's DeclaringType != this");
#endif
		}

		/// <inheritdoc/>
		void IListListener<EventDef>.OnAdd(int index, EventDef value) {
			if (value.DeclaringType != null)
				throw new InvalidOperationException("Event is already owned by another type. Set DeclaringType to null first.");
			value.DeclaringType2 = this;
		}

		/// <inheritdoc/>
		void IListListener<EventDef>.OnRemove(int index, EventDef value) {
			value.DeclaringType2 = null;
		}

		/// <inheritdoc/>
		void IListListener<EventDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<EventDef>.OnClear() {
			foreach (var @event in Events.GetEnumerable_NoLock())
				@event.DeclaringType2 = null;
		}

		/// <inheritdoc/>
		void IListListener<PropertyDef>.OnLazyAdd(int index, ref PropertyDef value) {
			OnLazyAdd2(index, ref value);
		}

		internal virtual void OnLazyAdd2(int index, ref PropertyDef value) {
#if DEBUG
			if (value.DeclaringType != this)
				throw new InvalidOperationException("Added property's DeclaringType != this");
#endif
		}

		/// <inheritdoc/>
		void IListListener<PropertyDef>.OnAdd(int index, PropertyDef value) {
			if (value.DeclaringType != null)
				throw new InvalidOperationException("Property is already owned by another type. Set DeclaringType to null first.");
			value.DeclaringType2 = this;
		}

		/// <inheritdoc/>
		void IListListener<PropertyDef>.OnRemove(int index, PropertyDef value) {
			value.DeclaringType2 = null;
		}

		/// <inheritdoc/>
		void IListListener<PropertyDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<PropertyDef>.OnClear() {
			foreach (var prop in Properties.GetEnumerable_NoLock())
				prop.DeclaringType2 = null;
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnLazyAdd(int index, ref GenericParam value) {
			OnLazyAdd2(index, ref value);
		}

		internal virtual void OnLazyAdd2(int index, ref GenericParam value) {
#if DEBUG
			if (value.Owner != this)
				throw new InvalidOperationException("Added generic param's Owner != this");
#endif
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnAdd(int index, GenericParam value) {
			if (value.Owner != null)
				throw new InvalidOperationException("Generic param is already owned by another type/method. Set Owner to null first.");
			value.Owner = this;
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnRemove(int index, GenericParam value) {
			value.Owner = null;
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnClear() {
			foreach (var gp in GenericParameters.GetEnumerable_NoLock())
				gp.Owner = null;
		}

		/// <summary>
		/// Gets all fields named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Field name</param>
		/// <returns>A list of 0 or more fields with name <paramref name="name"/></returns>
		public IList<FieldDef> GetFields(UTF8String name) {
			var fields = new List<FieldDef>();
			foreach (var field in Fields.GetSafeEnumerable()) {
				if (field.Name == name)
					fields.Add(field);
			}
			return fields;
		}

		/// <summary>
		/// Gets the first field named <paramref name="name"/>
		/// </summary>
		/// <param name="name">Field name</param>
		/// <returns>The field or <c>null</c> if none found</returns>
		public FieldDef GetField(UTF8String name) {
			foreach (var field in Fields.GetSafeEnumerable()) {
				if (field.Name == name)
					return field;
			}
			return null;
		}

		internal static bool GetClassSize(TypeDef td, out uint size) {
			size = 0;
			if (td == null)
				return false;
			if (!td.IsValueType)
				return false;	// Not supported by us
			if (!td.IsSequentialLayout && !td.IsExplicitLayout) {
				if (td.Fields.Count != 1)
					return false;
				var fd = td.Fields.Get(0, null);
				if (fd == null)
					return false;
				return fd.GetFieldSize(out size);
			}

			var classLayout = td.ClassLayout;
			if (classLayout == null)
				return false;
			uint classSize = classLayout.ClassSize;
			if (classSize != 0) {
				size = classSize;
				return true;
			}

			// Not supported by us
			return false;
		}

		/// <summary>
		/// FInd a method implementation method
		/// </summary>
		/// <param name="mdr">Method</param>
		/// <returns></returns>
		protected MethodDef FindMethodImplMethod(IMethodDefOrRef mdr) {
			// Check common case first
			var md = mdr as MethodDef;
			if (md != null)
				return md;

			// Must be a member ref
			var mr = mdr as MemberRef;
			if (mr == null)
				return null;

			// If Class is MethodDef, then it should be a vararg method
			var parent = mr.Class;
			md = parent as MethodDef;
			if (md != null)
				return md;

			// If it's a TypeSpec, it must be a generic instance type
			for (int i = 0; i < 10; i++) {
				var ts = parent as TypeSpec;
				if (ts == null)
					break;

				var gis = ts.TypeSig as GenericInstSig;
				if (gis == null || gis.GenericType == null)
					return null;
				parent = gis.GenericType.TypeDefOrRef;
			}

			var td = parent as TypeDef;
			if (td == null) {
				// If it's a TypeRef, resolve it as if it is a reference to a type in the
				// current module, even if its ResolutionScope happens to be some other
				// assembly/module (that's what the CLR does)
				var tr = parent as TypeRef;
				if (tr != null && Module != null)
					td = Module.Find(tr);
			}
			if (td == null)
				return null;
			return td.FindMethod(mr.Name, mr.MethodSig);
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
		/// <param name="baseType">Base class or <c>null</c> if it's an interface</param>
		public TypeDefUser(UTF8String name, ITypeDefOrRef baseType)
			: this(null, name, baseType) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="namespace">Namespace</param>
		/// <param name="name">Name</param>
		/// <param name="baseType">Base class or <c>null</c> if it's an interface</param>
		public TypeDefUser(UTF8String @namespace, UTF8String name, ITypeDefOrRef baseType) {
			this.fields = new LazyList<FieldDef>(this);
			this.methods = new LazyList<MethodDef>(this);
			this.genericParameters = new LazyList<GenericParam>(this);
			this.nestedTypes = new LazyList<TypeDef>(this);
			this.events = new LazyList<EventDef>(this);
			this.properties = new LazyList<PropertyDef>(this);
			this.@namespace = @namespace;
			this.name = name;
			this.baseType = baseType;
			this.baseType_isInitialized = true;
		}
	}

	/// <summary>
	/// Created from a row in the TypeDef table
	/// </summary>
	sealed class TypeDefMD : TypeDef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;
		readonly uint extendsCodedToken;
		Dictionary<uint, ThreadSafe.IList<MethodOverrideTokens>> methodRidToOverrides;

		/// <inheritdoc/>
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override ITypeDefOrRef GetBaseType_NoLock() {
			return readerModule.ResolveTypeDefOrRef(extendsCodedToken, new GenericParamContext(this));
		}

		/// <inheritdoc/>
		protected override void InitializeFields() {
			var list = readerModule.MetaData.GetFieldRidList(origRid);
			var tmp = new LazyList<FieldDef>((int)list.Length, this, list, (list2, index) => readerModule.ResolveField(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref fields, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeMethods() {
			var list = readerModule.MetaData.GetMethodRidList(origRid);
			var tmp = new LazyList<MethodDef>((int)list.Length, this, list, (list2, index) => readerModule.ResolveMethod(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref methods, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeGenericParameters() {
			var list = readerModule.MetaData.GetGenericParamRidList(Table.TypeDef, origRid);
			var tmp = new LazyList<GenericParam>((int)list.Length, this, list, (list2, index) => readerModule.ResolveGenericParam(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref genericParameters, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeInterfaces() {
			var list = readerModule.MetaData.GetInterfaceImplRidList(origRid);
			var tmp = new LazyList<InterfaceImpl>((int)list.Length, list, (list2, index) => readerModule.ResolveInterfaceImpl(((RidList)list2)[index], new GenericParamContext(this)));
			Interlocked.CompareExchange(ref interfaces, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeDeclSecurities() {
			var list = readerModule.MetaData.GetDeclSecurityRidList(Table.TypeDef, origRid);
			var tmp = new LazyList<DeclSecurity>((int)list.Length, list, (list2, index) => readerModule.ResolveDeclSecurity(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref declSecurities, tmp, null);
		}

		/// <inheritdoc/>
		protected override ClassLayout GetClassLayout_NoLock() {
			return readerModule.ResolveClassLayout(readerModule.MetaData.GetClassLayoutRid(origRid));
		}

		/// <inheritdoc/>
		protected override TypeDef GetDeclaringType2_NoLock() {
			uint enclosingClass = readerModule.TablesStream.ReadNestedClassRow2(readerModule.MetaData.GetNestedClassRid(origRid));
			return enclosingClass == 0 ? null : readerModule.ResolveTypeDef(enclosingClass);
		}

		TypeDef DeclaringType2_NoLock {
			get {
				if (!declaringType2_isInitialized) {
					declaringType2 = GetDeclaringType2_NoLock();
					declaringType2_isInitialized = true;
				}
				return declaringType2;
			}
		}

		/// <inheritdoc/>
		protected override void InitializeEvents() {
			var mapRid = readerModule.MetaData.GetEventMapRid(origRid);
			var list = readerModule.MetaData.GetEventRidList(mapRid);
			var tmp = new LazyList<EventDef>((int)list.Length, this, list, (list2, index) => readerModule.ResolveEvent(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref events, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeProperties() {
			var mapRid = readerModule.MetaData.GetPropertyMapRid(origRid);
			var list = readerModule.MetaData.GetPropertyRidList(mapRid);
			var tmp = new LazyList<PropertyDef>((int)list.Length, this, list, (list2, index) => readerModule.ResolveProperty(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref properties, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeNestedTypes() {
			var list = readerModule.MetaData.GetNestedClassRidList(origRid);
			var tmp = new LazyList<TypeDef>((int)list.Length, this, list, (list2, index) => readerModule.ResolveTypeDef(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref nestedTypes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.TypeDef, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = ThreadSafeListCreator.Create<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), new GenericParamContext(this), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <inheritdoc/>
		protected override ModuleDef GetModule2_NoLock() {
			return DeclaringType2_NoLock != null ? null : readerModule;
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
			if (readerModule.TablesStream.TypeDefTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("TypeDef rid {0} does not exist", rid));
#endif
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			uint name, @namespace;
			extendsCodedToken = readerModule.TablesStream.ReadTypeDefRow(origRid, out this.attributes, out name, out @namespace);
			this.name = readerModule.StringsStream.ReadNoNull(name);
			this.@namespace = readerModule.StringsStream.ReadNoNull(@namespace);
		}

		/// <summary>
		/// Gets all methods <paramref name="method"/> overrides
		/// </summary>
		/// <param name="method">The method</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A list (possibly empty) of all methods <paramref name="method"/> overrides</returns>
		internal ThreadSafe.IList<MethodOverride> GetMethodOverrides(MethodDefMD method, GenericParamContext gpContext) {
			if (method == null)
				return ThreadSafeListCreator.Create<MethodOverride>();

			if (methodRidToOverrides == null)
				InitializeMethodOverrides();

			ThreadSafe.IList<MethodOverrideTokens> overrides;
			if (methodRidToOverrides.TryGetValue(method.OrigRid, out overrides)) {
				var newList = ThreadSafeListCreator.Create<MethodOverride>(overrides.Count);

				for (int i = 0; i < overrides.Count; i++) {
					var ovr = overrides[i];
					var newMethodBody = (IMethodDefOrRef)readerModule.ResolveToken(ovr.MethodBodyToken, gpContext);
					var newMethodDeclaration = (IMethodDefOrRef)readerModule.ResolveToken(ovr.MethodDeclarationToken, gpContext);
					newList.Add(new MethodOverride(newMethodBody, newMethodDeclaration));
				}
				return newList;
			}
			return ThreadSafeListCreator.Create<MethodOverride>();
		}

		struct MethodOverrideTokens {
			public readonly uint MethodBodyToken;
			public readonly uint MethodDeclarationToken;

			public MethodOverrideTokens(uint methodBodyToken, uint methodDeclarationToken) {
				this.MethodBodyToken = methodBodyToken;
				this.MethodDeclarationToken = methodDeclarationToken;
			}
		}

		void InitializeMethodOverrides() {
			var newMethodRidToOverrides = new Dictionary<uint, ThreadSafe.IList<MethodOverrideTokens>>();

			var ridList = readerModule.MetaData.GetMethodImplRidList(origRid);
			for (uint i = 0; i < ridList.Length; i++) {
				uint methodBodyToken;
				uint methodDeclToken = readerModule.TablesStream.ReadMethodImplRow(ridList[i], out methodBodyToken);

				var methodBody = readerModule.ResolveMethodDefOrRef(methodBodyToken);
				var methodDecl = readerModule.ResolveMethodDefOrRef(methodDeclToken);
				if (methodBody == null || methodDecl == null)
					continue;	// Should only happen if some obfuscator added invalid metadata

				// Find the real method. This is usually methodBody since it's usually a
				// MethodDef. The CLR only allows method bodies in the current type, and
				// so shall we.
				var method = FindMethodImplMethod(methodBody);
				if (method == null || method.DeclaringType != this)
					continue;

				ThreadSafe.IList<MethodOverrideTokens> overrides;
				uint rid = method.Rid;
				if (!newMethodRidToOverrides.TryGetValue(rid, out overrides))
					newMethodRidToOverrides[rid] = overrides = ThreadSafeListCreator.Create<MethodOverrideTokens>();
				overrides.Add(new MethodOverrideTokens(methodBody.MDToken.Raw, methodDecl.MDToken.Raw));
			}
			Interlocked.CompareExchange(ref methodRidToOverrides, newMethodRidToOverrides, null);
		}

		/// <summary>
		/// Initializes all <see cref="MethodDef.semAttrs"/>. Only those <see cref="MethodDef"/>s
		/// that are property or event handlers get updated.
		/// </summary>
		internal void InitializeMethodSemanticsAttributes() {
			var mapRid = readerModule.MetaData.GetPropertyMapRid(origRid);
			var list = readerModule.MetaData.GetPropertyRidList(mapRid);
			for (uint i = 0; i < list.Length; i++) {
				var ridList = readerModule.MetaData.GetMethodSemanticsRidList(Table.Property, list[i]);
				for (uint j = 0; j < ridList.Length; j++) {
					MethodSemanticsAttributes semantics;
					uint methodToken = readerModule.TablesStream.ReadMethodSemanticsRow(ridList[j], out semantics);
					var method = readerModule.ResolveMethod(methodToken);
					if (method == null)
						continue;

					Interlocked.CompareExchange(ref method.semAttrs, (int)semantics | MethodDef.SEMATTRS_INITD, 0);
				}
			}

			mapRid = readerModule.MetaData.GetEventMapRid(origRid);
			list = readerModule.MetaData.GetEventRidList(mapRid);
			for (uint i = 0; i < list.Length; i++) {
				var ridList = readerModule.MetaData.GetMethodSemanticsRidList(Table.Event, list[i]);
				for (uint j = 0; j < ridList.Length; j++) {
					MethodSemanticsAttributes semantics;
					uint methodToken = readerModule.TablesStream.ReadMethodSemanticsRow(ridList[j], out semantics);
					var method = readerModule.ResolveMethod(methodToken);
					if (method == null)
						continue;

					Interlocked.CompareExchange(ref method.semAttrs, (int)semantics | MethodDef.SEMATTRS_INITD, 0);
				}
			}
		}

		/// <summary>
		/// Initializes a property's special methods
		/// </summary>
		/// <param name="prop">The property</param>
		/// <param name="getMethods">Updated with a list of all get methods</param>
		/// <param name="setMethods">Updated with a list of all set methods</param>
		/// <param name="otherMethods">Updated with a list of all other methods</param>
		internal void InitializeProperty(PropertyDefMD prop, out ThreadSafe.IList<MethodDef> getMethods, out ThreadSafe.IList<MethodDef> setMethods, out ThreadSafe.IList<MethodDef> otherMethods) {
			getMethods = ThreadSafeListCreator.Create<MethodDef>();
			setMethods = ThreadSafeListCreator.Create<MethodDef>();
			otherMethods = ThreadSafeListCreator.Create<MethodDef>();
			if (prop == null)
				return;

			var ridList = readerModule.MetaData.GetMethodSemanticsRidList(Table.Property, prop.OrigRid);
			for (uint i = 0; i < ridList.Length; i++) {
				MethodSemanticsAttributes semantics;
				uint methodToken = readerModule.TablesStream.ReadMethodSemanticsRow(ridList[i], out semantics);
				var method = readerModule.ResolveMethod(methodToken);
				if (method == null || method.DeclaringType != prop.DeclaringType)
					continue;

				// It's documented to be flags, but ignore those with more than one bit set
				switch (semantics) {
				case MethodSemanticsAttributes.Setter:
					if (!setMethods.Contains(method))
						setMethods.Add(method);
					break;

				case MethodSemanticsAttributes.Getter:
					if (!getMethods.Contains(method))
						getMethods.Add(method);
					break;

				case MethodSemanticsAttributes.Other:
					if (!otherMethods.Contains(method))
						otherMethods.Add(method);
					break;

				default:
					// Ignore anything else
					break;
				}
			}
		}

		/// <summary>
		/// Initializes an event's special methods
		/// </summary>
		/// <param name="evt">The event</param>
		/// <param name="addMethod">Updated with the addOn method or <c>null</c> if none</param>
		/// <param name="invokeMethod">Updated with the fire method or <c>null</c> if none</param>
		/// <param name="removeMethod">Updated with the removeOn method or <c>null</c> if none</param>
		/// <param name="otherMethods">Updated with a list of all other methods</param>
		internal void InitializeEvent(EventDefMD evt, out MethodDef addMethod, out MethodDef invokeMethod, out MethodDef removeMethod, out ThreadSafe.IList<MethodDef> otherMethods) {
			addMethod = null;
			invokeMethod = null;
			removeMethod = null;
			otherMethods = ThreadSafeListCreator.Create<MethodDef>();
			if (evt == null)
				return;

			var ridList = readerModule.MetaData.GetMethodSemanticsRidList(Table.Event, evt.OrigRid);
			for (uint i = 0; i < ridList.Length; i++) {
				MethodSemanticsAttributes semantics;
				uint methodToken = readerModule.TablesStream.ReadMethodSemanticsRow(ridList[i], out semantics);
				var method = readerModule.ResolveMethod(methodToken);
				if (method == null || method.DeclaringType != evt.DeclaringType)
					continue;

				// It's documented to be flags, but ignore those with more than one bit set
				switch (semantics) {
				case MethodSemanticsAttributes.AddOn:
					if (addMethod == null)
						addMethod = method;
					break;

				case MethodSemanticsAttributes.RemoveOn:
					if (removeMethod == null)
						removeMethod = method;
					break;

				case MethodSemanticsAttributes.Fire:
					if (invokeMethod == null)
						invokeMethod = method;
					break;

				case MethodSemanticsAttributes.Other:
					if (!otherMethods.Contains(method))
						otherMethods.Add(method);
					break;

				default:
					// Ignore anything else
					break;
				}
			}
		}

		/// <inheritdoc/>
		internal override void OnLazyAdd2(int index, ref FieldDef value) {
			if (value.DeclaringType != this) {
				// More than one owner... This module has invalid metadata.
				value = readerModule.ForceUpdateRowId(readerModule.ReadField(value.Rid).InitializeAll());
				value.DeclaringType2 = this;
			}
		}

		/// <inheritdoc/>
		internal override void OnLazyAdd2(int index, ref MethodDef value) {
			if (value.DeclaringType != this) {
				// More than one owner... This module has invalid metadata.
				value = readerModule.ForceUpdateRowId(readerModule.ReadMethod(value.Rid).InitializeAll());
				value.DeclaringType2 = this;
				value.Parameters.UpdateThisParameterType(this);
			}
		}

		/// <inheritdoc/>
		internal override void OnLazyAdd2(int index, ref EventDef value) {
			if (value.DeclaringType != this) {
				// More than one owner... This module has invalid metadata.
				value = readerModule.ForceUpdateRowId(readerModule.ReadEvent(value.Rid).InitializeAll());
				value.DeclaringType2 = this;
			}
		}

		/// <inheritdoc/>
		internal override void OnLazyAdd2(int index, ref PropertyDef value) {
			if (value.DeclaringType != this) {
				// More than one owner... This module has invalid metadata.
				value = readerModule.ForceUpdateRowId(readerModule.ReadProperty(value.Rid).InitializeAll());
				value.DeclaringType2 = this;
			}
		}

		/// <inheritdoc/>
		internal override void OnLazyAdd2(int index, ref GenericParam value) {
			if (value.Owner != this) {
				// More than one owner... This module has invalid metadata.
				value = readerModule.ForceUpdateRowId(readerModule.ReadGenericParam(value.Rid).InitializeAll());
				value.Owner = this;
			}
		}
	}
}
