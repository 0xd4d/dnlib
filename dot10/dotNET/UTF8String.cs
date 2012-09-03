using System;
using System.Diagnostics;
using System.Text;

namespace dot10.dotNET {
	/// <summary>
	/// A UTF-8 encoded string where the original data is kept in memory to avoid conversions
	/// when the data is not really valid UTF-8 encoded data
	/// </summary>
	/// <remarks>When comparing strings, a byte compare is performed. The reason is that this
	/// is what the CLR does when comparing strings in the #Strings stream.</remarks>
	[DebuggerDisplay("{String}")]
	public class UTF8String : IEquatable<UTF8String>, IComparable<UTF8String> {
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
		/// Checks whether <paramref name="utf8"/> is null or if its data is null or the
		/// data is zero length.
		/// </summary>
		/// <param name="utf8">The instance to check</param>
		/// <returns><c>true</c> if null or empty, <c>false</c> otherwise</returns>
		public static bool IsNullOrEmpty(UTF8String utf8) {
			return (object)utf8 == null || utf8.data == null || utf8.data.Length == 0;
		}

		/// <inheritdoc/>
		public int CompareTo(UTF8String other) {
			return CompareTo(this, other);
		}

		static int CompareTo(UTF8String a, UTF8String b) {
			if ((object)a == null)
				return -1;
			if ((object)b == null)
				return 1;
			return CompareBytes(a.data, b.data);
		}

		static int CompareBytes(byte[] a, byte[] b) {
			if (a == b)
				return 0;
			if (a == null)
				return -1;
			if (b == null)
				return 1;
			if (a.Length != b.Length)
				return a.Length.CompareTo(b.Length);
			for (int i = 0; i < a.Length; i++) {
				var ai = a[i];
				var bi = b[i];
				if (ai < bi)
					return -1;
				if (ai > bi)
					return 1;
			}
			return 0;
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
			foreach (var b in data) {
				if (b == 0)
					throw new ArgumentException("No nul bytes are allowed");
			}
#endif
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="s">The string</param>
		public UTF8String(string s)
			: this(Encoding.UTF8.GetBytes(s)) {
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
