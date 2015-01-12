// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;

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

		readonly byte[] data;
		string asString;

		/// <summary>
		/// Gets the value as a UTF8 decoded string. Only use it for display purposes,
		/// not for serialization.
		/// </summary>
		public string String {
			get {
				if (asString == null)
					Interlocked.CompareExchange(ref asString, ConvertFromUTF8(data), null);
				return asString;
			}
		}

		/// <summary>
		/// Gets the original encoded data. Don't modify this data.
		/// </summary>
		public byte[] Data {
			get { return data; }
		}

		/// <summary>
		/// Gets the length of the this as a <see cref="string"/>. I.e., it's the same as
		/// <c>String.Length</c>.
		/// </summary>
		/// <seealso cref="DataLength"/>
		public int Length {
			get { return String.Length; }
		}

		/// <summary>
		/// Gets the length of the raw data. It's the same as <c>Data.Length</c>
		/// </summary>
		/// <seealso cref="Length"/>
		public int DataLength {
			get { return data == null ? 0 : data.Length; }
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

		static string ConvertFromUTF8(byte[] data) {
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

		/// <summary>
		/// Checks whether <paramref name="value"/> exists in this string
		/// </summary>
		/// <param name="value">Value to find</param>
		/// <returns><c>true</c> if <paramref name="value"/> exists in string or is the
		/// empty string, else <c>false</c></returns>
		public bool Contains(string value) {
			return String.Contains(value);
		}

		/// <summary>
		/// Checks whether <paramref name="value"/> matches the end of this string
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public bool EndsWith(string value) {
			return String.EndsWith(value);
		}

		/// <summary>
		/// Checks whether <paramref name="value"/> matches the end of this string
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="ignoreCase"><c>true</c> to ignore case</param>
		/// <param name="culture">Culture info</param>
		/// <returns></returns>
		public bool EndsWith(string value, bool ignoreCase, CultureInfo culture) {
			return String.EndsWith(value, ignoreCase, culture);
		}

		/// <summary>
		/// Checks whether <paramref name="value"/> matches the end of this string
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="comparisonType">Comparison type</param>
		/// <returns></returns>
		public bool EndsWith(string value, StringComparison comparisonType) {
			return String.EndsWith(value, comparisonType);
		}

		/// <summary>
		/// Checks whether <paramref name="value"/> matches the beginning of this string
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public bool StartsWith(string value) {
			return String.StartsWith(value);
		}

		/// <summary>
		/// Checks whether <paramref name="value"/> matches the beginning of this string
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="ignoreCase"><c>true</c> to ignore case</param>
		/// <param name="culture">Culture info</param>
		/// <returns></returns>
		public bool StartsWith(string value, bool ignoreCase, CultureInfo culture) {
			return String.StartsWith(value, ignoreCase, culture);
		}

		/// <summary>
		/// Checks whether <paramref name="value"/> matches the beginning of this string
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="comparisonType">Comparison type</param>
		/// <returns></returns>
		public bool StartsWith(string value, StringComparison comparisonType) {
			return String.StartsWith(value, comparisonType);
		}

		/// <summary>
		/// Compares this instance with <paramref name="strB"/>
		/// </summary>
		/// <param name="strB">Other string</param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		public int CompareTo(string strB) {
			return String.CompareTo(strB);
		}

		/// <summary>
		/// Returns the index of the first character <paramref name="value"/> in this string
		/// </summary>
		/// <param name="value">Character</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int IndexOf(char value) {
			return String.IndexOf(value);
		}

		/// <summary>
		/// Returns the index of the first character <paramref name="value"/> in this string
		/// starting from index <paramref name="startIndex"/>
		/// </summary>
		/// <param name="value">Character</param>
		/// <param name="startIndex">Start index</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int IndexOf(char value, int startIndex) {
			return String.IndexOf(value, startIndex);
		}

		/// <summary>
		/// Returns the index of the first character <paramref name="value"/> in this string
		/// starting from index <paramref name="startIndex"/> for max <paramref name="count"/>
		/// characters.
		/// </summary>
		/// <param name="value">Character</param>
		/// <param name="startIndex">Start index</param>
		/// <param name="count">Max number of chars to scan</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int IndexOf(char value, int startIndex, int count) {
			return String.IndexOf(value, startIndex, count);
		}

		/// <summary>
		/// Returns the index of the first sub string <paramref name="value"/> in this string
		/// </summary>
		/// <param name="value">String</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int IndexOf(string value) {
			return String.IndexOf(value);
		}

		/// <summary>
		/// Returns the index of the first sub string <paramref name="value"/> in this string
		/// starting from index <paramref name="startIndex"/>
		/// </summary>
		/// <param name="value">String</param>
		/// <param name="startIndex">Start index</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int IndexOf(string value, int startIndex) {
			return String.IndexOf(value, startIndex);
		}

		/// <summary>
		/// Returns the index of the first sub string <paramref name="value"/> in this string
		/// starting from index <paramref name="startIndex"/> for max <paramref name="count"/>
		/// characters.
		/// </summary>
		/// <param name="value">String</param>
		/// <param name="startIndex">Start index</param>
		/// <param name="count">Max number of chars to scan</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int IndexOf(string value, int startIndex, int count) {
			return String.IndexOf(value, startIndex, count);
		}

		/// <summary>
		/// Returns the index of the first sub string <paramref name="value"/> in this string
		/// starting from index <paramref name="startIndex"/> for max <paramref name="count"/>
		/// characters.
		/// </summary>
		/// <param name="value">String</param>
		/// <param name="startIndex">Start index</param>
		/// <param name="count">Max number of chars to scan</param>
		/// <param name="comparisonType">Comparison type</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int IndexOf(string value, int startIndex, int count, StringComparison comparisonType) {
			return String.IndexOf(value, startIndex, count, comparisonType);
		}

		/// <summary>
		/// Returns the index of the first sub string <paramref name="value"/> in this string
		/// starting from index <paramref name="startIndex"/>
		/// </summary>
		/// <param name="value">String</param>
		/// <param name="startIndex">Start index</param>
		/// <param name="comparisonType">Comparison type</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int IndexOf(string value, int startIndex, StringComparison comparisonType) {
			return String.IndexOf(value, startIndex, comparisonType);
		}

		/// <summary>
		/// Returns the index of the first sub string <paramref name="value"/> in this string
		/// </summary>
		/// <param name="value">String</param>
		/// <param name="comparisonType">Comparison type</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int IndexOf(string value, StringComparison comparisonType) {
			return String.IndexOf(value, comparisonType);
		}

		/// <summary>
		/// Returns the index of the last character <paramref name="value"/> in this string
		/// </summary>
		/// <param name="value">Character</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int LastIndexOf(char value) {
			return String.LastIndexOf(value);
		}

		/// <summary>
		/// Returns the index of the last character <paramref name="value"/> in this string
		/// starting from index <paramref name="startIndex"/>
		/// </summary>
		/// <param name="value">Character</param>
		/// <param name="startIndex">Start index</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int LastIndexOf(char value, int startIndex) {
			return String.LastIndexOf(value, startIndex);
		}

		/// <summary>
		/// Returns the index of the last character <paramref name="value"/> in this string
		/// starting from index <paramref name="startIndex"/> for max <paramref name="count"/>
		/// characters.
		/// </summary>
		/// <param name="value">Character</param>
		/// <param name="startIndex">Start index</param>
		/// <param name="count">Max number of chars to scan</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int LastIndexOf(char value, int startIndex, int count) {
			return String.LastIndexOf(value, startIndex, count);
		}

		/// <summary>
		/// Returns the index of the last sub string <paramref name="value"/> in this string
		/// </summary>
		/// <param name="value">String</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int LastIndexOf(string value) {
			return String.LastIndexOf(value);
		}

		/// <summary>
		/// Returns the index of the last sub string <paramref name="value"/> in this string
		/// starting from index <paramref name="startIndex"/>
		/// </summary>
		/// <param name="value">String</param>
		/// <param name="startIndex">Start index</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int LastIndexOf(string value, int startIndex) {
			return String.LastIndexOf(value, startIndex);
		}

		/// <summary>
		/// Returns the index of the last sub string <paramref name="value"/> in this string
		/// starting from index <paramref name="startIndex"/> for max <paramref name="count"/>
		/// characters.
		/// </summary>
		/// <param name="value">String</param>
		/// <param name="startIndex">Start index</param>
		/// <param name="count">Max number of chars to scan</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int LastIndexOf(string value, int startIndex, int count) {
			return String.LastIndexOf(value, startIndex, count);
		}

		/// <summary>
		/// Returns the index of the last sub string <paramref name="value"/> in this string
		/// starting from index <paramref name="startIndex"/> for max <paramref name="count"/>
		/// characters.
		/// </summary>
		/// <param name="value">String</param>
		/// <param name="startIndex">Start index</param>
		/// <param name="count">Max number of chars to scan</param>
		/// <param name="comparisonType">Comparison type</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int LastIndexOf(string value, int startIndex, int count, StringComparison comparisonType) {
			return String.LastIndexOf(value, startIndex, count, comparisonType);
		}

		/// <summary>
		/// Returns the index of the last sub string <paramref name="value"/> in this string
		/// starting from index <paramref name="startIndex"/>
		/// </summary>
		/// <param name="value">String</param>
		/// <param name="startIndex">Start index</param>
		/// <param name="comparisonType">Comparison type</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int LastIndexOf(string value, int startIndex, StringComparison comparisonType) {
			return String.LastIndexOf(value, startIndex, comparisonType);
		}

		/// <summary>
		/// Returns the index of the last sub string <paramref name="value"/> in this string
		/// </summary>
		/// <param name="value">String</param>
		/// <param name="comparisonType">Comparison type</param>
		/// <returns>The index of <paramref name="value"/> or <c>-1</c> if not found</returns>
		public int LastIndexOf(string value, StringComparison comparisonType) {
			return String.LastIndexOf(value, comparisonType);
		}

		/// <summary>
		/// Inserts string <paramref name="value"/> at a index <paramref name="startIndex"/>
		/// </summary>
		/// <param name="startIndex">Start index</param>
		/// <param name="value">Value to insert</param>
		/// <returns>A new instance with the <paramref name="value"/> inserted at position
		/// <paramref name="startIndex"/></returns>
		public UTF8String Insert(int startIndex, string value) {
			return new UTF8String(String.Insert(startIndex, value));
		}

		/// <summary>
		/// Removes all characters starting from position <paramref name="startIndex"/>
		/// </summary>
		/// <param name="startIndex">Start index</param>
		/// <returns>A new instance</returns>
		public UTF8String Remove(int startIndex) {
			return new UTF8String(String.Remove(startIndex));
		}

		/// <summary>
		/// Removes <paramref name="count"/> characters starting from position
		/// <paramref name="startIndex"/>
		/// </summary>
		/// <param name="startIndex">Start index</param>
		/// <param name="count">Number of characters to remove</param>
		/// <returns>A new instance</returns>
		public UTF8String Remove(int startIndex, int count) {
			return new UTF8String(String.Remove(startIndex, count));
		}

		/// <summary>
		/// Replaces all characters <paramref name="oldChar"/> with <paramref name="newChar"/>
		/// </summary>
		/// <param name="oldChar">Character to find</param>
		/// <param name="newChar">Character to replace all <paramref name="oldChar"/></param>
		/// <returns>A new instance</returns>
		public UTF8String Replace(char oldChar, char newChar) {
			return new UTF8String(String.Replace(oldChar, newChar));
		}

		/// <summary>
		/// Replaces all sub strings <paramref name="oldValue"/> with <paramref name="newValue"/>
		/// </summary>
		/// <param name="oldValue">Sub string to find</param>
		/// <param name="newValue">Sub string to replace all <paramref name="oldValue"/></param>
		/// <returns>A new instance</returns>
		public UTF8String Replace(string oldValue, string newValue) {
			return new UTF8String(String.Replace(oldValue, newValue));
		}

		/// <summary>
		/// Returns a sub string of this string starting at offset <paramref name="startIndex"/>
		/// </summary>
		/// <param name="startIndex">Start index</param>
		/// <returns>A new instance</returns>
		public UTF8String Substring(int startIndex) {
			return new UTF8String(String.Substring(startIndex));
		}

		/// <summary>
		/// Returns a sub string of this string starting at offset <paramref name="startIndex"/>.
		/// Length of sub string is <paramref name="length"/>.
		/// </summary>
		/// <param name="startIndex">Start index</param>
		/// <param name="length">Length of sub string</param>
		/// <returns>A new instance</returns>
		public UTF8String Substring(int startIndex, int length) {
			return new UTF8String(String.Substring(startIndex, length));
		}

		/// <summary>
		/// Returns the lower case version of this string
		/// </summary>
		/// <returns>A new instance</returns>
		public UTF8String ToLower() {
			return new UTF8String(String.ToLower());
		}

		/// <summary>
		/// Returns the lower case version of this string
		/// </summary>
		/// <param name="culture">Culture info</param>
		/// <returns>A new instance</returns>
		public UTF8String ToLower(CultureInfo culture) {
			return new UTF8String(String.ToLower(culture));
		}

		/// <summary>
		/// Returns the lower case version of this string using the invariant culture
		/// </summary>
		/// <returns>A new instance</returns>
		public UTF8String ToLowerInvariant() {
			return new UTF8String(String.ToLowerInvariant());
		}

		/// <summary>
		/// Returns the upper case version of this string
		/// </summary>
		/// <returns>A new instance</returns>
		public UTF8String ToUpper() {
			return new UTF8String(String.ToUpper());
		}

		/// <summary>
		/// Returns the upper case version of this string
		/// </summary>
		/// <param name="culture">Culture info</param>
		/// <returns>A new instance</returns>
		public UTF8String ToUpper(CultureInfo culture) {
			return new UTF8String(String.ToUpper(culture));
		}

		/// <summary>
		/// Returns the upper case version of this string using the invariant culture
		/// </summary>
		/// <returns>A new instance</returns>
		public UTF8String ToUpperInvariant() {
			return new UTF8String(String.ToUpperInvariant());
		}

		/// <summary>
		/// Removes all leading and trailing whitespace characters
		/// </summary>
		/// <returns>A new instance</returns>
		public UTF8String Trim() {
			return new UTF8String(String.Trim());
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
