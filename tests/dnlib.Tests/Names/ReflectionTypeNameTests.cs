// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Names {
	/// <summary>
	/// dnlib creates the same type names as System.Reflection (<c>ReflectionFullName</c>,
	/// <c>AssemblyQualifiedName</c>, ...) and can parse them (<see cref="TypeNameParser"/>). These names are
	/// stored in metadata, eg. a <c>typeof()</c> argument of a custom attribute, so they must be exact.
	/// The expected names come from <see cref="Type"/>.
	/// </summary>
	public unsafe class ReflectionTypeNameTests {
		public static TheoryData<Type> Types => new TheoryData<Type> {
			typeof(int),
			typeof(string),
			typeof(object),
			typeof(void),
			typeof(IntPtr),
			typeof(TypedReference),
			// Arrays, pointers and references
			typeof(int[]),
			typeof(int[,]),
			typeof(int[,,][]),
			typeof(string[][]),
			typeof(int*),
			typeof(void**),
			typeof(int*[]),
			typeof(int).MakeByRefType(),
			typeof(string[]).MakeByRefType(),
			// An array with one dimension that isn't a vector (it can have a lower bound)
			typeof(int).MakeArrayType(1),
			// Nested types
			typeof(Environment.SpecialFolder),
			typeof(Environment.SpecialFolder[]),
			// Generic types
			typeof(List<int>),
			typeof(List<string>[]),
			typeof(Dictionary<string, int>),
			typeof(Dictionary<string, List<int[]>>),
			typeof(KeyValuePair<int, string>[,]),
			typeof(int?),
			typeof(Action<int>),
			typeof(Func<int[], Dictionary<string, object>, string>),
			typeof(IEnumerable<IEnumerable<int>>),
			// Generic type definitions
			typeof(List<>),
			typeof(Dictionary<,>),
			// Types nested in generic types have the generic arguments of their declaring types
			typeof(List<int>.Enumerator),
			typeof(Dictionary<int, string>.KeyCollection),
			typeof(Dictionary<int, string>.KeyCollection.Enumerator),
			typeof(Dictionary<,>.KeyCollection),
			typeof(Dictionary<List<int>, string[]>.ValueCollection[]),
			// Types of this assembly
			typeof(ReflectionTypeNameTests),
			typeof(Outer<int>.Inner<string>),
			typeof(Outer<>.Inner<>),
			typeof(Outer<int[]>.Inner<List<string>>.Innermost),
			typeof(Outer<int>.NotGeneric),
		};

		public class Outer<T> {
			public class Inner<U> {
				public class Innermost { }
			}
			public class NotGeneric { }
		}

		// A module whose core library is the core library of the runtime that runs the tests, so the assembly
		// names of types like System.Int32 are the ones reflection returns
		static ModuleDefUser CreateModule() {
			var coreLibrary = new AssemblyRefUser(typeof(object).Assembly.GetName());
			var module = new ModuleDefUser("Test.dll", null, coreLibrary);
			new AssemblyDefUser("Test").Modules.Add(module);
			return module;
		}

		static TypeSig Import(Type type) {
			var imported = new Importer(CreateModule()).ImportAsTypeSigOrNull(type);
			Assert.NotNull(imported);
			return imported;
		}

		[Theory, MemberData(nameof(Types))]
		public void ReflectionFullNameIsTypeFullName(Type type) =>
			Assert.Equal(type.FullName, Import(type).ReflectionFullName);

		[Theory, MemberData(nameof(Types))]
		public void ReflectionNameIsTypeName(Type type) =>
			Assert.Equal(type.Name, Import(type).ReflectionName);

		[Theory, MemberData(nameof(Types))]
		public void ReflectionNamespaceIsTypeNamespaceExceptForNestedTypes(Type type) {
			// Type.Namespace of a nested type is the namespace of its outermost declaring type. dnlib returns
			// the namespace that's stored in the nested type's own metadata row, which is empty.
			var expected = IsNested(type) ? "" : type.Namespace;

			Assert.Equal(expected, Import(type).ReflectionNamespace);
		}

		// true if the type, or the type an array/pointer/reference type is made of, is a nested type
		static bool IsNested(Type type) {
			while (type.HasElementType)
				type = type.GetElementType()!;
			return type.IsNested;
		}

		[Theory, MemberData(nameof(Types))]
		public void AssemblyQualifiedNameIsTypeAssemblyQualifiedName(Type type) =>
			Assert.Equal(type.AssemblyQualifiedName, Import(type).AssemblyQualifiedName);

		[Theory, MemberData(nameof(Types))]
		public void ParsedAssemblyQualifiedNameIsTheSameType(Type type) {
			var module = CreateModule();

			var parsed = TypeNameParser.ParseAsTypeSigReflection(module, type.AssemblyQualifiedName!, null);

			Assert.NotNull(parsed);
			Assert.Equal(type.AssemblyQualifiedName, parsed.AssemblyQualifiedName);
			Assert.True(new SigComparer().Equals(parsed, type), $"The parsed type {parsed.FullName} doesn't equal the Type");
		}

		[Theory, MemberData(nameof(Types))]
		public void ParsedFullNameIsTheSameTypeInTheCoreLibrary(Type type) {
			// A name without an assembly is looked up in the module and then in the core library
			if (type.Assembly != typeof(object).Assembly)
				return;
			var module = CreateModule();

			var parsed = TypeNameParser.ParseAsTypeSigReflection(module, type.FullName!, null);

			Assert.NotNull(parsed);
			Assert.Equal(type.FullName, parsed.ReflectionFullName);
		}
	}
}
