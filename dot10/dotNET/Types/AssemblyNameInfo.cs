using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Stores assembly name information
	/// </summary>
	public class AssemblyNameInfo {
		AssemblyHashAlgorithm hashAlgId;
		Version version;
		AssemblyFlags flags;
		byte[] publicKey;
		byte[] publicKeyToken;
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
		public byte[] PublicKey {
			get { return publicKey; }
			set { publicKey = value; }
		}

		/// <summary>
		/// Gets/sets the public key token or null if none specified
		/// </summary>
		public byte[] PublicKeyToken {
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
		public AssemblyNameInfo(string asmFullName) {
			if (asmFullName == null)
				throw new ArgumentNullException("asmFullName");
			Parse(asmFullName);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		public AssemblyNameInfo(AssemblyName asmName) {
			if (asmName == null)
				throw new ArgumentNullException("asmName");
			this.hashAlgId = (AssemblyHashAlgorithm)asmName.HashAlgorithm;
			this.version = asmName.Version ?? new Version(0, 0, 0, 0);
			this.flags = (AssemblyFlags)asmName.Flags;
			this.publicKey = asmName.GetPublicKey();
			this.publicKeyToken = asmName.GetPublicKeyToken();
			this.name = new UTF8String(asmName.Name ?? string.Empty);
			this.locale = new UTF8String(asmName.CultureInfo != null && asmName.CultureInfo.Name != null ? asmName.CultureInfo.Name : "");
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
					publicKey = ParseBytes(value);
					if (publicKey == null)
						error = true;
					break;

				case "publickeytoken":
					publicKeyToken = ParseBytes(value);
					if (publicKeyToken == null)
						error = true;
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

		/// <summary>
		/// Converts a hex string to a <see cref="byte[]"/>
		/// </summary>
		/// <param name="hexString">A string with an even number of hex characters</param>
		/// <returns><paramref name="hexString"/> converted to a <see cref="byte[]"/> or null
		/// if <paramref name="hexString"/> is invalid</returns>
		static byte[] ParseBytes(string hexString) {
			try {
				if (hexString.Length % 2 != 0)
					return null;
				var bytes = new byte[hexString.Length / 2];
				for (int i = 0; i < hexString.Length; i += 2) {
					int upper = TryParseHexChar(hexString[i]);
					int lower = TryParseHexChar(hexString[i + 1]);
					if (upper < 0 || lower < 0)
						return null;
					bytes[i / 2] = (byte)((upper << 4) | lower);
				}
				return bytes;
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Converts a character to a hex digit
		/// </summary>
		/// <param name="c">Hex character</param>
		/// <returns><c>0x00</c>-<c>0x0F</c> if successful, <c>-1</c> if <paramref name="c"/> is not
		/// a valid hex digit</returns>
		static int TryParseHexChar(char c) {
			if ('0' <= c && c <= '9')
				return (ushort)c - (ushort)'0';
			if ('a' <= c && c <= 'f')
				return 10 + (ushort)c - (ushort)'a';
			if ('A' <= c && c <= 'F')
				return 10 + (ushort)c - (ushort)'A';
			return -1;
		}

		/// <inhertidoc/>
		public override string ToString() {
			if (publicKey != null && publicKey.Length > 0)
				return Extensions.GetAssemblyNameString(name, version, locale, publicKey, false);
			return Extensions.GetAssemblyNameString(name, version, locale, publicKeyToken, true);
		}
	}

	public partial class Extensions {
		/// <summary>
		/// Returns a human readable assembly name string
		/// </summary>
		/// <param name="name">Simple assembly name</param>
		/// <param name="version">Version or null</param>
		/// <param name="culture">Culture or null</param>
		/// <param name="publicKeyOrToken">Public key / public key token or null</param>
		/// <param name="isToken"><c>true</c> if <paramref name="publicKeyOrToken"/> is a public
		/// key token, <c>false</c> if <paramref name="publicKeyOrToken"/> is a public key</param>
		/// <returns>A human readable assembly name string</returns>
		internal static string GetAssemblyNameString(UTF8String name, Version version, UTF8String culture, byte[] publicKeyOrToken, bool isToken) {
			var sb = new StringBuilder();
			sb.Append(UTF8String.IsNullOrEmpty(name) ? "" : name.String);

			if (version != null) {
				sb.Append(", Version=");
				sb.Append(version.ToString());
			}

			if ((object)culture != null) {
				sb.Append(", Culture=");
				sb.Append(UTF8String.IsNullOrEmpty(culture) ? "neutral" : culture.String);
			}

			sb.Append(", ");
			sb.Append(isToken ? "PublicKeyToken=" : "PublicKey=");
			if (publicKeyOrToken != null && publicKeyOrToken.Length > 0)
				sb.Append(ToHex(publicKeyOrToken, false));
			else
				sb.Append("null");

			return sb.ToString();
		}

		static string ToHex(byte[] bytes, bool upper) {
			if (bytes == null)
				return "";
			var sb = new StringBuilder(bytes.Length / 2);
			foreach (var b in bytes) {
				sb.Append(ToHexChar(b >> 4, upper));
				sb.Append(ToHexChar(b & 0x0F, upper));
			}
			return sb.ToString();
		}

		static char ToHexChar(int val, bool upper) {
			if (0 <= val && val <= 9)
				return (char)(val + (int)'0');
			return (char)(val - 10 + (upper ? (int)'A' : (int)'a'));
		}
	}
}
