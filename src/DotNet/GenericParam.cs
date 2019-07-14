// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Threading;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using System.Collections.Generic;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the GenericParam table
	/// </summary>
	[DebuggerDisplay("{Name.String}")]
	public abstract class GenericParam : IHasCustomAttribute, IHasCustomDebugInformation, IMemberDef, IListListener<GenericParamConstraint> {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.GenericParam, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 19;

		/// <summary>
		/// Gets the owner type/method
		/// </summary>
		public ITypeOrMethodDef Owner {
			get => owner;
			internal set => owner = value;
		}
		/// <summary/>
		protected ITypeOrMethodDef owner;

		/// <summary>
		/// Gets the declaring type or <c>null</c> if none or if <see cref="Owner"/> is
		/// not a <see cref="TypeDef"/>
		/// </summary>
		public TypeDef DeclaringType => owner as TypeDef;

		/// <inheritdoc/>
		ITypeDefOrRef IMemberRef.DeclaringType => owner as TypeDef;

		/// <summary>
		/// Gets the declaring method or <c>null</c> if none or if <see cref="Owner"/> is
		/// not a <see cref="MethodDef"/>
		/// </summary>
		public MethodDef DeclaringMethod => owner as MethodDef;

		/// <summary>
		/// From column GenericParam.Number
		/// </summary>
		public ushort Number {
			get => number;
			set => number = value;
		}
		/// <summary/>
		protected ushort number;

		/// <summary>
		/// From column GenericParam.Flags
		/// </summary>
		public GenericParamAttributes Flags {
			get => (GenericParamAttributes)attributes;
			set => attributes = (int)value;
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column GenericParam.Name
		/// </summary>
		public UTF8String Name {
			get => name;
			set => name = value;
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column GenericParam.Kind (v1.1 only)
		/// </summary>
		public ITypeDefOrRef Kind {
			get => kind;
			set => kind = value;
		}
		/// <summary/>
		protected ITypeDefOrRef kind;

		/// <summary>
		/// Gets the generic param constraints
		/// </summary>
		public IList<GenericParamConstraint> GenericParamConstraints {
			get {
				if (genericParamConstraints is null)
					InitializeGenericParamConstraints();
				return genericParamConstraints;
			}
		}
		/// <summary/>
		protected LazyList<GenericParamConstraint> genericParamConstraints;
		/// <summary>Initializes <see cref="genericParamConstraints"/></summary>
		protected virtual void InitializeGenericParamConstraints() =>
			Interlocked.CompareExchange(ref genericParamConstraints, new LazyList<GenericParamConstraint>(this), null);

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
		public int HasCustomDebugInformationTag => 19;

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
		/// <c>true</c> if <see cref="GenericParamConstraints"/> is not empty
		/// </summary>
		public bool HasGenericParamConstraints => GenericParamConstraints.Count > 0;

		/// <inheritdoc/>
		public ModuleDef Module => owner?.Module;

		/// <inheritdoc/>
		public string FullName => UTF8String.ToSystemStringOrEmpty(name);

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
		bool IMemberRef.IsPropertyDef => false;
		bool IMemberRef.IsEventDef => false;
		bool IMemberRef.IsGenericParam => true;

		/// <summary>
		/// Modify <see cref="attributes"/> property: <see cref="attributes"/> =
		/// (<see cref="attributes"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(GenericParamAttributes andMask, GenericParamAttributes orMask) =>
			attributes = (attributes & (int)andMask) | (int)orMask;

		/// <summary>
		/// Set or clear flags in <see cref="attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, GenericParamAttributes flags) {
			if (set)
				attributes |= (int)flags;
			else
				attributes &= ~(int)flags;
		}

		/// <summary>
		/// Gets/sets variance (non, contra, co)
		/// </summary>
		public GenericParamAttributes Variance {
			get => (GenericParamAttributes)attributes & GenericParamAttributes.VarianceMask;
			set => ModifyAttributes(~GenericParamAttributes.VarianceMask, value & GenericParamAttributes.VarianceMask);
		}

		/// <summary>
		/// <c>true</c> if <see cref="GenericParamAttributes.NonVariant"/> is set
		/// </summary>
		public bool IsNonVariant => Variance == GenericParamAttributes.NonVariant;

		/// <summary>
		/// <c>true</c> if <see cref="GenericParamAttributes.Covariant"/> is set
		/// </summary>
		public bool IsCovariant => Variance == GenericParamAttributes.Covariant;

		/// <summary>
		/// <c>true</c> if <see cref="GenericParamAttributes.Contravariant"/> is set
		/// </summary>
		public bool IsContravariant => Variance == GenericParamAttributes.Contravariant;

		/// <summary>
		/// Gets/sets the special constraint
		/// </summary>
		public GenericParamAttributes SpecialConstraint {
			get => (GenericParamAttributes)attributes & GenericParamAttributes.SpecialConstraintMask;
			set => ModifyAttributes(~GenericParamAttributes.SpecialConstraintMask, value & GenericParamAttributes.SpecialConstraintMask);
		}

		/// <summary>
		/// <c>true</c> if there are no special constraints
		/// </summary>
		public bool HasNoSpecialConstraint => ((GenericParamAttributes)attributes & GenericParamAttributes.SpecialConstraintMask) == GenericParamAttributes.NoSpecialConstraint;

		/// <summary>
		/// Gets/sets the <see cref="GenericParamAttributes.ReferenceTypeConstraint"/> bit
		/// </summary>
		public bool HasReferenceTypeConstraint {
			get => ((GenericParamAttributes)attributes & GenericParamAttributes.ReferenceTypeConstraint) != 0;
			set => ModifyAttributes(value, GenericParamAttributes.ReferenceTypeConstraint);
		}

		/// <summary>
		/// Gets/sets the <see cref="GenericParamAttributes.NotNullableValueTypeConstraint"/> bit
		/// </summary>
		public bool HasNotNullableValueTypeConstraint {
			get => ((GenericParamAttributes)attributes & GenericParamAttributes.NotNullableValueTypeConstraint) != 0;
			set => ModifyAttributes(value, GenericParamAttributes.NotNullableValueTypeConstraint);
		}

		/// <summary>
		/// Gets/sets the <see cref="GenericParamAttributes.DefaultConstructorConstraint"/> bit
		/// </summary>
		public bool HasDefaultConstructorConstraint {
			get => ((GenericParamAttributes)attributes & GenericParamAttributes.DefaultConstructorConstraint) != 0;
			set => ModifyAttributes(value, GenericParamAttributes.DefaultConstructorConstraint);
		}

		/// <inheritdoc/>
		void IListListener<GenericParamConstraint>.OnLazyAdd(int index, ref GenericParamConstraint value) => OnLazyAdd2(index, ref value);

		internal virtual void OnLazyAdd2(int index, ref GenericParamConstraint value) {
#if DEBUG
			if (value.Owner != this)
				throw new InvalidOperationException("Added generic param constraint's Owner != this");
#endif
		}

		/// <inheritdoc/>
		void IListListener<GenericParamConstraint>.OnAdd(int index, GenericParamConstraint value) {
			if (!(value.Owner is null))
				throw new InvalidOperationException("Generic param constraint is already owned by another generic param. Set Owner to null first.");
			value.Owner = this;
		}

		/// <inheritdoc/>
		void IListListener<GenericParamConstraint>.OnRemove(int index, GenericParamConstraint value) => value.Owner = null;

		/// <inheritdoc/>
		void IListListener<GenericParamConstraint>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<GenericParamConstraint>.OnClear() {
			foreach (var gpc in genericParamConstraints.GetEnumerable_NoLock())
				gpc.Owner = null;
		}

		/// <inheritdoc/>
		public override string ToString() {
			var o = owner;
			if (o is TypeDef)
				return $"!{number}";
			if (o is MethodDef)
				return $"!!{number}";
			return $"??{number}";
		}
	}

	/// <summary>
	/// A GenericParam row created by the user and not present in the original .NET file
	/// </summary>
	public class GenericParamUser : GenericParam {
		/// <summary>
		/// Default constructor
		/// </summary>
		public GenericParamUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number">The generic param number</param>
		public GenericParamUser(ushort number)
			: this(number, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number">The generic param number</param>
		/// <param name="flags">Flags</param>
		public GenericParamUser(ushort number, GenericParamAttributes flags)
			: this(number, flags, UTF8String.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number">The generic param number</param>
		/// <param name="flags">Flags</param>
		/// <param name="name">Name</param>
		public GenericParamUser(ushort number, GenericParamAttributes flags, UTF8String name) {
			genericParamConstraints = new LazyList<GenericParamConstraint>(this);
			this.number = number;
			attributes = (int)flags;
			this.name = name;
		}
	}

	/// <summary>
	/// Created from a row in the GenericParam table
	/// </summary>
	sealed class GenericParamMD : GenericParam, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.GenericParam, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), GetGenericParamContext(owner), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <inheritdoc/>
		protected override void InitializeGenericParamConstraints() {
			var list = readerModule.Metadata.GetGenericParamConstraintRidList(origRid);
			var tmp = new LazyList<GenericParamConstraint, RidList>(list.Count, this, list, (list2, index) => readerModule.ResolveGenericParamConstraint(list2[index], GetGenericParamContext(owner)));
			Interlocked.CompareExchange(ref genericParamConstraints, tmp, null);
		}

		static GenericParamContext GetGenericParamContext(ITypeOrMethodDef tmOwner) {
			if (tmOwner is MethodDef md)
				return GenericParamContext.Create(md);
			return new GenericParamContext(tmOwner as TypeDef);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>GenericParam</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public GenericParamMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.GenericParamTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"GenericParam rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			bool b = readerModule.TablesStream.TryReadGenericParamRow(origRid, out var row);
			Debug.Assert(b);
			number = row.Number;
			attributes = row.Flags;
			name = readerModule.StringsStream.ReadNoNull(row.Name);
			owner = readerModule.GetOwner(this);
			if (row.Kind != 0)
				kind = readerModule.ResolveTypeDefOrRef(row.Kind, GetGenericParamContext(owner));
		}

		internal GenericParamMD InitializeAll() {
			MemberMDInitializer.Initialize(Owner);
			MemberMDInitializer.Initialize(Number);
			MemberMDInitializer.Initialize(Flags);
			MemberMDInitializer.Initialize(Name);
			MemberMDInitializer.Initialize(Kind);
			MemberMDInitializer.Initialize(CustomAttributes);
			MemberMDInitializer.Initialize(GenericParamConstraints);
			return this;
		}

		/// <inheritdoc/>
		internal override void OnLazyAdd2(int index, ref GenericParamConstraint value) {
			if (value.Owner != this) {
				// More than one owner... This module has invalid metadata.
				value = readerModule.ForceUpdateRowId(readerModule.ReadGenericParamConstraint(value.Rid, GetGenericParamContext(owner)).InitializeAll());
				value.Owner = this;
			}
		}
	}
}
