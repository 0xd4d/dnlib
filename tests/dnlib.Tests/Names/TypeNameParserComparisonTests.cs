// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Names {
	/// <summary>
	/// dnlib's parser of reflection type names ("System.Collections.Generic.List`1[[System.Int32]][]") and
	/// System.Type.GetType() read the same text. The tests have what both read. All types are in the core
	/// library, so the runtime finds them; dnlib doesn't look for a type, it only parses the text.
	/// <list type="bullet">
	/// <item>Where the runtime reads a type, dnlib reads the same type. (The runtime writes a generic type
	/// without arguments with the names of its parameters, "List`1[T]".)</item>
	/// <item>dnlib reads texts that the runtime rejects ("invalid"): a space or an escaped character that
	/// isn't part of the real name, a generic type with the wrong number of arguments or with a pointer
	/// as an argument, a reference to a reference, array bounds.</item>
	/// </list>
	/// </summary>
	public class TypeNameParserComparisonTests {
		// The name like System.Type.ToString() writes it: dnlib writes <> around generic arguments and
		// '/' before a nested type
		static string ParseWithDnlib(string text) {
			var module = new ModuleDefUser("Test.dll");
			var type = TypeNameParser.ParseAsTypeSigReflection(module, text, null);
			if (type is null)
				return "invalid";
			return type.FullName.Replace('<', '[').Replace('>', ']').Replace('/', '+');
		}

		static string ParseWithSystemReflection(string text) {
			try {
				return Type.GetType(text, throwOnError: false)?.ToString() ?? "invalid";
			}
			catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is System.IO.IOException || ex is TypeLoadException) {
				return "invalid";
			}
		}

		[Theory]
		[InlineData("System.Int32", "System.Int32", "System.Int32")]
		[InlineData("System.Int32, mscorlib", "System.Int32", "System.Int32")]
		[InlineData("  System.Int32  ", "System.Int32  ", "invalid")]
		[InlineData("System.Int32 , mscorlib", "System.Int32 ", "invalid")]
		[InlineData("", "invalid", "invalid")]
		[InlineData(" ", "invalid", "invalid")]
		[InlineData(",", "invalid", "invalid")]
		[InlineData("System.Int32,", "invalid", "invalid")]
		[InlineData("System.Int32, ", "invalid", "invalid")]
		// Arrays, pointers and references
		[InlineData("System.Int32[]", "System.Int32[]", "System.Int32[]")]
		[InlineData("System.Int32[,]", "System.Int32[,]", "System.Int32[,]")]
		[InlineData("System.Int32[,,]", "System.Int32[,,]", "System.Int32[,,]")]
		[InlineData("System.Int32[*]", "System.Int32[*]", "System.Int32[*]")]
		[InlineData("System.Int32[][]", "System.Int32[][]", "System.Int32[][]")]
		[InlineData("System.Int32[,][]", "System.Int32[,][]", "System.Int32[,][]")]
		[InlineData("System.Int32[ ]", "System.Int32[]", "System.Int32[]")]
		[InlineData("System.Int32[ , ]", "System.Int32[,]", "System.Int32[,]")]
		[InlineData("System.Int32 []", "System.Int32 []", "invalid")]
		[InlineData("System.Int32[*,*]", "System.Int32[,]", "invalid")]
		[InlineData("System.Int32[0...]", "System.Int32[*]", "invalid")]
		[InlineData("System.Int32[1..5]", "System.Int32[*]", "invalid")]
		[InlineData("System.Int32[...]", "System.Int32[...]", "invalid")]
		[InlineData("System.Int32[", "invalid", "invalid")]
		[InlineData("System.Int32]", "invalid", "invalid")]
		[InlineData("System.Int32[]]", "invalid", "invalid")]
		[InlineData("System.Int32*", "System.Int32*", "System.Int32*")]
		[InlineData("System.Int32**", "System.Int32**", "System.Int32**")]
		[InlineData("System.Int32&", "System.Int32&", "System.Int32&")]
		[InlineData("System.Int32*&", "System.Int32*&", "System.Int32*&")]
		[InlineData("System.Int32[]&", "System.Int32[]&", "System.Int32[]&")]
		[InlineData("System.Int32*[]", "System.Int32*[]", "System.Int32*[]")]
		[InlineData("System.Int32[]*", "System.Int32[]*", "System.Int32[]*")]
		[InlineData("System.Int32&&", "System.Int32&&", "invalid")]
		[InlineData("System.Int32&*", "System.Int32&*", "invalid")]
		[InlineData("System.Int32&[]", "System.Int32&[]", "invalid")]
		[InlineData("System.Int32 *", "System.Int32 *", "invalid")]
		[InlineData("System.Int32* &", "System.Int32*&", "System.Int32*&")]
		// Nested types
		[InlineData("System.Environment+SpecialFolder", "System.Environment+SpecialFolder", "System.Environment+SpecialFolder")]
		[InlineData("System.Environment+SpecialFolder[]", "System.Environment+SpecialFolder[]", "System.Environment+SpecialFolder[]")]
		[InlineData("System.Environment + SpecialFolder", "System.Environment +SpecialFolder", "invalid")]
		[InlineData("System.Environment+", "invalid", "invalid")]
		[InlineData("+SpecialFolder", "invalid", "invalid")]
		[InlineData("System.Environment++SpecialFolder", "invalid", "invalid")]
		[InlineData("System.Environment/SpecialFolder", "System.Environment+SpecialFolder", "invalid")]
		// Generic types
		[InlineData("System.Collections.Generic.List`1", "System.Collections.Generic.List`1", "System.Collections.Generic.List`1[T]")]
		[InlineData("System.Collections.Generic.List`1[System.Int32]", "System.Collections.Generic.List`1[System.Int32]", "System.Collections.Generic.List`1[System.Int32]")]
		[InlineData("System.Collections.Generic.List`1[[System.Int32]]", "System.Collections.Generic.List`1[System.Int32]", "System.Collections.Generic.List`1[System.Int32]")]
		[InlineData("System.Collections.Generic.List`1[[System.Int32, mscorlib]]", "System.Collections.Generic.List`1[System.Int32]", "System.Collections.Generic.List`1[System.Int32]")]
		[InlineData("System.Collections.Generic.List`1[System.Int32, mscorlib]", "System.Collections.Generic.List`1[System.Int32,mscorlib]", "invalid")]
		[InlineData("System.Collections.Generic.List`1[[System.Int32]], mscorlib", "System.Collections.Generic.List`1[System.Int32]", "System.Collections.Generic.List`1[System.Int32]")]
		[InlineData("System.Collections.Generic.List`1[ System.Int32 ]", "System.Collections.Generic.List`1[System.Int32 ]", "invalid")]
		[InlineData("System.Collections.Generic.List`1[ [System.Int32] ]", "System.Collections.Generic.List`1[System.Int32]", "System.Collections.Generic.List`1[System.Int32]")]
		[InlineData("System.Collections.Generic.List`1 [System.Int32]", "System.Collections.Generic.List`1 [System.Int32]", "invalid")]
		[InlineData("System.Collections.Generic.List`1[System.Int32][]", "System.Collections.Generic.List`1[System.Int32][]", "System.Collections.Generic.List`1[System.Int32][]")]
		[InlineData("System.Collections.Generic.List`1[System.Int32[]]", "System.Collections.Generic.List`1[System.Int32[]]", "System.Collections.Generic.List`1[System.Int32[]]")]
		[InlineData("System.Collections.Generic.List`1[System.Int32*]", "System.Collections.Generic.List`1[System.Int32*]", "invalid")]
		[InlineData("System.Collections.Generic.List`1[System.Int32]&", "System.Collections.Generic.List`1[System.Int32]&", "System.Collections.Generic.List`1[System.Int32]&")]
		[InlineData("System.Collections.Generic.List`1[]", "System.Collections.Generic.List`1[]", "System.Collections.Generic.List`1[T][]")]
		[InlineData("System.Collections.Generic.List`1[[]]", "invalid", "invalid")]
		[InlineData("System.Collections.Generic.List`1[System.Int32", "invalid", "invalid")]
		[InlineData("System.Collections.Generic.List`1[[System.Int32]", "invalid", "invalid")]
		[InlineData("System.Collections.Generic.List`1[System.Int32,]", "System.Collections.Generic.List`1[System.Int32]", "invalid")]
		[InlineData("System.Collections.Generic.List`1[System.Collections.Generic.List`1[System.Int32]]", "System.Collections.Generic.List`1[System.Collections.Generic.List`1[System.Int32]]", "System.Collections.Generic.List`1[System.Collections.Generic.List`1[System.Int32]]")]
		[InlineData("System.Collections.Generic.Dictionary`2[System.Int32,System.String]", "System.Collections.Generic.Dictionary`2[System.Int32,System.String]", "System.Collections.Generic.Dictionary`2[System.Int32,System.String]")]
		[InlineData("System.Collections.Generic.Dictionary`2[[System.Int32],[System.String]]", "System.Collections.Generic.Dictionary`2[System.Int32,System.String]", "System.Collections.Generic.Dictionary`2[System.Int32,System.String]")]
		[InlineData("System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib],System.String]", "System.Collections.Generic.Dictionary`2[System.Int32,System.String]", "System.Collections.Generic.Dictionary`2[System.Int32,System.String]")]
		[InlineData("System.Collections.Generic.Dictionary`2[System.Int32, System.String]", "System.Collections.Generic.Dictionary`2[System.Int32,System.String]", "System.Collections.Generic.Dictionary`2[System.Int32,System.String]")]
		[InlineData("System.Collections.Generic.Dictionary`2[System.Int32]", "System.Collections.Generic.Dictionary`2[System.Int32]", "invalid")]
		[InlineData("System.Collections.Generic.Dictionary`2+KeyCollection[System.Int32,System.String]", "System.Collections.Generic.Dictionary`2+KeyCollection[System.Int32,System.String]", "System.Collections.Generic.Dictionary`2+KeyCollection[System.Int32,System.String]")]
		[InlineData("System.Collections.Generic.Dictionary`2[System.Int32,System.String]+KeyCollection", "invalid", "invalid")]
		[InlineData("System.Int32[System.Int32]", "System.Int32[System.Int32]", "invalid")]
		// Escaped characters
		[InlineData("System.Int\\32", "System.Int32", "invalid")]
		[InlineData("System\\.Int32", "System.Int32", "invalid")]
		[InlineData("System.Environment\\+SpecialFolder", "System.Environment+SpecialFolder", "invalid")]
		[InlineData("System.Int32\\", "System.Int32", "invalid")]
		[InlineData("System.Int32\\[\\]", "System.Int32[]", "invalid")]
		public void TypeNameIsParsed(string text, string expectedDnlib, string expectedSystemReflection) {
			Assert.Equal(expectedDnlib, ParseWithDnlib(text));
			// .NET Framework and Mono have other parsers
			if (typeof(object).Assembly.GetName().Name == "System.Private.CoreLib")
				Assert.Equal(expectedSystemReflection, ParseWithSystemReflection(text));
		}
	}
}
