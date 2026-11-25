// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Names {
	/// <summary>
	/// The names in the metadata are UTF-8 bytes, and not always valid UTF-8 (obfuscators). <see cref="UTF8String"/>
	/// keeps the bytes of a name, so a name that can't be shown as a string is still written back as it was, and
	/// two names are equal if their bytes are equal. It also has the methods of <see cref="string"/> that are
	/// used most.
	/// </summary>
	public class UTF8StringTests {
		[Fact]
		public void StringAndBytesAreTheSameText() {
			var fromString = new UTF8String("Größe");
			var fromBytes = new UTF8String(Encoding.UTF8.GetBytes("Größe"));

			Assert.Equal("Größe", fromString.String);
			Assert.Equal("Größe", fromBytes.String);
			Assert.Equal("Größe", fromBytes.ToString());
			Assert.Equal(Encoding.UTF8.GetBytes("Größe"), fromString.Data);
			// The number of chars, and the number of bytes
			Assert.Equal(5, fromString.Length);
			Assert.Equal(7, fromString.DataLength);
			Assert.True(fromString == fromBytes);
			Assert.Equal(fromString.GetHashCode(), fromBytes.GetHashCode());
		}

		// 0xFF isn't valid UTF-8. The string has a replacement char, the bytes are kept.
		[Fact]
		public void InvalidUtf8KeepsItsBytes() {
			var first = new UTF8String(new byte[] { (byte)'A', 0xFF });
			var second = new UTF8String(new byte[] { (byte)'A', 0xFE });

			Assert.Equal("A�", first.String);
			Assert.Equal("A�", second.String);
			Assert.Equal(new byte[] { (byte)'A', 0xFF }, first.Data);
			// They look the same as strings, but they're two names
			Assert.False(first == second);
			Assert.False(first.Equals(second));
			Assert.NotEqual(0, first.CompareTo(second));
		}

		[Fact]
		public void NullAndEmpty() {
			UTF8String? none = null;
			var noData = new UTF8String((byte[])null!);

			Assert.True(UTF8String.IsNull(none));
			Assert.True(UTF8String.IsNull(noData));
			Assert.False(UTF8String.IsNull(UTF8String.Empty));
			Assert.True(UTF8String.IsNullOrEmpty(none));
			Assert.True(UTF8String.IsNullOrEmpty(noData));
			Assert.True(UTF8String.IsNullOrEmpty(UTF8String.Empty));
			Assert.False(UTF8String.IsNullOrEmpty(new UTF8String("a")));
			Assert.Equal(0, noData.DataLength);
			Assert.Equal(0, UTF8String.Empty.Length);

			Assert.Null(UTF8String.ToSystemString(none));
			Assert.Null(UTF8String.ToSystemString(noData));
			Assert.Equal("", UTF8String.ToSystemString(UTF8String.Empty));
			Assert.Equal("", UTF8String.ToSystemStringOrEmpty(none));
			Assert.Equal("a", UTF8String.ToSystemStringOrEmpty(new UTF8String("a")));
			Assert.Equal(0, UTF8String.GetHashCode(none));
			// No name and an empty name have the same hash code, but they aren't equal
			Assert.Equal(UTF8String.GetHashCode(none), UTF8String.GetHashCode(UTF8String.Empty));
			Assert.False(UTF8String.Equals(none, UTF8String.Empty));
			Assert.True(UTF8String.Equals(none, null));
		}

		[Fact]
		public void ConversionsFromAndToString() {
			UTF8String? fromString = "text";
			string? toString = new UTF8String("text");
			UTF8String? fromNull = (string?)null;
			string? toNull = (UTF8String?)null;

			Assert.Equal("text", fromString.String);
			Assert.Equal("text", toString);
			Assert.Null(fromNull);
			Assert.Null(toNull);
		}

		[Fact]
		public void ComparisonOperators() {
			var a = new UTF8String("a");
			var sameA = new UTF8String("a");
			var b = new UTF8String("b");
			UTF8String? none = null;

			Assert.True(a == sameA && !(a != sameA));
			Assert.True(a != b && !(a == b));
			Assert.True(a < b && a <= b && b > a && b >= a && a <= sameA && a >= sameA);
			Assert.False(a > b || b < a);
			// With a string on either side
			Assert.True(a == "a" && "a" == a && a != "b" && "b" != a);
			Assert.False(a == "b" || "b" == a || a != "a" || "a" != a);
			// null is less than every string, and equal to null
			Assert.True(none == (UTF8String?)null && none < a && a > none && none != a);
			Assert.True(none == (string?)null && (string?)null == none);

			Assert.True(a.Equals((object)sameA));
			Assert.False(a.Equals((object)b));
			// A string is converted to a UTF8String, but a string in an object variable is another kind of object
			Assert.True(a.Equals("a"));
			Assert.False(a.Equals((object)"a"));
			Assert.False(a.Equals((object?)null));
			Assert.True(a.CompareTo(b) < 0 && b.CompareTo(a) > 0 && a.CompareTo(sameA) == 0);
			Assert.True(a.CompareTo("b") < 0);
		}

		// The bytes are compared, not the chars: the order is the order of the Unicode code points
		[Fact]
		public void StringsAreOrderedByTheirBytes() {
			var names = new[] { "b", "B", "a", "ä", "", "ab" }.Select(a => new UTF8String(a)).ToList();

			names.Sort();

			Assert.Equal(new[] { "", "B", "a", "ab", "b", "ä" }, names.Select(a => a.String));
		}

		[Fact]
		public void CaseInsensitiveComparison() {
			Assert.True(UTF8String.CaseInsensitiveEquals(new UTF8String("Name"), new UTF8String("NAME")));
			Assert.False(UTF8String.CaseInsensitiveEquals(new UTF8String("Name"), new UTF8String("Names")));
			Assert.True(UTF8String.CaseInsensitiveEquals(null, null));
			Assert.False(UTF8String.CaseInsensitiveEquals(null, UTF8String.Empty));
			Assert.True(UTF8String.CaseInsensitiveCompareTo(null, new UTF8String("a")) < 0);
			Assert.True(UTF8String.CaseInsensitiveCompareTo(new UTF8String("a"), null) > 0);
			Assert.True(UTF8String.CaseInsensitiveCompareTo(new UTF8String("a"), new UTF8String("B")) < 0);
		}

		[Fact]
		public void EqualityComparerIsForDictionaries() {
			var dictionary = new Dictionary<UTF8String, int>(UTF8StringEqualityComparer.Instance) { { new UTF8String("key"), 1 } };

			Assert.Equal(1, dictionary[new UTF8String(Encoding.UTF8.GetBytes("key"))]);
			Assert.False(dictionary.ContainsKey(new UTF8String("Key")));
			Assert.True(UTF8StringEqualityComparer.Instance.Equals(null, null));
			Assert.Equal(0, UTF8StringEqualityComparer.Instance.GetHashCode(null));
		}

		// The methods with the names of string's methods do what string's methods do
		[Fact]
		public void StringMethods() {
			const string text = "Hello, hello";
			var name = new UTF8String(text);
			var invariant = CultureInfo.InvariantCulture;

			Assert.True(name.Contains("lo, h"));
			Assert.False(name.Contains("HELLO"));
			Assert.True(name.StartsWith("Hello") && !name.StartsWith("hello"));
			Assert.True(name.StartsWith("hello", ignoreCase: true, invariant));
			Assert.True(name.StartsWith("HELLO", StringComparison.OrdinalIgnoreCase));
			Assert.True(name.EndsWith("hello") && !name.EndsWith("Hello"));
			Assert.True(name.EndsWith("HELLO", ignoreCase: true, invariant));
			Assert.True(name.EndsWith("HELLO", StringComparison.OrdinalIgnoreCase));

			Assert.Equal(text.IndexOf('l'), name.IndexOf('l'));
			Assert.Equal(text.IndexOf('l', 4), name.IndexOf('l', 4));
			Assert.Equal(text.IndexOf('l', 4, 3), name.IndexOf('l', 4, 3));
			Assert.Equal(text.IndexOf("hello", StringComparison.Ordinal), name.IndexOf("hello"));
			Assert.Equal(text.IndexOf("ello", 2, StringComparison.Ordinal), name.IndexOf("ello", 2));
			Assert.Equal(text.IndexOf("ello", 2, 4, StringComparison.Ordinal), name.IndexOf("ello", 2, 4));
			Assert.Equal(0, name.IndexOf("HELLO", StringComparison.OrdinalIgnoreCase));
			Assert.Equal(7, name.IndexOf("HELLO", 1, StringComparison.OrdinalIgnoreCase));
			Assert.Equal(-1, name.IndexOf("HELLO", 1, 5, StringComparison.OrdinalIgnoreCase));

			Assert.Equal(text.LastIndexOf('l'), name.LastIndexOf('l'));
			Assert.Equal(text.LastIndexOf('l', 5), name.LastIndexOf('l', 5));
			Assert.Equal(text.LastIndexOf('l', 5, 2), name.LastIndexOf('l', 5, 2));
			Assert.Equal(text.LastIndexOf("ello", StringComparison.Ordinal), name.LastIndexOf("ello"));
			Assert.Equal(text.LastIndexOf("ello", 6, StringComparison.Ordinal), name.LastIndexOf("ello", 6));
			Assert.Equal(text.LastIndexOf("ello", 6, 6, StringComparison.Ordinal), name.LastIndexOf("ello", 6, 6));
			Assert.Equal(7, name.LastIndexOf("HELLO", StringComparison.OrdinalIgnoreCase));
			Assert.Equal(0, name.LastIndexOf("HELLO", 6, StringComparison.OrdinalIgnoreCase));
			Assert.Equal(-1, name.LastIndexOf("HELLO", 6, 3, StringComparison.OrdinalIgnoreCase));
		}

		// The methods that return a changed string return a new UTF8String
		[Fact]
		public void MethodsThatReturnAnotherString() {
			var name = new UTF8String(" Hello ");

			Assert.Equal("Hello", name.Trim().String);
			Assert.Equal(" He--llo ", name.Insert(3, "--").String);
			Assert.Equal(" He", name.Remove(3).String);
			Assert.Equal(" Heo ", name.Remove(3, 2).String);
			Assert.Equal(" HeLLo ", name.Replace('l', 'L').String);
			Assert.Equal(" Help ", name.Replace("lo", "p").String);
			Assert.Equal("llo ", name.Substring(3).String);
			Assert.Equal("ll", name.Substring(3, 2).String);
			Assert.Equal(" hello ", name.ToLower().String);
			Assert.Equal(" hello ", name.ToLower(CultureInfo.InvariantCulture).String);
			Assert.Equal(" hello ", name.ToLowerInvariant().String);
			Assert.Equal(" HELLO ", name.ToUpper().String);
			Assert.Equal(" HELLO ", name.ToUpper(CultureInfo.InvariantCulture).String);
			Assert.Equal(" HELLO ", name.ToUpperInvariant().String);
			// The original isn't changed
			Assert.Equal(" Hello ", name.String);
		}
	}
}
