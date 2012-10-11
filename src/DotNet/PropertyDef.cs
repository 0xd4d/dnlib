using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Property table
	/// </summary>
	public abstract class PropertyDef : IHasConstant, IHasCustomAttribute, IHasSemantic, IFullName {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Property, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasConstantTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 9; }
		}

		/// <inheritdoc/>
		public int HasSemanticTag {
			get { return 1; }
		}

		/// <summary>
		/// From column Property.PropFlags
		/// </summary>
		public abstract PropertyAttributes Flags { get; set; }

		/// <summary>
		/// From column Property.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column Property.Type
		/// </summary>
		public abstract CallingConventionSig Type { get; set; }

		/// <inheritdoc/>
		public abstract Constant Constant { get; set; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <summary>
		/// Gets/sets the property sig
		/// </summary>
		public PropertySig PropertySig {
			get { return Type as PropertySig; }
			set { Type = value; }
		}

		/// <summary>
		/// Gets/sets the declaring type (owner type)
		/// </summary>
		public TypeDef DeclaringType {
			get { return DeclaringType2; }
			set {
				var currentDeclaringType = DeclaringType2;
				if (currentDeclaringType != null)
					currentDeclaringType.Properties.Remove(this);	// Will set DeclaringType2 = null
				if (value != null)
					value.Properties.Add(this);	// Will set DeclaringType2 = value
			}
		}

		/// <summary>
		/// Called by <see cref="DeclaringType"/>
		/// </summary>
		protected internal abstract TypeDef DeclaringType2 { get; set; }

		/// <summary>
		/// Gets the full name of the property
		/// </summary>
		public string FullName {
			get {
				var dt = DeclaringType;
				return FullNameCreator.PropertyFullName(dt == null ? null : dt.FullName, Name, Type);
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="PropertyAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get { return (Flags & PropertyAttributes.SpecialName) != 0; }
			set {
				if (value)
					Flags |= PropertyAttributes.SpecialName;
				else
					Flags &= ~PropertyAttributes.SpecialName;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="PropertyAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRTSpecialName {
			get { return (Flags & PropertyAttributes.RTSpecialName) != 0; }
			set {
				if (value)
					Flags |= PropertyAttributes.RTSpecialName;
				else
					Flags &= ~PropertyAttributes.RTSpecialName;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="PropertyAttributes.HasDefault"/> bit
		/// </summary>
		public bool HasDefault {
			get { return (Flags & PropertyAttributes.HasDefault) != 0; }
			set {
				if (value)
					Flags |= PropertyAttributes.HasDefault;
				else
					Flags &= ~PropertyAttributes.HasDefault;
			}
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A Property row created by the user and not present in the original .NET file
	/// </summary>
	public class PropertyDefUser : PropertyDef {
		PropertyAttributes flags;
		UTF8String name;
		CallingConventionSig type;
		Constant constant;
		CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();
		TypeDef declaringType;

		/// <inheritdoc/>
		public override PropertyAttributes Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override CallingConventionSig Type {
			get { return type; }
			set { type = value; }
		}

		/// <inheritdoc/>
		public override Constant Constant {
			get { return constant; }
			set { constant = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

		/// <inheritdoc/>
		protected internal override TypeDef DeclaringType2 {
			get { return declaringType; }
			set { declaringType = value; }
		}

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
			this.type = sig;
			this.flags = flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public PropertyDefUser(string name)
			: this(name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sig">Property signature</param>
		public PropertyDefUser(string name, PropertySig sig)
			: this(name, sig, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sig">Property signature</param>
		/// <param name="flags">Flags</param>
		public PropertyDefUser(string name, PropertySig sig, PropertyAttributes flags)
			: this(new UTF8String(name), sig, flags) {
		}
	}

	/// <summary>
	/// Created from a row in the Property table
	/// </summary>
	sealed class PropertyDefMD : PropertyDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawPropertyRow rawRow;

		UserValue<PropertyAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<CallingConventionSig> type;
		UserValue<Constant> constant;
		CustomAttributeCollection customAttributeCollection;
		UserValue<TypeDef> declaringType;

		/// <inheritdoc/>
		public override PropertyAttributes Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override CallingConventionSig Type {
			get { return type.Value; }
			set { type.Value = value; }
		}

		/// <inheritdoc/>
		public override Constant Constant {
			get { return constant.Value; }
			set { constant.Value = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.Property, rid);
					customAttributeCollection = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
				}
				return customAttributeCollection;
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
		/// <param name="readerModule">The module which contains this <c>Property</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public PropertyDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.Property).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("Property rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (PropertyAttributes)rawRow.PropFlags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			type.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ReadSignature(rawRow.Type);
			};
			constant.ReadOriginalValue = () => {
				return readerModule.ResolveConstant(readerModule.MetaData.GetConstantRid(Table.Property, rid));
			};
			declaringType.ReadOriginalValue = () => {
				return readerModule.GetOwnerType(this);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadPropertyRow(rid);
		}
	}
}
