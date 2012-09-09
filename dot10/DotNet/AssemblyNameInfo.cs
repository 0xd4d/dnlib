using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// Stores assembly name information
	/// </summary>
	public class AssemblyNameInfo {
		AssemblyHashAlgorithm hashAlgId;
		Version version;
		AssemblyFlags flags;
		PublicKey publicKey;
		PublicKeyToken publicKeyToken;
		UTF8String name;
		UTF8String locale;

		/// <summary>
		/// Gets/sets the <see cref="AssemblyHashAlgorithm"/>
		/// </summary>
		public AssemblyHashAlgorithm HashAlgId {
			get { return hashAlgId; }
			set { hashAlgId = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="Version"/> or null if none specified
		/// </summary>
		public Version Version {
			get { return version; }
			set { version = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyFlags"/>
		/// </summary>
		public AssemblyFlags Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <summary>
		/// Gets/sets the public key or null if none specified
		/// </summary>
		public PublicKey PublicKey {
			get { return publicKey; }
			set { publicKey = value; }
		}

		/// <summary>
		/// Gets/sets the public key token or null if none specified
		/// </summary>
		public PublicKeyToken PublicKeyToken {
			get { return publicKeyToken; }
			set { publicKeyToken = value; }
		}

		/// <summary>
		/// Gets/sets the name. It is never null.
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value ?? UTF8String.Empty; }
		}

		/// <summary>
		/// Gets/sets the locale or null if none specified
		/// </summary>
		public UTF8String Locale {
			get { return locale; }
			set { locale = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmFullName">An assembly name</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmFullName"/> is null</exception>
		public AssemblyNameInfo(string asmFullName) {
			if (asmFullName == null)
				throw new ArgumentNullException("asmFullName");
			Parse(asmFullName);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is null</exception>
		public AssemblyNameInfo(AssemblyName asmName) {
			if (asmName == null)
				throw new ArgumentNullException("asmName");
			this.hashAlgId = (AssemblyHashAlgorithm)asmName.HashAlgorithm;
			this.version = asmName.Version ?? new Version(0, 0, 0, 0);
			this.flags = (AssemblyFlags)asmName.Flags;
			this.publicKey = CreatePublicKey(asmName.GetPublicKey());
			this.publicKeyToken = CreatePublicKeyToken(asmName.GetPublicKeyToken());
			this.name = new UTF8String(asmName.Name ?? string.Empty);
			this.locale = new UTF8String(asmName.CultureInfo != null && asmName.CultureInfo.Name != null ? asmName.CultureInfo.Name : "");
		}

		static PublicKey CreatePublicKey(byte[] data) {
			if (data == null)
				return null;
			return new PublicKey(data);
		}

		static PublicKeyToken CreatePublicKeyToken(byte[] data) {
			if (data == null)
				return null;
			return new PublicKeyToken(data);
		}

		/// <summary>
		/// Parses an assembly name string
		/// </summary>
		/// <param name="asmFullName">Assembly name</param>
		/// <returns><c>true</c> if no error was detected</returns>
		bool Parse(string asmFullName) {
			//TODO:  http://msdn.microsoft.com/en-us/library/yfsftwz6.aspx

			bool error = false;

			var s = asmFullName;
			int index = s.IndexOf(',');
			if (index < 0) {
				name = new UTF8String(asmFullName);
				return error;
			}

			name = new UTF8String(s.Substring(0, index));
			var kvString = s.Substring(index + 1);
			if (kvString.Trim() == "")
				return error;
			foreach (var kv in kvString.Split(',')) {
				index = kv.IndexOf('=');
				if (index < 0) {
					error = true;
					continue;
				}
				var key = kv.Substring(0, index).Trim().ToLowerInvariant();
				var value = kv.Substring(index + 1).Trim();
				switch (key) {
				case "version":
					version = ParseVersion(value);
					if (version == null)
						error = true;
					break;

				case "publickey":
					if (value == "null")
						publicKey = null;
					else {
						publicKey = CreatePublicKey(Utils.ParseBytes(value));
						if (publicKey == null)
							error = true;
					}
					break;

				case "publickeytoken":
					if (value == "null")
						publicKey = null;
					else {
						publicKeyToken = CreatePublicKeyToken(Utils.ParseBytes(value));
						if (publicKeyToken == null)
							error = true;
					}
					break;

				case "culture":
					locale = new UTF8String(value);
					break;
				}
			}

			return error;
		}

		/// <summary>
		/// Parses a version string
		/// </summary>
		/// <param name="versionString">Version string</param>
		/// <returns>A new <see cref="Version"/> or null if <paramref name="versionString"/>
		/// is an invalid version</returns>
		static Version ParseVersion(string versionString) {
			try {
				return new Version(versionString);
			}
			catch {
				return null;
			}
		}

		/// <inhertidoc/>
		public override string ToString() {
			return Utils.GetAssemblyNameString(name, version, locale, publicKey != null && !publicKey.IsNullOrEmpty ? (PublicKeyBase)publicKey : publicKeyToken);
		}
	}
}
