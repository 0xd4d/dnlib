// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the TypeRef table
	/// </summary>
	public abstract class TypeRef : ITypeDefOrRef, IHasCustomAttribute, IMemberRefParent, IHasCustomDebugInformation, IResolutionScope {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// The owner module
		/// </summary>
		protected ModuleDef module;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.TypeRef, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		public int TypeDefOrRefTag => 1;

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 2;

		/// <inheritdoc/>
		public int MemberRefParentTag => 1;

		/// <inheritdoc/>
		public int ResolutionScopeTag => 3;

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters => 0;

		/// <inheritdoc/>
		string IType.TypeName => FullNameFactory.Name(this, false, null);

		/// <inheritdoc/>
		public string ReflectionName => FullNameFactory.Name(this, true, null);

		/// <inheritdoc/>
		string IType.Namespace => FullNameFactory.Namespace(this, false, null);

		/// <inheritdoc/>
		public string ReflectionNamespace => FullNameFactory.Namespace(this, true, null);

		/// <inheritdoc/>
		public string FullName => FullNameFactory.FullName(this, false, null, null);

		/// <inheritdoc/>
		public string ReflectionFullName => FullNameFactory.FullName(this, true, null, null);

		/// <inheritdoc/>
		public string AssemblyQualifiedName => FullNameFactory.AssemblyQualifiedName(this, null, null);

		/// <inheritdoc/>
		public IAssembly DefinitionAssembly => FullNameFactory.DefinitionAssembly(this);

		/// <inheritdoc/>
		public IScope Scope => FullNameFactory.Scope(this);

		/// <inheritdoc/>
		public ITypeDefOrRef ScopeType => this;

		/// <summary>
		/// Always returns <c>false</c> since a <see cref="TypeRef"/> does not contain any
		/// <see cref="GenericVar"/> or <see cref="GenericMVar"/>.
		/// </summary>
		public bool ContainsGenericParameter => false;

		/// <inheritdoc/>
		public ModuleDef Module => module;

		/// <summary>
		/// From column TypeRef.ResolutionScope
		/// </summary>
		public IResolutionScope ResolutionScope {
			get {
				if (!resolutionScope_isInitialized)
					InitializeResolutionScope();
				return resolutionScope;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				resolutionScope = value;
				resolutionScope_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected IResolutionScope resolutionScope;
		/// <summary/>
		protected bool resolutionScope_isInitialized;

		void InitializeResolutionScope() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (resolutionScope_isInitialized)
				return;
			resolutionScope = GetResolutionScope_NoLock();
			resolutionScope_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="resolutionScope"/></summary>
		protected virtual IResolutionScope GetResolutionScope_NoLock() => null;

		/// <summary>
		/// From column TypeRef.Name
		/// </summary>
		public UTF8String Name {
			get => name;
			set => name = value;
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column TypeRef.Namespace
		/// </summary>
		public UTF8String Namespace {
			get => @namespace;
			set => @namespace = value;
		}
		/// <summary>Name</summary>
		protected UTF8String @namespace;

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributes is null)
					InitializeCustomAttributes();
				return customAttributes;
			}
		}
		/// <summary/>
		protected CustomAttributeCollection customAttributes;
		/// <summary>Initializes <see cref="customAttributes"/></summary>
		protected virtual void InitializeCustomAttributes() =>
			Interlocked.CompareExchange(ref customAttributes, new CustomAttributeCollection(), null);

		/// <inheritdoc/>
		public bool HasCustomAttributes => CustomAttributes.Count > 0;

		/// <inheritdoc/>
		public int HasCustomDebugInformationTag => 2;

		/// <inheritdoc/>
		public bool HasCustomDebugInfos => CustomDebugInfos.Count > 0;

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public IList<PdbCustomDebugInfo> CustomDebugInfos {
			get {
				if (customDebugInfos is null)
					InitializeCustomDebugInfos();
				return customDebugInfos;
			}
		}
		/// <summary/>
		protected IList<PdbCustomDebugInfo> customDebugInfos;
		/// <summary>Initializes <see cref="customDebugInfos"/></summary>
		protected virtual void InitializeCustomDebugInfos() =>
			Interlocked.CompareExchange(ref customDebugInfos, new List<PdbCustomDebugInfo>(), null);

		/// <summary>
		/// <c>true</c> if it's nested within another <see cref="TypeRef"/>
		/// </summary>
		public bool IsNested => !(DeclaringType is null);

		/// <inheritdoc/>
		public bool IsValueType {
			get {
				var td = Resolve();
				return !(td is null) && td.IsValueType;
			}
		}

		/// <inheritdoc/>
		public bool IsPrimitive => this.IsPrimitive();

		/// <summary>
		/// Gets the declaring type, if any
		/// </summary>
		public TypeRef DeclaringType => ResolutionScope as TypeRef;

		/// <inheritdoc/>
		ITypeDefOrRef IMemberRef.DeclaringType => DeclaringType;

		bool IIsTypeOrMethod.IsType => true;
		bool IIsTypeOrMethod.IsMethod => false;
		bool IMemberRef.IsField => false;
		bool IMemberRef.IsTypeSpec => false;
		bool IMemberRef.IsTypeRef => true;
		bool IMemberRef.IsTypeDef => false;
		bool IMemberRef.IsMethodSpec => false;
		bool IMemberRef.IsMethodDef => false;
		bool IMemberRef.IsMemberRef => false;
		bool IMemberRef.IsFieldDef => false;
		bool IMemberRef.IsPropertyDef => false;
		bool IMemberRef.IsEventDef => false;
		bool IMemberRef.IsGenericParam => false;

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <returns>A <see cref="TypeDef"/> instance or <c>null</c> if it couldn't be resolved</returns>
		public TypeDef Resolve() => Resolve(null);

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <param name="sourceModule">The module that needs to resolve the type or <c>null</c></param>
		/// <returns>A <see cref="TypeDef"/> instance or <c>null</c> if it couldn't be resolved</returns>
		public TypeDef Resolve(ModuleDef sourceModule) {
			if (module is null)
				return null;
			return module.Context.Resolver.Resolve(this, sourceModule ?? module);
		}

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <returns>A <see cref="TypeDef"/> instance</returns>
		/// <exception cref="TypeResolveException">If the type couldn't be resolved</exception>
		public TypeDef ResolveThrow() => ResolveThrow(null);

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <param name="sourceModule">The module that needs to resolve the type or <c>null</c></param>
		/// <returns>A <see cref="TypeDef"/> instance</returns>
		/// <exception cref="TypeResolveException">If the type couldn't be resolved</exception>
		public TypeDef ResolveThrow(ModuleDef sourceModule) {
			var type = Resolve(sourceModule);
			if (!(type is null))
				return type;
			throw new TypeResolveException($"Could not resolve type: {this} ({DefinitionAssembly})");
		}

		/// <summary>
		/// Gets the top-most (non-nested) <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">Input</param>
		/// <returns>The non-nested <see cref="TypeRef"/> or <c>null</c></returns>
		internal static TypeRef GetNonNestedTypeRef(TypeRef typeRef) {
			if (typeRef is null)
				return null;
			for (int i = 0; i < 1000; i++) {
				var next = typeRef.ResolutionScope as TypeRef;
				if (next is null)
					return typeRef;
				typeRef = next;
			}
			return null;	// Here if eg. the TypeRef has an infinite loop
		}

		/// <inheritdoc/>
		public override string ToString() => FullName;
	}

	/// <summary>
	/// A TypeRef row created by the user and not present in the original .NET file
	/// </summary>
	public class TypeRefUser : TypeRef {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Type name</param>
		public TypeRefUser(ModuleDef module, UTF8String name)
			: this(module, UTF8String.Empty, name) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="namespace">Type namespace</param>
		/// <param name="name">Type name</param>
		public TypeRefUser(ModuleDef module, UTF8String @namespace, UTF8String name)
			: this(module, @namespace, name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="namespace">Type namespace</param>
		/// <param name="name">Type name</param>
		/// <param name="resolutionScope">Resolution scope (a <see cref="ModuleDef"/>,
		/// <see cref="ModuleRef"/>, <see cref="AssemblyRef"/> or <see cref="TypeRef"/>)</param>
		public TypeRefUser(ModuleDef module, UTF8String @namespace, UTF8String name, IResolutionScope resolutionScope) {
			this.module = module;
			this.resolutionScope = resolutionScope;
			resolutionScope_isInitialized = true;
			this.name = name;
			this.@namespace = @namespace;
		}
	}

	/// <summary>
	/// Created from a row in the TypeRef table
	/// </summary>
	sealed class TypeRefMD : TypeRef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;
		readonly uint resolutionScopeCodedToken;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override IResolutionScope GetResolutionScope_NoLock() => readerModule.ResolveResolutionScope(resolutionScopeCodedToken);

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.TypeRef, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), new GenericParamContext(), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>TypeRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public TypeRefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.TypeRefTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"TypeRef rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			module = readerModule;
			bool b = readerModule.TablesStream.TryReadTypeRefRow(origRid, out var row);
			Debug.Assert(b);
			name = readerModule.StringsStream.ReadNoNull(row.Name);
			@namespace = readerModule.StringsStream.ReadNoNull(row.Namespace);
			resolutionScopeCodedToken = row.ResolutionScope;
		}
	}
}
