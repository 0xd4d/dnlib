// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the MemberRef table
	/// </summary>
	public abstract class MemberRef : IHasCustomAttribute, IMethodDefOrRef, ICustomAttributeType, IField, IContainsGenericParameter, IHasCustomDebugInformation {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// The owner module
		/// </summary>
		protected ModuleDef module;

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.MemberRef, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 6;

		/// <inheritdoc/>
		public int MethodDefOrRefTag => 1;

		/// <inheritdoc/>
		public int CustomAttributeTypeTag => 3;

		/// <summary>
		/// From column MemberRef.Class
		/// </summary>
		public IMemberRefParent Class {
			get => @class;
			set => @class = value;
		}
		/// <summary/>
		protected IMemberRefParent @class;

		/// <summary>
		/// From column MemberRef.Name
		/// </summary>
		public UTF8String Name {
			get => name;
			set => name = value;
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column MemberRef.Signature
		/// </summary>
		public CallingConventionSig Signature {
			get => signature;
			set => signature = value;
		}
		/// <summary/>
		protected CallingConventionSig signature;

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
		public int HasCustomDebugInformationTag => 6;

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

		/// <inheritdoc/>
		public ITypeDefOrRef DeclaringType {
			get {
				var owner = @class;

				if (owner is ITypeDefOrRef tdr)
					return tdr;

				if (owner is MethodDef method)
					return method.DeclaringType;

				if (owner is ModuleRef mr) {
					var tr = GetGlobalTypeRef(mr);
					if (!(module is null))
						return module.UpdateRowId(tr);
					return tr;
				}

				return null;
			}
		}

		TypeRefUser GetGlobalTypeRef(ModuleRef mr) {
			if (module is null)
				return CreateDefaultGlobalTypeRef(mr);
			var globalType = module.GlobalType;
			if (!(globalType is null) && new SigComparer().Equals(module, mr))
				return new TypeRefUser(module, globalType.Namespace, globalType.Name, mr);
			var asm = module.Assembly;
			if (asm is null)
				return CreateDefaultGlobalTypeRef(mr);
			var mod = asm.FindModule(mr.Name);
			if (mod is null)
				return CreateDefaultGlobalTypeRef(mr);
			globalType = mod.GlobalType;
			if (globalType is null)
				return CreateDefaultGlobalTypeRef(mr);
			return new TypeRefUser(module, globalType.Namespace, globalType.Name, mr);
		}

		TypeRefUser CreateDefaultGlobalTypeRef(ModuleRef mr) {
			var tr = new TypeRefUser(module, string.Empty, "<Module>", mr);
			if (!(module is null))
				module.UpdateRowId(tr);
			return tr;
		}

		bool IIsTypeOrMethod.IsType => false;
		bool IIsTypeOrMethod.IsMethod => IsMethodRef;
		bool IMemberRef.IsField => IsFieldRef;
		bool IMemberRef.IsTypeSpec => false;
		bool IMemberRef.IsTypeRef => false;
		bool IMemberRef.IsTypeDef => false;
		bool IMemberRef.IsMethodSpec => false;
		bool IMemberRef.IsMethodDef => false;
		bool IMemberRef.IsMemberRef => true;
		bool IMemberRef.IsFieldDef => false;
		bool IMemberRef.IsPropertyDef => false;
		bool IMemberRef.IsEventDef => false;
		bool IMemberRef.IsGenericParam => false;

		/// <summary>
		/// <c>true</c> if this is a method reference (<see cref="MethodSig"/> != <c>null</c>)
		/// </summary>
		public bool IsMethodRef => !(MethodSig is null);

		/// <summary>
		/// <c>true</c> if this is a field reference (<see cref="FieldSig"/> != <c>null</c>)
		/// </summary>
		public bool IsFieldRef => !(FieldSig is null);

		/// <summary>
		/// Gets/sets the method sig
		/// </summary>
		public MethodSig MethodSig {
			get => signature as MethodSig;
			set => signature = value;
		}

		/// <summary>
		/// Gets/sets the field sig
		/// </summary>
		public FieldSig FieldSig {
			get => signature as FieldSig;
			set => signature = value;
		}

		/// <inheritdoc/>
		public ModuleDef Module => module;

		/// <summary>
		/// <c>true</c> if the method has a hidden 'this' parameter
		/// </summary>
		public bool HasThis {
			get {
				var ms = MethodSig;
				return ms is null ? false : ms.HasThis;
			}
		}

		/// <summary>
		/// <c>true</c> if the method has an explicit 'this' parameter
		/// </summary>
		public bool ExplicitThis {
			get {
				var ms = MethodSig;
				return ms is null ? false : ms.ExplicitThis;
			}
		}

		/// <summary>
		/// Gets the calling convention
		/// </summary>
		public CallingConvention CallingConvention {
			get {
				var ms = MethodSig;
				return ms is null ? 0 : ms.CallingConvention & CallingConvention.Mask;
			}
		}

		/// <summary>
		/// Gets/sets the method return type
		/// </summary>
		public TypeSig ReturnType {
			get => MethodSig?.RetType;
			set {
				var ms = MethodSig;
				if (!(ms is null))
					ms.RetType = value;
			}
		}

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters => (int)(MethodSig?.GenParamCount ?? 0);

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string FullName {
			get {
				var parent = @class;
				IList<TypeSig> typeGenArgs = null;
				if (parent is TypeSpec) {
					if (((TypeSpec)parent).TypeSig is GenericInstSig sig)
						typeGenArgs = sig.GenericArguments;
				}
				var methodSig = MethodSig;
				if (!(methodSig is null))
					return FullNameFactory.MethodFullName(GetDeclaringTypeFullName(parent), name, methodSig, typeGenArgs, null, null, null);
				var fieldSig = FieldSig;
				if (!(fieldSig is null))
					return FullNameFactory.FieldFullName(GetDeclaringTypeFullName(parent), name, fieldSig, typeGenArgs, null);
				return string.Empty;
			}
		}

		/// <summary>
		/// Get the declaring type's full name
		/// </summary>
		/// <returns>Full name or <c>null</c> if there's no declaring type</returns>
		public string GetDeclaringTypeFullName() => GetDeclaringTypeFullName(@class);

		string GetDeclaringTypeFullName(IMemberRefParent parent) {
			if (parent is null)
				return null;
			if (parent is ITypeDefOrRef)
				return ((ITypeDefOrRef)parent).FullName;
			if (parent is ModuleRef)
				return $"[module:{((ModuleRef)parent).ToString()}]<Module>";
			if (parent is MethodDef) {
				var declaringType = ((MethodDef)parent).DeclaringType;
				return declaringType?.FullName;
			}
			return null;	// Should never be reached
		}

		/// <summary>
		/// Resolves the method/field
		/// </summary>
		/// <returns>A <see cref="MethodDef"/> or a <see cref="FieldDef"/> instance or <c>null</c>
		/// if it couldn't be resolved.</returns>
		public IMemberForwarded Resolve() {
			if (module is null)
				return null;
			return module.Context.Resolver.Resolve(this);
		}

		/// <summary>
		/// Resolves the method/field
		/// </summary>
		/// <returns>A <see cref="MethodDef"/> or a <see cref="FieldDef"/> instance</returns>
		/// <exception cref="MemberRefResolveException">If the method/field couldn't be resolved</exception>
		public IMemberForwarded ResolveThrow() {
			var memberDef = Resolve();
			if (!(memberDef is null))
				return memberDef;
			throw new MemberRefResolveException($"Could not resolve method/field: {this} ({this.GetDefinitionAssembly()})");
		}

		/// <summary>
		/// Resolves the field
		/// </summary>
		/// <returns>A <see cref="FieldDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		public FieldDef ResolveField() => Resolve() as FieldDef;

		/// <summary>
		/// Resolves the field
		/// </summary>
		/// <returns>A <see cref="FieldDef"/> instance</returns>
		/// <exception cref="MemberRefResolveException">If the field couldn't be resolved</exception>
		public FieldDef ResolveFieldThrow() {
			var field = ResolveField();
			if (!(field is null))
				return field;
			throw new MemberRefResolveException($"Could not resolve field: {this} ({this.GetDefinitionAssembly()})");
		}

		/// <summary>
		/// Resolves the method
		/// </summary>
		/// <returns>A <see cref="MethodDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		public MethodDef ResolveMethod() => Resolve() as MethodDef;

		/// <summary>
		/// Resolves the method
		/// </summary>
		/// <returns>A <see cref="MethodDef"/> instance</returns>
		/// <exception cref="MemberRefResolveException">If the method couldn't be resolved</exception>
		public MethodDef ResolveMethodThrow() {
			var method = ResolveMethod();
			if (!(method is null))
				return method;
			throw new MemberRefResolveException($"Could not resolve method: {this} ({this.GetDefinitionAssembly()})");
		}

		bool IContainsGenericParameter.ContainsGenericParameter => TypeHelper.ContainsGenericParameter(this);

		/// <summary>
		/// Gets a <see cref="GenericParamContext"/> that can be used as signature context
		/// </summary>
		/// <param name="gpContext">Context passed to the constructor</param>
		/// <param name="class">Field/method class owner</param>
		/// <returns></returns>
		protected static GenericParamContext GetSignatureGenericParamContext(GenericParamContext gpContext, IMemberRefParent @class) {
			TypeDef type = null;
			var method = gpContext.Method;

			if (@class is TypeSpec ts && ts.TypeSig is GenericInstSig gis)
				type = gis.GenericType.ToTypeDefOrRef().ResolveTypeDef();

			return new GenericParamContext(type, method);
		}

		/// <inheritdoc/>
		public override string ToString() => FullName;
	}

	/// <summary>
	/// A MemberRef row created by the user and not present in the original .NET file
	/// </summary>
	public class MemberRefUser : MemberRef {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		public MemberRefUser(ModuleDef module) => this.module = module;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Name of ref</param>
		public MemberRefUser(ModuleDef module, UTF8String name) {
			this.module = module;
			this.name = name;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Name of field ref</param>
		/// <param name="sig">Field sig</param>
		public MemberRefUser(ModuleDef module, UTF8String name, FieldSig sig)
			: this(module, name, sig, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Name of field ref</param>
		/// <param name="sig">Field sig</param>
		/// <param name="class">Owner of field</param>
		public MemberRefUser(ModuleDef module, UTF8String name, FieldSig sig, IMemberRefParent @class) {
			this.module = module;
			this.name = name;
			this.@class = @class;
			signature = sig;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Name of method ref</param>
		/// <param name="sig">Method sig</param>
		public MemberRefUser(ModuleDef module, UTF8String name, MethodSig sig)
			: this(module, name, sig, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Name of method ref</param>
		/// <param name="sig">Method sig</param>
		/// <param name="class">Owner of method</param>
		public MemberRefUser(ModuleDef module, UTF8String name, MethodSig sig, IMemberRefParent @class) {
			this.module = module;
			this.name = name;
			this.@class = @class;
			signature = sig;
		}
	}

	/// <summary>
	/// Created from a row in the MemberRef table
	/// </summary>
	sealed class MemberRefMD : MemberRef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;
		readonly GenericParamContext gpContext;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.MemberRef, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), gpContext, list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>MemberRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public MemberRefMD(ModuleDefMD readerModule, uint rid, GenericParamContext gpContext) {
#if DEBUG
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.MemberRefTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"MemberRef rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			this.gpContext = gpContext;
			module = readerModule;
			bool b = readerModule.TablesStream.TryReadMemberRefRow(origRid, out var row);
			Debug.Assert(b);
			name = readerModule.StringsStream.ReadNoNull(row.Name);
			@class = readerModule.ResolveMemberRefParent(row.Class, gpContext);
			signature = readerModule.ReadSignature(row.Signature, GetSignatureGenericParamContext(gpContext, @class));
		}
	}
}
