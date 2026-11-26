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

		// The type names that the tests of System.Reflection.Metadata's TypeName parser have. These types don't
		// exist, so the runtime isn't asked. The first ones are invalid for both parsers.
		[Theory]
		[InlineData("Namespace.Containing++Nested", "invalid")]
		[InlineData("DoubleNestingChar++Bla", "invalid")]
		[InlineData("TypeNameFollowedBySome[] unconsumedCharacters", "invalid")]
		[InlineData("MissingAssemblyName, ", "invalid")]
		[InlineData("ExtraComma, ,", "invalid")]
		[InlineData("ExtraComma, , System.Runtime", "invalid")]
		[InlineData("UsingGenericSyntaxButNotProvidingGenericArgs[[]]", "invalid")]
		[InlineData("MissingClosingSquareBrackets`1[[type1, assembly1", "invalid")]
		[InlineData("MissingClosingSquareBracket`1[[type1, assembly1]", "invalid")]
		[InlineData("MissingClosingSquareBracketsMixedMode`2[[type1, assembly1], type2", "invalid")]
		[InlineData("MissingClosingSquareBrackets`2[[type1, assembly1], [type2, assembly2", "invalid")]
		[InlineData("MissingClosingSquareBracketsMixedMode`2[type1, [type2, assembly2", "invalid")]
		[InlineData("MissingClosingSquareBracketsMixedMode`2[type1, [type2, assembly2]", "invalid")]
		// System.Reflection.Metadata rejects these, dnlib reads them: a comma after the last generic argument,
		// and a backslash that isn't before a special character or that is the last character
		[InlineData("ExtraCommaAfterFirstGenericArg`1[[type1, assembly1],]", "ExtraCommaAfterFirstGenericArg`1[type1]")]
		[InlineData("EscapeCharacterAtTheEnd\\", "EscapeCharacterAtTheEnd")]
		[InlineData("EscapeNonSpecialChar\\a", "EscapeNonSpecialChara")]
		[InlineData("EscapeNonSpecialChar\\0", "EscapeNonSpecialChar0")]
		// Both read these: a name that starts with a dot, nested types, an escaped '+' that is a part of the
		// name, a name that ends with a backslash, and more or fewer generic arguments than the name says
		[InlineData(".NoNamespace", "NoNamespace")]
		[InlineData("Type+InnerType+InnermostType", "Type+InnerType+InnermostType")]
		[InlineData("NotNested\\+Name", "NotNested+Name")]
		[InlineData("NameEndingInBackSlash\\\\+NestedName", "NameEndingInBackSlash\\+NestedName")]
		[InlineData("Name`3[[int], [bool]]", "Name`3[int,bool]")]
		[InlineData("Name[[int], [bool]]", "Name[int,bool]")]
		public void TypeNameOfATypeThatDoesNotExistIsParsed(string text, string expectedDnlib) =>
			Assert.Equal(expectedDnlib, ParseWithDnlib(text));

		// The runtime has arrays of up to 32 dimensions, the parsers don't have that limit
		[Fact]
		public void ArrayWithMoreDimensionsThanTheRuntimeHasIsParsed() {
			string text = "RankOf128[" + new string(',', 127) + "]";
			var module = new ModuleDefUser("Test.dll");

			var type = TypeNameParser.ParseAsTypeSigReflection(module, text, null);

			var array = Assert.IsType<ArraySig>(type);
			Assert.Equal(128U, array.Rank);
			// A full name is written with 100 dimensions at most (a file can have any number in a signature)
			Assert.Equal("RankOf128[" + new string(',', 99) + "]", type.FullName);
		}
	}
}
