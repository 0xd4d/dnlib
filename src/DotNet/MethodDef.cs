using System;
using System.Collections.Generic;
using dot10.PE;
using dot10.DotNet.MD;
using dot10.DotNet.Emit;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Method table
	/// </summary>
	public abstract class MethodDef : IHasCustomAttribute, IHasDeclSecurity, IMemberRefParent, IMethodDefOrRef, IMemberForwarded, ICustomAttributeType, ITypeOrMethodDef, IManagedEntryPoint, IListListener<GenericParam>, IListListener<ParamDef> {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// All parameters
		/// </summary>
		protected ParameterList parameterList;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Method, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int HasDeclSecurityTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 3; }
		}

		/// <inheritdoc/>
		public int MethodDefOrRefTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int MemberForwardedTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int CustomAttributeTypeTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int TypeOrMethodDefTag {
			get { return 1; }
		}

		/// <summary>
		/// From column Method.RVA
		/// </summary>
		public abstract RVA RVA { get; set; }

		/// <summary>
		/// From column Method.ImplFlags
		/// </summary>
		public abstract MethodImplAttributes ImplFlags { get; set; }

		/// <summary>
		/// From column Method.Flags
		/// </summary>
		public abstract MethodAttributes Flags { get; set; }

		/// <summary>
		/// From column Method.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column Method.Signature
		/// </summary>
		public abstract CallingConventionSig Signature { get; set; }

		/// <summary>
		/// From column Method.ParamList
		/// </summary>
		public abstract IList<ParamDef> ParamList { get; }

		/// <inheritdoc/>
		public abstract IList<GenericParam> GenericParams { get; }

		/// <inheritdoc/>
		public abstract IList<DeclSecurity> DeclSecurities { get; }

		/// <inheritdoc/>
		public abstract ImplMap ImplMap { get; set; }

		/// <summary>
		/// Gets/sets the method body. See also <see cref="CilBody"/>
		/// </summary>
		public abstract MethodBody MethodBody { get; set; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <summary>
		/// Gets the methods this method implements
		/// </summary>
		public abstract IList<MethodOverride> Overrides { get; }

		/// <summary>
		/// Gets/sets the declaring type (owner type)
		/// </summary>
		public TypeDef DeclaringType {
			get { return DeclaringType2; }
			set {
				var currentDeclaringType = DeclaringType2;
				if (currentDeclaringType != null)
					currentDeclaringType.Methods.Remove(this);	// Will set DeclaringType2 = null
				if (value != null)
					value.Methods.Add(this);	// Will set DeclaringType2 = value
			}
		}

		/// <inheritdoc/>
		ITypeDefOrRef IMethod.DeclaringType {
			get { return DeclaringType; }
		}

		/// <summary>
		/// Called by <see cref="DeclaringType"/>
		/// </summary>
		protected internal abstract TypeDef DeclaringType2 { get; set; }

		/// <summary>
		/// Gets/sets the CIL method body
		/// </summary>
		public CilBody CilBody {
			get { return MethodBody as CilBody; }
			set { MethodBody = value; }
		}

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string FullName {
			get {
				var declaringType = DeclaringType;
				return FullNameCreator.MethodFullName(declaringType == null ? null : declaringType.FullName, Name, MethodSig);
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSig"/>
		/// </summary>
		public MethodSig MethodSig {
			get { return Signature as MethodSig; }
			set { Signature = value; }
		}

		/// <summary>
		/// Gets the parameters
		/// </summary>
		public ParameterList Parameters {
			get { return parameterList; }
		}

		/// <summary>
		/// Gets/sets the method access
		/// </summary>
		public MethodAttributes Access {
			get { return Flags & MethodAttributes.MemberAccessMask; }
			set { Flags = (Flags & ~MethodAttributes.MemberAccessMask) | (value & MethodAttributes.MemberAccessMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.PrivateScope"/> is set
		/// </summary>
		public bool IsPrivateScope {
			get { return (Flags & MethodAttributes.MemberAccessMask) == MethodAttributes.PrivateScope; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.Private"/> is set
		/// </summary>
		public bool IsPrivate {
			get { return (Flags & MethodAttributes.MemberAccessMask) == MethodAttributes.Private; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.FamANDAssem"/> is set
		/// </summary>
		public bool IsFamANDAssem {
			get { return (Flags & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.Assembly"/> is set
		/// </summary>
		public bool IsAssembly {
			get { return (Flags & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.Family"/> is set
		/// </summary>
		public bool IsFamily {
			get { return (Flags & MethodAttributes.MemberAccessMask) == MethodAttributes.Family; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.FamORAssem"/> is set
		/// </summary>
		public bool IsFamORAssem {
			get { return (Flags & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.Public"/> is set
		/// </summary>
		public bool IsPublic {
			get { return (Flags & MethodAttributes.MemberAccessMask) == MethodAttributes.Public; }
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.Static"/> bit
		/// </summary>
		public bool IsStatic {
			get { return (Flags & MethodAttributes.Static) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.Static;
				else
					Flags &= ~MethodAttributes.Static;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.Final"/> bit
		/// </summary>
		public bool IsFinal {
			get { return (Flags & MethodAttributes.Final) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.Final;
				else
					Flags &= ~MethodAttributes.Final;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.Virtual"/> bit
		/// </summary>
		public bool IsVirtual {
			get { return (Flags & MethodAttributes.Virtual) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.Virtual;
				else
					Flags &= ~MethodAttributes.Virtual;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.HideBySig"/> bit
		/// </summary>
		public bool IsHideBySig {
			get { return (Flags & MethodAttributes.HideBySig) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.HideBySig;
				else
					Flags &= ~MethodAttributes.HideBySig;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.NewSlot"/> bit
		/// </summary>
		public bool IsNewSlot {
			get { return (Flags & MethodAttributes.NewSlot) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.NewSlot;
				else
					Flags &= ~MethodAttributes.NewSlot;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.ReuseSlot"/> bit
		/// </summary>
		public bool IsReuseSlot {
			get { return (Flags & MethodAttributes.NewSlot) == 0; }
			set {
				if (value)
					Flags &= ~MethodAttributes.NewSlot;
				else
					Flags |= MethodAttributes.NewSlot;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.CheckAccessOnOverride"/> bit
		/// </summary>
		public bool IsCheckAccessOnOverride {
			get { return (Flags & MethodAttributes.CheckAccessOnOverride) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.CheckAccessOnOverride;
				else
					Flags &= ~MethodAttributes.CheckAccessOnOverride;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.Abstract"/> bit
		/// </summary>
		public bool IsAbstract {
			get { return (Flags & MethodAttributes.Abstract) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.Abstract;
				else
					Flags &= ~MethodAttributes.Abstract;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get { return (Flags & MethodAttributes.SpecialName) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.SpecialName;
				else
					Flags &= ~MethodAttributes.SpecialName;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.PinvokeImpl"/> bit
		/// </summary>
		public bool IsPinvokeImpl {
			get { return (Flags & MethodAttributes.PinvokeImpl) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.PinvokeImpl;
				else
					Flags &= ~MethodAttributes.PinvokeImpl;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.UnmanagedExport"/> bit
		/// </summary>
		public bool IsUnmanagedExport {
			get { return (Flags & MethodAttributes.UnmanagedExport) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.UnmanagedExport;
				else
					Flags &= ~MethodAttributes.UnmanagedExport;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRTSpecialName {
			get { return (Flags & MethodAttributes.RTSpecialName) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.RTSpecialName;
				else
					Flags &= ~MethodAttributes.RTSpecialName;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.HasSecurity"/> bit
		/// </summary>
		public bool HasSecurity {
			get { return (Flags & MethodAttributes.HasSecurity) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.HasSecurity;
				else
					Flags &= ~MethodAttributes.HasSecurity;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.RequireSecObject"/> bit
		/// </summary>
		public bool IsRequireSecObject {
			get { return (Flags & MethodAttributes.RequireSecObject) != 0; }
			set {
				if (value)
					Flags |= MethodAttributes.RequireSecObject;
				else
					Flags &= ~MethodAttributes.RequireSecObject;
			}
		}

		/// <summary>
		/// Gets/sets the code type
		/// </summary>
		public MethodImplAttributes CodeType {
			get { return ImplFlags & MethodImplAttributes.CodeTypeMask; }
			set { ImplFlags = (ImplFlags & ~MethodImplAttributes.CodeTypeMask) | (value & MethodImplAttributes.CodeTypeMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodImplAttributes.IL"/> is set
		/// </summary>
		public bool IsIL {
			get { return (ImplFlags & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.IL; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodImplAttributes.Native"/> is set
		/// </summary>
		public bool IsNative {
			get { return (ImplFlags & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Native; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodImplAttributes.OPTIL"/> is set
		/// </summary>
		public bool IsOPTIL {
			get { return (ImplFlags & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.OPTIL; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodImplAttributes.Runtime"/> is set
		/// </summary>
		public bool IsRuntime {
			get { return (ImplFlags & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Runtime; }
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.Unmanaged"/> bit
		/// </summary>
		public bool IsUnmanaged {
			get { return (ImplFlags & MethodImplAttributes.Unmanaged) != 0; }
			set {
				if (value)
					ImplFlags |= MethodImplAttributes.Unmanaged;
				else
					ImplFlags &= ~MethodImplAttributes.Unmanaged;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.Managed"/> bit
		/// </summary>
		public bool IsManaged {
			get { return (ImplFlags & MethodImplAttributes.Unmanaged) == 0; }
			set {
				if (value)
					ImplFlags &= ~MethodImplAttributes.Unmanaged;
				else
					ImplFlags |= MethodImplAttributes.Unmanaged;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.ForwardRef"/> bit
		/// </summary>
		public bool IsForwardRef {
			get { return (ImplFlags & MethodImplAttributes.ForwardRef) != 0; }
			set {
				if (value)
					ImplFlags |= MethodImplAttributes.ForwardRef;
				else
					ImplFlags &= ~MethodImplAttributes.ForwardRef;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.PreserveSig"/> bit
		/// </summary>
		public bool IsPreserveSig {
			get { return (ImplFlags & MethodImplAttributes.PreserveSig) != 0; }
			set {
				if (value)
					ImplFlags |= MethodImplAttributes.PreserveSig;
				else
					ImplFlags &= ~MethodImplAttributes.PreserveSig;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.InternalCall"/> bit
		/// </summary>
		public bool IsInternalCall {
			get { return (ImplFlags & MethodImplAttributes.InternalCall) != 0; }
			set {
				if (value)
					ImplFlags |= MethodImplAttributes.InternalCall;
				else
					ImplFlags &= ~MethodImplAttributes.InternalCall;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.Synchronized"/> bit
		/// </summary>
		public bool IsSynchronized {
			get { return (ImplFlags & MethodImplAttributes.Synchronized) != 0; }
			set {
				if (value)
					ImplFlags |= MethodImplAttributes.Synchronized;
				else
					ImplFlags &= ~MethodImplAttributes.Synchronized;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.NoInlining"/> bit
		/// </summary>
		public bool IsNoInlining {
			get { return (ImplFlags & MethodImplAttributes.NoInlining) != 0; }
			set {
				if (value)
					ImplFlags |= MethodImplAttributes.NoInlining;
				else
					ImplFlags &= ~MethodImplAttributes.NoInlining;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.AggressiveInlining"/> bit
		/// </summary>
		public bool IsAggressiveInlining {
			get { return (ImplFlags & MethodImplAttributes.AggressiveInlining) != 0; }
			set {
				if (value)
					ImplFlags |= MethodImplAttributes.AggressiveInlining;
				else
					ImplFlags &= ~MethodImplAttributes.AggressiveInlining;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.NoOptimization"/> bit
		/// </summary>
		public bool IsNoOptimization {
			get { return (ImplFlags & MethodImplAttributes.NoOptimization) != 0; }
			set {
				if (value)
					ImplFlags |= MethodImplAttributes.NoOptimization;
				else
					ImplFlags &= ~MethodImplAttributes.NoOptimization;
			}
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnAdd(int index, GenericParam value, bool isLazyAdd) {
			if (isLazyAdd) {
#if DEBUG
				if (value.Owner != this)
					throw new InvalidOperationException("Added generic param's Owner != this");
#endif
				return;
			}
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
			foreach (var gp in GenericParams)
				gp.Owner = null;
		}

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnAdd(int index, ParamDef value, bool isLazyAdd) {
			if (isLazyAdd) {
#if DEBUG
				if (value.DeclaringMethod != this)
					throw new InvalidOperationException("Added param's DeclaringMethod != this");
#endif
				return;
			}
			if (value.DeclaringMethod != null)
				throw new InvalidOperationException("Param is already owned by another method. Set DeclaringMethod to null first.");
			value.DeclaringMethod = this;
		}

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnRemove(int index, ParamDef value) {
			value.DeclaringMethod = null;
		}

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnClear() {
			foreach (var pd in ParamList)
				pd.DeclaringMethod = null;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A Method row created by the user and not present in the original .NET file
	/// </summary>
	public class MethodDefUser : MethodDef {
		RVA rva;
		MethodImplAttributes implFlags;
		MethodAttributes flags;
		UTF8String name;
		CallingConventionSig signature;
		IList<ParamDef> parameters = new List<ParamDef>();
		LazyList<GenericParam> genericParams;
		IList<DeclSecurity> declSecurities = new List<DeclSecurity>();
		ImplMap implMap;
		MethodBody methodBody;
		CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();
		IList<MethodOverride> overrides = new List<MethodOverride>();
		TypeDef declaringType;

		/// <inheritdoc/>
		public override RVA RVA {
			get { return rva; }
			set { rva = value; }
		}

		/// <inheritdoc/>
		public override MethodImplAttributes ImplFlags {
			get { return implFlags; }
			set { implFlags = value; }
		}

		/// <inheritdoc/>
		public override MethodAttributes Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override CallingConventionSig Signature {
			get { return signature; }
			set {
				signature = value;
				parameterList.UpdateParameterTypes();
			}
		}

		/// <inheritdoc/>
		public override IList<ParamDef> ParamList {
			get { return parameters; }
		}

		/// <inheritdoc/>
		public override IList<GenericParam> GenericParams {
			get { return genericParams; }
		}

		/// <inheritdoc/>
		public override IList<DeclSecurity> DeclSecurities {
			get { return declSecurities; }
		}

		/// <inheritdoc/>
		public override ImplMap ImplMap {
			get { return implMap; }
			set { implMap = value; }
		}

		/// <inheritdoc/>
		public override MethodBody MethodBody {
			get { return methodBody; }
			set { methodBody = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

		/// <inheritdoc/>
		public override IList<MethodOverride> Overrides {
			get { return overrides; }
		}

		/// <inheritdoc/>
		protected internal override TypeDef DeclaringType2 {
			get { return declaringType; }
			set { declaringType = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MethodDefUser() {
			this.parameterList = new ParameterList(this);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		public MethodDefUser(UTF8String name)
			: this(name, null, 0, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		public MethodDefUser(UTF8String name, MethodSig methodSig)
			: this(name, methodSig, 0, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="flags">Flags</param>
		public MethodDefUser(UTF8String name, MethodSig methodSig, MethodAttributes flags)
			: this(name, methodSig, 0, flags) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="implFlags">Impl flags</param>
		public MethodDefUser(UTF8String name, MethodSig methodSig, MethodImplAttributes implFlags)
			: this(name, methodSig, implFlags, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="implFlags">Impl flags</param>
		/// <param name="flags">Flags</param>
		public MethodDefUser(UTF8String name, MethodSig methodSig, MethodImplAttributes implFlags, MethodAttributes flags) {
			this.name = name;
			this.signature = methodSig;
			this.implFlags = implFlags;
			this.flags = flags;
			this.genericParams = new LazyList<GenericParam>(this);
			this.parameterList = new ParameterList(this);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		public MethodDefUser(string name)
			: this(name, null, 0, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		public MethodDefUser(string name, MethodSig methodSig)
			: this(name, methodSig, 0, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="flags">Flags</param>
		public MethodDefUser(string name, MethodSig methodSig, MethodAttributes flags)
			: this(name, methodSig, 0, flags) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="implFlags">Impl flags</param>
		public MethodDefUser(string name, MethodSig methodSig, MethodImplAttributes implFlags)
			: this(name, methodSig, implFlags, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="methodSig">Method sig</param>
		/// <param name="implFlags">Impl flags</param>
		/// <param name="flags">Flags</param>
		public MethodDefUser(string name, MethodSig methodSig, MethodImplAttributes implFlags, MethodAttributes flags)
			: this(new UTF8String(name), methodSig, implFlags, flags) {
		}
	}

	/// <summary>
	/// Created from a row in the Method table
	/// </summary>
	sealed class MethodDefMD : MethodDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawMethodRow rawRow;

		UserValue<RVA> rva;
		UserValue<MethodImplAttributes> implFlags;
		UserValue<MethodAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<CallingConventionSig> signature;
		LazyList<ParamDef> parameters;
		LazyList<GenericParam> genericParams;
		LazyList<DeclSecurity> declSecurities;
		UserValue<ImplMap> implMap;
		UserValue<MethodBody> methodBody;
		CustomAttributeCollection customAttributeCollection;
		IList<MethodOverride> overrides;
		UserValue<TypeDef> declaringType;

		/// <inheritdoc/>
		public override RVA RVA {
			get { return rva.Value; }
			set { rva.Value = value; }
		}

		/// <inheritdoc/>
		public override MethodImplAttributes ImplFlags {
			get { return implFlags.Value; }
			set { implFlags.Value = value; }
		}

		/// <inheritdoc/>
		public override MethodAttributes Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override CallingConventionSig Signature {
			get { return signature.Value; }
			set {
				signature.Value = value;
				parameterList.UpdateParameterTypes();
			}
		}

		/// <inheritdoc/>
		public override IList<ParamDef> ParamList {
			get {
				if (parameters == null) {
					var list = readerModule.MetaData.GetParamRidList(rid);
					parameters = new LazyList<ParamDef>((int)list.Length, list, (list2, index) => readerModule.ResolveParam(((RidList)list2)[index]));
				}
				return parameters;
			}
		}

		/// <inheritdoc/>
		public override IList<GenericParam> GenericParams {
			get {
				if (genericParams == null) {
					var list = readerModule.MetaData.GetGenericParamRidList(Table.Method, rid);
					genericParams = new LazyList<GenericParam>((int)list.Length, list, (list2, index) => readerModule.ResolveGenericParam(((RidList)list2)[index]));
				}
				return genericParams;
			}
		}

		/// <inheritdoc/>
		public override IList<DeclSecurity> DeclSecurities {
			get {
				if (declSecurities == null) {
					var list = readerModule.MetaData.GetDeclSecurityRidList(Table.Method, rid);
					declSecurities = new LazyList<DeclSecurity>((int)list.Length, list, (list2, index) => readerModule.ResolveDeclSecurity(((RidList)list2)[index]));
				}
				return declSecurities;
			}
		}

		/// <inheritdoc/>
		public override ImplMap ImplMap {
			get { return implMap.Value; }
			set { implMap.Value = value; }
		}

		/// <inheritdoc/>
		public override MethodBody MethodBody {
			get { return methodBody.Value; }
			set { methodBody.Value = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.Method, rid);
					customAttributeCollection = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
				}
				return customAttributeCollection;
			}
		}

		/// <inheritdoc/>
		public override IList<MethodOverride> Overrides {
			get {
				if (overrides != null)
					return overrides;
				var dt = DeclaringType as TypeDefMD;
				if (dt == null)
					return overrides = new List<MethodOverride>();
				return overrides = dt.GetMethodOverrides(this);
			}
		}

		/// <inheritdoc/>
		protected internal override TypeDef DeclaringType2 {
			get { return declaringType.Value; }
			set { declaringType.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Method</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public MethodDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.Method).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("Method rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
			this.parameterList = new ParameterList(this);
		}

		void Initialize() {
			rva.ReadOriginalValue = () => {
				InitializeRawRow();
				return (RVA)rawRow.RVA;
			};
			implFlags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (MethodImplAttributes)rawRow.ImplFlags;
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (MethodAttributes)rawRow.Flags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.ReadNoNull(rawRow.Name);
			};
			signature.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ReadSignature(rawRow.Signature);
			};
			implMap.ReadOriginalValue = () => {
				return readerModule.ResolveImplMap(readerModule.MetaData.GetImplMapRid(Table.Method, rid));
			};
			methodBody.ReadOriginalValue = () => {
				// Use RVA and ImplFlags from the original row, not the current values the
				// user may have modified.
				InitializeRawRow();
				if (rawRow.RVA == 0)
					return null;
				if (((MethodImplAttributes)rawRow.ImplFlags & MethodImplAttributes.CodeTypeMask) != MethodImplAttributes.IL)
					return null;
				return readerModule.ReadCilBody(Parameters.Parameters, (RVA)rawRow.RVA);
			};
			declaringType.ReadOriginalValue = () => {
				return readerModule.GetOwnerType(this);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadMethodRow(rid);
		}
	}
}
