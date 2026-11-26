// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Names {
	/// <summary>
	/// <see cref="TypeNameParser"/> parses type names in the format of <see cref="Type.AssemblyQualifiedName"/>.
	/// Such names are stored as strings in metadata (eg. a <c>typeof()</c> argument of a custom attribute), so
	/// obfuscated files can have any string there. See also <see cref="ReflectionTypeNameTests"/>.
	/// </summary>
	public class TypeNameParserTests {
		static ModuleDefUser CreateModule() {
			var module = new ModuleDefUser("Test.dll");
			new AssemblyDefUser("Test").Modules.Add(module);
			return module;
		}

		static TypeSig? Parse(string name) => TypeNameParser.ParseAsTypeSigReflection(CreateModule(), name, null);

		static TypeRef ParseTypeRef(string name) {
			var type = TypeNameParser.ParseReflection(CreateModule(), name, null);
			return Assert.IsAssignableFrom<TypeRef>(type);
		}

		static AssemblyRef GetAssembly(TypeRef type) => Assert.IsAssignableFrom<AssemblyRef>(type.ResolutionScope);

		[Fact]
		public void NamespaceAndName() {
			var type = ParseTypeRef("My.Name.Space.MyType, MyAssembly");

			Assert.Equal("My.Name.Space", type.Namespace);
			Assert.Equal("MyType", type.Name);
			Assert.Equal("MyAssembly", GetAssembly(type).Name);
		}

		[Fact]
		public void TypeWithoutNamespace() {
			var type = ParseTypeRef("MyType, MyAssembly");

			Assert.Equal("", type.Namespace);
			Assert.Equal("MyType", type.Name);
		}

		[Fact]
		public void AssemblyNameAttributes() {
			var assembly = GetAssembly(ParseTypeRef("Ns.MyType, MyAssembly, Version=1.2.3.4, Culture=en-US, PublicKeyToken=0123456789abcdef"));

			Assert.Equal("MyAssembly", assembly.Name);
			Assert.Equal(new Version(1, 2, 3, 4), assembly.Version);
			Assert.Equal("en-US", assembly.Culture);
			Assert.Equal(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF }, assembly.PublicKeyOrToken?.Data);
		}

		[Fact]
		public void NeutralCultureAndNullPublicKeyToken() {
			var assembly = GetAssembly(ParseTypeRef("Ns.MyType, MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"));

			Assert.Equal("", assembly.Culture);
			Assert.True(PublicKeyBase.IsNullOrEmpty2(assembly.PublicKeyOrToken));
		}

		[Fact]
		public void NestedTypes() {
			var innermost = ParseTypeRef("Ns.Outer+Inner+Innermost, MyAssembly");

			Assert.Equal("Innermost", innermost.Name);
			var inner = Assert.IsAssignableFrom<TypeRef>(innermost.ResolutionScope);
			Assert.Equal("Inner", inner.Name);
			var outer = Assert.IsAssignableFrom<TypeRef>(inner.ResolutionScope);
			Assert.Equal("Ns", outer.Namespace);
			Assert.Equal("Outer", outer.Name);
			Assert.Equal("MyAssembly", GetAssembly(outer).Name);
		}

		[Theory]
		[InlineData("Ns.T[], A", typeof(SZArraySig))]
		[InlineData("Ns.T[,], A", typeof(ArraySig))]
		[InlineData("Ns.T[*], A", typeof(ArraySig))]
		[InlineData("Ns.T*, A", typeof(PtrSig))]
		[InlineData("Ns.T&, A", typeof(ByRefSig))]
		public void ArraysPointersAndReferences(string name, Type expectedSigType) {
			var type = Parse(name);

			Assert.NotNull(type);
			Assert.IsType(expectedSigType, type);
			Assert.Equal("Ns.T", type.Next?.FullName);
		}

		[Fact]
		public void GenericInstanceWithAssemblyQualifiedArguments() {
			var type = Assert.IsType<GenericInstSig>(Parse("Ns.G`2[[Ns.A, AsmA],[Ns.B[], AsmB]], AsmG"));

			Assert.Equal("Ns.G`2", type.GenericType?.FullName);
			Assert.Equal(2, type.GenericArguments.Count);
			Assert.Equal("Ns.A", type.GenericArguments[0]?.FullName);
			Assert.Equal("AsmA", type.GenericArguments[0]?.DefinitionAssembly?.Name);
			Assert.Equal("Ns.B[]", type.GenericArguments[1]?.FullName);
			Assert.Equal("AsmB", type.GenericArguments[1]?.DefinitionAssembly?.Name);
			Assert.Equal("AsmG", type.GenericType?.DefinitionAssembly?.Name);
		}

		[Fact]
		public void EscapedCharsAreAPartOfTheName() {
			// These chars have a meaning in a type name, so they're escaped with '\' if they're in a name
			var type = ParseTypeRef(@"Ns.A\+B\,C\[D\]E\*F\&G\\H, MyAssembly");

			Assert.Equal(@"A+B,C[D]E*F&G\H", type.Name);
		}

		// Fixed in 0d09ff0c: the CLR's parser gets a null-terminated string
		[Fact]
		public void NameEndsAtTheFirstNullChar() {
			var type = ParseTypeRef("Ns.MyType\0xyz, Other");

			Assert.Equal("Ns", type.Namespace);
			Assert.Equal("MyType", type.Name);
		}

		// Fixed in d8a11f00: the CLR ignores attributes it doesn't know
		[Fact]
		public void AssemblyNameAttributeThatCanNotBeParsedIsIgnored() {
			var assembly = GetAssembly(ParseTypeRef("Ns.MyType, MyAssembly, Version=1.2.3.4, Publi*KeyToken=0123456789abcdef, Culture=fr"));

			Assert.Equal("MyAssembly", assembly.Name);
			Assert.Equal(new Version(1, 2, 3, 4), assembly.Version);
			Assert.Equal("fr", assembly.Culture);
			Assert.True(PublicKeyBase.IsNullOrEmpty2(assembly.PublicKeyOrToken));
		}

		[Fact]
		public void UnknownAssemblyNameAttributeIsIgnored() {
			var assembly = GetAssembly(ParseTypeRef("Ns.MyType, MyAssembly, Unknown=value, Version=1.2.3.4"));

			Assert.Equal(new Version(1, 2, 3, 4), assembly.Version);
		}

		// Fixed in b84dcd42: values were read like type names and ended at a space or '+'
		[Fact]
		public void AssemblyNameAttributeValueEndsAtTheNextComma() {
			var assembly = GetAssembly(ParseTypeRef("Ns.MyType, MyAssembly, Culture=a b+c, Version=1.2.3.4"));

			Assert.Equal("a b+c", assembly.Culture);
			Assert.Equal(new Version(1, 2, 3, 4), assembly.Version);
		}

		// Fixed in eac95c2e: ']' ended the assembly name everywhere
		[Fact]
		public void ClosingBracketIsAPartOfAnAssemblyNameThatIsNotInAGenericArgument() {
			var assembly = GetAssembly(ParseTypeRef("Ns.MyType, My]Assembly"));

			Assert.Equal("My]Assembly", assembly.Name);
		}

		[Fact]
		public void ClosingBracketEndsAnAssemblyNameInAGenericArgument() {
			var type = Assert.IsType<GenericInstSig>(Parse("Ns.G`1[[Ns.A, AsmA]], AsmG"));

			Assert.Equal("AsmA", type.GenericArguments[0]?.DefinitionAssembly?.Name);
		}

		// Fixed in 4f409c31: this created a generic instance without a generic type
		[Theory]
		[InlineData("Ns.G`1&[[Ns.A, AsmA]], AsmG")]
		[InlineData("Ns.G`1[][[Ns.A, AsmA]], AsmG")]
		[InlineData("Ns.G`1*[[Ns.A, AsmA]], AsmG")]
		public void GenericArgumentsThatDoNotFollowAGenericTypeAreInvalid(string name) =>
			Assert.Null(Parse(name));

		// Names that look wrong but are names: a space is a character of a name, and a name in brackets is a
		// generic argument that's in the same assembly
		[Fact]
		public void UnusualNamesThatAreValid() {
			Assert.Equal("Ns.T extra", Parse("Ns.T extra")!.FullName);
			var instance = Assert.IsType<GenericInstSig>(Parse("Ns.T[x]"));
			Assert.Equal("Ns.T<x>", instance.FullName);
			Assert.Equal("x", Assert.Single(instance.GenericArguments)!.TypeName);
		}

		[Theory]
		[InlineData("")]
		[InlineData(",")]
		[InlineData("Ns.T[")]
		[InlineData("Ns.G`1[[")]
		[InlineData("Ns.G`1[[Ns.A, AsmA")]
		[InlineData("+")]
		[InlineData("!")]
		[InlineData("!x")]
		[InlineData("!!")]
		[InlineData("!!x")]
		[InlineData("!99999999999")]
		[InlineData("!!4294967296")]
		[InlineData("Ns.T[*")]
		[InlineData("Ns.T[,")]
		[InlineData("Ns.T[1..")]
		[InlineData("Ns.T[1..2")]
		[InlineData("Ns.T[1...")]
		[InlineData("Ns.T[0..0x10]")]
		[InlineData("Ns.T[-2147483649..0]")]
		[InlineData("Ns.T[0..536870912]")]
		[InlineData("Ns.G`1[[Ns.A, AsmA]")]
		[InlineData("Ns.G`1[[Ns.A, AsmA],")]
		[InlineData("Ns.G`1[[Ns.A, AsmA],[")]
		[InlineData("Ns.G`1[Ns.A,")]
		[InlineData("Ns.T, ")]
		[InlineData("Ns.T,")]
		[InlineData("Ns.T+")]
		[InlineData("Ns.T&&x")]
		public void InvalidNameReturnsNull(string name) =>
			Assert.Null(Parse(name));

		[Fact]
		public void OrThrowMethodsThrowTypeNameParserException() {
			var module = CreateModule();

			Assert.Throws<TypeNameParserException>(() => TypeNameParser.ParseAsTypeSigReflectionOrThrow(module, "Ns.T[", null));
			Assert.Throws<TypeNameParserException>(() => TypeNameParser.ParseReflectionOrThrow(module, "Ns.T[", null));
		}

		// Fixed in a322e707: these chars weren't escaped, so the name couldn't be parsed again
		[Theory]
		[InlineData(@"My\Assembly", "")]
		[InlineData("MyAssembly", "a,b")]
		[InlineData("MyAssembly", "a=b")]
		[InlineData("MyAssembly", @"a\b")]
		[InlineData("My,Assembly", "")]
		[InlineData("My=Assembly", "")]
		public void AssemblyQualifiedNameWithSpecialCharsCanBeParsedAgain(string assemblyName, string culture) {
			var module = CreateModule();
			var assembly = new AssemblyRefUser(assemblyName, new Version(1, 2, 3, 4), null, culture);
			var type = new TypeRefUser(module, "Ns", "MyType", assembly);

			var parsed = Assert.IsAssignableFrom<TypeRef>(TypeNameParser.ParseReflection(module, type.AssemblyQualifiedName, null));

			var parsedAssembly = GetAssembly(parsed);
			Assert.Equal(assemblyName, parsedAssembly.Name);
			Assert.Equal(culture, parsedAssembly.Culture);
			Assert.Equal(new Version(1, 2, 3, 4), parsedAssembly.Version);
		}

		[Fact]
		public void TypeWithoutAssemblyIsFoundInTheModule() {
			var module = CreateModule();
			var typeDef = new TypeDefUser("Ns", "InModule", module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(typeDef);

			Assert.Same(typeDef, TypeNameParser.ParseReflection(module, "Ns.InModule", null));
		}

		// The CLR looks for such a type in the assembly that has the name, then in the core library. dnlib can
		// only tell that it's in the core library if it can load the core library. Here it can't (the module has
		// no assembly resolver), so the type is a reference to the module's own assembly.
		[Fact]
		public void TypeWithoutAssemblyThatIsNotFoundIsAReferenceToTheModulesAssembly() {
			var module = CreateModule();

			var type = Assert.IsAssignableFrom<TypeRef>(TypeNameParser.ParseReflection(module, "Ns.NotInModule", null));

			Assert.Equal("Test", GetAssembly(type).Name);
		}

		// The bounds of an array dimension: "lower..upper", or "lower..." if it has no size. The runtime's type
		// names don't have bounds (such an array is "T[*]" or "T[,]"), this is dnlib's own syntax.
		[Theory]
		[InlineData("Ns.T[0..4], A", 1, new[] { 0 }, new uint[] { 5 })]
		[InlineData("Ns.T[1..5,-2..2], A", 2, new[] { 1, -2 }, new uint[] { 5, 5 })]
		[InlineData("Ns.T[-5..-3], A", 1, new[] { -5 }, new uint[] { 3 })]
		[InlineData("Ns.T[3...], A", 1, new[] { 3 }, new uint[0])]
		[InlineData("Ns.T[ 0 .. 9 , 1..1 ], A", 2, new[] { 0, 1 }, new uint[] { 10, 1 })]
		[InlineData("Ns.T[*,*], A", 2, new int[0], new uint[0])]
		public void ArrayBounds(string name, uint expectedRank, int[] expectedLowerBounds, uint[] expectedSizes) {
			var array = Assert.IsType<ArraySig>(Parse(name));

			Assert.Equal(expectedRank, array.Rank);
			Assert.Equal(expectedLowerBounds, array.LowerBounds);
			Assert.Equal(expectedSizes, array.Sizes);
			Assert.Equal("Ns.T", array.Next!.FullName);
		}

		[Theory]
		[InlineData("Ns.T[5..4], A")]               // upper < lower
		[InlineData("Ns.T[0..99999999999], A")]     // too big for an integer
		[InlineData("Ns.T[0.4], A")]                // one dot
		[InlineData("Ns.T[0...4], A")]              // three dots and a size
		[InlineData("Ns.T[-], A")]
		public void InvalidArrayBoundsReturnNull(string name) =>
			Assert.Null(Parse(name));

		[Fact]
		public void AssemblyNameWithAPublicKeyAndFlags() {
			var publicKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
			string publicKeyText = string.Concat(publicKey.Select(a => a.ToString("X2")));

			var assembly = GetAssembly(ParseTypeRef($"Ns.T, Lib, Version=1.2.3.4, Culture=en-US, PublicKey={publicKeyText}, Retargetable=Yes, ContentType=WindowsRuntime"));

			Assert.Equal("Lib", assembly.Name);
			Assert.Equal(new Version(1, 2, 3, 4), assembly.Version);
			Assert.Equal("en-US", assembly.Culture);
			Assert.True(assembly.HasPublicKey);
			Assert.Equal(publicKey, Assert.IsType<PublicKey>(assembly.PublicKeyOrToken).Data);
			Assert.True(assembly.IsRetargetable);
			Assert.True(assembly.IsContentTypeWindowsRuntime);
		}

		[Fact]
		public void AssemblyNameWithANullPublicKeyAndFlagsThatAreOff() {
			var assembly = GetAssembly(ParseTypeRef("Ns.T, Lib, PublicKey=null, Retargetable=No, ContentType=Default"));

			Assert.True(assembly.HasPublicKey);
			Assert.True(PublicKeyBase.IsNullOrEmpty2(Assert.IsType<PublicKey>(assembly.PublicKeyOrToken)));
			Assert.False(assembly.IsRetargetable);
			Assert.True(assembly.IsContentTypeDefault);
		}

		sealed class FixedAssemblyFinder : IAssemblyRefFinder {
			public TypeRef? AskedFor;
			public AssemblyRef? FindAssemblyRef(TypeRef nonNestedTypeRef) {
				AskedFor = nonNestedTypeRef;
				return new AssemblyRefUser("FoundByTheHelper", new Version(7, 0, 0, 0));
			}
		}

		// A helper can say in which assembly a type without an assembly name is. It's asked for the
		// outermost type, and not if the name has an assembly.
		[Fact]
		public void HelperFindsTheAssemblyOfATypeWithoutAnAssemblyName() {
			var module = CreateModule();
			var finder = new FixedAssemblyFinder();

			var nested = Assert.IsAssignableFrom<TypeRef>(TypeNameParser.ParseReflection(module, "Ns.Outer+Nested", finder));

			Assert.Equal("Ns.Outer", finder.AskedFor!.FullName);
			Assert.Equal("FoundByTheHelper", nested.DefinitionAssembly!.Name);

			finder.AskedFor = null;
			var withAssembly = Assert.IsAssignableFrom<TypeRef>(TypeNameParser.ParseReflection(module, "Ns.Outer, Other", finder));
			Assert.Null(finder.AskedFor);
			Assert.Equal("Other", withAssembly.DefinitionAssembly!.Name);
		}

		// A generic parameter (!0, !!0) in a name gets its owner from the context
		[Fact]
		public void ParseWithAGenericParameterContext() {
			var module = CreateModule();
			var type = new TypeDefUser("Ns", "Generic`1", module.CorLibTypes.Object.TypeDefOrRef);
			type.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			module.Types.Add(type);
			var context = new GenericParamContext(type);

			var list = Assert.IsType<GenericInstSig>(TypeNameParser.ParseAsTypeSigReflection(module, "Ns.List`1[[System.String, mscorlib]], A", null, context));
			Assert.Equal("Ns.List`1<System.String>", list.FullName);
			var asType = TypeNameParser.ParseReflectionOrThrow(module, "Ns.List`1[[System.String, mscorlib]], A", null, context);
			Assert.Equal("Ns.List`1<System.String>", Assert.IsAssignableFrom<TypeSpec>(asType).FullName);
			Assert.Equal("Ns.T[]", TypeNameParser.ParseAsTypeSigReflectionOrThrow(module, "Ns.T[], A", null, context).FullName);
		}

		static ModuleDefUser CreateModuleWithAType(Version assemblyVersion) {
			var module = new ModuleDefUser("Test.dll");
			new AssemblyDefUser("Test", assemblyVersion).Modules.Add(module);
			module.Types.Add(new TypeDefUser("Ns", "MyType", module.CorLibTypes.Object.TypeDefOrRef));
			return module;
		}

		// Fixed in this commit: a version number that isn't in the name (or is 65535) has no value, and the CLR
		// then accepts any number there and after it. Such a name of the module's own assembly was a reference
		// to another assembly, so a type of a module whose assembly version has a 65535 number wasn't the same
		// type any more when the module was written and read again.
		[Theory]
		[InlineData("1.2.3.4", "Ns.MyType, Test, Version=1.2.3.4")]
		[InlineData("1.2.3.4", "Ns.MyType, Test, Version=1.2.3")]
		[InlineData("1.2.3.4", "Ns.MyType, Test, Version=1.2")]
		[InlineData("1.2.3.4", "Ns.MyType, Test, Version=1.2.65535.4")]
		[InlineData("1.2.3.4", "Ns.MyType, Test")]
		[InlineData("1.2.65535.4", "Ns.MyType, Test, Version=1.2")]
		[InlineData("65535.2.3.4", "Ns.MyType, Test")]
		public void NameWithVersionNumbersWithoutAValueIsTheTypeOfTheModule(string assemblyVersion, string name) {
			var module = CreateModuleWithAType(Version.Parse(assemblyVersion));

			var type = TypeNameParser.ParseReflection(module, name, null);

			Assert.Same(module.Types[1], type);
		}

		[Theory]
		[InlineData("Ns.MyType, Test, Version=1.2.3.5")]
		[InlineData("Ns.MyType, Test, Version=1.3")]
		[InlineData("Ns.MyType, Test, Version=1.2, Culture=de")]
		[InlineData("Ns.MyType, Other")]
		public void NameOfAnotherAssemblyIsNotTheTypeOfTheModule(string name) {
			var module = CreateModuleWithAType(new Version(1, 2, 3, 4));

			Assert.IsAssignableFrom<TypeRef>(TypeNameParser.ParseReflection(module, name, null));
		}

		// Fixed in this commit: the full name of an assembly ends its version before the first number that is
		// 65535 (like AssemblyName.FullName), which can leave one number. Nothing can read that version, not
		// System.Reflection either, so the assembly in the name of a type has no version then: a type of such
		// an assembly was null when its name was read from a custom attribute that dnlib had written.
		[Theory]
		[InlineData("1.2.3.4", "Ns.MyType, Test, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("1.2.3.65535", "Ns.MyType, Test, Version=1.2.3, Culture=neutral, PublicKeyToken=null")]
		[InlineData("1.2.65535.4", "Ns.MyType, Test, Version=1.2, Culture=neutral, PublicKeyToken=null")]
		[InlineData("1.65535.3.4", "Ns.MyType, Test, Culture=neutral, PublicKeyToken=null")]
		[InlineData("65535.2.3.4", "Ns.MyType, Test, Culture=neutral, PublicKeyToken=null")]
		public void AssemblyQualifiedNameOfATypeOfTheModuleIsReadBack(string assemblyVersion, string expectedName) {
			var module = CreateModuleWithAType(Version.Parse(assemblyVersion));

			string name = module.Types[1].AssemblyQualifiedName;

			Assert.Equal(expectedName, name);
			Assert.Same(module.Types[1], TypeNameParser.ParseReflection(module, name, null));
			// The name of the assembly itself is the one of System.Reflection
			if (assemblyVersion == "1.65535.3.4")
				Assert.Equal("Test, Version=1, Culture=neutral, PublicKeyToken=null", module.Assembly!.FullName);
		}
	}
}
