using System;
using System.Collections.Generic;
using System.Diagnostics;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the GenericParam table
	/// </summary>
	[DebuggerDisplay("{Name.String}")]
	public abstract class GenericParam : IHasCustomAttribute, IListListener<GenericParamConstraint> {
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
		public abstract GenericParamAttributes Flags { get; set; }

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
		public abstract IList<GenericParamConstraint> GenericParamConstraints { get; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <summary>
		/// Gets/sets variance (non, contra, co)
		/// </summary>
		public GenericParamAttributes Variance {
			get { return Flags & GenericParamAttributes.VarianceMask; }
			set { Flags = (Flags & ~GenericParamAttributes.VarianceMask) | (value & GenericParamAttributes.VarianceMask); }
		}

		/// <summary>
		/// Gets/sets the special constraint
		/// </summary>
		public GenericParamAttributes SpecialConstraint {
			get { return Flags & GenericParamAttributes.SpecialConstraintMask; }
			set { Flags = (Flags & ~GenericParamAttributes.SpecialConstraintMask) | (value & GenericParamAttributes.SpecialConstraintMask); }
		}

		/// <inheritdoc/>
		void IListListener<GenericParamConstraint>.OnAdd(int index, GenericParamConstraint value, bool isLazyAdd) {
			if (isLazyAdd) {
#if DEBUG
				if (value.Owner != this)
					throw new InvalidOperationException("Added generic param constraint's Owner != this");
#endif
				return;
			}
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
			foreach (var gpc in GenericParamConstraints)
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
		CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();

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
		public override GenericParamAttributes Flags {
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
		public override IList<GenericParamConstraint> GenericParamConstraints {
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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number">The generic param number</param>
		/// <param name="flags">Flags</param>
		/// <param name="name">Name</param>
		public GenericParamUser(ushort number, GenericParamAttributes flags, string name)
			: this(number, flags, new UTF8String(name)) {
		}
	}

	/// <summary>
	/// Created from a row in the GenericParam table
	/// </summary>
	sealed class GenericParamMD : GenericParam {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
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
		public override GenericParamAttributes Flags {
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
					customAttributeCollection = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
				}
				return customAttributeCollection;
			}
		}

		/// <inheritdoc/>
		public override IList<GenericParamConstraint> GenericParamConstraints {
			get {
				if (genericParamConstraints == null) {
					var list = readerModule.MetaData.GetGenericParamConstraintRidList(rid);
					genericParamConstraints = new LazyList<GenericParamConstraint>((int)list.Length, list, (list2, index) => readerModule.ResolveGenericParamConstraint(((RidList)list2)[index]));
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
			if (readerModule.TablesStream.Get(Table.GenericParam).IsInvalidRID(rid))
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
				InitializeRawRow();
				return rawRow.Number;
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (GenericParamAttributes)rawRow.Flags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.ReadNoNull(rawRow.Name);
			};
			kind.ReadOriginalValue = () => {
				if (readerModule.TablesStream.Get(Table.GenericParam).TableInfo.Columns.Count != 5)
					return null;
				InitializeRawRow();
				return readerModule.ResolveTypeDefOrRef(rawRow.Kind);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadGenericParamRow(rid);
		}
	}
}
