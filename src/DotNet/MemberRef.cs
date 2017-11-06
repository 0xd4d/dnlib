// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
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
		public MDToken MDToken {
			get { return new MDToken(Table.MemberRef, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 6; }
		}

		/// <inheritdoc/>
		public int MethodDefOrRefTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int CustomAttributeTypeTag {
			get { return 3; }
		}

		/// <summary>
		/// From column MemberRef.Class
		/// </summary>
		public IMemberRefParent Class {
			get { return @class; }
			set { @class = value; }
		}
		/// <summary/>
		protected IMemberRefParent @class;

		/// <summary>
		/// From column MemberRef.Name
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value; }
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column MemberRef.Signature
		/// </summary>
		public CallingConventionSig Signature {
			get { return signature; }
			set { signature = value; }
		}
		/// <summary/>
		protected CallingConventionSig signature;

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
			get { return 6; }
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
		public ITypeDefOrRef DeclaringType {
			get {
				var owner = @class;

				var tdr = owner as ITypeDefOrRef;
				if (tdr != null)
					return tdr;

				var method = owner as MethodDef;
				if (method != null)
					return method.DeclaringType;

				var mr = owner as ModuleRef;
				if (mr != null) {
					var tr = GetGlobalTypeRef(mr);
					if (module != null)
						return module.UpdateRowId(tr);
					return tr;
				}

				return null;
			}
		}

		TypeRefUser GetGlobalTypeRef(ModuleRef mr) {
			if (module == null)
				return CreateDefaultGlobalTypeRef(mr);
			var globalType = module.GlobalType;
			if (globalType != null && new SigComparer().Equals(module, mr))
				return new TypeRefUser(module, globalType.Namespace, globalType.Name, mr);
			var asm = module.Assembly;
			if (asm == null)
				return CreateDefaultGlobalTypeRef(mr);
			var mod = asm.FindModule(mr.Name);
			if (mod == null)
				return CreateDefaultGlobalTypeRef(mr);
			globalType = mod.GlobalType;
			if (globalType == null)
				return CreateDefaultGlobalTypeRef(mr);
			return new TypeRefUser(module, globalType.Namespace, globalType.Name, mr);
		}

		TypeRefUser CreateDefaultGlobalTypeRef(ModuleRef mr) {
			var tr = new TypeRefUser(module, string.Empty, "<Module>", mr);
			if (module != null)
				module.UpdateRowId(tr);
			return tr;
		}

		bool IIsTypeOrMethod.IsType {
			get { return false; }
		}

		bool IIsTypeOrMethod.IsMethod {
			get { return IsMethodRef; }
		}

		bool IMemberRef.IsField {
			get { return IsFieldRef; }
		}

		bool IMemberRef.IsTypeSpec {
			get { return false; }
		}

		bool IMemberRef.IsTypeRef {
			get { return false; }
		}

		bool IMemberRef.IsTypeDef {
			get { return false; }
		}

		bool IMemberRef.IsMethodSpec {
			get { return false; }
		}

		bool IMemberRef.IsMethodDef {
			get { return false; }
		}

		bool IMemberRef.IsMemberRef {
			get { return true; }
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
		/// <c>true</c> if this is a method reference (<see cref="MethodSig"/> != <c>null</c>)
		/// </summary>
		public bool IsMethodRef {
			get { return MethodSig != null; }
		}

		/// <summary>
		/// <c>true</c> if this is a field reference (<see cref="FieldSig"/> != <c>null</c>)
		/// </summary>
		public bool IsFieldRef {
			get { return FieldSig != null; }
		}

		/// <summary>
		/// Gets/sets the method sig
		/// </summary>
		public MethodSig MethodSig {
			get { return signature as MethodSig; }
			set { signature = value; }
		}

		/// <summary>
		/// Gets/sets the field sig
		/// </summary>
		public FieldSig FieldSig {
			get { return signature as FieldSig; }
			set { signature = value; }
		}

		/// <inheritdoc/>
		public ModuleDef Module {
			get { return module; }
		}

		/// <summary>
		/// <c>true</c> if the method has a hidden 'this' parameter
		/// </summary>
		public bool HasThis {
			get {
				var ms = MethodSig;
				return ms == null ? false : ms.HasThis;
			}
		}

		/// <summary>
		/// <c>true</c> if the method has an explicit 'this' parameter
		/// </summary>
		public bool ExplicitThis {
			get {
				var ms = MethodSig;
				return ms == null ? false : ms.ExplicitThis;
			}
		}

		/// <summary>
		/// Gets the calling convention
		/// </summary>
		public CallingConvention CallingConvention {
			get {
				var ms = MethodSig;
				return ms == null ? 0 : ms.CallingConvention & CallingConvention.Mask;
			}
		}

		/// <summary>
		/// Gets/sets the method return type
		/// </summary>
		public TypeSig ReturnType {
			get {
				var ms = MethodSig;
				return ms == null ? null : ms.RetType;
			}
			set {
				var ms = MethodSig;
				if (ms != null)
					ms.RetType = value;
			}
		}

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get {
				var sig = MethodSig;
				return sig == null ? 0 : (int)sig.GenParamCount;
			}
		}

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string FullName {
			get {
				var parent = @class;
				IList<TypeSig> typeGenArgs = null;
				if (parent is TypeSpec) {
					var sig = ((TypeSpec)parent).TypeSig as GenericInstSig;
					if (sig != null)
						typeGenArgs = sig.GenericArguments;
				}
				var methodSig = MethodSig;
				if (methodSig != null)
					return FullNameCreator.MethodFullName(GetDeclaringTypeFullName(parent), name, methodSig, typeGenArgs, null, null, null);
				var fieldSig = FieldSig;
				if (fieldSig != null)
					return FullNameCreator.FieldFullName(GetDeclaringTypeFullName(parent), name, fieldSig, typeGenArgs, null);
				return string.Empty;
			}
		}

		/// <summary>
		/// Get the declaring type's full name
		/// </summary>
		/// <returns>Full name or <c>null</c> if there's no declaring type</returns>
		public string GetDeclaringTypeFullName() {
			return GetDeclaringTypeFullName(@class);
		}

		string GetDeclaringTypeFullName(IMemberRefParent parent) {
			if (parent == null)
				return null;
			if (parent is ITypeDefOrRef)
				return ((ITypeDefOrRef)parent).FullName;
			if (parent is ModuleRef)
				return string.Format("[module:{0}]<Module>", ((ModuleRef)parent).ToString());
			if (parent is MethodDef) {
				var declaringType = ((MethodDef)parent).DeclaringType;
				return declaringType == null ? null : declaringType.FullName;
			}
			return null;	// Should never be reached
		}

		/// <summary>
		/// Resolves the method/field
		/// </summary>
		/// <returns>A <see cref="MethodDef"/> or a <see cref="FieldDef"/> instance or <c>null</c>
		/// if it couldn't be resolved.</returns>
		public IMemberForwarded Resolve() {
			if (module == null)
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
			if (memberDef != null)
				return memberDef;
			throw new MemberRefResolveException(string.Format("Could not resolve method/field: {0} ({1})", this, this.GetDefinitionAssembly()));
		}

		/// <summary>
		/// Resolves the field
		/// </summary>
		/// <returns>A <see cref="FieldDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		public FieldDef ResolveField() {
			return Resolve() as FieldDef;
		}

		/// <summary>
		/// Resolves the field
		/// </summary>
		/// <returns>A <see cref="FieldDef"/> instance</returns>
		/// <exception cref="MemberRefResolveException">If the field couldn't be resolved</exception>
		public FieldDef ResolveFieldThrow() {
			var field = ResolveField();
			if (field != null)
				return field;
			throw new MemberRefResolveException(string.Format("Could not resolve field: {0} ({1})", this, this.GetDefinitionAssembly()));
		}

		/// <summary>
		/// Resolves the method
		/// </summary>
		/// <returns>A <see cref="MethodDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		public MethodDef ResolveMethod() {
			return Resolve() as MethodDef;
		}

		/// <summary>
		/// Resolves the method
		/// </summary>
		/// <returns>A <see cref="MethodDef"/> instance</returns>
		/// <exception cref="MemberRefResolveException">If the method couldn't be resolved</exception>
		public MethodDef ResolveMethodThrow() {
			var method = ResolveMethod();
			if (method != null)
				return method;
			throw new MemberRefResolveException(string.Format("Could not resolve method: {0} ({1})", this, this.GetDefinitionAssembly()));
		}

		bool IContainsGenericParameter.ContainsGenericParameter {
			get { return TypeHelper.ContainsGenericParameter(this); }
		}

		/// <summary>
		/// Gets a <see cref="GenericParamContext"/> that can be used as signature context
		/// </summary>
		/// <param name="gpContext">Context passed to the constructor</param>
		/// <param name="class">Field/method class owner</param>
		/// <returns></returns>
		protected static GenericParamContext GetSignatureGenericParamContext(GenericParamContext gpContext, IMemberRefParent @class) {
			TypeDef type = null;
			MethodDef method = gpContext.Method;

			var ts = @class as TypeSpec;
			if (ts != null) {
				var gis = ts.TypeSig as GenericInstSig;
				if (gis != null)
					type = gis.GenericType.ToTypeDefOrRef().ResolveTypeDef();
			}

			return new GenericParamContext(type, method);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A MemberRef row created by the user and not present in the original .NET file
	/// </summary>
	public class MemberRefUser : MemberRef {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		public MemberRefUser(ModuleDef module) {
			this.module = module;
		}

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
			this.signature = sig;
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
			this.signature = sig;
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
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.MemberRef, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = ThreadSafeListCreator.Create<PdbCustomDebugInfo>();
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
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.MemberRefTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("MemberRef rid {0} does not exist", rid));
#endif
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			this.gpContext = gpContext;
			this.module = readerModule;
			uint @class, name;
			uint signature = readerModule.TablesStream.ReadMemberRefRow(origRid, out @class, out name);
			this.name = readerModule.StringsStream.ReadNoNull(name);
			this.@class = readerModule.ResolveMemberRefParent(@class, gpContext);
			this.signature = readerModule.ReadSignature(signature, GetSignatureGenericParamContext(gpContext, this.@class));
		}
	}
}
