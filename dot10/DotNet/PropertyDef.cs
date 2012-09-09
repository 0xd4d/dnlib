using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Property table
	/// </summary>
	public abstract class PropertyDef : IHasConstant, IHasCustomAttribute, IHasSemantic {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Property, rid); }
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

		/// <summary>
		/// Gets/sets the property sig
		/// </summary>
		public PropertySig PropertySig {
			get { return Type as PropertySig; }
			set { Type = value; }
		}
	}

	/// <summary>
	/// A Property row created by the user and not present in the original .NET file
	/// </summary>
	public class PropertyDefUser : PropertyDef {
		PropertyAttributes flags;
		UTF8String name;
		CallingConventionSig type;

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
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawPropertyRow rawRow;

		UserValue<PropertyAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<CallingConventionSig> type;

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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Property</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public PropertyDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.Property).Rows < rid)
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
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadPropertyRow(rid);
		}
	}
}
