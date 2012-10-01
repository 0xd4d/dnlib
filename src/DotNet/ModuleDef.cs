using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Module table
	/// </summary>
	public abstract class ModuleDef : IHasCustomAttribute, IResolutionScope, IDisposable, IListListener<TypeDef>, IModule {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// Initialize this in the ctor
		/// </summary>
		protected ICorLibTypes corLibTypes;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Module, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 7; }
		}

		/// <inheritdoc/>
		public int ResolutionScopeTag {
			get { return 0; }
		}

		/// <summary>
		/// Gets/sets Module.Generation column
		/// </summary>
		public abstract ushort Generation { get; set; }

		/// <summary>
		/// Gets/sets Module.Name column
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// Gets/sets Module.Mvid column
		/// </summary>
		public abstract Guid? Mvid { get; set; }

		/// <summary>
		/// Gets/sets Module.EncId column
		/// </summary>
		public abstract Guid? EncId { get; set; }

		/// <summary>
		/// Gets/sets Module.EncBaseId column
		/// </summary>
		public abstract Guid? EncBaseId { get; set; }

		/// <summary>
		/// Gets the module's assembly
		/// </summary>
		public abstract AssemblyDef Assembly { get; internal set; }

		/// <summary>
		/// Gets a list of all non-nested <see cref="TypeDef"/>s
		/// </summary>
		public abstract IList<TypeDef> Types { get; }

		/// <summary>
		/// Gets a list of all <see cref="ExportedType"/>s
		/// </summary>
		public abstract IList<ExportedType> ExportedTypes { get; }

		/// <summary>
		/// Gets a list of all <see cref="Resource"/>s
		/// </summary>
		public IList<Resource> Resources {
			get { return Resources2; }
		}

		/// <summary>
		/// Gets a list of all <see cref="Resource"/>s
		/// </summary>
		internal abstract ILazyList<Resource> Resources2 { get; }

		/// <inheritdoc/>
		public string FullName {
			get { return UTF8String.ToSystemStringOrEmpty(Name); }
		}

		/// <summary>
		/// Gets/sets the path of the module or an empty string if it wasn't loaded from disk
		/// </summary>
		public abstract string Location { get; set; }

		/// <summary>
		/// Gets the <see cref="ICorLibTypes"/>
		/// </summary>
		public ICorLibTypes CorLibTypes {
			get { return corLibTypes; }
		}

		/// <summary>
		/// <c>true</c> if this is the manifest (main) module
		/// </summary>
		public bool IsManifestModule {
			get { return Assembly != null && Assembly.ManifestModule == this; }
		}

		/// <summary>
		/// Gets the global (aka. &lt;Module&gt;) type or <c>null</c> if there are no types
		/// </summary>
		public TypeDef GlobalType {
			get { return Types.Count == 0 ? null : Types[0]; }
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a file
		/// </summary>
		/// <param name="fileName">File name of an existing .NET module/assembly</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fileName"/> is <c>null</c></exception>
		/// <seealso cref="AssemblyDef.Load(string)"/>
		public static ModuleDefMD Load(string fileName) {
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			return ModuleDefMD.Load(fileName);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a byte[]
		/// </summary>
		/// <param name="data">Contents of a .NET module/assembly</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="data"/> is <c>null</c></exception>
		/// <seealso cref="AssemblyDef.Load(byte[])"/>
		public static ModuleDefMD Load(byte[] data) {
			if (data == null)
				throw new ArgumentNullException("data");
			return ModuleDefMD.Load(data);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a memory location
		/// </summary>
		/// <param name="addr">Address of a .NET module/assembly</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="addr"/> is <c>null</c></exception>
		/// <seealso cref="AssemblyDef.Load(IntPtr)"/>
		public static ModuleDefMD Load(IntPtr addr) {
			if (addr == IntPtr.Zero)
				throw new ArgumentNullException("addr");
			return ModuleDefMD.Load(addr);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a stream
		/// </summary>
		/// <remarks>This will read all bytes from the stream and call <see cref="Load(byte[])"/>.
		/// It's better to use one of the other Load() methods.</remarks>
		/// <param name="stream">The stream</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <c>null</c></exception>
		/// <seealso cref="Load(string)"/>
		/// <seealso cref="Load(byte[])"/>
		/// <seealso cref="Load(IntPtr)"/>
		/// <seealso cref="AssemblyDef.Load(Stream)"/>
		public static ModuleDefMD Load(Stream stream) {
			if (stream == null)
				throw new ArgumentNullException("stream");
			if (stream.Length > int.MaxValue)
				throw new ArgumentException("Stream is too big");
			var data = new byte[(int)stream.Length];
			stream.Position = 0;
			if (stream.Read(data, 0, data.Length) != data.Length)
				throw new IOException("Could not read all bytes from the stream");
			return Load(data);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a <see cref="DotNetFile"/>
		/// </summary>
		/// <param name="dnFile">The loaded .NET file</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance that now owns <paramref name="dnFile"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="dnFile"/> is <c>null</c></exception>
		/// <seealso cref="AssemblyDef.Load(DotNetFile)"/>
		public static ModuleDefMD Load(DotNetFile dnFile) {
			if (dnFile == null)
				throw new ArgumentNullException("dnFile");
			return ModuleDefMD.Load(dnFile);
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing"><c>true</c> if called by <see cref="Dispose()"/></param>
		protected virtual void Dispose(bool disposing) {
			if (!disposing)
				return;
			foreach (var resource in Resources) {
				if (resource != null)
					resource.Dispose();
			}
			Resources.Clear();
		}

		/// <summary>
		/// Gets all the types (including nested types) present in this module
		/// </summary>
		public IEnumerable<TypeDef> GetTypes() {
			return AllTypesHelper.Types(Types);
		}

		/// <summary>
		/// Adds <paramref name="typeDef"/> as a non-nested type. If it's already nested, its
		/// <see cref="TypeDef.DeclaringType"/> will be set to <c>null</c>.
		/// </summary>
		/// <param name="typeDef">The <see cref="TypeDef"/> to insert</param>
		public void AddAsNonNestedType(TypeDef typeDef) {
			if (typeDef == null)
				return;
			typeDef.DeclaringType = null;
			Types.Add(typeDef);
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnAdd(int index, TypeDef value, bool isLazyAdd) {
			if (isLazyAdd) {
#if DEBUG
				if (value.DeclaringType != null)
					throw new InvalidOperationException("Added nested type's DeclaringType != null");
#endif
				value.OwnerModule2 = this;
				return;
			}
			if (value.DeclaringType != null)
				throw new InvalidOperationException("Nested type is already owned by another type. Set DeclaringType to null first.");
			if (value.OwnerModule != null)
				throw new InvalidOperationException("Type is already owned by another module. Remove it from that module's type list.");
			value.OwnerModule2 = this;
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnRemove(int index, TypeDef value) {
			value.OwnerModule2 = null;
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnClear() {
			foreach (var type in Types)
				type.OwnerModule2 = null;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A Module row created by the user and not present in the original .NET file
	/// </summary>
	public class ModuleDefUser : ModuleDef {
		ushort generation;
		UTF8String name;
		Guid? mvid;
		Guid? encId;
		Guid? encBaseId;
		AssemblyDef assembly;
		LazyList<TypeDef> types;
		List<ExportedType> exportedTypes = new List<ExportedType>();
		ILazyList<Resource> resources = new LazyList<Resource>();
		string location = string.Empty;

		/// <inheritdoc/>
		public override ushort Generation {
			get { return generation; }
			set { generation = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override Guid? Mvid {
			get { return mvid; }
			set { mvid = value; }
		}

		/// <inheritdoc/>
		public override Guid? EncId {
			get { return encId; }
			set { encId = value; }
		}

		/// <inheritdoc/>
		public override Guid? EncBaseId {
			get { return encBaseId; }
			set { encBaseId = value; }
		}

		/// <inheritdoc/>
		public override AssemblyDef Assembly {
			get { return assembly; }
			internal set { assembly = value; }
		}

		/// <inheritdoc/>
		public override IList<TypeDef> Types {
			get { return types; }
		}

		/// <inheritdoc/>
		public override IList<ExportedType> ExportedTypes {
			get { return exportedTypes; }
		}

		/// <inheritdoc/>
		internal override ILazyList<Resource> Resources2 {
			get { return resources; }
		}

		/// <inheritdoc/>
		public override string Location {
			get { return location; }
			set { location = value ?? string.Empty; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ModuleDefUser()
			: this((UTF8String)null, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <remarks><see cref="Mvid"/> is initialized to a random <see cref="Guid"/></remarks>
		/// <param name="name">Module nam</param>
		public ModuleDefUser(string name)
			: this(new UTF8String(name)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <remarks><see cref="Mvid"/> is initialized to a random <see cref="Guid"/></remarks>
		/// <param name="name">Module nam</param>
		public ModuleDefUser(UTF8String name)
			: this(name, Guid.NewGuid()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Module name</param>
		/// <param name="mvid">Module version ID</param>
		public ModuleDefUser(string name, Guid? mvid)
			: this(new UTF8String(name), mvid) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Module name</param>
		/// <param name="mvid">Module version ID</param>
		public ModuleDefUser(UTF8String name, Guid? mvid) {
			this.corLibTypes = new CorLibTypes(this);
			this.types = new LazyList<TypeDef>(this);
			this.name = name;
			this.mvid = mvid;
			types.Add(CreateModuleType());
		}

		static TypeDef CreateModuleType() {
			var type = new TypeDefUser(null, "<Module>", null);
			type.Flags = TypeAttributes.NotPublic | TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass;
			return type;
		}
	}

	/// <summary>
	/// Created from a row in the Module table
	/// </summary>
	public class ModuleDefMD2 : ModuleDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawModuleRow rawRow;

		UserValue<ushort> generation;
		UserValue<UTF8String> name;
		UserValue<Guid?> mvid;
		UserValue<Guid?> encId;
		UserValue<Guid?> encBaseId;
		UserValue<AssemblyDef> assembly;
		/// <summary></summary>
		protected IList<TypeDef> types;
		/// <summary></summary>
		protected IList<ExportedType> exportedTypes;
		/// <summary></summary>
		internal ILazyList<Resource> resources;
		string location;

		/// <inheritdoc/>
		public override ushort Generation {
			get { return generation.Value; }
			set { generation.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override Guid? Mvid {
			get { return mvid.Value; }
			set { mvid.Value = value; }
		}

		/// <inheritdoc/>
		public override Guid? EncId {
			get { return encId.Value; }
			set { encId.Value = value; }
		}

		/// <inheritdoc/>
		public override Guid? EncBaseId {
			get { return encBaseId.Value; }
			set { encBaseId.Value = value; }
		}

		/// <inheritdoc/>
		public override AssemblyDef Assembly {
			get { return assembly.Value; }
			internal set { assembly.Value = value; }
		}

		/// <inheritdoc/>
		public override IList<TypeDef> Types {
			get { return types; }
		}

		/// <inheritdoc/>
		public override IList<ExportedType> ExportedTypes {
			get { return exportedTypes; }
		}

		/// <inheritdoc/>
		internal override ILazyList<Resource> Resources2 {
			get { return resources; }
		}

		/// <inheritdoc/>
		public override string Location {
			get { return location; }
			set { location = value ?? string.Empty; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Module</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ModuleDefMD2(ModuleDefMD readerModule, uint rid) {
			if (rid == 1 && readerModule == null)
				readerModule = (ModuleDefMD)this;
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid != 1 && readerModule.TablesStream.Get(Table.Module).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("Module rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			if (rid != 1) {
				this.types = new LazyList<TypeDef>(this);
				this.exportedTypes = new List<ExportedType>();
				this.resources = new LazyList<Resource>();
				this.corLibTypes = new CorLibTypes(this);
				this.location = string.Empty;
			}
			Initialize();
		}

		void Initialize() {
			generation.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.Generation;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			mvid.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.GuidStream.Read(rawRow.Mvid);
			};
			encId.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.GuidStream.Read(rawRow.EncId);
			};
			encBaseId.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.GuidStream.Read(rawRow.EncBaseId);
			};
			assembly.ReadOriginalValue = () => {
				if (rid != 1)
					return null;
				return readerModule.ResolveAssembly(1);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadModuleRow(rid) ?? new RawModuleRow();
		}
	}
}
