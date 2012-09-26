using System;
using System.Collections.Generic;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the MemberRef table
	/// </summary>
	public abstract class MemberRef : IHasCustomAttribute, IMethodDefOrRef, ICustomAttributeType, IField {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// The owner module
		/// </summary>
		protected ModuleDef ownerModule;

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

		/// <inheritdoc/>
		public ModuleDef OwnerModule {
			get { return ownerModule; }
		}

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string FullName {
			get {
				var parent = Class;
				IList<TypeSig> typeGenArgs = null;
				if (parent is TypeSpec) {
					var sig = ((TypeSpec)parent).TypeSig as GenericInstSig;
					if (sig != null)
						typeGenArgs = sig.GenericArguments;
				}
				if (IsMethodRef)
					return FullNameCreator.MethodFullName(GetDeclaringTypeFullName(), Name, MethodSig, typeGenArgs, null);
				if (IsFieldRef)
					return FullNameCreator.FieldFullName(GetDeclaringTypeFullName(), Name, FieldSig, typeGenArgs);
				return string.Empty;
			}
		}

		/// <summary>
		/// Get the declaring type's full name
		/// </summary>
		/// <returns>Full name or <c>null</c> if there's no declaring type</returns>
		public string GetDeclaringTypeFullName() {
			var parent = Class;
			if (parent == null)
				return null;
			if (parent is ITypeDefOrRef)
				return ((ITypeDefOrRef)parent).FullName;
			if (parent is ModuleRef)
				return string.Format("[module:{0}]<Module>", ((ModuleRef)parent).ToString());
			if (parent is MethodDef) {
				var declaringType = ((MethodDef)parent).DeclaringType;
				return declaringType == null ? null : declaringType.FullName;
			}
			return null;	// Should never be reached
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
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		public MemberRefUser(ModuleDef ownerModule) {
			this.ownerModule = ownerModule;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		/// <param name="name">Name of ref</param>
		public MemberRefUser(ModuleDef ownerModule, UTF8String name) {
			this.ownerModule = ownerModule;
			this.name = name;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		/// <param name="name">Name of field ref</param>
		/// <param name="sig">Field sig</param>
		public MemberRefUser(ModuleDef ownerModule, UTF8String name, FieldSig sig)
			: this(ownerModule, name, sig, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		/// <param name="name">Name of field ref</param>
		/// <param name="sig">Field sig</param>
		/// <param name="class">Owner of field</param>
		public MemberRefUser(ModuleDef ownerModule, UTF8String name, FieldSig sig, IMemberRefParent @class) {
			this.ownerModule = ownerModule;
			this.name = name;
			this.@class = @class;
			this.signature = sig;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		/// <param name="name">Name of method ref</param>
		/// <param name="sig">Method sig</param>
		public MemberRefUser(ModuleDef ownerModule, UTF8String name, MethodSig sig)
			: this(ownerModule, name, sig, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		/// <param name="name">Name of method ref</param>
		/// <param name="sig">Method sig</param>
		/// <param name="class">Owner of method</param>
		public MemberRefUser(ModuleDef ownerModule, UTF8String name, MethodSig sig, IMemberRefParent @class) {
			this.ownerModule = ownerModule;
			this.name = name;
			this.@class = @class;
			this.signature = sig;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		/// <param name="name">Name of ref</param>
		public MemberRefUser(ModuleDef ownerModule, string name)
			: this(ownerModule, new UTF8String(name)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		/// <param name="name">Name of field ref</param>
		/// <param name="sig">Field sig</param>
		public MemberRefUser(ModuleDef ownerModule, string name, FieldSig sig)
			: this(ownerModule, name, sig, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		/// <param name="name">Name of field ref</param>
		/// <param name="sig">Field sig</param>
		/// <param name="class">Owner of field</param>
		public MemberRefUser(ModuleDef ownerModule, string name, FieldSig sig, IMemberRefParent @class)
			: this(ownerModule, new UTF8String(name), sig, @class) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		/// <param name="name">Name of method ref</param>
		/// <param name="sig">Method sig</param>
		public MemberRefUser(ModuleDef ownerModule, string name, MethodSig sig)
			: this(ownerModule, name, sig, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		/// <param name="name">Name of method ref</param>
		/// <param name="sig">Method sig</param>
		/// <param name="class">Owner of method</param>
		public MemberRefUser(ModuleDef ownerModule, string name, MethodSig sig, IMemberRefParent @class)
			: this(ownerModule, new UTF8String(name), sig, @class) {
		}
	}

	/// <summary>
	/// Created from a row in the MemberRef table
	/// </summary>
	sealed class MemberRefMD : MemberRef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
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
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public MemberRefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.MemberRef).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("MemberRef rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			this.ownerModule = readerModule;
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
