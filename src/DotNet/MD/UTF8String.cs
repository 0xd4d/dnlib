using System;
using System.Diagnostics;
using System.Text;

namespace dot10.DotNet.MD {
	/// <summary>
	/// A UTF-8 encoded string where the original data is kept in memory to avoid conversions
	/// when the data is not really valid UTF-8 encoded data
	/// </summary>
	/// <remarks>When comparing strings, a byte compare is performed. The reason is that this
	/// is what the CLR does when comparing strings in the #Strings stream.</remarks>
	[DebuggerDisplay("{String}")]
	public class UTF8String : IEquatable<UTF8String>, IComparable<UTF8String> {
		/// <summary>
		/// An empty <see cref="UTF8String"/>
		/// </summary>
		public static readonly UTF8String Empty = new UTF8String(string.Empty);

		byte[] data;
		string asString;

		/// <summary>
		/// Gets the value as a UTF8 decoded string. Only use it for display purposes,
		/// not for serialization.
		/// </summary>
		public string String {
			get {
				if (asString == null)
					asString = ConvertToUTF8(data);
				return asString;
			}
		}

		/// <summary>
		/// Gets the original encoded data
		/// </summary>
		public byte[] Data {
			get { return data; }
		}

		/// <summary>
		/// Checks whether <paramref name="utf8"/> is <c>null</c> or if its data is <c>null</c>.
		/// </summary>
		/// <param name="utf8">The instance to check</param>
		/// <returns><c>true</c> if <c>null</c> or empty, <c>false</c> otherwise</returns>
		public static bool IsNull(UTF8String utf8) {
			return (object)utf8 == null || utf8.data == null;
		}

		/// <summary>
		/// Checks whether <paramref name="utf8"/> is <c>null</c> or if its data is <c>null</c> or the
		/// data is zero length.
		/// </summary>
		/// <param name="utf8">The instance to check</param>
		/// <returns><c>true</c> if <c>null</c> or empty, <c>false</c> otherwise</returns>
		public static bool IsNullOrEmpty(UTF8String utf8) {
			return (object)utf8 == null || utf8.data == null || utf8.data.Length == 0;
		}

		/// <summary>
		/// Converts it to a <see cref="string"/>
		/// </summary>
		/// <param name="utf8">The UTF-8 string instace or <c>null</c></param>
		/// <returns>A <see cref="string"/> or <c>null</c> if <paramref name="utf8"/> is <c>null</c></returns>
		public static string ToSystemString(UTF8String utf8) {
			if ((object)utf8 == null || utf8.data == null)
				return null;
			if (utf8.data.Length == 0)
				return string.Empty;
			return utf8.String;
		}

		/// <summary>
		/// Converts it to a <see cref="string"/> or an empty string if <paramref name="utf8"/> is <c>null</c>
		/// </summary>
		/// <param name="utf8">The UTF-8 string instace or <c>null</c></param>
		/// <returns>A <see cref="string"/> (never <c>null</c>)</returns>
		public static string ToSystemStringOrEmpty(UTF8String utf8) {
			return ToSystemString(utf8) ?? string.Empty;
		}

		/// <summary>
		/// Gets the hash code of a <see cref="UTF8String"/>
		/// </summary>
		/// <param name="utf8">Input</param>
		public static int GetHashCode(UTF8String utf8) {
			if (IsNullOrEmpty(utf8))
				return 0;
			return Utils.GetHashCode(utf8.data);
		}

		/// <inheritdoc/>
		public int CompareTo(UTF8String other) {
			return CompareTo(this, other);
		}

		/// <summary>
		/// Compares two <see cref="UTF8String"/> instances (case sensitive)
		/// </summary>
		/// <param name="a">Instance #1 or <c>null</c></param>
		/// <param name="b">Instance #2 or <c>null</c></param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		public static int CompareTo(UTF8String a, UTF8String b) {
			return Utils.CompareTo((object)a == null ? null : a.data, (object)b == null ? null : b.data);
		}

		/// <summary>
		/// Compares two <see cref="UTF8String"/> instances (case insensitive)
		/// </summary>
		/// <param name="a">Instance #1 or <c>null</c></param>
		/// <param name="b">Instance #2 or <c>null</c></param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		public static int CaseInsensitiveCompareTo(UTF8String a, UTF8String b) {
			if ((object)a == (object)b)
				return 0;
			var sa = ToSystemString(a);
			var sb = ToSystemString(b);
			if ((object)sa == (object)sb)
				return 0;
			if (sa == null)
				return -1;
			if (sb == null)
				return 1;
			return sa.ToLowerInvariant().CompareTo(sb.ToLowerInvariant());
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(UTF8String left, UTF8String right) {
			return CompareTo(left, right) == 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(UTF8String left, string right) {
			return CompareTo(left, new UTF8String(right)) == 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(string left, UTF8String right) {
			return CompareTo(new UTF8String(left), right) == 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(UTF8String left, UTF8String right) {
			return CompareTo(left, right) != 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(UTF8String left, string right) {
			return CompareTo(left, new UTF8String(right)) != 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(string left, UTF8String right) {
			return CompareTo(new UTF8String(left), right) != 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >(UTF8String left, UTF8String right) {
			return CompareTo(left, right) > 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <(UTF8String left, UTF8String right) {
			return CompareTo(left, right) < 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >=(UTF8String left, UTF8String right) {
			return CompareTo(left, right) >= 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <=(UTF8String left, UTF8String right) {
			return CompareTo(left, right) <= 0;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">UTF-8 data that this instance now owns</param>
		/// <exception cref="ArgumentException">If <paramref name="data"/> contains a nul byte</exception>
		public UTF8String(byte[] data) {
			this.data = data;
#if DEBUG
			if (data != null && Array.IndexOf(data, 0) >= 0)
				throw new ArgumentException("No nul bytes are allowed");
#endif
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="s">The string</param>
		public UTF8String(string s)
			: this(s == null ? null : Encoding.UTF8.GetBytes(s)) {
		}

		static string ConvertToUTF8(byte[] data) {
			if (data == null)
				return null;
			try {
				return Encoding.UTF8.GetString(data);
			}
			catch {
			}
			return null;
		}

		/// <inheritdoc/>
		public bool Equals(UTF8String other) {
			return CompareTo(this, other) == 0;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			var other = obj as UTF8String;
			if ((object)other == null)
				return false;
			return CompareTo(this, other) == 0;
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			if (data == null)
				return 0;
			uint val = 0;
			int last = data.Length - 1;
			int count = Math.Min(30, last + 1) / 2 + 1;
			for (int i = 0; i < count; i++) {
				val ^= data[i];
				val ^= (uint)(data[last - i] << 8);
				val = (val << 5) | (val >> 27);
			}
			return (int)val;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return String;
		}
	}
}
