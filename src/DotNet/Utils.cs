// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// Compares byte arrays
	/// </summary>
	public sealed class ByteArrayEqualityComparer : IEqualityComparer<byte[]> {
		/// <summary>
		/// Default instance
		/// </summary>
		public static readonly ByteArrayEqualityComparer Instance = new ByteArrayEqualityComparer();

		/// <inheritdoc/>
		public bool Equals(byte[] x, byte[] y) {
			return Utils.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(byte[] obj) {
			return Utils.GetHashCode(obj);
		}
	}

	static class Utils {
		/// <summary>
		/// Returns an assembly name string
		/// </summary>
		/// <param name="name">Simple assembly name</param>
		/// <param name="version">Version or <c>null</c></param>
		/// <param name="culture">Culture or <c>null</c></param>
		/// <param name="publicKey">Public key / public key token or <c>null</c></param>
		/// <param name="attributes">Assembly attributes</param>
		/// <returns>An assembly name string</returns>
		internal static string GetAssemblyNameString(UTF8String name, Version version, UTF8String culture, PublicKeyBase publicKey, AssemblyAttributes attributes) {
			var sb = new StringBuilder();

			foreach (var c in UTF8String.ToSystemStringOrEmpty(name)) {
				if (c == ',' || c == '=')
					sb.Append('\\');
				sb.Append(c);
			}

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

			if ((attributes & AssemblyAttributes.Retargetable) != 0)
				sb.Append(", Retargetable=Yes");

			if ((attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_WindowsRuntime)
				sb.Append(", ContentType=WindowsRuntime");

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
		/// Compares two locales (cultures)
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		internal static bool LocaleEquals(UTF8String a, UTF8String b) {
			return LocaleCompareTo(a, b) == 0;
		}

		/// <summary>
		/// Compares two locales (cultures)
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		internal static int LocaleCompareTo(UTF8String a, string b) {
			return GetCanonicalLocale(a).CompareTo(GetCanonicalLocale(b));
		}

		/// <summary>
		/// Compares two locales (cultures)
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		internal static bool LocaleEquals(UTF8String a, string b) {
			return LocaleCompareTo(a, b) == 0;
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
			return GetCanonicalLocale(UTF8String.ToSystemStringOrEmpty(locale));
		}

		static string GetCanonicalLocale(string locale) {
			var s = locale.ToUpperInvariant();
			if (s == "NEUTRAL")
				s = string.Empty;
			return s;
		}

		/// <summary>
		/// Align up
		/// </summary>
		/// <param name="v">Value</param>
		/// <param name="alignment">Alignment</param>
		public static uint AlignUp(uint v, uint alignment) {
			return (v + alignment - 1) & ~(alignment - 1);
		}

		/// <summary>
		/// Align up
		/// </summary>
		/// <param name="v">Value</param>
		/// <param name="alignment">Alignment</param>
		public static int AlignUp(int v, uint alignment) {
			return (int)AlignUp((uint)v, alignment);
		}

		/// <summary>
		/// Gets length of compressed integer
		/// </summary>
		/// <param name="value">Integer</param>
		/// <returns>Size of compressed integer in bytes (1, 2 or 4 bytes)</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> can't be compressed (too big)</exception>
		public static int GetCompressedUInt32Length(uint value) {
			if (value <= 0x7F)
				return 1;
			if (value <= 0x3FFF)
				return 2;
			if (value <= 0x1FFFFFFF)
				return 4;
			throw new ArgumentOutOfRangeException("UInt32 value can't be compressed");
		}

		/// <summary>
		/// Writes a compressed <see cref="UInt32"/>
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="value">Value</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> can't be compressed (too big)</exception>
		public static void WriteCompressedUInt32(this BinaryWriter writer, uint value) {
			if (value <= 0x7F)
				writer.Write((byte)value);
			else if (value <= 0x3FFF) {
				writer.Write((byte)((value >> 8) | 0x80));
				writer.Write((byte)value);
			}
			else if (value <= 0x1FFFFFFF) {
				writer.Write((byte)((value >> 24) | 0xC0));
				writer.Write((byte)(value >> 16));
				writer.Write((byte)(value >> 8));
				writer.Write((byte)value);
			}
			else
				throw new ArgumentOutOfRangeException("UInt32 value can't be compressed");
		}

		/// <summary>
		/// Writes a compressed <see cref="Int32"/>
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="value">Value</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> can't be compressed (too big/small)</exception>
		public static void WriteCompressedInt32(this BinaryWriter writer, int value) {
			// This is almost identical to compressing a UInt32, except that we first
			// recode value so the sign bit is in bit 0. Then we compress it the same
			// way a UInt32 is compressed.
			uint sign = (uint)value >> 31;
			if (-0x40 <= value && value <= 0x3F) {
				uint v = (uint)((value & 0x3F) << 1) | sign;
				writer.Write((byte)v);
			}
			else if (-0x2000 <= value && value <= 0x1FFF) {
				uint v = ((uint)(value & 0x1FFF) << 1) | sign;
				writer.Write((byte)((v >> 8) | 0x80));
				writer.Write((byte)v);
			}
			else if (-0x10000000 <= value && value <= 0x0FFFFFFF) {
				uint v = ((uint)(value & 0x0FFFFFFF) << 1) | sign;
				writer.Write((byte)((v >> 24) | 0xC0));
				writer.Write((byte)(v >> 16));
				writer.Write((byte)(v >> 8));
				writer.Write((byte)v);
			}
			else
				throw new ArgumentOutOfRangeException("Int32 value can't be compressed");
		}
	}
}
