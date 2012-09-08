using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using dot10.dotNET.MD;

namespace dot10.dotNET {
	/// <summary>
	/// A high-level representation of a row in the Assembly table
	/// </summary>
	[DebuggerDisplay("{GetFullNameWithPublicKeyToken()}")]
	public abstract class AssemblyDef : IHasCustomAttribute, IHasDeclSecurity {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// The manifest module
		/// </summary>
		protected ModuleDef manifestModule;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Assembly, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 14; }
		}

		/// <inheritdoc/>
		public int HasDeclSecurityTag {
			get { return 2; }
		}

		/// <summary>
		/// From column Assembly.HashAlgId
		/// </summary>
		public abstract AssemblyHashAlgorithm HashAlgId { get; set; }

		/// <summary>
		/// From columns Assembly.MajorVersion, Assembly.MinorVersion, Assembly.BuildNumber,
		/// Assembly.RevisionNumber.
		/// </summary>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null</exception>
		public abstract Version Version { get; set; }

		/// <summary>
		/// From column Assembly.Flags
		/// </summary>
		public abstract AssemblyFlags Flags { get; set; }

		/// <summary>
		/// From column Assembly.PublicKey
		/// </summary>
		/// <remarks>An empty <see cref="PublicKey"/> is created if the caller writes <c>null</c></remarks>
		public abstract PublicKey PublicKey { get; set; }

		/// <summary>
		/// Gets the public key token which is calculated from <see cref="PublicKey"/>
		/// </summary>
		public PublicKeyToken PublicKeyToken {
			get { return PublicKey.Token; }
		}

		/// <summary>
		/// From column Assembly.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column Assembly.Locale
		/// </summary>
		public abstract UTF8String Locale { get; set; }

		/// <summary>
		/// Gets/sets the manifest (main) module
		/// </summary>
		public ModuleDef ManifestModule {
			get { return manifestModule; }
			set { manifestModule = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyFlags.PublicKey"/> bit
		/// </summary>
		public bool HasPublicKey {
			get { return (Flags & AssemblyFlags.PublicKey) != 0; }
			set {
				if (value)
					Flags |= AssemblyFlags.PublicKey;
				else
					Flags &= ~AssemblyFlags.PublicKey;
			}
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public AssemblyFlags ProcessorArchitecture {
			get { return Flags & AssemblyFlags.PA_Mask; }
			set { Flags = (Flags & ~AssemblyFlags.PA_Mask) | (value & AssemblyFlags.PA_Mask); }
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public AssemblyFlags ProcessorArchitectureFull {
			get { return Flags & AssemblyFlags.PA_FullMask; }
			set { Flags = (Flags & ~AssemblyFlags.PA_FullMask) | (value & AssemblyFlags.PA_FullMask); }
		}

		/// <summary>
		/// <c>true</c> if unspecified processor architecture
		/// </summary>
		public bool IsProcessorArchitectureNone {
			get { return (Flags & AssemblyFlags.PA_Mask) == AssemblyFlags.PA_None; }
		}

		/// <summary>
		/// <c>true</c> if neutral (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureMSIL {
			get { return (Flags & AssemblyFlags.PA_Mask) == AssemblyFlags.PA_MSIL; }
		}

		/// <summary>
		/// <c>true</c> if x86 (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureX86 {
			get { return (Flags & AssemblyFlags.PA_Mask) == AssemblyFlags.PA_x86; }
		}

		/// <summary>
		/// <c>true</c> if IA-64 (PE32+) architecture
		/// </summary>
		public bool IsProcessorArchitectureIA64 {
			get { return (Flags & AssemblyFlags.PA_Mask) == AssemblyFlags.PA_IA64; }
		}

		/// <summary>
		/// <c>true</c> if x64 (PE32+) architecture
		/// </summary>
		public bool IsProcessorArchitectureX64 {
			get { return (Flags & AssemblyFlags.PA_Mask) == AssemblyFlags.PA_AMD64; }
		}

		/// <summary>
		/// <c>true</c> if ARM (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureARM {
			get { return (Flags & AssemblyFlags.PA_Mask) == AssemblyFlags.PA_ARM; }
		}

		/// <summary>
		/// <c>true</c> if eg. reference assembly (not runnable)
		/// </summary>
		public bool IsProcessorArchitectureNoPlatform {
			get { return (Flags & AssemblyFlags.PA_Mask) == AssemblyFlags.PA_NoPlatform; }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyFlags.PA_Specified"/> bit
		/// </summary>
		public bool IsProcessorArchitectureSpecified {
			get { return (Flags & AssemblyFlags.PA_Specified) != 0; }
			set {
				if (value)
					Flags |= AssemblyFlags.PA_Specified;
				else
					Flags &= ~AssemblyFlags.PA_Specified;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyFlags.EnableJITcompileTracking"/> bit
		/// </summary>
		public bool EnableJITcompileTracking {
			get { return (Flags & AssemblyFlags.EnableJITcompileTracking) != 0; }
			set {
				if (value)
					Flags |= AssemblyFlags.EnableJITcompileTracking;
				else
					Flags &= ~AssemblyFlags.EnableJITcompileTracking;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyFlags.DisableJITcompileOptimizer"/> bit
		/// </summary>
		public bool DisableJITcompileOptimizer {
			get { return (Flags & AssemblyFlags.DisableJITcompileOptimizer) != 0; }
			set {
				if (value)
					Flags |= AssemblyFlags.DisableJITcompileOptimizer;
				else
					Flags &= ~AssemblyFlags.DisableJITcompileOptimizer;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyFlags.Retargetable"/> bit
		/// </summary>
		public bool IsRetargetable {
			get { return (Flags & AssemblyFlags.Retargetable) != 0; }
			set {
				if (value)
					Flags |= AssemblyFlags.Retargetable;
				else
					Flags &= ~AssemblyFlags.Retargetable;
			}
		}

		/// <summary>
		/// Gets/sets the content type
		/// </summary>
		public AssemblyFlags ContentType {
			get { return Flags & AssemblyFlags.ContentType_Mask; }
			set { Flags = (Flags & ~AssemblyFlags.ContentType_Mask) | (value & AssemblyFlags.ContentType_Mask); }
		}

		/// <summary>
		/// <c>true</c> if content type is <c>WindowsRuntime</c>
		/// </summary>
		public bool IsContentTypeWindowsRuntime {
			get { return (Flags & AssemblyFlags.ContentType_Mask) == AssemblyFlags.ContentType_WindowsRuntime; }
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a file
		/// </summary>
		/// <param name="fileName">File name of an existing .NET assembly</param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fileName"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		/// <seealso cref="ModuleDef.Load(string)"/>
		public static AssemblyDef Load(string fileName) {
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(fileName);
				var asm = module.Assembly;
				if (asm == null)
					throw new BadImageFormatException(string.Format("{0} is only a .NET module, not a .NET assembly. Use ModuleDef.Load().", fileName));
				return asm;
			}
			catch {
				if (module != null)
					module.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a byte[]
		/// </summary>
		/// <param name="data">Contents of a .NET assembly</param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="data"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		/// <seealso cref="ModuleDef.Load(byte[])"/>
		public static AssemblyDef Load(byte[] data) {
			if (data == null)
				throw new ArgumentNullException("data");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(data);
				var asm = module.Assembly;
				if (asm == null)
					throw new BadImageFormatException(string.Format("{0} is only a .NET module, not a .NET assembly. Use ModuleDef.Load().", module.ToString()));
				return asm;
			}
			catch {
				if (module != null)
					module.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a memory location
		/// </summary>
		/// <param name="addr">Address of a .NET assembly</param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="addr"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		/// <seealso cref="ModuleDef.Load(IntPtr)"/>
		public static AssemblyDef Load(IntPtr addr) {
			if (addr == IntPtr.Zero)
				throw new ArgumentNullException("addr");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(addr);
				var asm = module.Assembly;
				if (asm == null)
					throw new BadImageFormatException(string.Format("{0} (addr: {1:X8}) is only a .NET module, not a .NET assembly. Use ModuleDef.Load().", module.ToString(), addr.ToInt64()));
				return asm;
			}
			catch {
				if (module != null)
					module.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a stream
		/// </summary>
		/// <remarks>This will read all bytes from the stream and call <see cref="Load(byte[])"/>.
		/// It's better to use one of the other Load() methods.</remarks>
		/// <param name="stream">The stream</param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		/// <seealso cref="Load(string)"/>
		/// <seealso cref="Load(byte[])"/>
		/// <seealso cref="Load(IntPtr)"/>
		/// <seealso cref="ModuleDef.Load(Stream)"/>
		public static AssemblyDef Load(Stream stream) {
			if (stream == null)
				throw new ArgumentNullException("stream");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(stream);
				var asm = module.Assembly;
				if (asm == null)
					throw new BadImageFormatException(string.Format("{0} is only a .NET module, not a .NET assembly. Use ModuleDef.Load().", module.ToString()));
				return asm;
			}
			catch {
				if (module != null)
					module.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a <see cref="DotNetFile"/>
		/// </summary>
		/// <param name="dnFile">The loaded .NET file</param>
		/// <returns>A new <see cref="AssemblyDef"/> instance that now owns <paramref name="dnFile"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="dnFile"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		/// <seealso cref="ModuleDef.Load(DotNetFile)"/>
		public static AssemblyDef Load(DotNetFile dnFile) {
			if (dnFile == null)
				throw new ArgumentNullException("dnFile");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(dnFile);
				var asm = module.Assembly;
				if (asm == null)
					throw new BadImageFormatException(string.Format("{0} is only a .NET module, not a .NET assembly. Use ModuleDef.Load().", module.ToString()));
				return asm;
			}
			catch {
				if (module != null)
					module.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Gets the assembly name with the public key
		/// </summary>
		public string GetFullNameWithPublicKey() {
			return GetFullName(PublicKey);
		}

		/// <summary>
		/// Gets the assembly name with the public key token
		/// </summary>
		public string GetFullNameWithPublicKeyToken() {
			return GetFullName(PublicKeyToken);
		}

		string GetFullName(PublicKeyBase pkBase) {
			return Utils.GetAssemblyNameString(Name, Version, Locale, pkBase);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return GetFullNameWithPublicKeyToken();
		}
	}

	/// <summary>
	/// An Assembly row created by the user and not present in the original .NET file
	/// </summary>
	public class AssemblyDefUser : AssemblyDef {
		AssemblyHashAlgorithm hashAlgId;
		Version version;
		AssemblyFlags flags;
		PublicKey publicKey;
		UTF8String name;
		UTF8String locale;

		/// <inheritdoc/>
		public override AssemblyHashAlgorithm HashAlgId {
			get { return hashAlgId; }
			set {
				hashAlgId = value;
				publicKey.HashAlgorithm = hashAlgId;
			}
		}

		/// <inheritdoc/>
		public override Version Version {
			get { return version; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				version = value;
			}
		}

		/// <inheritdoc/>
		public override AssemblyFlags Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override PublicKey PublicKey {
			get { return publicKey; }
			set {
				publicKey = value ?? new PublicKey();
				publicKey.HashAlgorithm = hashAlgId;
			}
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Locale {
			get { return locale; }
			set { locale = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyDefUser()
			: this(UTF8String.Empty, new Version()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(string name, Version version)
			: this(name, version, new PublicKey()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(string name, Version version, PublicKey publicKey)
			: this(name, version, publicKey, "") {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key</param>
		/// <param name="locale">Locale</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(string name, Version version, PublicKey publicKey, string locale)
			: this(new UTF8String(name), version, publicKey, new UTF8String(locale)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(UTF8String name, Version version)
			: this(name, version, new PublicKey()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(UTF8String name, Version version, PublicKey publicKey)
			: this(name, version, publicKey, UTF8String.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key</param>
		/// <param name="locale">Locale</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(UTF8String name, Version version, PublicKey publicKey, UTF8String locale) {
			if ((object)name == null)
				throw new ArgumentNullException("name");
			if (version == null)
				throw new ArgumentNullException("version");
			if ((object)locale == null)
				throw new ArgumentNullException("locale");
			this.name = name;
			this.version = version;
			this.publicKey = publicKey ?? new PublicKey();
			this.locale = locale;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is null</exception>
		public AssemblyDefUser(AssemblyName asmName)
			: this(new AssemblyNameInfo(asmName)) {
			this.hashAlgId = (AssemblyHashAlgorithm)asmName.HashAlgorithm;
			this.flags = (AssemblyFlags)asmName.Flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is null</exception>
		public AssemblyDefUser(AssemblyNameInfo asmName) {
			if (asmName == null)
				throw new ArgumentNullException("asmName");
			this.name = asmName.Name;
			this.version = asmName.Version ?? new Version();
			this.publicKey = asmName.PublicKey ?? new PublicKey();
			this.locale = asmName.Locale;
			this.flags = AssemblyFlags.None;
			this.hashAlgId = AssemblyHashAlgorithm.SHA1;
		}
	}

	/// <summary>
	/// Created from a row in the Assembly table
	/// </summary>
	sealed class AssemblyDefMD : AssemblyDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawAssemblyRow rawRow;
		UserValue<AssemblyHashAlgorithm> hashAlgId;
		UserValue<Version> version;
		UserValue<AssemblyFlags> flags;
		UserValue<PublicKey> publicKey;
		UserValue<UTF8String> name;
		UserValue<UTF8String> locale;

		/// <inheritdoc/>
		public override AssemblyHashAlgorithm HashAlgId {
			get { return hashAlgId.Value; }
			set {
				hashAlgId.Value = value;
				publicKey.Value.HashAlgorithm = hashAlgId.Value;
			}
		}

		/// <inheritdoc/>
		public override Version Version {
			get { return version.Value; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				version.Value = value;
			}
		}

		/// <inheritdoc/>
		public override AssemblyFlags Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override PublicKey PublicKey {
			get { return publicKey.Value; }
			set {
				publicKey.Value = value ?? new PublicKey();
				publicKey.Value.HashAlgorithm = hashAlgId.Value;
			}
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Locale {
			get { return locale.Value; }
			set { locale.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Assembly</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public AssemblyDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.Assembly).Rows < rid)
				throw new BadImageFormatException(string.Format("Assembly rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			hashAlgId.ReadOriginalValue = () => {
				InitializeRawRow();
				return (AssemblyHashAlgorithm)rawRow.HashAlgId;
			};
			version.ReadOriginalValue = () => {
				InitializeRawRow();
				return new Version(rawRow.MajorVersion, rawRow.MinorVersion, rawRow.BuildNumber, rawRow.RevisionNumber);
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (AssemblyFlags)rawRow.Flags;
			};
			publicKey.ReadOriginalValue = () => {
				InitializeRawRow();
				return new PublicKey(readerModule.BlobStream.Read(rawRow.PublicKey));
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			locale.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Locale);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadAssemblyRow(rid);
		}
	}
}
