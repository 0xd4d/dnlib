// dnlib: See LICENSE.txt for more info

using System;
using System.Threading;
using dnlib.Utils;
using dnlib.PE;
using dnlib.DotNet.MD;
using dnlib.DotNet.Emit;
using dnlib.Threading;
using dnlib.DotNet.Pdb;
using System.Collections.Generic;
using System.Diagnostics;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Method table
	/// </summary>
	public abstract class MethodDef : IHasCustomAttribute, IHasDeclSecurity, IMemberRefParent, IMethodDefOrRef, IMemberForwarded, ICustomAttributeType, ITypeOrMethodDef, IManagedEntryPoint, IHasCustomDebugInformation, IListListener<GenericParam>, IListListener<ParamDef>, IMemberDef {
		internal static readonly UTF8String StaticConstructorName = ".cctor";
		internal static readonly UTF8String InstanceConstructorName = ".ctor";

		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <summary>
		/// All parameters
		/// </summary>
		protected ParameterList parameterList;

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.Method, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 0;

		/// <inheritdoc/>
		public int HasDeclSecurityTag => 1;

		/// <inheritdoc/>
		public int MemberRefParentTag => 3;

		/// <inheritdoc/>
		public int MethodDefOrRefTag => 0;

		/// <inheritdoc/>
		public int MemberForwardedTag => 1;

		/// <inheritdoc/>
		public int CustomAttributeTypeTag => 2;

		/// <inheritdoc/>
		public int TypeOrMethodDefTag => 1;

		/// <summary>
		/// From column Method.RVA
		/// </summary>
		public RVA RVA {
			get => rva;
			set => rva = value;
		}
		/// <summary/>
		protected RVA rva;

		/// <summary>
		/// From column Method.ImplFlags
		/// </summary>
		public MethodImplAttributes ImplAttributes {
			get => (MethodImplAttributes)implAttributes;
			set => implAttributes = (int)value;
		}
		/// <summary>Implementation attributes</summary>
		protected int implAttributes;

		/// <summary>
		/// From column Method.Flags
		/// </summary>
		public MethodAttributes Attributes {
			get => (MethodAttributes)attributes;
			set => attributes = (int)value;
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column Method.Name
		/// </summary>
		public UTF8String Name {
			get => name;
			set => name = value;
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column Method.Signature
		/// </summary>
		public CallingConventionSig Signature {
			get => signature;
			set => signature = value;
		}
		/// <summary/>
		protected CallingConventionSig signature;

		/// <summary>
		/// From column Method.ParamList
		/// </summary>
		public IList<ParamDef> ParamDefs {
			get {
				if (paramDefs is null)
					InitializeParamDefs();
				return paramDefs;
			}
		}
		/// <summary/>
		protected LazyList<ParamDef> paramDefs;
		/// <summary>Initializes <see cref="paramDefs"/></summary>
		protected virtual void InitializeParamDefs() =>
			Interlocked.CompareExchange(ref paramDefs, new LazyList<ParamDef>(this), null);

		/// <inheritdoc/>
		public IList<GenericParam> GenericParameters {
			get {
				if (genericParameters is null)
					InitializeGenericParameters();
				return genericParameters;
			}
		}
		/// <summary/>
		protected LazyList<GenericParam> genericParameters;
		/// <summary>Initializes <see cref="genericParameters"/></summary>
		protected virtual void InitializeGenericParameters() =>
			Interlocked.CompareExchange(ref genericParameters, new LazyList<GenericParam>(this), null);

		/// <inheritdoc/>
		public IList<DeclSecurity> DeclSecurities {
			get {
				if (declSecurities is null)
					InitializeDeclSecurities();
				return declSecurities;
			}
		}
		/// <summary/>
		protected IList<DeclSecurity> declSecurities;
		/// <summary>Initializes <see cref="declSecurities"/></summary>
		protected virtual void InitializeDeclSecurities() =>
			Interlocked.CompareExchange(ref declSecurities, new List<DeclSecurity>(), null);

		/// <inheritdoc/>
		public ImplMap ImplMap {
			get {
				if (!implMap_isInitialized)
					InitializeImplMap();
				return implMap;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				implMap = value;
				implMap_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected ImplMap implMap;
		/// <summary/>
		protected bool implMap_isInitialized;

		void InitializeImplMap() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (implMap_isInitialized)
				return;
			implMap = GetImplMap_NoLock();
			implMap_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="implMap"/></summary>
		protected virtual ImplMap GetImplMap_NoLock() => null;

		/// <summary>Reset <see cref="ImplMap"/></summary>
		protected void ResetImplMap() => implMap_isInitialized = false;

		/// <summary>
		/// Gets/sets the method body. See also <see cref="Body"/>
		/// </summary>
		public MethodBody MethodBody {
			get {
				if (!methodBody_isInitialized)
					InitializeMethodBody();
				return methodBody;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				methodBody = value;
				methodBody_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected MethodBody methodBody;
		/// <summary/>
		protected bool methodBody_isInitialized;

		void InitializeMethodBody() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (methodBody_isInitialized)
				return;
			methodBody = GetMethodBody_NoLock();
			methodBody_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Frees the method body if it has been loaded. This does nothing if <see cref="CanFreeMethodBody"/>
		/// returns <c>false</c>.
		/// </summary>
		public void FreeMethodBody() {
			if (!CanFreeMethodBody)
				return;
			if (!methodBody_isInitialized)
				return;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			methodBody = null;
			methodBody_isInitialized = false;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="methodBody"/></summary>
		protected virtual MethodBody GetMethodBody_NoLock() => null;

		/// <summary>
		/// true if <see cref="FreeMethodBody()"/> can free the method body
		/// </summary>
		protected virtual bool CanFreeMethodBody => true;

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
		public int HasCustomDebugInformationTag => 0;

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
		/// Gets the methods this method implements
		/// </summary>
		public IList<MethodOverride> Overrides {
			get {
				if (overrides is null)
					InitializeOverrides();
				return overrides;
			}
		}
		/// <summary/>
		protected IList<MethodOverride> overrides;
		/// <summary>Initializes <see cref="overrides"/></summary>
		protected virtual void InitializeOverrides() =>
			Interlocked.CompareExchange(ref overrides, new List<MethodOverride>(), null);

		/// <summary>
		/// Gets the export info or null if the method isn't exported to unmanaged code.
		/// </summary>
		public MethodExportInfo ExportInfo {
			get => exportInfo;
			set => exportInfo = value;
		}
		/// <summary/>
		protected MethodExportInfo exportInfo;

		/// <inheritdoc/>
		public bool HasCustomAttributes => CustomAttributes.Count > 0;

		/// <inheritdoc/>
		public bool HasDeclSecurities => DeclSecurities.Count > 0;

		/// <summary>
		/// <c>true</c> if <see cref="ParamDefs"/> is not empty
		/// </summary>
		public bool HasParamDefs => ParamDefs.Count > 0;

		/// <summary>
		/// Gets/sets the declaring type (owner type)
		/// </summary>
		public TypeDef DeclaringType {
			get => declaringType2;
			set {
				var currentDeclaringType = DeclaringType2;
				if (currentDeclaringType == value)
					return;
				if (!(currentDeclaringType is null))
					currentDeclaringType.Methods.Remove(this);	// Will set DeclaringType2 = null
				if (!(value is null))
					value.Methods.Add(this);	// Will set DeclaringType2 = value
			}
		}

		/// <inheritdoc/>
		ITypeDefOrRef IMemberRef.DeclaringType => declaringType2;

		/// <summary>
		/// Called by <see cref="DeclaringType"/> and should normally not be called by any user
		/// code. Use <see cref="DeclaringType"/> instead. Only call this if you must set the
		/// declaring type without inserting it in the declaring type's method list.
		/// </summary>
		public TypeDef DeclaringType2 {
			get => declaringType2;
			set => declaringType2 = value;
		}
		/// <summary/>
		protected TypeDef declaringType2;

		/// <inheritdoc/>
		public ModuleDef Module => declaringType2?.Module;

		bool IIsTypeOrMethod.IsType => false;
		bool IIsTypeOrMethod.IsMethod => true;
		bool IMemberRef.IsField => false;
		bool IMemberRef.IsTypeSpec => false;
		bool IMemberRef.IsTypeRef => false;
		bool IMemberRef.IsTypeDef => false;
		bool IMemberRef.IsMethodSpec => false;
		bool IMemberRef.IsMethodDef => true;
		bool IMemberRef.IsMemberRef => false;
		bool IMemberRef.IsFieldDef => false;
		bool IMemberRef.IsPropertyDef => false;
		bool IMemberRef.IsEventDef => false;
		bool IMemberRef.IsGenericParam => false;

		/// <summary>
		/// Gets/sets the CIL method body. See also <see cref="FreeMethodBody()"/>
		/// </summary>
		public CilBody Body {
			get {
				if (!methodBody_isInitialized)
					InitializeMethodBody();
				return methodBody as CilBody;
			}
			set => MethodBody = value;
		}

		/// <summary>
		/// Gets/sets the native method body
		/// </summary>
		public NativeMethodBody NativeBody {
			get {
				if (!methodBody_isInitialized)
					InitializeMethodBody();
				return methodBody as NativeMethodBody;
			}
			set => MethodBody = value;
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="GenericParam"/> in <see cref="GenericParameters"/>
		/// </summary>
		public bool HasGenericParameters => GenericParameters.Count > 0;

		/// <summary>
		/// <c>true</c> if it has a <see cref="Body"/>
		/// </summary>
		public bool HasBody => !(Body is null);

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="MethodOverride"/> in <see cref="Overrides"/>
		/// </summary>
		public bool HasOverrides => Overrides.Count > 0;

		/// <summary>
		/// <c>true</c> if <see cref="ImplMap"/> is not <c>null</c>
		/// </summary>
		public bool HasImplMap => !(ImplMap is null);

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string FullName => FullNameFactory.MethodFullName(declaringType2?.FullName, name, MethodSig, null, null, this, null);

		/// <summary>
		/// Gets/sets the <see cref="MethodSig"/>
		/// </summary>
		public MethodSig MethodSig {
			get => signature as MethodSig;
			set => signature = value;
		}

		/// <summary>
		/// Gets the parameters
		/// </summary>
		public ParameterList Parameters => parameterList;

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get {
				var sig = MethodSig;
				return sig is null ? 0 : (int)sig.GenParamCount;
			}
		}

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

		/// <summary>
		/// <c>true</c> if the method returns a value (i.e., return type is not <see cref="System.Void"/>)
		/// </summary>
		public bool HasReturnType => ReturnType.RemovePinnedAndModifiers().GetElementType() != ElementType.Void;

		/// <summary>
		/// Gets/sets the method semantics attributes. If you remove/add a method to a property or
		/// an event, you must manually update this property or eg. <see cref="IsSetter"/> won't
		/// work as expected.
		/// </summary>
		public MethodSemanticsAttributes SemanticsAttributes {
			get {
				if ((semAttrs & SEMATTRS_INITD) == 0)
					InitializeSemanticsAttributes();
				return (MethodSemanticsAttributes)semAttrs;
			}
			set => semAttrs = (ushort)value | SEMATTRS_INITD;
		}
		/// <summary>Set when <see cref="semAttrs"/> has been initialized</summary>
		protected internal static int SEMATTRS_INITD = unchecked((int)0x80000000);
		/// <summary/>
		protected internal int semAttrs;
		/// <summary>Initializes <see cref="semAttrs"/></summary>
		protected virtual void InitializeSemanticsAttributes() => semAttrs = 0 | SEMATTRS_INITD;

		/// <summary>
		/// Set or clear flags in <see cref="semAttrs"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, MethodSemanticsAttributes flags) {
			if ((semAttrs & SEMATTRS_INITD) == 0)
				InitializeSemanticsAttributes();
			if (set)
				semAttrs |= (int)flags;
			else
				semAttrs &= ~(int)flags;
		}

		/// <summary>
		/// Modify <see cref="attributes"/> property: <see cref="attributes"/> =
		/// (<see cref="attributes"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(MethodAttributes andMask, MethodAttributes orMask) =>
			attributes = (attributes & (int)andMask) | (int)orMask;

		/// <summary>
		/// Set or clear flags in <see cref="attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, MethodAttributes flags) {
			if (set)
				attributes |= (int)flags;
			else
				attributes &= ~(int)flags;
		}

		/// <summary>
		/// Modify <see cref="implAttributes"/> property: <see cref="implAttributes"/> =
		/// (<see cref="implAttributes"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyImplAttributes(MethodImplAttributes andMask, MethodImplAttributes orMask) =>
			implAttributes = (implAttributes & (int)andMask) | (int)orMask;

		/// <summary>
		/// Set or clear flags in <see cref="implAttributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyImplAttributes(bool set, MethodImplAttributes flags) {
			if (set)
				implAttributes |= (int)flags;
			else
				implAttributes &= ~(int)flags;
		}

		/// <summary>
		/// Gets/sets the method access
		/// </summary>
		public MethodAttributes Access {
			get => (MethodAttributes)attributes & MethodAttributes.MemberAccessMask;
			set => ModifyAttributes(~MethodAttributes.MemberAccessMask, value & MethodAttributes.MemberAccessMask);
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.PrivateScope"/> is set
		/// </summary>
		public bool IsCompilerControlled => IsPrivateScope;

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.PrivateScope"/> is set
		/// </summary>
		public bool IsPrivateScope => ((MethodAttributes)attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.PrivateScope;

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.Private"/> is set
		/// </summary>
		public bool IsPrivate => ((MethodAttributes)attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.FamANDAssem"/> is set
		/// </summary>
		public bool IsFamilyAndAssembly => ((MethodAttributes)attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem;

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.Assembly"/> is set
		/// </summary>
		public bool IsAssembly => ((MethodAttributes)attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.Family"/> is set
		/// </summary>
		public bool IsFamily => ((MethodAttributes)attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.FamORAssem"/> is set
		/// </summary>
		public bool IsFamilyOrAssembly => ((MethodAttributes)attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;

		/// <summary>
		/// <c>true</c> if <see cref="MethodAttributes.Public"/> is set
		/// </summary>
		public bool IsPublic => ((MethodAttributes)attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.Static"/> bit
		/// </summary>
		public bool IsStatic {
			get => ((MethodAttributes)attributes & MethodAttributes.Static) != 0;
			set => ModifyAttributes(value, MethodAttributes.Static);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.Final"/> bit
		/// </summary>
		public bool IsFinal {
			get => ((MethodAttributes)attributes & MethodAttributes.Final) != 0;
			set => ModifyAttributes(value, MethodAttributes.Final);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.Virtual"/> bit
		/// </summary>
		public bool IsVirtual {
			get => ((MethodAttributes)attributes & MethodAttributes.Virtual) != 0;
			set => ModifyAttributes(value, MethodAttributes.Virtual);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.HideBySig"/> bit
		/// </summary>
		public bool IsHideBySig {
			get => ((MethodAttributes)attributes & MethodAttributes.HideBySig) != 0;
			set => ModifyAttributes(value, MethodAttributes.HideBySig);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.NewSlot"/> bit
		/// </summary>
		public bool IsNewSlot {
			get => ((MethodAttributes)attributes & MethodAttributes.NewSlot) != 0;
			set => ModifyAttributes(value, MethodAttributes.NewSlot);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.ReuseSlot"/> bit
		/// </summary>
		public bool IsReuseSlot {
			get => ((MethodAttributes)attributes & MethodAttributes.NewSlot) == 0;
			set => ModifyAttributes(!value, MethodAttributes.NewSlot);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.CheckAccessOnOverride"/> bit
		/// </summary>
		public bool IsCheckAccessOnOverride {
			get => ((MethodAttributes)attributes & MethodAttributes.CheckAccessOnOverride) != 0;
			set => ModifyAttributes(value, MethodAttributes.CheckAccessOnOverride);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.Abstract"/> bit
		/// </summary>
		public bool IsAbstract {
			get => ((MethodAttributes)attributes & MethodAttributes.Abstract) != 0;
			set => ModifyAttributes(value, MethodAttributes.Abstract);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get => ((MethodAttributes)attributes & MethodAttributes.SpecialName) != 0;
			set => ModifyAttributes(value, MethodAttributes.SpecialName);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.PinvokeImpl"/> bit
		/// </summary>
		public bool IsPinvokeImpl {
			get => ((MethodAttributes)attributes & MethodAttributes.PinvokeImpl) != 0;
			set => ModifyAttributes(value, MethodAttributes.PinvokeImpl);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.UnmanagedExport"/> bit
		/// </summary>
		public bool IsUnmanagedExport {
			get => ((MethodAttributes)attributes & MethodAttributes.UnmanagedExport) != 0;
			set => ModifyAttributes(value, MethodAttributes.UnmanagedExport);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRuntimeSpecialName {
			get => ((MethodAttributes)attributes & MethodAttributes.RTSpecialName) != 0;
			set => ModifyAttributes(value, MethodAttributes.RTSpecialName);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.HasSecurity"/> bit
		/// </summary>
		public bool HasSecurity {
			get => ((MethodAttributes)attributes & MethodAttributes.HasSecurity) != 0;
			set => ModifyAttributes(value, MethodAttributes.HasSecurity);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodAttributes.RequireSecObject"/> bit
		/// </summary>
		public bool IsRequireSecObject {
			get => ((MethodAttributes)attributes & MethodAttributes.RequireSecObject) != 0;
			set => ModifyAttributes(value, MethodAttributes.RequireSecObject);
		}

		/// <summary>
		/// Gets/sets the code type
		/// </summary>
		public MethodImplAttributes CodeType {
			get => (MethodImplAttributes)implAttributes & MethodImplAttributes.CodeTypeMask;
			set => ModifyImplAttributes(~MethodImplAttributes.CodeTypeMask, value & MethodImplAttributes.CodeTypeMask);
		}

		/// <summary>
		/// <c>true</c> if <see cref="MethodImplAttributes.IL"/> is set
		/// </summary>
		public bool IsIL => ((MethodImplAttributes)implAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.IL;

		/// <summary>
		/// <c>true</c> if <see cref="MethodImplAttributes.Native"/> is set
		/// </summary>
		public bool IsNative => ((MethodImplAttributes)implAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Native;

		/// <summary>
		/// <c>true</c> if <see cref="MethodImplAttributes.OPTIL"/> is set
		/// </summary>
		public bool IsOPTIL => ((MethodImplAttributes)implAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.OPTIL;

		/// <summary>
		/// <c>true</c> if <see cref="MethodImplAttributes.Runtime"/> is set
		/// </summary>
		public bool IsRuntime => ((MethodImplAttributes)implAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Runtime;

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.Unmanaged"/> bit
		/// </summary>
		public bool IsUnmanaged {
			get => ((MethodImplAttributes)implAttributes & MethodImplAttributes.Unmanaged) != 0;
			set => ModifyImplAttributes(value, MethodImplAttributes.Unmanaged);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.Managed"/> bit
		/// </summary>
		public bool IsManaged {
			get => ((MethodImplAttributes)implAttributes & MethodImplAttributes.Unmanaged) == 0;
			set => ModifyImplAttributes(!value, MethodImplAttributes.Unmanaged);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.ForwardRef"/> bit
		/// </summary>
		public bool IsForwardRef {
			get => ((MethodImplAttributes)implAttributes & MethodImplAttributes.ForwardRef) != 0;
			set => ModifyImplAttributes(value, MethodImplAttributes.ForwardRef);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.PreserveSig"/> bit
		/// </summary>
		public bool IsPreserveSig {
			get => ((MethodImplAttributes)implAttributes & MethodImplAttributes.PreserveSig) != 0;
			set => ModifyImplAttributes(value, MethodImplAttributes.PreserveSig);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.InternalCall"/> bit
		/// </summary>
		public bool IsInternalCall {
			get => ((MethodImplAttributes)implAttributes & MethodImplAttributes.InternalCall) != 0;
			set => ModifyImplAttributes(value, MethodImplAttributes.InternalCall);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.Synchronized"/> bit
		/// </summary>
		public bool IsSynchronized {
			get => ((MethodImplAttributes)implAttributes & MethodImplAttributes.Synchronized) != 0;
			set => ModifyImplAttributes(value, MethodImplAttributes.Synchronized);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.NoInlining"/> bit
		/// </summary>
		public bool IsNoInlining {
			get => ((MethodImplAttributes)implAttributes & MethodImplAttributes.NoInlining) != 0;
			set => ModifyImplAttributes(value, MethodImplAttributes.NoInlining);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.AggressiveInlining"/> bit
		/// </summary>
		public bool IsAggressiveInlining {
			get => ((MethodImplAttributes)implAttributes & MethodImplAttributes.AggressiveInlining) != 0;
			set => ModifyImplAttributes(value, MethodImplAttributes.AggressiveInlining);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.NoOptimization"/> bit
		/// </summary>
		public bool IsNoOptimization {
			get => ((MethodImplAttributes)implAttributes & MethodImplAttributes.NoOptimization) != 0;
			set => ModifyImplAttributes(value, MethodImplAttributes.NoOptimization);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.AggressiveOptimization"/> bit
		/// </summary>
		public bool IsAggressiveOptimization {
			get => ((MethodImplAttributes)implAttributes & MethodImplAttributes.AggressiveOptimization) != 0;
			set => ModifyImplAttributes(value, MethodImplAttributes.AggressiveOptimization);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodImplAttributes.SecurityMitigations"/> bit
		/// </summary>
		public bool HasSecurityMitigations {
			get => ((MethodImplAttributes)implAttributes & MethodImplAttributes.SecurityMitigations) != 0;
			set => ModifyImplAttributes(value, MethodImplAttributes.SecurityMitigations);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSemanticsAttributes.Setter"/> bit
		/// </summary>
		public bool IsSetter {
			get => (SemanticsAttributes & MethodSemanticsAttributes.Setter) != 0;
			set => ModifyAttributes(value, MethodSemanticsAttributes.Setter);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSemanticsAttributes.Getter"/> bit
		/// </summary>
		public bool IsGetter {
			get => (SemanticsAttributes & MethodSemanticsAttributes.Getter) != 0;
			set => ModifyAttributes(value, MethodSemanticsAttributes.Getter);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSemanticsAttributes.Other"/> bit
		/// </summary>
		public bool IsOther {
			get => (SemanticsAttributes & MethodSemanticsAttributes.Other) != 0;
			set => ModifyAttributes(value, MethodSemanticsAttributes.Other);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSemanticsAttributes.AddOn"/> bit
		/// </summary>
		public bool IsAddOn {
			get => (SemanticsAttributes & MethodSemanticsAttributes.AddOn) != 0;
			set => ModifyAttributes(value, MethodSemanticsAttributes.AddOn);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSemanticsAttributes.RemoveOn"/> bit
		/// </summary>
		public bool IsRemoveOn {
			get => (SemanticsAttributes & MethodSemanticsAttributes.RemoveOn) != 0;
			set => ModifyAttributes(value, MethodSemanticsAttributes.RemoveOn);
		}

		/// <summary>
		/// Gets/sets the <see cref="MethodSemanticsAttributes.Fire"/> bit
		/// </summary>
		public bool IsFire {
			get => (SemanticsAttributes & MethodSemanticsAttributes.Fire) != 0;
			set => ModifyAttributes(value, MethodSemanticsAttributes.Fire);
		}

		/// <summary>
		/// <c>true</c> if this is the static type constructor
		/// </summary>
		public bool IsStaticConstructor => IsRuntimeSpecialName && UTF8String.Equals(name, StaticConstructorName);

		/// <summary>
		/// <c>true</c> if this is an instance constructor
		/// </summary>
		public bool IsInstanceConstructor => IsRuntimeSpecialName && UTF8String.Equals(name, InstanceConstructorName);

		/// <summary>
		/// <c>true</c> if this is a static or an instance constructor
		/// </summary>
		public bool IsConstructor => IsStaticConstructor || IsInstanceConstructor;

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnLazyAdd(int index, ref GenericParam value) => OnLazyAdd2(index, ref value);

		internal virtual void OnLazyAdd2(int index, ref GenericParam value) {
#if DEBUG
			if (value.Owner != this)
				throw new InvalidOperationException("Added generic param's Owner != this");
#endif
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnAdd(int index, GenericParam value) {
			if (!(value.Owner is null))
				throw new InvalidOperationException("Generic param is already owned by another type/method. Set Owner to null first.");
			value.Owner = this;
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnRemove(int index, GenericParam value) => value.Owner = null;

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<GenericParam>.OnClear() {
			foreach (var gp in genericParameters.GetEnumerable_NoLock())
				gp.Owner = null;
		}

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnLazyAdd(int index, ref ParamDef value) => OnLazyAdd2(index, ref value);

		internal virtual void OnLazyAdd2(int index, ref ParamDef value) {
#if DEBUG
			if (value.DeclaringMethod != this)
				throw new InvalidOperationException("Added param's DeclaringMethod != this");
#endif
		}

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnAdd(int index, ParamDef value) {
			if (!(value.DeclaringMethod is null))
				throw new InvalidOperationException("Param is already owned by another method. Set DeclaringMethod to null first.");
			value.DeclaringMethod = this;
		}

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnRemove(int index, ParamDef value) => value.DeclaringMethod = null;

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<ParamDef>.OnClear() {
			foreach (var pd in paramDefs.GetEnumerable_NoLock())
				pd.DeclaringMethod = null;
		}

		/// <inheritdoc/>
		public override string ToString() => FullName;
	}

	/// <summary>
	/// A Method row created by the user and not present in the original .NET file
	/// </summary>
	public class MethodDefUser : MethodDef {
		/// <summary>
		/// Default constructor
		/// </summary>
		public MethodDefUser() {
			paramDefs = new LazyList<ParamDef>(this);
			genericParameters = new LazyList<GenericParam>(this);
			parameterList = new ParameterList(this, null);
			semAttrs = 0 | SEMATTRS_INITD;
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
			signature = methodSig;
			paramDefs = new LazyList<ParamDef>(this);
			genericParameters = new LazyList<GenericParam>(this);
			implAttributes = (int)implFlags;
			attributes = (int)flags;
			parameterList = new ParameterList(this, null);
			semAttrs = 0 | SEMATTRS_INITD;
		}
	}

	/// <summary>
	/// Created from a row in the Method table
	/// </summary>
	sealed class MethodDefMD : MethodDef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;
		readonly RVA origRva;
		readonly MethodImplAttributes origImplAttributes;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override void InitializeParamDefs() {
			var list = readerModule.Metadata.GetParamRidList(origRid);
			var tmp = new LazyList<ParamDef, RidList>(list.Count, this, list, (list2, index) => readerModule.ResolveParam(list2[index]));
			Interlocked.CompareExchange(ref paramDefs, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeGenericParameters() {
			var list = readerModule.Metadata.GetGenericParamRidList(Table.Method, origRid);
			var tmp = new LazyList<GenericParam, RidList>(list.Count, this, list, (list2, index) => readerModule.ResolveGenericParam(list2[index]));
			Interlocked.CompareExchange(ref genericParameters, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeDeclSecurities() {
			var list = readerModule.Metadata.GetDeclSecurityRidList(Table.Method, origRid);
			var tmp = new LazyList<DeclSecurity, RidList>(list.Count, list, (list2, index) => readerModule.ResolveDeclSecurity(list2[index]));
			Interlocked.CompareExchange(ref declSecurities, tmp, null);
		}

		/// <inheritdoc/>
		protected override ImplMap GetImplMap_NoLock() => readerModule.ResolveImplMap(readerModule.Metadata.GetImplMapRid(Table.Method, origRid));

		/// <inheritdoc/>
		protected override MethodBody GetMethodBody_NoLock() => readerModule.ReadMethodBody(this, origRva, origImplAttributes, new GenericParamContext(declaringType2, this));

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.Method, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
			if (Interlocked.CompareExchange(ref customDebugInfos, list, null) is null) {
				var body = Body;
				readerModule.InitializeCustomDebugInfos(this, body, list);
			}
		}

		/// <inheritdoc/>
		protected override void InitializeOverrides() {
			var dt = declaringType2 as TypeDefMD;
			var tmp = dt is null ? new List<MethodOverride>() : dt.GetMethodOverrides(this, new GenericParamContext(declaringType2, this));
			Interlocked.CompareExchange(ref overrides, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeSemanticsAttributes() {
			if (DeclaringType is TypeDefMD dt)
				dt.InitializeMethodSemanticsAttributes();
			semAttrs |= SEMATTRS_INITD;
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
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.MethodTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"Method rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			bool b = readerModule.TablesStream.TryReadMethodRow(origRid, out var row);
			Debug.Assert(b);
			rva = (RVA)row.RVA;
			implAttributes = row.ImplFlags;
			attributes = row.Flags;
			name = readerModule.StringsStream.ReadNoNull(row.Name);
			origRva = rva;
			origImplAttributes = (MethodImplAttributes)implAttributes;
			declaringType2 = readerModule.GetOwnerType(this);
			signature = readerModule.ReadSignature(row.Signature, new GenericParamContext(declaringType2, this));
			parameterList = new ParameterList(this, declaringType2);
			exportInfo = readerModule.GetExportInfo(rid);
		}

		internal MethodDefMD InitializeAll() {
			MemberMDInitializer.Initialize(RVA);
			MemberMDInitializer.Initialize(Attributes);
			MemberMDInitializer.Initialize(ImplAttributes);
			MemberMDInitializer.Initialize(Name);
			MemberMDInitializer.Initialize(Signature);
			MemberMDInitializer.Initialize(ImplMap);
			MemberMDInitializer.Initialize(MethodBody);
			MemberMDInitializer.Initialize(DeclaringType);
			MemberMDInitializer.Initialize(CustomAttributes);
			MemberMDInitializer.Initialize(Overrides);
			MemberMDInitializer.Initialize(ParamDefs);
			MemberMDInitializer.Initialize(GenericParameters);
			MemberMDInitializer.Initialize(DeclSecurities);
			return this;
		}

		/// <inheritdoc/>
		internal override void OnLazyAdd2(int index, ref GenericParam value) {
			if (value.Owner != this) {
				// More than one owner... This module has invalid metadata.
				value = readerModule.ForceUpdateRowId(readerModule.ReadGenericParam(value.Rid).InitializeAll());
				value.Owner = this;
			}
		}

		/// <inheritdoc/>
		internal override void OnLazyAdd2(int index, ref ParamDef value) {
			if (value.DeclaringMethod != this) {
				// More than one owner... This module has invalid metadata.
				value = readerModule.ForceUpdateRowId(readerModule.ReadParam(value.Rid).InitializeAll());
				value.DeclaringMethod = this;
			}
		}
	}
}
