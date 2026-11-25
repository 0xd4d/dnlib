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

		// Type.Namespace of a nested type is the namespace of its outermost declaring type, and null if there's
		// no namespace. dnlib returns an empty string then.
		[Theory, MemberData(nameof(Types))]
		public void ReflectionNamespaceIsTypeNamespace(Type type) {
			// The namespace of an array, pointer or reference type is the namespace of the type it's made of.
			// Mono returns null for an array of a nested type, so ask that type.
			var elementType = type;
			while (elementType.HasElementType)
				elementType = elementType.GetElementType()!;
			Assert.Equal(elementType.Namespace ?? string.Empty, Import(type).ReflectionNamespace);
		}

		// System.Type.Name of a generic parameter is its name, Type.Namespace is the namespace of the type that
		// declares it (or that declares its method) and Type.FullName is null. dnlib only knows the name and the
		// namespace if the signature has an owner. ReflectionFullName is never null, it's an empty string.
		[Fact]
		public void ReflectionNamesOfGenericParameters() {
			var module = CreateModule();
			var outer = new TypeDefUser("Ns", "Outer");
			var inner = new TypeDefUser("Inner`1");
			inner.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "TType"));
			var method = new MethodDefUser("Method", MethodSig.CreateStaticGeneric(1, module.CorLibTypes.Void));
			method.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "TMethod"));
			module.Types.Add(outer);
			outer.NestedTypes.Add(inner);
			inner.Methods.Add(method);

			var typeParameter = new GenericVar(0, inner);
			Assert.Equal("TType", typeParameter.ReflectionName);
			Assert.Equal("Ns", typeParameter.ReflectionNamespace);
			Assert.Equal(string.Empty, typeParameter.ReflectionFullName);
			Assert.Equal(string.Empty, new TypeSpecUser(typeParameter).ReflectionFullName);
			Assert.Equal(string.Empty, new SZArraySig(typeParameter).ReflectionFullName);
			Assert.Equal(string.Empty, new PtrSig(new ByRefSig(typeParameter)).ReflectionFullName);
			// FullName is dnlib's own name and has no such rule
			Assert.Equal("TType", typeParameter.FullName);
			Assert.Equal("Ns", new SZArraySig(typeParameter).ReflectionNamespace);

			var methodParameter = new GenericMVar(0, method);
			Assert.Equal("TMethod", methodParameter.ReflectionName);
			Assert.Equal("Ns", methodParameter.ReflectionNamespace);
			Assert.Equal(string.Empty, methodParameter.ReflectionFullName);

			var withoutOwner = new GenericVar(1);
			Assert.Equal("!1", withoutOwner.ReflectionName);
			Assert.Equal(string.Empty, withoutOwner.ReflectionNamespace);
			Assert.Equal(string.Empty, withoutOwner.ReflectionFullName);
			Assert.Equal("!!0", new GenericMVar(0).ReflectionName);
		}

		// The same as above, compared with reflection
		[Fact]
		public void ReflectionNamespaceOfAGenericParameterIsTypeNamespace() {
			Assert.SkipWhen(Type.GetType("Mono.Runtime") is not null, "Mono's Type.Namespace of a generic parameter is null");
			var typeParameter = typeof(Outer<>.Inner<>).GetGenericArguments()[1];
			Assert.Equal("U", typeParameter.Name);
			Assert.Equal(typeof(Outer<>).Namespace, typeParameter.Namespace);
			Assert.Null(typeParameter.FullName);
			Assert.Null(typeParameter.MakeArrayType().FullName);
			Assert.Null(typeParameter.MakePointerType().FullName);

			var methodParameter = typeof(Array).GetMethod("Empty")!.GetGenericArguments()[0];
			Assert.Equal("System", methodParameter.Namespace);
		}

		// The namespace in the metadata row of a nested type is empty, and that's what Namespace returns
		[Fact]
		public void NamespaceOfANestedTypeIsEmpty() {
			var nested = Import(typeof(Outer<int>.Inner<string>.Innermost));

			Assert.Equal(string.Empty, nested.Namespace);
			Assert.Equal(typeof(Outer<>).Namespace, nested.ReflectionNamespace);
		}

		[Fact]
		public void ReflectionNamespaceOfANestedTypeDefTypeRefAndExportedType() {
			var module = CreateModule();
			var outerDef = new TypeDefUser("Ns", "Outer");
			var innerDef = new TypeDefUser("Inner");
			var innermostDef = new TypeDefUser("Innermost");
			module.Types.Add(outerDef);
			outerDef.NestedTypes.Add(innerDef);
			innerDef.NestedTypes.Add(innermostDef);
			Assert.Equal("Ns", innermostDef.ReflectionNamespace);
			Assert.Equal("Ns", outerDef.ReflectionNamespace);
			Assert.Equal(string.Empty, innermostDef.Namespace);

			var outerRef = new TypeRefUser(module, "Ns", "Outer", module.CorLibTypes.AssemblyRef);
			var innerRef = new TypeRefUser(module, string.Empty, "Inner", outerRef);
			var innermostRef = new TypeRefUser(module, string.Empty, "Innermost", innerRef);
			Assert.Equal("Ns", innermostRef.ReflectionNamespace);
			Assert.Equal("Ns", outerRef.ReflectionNamespace);
			Assert.Equal(string.Empty, innermostRef.Namespace);

			var outerExported = new ExportedTypeUser(module, 0, "Ns", "Outer", TypeAttributes.Public, module.CorLibTypes.AssemblyRef);
			var innerExported = new ExportedTypeUser(module, 0, string.Empty, "Inner", TypeAttributes.NestedPublic, outerExported);
			var innermostExported = new ExportedTypeUser(module, 0, string.Empty, "Innermost", TypeAttributes.NestedPublic, innerExported);
			Assert.Equal("Ns", innermostExported.ReflectionNamespace);
			Assert.Equal("Ns", outerExported.ReflectionNamespace);
			Assert.Equal(string.Empty, innermostExported.Namespace);
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
		public void FullNameWithoutAssemblyCanBeParsed(Type type) {
			// The name without the assembly can be parsed too. Which assembly the type is then in isn't checked.
			if (type.Assembly != typeof(object).Assembly)
				return;
			var module = CreateModule();

			var parsed = TypeNameParser.ParseAsTypeSigReflection(module, type.FullName!, null);

			Assert.NotNull(parsed);
			Assert.Equal(type.FullName, parsed.ReflectionFullName);
		}
	}
}
