// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.Threading;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.Threading;
using dnlib.DotNet.Pdb;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

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
		public MDToken MDToken {
			get { return new MDToken(Table.GenericParam, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 19; }
		}

		/// <summary>
		/// Gets the owner type/method
		/// </summary>
		public ITypeOrMethodDef Owner {
			get { return owner; }
			internal set { owner = value; }
		}
		/// <summary/>
		protected ITypeOrMethodDef owner;

		/// <summary>
		/// Gets the declaring type or <c>null</c> if none or if <see cref="Owner"/> is
		/// not a <see cref="TypeDef"/>
		/// </summary>
		public TypeDef DeclaringType {
			get { return owner as TypeDef; }
		}

		/// <inheritdoc/>
		ITypeDefOrRef IMemberRef.DeclaringType {
			get { return owner as TypeDef; }
		}

		/// <summary>
		/// Gets the declaring method or <c>null</c> if none or if <see cref="Owner"/> is
		/// not a <see cref="MethodDef"/>
		/// </summary>
		public MethodDef DeclaringMethod {
			get { return owner as MethodDef; }
		}

		/// <summary>
		/// From column GenericParam.Number
		/// </summary>
		public ushort Number {
			get { return number; }
			set { number = value; }
		}
		/// <summary/>
		protected ushort number;

		/// <summary>
		/// From column GenericParam.Flags
		/// </summary>
		public GenericParamAttributes Flags {
			get { return (GenericParamAttributes)attributes; }
			set { attributes = (int)value; }
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column GenericParam.Name
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value; }
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column GenericParam.Kind (v1.1 only)
		/// </summary>
		public ITypeDefOrRef Kind {
			get { return kind; }
			set { kind = value; }
		}
		/// <summary/>
		protected ITypeDefOrRef kind;

		/// <summary>
		/// Gets the generic param constraints
		/// </summary>
		public ThreadSafe.IList<GenericParamConstraint> GenericParamConstraints {
			get {
				if (genericParamConstraints == null)
					InitializeGenericParamConstraints();
				return genericParamConstraints;
			}
		}
		/// <summary/>
		protected LazyList<GenericParamConstraint> genericParamConstraints;
		/// <summary>Initializes <see cref="genericParamConstraints"/></summary>
		protected virtual void InitializeGenericParamConstraints() {
			Interlocked.CompareExchange(ref genericParamConstraints, new LazyList<GenericParamConstraint>(this), null);
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
			get { return 19; }
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
		/// <c>true</c> if <see cref="GenericParamConstraints"/> is not empty
		/// </summary>
		public bool HasGenericParamConstraints {
			get { return GenericParamConstraints.Count > 0; }
		}

		/// <inheritdoc/>
		public ModuleDef Module {
			get {
				var dt = owner;
				return dt == null ? null : dt.Module;
			}
		}

		/// <inheritdoc/>
		public string FullName {
			get { return UTF8String.ToSystemStringOrEmpty(name); }
		}

		bool IIsTypeOrMethod.IsType {
			get { return false; }
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
			get { return false; }
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
			get { return true; }
		}

		/// <summary>
		/// Modify <see cref="attributes"/> property: <see cref="attributes"/> =
		/// (<see cref="attributes"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(GenericParamAttributes andMask, GenericParamAttributes orMask) {
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
		void ModifyAttributes(bool set, GenericParamAttributes flags) {
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
		/// Gets/sets variance (non, contra, co)
		/// </summary>
		public GenericParamAttributes Variance {
			get { return (GenericParamAttributes)attributes & GenericParamAttributes.VarianceMask; }
			set { ModifyAttributes(~GenericParamAttributes.VarianceMask, value & GenericParamAttributes.VarianceMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="GenericParamAttributes.NonVariant"/> is set
		/// </summary>
		public bool IsNonVariant {
			get { return Variance == GenericParamAttributes.NonVariant; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="GenericParamAttributes.Covariant"/> is set
		/// </summary>
		public bool IsCovariant {
			get { return Variance == GenericParamAttributes.Covariant; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="GenericParamAttributes.Contravariant"/> is set
		/// </summary>
		public bool IsContravariant {
			get { return Variance == GenericParamAttributes.Contravariant; }
		}

		/// <summary>
		/// Gets/sets the special constraint
		/// </summary>
		public GenericParamAttributes SpecialConstraint {
			get { return (GenericParamAttributes)attributes & GenericParamAttributes.SpecialConstraintMask; }
			set { ModifyAttributes(~GenericParamAttributes.SpecialConstraintMask, value & GenericParamAttributes.SpecialConstraintMask); }
		}

		/// <summary>
		/// <c>true</c> if there are no special constraints
		/// </summary>
		public bool HasNoSpecialConstraint {
			get { return ((GenericParamAttributes)attributes & GenericParamAttributes.SpecialConstraintMask) == GenericParamAttributes.NoSpecialConstraint; }
		}

		/// <summary>
		/// Gets/sets the <see cref="GenericParamAttributes.ReferenceTypeConstraint"/> bit
		/// </summary>
		public bool HasReferenceTypeConstraint {
			get { return ((GenericParamAttributes)attributes & GenericParamAttributes.ReferenceTypeConstraint) != 0; }
			set { ModifyAttributes(value, GenericParamAttributes.ReferenceTypeConstraint); }
		}

		/// <summary>
		/// Gets/sets the <see cref="GenericParamAttributes.NotNullableValueTypeConstraint"/> bit
		/// </summary>
		public bool HasNotNullableValueTypeConstraint {
			get { return ((GenericParamAttributes)attributes & GenericParamAttributes.NotNullableValueTypeConstraint) != 0; }
			set { ModifyAttributes(value, GenericParamAttributes.NotNullableValueTypeConstraint); }
		}

		/// <summary>
		/// Gets/sets the <see cref="GenericParamAttributes.DefaultConstructorConstraint"/> bit
		/// </summary>
		public bool HasDefaultConstructorConstraint {
			get { return ((GenericParamAttributes)attributes & GenericParamAttributes.DefaultConstructorConstraint) != 0; }
			set { ModifyAttributes(value, GenericParamAttributes.DefaultConstructorConstraint); }
		}

		/// <inheritdoc/>
		void IListListener<GenericParamConstraint>.OnLazyAdd(int index, ref GenericParamConstraint value) {
			OnLazyAdd2(index, ref value);
		}

		internal virtual void OnLazyAdd2(int index, ref GenericParamConstraint value) {
#if DEBUG
			if (value.Owner != this)
				throw new InvalidOperationException("Added generic param constraint's Owner != this");
#endif
		}

		/// <inheritdoc/>
		void IListListener<GenericParamConstraint>.OnAdd(int index, GenericParamConstraint value) {
			if (value.Owner != null)
				throw new InvalidOperationException("Generic param constraint is already owned by another generic param. Set Owner to null first.");
			value.Owner = this;
		}

		/// <inheritdoc/>
		void IListListener<GenericParamConstraint>.OnRemove(int index, GenericParamConstraint value) {
			value.Owner = null;
		}

		/// <inheritdoc/>
		void IListListener<GenericParamConstraint>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<GenericParamConstraint>.OnClear() {
			foreach (var gpc in GenericParamConstraints.GetEnumerable_NoLock())
				gpc.Owner = null;
		}

		/// <inheritdoc/>
		public override string ToString() {
			var o = owner;
			if (o is TypeDef)
				return string.Format("!{0}", number);
			if (o is MethodDef)
				return string.Format("!!{0}", number);
			return string.Format("??{0}", number);
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
			this.genericParamConstraints = new LazyList<GenericParamConstraint>(this);
			this.number = number;
			this.attributes = (int)flags;
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
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.GenericParam, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = ThreadSafeListCreator.Create<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), GetGenericParamContext(owner), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <inheritdoc/>
		protected override void InitializeGenericParamConstraints() {
			var list = readerModule.MetaData.GetGenericParamConstraintRidList(origRid);
			var tmp = new LazyList<GenericParamConstraint>((int)list.Length, this, list, (list2, index) => readerModule.ResolveGenericParamConstraint(((RidList)list2)[index], GetGenericParamContext(owner)));
			Interlocked.CompareExchange(ref genericParamConstraints, tmp, null);
		}

		static GenericParamContext GetGenericParamContext(ITypeOrMethodDef tmOwner) {
			var md = tmOwner as MethodDef;
			if (md != null)
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
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.GenericParamTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("GenericParam rid {0} does not exist", rid));
#endif
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			uint name;
			uint kind = readerModule.TablesStream.ReadGenericParamRow(origRid, out this.number, out this.attributes, out name);
			this.name = readerModule.StringsStream.ReadNoNull(name);
			this.owner = readerModule.GetOwner(this);
			if (kind != 0)
				this.kind = readerModule.ResolveTypeDefOrRef(kind, GetGenericParamContext(owner));
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
