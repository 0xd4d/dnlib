using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the TypeRef table
	/// </summary>
	public abstract class TypeRef : ITypeDefOrRef, IHasCustomAttribute, IMemberRefParent, IResolutionScope {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.TypeRef, rid); }
		}

		/// <inheritdoc/>
		public int TypeDefOrRefTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int ResolutionScopeTag {
			get { return 3; }
		}

		/// <inheritdoc/>
		string IFullName.Name {
			get { return FullNameHelper.GetName(Name); }
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get { return FullNameHelper.GetReflectionName(Name); }
		}

		/// <inheritdoc/>
		string IFullName.Namespace {
			get { return FullNameHelper.GetNamespace(Namespace); }
		}

		/// <inheritdoc/>
		public string ReflectionNamespace {
			get { return FullNameHelper.GetReflectionNamespace(Namespace); }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return FullNameHelper.GetFullName(Namespace, Name); }
		}

		/// <inheritdoc/>
		public string ReflectionFullName {
			//TODO: Include the assembly name
			//TODO: If nested, add the nested class
			get { return FullNameHelper.GetReflectionFullName(Namespace, Name); }
		}

		/// <summary>
		/// From column TypeRef.ResolutionScope
		/// </summary>
		public abstract IResolutionScope ResolutionScope { get; set; }

		/// <summary>
		/// From column TypeRef.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column TypeRef.Namespace
		/// </summary>
		public abstract UTF8String Namespace { get; set; }

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A TypeRef row created by the user and not present in the original .NET file
	/// </summary>
	public class TypeRefUser : TypeRef {
		IResolutionScope resolutionScope;
		UTF8String name;
		UTF8String @namespace;

		/// <inheritdoc/>
		public override IResolutionScope ResolutionScope {
			get { return resolutionScope; }
			set { resolutionScope = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Namespace {
			get { return @namespace; }
			set { @namespace = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Type name</param>
		public TypeRefUser(UTF8String name)
			: this(UTF8String.Empty, name) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="namespace">Type namespace</param>
		/// <param name="name">Type name</param>
		public TypeRefUser(UTF8String @namespace, UTF8String name)
			: this(@namespace, name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="namespace">Type namespace</param>
		/// <param name="name">Type name</param>
		/// <param name="resolutionScope">Resolution scope (a <see cref="ModuleDef"/>,
		/// <see cref="ModuleRef"/>, <see cref="AssemblyRef"/> or <see cref="TypeRef"/>)</param>
		public TypeRefUser(UTF8String @namespace, UTF8String name, IResolutionScope resolutionScope) {
			this.resolutionScope = resolutionScope;
			this.name = name;
			this.@namespace = @namespace;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Type name</param>
		public TypeRefUser(string name)
			: this(string.Empty, name) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="namespace">Type namespace</param>
		/// <param name="name">Type name</param>
		public TypeRefUser(string @namespace, string name)
			: this(@namespace, name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="namespace">Type namespace</param>
		/// <param name="name">Type name</param>
		/// <param name="resolutionScope">Resolution scope (a <see cref="ModuleDef"/>,
		/// <see cref="ModuleRef"/>, <see cref="AssemblyRef"/> or <see cref="TypeRef"/>)</param>
		public TypeRefUser(string @namespace, string name, IResolutionScope resolutionScope)
			: this(new UTF8String(@namespace), new UTF8String(name), resolutionScope) {
		}
	}

	/// <summary>
	/// Created from a row in the TypeRef table
	/// </summary>
	sealed class TypeRefMD : TypeRef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawTypeRefRow rawRow;

		UserValue<IResolutionScope> resolutionScope;
		UserValue<UTF8String> name;
		UserValue<UTF8String> @namespace;

		/// <inheritdoc/>
		public override IResolutionScope ResolutionScope {
			get { return resolutionScope.Value; }
			set { resolutionScope.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Namespace {
			get { return @namespace.Value; }
			set { @namespace.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>TypeRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public TypeRefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.TypeRef).Rows < rid)
				throw new BadImageFormatException(string.Format("TypeRef rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			resolutionScope.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveResolutionScope(rawRow.ResolutionScope);
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			@namespace.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Namespace);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadTypeRefRow(rid);
		}
	}
}
