using System;
using System.IO;
using System.Reflection;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the Assembly table
	/// </summary>
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
		public abstract byte[] PublicKey { get; set; }

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
	}

	/// <summary>
	/// An Assembly row created by the user and not present in the original .NET file
	/// </summary>
	public class AssemblyDefUser : AssemblyDef {
		AssemblyHashAlgorithm hashAlgId;
		Version version;
		AssemblyFlags flags;
		byte[] publicKey;
		UTF8String name;
		UTF8String locale;

		/// <inheritdoc/>
		public override AssemblyHashAlgorithm HashAlgId {
			get { return hashAlgId; }
			set { hashAlgId = value; }
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
		public override byte[] PublicKey {
			get { return publicKey; }
			set { publicKey = value; }
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
			: this(name, version, new byte[0]) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(string name, Version version, byte[] publicKey)
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
		public AssemblyDefUser(string name, Version version, byte[] publicKey, string locale)
			: this(new UTF8String(name), version, publicKey, new UTF8String(locale)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(UTF8String name, Version version)
			: this(name, version, new byte[0]) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(UTF8String name, Version version, byte[] publicKey)
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
		public AssemblyDefUser(UTF8String name, Version version, byte[] publicKey, UTF8String locale) {
			if ((object)name == null)
				throw new ArgumentNullException("name");
			if (version == null)
				throw new ArgumentNullException("version");
			if ((object)locale == null)
				throw new ArgumentNullException("locale");
			this.name = name;
			this.version = version;
			this.publicKey = publicKey;
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
			this.publicKey = asmName.PublicKey;
			this.locale = asmName.Locale;
			this.flags = AssemblyFlags.None;
			this.hashAlgId = AssemblyHashAlgorithm.SHA1;
		}
	}
}
