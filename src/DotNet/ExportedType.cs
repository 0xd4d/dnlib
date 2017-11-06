// dnlib: See LICENSE.txt for more info

using System;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the ExportedType table
	/// </summary>
	public abstract class ExportedType : IHasCustomAttribute, IImplementation, IHasCustomDebugInformation, IType {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <summary>
		/// The owner module
		/// </summary>
		protected ModuleDef module;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.ExportedType, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 17; }
		}

		/// <inheritdoc/>
		public int ImplementationTag {
			get { return 2; }
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
			get { return 17; }
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

		/// <inheritdoc/>
		public bool IsValueType {
			get {
				var td = Resolve();
				return td != null && td.IsValueType;
			}
		}

		/// <inheritdoc/>
		public bool IsPrimitive {
			get { return this.IsPrimitive(); }
		}

		/// <inheritdoc/>
		string IType.TypeName {
			get { return FullNameCreator.Name(this, false, null); }
		}

		/// <inheritdoc/>
		public UTF8String Name {
			get { return typeName; }
			set { typeName = value; }
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get { return FullNameCreator.Name(this, true, null); }
		}

		/// <inheritdoc/>
		public string Namespace {
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
			get { return FullNameCreator.Scope(this); }
		}

		/// <inheritdoc/>
		public ITypeDefOrRef ScopeType {
			get { return FullNameCreator.ScopeType(this); }
		}

		/// <summary>
		/// Always returns <c>false</c> since a <see cref="ExportedType"/> does not contain any
		/// <see cref="GenericVar"/> or <see cref="GenericMVar"/>.
		/// </summary>
		public bool ContainsGenericParameter {
			get { return false; }
		}

		/// <inheritdoc/>
		public ModuleDef Module {
			get { return module; }
		}

		/// <inheritdoc/>
		bool IIsTypeOrMethod.IsMethod {
			get { return false; }
		}

		/// <inheritdoc/>
		bool IIsTypeOrMethod.IsType {
			get { return true; }
		}

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get { return 0; }
		}

		/// <summary>
		/// From column ExportedType.Flags
		/// </summary>
		public TypeAttributes Attributes {
			get { return (TypeAttributes)attributes; }
			set { attributes = (int)value; }
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column ExportedType.TypeDefId
		/// </summary>
		public uint TypeDefId {
			get { return typeDefId; }
			set { typeDefId = value; }
		}
		/// <summary/>
		protected uint typeDefId;

		/// <summary>
		/// From column ExportedType.TypeName
		/// </summary>
		public UTF8String TypeName {
			get { return typeName; }
			set { typeName = value; }
		}
		/// <summary/>
		protected UTF8String typeName;

		/// <summary>
		/// From column ExportedType.TypeNamespace
		/// </summary>
		public UTF8String TypeNamespace {
			get { return typeNamespace; }
			set { typeNamespace = value; }
		}
		/// <summary/>
		protected UTF8String typeNamespace;

		/// <summary>
		/// From column ExportedType.Implementation
		/// </summary>
		public IImplementation Implementation {
			get {
				if (!implementation_isInitialized)
					InitializeImplementation();
				return implementation;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				implementation = value;
				implementation_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected IImplementation implementation;
		/// <summary/>
		protected bool implementation_isInitialized;

		void InitializeImplementation() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (implementation_isInitialized)
				return;
			implementation = GetImplementation_NoLock();
			implementation_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="implementation"/></summary>
		protected virtual IImplementation GetImplementation_NoLock() {
			return null;
		}

		/// <summary>
		/// <c>true</c> if it's nested within another <see cref="ExportedType"/>
		/// </summary>
		public bool IsNested {
			get { return DeclaringType != null; }
		}

		/// <summary>
		/// Gets the declaring type, if any
		/// </summary>
		public ExportedType DeclaringType {
			get {
				if (!implementation_isInitialized)
					InitializeImplementation();
				return implementation as ExportedType;
			}
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
		/// Gets/sets the <see cref="TypeAttributes.Forwarder"/> bit. See also <see cref="MovedToAnotherAssembly"/>
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

		const int MAX_LOOP_ITERS = 50;

		/// <summary>
		/// <c>true</c> if this type has been moved to another assembly
		/// </summary>
		public bool MovedToAnotherAssembly {
			get {
				ExportedType et = this;
				for (int i = 0; i < MAX_LOOP_ITERS; i++) {
					var impl = et.Implementation;
					if (impl is AssemblyRef)
						return et.IsForwarder;

					et = impl as ExportedType;
					if (et == null)
						break;
				}
				return false;
			}
		}

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <returns>A <see cref="TypeDef"/> instance or <c>null</c> if it couldn't be resolved</returns>
		public TypeDef Resolve() {
			return Resolve(null);
		}

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <param name="sourceModule">Source module or <c>null</c></param>
		/// <returns>A <see cref="TypeDef"/> instance or <c>null</c> if it couldn't be resolved</returns>
		public TypeDef Resolve(ModuleDef sourceModule) {
			if (module == null)
				return null;

			return Resolve(sourceModule, this);
		}

		static TypeDef Resolve(ModuleDef sourceModule, ExportedType et) {
			for (int i = 0; i < MAX_LOOP_ITERS; i++) {
				if (et == null || et.module == null)
					break;
				var resolver = et.module.Context.AssemblyResolver;
				var etAsm = resolver.Resolve(et.DefinitionAssembly, sourceModule ?? et.module);
				if (etAsm == null)
					break;

				var td = etAsm.Find(et.FullName, false);
				if (td != null)
					return td;

				et = FindExportedType(etAsm, et);
			}

			return null;
		}

		static ExportedType FindExportedType(AssemblyDef asm, ExportedType et) {
			foreach (var mod in asm.Modules.GetSafeEnumerable()) {
				foreach (var et2 in mod.ExportedTypes.GetSafeEnumerable()) {
					if (new SigComparer(SigComparerOptions.DontCompareTypeScope).Equals(et, et2))
						return et2;
				}
			}
			return null;
		}

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <returns>A <see cref="TypeDef"/> instance</returns>
		/// <exception cref="TypeResolveException">If the type couldn't be resolved</exception>
		public TypeDef ResolveThrow() {
			var type = Resolve();
			if (type != null)
				return type;
			throw new TypeResolveException(string.Format("Could not resolve type: {0} ({1})", this, DefinitionAssembly));
		}

		/// <summary>
		/// Converts this instance to a <see cref="TypeRef"/>
		/// </summary>
		/// <returns>A new <see cref="TypeRef"/> instance</returns>
		public TypeRef ToTypeRef() {
			TypeRef result = null, prev = null;
			var mod = module;
			IImplementation impl = this;
			for (int i = 0; i < MAX_LOOP_ITERS && impl != null; i++) {
				var et = impl as ExportedType;
				if (et != null) {
					var newTr = mod.UpdateRowId(new TypeRefUser(mod, et.TypeNamespace, et.TypeName));
					if (result == null)
						result = newTr;
					if (prev != null)
						prev.ResolutionScope = newTr;

					prev = newTr;
					impl = et.Implementation;
					continue;
				}

				var asmRef = impl as AssemblyRef;
				if (asmRef != null) {
					// prev is never null when we're here
					prev.ResolutionScope = asmRef;
					return result;
				}

				var file = impl as FileDef;
				if (file != null) {
					// prev is never null when we're here
					prev.ResolutionScope = FindModule(mod, file);
					return result;
				}

				break;
			}
			return result;
		}

		static ModuleDef FindModule(ModuleDef module, FileDef file) {
			if (module == null || file == null)
				return null;
			if (UTF8String.CaseInsensitiveEquals(module.Name, file.Name))
				return module;
			var asm = module.Assembly;
			if (asm == null)
				return null;
			return asm.FindModule(file.Name);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// An ExportedType row created by the user and not present in the original .NET file
	/// </summary>
	public class ExportedTypeUser : ExportedType {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		public ExportedTypeUser(ModuleDef module) {
			this.module = module;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="typeDefId">TypeDef ID</param>
		/// <param name="typeName">Type name</param>
		/// <param name="typeNamespace">Type namespace</param>
		/// <param name="flags">Flags</param>
		/// <param name="implementation">Implementation</param>
		public ExportedTypeUser(ModuleDef module, uint typeDefId, UTF8String typeNamespace, UTF8String typeName, TypeAttributes flags, IImplementation implementation) {
			this.module = module;
			this.typeDefId = typeDefId;
			this.typeName = typeName;
			this.typeNamespace = typeNamespace;
			this.attributes = (int)flags;
			this.implementation = implementation;
			this.implementation_isInitialized = true;
		}
	}

	/// <summary>
	/// Created from a row in the ExportedType table
	/// </summary>
	sealed class ExportedTypeMD : ExportedType, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;
		readonly uint implementationRid;

		/// <inheritdoc/>
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.ExportedType, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = ThreadSafeListCreator.Create<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), new GenericParamContext(), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <inheritdoc/>
		protected override IImplementation GetImplementation_NoLock() {
			return readerModule.ResolveImplementation(implementationRid);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ExportedType</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ExportedTypeMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.ExportedTypeTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("ExportedType rid {0} does not exist", rid));
#endif
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			this.module = readerModule;
			uint name, @namespace;
			this.implementationRid = readerModule.TablesStream.ReadExportedTypeRow(origRid, out this.attributes, out this.typeDefId, out name, out @namespace);
			this.typeName = readerModule.StringsStream.ReadNoNull(name);
			this.typeNamespace = readerModule.StringsStream.ReadNoNull(@namespace);
		}
	}
}
