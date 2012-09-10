using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the MemberRef table
	/// </summary>
	public abstract class MemberRef : IHasCustomAttribute, IMethodDefOrRef, ICustomAttributeType {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.MemberRef, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 6; }
		}

		/// <inheritdoc/>
		public int MethodDefOrRefTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int CustomAttributeTypeTag {
			get { return 3; }
		}

		/// <summary>
		/// From column MemberRef.Class
		/// </summary>
		public abstract IMemberRefParent Class { get; set; }

		/// <summary>
		/// From column MemberRef.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column MemberRef.Signature
		/// </summary>
		public abstract CallingConventionSig Signature { get; set; }

		/// <summary>
		/// <c>true</c> if this is a method reference (<see cref="MethodSig"/> != <c>null</c>)
		/// </summary>
		public bool IsMethodRef {
			get { return MethodSig != null; }
		}

		/// <summary>
		/// <c>true</c> if this is a field reference (<see cref="FieldSig"/> != <c>null</c>)
		/// </summary>
		public bool IsFieldRef {
			get { return FieldSig != null; }
		}

		/// <summary>
		/// Gets/sets the method sig
		/// </summary>
		public MethodSig MethodSig {
			get { return Signature as MethodSig; }
			set { Signature = value; }
		}

		/// <summary>
		/// Gets/sets the field sig
		/// </summary>
		public FieldSig FieldSig {
			get { return Signature as FieldSig; }
			set { Signature = value; }
		}

		/// <summary>
		/// Get the full name
		/// </summary>
		public string FullName {
			get {
				if (IsMethodRef)
					return Utils.GetMethodString(null, Name, MethodSig);
				if (IsFieldRef)
					return Utils.GetFieldString(null, Name, FieldSig);
				return string.Empty;
			}
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A MemberRef row created by the user and not present in the original .NET file
	/// </summary>
	public class MemberRefUser : MemberRef {
		IMemberRefParent @class;
		UTF8String name;
		CallingConventionSig signature;

		/// <inheritdoc/>
		public override IMemberRefParent Class {
			get { return @class; }
			set { @class = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override CallingConventionSig Signature {
			get { return signature; }
			set { signature = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MemberRefUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of ref</param>
		public MemberRefUser(UTF8String name) {
			this.name = name;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of field ref</param>
		/// <param name="sig">Field sig</param>
		public MemberRefUser(UTF8String name, FieldSig sig)
			: this(name, sig, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of field ref</param>
		/// <param name="sig">Field sig</param>
		/// <param name="class">Owner of field</param>
		public MemberRefUser(UTF8String name, FieldSig sig, IMemberRefParent @class) {
			this.name = name;
			this.@class = @class;
			this.signature = sig;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of method ref</param>
		/// <param name="sig">Method sig</param>
		public MemberRefUser(UTF8String name, MethodSig sig)
			: this(name, sig, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of method ref</param>
		/// <param name="sig">Method sig</param>
		/// <param name="class">Owner of method</param>
		public MemberRefUser(UTF8String name, MethodSig sig, IMemberRefParent @class) {
			this.name = name;
			this.@class = @class;
			this.signature = sig;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of ref</param>
		public MemberRefUser(string name)
			: this(new UTF8String(name)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of field ref</param>
		/// <param name="sig">Field sig</param>
		public MemberRefUser(string name, FieldSig sig)
			: this(name, sig, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of field ref</param>
		/// <param name="sig">Field sig</param>
		/// <param name="class">Owner of field</param>
		public MemberRefUser(string name, FieldSig sig, IMemberRefParent @class)
			: this(new UTF8String(name), sig, @class) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of method ref</param>
		/// <param name="sig">Method sig</param>
		public MemberRefUser(string name, MethodSig sig)
			: this(name, sig, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of method ref</param>
		/// <param name="sig">Method sig</param>
		/// <param name="class">Owner of method</param>
		public MemberRefUser(string name, MethodSig sig, IMemberRefParent @class)
			: this(new UTF8String(name), sig, @class) {
		}
	}

	/// <summary>
	/// Created from a row in the MemberRef table
	/// </summary>
	sealed class MemberRefMD : MemberRef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawMemberRefRow rawRow;

		UserValue<IMemberRefParent> @class;
		UserValue<UTF8String> name;
		UserValue<CallingConventionSig> signature;

		/// <inheritdoc/>
		public override IMemberRefParent Class {
			get { return @class.Value; }
			set { @class.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override CallingConventionSig Signature {
			get { return signature.Value; }
			set { signature.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>MemberRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public MemberRefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.MemberRef).Rows < rid)
				throw new BadImageFormatException(string.Format("MemberRef rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			@class.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveMemberRefParent(rawRow.Class);
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
			rawRow = readerModule.TablesStream.ReadMemberRefRow(rid);
		}
	}
}
