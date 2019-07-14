// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using dnlib.PE;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Field table
	/// </summary>
	public abstract class FieldDef : IHasConstant, IHasCustomAttribute, IHasFieldMarshal, IMemberForwarded, IHasCustomDebugInformation, IField, ITokenOperand, IMemberDef {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.Field, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		public int HasConstantTag => 0;

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 1;

		/// <inheritdoc/>
		public int HasFieldMarshalTag => 0;

		/// <inheritdoc/>
		public int MemberForwardedTag => 0;

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
		public int HasCustomDebugInformationTag => 1;

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
		/// From column Field.Flags
		/// </summary>
		public FieldAttributes Attributes {
			get => (FieldAttributes)attributes;
			set => attributes = (int)value;
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column Field.Name
		/// </summary>
		public UTF8String Name {
			get => name;
			set => name = value;
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column Field.Signature
		/// </summary>
		public CallingConventionSig Signature {
			get => signature;
			set => signature = value;
		}
		/// <summary/>
		protected CallingConventionSig signature;

		/// <summary>
		/// Gets/sets the field layout offset
		/// </summary>
		public uint? FieldOffset {
			get {
				if (!fieldOffset_isInitialized)
					InitializeFieldOffset();
				return fieldOffset;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				fieldOffset = value;
				fieldOffset_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected uint? fieldOffset;
		/// <summary/>
		protected bool fieldOffset_isInitialized;

		void InitializeFieldOffset() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (fieldOffset_isInitialized)
				return;
			fieldOffset = GetFieldOffset_NoLock();
			fieldOffset_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="fieldOffset"/></summary>
		protected virtual uint? GetFieldOffset_NoLock() => null;

		/// <inheritdoc/>
		public MarshalType MarshalType {
			get {
				if (!marshalType_isInitialized)
					InitializeMarshalType();
				return marshalType;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				marshalType = value;
				marshalType_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected MarshalType marshalType;
		/// <summary/>
		protected bool marshalType_isInitialized;

		void InitializeMarshalType() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (marshalType_isInitialized)
				return;
			marshalType = GetMarshalType_NoLock();
			marshalType_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="marshalType"/></summary>
		protected virtual MarshalType GetMarshalType_NoLock() => null;

		/// <summary>Reset <see cref="MarshalType"/></summary>
		protected void ResetMarshalType() =>
			marshalType_isInitialized = false;

		/// <summary>
		/// Gets/sets the field RVA
		/// </summary>
		public RVA RVA {
			get {
				if (!rva_isInitialized)
					InitializeRVA();
				return rva;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				rva = value;
				rva_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected RVA rva;
		/// <summary/>
		protected bool rva_isInitialized;

		void InitializeRVA() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (rva_isInitialized)
				return;
			rva = GetRVA_NoLock();
			rva_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="rva"/></summary>
		protected virtual RVA GetRVA_NoLock() => 0;

		/// <summary>Reset <see cref="RVA"/></summary>
		protected void ResetRVA() => rva_isInitialized = false;

		/// <summary>
		/// Gets/sets the initial value. Be sure to set <see cref="HasFieldRVA"/> to <c>true</c> if
		/// you write to this field.
		/// </summary>
		public byte[] InitialValue {
			get {
				if (!initialValue_isInitialized)
					InitializeInitialValue();
				return initialValue;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				initialValue = value;
				initialValue_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected byte[] initialValue;
		/// <summary/>
		protected bool initialValue_isInitialized;

		void InitializeInitialValue() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (initialValue_isInitialized)
				return;
			initialValue = GetInitialValue_NoLock();
			initialValue_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="initialValue"/></summary>
		protected virtual byte[] GetInitialValue_NoLock() => null;

		/// <summary>Reset <see cref="InitialValue"/></summary>
		protected void ResetInitialValue() => initialValue_isInitialized = false;

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

		/// <inheritdoc/>
		public Constant Constant {
			get {
				if (!constant_isInitialized)
					InitializeConstant();
				return constant;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				constant = value;
				constant_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected Constant constant;
		/// <summary/>
		protected bool constant_isInitialized;

		void InitializeConstant() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (constant_isInitialized)
				return;
			constant = GetConstant_NoLock();
			constant_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="constant"/></summary>
		protected virtual Constant GetConstant_NoLock() => null;

		/// <summary>Reset <see cref="Constant"/></summary>
		protected void ResetConstant() => constant_isInitialized = false;

		/// <inheritdoc/>
		public bool HasCustomAttributes => CustomAttributes.Count > 0;

		/// <inheritdoc/>
		public bool HasImplMap => !(ImplMap is null);

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
					currentDeclaringType.Fields.Remove(this);	// Will set DeclaringType2 = null
				if (!(value is null))
					value.Fields.Add(this);		// Will set DeclaringType2 = value
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

		/// <summary>
		/// Gets/sets the <see cref="FieldSig"/>
		/// </summary>
		public FieldSig FieldSig {
			get => signature as FieldSig;
			set => signature = value;
		}

		/// <inheritdoc/>
		public ModuleDef Module => declaringType2?.Module;

		bool IIsTypeOrMethod.IsType => false;
		bool IIsTypeOrMethod.IsMethod => false;
		bool IMemberRef.IsField => true;
		bool IMemberRef.IsTypeSpec => false;
		bool IMemberRef.IsTypeRef => false;
		bool IMemberRef.IsTypeDef => false;
		bool IMemberRef.IsMethodSpec => false;
		bool IMemberRef.IsMethodDef => false;
		bool IMemberRef.IsMemberRef => false;
		bool IMemberRef.IsFieldDef => true;
		bool IMemberRef.IsPropertyDef => false;
		bool IMemberRef.IsEventDef => false;
		bool IMemberRef.IsGenericParam => false;

		/// <summary>
		/// <c>true</c> if <see cref="FieldOffset"/> is not <c>null</c>
		/// </summary>
		public bool HasLayoutInfo => !(FieldOffset is null);

		/// <summary>
		/// <c>true</c> if <see cref="Constant"/> is not <c>null</c>
		/// </summary>
		public bool HasConstant => !(Constant is null);

		/// <summary>
		/// Gets the constant element type or <see cref="dnlib.DotNet.ElementType.End"/> if there's no constant
		/// </summary>
		public ElementType ElementType {
			get {
				var c = Constant;
				return c is null ? ElementType.End : c.Type;
			}
		}

		/// <summary>
		/// <c>true</c> if <see cref="MarshalType"/> is not <c>null</c>
		/// </summary>
		public bool HasMarshalType => !(MarshalType is null);

		/// <summary>
		/// Gets/sets the field type
		/// </summary>
		public TypeSig FieldType {
			get => FieldSig.GetFieldType();
			set {
				var sig = FieldSig;
				if (!(sig is null))
					sig.Type = value;
			}
		}

		/// <summary>
		/// Modify <see cref="attributes"/> field: <see cref="attributes"/> =
		/// (<see cref="attributes"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(FieldAttributes andMask, FieldAttributes orMask) =>
			attributes = (attributes & (int)andMask) | (int)orMask;

		/// <summary>
		/// Set or clear flags in <see cref="attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, FieldAttributes flags) {
			if (set)
				attributes |= (int)flags;
			else
				attributes &= ~(int)flags;
		}

		/// <summary>
		/// Gets/sets the field access
		/// </summary>
		public FieldAttributes Access {
			get => (FieldAttributes)attributes & FieldAttributes.FieldAccessMask;
			set => ModifyAttributes(~FieldAttributes.FieldAccessMask, value & FieldAttributes.FieldAccessMask);
		}

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.PrivateScope"/> is set
		/// </summary>
		public bool IsCompilerControlled => IsPrivateScope;

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.PrivateScope"/> is set
		/// </summary>
		public bool IsPrivateScope => ((FieldAttributes)attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.PrivateScope;

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.Private"/> is set
		/// </summary>
		public bool IsPrivate => ((FieldAttributes)attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.FamANDAssem"/> is set
		/// </summary>
		public bool IsFamilyAndAssembly => ((FieldAttributes)attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamANDAssem;

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.Assembly"/> is set
		/// </summary>
		public bool IsAssembly => ((FieldAttributes)attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly;

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.Family"/> is set
		/// </summary>
		public bool IsFamily => ((FieldAttributes)attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family;

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.FamORAssem"/> is set
		/// </summary>
		public bool IsFamilyOrAssembly => ((FieldAttributes)attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem;

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.Public"/> is set
		/// </summary>
		public bool IsPublic => ((FieldAttributes)attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.Static"/> bit
		/// </summary>
		public bool IsStatic {
			get => ((FieldAttributes)attributes & FieldAttributes.Static) != 0;
			set => ModifyAttributes(value, FieldAttributes.Static);
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.InitOnly"/> bit
		/// </summary>
		public bool IsInitOnly {
			get => ((FieldAttributes)attributes & FieldAttributes.InitOnly) != 0;
			set => ModifyAttributes(value, FieldAttributes.InitOnly);
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.Literal"/> bit
		/// </summary>
		public bool IsLiteral {
			get => ((FieldAttributes)attributes & FieldAttributes.Literal) != 0;
			set => ModifyAttributes(value, FieldAttributes.Literal);
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.NotSerialized"/> bit
		/// </summary>
		public bool IsNotSerialized {
			get => ((FieldAttributes)attributes & FieldAttributes.NotSerialized) != 0;
			set => ModifyAttributes(value, FieldAttributes.NotSerialized);
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get => ((FieldAttributes)attributes & FieldAttributes.SpecialName) != 0;
			set => ModifyAttributes(value, FieldAttributes.SpecialName);
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.PinvokeImpl"/> bit
		/// </summary>
		public bool IsPinvokeImpl {
			get => ((FieldAttributes)attributes & FieldAttributes.PinvokeImpl) != 0;
			set => ModifyAttributes(value, FieldAttributes.PinvokeImpl);
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRuntimeSpecialName {
			get => ((FieldAttributes)attributes & FieldAttributes.RTSpecialName) != 0;
			set => ModifyAttributes(value, FieldAttributes.RTSpecialName);
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.HasFieldMarshal"/> bit
		/// </summary>
		public bool HasFieldMarshal {
			get => ((FieldAttributes)attributes & FieldAttributes.HasFieldMarshal) != 0;
			set => ModifyAttributes(value, FieldAttributes.HasFieldMarshal);
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.HasDefault"/> bit
		/// </summary>
		public bool HasDefault {
			get => ((FieldAttributes)attributes & FieldAttributes.HasDefault) != 0;
			set => ModifyAttributes(value, FieldAttributes.HasDefault);
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.HasFieldRVA"/> bit
		/// </summary>
		public bool HasFieldRVA {
			get => ((FieldAttributes)attributes & FieldAttributes.HasFieldRVA) != 0;
			set => ModifyAttributes(value, FieldAttributes.HasFieldRVA);
		}

		/// <summary>
		/// Returns the full name of this field
		/// </summary>
		public string FullName => FullNameFactory.FieldFullName(declaringType2?.FullName, name, FieldSig, null, null);

		/// <summary>
		/// Gets the size of this field in bytes or <c>0</c> if unknown.
		/// </summary>
		public uint GetFieldSize() {
			if (!GetFieldSize(out uint size))
				return 0;
			return size;
		}

		/// <summary>
		/// Gets the size of this field in bytes or <c>0</c> if unknown.
		/// </summary>
		/// <param name="size">Updated with size</param>
		/// <returns><c>true</c> if <paramref name="size"/> is valid, <c>false</c> otherwise</returns>
		public bool GetFieldSize(out uint size) => GetFieldSize(declaringType2, FieldSig, out size);

		/// <summary>
		/// Gets the size of this field in bytes or <c>0</c> if unknown.
		/// </summary>
		/// <param name="declaringType">The declaring type of <c>this</c></param>
		/// <param name="fieldSig">The field signature of <c>this</c></param>
		/// <param name="size">Updated with size</param>
		/// <returns><c>true</c> if <paramref name="size"/> is valid, <c>false</c> otherwise</returns>
		protected bool GetFieldSize(TypeDef declaringType, FieldSig fieldSig, out uint size) => GetFieldSize(declaringType, fieldSig, GetPointerSize(declaringType), out size);

		/// <summary>
		/// Gets the size of this field in bytes or <c>0</c> if unknown.
		/// </summary>
		/// <param name="declaringType">The declaring type of <c>this</c></param>
		/// <param name="fieldSig">The field signature of <c>this</c></param>
		/// <param name="ptrSize">Size of a pointer</param>
		/// <param name="size">Updated with size</param>
		/// <returns><c>true</c> if <paramref name="size"/> is valid, <c>false</c> otherwise</returns>
		protected bool GetFieldSize(TypeDef declaringType, FieldSig fieldSig, int ptrSize, out uint size) {
			size = 0;
			if (fieldSig is null)
				return false;
			return GetClassSize(declaringType, fieldSig.Type, ptrSize, out size);
		}

		bool GetClassSize(TypeDef declaringType, TypeSig ts, int ptrSize, out uint size) {
			size = 0;
			ts = ts.RemovePinnedAndModifiers();
			if (ts is null)
				return false;

			int size2 = ts.ElementType.GetPrimitiveSize(ptrSize);
			if (size2 >= 0) {
				size = (uint)size2;
				return true;
			}

			var tdrs = ts as TypeDefOrRefSig;
			if (tdrs is null)
				return false;

			var td = tdrs.TypeDef;
			if (!(td is null))
				return TypeDef.GetClassSize(td, out size);

			var tr = tdrs.TypeRef;
			if (!(tr is null))
				return TypeDef.GetClassSize(tr.Resolve(), out size);

			return false;
		}

		int GetPointerSize(TypeDef declaringType) {
			if (declaringType is null)
				return 4;
			var module = declaringType.Module;
			if (module is null)
				return 4;
			return module.GetPointerSize();
		}

		/// <inheritdoc/>
		public override string ToString() => FullName;
	}

	/// <summary>
	/// A Field row created by the user and not present in the original .NET file
	/// </summary>
	public class FieldDefUser : FieldDef {
		/// <summary>
		/// Default constructor
		/// </summary>
		public FieldDefUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public FieldDefUser(UTF8String name)
			: this(name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="signature">Signature</param>
		public FieldDefUser(UTF8String name, FieldSig signature)
			: this(name, signature, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="signature">Signature</param>
		/// <param name="attributes">Flags</param>
		public FieldDefUser(UTF8String name, FieldSig signature, FieldAttributes attributes) {
			this.name = name;
			this.signature = signature;
			this.attributes = (int)attributes;
		}
	}

	/// <summary>
	/// Created from a row in the Field table
	/// </summary>
	sealed class FieldDefMD : FieldDef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;
		readonly FieldAttributes origAttributes;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.Field, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), new GenericParamContext(declaringType2), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <inheritdoc/>
		protected override uint? GetFieldOffset_NoLock() {
			if (readerModule.TablesStream.TryReadFieldLayoutRow(readerModule.Metadata.GetFieldLayoutRid(origRid), out var row))
				return row.OffSet;
			return null;
		}

		/// <inheritdoc/>
		protected override MarshalType GetMarshalType_NoLock() =>
			readerModule.ReadMarshalType(Table.Field, origRid, new GenericParamContext(declaringType2));

		/// <inheritdoc/>
		protected override RVA GetRVA_NoLock() {
			GetFieldRVA_NoLock(out var rva2);
			return rva2;
		}

		/// <inheritdoc/>
		protected override byte[] GetInitialValue_NoLock() {
			if (!GetFieldRVA_NoLock(out var rva2))
				return null;
			return ReadInitialValue_NoLock(rva2);
		}

		/// <inheritdoc/>
		protected override ImplMap GetImplMap_NoLock() =>
			readerModule.ResolveImplMap(readerModule.Metadata.GetImplMapRid(Table.Field, origRid));

		/// <inheritdoc/>
		protected override Constant GetConstant_NoLock() =>
			readerModule.ResolveConstant(readerModule.Metadata.GetConstantRid(Table.Field, origRid));

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Field</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public FieldDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.FieldTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"Field rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			bool b = readerModule.TablesStream.TryReadFieldRow(origRid, out var row);
			Debug.Assert(b);
			name = readerModule.StringsStream.ReadNoNull(row.Name);
			attributes = row.Flags;
			origAttributes = (FieldAttributes)attributes;
			declaringType2 = readerModule.GetOwnerType(this);
			signature = readerModule.ReadSignature(row.Signature, new GenericParamContext(declaringType2));
		}

		internal FieldDefMD InitializeAll() {
			MemberMDInitializer.Initialize(CustomAttributes);
			MemberMDInitializer.Initialize(Attributes);
			MemberMDInitializer.Initialize(Name);
			MemberMDInitializer.Initialize(Signature);
			MemberMDInitializer.Initialize(FieldOffset);
			MemberMDInitializer.Initialize(MarshalType);
			MemberMDInitializer.Initialize(RVA);
			MemberMDInitializer.Initialize(InitialValue);
			MemberMDInitializer.Initialize(ImplMap);
			MemberMDInitializer.Initialize(Constant);
			MemberMDInitializer.Initialize(DeclaringType);
			return this;
		}

		bool GetFieldRVA_NoLock(out RVA rva) {
			if ((origAttributes & FieldAttributes.HasFieldRVA) == 0) {
				rva = 0;
				return false;
			}
			if (!readerModule.TablesStream.TryReadFieldRVARow(readerModule.Metadata.GetFieldRVARid(origRid), out var row)) {
				rva = 0;
				return false;
			}
			rva = (RVA)row.RVA;
			return true;
		}

		byte[] ReadInitialValue_NoLock(RVA rva) {
			if (!GetFieldSize(declaringType2, signature as FieldSig, out uint size))
				return null;
			if (size >= int.MaxValue)
				return null;
			return readerModule.ReadDataAt(rva, (int)size);
		}
	}
}
