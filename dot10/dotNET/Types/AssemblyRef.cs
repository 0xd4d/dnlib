using System;
using System.Reflection;

namespace dot10.dotNET.Types {
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
}
