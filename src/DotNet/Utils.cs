// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// Compares byte arrays
	/// </summary>
	sealed class ByteArrayEqualityComparer : IEqualityComparer<byte[]> {
		/// <summary>
		/// Default instance
		/// </summary>
		public static readonly ByteArrayEqualityComparer Instance = new ByteArrayEqualityComparer();

		/// <inheritdoc/>
		public bool Equals(byte[]? x, byte[]? y) => Utils.Equals(x, y);

		/// <inheritdoc/>
		public int GetHashCode(byte[] obj) => Utils.GetHashCode(obj);
	}

	static class Utils {
		/// <summary>
		/// Convert a byte[] to a <see cref="string"/>
		/// </summary>
		/// <param name="bytes">All bytes</param>
		/// <param name="upper"><c>true</c> if output should be in upper case hex</param>
		/// <returns><paramref name="bytes"/> as a hex string</returns>
		internal static string ToHex(byte[]? bytes, bool upper) {
			if (bytes is null)
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
		internal static byte[]? ParseBytes(string hexString) {
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
		internal static int CompareTo(byte[]? a, byte[]? b) {
			if (a == b)
				return 0;
			if (a is null)
				return -1;
			if (b is null)
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
		internal static bool Equals(byte[]? a, byte[]? b) {
			if (a == b)
				return true;
			if (a is null || b is null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		// The files are little endian, BitConverter uses the byte order of the CPU

		internal static uint ReadUInt32LittleEndian(byte[] data, int index) =>
			data[index] | ((uint)data[index + 1] << 8) | ((uint)data[index + 2] << 16) | ((uint)data[index + 3] << 24);

		internal static int ReadInt32LittleEndian(byte[] data, int index) => (int)ReadUInt32LittleEndian(data, index);

		internal static byte[] GetLittleEndianBytes(ushort value) => new byte[2] { (byte)value, (byte)(value >> 8) };

		internal static byte[] GetLittleEndianBytes(uint value) =>
			new byte[4] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };

		internal static byte[] GetLittleEndianBytes(ulong value) =>
			new byte[8] {
				(byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24),
				(byte)(value >> 32), (byte)(value >> 40), (byte)(value >> 48), (byte)(value >> 56),
			};

		internal static unsafe byte[] GetLittleEndianBytes(float value) => GetLittleEndianBytes(*(uint*)&value);

		internal static unsafe byte[] GetLittleEndianBytes(double value) => GetLittleEndianBytes(*(ulong*)&value);

		/// <summary>
		/// Gets the hash code of a byte array
		/// </summary>
		/// <param name="a">Byte array</param>
		/// <returns>The hash code</returns>
		internal static unsafe int GetHashCode(byte[]? a) {
			if (a is null || a.Length == 0)
				return 0;
			// All bytes are hashed (xxHash32 algorithm, seed 0). Hashing only some of the bytes makes
			// it easy to create many different arrays with the same hash code, which makes the
			// dictionaries (eg. #Blob heap, FieldRVA data, method bodies) quadratic.
			// The bytes are read with the byte order of the CPU: a big endian CPU gets other hash codes, which
			// are as good. A hash code is never written to a file.
			int length = a.Length;
			uint hash;
			fixed (byte* start = a) {
				byte* p = start;
				byte* end = start + length;
				if (length >= 16) {
					uint v1 = unchecked(HashCodeCombiner.Prime1 + HashCodeCombiner.Prime2);
					uint v2 = HashCodeCombiner.Prime2;
					uint v3 = 0;
					uint v4 = unchecked(0 - HashCodeCombiner.Prime1);
					byte* limit = end - 16;
					do {
						v1 = HashCodeCombiner.Round(v1, *(uint*)p);
						v2 = HashCodeCombiner.Round(v2, *(uint*)(p + 4));
						v3 = HashCodeCombiner.Round(v3, *(uint*)(p + 8));
						v4 = HashCodeCombiner.Round(v4, *(uint*)(p + 12));
						p += 16;
					} while (p <= limit);
					hash = HashCodeCombiner.RotateLeft(v1, 1) + HashCodeCombiner.RotateLeft(v2, 7) +
						HashCodeCombiner.RotateLeft(v3, 12) + HashCodeCombiner.RotateLeft(v4, 18);
				}
				else
					hash = HashCodeCombiner.Prime5;
				hash = unchecked(hash + (uint)length);
				for (; p + 4 <= end; p += 4)
					hash = unchecked(HashCodeCombiner.RotateLeft(hash + *(uint*)p * HashCodeCombiner.Prime3, 17) * HashCodeCombiner.Prime4);
				for (; p < end; p++)
					hash = unchecked(HashCodeCombiner.RotateLeft(hash + *p * HashCodeCombiner.Prime5, 11) * HashCodeCombiner.Prime1);
			}
			return HashCodeCombiner.Finish(hash);
		}

		/// <summary>
		/// Compares two versions
		/// </summary>
		/// <remarks>This differs from <see cref="System.Version.CompareTo(Version)"/> if the build
		/// and/or revision numbers haven't been initialized or if one of the args is <c>null</c>: a number
		/// that hasn't been initialized (-1) is the same as 65535, which is how "no value" is stored in the
		/// metadata.</remarks>
		/// <param name="a">Version #1 or <c>null</c> to be treated as no version (65535.65535.65535.65535)</param>
		/// <param name="b">Version #2 or <c>null</c> to be treated as no version (65535.65535.65535.65535)</param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		internal static int CompareTo(Version? a, Version? b) {
			if (a is null)
				a = NoAssemblyVersion;
			if (b is null)
				b = NoAssemblyVersion;
			if (a.Major != b.Major)
				return a.Major.CompareTo(b.Major);
			if (a.Minor != b.Minor)
				return a.Minor.CompareTo(b.Minor);
			if (GetMetadataVersionValue(a.Build) != GetMetadataVersionValue(b.Build))
				return GetMetadataVersionValue(a.Build).CompareTo(GetMetadataVersionValue(b.Build));
			return GetMetadataVersionValue(a.Revision).CompareTo(GetMetadataVersionValue(b.Revision));
		}

		/// <summary>
		/// Checks whether two versions are the same
		/// </summary>
		/// <remarks>This differs from <see cref="System.Version.Equals(Version)"/> if the build
		/// and/or revision numbers haven't been initialized or if one of the args is <c>null</c>, see
		/// <see cref="CompareTo(Version, Version)"/>.</remarks>
		/// <param name="a">Version #1 or <c>null</c> to be treated as no version (65535.65535.65535.65535)</param>
		/// <param name="b">Version #2 or <c>null</c> to be treated as no version (65535.65535.65535.65535)</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		internal static bool Equals(Version? a, Version? b) => CompareTo(a, b) == 0;

		/// <summary>
		/// Creates a new <see cref="Version"/> instance with no undefined version values: a build or
		/// revision number that is -1 becomes 65535, which is how "no value" is stored in the metadata
		/// (the CLR and <c>System.Reflection.Metadata</c> do the same).
		/// </summary>
		/// <param name="a">A <see cref="Version"/> instance or <c>null</c> to be treated as no version
		/// (65535.65535.65535.65535)</param>
		/// <returns>A new <see cref="Version"/> instance</returns>
		internal static Version CreateVersionWithNoUndefinedValues(Version? a) {
			if (a is null)
				return NoAssemblyVersion;
			return new Version(a.Major, a.Minor, GetMetadataVersionValue(a.Build), GetMetadataVersionValue(a.Revision));
		}

		static int GetMetadataVersionValue(int val) => val == -1 ? ushort.MaxValue : val;

		/// <summary>
		/// Gets the lowest and the highest version that a requested version is: a number of 65535 (or -1)
		/// has no value, and the CLR then accepts any value for that number and for the numbers after it.
		/// No version at all (<c>null</c> or 0.0.0.0) is any version.
		/// </summary>
		/// <param name="requested">The version of an assembly reference</param>
		/// <param name="lowest">The lowest version that it is</param>
		/// <param name="highest">The highest version that it is</param>
		internal static void GetRequestedVersionRange(Version? requested, out Version lowest, out Version highest) {
			var low = new int[4];
			var high = new int[4];
			int count = 0;
			if (requested is not null && requested != zeroVersion) {
				var parts = new int[] { requested.Major, requested.Minor, requested.Build, requested.Revision };
				while (count < parts.Length && GetMetadataVersionValue(parts[count]) != ushort.MaxValue) {
					low[count] = high[count] = parts[count];
					count++;
				}
			}
			for (int i = count; i < high.Length; i++)
				high[i] = ushort.MaxValue;
			lowest = new Version(low[0], low[1], low[2], low[3]);
			highest = new Version(high[0], high[1], high[2], high[3]);
		}
		static readonly Version zeroVersion = new Version(0, 0, 0, 0);

		/// <summary>
		/// The version of a reference to an assembly that has no version: every number is 65535, "no value".
		/// The CLR does the same with a <c>System.Reflection.AssemblyName</c> without a version, and such a
		/// reference is then a reference to any version of the assembly.
		/// </summary>
		internal static readonly Version NoAssemblyVersion = new Version(ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue);

		/// <summary>
		/// Checks whether a number of a version has no value: it's 65535, or the <see cref="Version"/>
		/// doesn't have it (-1)
		/// </summary>
		/// <param name="version">A version or <c>null</c> to be treated as no version (65535.65535.65535.65535)</param>
		internal static bool HasVersionNumberWithoutValue(Version? version) =>
			version is null || (version.Major == ushort.MaxValue || version.Minor == ushort.MaxValue ||
			GetMetadataVersionValue(version.Build) == ushort.MaxValue || GetMetadataVersionValue(version.Revision) == ushort.MaxValue);

		/// <summary>
		/// Parses the version of an assembly name like System.Reflection.AssemblyName does: 2 to 4 numbers from
		/// 0 to 65535. The third and fourth number have no value (-1 in the returned <see cref="Version"/>) if
		/// they're missing or 65535.
		/// </summary>
		/// <param name="versionString">Version string</param>
		/// <returns>A new <see cref="Version"/> or <c>null</c> if <paramref name="versionString"/>
		/// is an invalid version</returns>
		internal static Version? ParseVersion(string versionString) {
			var parts = versionString.Split('.');
			if (parts.Length < 2 || parts.Length > 4)
				return null;
			var numbers = new ushort[] { ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue };
			for (int i = 0; i < parts.Length; i++) {
				if (!ushort.TryParse(parts[i], System.Globalization.NumberStyles.None, System.Globalization.NumberFormatInfo.InvariantInfo, out numbers[i]))
					return null;
			}
			if (numbers[0] == ushort.MaxValue || numbers[1] == ushort.MaxValue)
				return null;
			if (numbers[2] == ushort.MaxValue)
				return new Version(numbers[0], numbers[1]);
			if (numbers[3] == ushort.MaxValue)
				return new Version(numbers[0], numbers[1], numbers[2]);
			return new Version(numbers[0], numbers[1], numbers[2], numbers[3]);
		}

		/// <summary>
		/// Compares two locales (cultures)
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		internal static int LocaleCompareTo(UTF8String? a, UTF8String? b) => GetCanonicalLocale(a).CompareTo(GetCanonicalLocale(b));

		/// <summary>
		/// Compares two locales (cultures)
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		internal static bool LocaleEquals(UTF8String? a, UTF8String? b) => LocaleCompareTo(a, b) == 0;

		/// <summary>
		/// Compares two locales (cultures)
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		internal static int LocaleCompareTo(UTF8String? a, string b) => GetCanonicalLocale(a).CompareTo(GetCanonicalLocale(b));

		/// <summary>
		/// Compares two locales (cultures)
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		internal static bool LocaleEquals(UTF8String? a, string b) => LocaleCompareTo(a, b) == 0;

		/// <summary>
		/// Gets the hash code of a locale
		/// </summary>
		/// <param name="a">Value</param>
		/// <returns>The hash code</returns>
		internal static int GetHashCodeLocale(UTF8String? a) => GetCanonicalLocale(a).GetHashCode();

		static string GetCanonicalLocale(UTF8String? locale) => GetCanonicalLocale(UTF8String.ToSystemStringOrEmpty(locale));

		// The CLR's binder only uses the text before the first ';' of an assembly's culture
		// (AssemblyName::Init() in the runtime's binder/assemblyname.cpp)
		static string GetCanonicalLocale(string locale) {
			int index = locale.IndexOf(';');
			if (index >= 0)
				locale = locale.Substring(0, index);
			var s = locale.ToUpperInvariant();
			if (s == "NEUTRAL")
				s = string.Empty;
			return s;
		}

		/// <summary>
		/// Gets the UTF-16 (little endian) bytes of a string. Unlike <see cref="System.Text.Encoding.Unicode"/>, every
		/// <see cref="char"/> is written as is, so invalid UTF-16 (eg. a lone surrogate) isn't replaced with U+FFFD.
		/// </summary>
		/// <param name="s">String</param>
		/// <returns></returns>
		internal static byte[] GetUtf16Bytes(string s) {
			var bytes = new byte[s.Length * 2];
			for (int i = 0; i < s.Length; i++) {
				var c = s[i];
				bytes[i * 2] = (byte)c;
				bytes[i * 2 + 1] = (byte)(c >> 8);
			}
			return bytes;
		}

		/// <summary>
		/// Align up
		/// </summary>
		/// <param name="v">Value</param>
		/// <param name="alignment">Alignment</param>
		public static uint AlignUp(uint v, uint alignment) => (v + alignment - 1) & ~(alignment - 1);

		/// <summary>
		/// Align up
		/// </summary>
		/// <param name="v">Value</param>
		/// <param name="alignment">Alignment</param>
		public static int AlignUp(int v, uint alignment) => (int)AlignUp((uint)v, alignment);

		/// <summary>
		/// Rounds up the provided number to the next power of 2
		/// </summary>
		/// <param name="num">The number to round</param>
		public static uint RoundToNextPowerOfTwo(uint num) {
			num--;
			num |= num >> 1;
			num |= num >> 2;
			num |= num >> 4;
			num |= num >> 8;
			num |= num >> 16;
			return num + 1;
		}
	}
}
