using System;
using dot10.dotNET.MD;

namespace dot10.dotNET.Hi {
	/// <summary>
	/// A high-level representation of a row in the Field table
	/// </summary>
	public abstract class FieldDef : IHasConstant, IHasCustomAttribute, IHasFieldMarshal, IMemberForwarded {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Field, rid); }
		}

		/// <inheritdoc/>
		public int HasConstantTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int HasFieldMarshalTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int MemberForwardedTag {
			get { return 0; }
		}

		/// <summary>
		/// From column Field.Flags
		/// </summary>
		public abstract FieldAttributes Flags { get; set; }

		/// <summary>
		/// From column Field.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column Field.Signature
		/// </summary>
		public abstract ISignature Signature { get; set; }

		/// <summary>
		/// Gets/sets the <see cref="FieldSig"/>
		/// </summary>
		public FieldSig FieldSig {
			get { return Signature as FieldSig; }
			set { Signature = value; }
		}

		/// <summary>
		/// Gets/sets the field access
		/// </summary>
		public FieldAttributes Access {
			get { return Flags & FieldAttributes.FieldAccessMask; }
			set { Flags = (Flags & ~FieldAttributes.FieldAccessMask) | (value & FieldAttributes.FieldAccessMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.PrivateScope"/> is set
		/// </summary>
		public bool IsPrivateScope {
			get { return (Flags & FieldAttributes.FieldAccessMask) == FieldAttributes.PrivateScope; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.Private"/> is set
		/// </summary>
		public bool IsPrivate {
			get { return (Flags & FieldAttributes.FieldAccessMask) == FieldAttributes.Private; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.FamANDAssem"/> is set
		/// </summary>
		public bool IsFamANDAssem {
			get { return (Flags & FieldAttributes.FieldAccessMask) == FieldAttributes.FamANDAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.Assembly"/> is set
		/// </summary>
		public bool IsAssembly {
			get { return (Flags & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.Family"/> is set
		/// </summary>
		public bool IsFamily {
			get { return (Flags & FieldAttributes.FieldAccessMask) == FieldAttributes.Family; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.FamORAssem"/> is set
		/// </summary>
		public bool IsFamORAssem {
			get { return (Flags & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="FieldAttributes.Public"/> is set
		/// </summary>
		public bool IsPublic {
			get { return (Flags & FieldAttributes.FieldAccessMask) == FieldAttributes.Public; }
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.Static"/> bit
		/// </summary>
		public bool IsStatic {
			get { return (Flags & FieldAttributes.Static) != 0; }
			set {
				if (value)
					Flags |= FieldAttributes.Static;
				else
					Flags &= ~FieldAttributes.Static;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.InitOnly"/> bit
		/// </summary>
		public bool IsInitOnly {
			get { return (Flags & FieldAttributes.InitOnly) != 0; }
			set {
				if (value)
					Flags |= FieldAttributes.InitOnly;
				else
					Flags &= ~FieldAttributes.InitOnly;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.Literal"/> bit
		/// </summary>
		public bool IsLiteral {
			get { return (Flags & FieldAttributes.Literal) != 0; }
			set {
				if (value)
					Flags |= FieldAttributes.Literal;
				else
					Flags &= ~FieldAttributes.Literal;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.NotSerialized"/> bit
		/// </summary>
		public bool IsNotSerialized {
			get { return (Flags & FieldAttributes.NotSerialized) != 0; }
			set {
				if (value)
					Flags |= FieldAttributes.NotSerialized;
				else
					Flags &= ~FieldAttributes.NotSerialized;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get { return (Flags & FieldAttributes.SpecialName) != 0; }
			set {
				if (value)
					Flags |= FieldAttributes.SpecialName;
				else
					Flags &= ~FieldAttributes.SpecialName;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.PinvokeImpl"/> bit
		/// </summary>
		public bool IsPinvokeImpl {
			get { return (Flags & FieldAttributes.PinvokeImpl) != 0; }
			set {
				if (value)
					Flags |= FieldAttributes.PinvokeImpl;
				else
					Flags &= ~FieldAttributes.PinvokeImpl;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRTSpecialName {
			get { return (Flags & FieldAttributes.RTSpecialName) != 0; }
			set {
				if (value)
					Flags |= FieldAttributes.RTSpecialName;
				else
					Flags &= ~FieldAttributes.RTSpecialName;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.HasFieldMarshal"/> bit
		/// </summary>
		public bool HasFieldMarshal {
			get { return (Flags & FieldAttributes.HasFieldMarshal) != 0; }
			set {
				if (value)
					Flags |= FieldAttributes.HasFieldMarshal;
				else
					Flags &= ~FieldAttributes.HasFieldMarshal;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.HasDefault"/> bit
		/// </summary>
		public bool HasDefault {
			get { return (Flags & FieldAttributes.HasDefault) != 0; }
			set {
				if (value)
					Flags |= FieldAttributes.HasDefault;
				else
					Flags &= ~FieldAttributes.HasDefault;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="FieldAttributes.HasFieldRVA"/> bit
		/// </summary>
		public bool HasFieldRVA {
			get { return (Flags & FieldAttributes.HasFieldRVA) != 0; }
			set {
				if (value)
					Flags |= FieldAttributes.HasFieldRVA;
				else
					Flags &= ~FieldAttributes.HasFieldRVA;
			}
		}

		/// <summary>
		/// Returns the full name of this field
		/// </summary>
		public string FullName {
			get {
				var name = UTF8String.IsNullOrEmpty(Name) ? "" : Name.String;
				var fieldSig = FieldSig;
				if (fieldSig == null || fieldSig.Type == null)
					return name;
				return string.Format("{0} {1}", fieldSig.Type.FullName, name);
			}
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A Field row created by the user and not present in the original .NET file
	/// </summary>
	public class FieldDefUser : FieldDef {
		FieldAttributes flags;
		UTF8String name;
		ISignature signature;

		/// <inheritdoc/>
		public override FieldAttributes Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override ISignature Signature {
			get { return signature; }
			set { signature = value; }
		}

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
		/// <param name="flags">Flags</param>
		public FieldDefUser(UTF8String name, FieldSig signature, FieldAttributes flags) {
			this.name = name;
			this.signature = signature;
			this.flags = flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public FieldDefUser(string name)
			: this(name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="signature">Signature</param>
		public FieldDefUser(string name, FieldSig signature)
			: this(name, signature, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="signature">Signature</param>
		/// <param name="flags">Flags</param>
		public FieldDefUser(string name, FieldSig signature, FieldAttributes flags)
			: this(new UTF8String(name), signature, flags) {
		}
	}

	/// <summary>
	/// Created from a row in the Field table
	/// </summary>
	sealed class FieldDefMD : FieldDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawFieldRow rawRow;

		UserValue<FieldAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<ISignature> signature;

		/// <inheritdoc/>
		public override FieldAttributes Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override ISignature Signature {
			get { return signature.Value; }
			set { signature.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Field</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public FieldDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.Field).Rows < rid)
				throw new BadImageFormatException(string.Format("Field rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (FieldAttributes)rawRow.Flags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			signature.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ReadSignature(rawRow.Signature);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadFieldRow(rid);
		}
	}
}
