using System;
using System.Reflection;
using dot10.dotNET.MD;

namespace dot10.dotNET.Hi {
	/// <summary>
	/// A high-level representation of a row in the AssemblyRef table
	/// </summary>
	public abstract class AssemblyRef : IHasCustomAttribute, IImplementation, IResolutionScope {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.AssemblyRef, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 15; }
		}

		/// <inheritdoc/>
		public int ImplementationTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int ResolutionScopeTag {
			get { return 2; }
		}

		/// <summary>
		/// From columns AssemblyRef.MajorVersion, AssemblyRef.MinorVersion,
		/// AssemblyRef.BuildNumber, AssemblyRef.RevisionNumber
		/// </summary>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null</exception>
		public abstract Version Version { get; set; }

		/// <summary>
		/// From column AssemblyRef.Flags
		/// </summary>
		public abstract AssemblyFlags Flags { get; set; }

		/// <summary>
		/// From column AssemblyRef.PublicKeyOrToken
		/// </summary>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null</exception>
		public abstract PublicKeyBase PublicKeyOrToken { get; set; }

		/// <summary>
		/// From column AssemblyRef.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column AssemblyRef.Locale
		/// </summary>
		public abstract UTF8String Locale { get; set; }

		/// <summary>
		/// From column AssemblyRef.HashValue
		/// </summary>
		public abstract byte[] HashValue { get; set; }

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

		/// <inheritdoc/>
		public override string ToString() {
			return Utils.GetAssemblyNameString(Name, Version, Locale, PublicKeyOrToken);
		}
	}

	/// <summary>
	/// An AssemblyRef row created by the user and not present in the original .NET file
	/// </summary>
	public class AssemblyRefUser : AssemblyRef {
		Version version;
		AssemblyFlags flags;
		PublicKeyBase publicKeyOrToken;
		UTF8String name;
		UTF8String locale;
		byte[] hashValue;

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
			set { flags = (value & ~AssemblyFlags.PublicKey) | (flags & AssemblyFlags.PublicKey); }
		}

		/// <inheritdoc/>
		public override PublicKeyBase PublicKeyOrToken {
			get { return publicKeyOrToken; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				if (value is PublicKey)
					flags |= AssemblyFlags.PublicKey;
				else
					flags &= ~AssemblyFlags.PublicKey;
				publicKeyOrToken = value;
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

		/// <inheritdoc/>
		public override byte[] HashValue {
			get { return hashValue; }
			set { hashValue = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyRefUser()
			: this(UTF8String.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyRefUser(UTF8String name)
			: this(name, new Version()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyRefUser(UTF8String name, Version version)
			: this(name, version, new PublicKey()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key or public key token</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyRefUser(UTF8String name, Version version, PublicKeyBase publicKey)
			: this(name, version, publicKey, UTF8String.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key or public key token</param>
		/// <param name="locale">Locale</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyRefUser(UTF8String name, Version version, PublicKeyBase publicKey, UTF8String locale) {
			if ((object)name == null)
				throw new ArgumentNullException("name");
			if (version == null)
				throw new ArgumentNullException("version");
			if ((object)locale == null)
				throw new ArgumentNullException("locale");
			this.name = name;
			this.version = version;
			this.publicKeyOrToken = publicKey;
			this.locale = locale;
			this.flags = publicKey is PublicKey ? AssemblyFlags.PublicKey : AssemblyFlags.None;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyRefUser(string name, Version version)
			: this(name, version, new PublicKey()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key or public key token</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyRefUser(string name, Version version, PublicKeyBase publicKey)
			: this(name, version, publicKey, string.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key or public key token</param>
		/// <param name="locale">Locale</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyRefUser(string name, Version version, PublicKeyBase publicKey, string locale)
			: this(new UTF8String(name), version, publicKey, new UTF8String(locale)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is null</exception>
		public AssemblyRefUser(AssemblyName asmName)
			: this(new AssemblyNameInfo(asmName)) {
			this.flags = (AssemblyFlags)asmName.Flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is null</exception>
		public AssemblyRefUser(AssemblyNameInfo asmName) {
			if (asmName == null)
				throw new ArgumentNullException("asmName");

			this.version = asmName.Version ?? new Version();
			if (!asmName.PublicKey.IsNullOrEmpty)
				this.publicKeyOrToken = asmName.PublicKey;
			else
				this.publicKeyOrToken = asmName.PublicKeyToken;
			this.name = asmName.Name;
			this.locale = asmName.Locale;
			this.flags = publicKeyOrToken is PublicKey ? AssemblyFlags.PublicKey : AssemblyFlags.None;
		}
	}

	/// <summary>
	/// Created from a row in the AssemblyRef table
	/// </summary>
	sealed class AssemblyRefMD : AssemblyRef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawAssemblyRefRow rawRow;

		UserValue<Version> version;
		UserValue<AssemblyFlags> flags;
		UserValue<PublicKeyBase> publicKeyOrToken;
		UserValue<UTF8String> name;
		UserValue<UTF8String> locale;
		UserValue<byte[]> hashValue;

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
			set { flags.Value = (value & ~AssemblyFlags.PublicKey) | (flags.Value & AssemblyFlags.PublicKey); }
		}

		/// <inheritdoc/>
		public override PublicKeyBase PublicKeyOrToken {
			get { return publicKeyOrToken.Value; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				if (value is PublicKey)
					flags.Value |= AssemblyFlags.PublicKey;
				else
					flags.Value &= ~AssemblyFlags.PublicKey;
				publicKeyOrToken.Value = value;
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

		/// <inheritdoc/>
		public override byte[] HashValue {
			get { return hashValue.Value; }
			set { hashValue.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>AssemblyRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public AssemblyRefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.AssemblyRef).Rows < rid)
				throw new BadImageFormatException(string.Format("AssemblyRef rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			version.ReadOriginalValue = () => {
				InitializeRawRow();
				return new Version(rawRow.MajorVersion, rawRow.MinorVersion, rawRow.BuildNumber, rawRow.RevisionNumber);
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (AssemblyFlags)rawRow.Flags;
			};
			publicKeyOrToken.ReadOriginalValue = () => {
				InitializeRawRow();
				var pkData = readerModule.BlobStream.Read(rawRow.PublicKeyOrToken);
				if ((rawRow.Flags & (uint)AssemblyFlags.PublicKey) != 0)
					return new PublicKey(pkData);
				return new PublicKeyToken(pkData);
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			locale.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Locale);
			};
			hashValue.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.BlobStream.Read(rawRow.HashValue);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadAssemblyRefRow(rid);
		}
	}
}
