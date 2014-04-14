/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using System.Diagnostics;
using System.Threading;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.Threading;

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
	public abstract class GenericParam : IHasCustomAttribute, IMemberRef, IListListener<GenericParamConstraint> {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

#if THREAD_SAFE
		/// <summary>
		/// The lock
		/// </summary>
		internal readonly Lock theLock = Lock.Create();
#endif

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
		public abstract ITypeOrMethodDef Owner { get; internal set; }

		/// <summary>
		/// Gets the declaring type or <c>null</c> if none or if <see cref="Owner"/> is
		/// not a <see cref="TypeDef"/>
		/// </summary>
		public TypeDef DeclaringType {
			get { return Owner as TypeDef; }
		}

		/// <summary>
		/// Gets the declaring method or <c>null</c> if none or if <see cref="Owner"/> is
		/// not a <see cref="MethodDef"/>
		/// </summary>
		public MethodDef DeclaringMethod {
			get { return Owner as MethodDef; }
		}

		/// <summary>
		/// From column GenericParam.Number
		/// </summary>
		public abstract ushort Number { get; set; }

		/// <summary>
		/// From column GenericParam.Flags
		/// </summary>
		public GenericParamAttributes Flags {
#if THREAD_SAFE
			get {
				theLock.EnterWriteLock();
				try {
					return Flags_NoLock;
				}
				finally { theLock.ExitWriteLock(); }
			}
			set {
				theLock.EnterWriteLock();
				try {
					Flags_NoLock = value;
				}
				finally { theLock.ExitWriteLock(); }
			}
#else
			get { return Flags_NoLock; }
			set { Flags_NoLock = value; }
#endif
		}

		/// <summary>
		/// From column GenericParam.Flags
		/// </summary>
		protected abstract GenericParamAttributes Flags_NoLock { get; set; }

		/// <summary>
		/// From column GenericParam.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column GenericParam.Kind (v1.1 only)
		/// </summary>
		public abstract ITypeDefOrRef Kind { get; set; }

		/// <summary>
		/// Gets the generic param constraints
		/// </summary>
		public abstract ThreadSafe.IList<GenericParamConstraint> GenericParamConstraints { get; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <inheritdoc/>
		public ModuleDef Module {
			get {
				var dt = Owner as IMemberRef;
				return dt == null ? null : dt.Module;
			}
		}

		/// <inheritdoc/>
		public string FullName {
			get { return UTF8String.ToSystemStringOrEmpty(Name); }
		}

		/// <summary>
		/// Modify <see cref="Flags_NoLock"/> property: <see cref="Flags_NoLock"/> =
		/// (<see cref="Flags_NoLock"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(GenericParamAttributes andMask, GenericParamAttributes orMask) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
				Flags_NoLock = (Flags_NoLock & andMask) | orMask;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Gets/sets variance (non, contra, co)
		/// </summary>
		public GenericParamAttributes Variance {
			get { return Flags & GenericParamAttributes.VarianceMask; }
			set { ModifyAttributes(~GenericParamAttributes.VarianceMask, value & GenericParamAttributes.VarianceMask); }
		}

		/// <summary>
		/// Gets/sets the special constraint
		/// </summary>
		public GenericParamAttributes SpecialConstraint {
			get { return Flags & GenericParamAttributes.SpecialConstraintMask; }
			set { ModifyAttributes(~GenericParamAttributes.SpecialConstraintMask, value & GenericParamAttributes.SpecialConstraintMask); }
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
			if (Owner is TypeDef)
				return string.Format("!{0}", Number);
			if (Owner is MethodDef)
				return string.Format("!!{0}", Number);
			return string.Format("??{0}", Number);
		}
	}

	/// <summary>
	/// A GenericParam row created by the user and not present in the original .NET file
	/// </summary>
	public class GenericParamUser : GenericParam {
		ITypeOrMethodDef owner;
		ushort number;
		GenericParamAttributes flags;
		UTF8String name;
		ITypeDefOrRef kind;
		LazyList<GenericParamConstraint> genericParamConstraints;
		readonly CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();

		/// <inheritdoc/>
		public override ITypeOrMethodDef Owner {
			get { return owner; }
			internal set { owner = value; }
		}

		/// <inheritdoc/>
		public override ushort Number {
			get { return number; }
			set { number = value; }
		}

		/// <inheritdoc/>
		protected override GenericParamAttributes Flags_NoLock {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef Kind {
			get { return kind; }
			set { kind = value; }
		}

		/// <inheritdoc/>
		public override ThreadSafe.IList<GenericParamConstraint> GenericParamConstraints {
			get { return genericParamConstraints; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

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
			this.flags = flags;
			this.name = name;
		}
	}

	/// <summary>
	/// Created from a row in the GenericParam table
	/// </summary>
	sealed class GenericParamMD : GenericParam {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow_NoLock"/> is called</summary>
		RawGenericParamRow rawRow;

		UserValue<ITypeOrMethodDef> owner;
		UserValue<ushort> number;
		UserValue<GenericParamAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<ITypeDefOrRef> kind;
		LazyList<GenericParamConstraint> genericParamConstraints;
		CustomAttributeCollection customAttributeCollection;

		/// <inheritdoc/>
		public override ITypeOrMethodDef Owner {
			get { return owner.Value; }
			internal set { owner.Value = value; }
		}

		/// <inheritdoc/>
		public override ushort Number {
			get { return number.Value; }
			set { number.Value = value; }
		}

		/// <inheritdoc/>
		protected override GenericParamAttributes Flags_NoLock {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef Kind {
			get { return kind.Value; }
			set { kind.Value = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.GenericParam, rid);
					var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
					Interlocked.CompareExchange(ref customAttributeCollection, tmp, null);
				}
				return customAttributeCollection;
			}
		}

		/// <inheritdoc/>
		public override ThreadSafe.IList<GenericParamConstraint> GenericParamConstraints {
			get {
				if (genericParamConstraints == null) {
					var list = readerModule.MetaData.GetGenericParamConstraintRidList(rid);
					var tmp = new LazyList<GenericParamConstraint>((int)list.Length, this, list, (list2, index) => readerModule.ResolveGenericParamConstraint(((RidList)list2)[index]));
					Interlocked.CompareExchange(ref genericParamConstraints, tmp, null);
				}
				return genericParamConstraints;
			}
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
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			owner.ReadOriginalValue = () => {
				return readerModule.GetOwner(this);
			};
			number.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return rawRow.Number;
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return (GenericParamAttributes)rawRow.Flags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.StringsStream.ReadNoNull(rawRow.Name);
			};
			kind.ReadOriginalValue = () => {
				if (readerModule.TablesStream.GenericParamTable.TableInfo.Columns.Count != 5)
					return null;
				InitializeRawRow_NoLock();
				return readerModule.ResolveTypeDefOrRef(rawRow.Kind);
			};
#if THREAD_SAFE
			owner.Lock = theLock;
			number.Lock = theLock;
			// flags.Lock = theLock;	No lock for this one
			name.Lock = theLock;
			kind.Lock = theLock;
#endif
		}

		void InitializeRawRow_NoLock() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadGenericParamRow(rid);
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
				value = readerModule.ForceUpdateRowId(readerModule.ReadGenericParamConstraint(value.Rid).InitializeAll());
				value.Owner = this;
			}
		}
	}
}
