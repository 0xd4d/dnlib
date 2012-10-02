using System;
using System.Collections.Generic;
using System.Text;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	static class Utils {
		/// <summary>
		/// Returns an assembly name string
		/// </summary>
		/// <param name="name">Simple assembly name</param>
		/// <param name="version">Version or <c>null</c></param>
		/// <param name="culture">Culture or <c>null</c></param>
		/// <param name="publicKey">Public key / public key token or <c>null</c></param>
		/// <returns>An assembly name string</returns>
		internal static string GetAssemblyNameString(UTF8String name, Version version, UTF8String culture, PublicKeyBase publicKey) {
			var sb = new StringBuilder();
			sb.Append(UTF8String.ToSystemStringOrEmpty(name));

			if (version != null) {
				sb.Append(", Version=");
				sb.Append(CreateVersionWithNoUndefinedValues(version).ToString());
			}

			if ((object)culture != null) {
				sb.Append(", Culture=");
				sb.Append(UTF8String.IsNullOrEmpty(culture) ? "neutral" : culture.String);
			}

			sb.Append(", ");
			sb.Append(publicKey == null || publicKey is PublicKeyToken ? "PublicKeyToken=" : "PublicKey=");
			sb.Append(publicKey == null ? "null" : publicKey.ToString());

			return sb.ToString();
		}

		/// <summary>
		/// Convert a byte[] to a <see cref="string"/>
		/// </summary>
		/// <param name="bytes">All bytes</param>
		/// <param name="upper"><c>true</c> if output should be in upper case hex</param>
		/// <returns><paramref name="bytes"/> as a hex string</returns>
		internal static string ToHex(byte[] bytes, bool upper) {
			if (bytes == null)
				return "";
			var chars = new char[bytes.Length * 2];
			for (int i = 0, j = 0; i < bytes.Length; i++) {
				byte b = bytes[i];
				chars[j++] = ToHexChar(b >> 4, upper);
				chars[j++] = ToHexChar(b & 0x0F, upper);
			}
			return new string(chars);
		}

		static char ToHexChar(int val, bool upper) {
			if (0 <= val && val <= 9)
				return (char)(val + (int)'0');
			return (char)(val - 10 + (upper ? (int)'A' : (int)'a'));
		}

		/// <summary>
		/// Converts a hex string to a byte[]
		/// </summary>
		/// <param name="hexString">A string with an even number of hex characters</param>
		/// <returns><paramref name="hexString"/> converted to a byte[] or <c>null</c>
		/// if <paramref name="hexString"/> is invalid</returns>
		internal static byte[] ParseBytes(string hexString) {
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

		/// <summary>
		/// Compares two byte arrays
		/// </summary>
		/// <param name="a">Byte array #1</param>
		/// <param name="b">Byte array #2</param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		internal static int CompareTo(byte[] a, byte[] b) {
			if (a == b)
				return 0;
			if (a == null)
				return -1;
			if (b == null)
				return 1;
			int count = Math.Min(a.Length, b.Length);
			for (int i = 0; i < count; i++) {
				var ai = a[i];
				var bi = b[i];
				if (ai < bi)
					return -1;
				if (ai > bi)
					return 1;
			}
			return a.Length.CompareTo(b.Length);
		}

		/// <summary>
		/// Checks whether two byte arrays are equal
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		internal static bool Equals(byte[] a, byte[] b) {
			return CompareTo(a, b) == 0;
		}

		/// <summary>
		/// Gets the hash code of a byte array
		/// </summary>
		/// <param name="a">Byte array</param>
		/// <returns>The hash code</returns>
		internal static int GetHashCode(byte[] a) {
			if (a == null || a.Length == 0)
				return 0;
			int count = Math.Min(a.Length / 2, 20);
			if (count == 0)
				count = 1;
			uint hash = 0;
			for (int i = 0, j = a.Length - 1; i < count; i++, j--) {
				hash ^= a[i] | ((uint)a[j] << 8);
				hash = (hash << 13) | (hash >> 19);
			}
			return (int)hash;
		}

		/// <summary>
		/// Compares two versions
		/// </summary>
		/// <remarks>This differs from <see cref="System.Version.CompareTo(Version)"/> if the build
		/// and/or revision numbers haven't been initialized or if one of the args is <c>null</c>.
		/// </remarks>
		/// <param name="a">Version #1 or <c>null</c> to be treated as v0.0.0.0</param>
		/// <param name="b">Version #2 or <c>null</c> to be treated as v0.0.0.0</param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		internal static int CompareTo(Version a, Version b) {
			if (a == null)
				a = new Version();
			if (b == null)
				b = new Version();
			if (a.Major != b.Major)
				return a.Major.CompareTo(b.Major);
			if (a.Minor != b.Minor)
				return a.Minor.CompareTo(b.Minor);
			if (GetDefaultVersionValue(a.Build) != GetDefaultVersionValue(b.Build))
				return GetDefaultVersionValue(a.Build).CompareTo(GetDefaultVersionValue(b.Build));
			return GetDefaultVersionValue(a.Revision).CompareTo(GetDefaultVersionValue(b.Revision));
		}

		/// <summary>
		/// Checks whether two versions are the same
		/// </summary>
		/// <remarks>This differs from <see cref="System.Version.Equals(Version)"/> if the build
		/// and/or revision numbers haven't been initialized or if one of the args is <c>null</c>.
		/// </remarks>
		/// <param name="a">Version #1 or <c>null</c> to be treated as v0.0.0.0</param>
		/// <param name="b">Version #2 or <c>null</c> to be treated as v0.0.0.0</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		internal static bool Equals(Version a, Version b) {
			return CompareTo(a, b) == 0;
		}

		/// <summary>
		/// Creates a new <see cref="Version"/> instance with no undefined version values (eg.
		/// the build and revision values won't be -1).
		/// </summary>
		/// <param name="a">A <see cref="Version"/> instance</param>
		/// <returns>A new <see cref="Version"/> instance</returns>
		internal static Version CreateVersionWithNoUndefinedValues(Version a) {
			if (a == null)
				return new Version(0, 0, 0, 0);
			return new Version(a.Major, a.Minor, GetDefaultVersionValue(a.Build), GetDefaultVersionValue(a.Revision));
		}

		static int GetDefaultVersionValue(int val) {
			return val == -1 ? 0 : val;
		}

		/// <summary>
		/// Parses a version string
		/// </summary>
		/// <param name="versionString">Version string</param>
		/// <returns>A new <see cref="Version"/> or <c>null</c> if <paramref name="versionString"/>
		/// is an invalid version</returns>
		internal static Version ParseVersion(string versionString) {
			try {
				return Utils.CreateVersionWithNoUndefinedValues(new Version(versionString));
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Compares two locales (cultures)
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		internal static int LocaleCompareTo(UTF8String a, UTF8String b) {
			return GetCanonicalLocale(a).CompareTo(GetCanonicalLocale(b));
		}

		/// <summary>
		/// Gets the hash code of a locale
		/// </summary>
		/// <param name="a">Value</param>
		/// <returns>The hash code</returns>
		internal static int GetHashCodeLocale(UTF8String a) {
			return GetCanonicalLocale(a).GetHashCode();
		}

		static string GetCanonicalLocale(UTF8String locale) {
			var s = UTF8String.ToSystemStringOrEmpty(locale).ToLowerInvariant();
			if (s == "neutral")
				s = string.Empty;
			return s;
		}
	}
}
