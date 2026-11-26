// dnlib: See LICENSE.txt for more info

using System;
using System.Globalization;
using System.Text;
using dnlib.DotNet;

namespace dnlib.TestUtils.Dumping {
	/// <summary>
	/// Converts values to the text that's used in dumps. Two values have the same text only if they're the same,
	/// so eg. strings are escaped and floating point numbers are shown as bits.
	/// </summary>
	static class DumpText {
		/// <summary>
		/// Returns the string in quotes. Everything that isn't printable ASCII is escaped, so different strings
		/// never look the same and a dump can be printed anywhere.
		/// </summary>
		public static string Quote(string? value) {
			if (value is null)
				return "null";
			var sb = new StringBuilder(value.Length + 2);
			sb.Append('"');
			foreach (var c in value) {
				if (c < 0x20 || c >= 0x7F || c == '"' || c == '\\')
					sb.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
				else
					sb.Append(c);
			}
			sb.Append('"');
			return sb.ToString();
		}

		/// <summary>
		/// Same as <see cref="Quote(string)"/>. If the bytes aren't valid UTF-8, the bytes are added: different
		/// invalid byte sequences are decoded to the same replacement character (U+FFFD).
		/// </summary>
		public static string Quote(UTF8String? value) {
			if (value is null)
				return "null";
			if (value.Data is null)
				return "null-data";
			var text = value.String;
			if (text.IndexOf('�') < 0)
				return Quote(text);
			return Quote(text) + Hex(value.Data);
		}

		/// <summary>
		/// Returns the bytes as hex. Long arrays are shown as their length and a hash to keep dumps small.
		/// </summary>
		public static string Hex(byte[]? data) {
			if (data is null)
				return "null";
			const int maxBytesShown = 32;
			if (data.Length <= maxBytesShown) {
				var sb = new StringBuilder(data.Length * 2 + 2);
				sb.Append('[');
				foreach (var b in data)
					sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
				sb.Append(']');
				return sb.ToString();
			}
			return "[" + data.Length.ToString(CultureInfo.InvariantCulture) + " bytes, hash " + Fnv1aHash(data).ToString("X16", CultureInfo.InvariantCulture) + "]";
		}

		static ulong Fnv1aHash(byte[] data) {
			ulong hash = 14695981039346656037;
			foreach (var b in data)
				hash = (hash ^ b) * 1099511628211;
			return hash;
		}

		/// <summary>
		/// Returns the bits of the value: there are many NaN values and two zeros, and they must stay what they are
		/// </summary>
		public static string Bits(float value) =>
			"r4:" + LittleEndian.ToUInt32(LittleEndian.GetBytes(value), 0).ToString("X8", CultureInfo.InvariantCulture);

		/// <summary>
		/// Returns the bits of the value: there are many NaN values and two zeros, and they must stay what they are
		/// </summary>
		public static string Bits(double value) =>
			"r8:" + BitConverter.DoubleToInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture);

		/// <summary>
		/// Returns the type and value of a primitive value (eg. <c>Int32:123</c>), a string or a byte array
		/// </summary>
		public static string Value(object? value) {
			switch (value) {
			case null: return "null";
			case string s: return Quote(s);
			case UTF8String s: return "utf8:" + Quote(s);
			case byte[] data: return Hex(data);
			case float f: return Bits(f);
			case double d: return Bits(d);
			case char c: return "Char:" + ((int)c).ToString("X4", CultureInfo.InvariantCulture);
			case IConvertible convertible: return value.GetType().Name + ":" + convertible.ToString(CultureInfo.InvariantCulture);
			default: return "unknown value of type " + value.GetType().Name;
			}
		}
	}
}
