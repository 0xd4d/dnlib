/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// Compares <see cref="UTF8String"/>s
	/// </summary>
	public sealed class UTF8StringEqualityComparer : IEqualityComparer<UTF8String> {
		/// <summary>
		/// The default instance
		/// </summary>
		public static readonly UTF8StringEqualityComparer Instance = new UTF8StringEqualityComparer();

		/// <inheritdoc/>
		public bool Equals(UTF8String x, UTF8String y) {
			return UTF8String.Equals(x, y);
		}

		/// <inheritdoc/>
		public int GetHashCode(UTF8String obj) {
			return UTF8String.GetHashCode(obj);
		}
	}

	/// <summary>
	/// A UTF-8 encoded string where the original data is kept in memory to avoid conversions
	/// when the data is not really valid UTF-8 encoded data
	/// </summary>
	/// <remarks>When comparing strings, a byte compare is performed. The reason is that this
	/// is what the CLR does when comparing strings in the #Strings stream.</remarks>
	[DebuggerDisplay("{String}")]
	public sealed class UTF8String : IEquatable<UTF8String>, IComparable<UTF8String> {
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

		/// <summary>Implicit conversion from <see cref="UTF8String"/> to <see cref="string"/></summary>
		public static implicit operator string(UTF8String s) {
			return UTF8String.ToSystemString(s);
		}

		/// <summary>Implicit conversion from <see cref="string"/> to <see cref="UTF8String"/></summary>
		public static implicit operator UTF8String(string s) {
			return s == null ? null : new UTF8String(s);
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
			return sa.ToUpperInvariant().CompareTo(sb.ToUpperInvariant());
		}

		/// <summary>
		/// Compares two <see cref="UTF8String"/> instances (case insensitive)
		/// </summary>
		/// <param name="a">Instance #1 or <c>null</c></param>
		/// <param name="b">Instance #2 or <c>null</c></param>
		/// <returns><c>true</c> if equals, <c>false</c> otherwise</returns>
		public static bool CaseInsensitiveEquals(UTF8String a, UTF8String b) {
			return CaseInsensitiveCompareTo(a, b) == 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(UTF8String left, UTF8String right) {
			return CompareTo(left, right) == 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(UTF8String left, string right) {
			return ToSystemString(left) == right;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(string left, UTF8String right) {
			return left == ToSystemString(right);
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(UTF8String left, UTF8String right) {
			return CompareTo(left, right) != 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(UTF8String left, string right) {
			return ToSystemString(left) != right;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(string left, UTF8String right) {
			return left != ToSystemString(right);
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

		/// <summary>
		/// Compares two instances
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns><c>true</c> if equals, <c>false</c> otherwise</returns>
		public static bool Equals(UTF8String a, UTF8String b) {
			return CompareTo(a, b) == 0;
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
			return UTF8String.GetHashCode(this);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return String;
		}
	}
}
