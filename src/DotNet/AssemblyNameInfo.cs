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
		PublicKeyBase publicKeyOrToken;
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
		/// Gets/sets the <see cref="Version"/> or <c>null</c> if none specified
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
		/// Gets/sets the public key or token
		/// </summary>
		public PublicKeyBase PublicKeyOrToken {
			get { return publicKeyOrToken; }
			set { publicKeyOrToken = value; }
		}

		/// <summary>
		/// Gets/sets the name. It is never <c>null</c>.
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value ?? UTF8String.Empty; }
		}

		/// <summary>
		/// Gets/sets the locale or <c>null</c> if none specified
		/// </summary>
		public UTF8String Locale {
			get { return locale; }
			set { locale = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmFullName">An assembly name</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmFullName"/> is <c>null</c></exception>
		public AssemblyNameInfo(string asmFullName) {
			if (asmFullName == null)
				throw new ArgumentNullException("asmFullName");
			Parse(asmFullName);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asm">The assembly</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asm"/> is <c>null</c></exception>
		public AssemblyNameInfo(IAssembly asm) {
			if (asm == null)
				throw new ArgumentNullException("asmName");
			var asmDef = asm as AssemblyDef;
			this.hashAlgId = asmDef == null ? 0 : asmDef.HashAlgId;
			this.version = asm.Version ?? new Version(0, 0, 0, 0);
			this.flags = asm.Flags;
			this.publicKeyOrToken = asm.PublicKeyOrToken;
			this.name = UTF8String.IsNullOrEmpty(asm.Name) ? UTF8String.Empty : asm.Name;
			this.locale = UTF8String.IsNullOrEmpty(asm.Locale) ? UTF8String.Empty : asm.Locale;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is <c>null</c></exception>
		public AssemblyNameInfo(AssemblyName asmName) {
			if (asmName == null)
				throw new ArgumentNullException("asmName");
			this.hashAlgId = (AssemblyHashAlgorithm)asmName.HashAlgorithm;
			this.version = asmName.Version ?? new Version(0, 0, 0, 0);
			this.flags = (AssemblyFlags)asmName.Flags;
			this.publicKeyOrToken = (PublicKeyBase)CreatePublicKey(asmName.GetPublicKey()) ?? CreatePublicKeyToken(asmName.GetPublicKeyToken());
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
						publicKeyOrToken = null;
					else {
						publicKeyOrToken = CreatePublicKey(Utils.ParseBytes(value));
						if (publicKeyOrToken == null)
							error = true;
					}
					break;

				case "publickeytoken":
					if (value == "null")
						publicKeyOrToken = null;
					else {
						publicKeyOrToken = CreatePublicKeyToken(Utils.ParseBytes(value));
						if (publicKeyOrToken == null)
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
		/// <returns>A new <see cref="Version"/> or <c>null</c> if <paramref name="versionString"/>
		/// is an invalid version</returns>
		static Version ParseVersion(string versionString) {
			try {
				return Utils.CreateVersionWithNoUndefinedValues(new Version(versionString));
			}
			catch {
				return null;
			}
		}

		/// <inhertidoc/>
		public override string ToString() {
			return Utils.GetAssemblyNameString(name, version, locale, publicKeyOrToken);
		}
	}
}
