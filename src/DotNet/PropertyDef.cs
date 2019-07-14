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
	/// A high-level representation of a row in the Property table
	/// </summary>
	public abstract class PropertyDef : IHasConstant, IHasCustomAttribute, IHasSemantic, IHasCustomDebugInformation, IFullName, IMemberDef {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.Property, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		public int HasConstantTag => 2;

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 9;

		/// <inheritdoc/>
		public int HasSemanticTag => 1;

		/// <summary>
		/// From column Property.PropFlags
		/// </summary>
		public PropertyAttributes Attributes {
			get => (PropertyAttributes)attributes;
			set => attributes = (int)value;
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column Property.Name
		/// </summary>
		public UTF8String Name {
			get => name;
			set => name = value;
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column Property.Type
		/// </summary>
		public CallingConventionSig Type {
			get => type;
			set => type = value;
		}
		/// <summary/>
		protected CallingConventionSig type;

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
		public int HasCustomDebugInformationTag => 9;

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
		/// Gets/sets the first getter method. Writing <c>null</c> will clear all get methods.
		/// </summary>
		public MethodDef GetMethod {
			get {
				if (otherMethods is null)
					InitializePropertyMethods();
				return getMethods.Count == 0 ? null : getMethods[0];
			}
			set {
				if (otherMethods is null)
					InitializePropertyMethods();
				if (value is null)
					getMethods.Clear();
				else if (getMethods.Count == 0)
					getMethods.Add(value);
				else
					getMethods[0] = value;
			}
		}

		/// <summary>
		/// Gets/sets the first setter method. Writing <c>null</c> will clear all set methods.
		/// </summary>
		public MethodDef SetMethod {
			get {
				if (otherMethods is null)
					InitializePropertyMethods();
				return setMethods.Count == 0 ? null : setMethods[0];
			}
			set {
				if (otherMethods is null)
					InitializePropertyMethods();
				if (value is null)
					setMethods.Clear();
				else if (setMethods.Count == 0)
					setMethods.Add(value);
				else
					setMethods[0] = value;
			}
		}

		/// <summary>
		/// Gets all getter methods
		/// </summary>
		public IList<MethodDef> GetMethods {
			get {
				if (otherMethods is null)
					InitializePropertyMethods();
				return getMethods;
			}
		}

		/// <summary>
		/// Gets all setter methods
		/// </summary>
		public IList<MethodDef> SetMethods {
			get {
				if (otherMethods is null)
					InitializePropertyMethods();
				return setMethods;
			}
		}

		/// <summary>
		/// Gets the other methods
		/// </summary>
		public IList<MethodDef> OtherMethods {
			get {
				if (otherMethods is null)
					InitializePropertyMethods();
				return otherMethods;
			}
		}

		void InitializePropertyMethods() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (otherMethods is null)
				InitializePropertyMethods_NoLock();
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Initializes <see cref="otherMethods"/>, <see cref="getMethods"/>,
		/// and <see cref="setMethods"/>.
		/// </summary>
		protected virtual void InitializePropertyMethods_NoLock() {
			getMethods = new List<MethodDef>();
			setMethods = new List<MethodDef>();
			otherMethods = new List<MethodDef>();
		}

		/// <summary/>
		protected IList<MethodDef> getMethods;
		/// <summary/>
		protected IList<MethodDef> setMethods;
		/// <summary/>
		protected IList<MethodDef> otherMethods;

		/// <summary>Reset <see cref="GetMethods"/>, <see cref="SetMethods"/>, <see cref="OtherMethods"/></summary>
		protected void ResetMethods() => otherMethods = null;

		/// <summary>
		/// <c>true</c> if there are no methods attached to this property
		/// </summary>
		public bool IsEmpty =>
			// The first property access initializes the other fields we access here
			GetMethods.Count == 0 &&
			setMethods.Count == 0 &&
			otherMethods.Count == 0;

		/// <inheritdoc/>
		public bool HasCustomAttributes => CustomAttributes.Count > 0;

		/// <summary>
		/// <c>true</c> if <see cref="OtherMethods"/> is not empty
		/// </summary>
		public bool HasOtherMethods => OtherMethods.Count > 0;

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
		/// Gets/sets the property sig
		/// </summary>
		public PropertySig PropertySig {
			get => type as PropertySig;
			set => type = value;
		}

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
					currentDeclaringType.Properties.Remove(this);	// Will set DeclaringType2 = null
				if (!(value is null))
					value.Properties.Add(this);	// Will set DeclaringType2 = value
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

		/// <summary>
		/// Gets the full name of the property
		/// </summary>
		public string FullName => FullNameFactory.PropertyFullName(declaringType2?.FullName, name, type, null, null);

		bool IIsTypeOrMethod.IsType => false;
		bool IIsTypeOrMethod.IsMethod => false;
		bool IMemberRef.IsField => false;
		bool IMemberRef.IsTypeSpec => false;
		bool IMemberRef.IsTypeRef => false;
		bool IMemberRef.IsTypeDef => false;
		bool IMemberRef.IsMethodSpec => false;
		bool IMemberRef.IsMethodDef => false;
		bool IMemberRef.IsMemberRef => false;
		bool IMemberRef.IsFieldDef => false;
		bool IMemberRef.IsPropertyDef => true;
		bool IMemberRef.IsEventDef => false;
		bool IMemberRef.IsGenericParam => false;

		/// <summary>
		/// Set or clear flags in <see cref="attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, PropertyAttributes flags) {
			if (set)
				attributes |= (int)flags;
			else
				attributes &= ~(int)flags;
		}

		/// <summary>
		/// Gets/sets the <see cref="PropertyAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get => ((PropertyAttributes)attributes & PropertyAttributes.SpecialName) != 0;
			set => ModifyAttributes(value, PropertyAttributes.SpecialName);
		}

		/// <summary>
		/// Gets/sets the <see cref="PropertyAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRuntimeSpecialName {
			get => ((PropertyAttributes)attributes & PropertyAttributes.RTSpecialName) != 0;
			set => ModifyAttributes(value, PropertyAttributes.RTSpecialName);
		}

		/// <summary>
		/// Gets/sets the <see cref="PropertyAttributes.HasDefault"/> bit
		/// </summary>
		public bool HasDefault {
			get => ((PropertyAttributes)attributes & PropertyAttributes.HasDefault) != 0;
			set => ModifyAttributes(value, PropertyAttributes.HasDefault);
		}

		/// <inheritdoc/>
		public override string ToString() => FullName;
	}

	/// <summary>
	/// A Property row created by the user and not present in the original .NET file
	/// </summary>
	public class PropertyDefUser : PropertyDef {
		/// <summary>
		/// Default constructor
		/// </summary>
		public PropertyDefUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public PropertyDefUser(UTF8String name)
			: this(name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sig">Property signature</param>
		public PropertyDefUser(UTF8String name, PropertySig sig)
			: this(name, sig, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sig">Property signature</param>
		/// <param name="flags">Flags</param>
		public PropertyDefUser(UTF8String name, PropertySig sig, PropertyAttributes flags) {
			this.name = name;
			type = sig;
			attributes = (int)flags;
		}
	}

	/// <summary>
	/// Created from a row in the Property table
	/// </summary>
	sealed class PropertyDefMD : PropertyDef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override Constant GetConstant_NoLock() => readerModule.ResolveConstant(readerModule.Metadata.GetConstantRid(Table.Property, origRid));

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.Property, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), new GenericParamContext(declaringType2), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Property</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public PropertyDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.PropertyTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"Property rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			bool b = readerModule.TablesStream.TryReadPropertyRow(origRid, out var row);
			Debug.Assert(b);
			attributes = row.PropFlags;
			name = readerModule.StringsStream.ReadNoNull(row.Name);
			declaringType2 = readerModule.GetOwnerType(this);
			type = readerModule.ReadSignature(row.Type, new GenericParamContext(declaringType2));
		}

		internal PropertyDefMD InitializeAll() {
			MemberMDInitializer.Initialize(Attributes);
			MemberMDInitializer.Initialize(Name);
			MemberMDInitializer.Initialize(Type);
			MemberMDInitializer.Initialize(Constant);
			MemberMDInitializer.Initialize(CustomAttributes);
			MemberMDInitializer.Initialize(GetMethod);
			MemberMDInitializer.Initialize(SetMethod);
			MemberMDInitializer.Initialize(OtherMethods);
			MemberMDInitializer.Initialize(DeclaringType);
			return this;
		}

		/// <inheritdoc/>
		protected override void InitializePropertyMethods_NoLock() {
			if (!(otherMethods is null))
				return;
			IList<MethodDef> newOtherMethods;
			IList<MethodDef> newGetMethods, newSetMethods;
			var dt = declaringType2 as TypeDefMD;
			if (dt is null) {
				newGetMethods = new List<MethodDef>();
				newSetMethods = new List<MethodDef>();
				newOtherMethods = new List<MethodDef>();
			}
			else
				dt.InitializeProperty(this, out newGetMethods, out newSetMethods, out newOtherMethods);
			getMethods = newGetMethods;
			setMethods = newSetMethods;
			// Must be initialized last
			otherMethods = newOtherMethods;
		}
	}
}
