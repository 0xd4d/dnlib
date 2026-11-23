// dnlib: See LICENSE.txt for more info

using System;
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

		[Theory]
		[InlineData("")]
		[InlineData(",")]
		[InlineData("Ns.T[")]
		[InlineData("Ns.G`1[[")]
		[InlineData("Ns.G`1[[Ns.A, AsmA")]
		[InlineData("+")]
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
	}
}
