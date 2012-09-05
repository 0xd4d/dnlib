using System;
using System.Diagnostics;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the GenericParam table
	/// </summary>
	[DebuggerDisplay("{Name.String}")]
	public abstract class GenericParam : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.GenericParam, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 19; }
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
		/// From column GenericParam.Owner
		/// </summary>
		public abstract ITypeOrMethodDef Owner { get; set; }

		/// <summary>
		/// From column GenericParam.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column GenericParam.Kind (v1.1 only)
		/// </summary>
		public abstract ITypeDefOrRef Kind { get; set; }

		/// <inheritdoc/>
		public override string ToString() {
			if (Owner is TypeDef)
				return string.Format("!{0}", Number);
			return string.Format("!!{0}", Number);
		}
	}

	/// <summary>
	/// A GenericParam row created by the user and not present in the original .NET file
	/// </summary>
	public class GenericParamUser : GenericParam {
		ushort number;
		GenericParamAttributes flags;
		ITypeOrMethodDef owner;
		UTF8String name;
		ITypeDefOrRef kind;

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
		public override ITypeOrMethodDef Owner {
			get { return owner; }
			set { owner = value; }
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

		/// <summary>
		/// Default constructor
		/// </summary>
		public GenericParamUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="owner">Owner type/method</param>
		/// <param name="number">The generic param number</param>
		public GenericParamUser(ITypeOrMethodDef owner, ushort number)
			: this(owner, number, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="owner">Owner type/method</param>
		/// <param name="number">The generic param number</param>
		/// <param name="flags">Flags</param>
		public GenericParamUser(ITypeOrMethodDef owner, ushort number, GenericParamAttributes flags)
			: this(owner, number, flags, UTF8String.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="owner">Owner type/method</param>
		/// <param name="number">The generic param number</param>
		/// <param name="flags">Flags</param>
		/// <param name="name">Name</param>
		public GenericParamUser(ITypeOrMethodDef owner, ushort number, GenericParamAttributes flags, UTF8String name) {
			this.owner = owner;
			this.number = number;
			this.flags = flags;
			this.name = name;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="owner">Owner type/method</param>
		/// <param name="number">The generic param number</param>
		/// <param name="flags">Flags</param>
		/// <param name="name">Name</param>
		public GenericParamUser(ITypeOrMethodDef owner, ushort number, GenericParamAttributes flags, string name)
			: this(owner, number, flags, new UTF8String(name)) {
		}
	}

	/// <summary>
	/// Created from a row in the GenericParam table
	/// </summary>
	sealed class GenericParamMD : GenericParam {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawGenericParamRow rawRow;

		UserValue<ushort> number;
		UserValue<GenericParamAttributes> flags;
		UserValue<ITypeOrMethodDef> owner;
		UserValue<UTF8String> name;
		UserValue<ITypeDefOrRef> kind;

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
		public override ITypeOrMethodDef Owner {
			get { return owner.Value; }
			set { owner.Value = value; }
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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>GenericParam</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public GenericParamMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.GenericParam).Rows < rid)
				throw new BadImageFormatException(string.Format("GenericParam rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			number.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.Number;
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (GenericParamAttributes)rawRow.Flags;
			};
			owner.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveTypeOrMethodDef(rawRow.Owner);
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
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
