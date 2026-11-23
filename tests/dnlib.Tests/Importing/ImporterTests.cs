// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using Xunit;
using FieldAttributes = dnlib.DotNet.FieldAttributes;

namespace dnlib.Tests.Importing {
	/// <summary>
	/// <see cref="Importer"/> creates references in one module to types and members of another module (or of
	/// System.Reflection), so they can be used in the first module's code.
	/// </summary>
	public class ImporterTests {
		static ModuleDefUser CreateModule(string assemblyName) {
			var module = new ModuleDefUser(assemblyName + ".dll");
			new AssemblyDefUser(assemblyName, new Version(1, 2, 3, 4)).Modules.Add(module);
			return module;
		}

		// A module with a class Ns.Source that has a static method and a static field
		static ModuleDefUser CreateSourceModule(out TypeDefUser type, out MethodDefUser method, out FieldDefUser field) {
			var module = CreateModule("Source");
			type = new TypeDefUser("Ns", "Source", module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(type);
			method = new MethodDefUser("Method", MethodSig.CreateStatic(module.CorLibTypes.Int32, module.CorLibTypes.String));
			type.Methods.Add(method);
			field = new FieldDefUser("Field", new FieldSig(module.CorLibTypes.Int64), FieldAttributes.Public | FieldAttributes.Static);
			type.Fields.Add(field);
			return module;
		}

		[Fact]
		public void TypeDefOfAnotherModuleBecomesATypeRefToItsAssembly() {
			CreateSourceModule(out var type, out _, out _);
			var target = CreateModule("Target");

			var imported = Assert.IsAssignableFrom<TypeRef>(new Importer(target).ImportOrNull(type));

			Assert.Equal("Ns.Source", imported.FullName);
			Assert.Same(target, imported.Module);
			var scope = Assert.IsAssignableFrom<AssemblyRef>(imported.ResolutionScope);
			Assert.Equal("Source", scope.Name);
			Assert.Equal(new Version(1, 2, 3, 4), scope.Version);
		}

		[Fact]
		public void NestedTypeDefBecomesATypeRefInATypeRef() {
			CreateSourceModule(out var type, out _, out _);
			var nested = new TypeDefUser("Nested");
			type.NestedTypes.Add(nested);

			var imported = Assert.IsAssignableFrom<TypeRef>(new Importer(CreateModule("Target")).ImportOrNull(nested));

			Assert.Equal("Nested", imported.Name);
			var declaringType = Assert.IsAssignableFrom<TypeRef>(imported.ResolutionScope);
			Assert.Equal("Ns.Source", declaringType.FullName);
			Assert.IsAssignableFrom<AssemblyRef>(declaringType.ResolutionScope);
		}

		[Fact]
		public void MethodDefBecomesAMemberRef() {
			CreateSourceModule(out _, out var method, out _);

			var imported = Assert.IsAssignableFrom<MemberRef>(new Importer(CreateModule("Target")).ImportOrNull(method));

			Assert.Equal("Method", imported.Name);
			Assert.True(imported.IsMethodRef);
			Assert.Equal("System.Int32 Ns.Source::Method(System.String)", imported.FullName);
			Assert.IsAssignableFrom<TypeRef>(imported.Class);
		}

		[Fact]
		public void FieldDefBecomesAMemberRef() {
			CreateSourceModule(out _, out _, out var field);

			var imported = Assert.IsAssignableFrom<MemberRef>(new Importer(CreateModule("Target")).ImportOrNull(field));

			Assert.True(imported.IsFieldRef);
			Assert.Equal("System.Int64 Ns.Source::Field", imported.FullName);
		}

		[Fact]
		public void DefsOfTheTargetModuleAreUsedIfTheOptionIsSet() {
			var module = CreateSourceModule(out var type, out var method, out var field);

			var importer = new Importer(module, ImporterOptions.TryToUseDefs);

			Assert.Same(type, importer.ImportOrNull(type));
			Assert.Same(method, importer.ImportOrNull(method));
			Assert.Same(field, importer.ImportOrNull(field));
		}

		[Fact]
		public void NullIsImportedAsNull() {
			var importer = new Importer(CreateModule("Target"));

			Assert.Null(importer.ImportOrNull((TypeDef?)null));
			Assert.Null(importer.ImportOrNull((TypeRef?)null));
			Assert.Null(importer.ImportOrNull((TypeSpec?)null));
			Assert.Null(importer.ImportOrNull((MethodDef?)null));
			Assert.Null(importer.ImportOrNull((FieldDef?)null));
			Assert.Null(importer.ImportOrNull((MemberRef?)null));
			Assert.Null(importer.ImportOrNull((MethodSpec?)null));
			Assert.Null(importer.ImportOrNull((TypeSig?)null));
			Assert.Null(importer.ImportOrNull((Type?)null));
			Assert.Null(importer.ImportOrNull((MethodBase?)null));
			Assert.Null(importer.ImportOrNull((FieldInfo?)null));
		}

		// Fixed in 0c607eef: the scope was created from the module the TypeRef is in, not from its own scope
		[Fact]
		public void ImportedTypeRefKeepsItsAssembly() {
			var source = CreateModule("Source");
			var typeRef = new TypeRefUser(source, "Ns", "Type", new AssemblyRefUser("Other", new Version(5, 6, 7, 8)));

			var imported = Assert.IsAssignableFrom<TypeRef>(new Importer(CreateModule("Target")).ImportOrNull(typeRef));

			var scope = Assert.IsAssignableFrom<AssemblyRef>(imported.ResolutionScope);
			Assert.Equal("Other", scope.Name);
			Assert.Equal(new Version(5, 6, 7, 8), scope.Version);
		}

		// A ModuleRef scope means the type is in another module of the same assembly (a multi-module assembly)
		[Fact]
		public void TypeRefToAnotherModuleOfTheAssemblyKeepsItsModuleRefInTheSameAssembly() {
			var source = CreateModule("Source");
			var typeRef = new TypeRefUser(source, "Ns", "Type", new ModuleRefUser(source, "other.netmodule"));
			// A second module of the same assembly
			var target = new ModuleDefUser("target.netmodule");
			source.Assembly!.Modules.Add(target);

			var imported = Assert.IsAssignableFrom<TypeRef>(new Importer(target).ImportOrNull(typeRef));

			var scope = Assert.IsAssignableFrom<ModuleRef>(imported.ResolutionScope);
			Assert.Equal("other.netmodule", scope.Name);
		}

		[Fact]
		public void TypeRefToAnotherModuleOfTheAssemblyBecomesAReferenceToTheAssemblyInAnotherAssembly() {
			var source = CreateModule("Source");
			var typeRef = new TypeRefUser(source, "Ns", "Type", new ModuleRefUser(source, "other.netmodule"));

			var imported = Assert.IsAssignableFrom<TypeRef>(new Importer(CreateModule("Target")).ImportOrNull(typeRef));

			var scope = Assert.IsAssignableFrom<AssemblyRef>(imported.ResolutionScope);
			Assert.Equal("Source", scope.Name);
		}

		// Fixed in 5cfc6641: the parameters after the sentinel ("...") of a call to a vararg method were dropped
		[Fact]
		public void ImportedMethodSigKeepsTheParametersAfterTheSentinel() {
			var source = CreateModule("Source");
			var signature = MethodSig.CreateStatic(source.CorLibTypes.Void, source.CorLibTypes.Int32);
			signature.CallingConvention |= CallingConvention.VarArg;
			signature.ParamsAfterSentinel = new List<TypeSig?> { source.CorLibTypes.String, source.CorLibTypes.Double };

			var imported = new Importer(CreateModule("Target")).ImportOrNull(signature);

			Assert.NotNull(imported);
			Assert.Equal("System.Int32", Assert.Single(imported.Params)?.FullName);
			Assert.NotNull(imported.ParamsAfterSentinel);
			Assert.Equal(new[] { "System.String", "System.Double" }, imported.ParamsAfterSentinel.Select(a => a?.FullName).ToArray());
		}

		// Fixed in acc2a910: a TypeSpec with a null signature was returned
		[Fact]
		public void TypeSpecWhoseSignatureCanNotBeImportedIsNotImported() {
			var importer = new Importer(CreateModule("Target"));
			var typeSpec = new TypeSpecUser();

			Assert.Null(importer.ImportOrNull(typeSpec));
			Assert.Throws<ImportException>(() => importer.Import(typeSpec));
		}

		public static class GenericMethods {
			public static T Create<T>() => default!;
		}

		public class GenericOwner<TOwner> { }

		// Fixed in 79efe381: the generic arguments were dropped and the generic method itself was returned
		[Fact]
		public void MethodInstantiatedWithAGenericParameterIsAMethodSpec() {
			var ownerParameter = typeof(GenericOwner<>).GetGenericArguments()[0];
			var method = typeof(GenericMethods).GetMethod(nameof(GenericMethods.Create))!.MakeGenericMethod(ownerParameter);

			var imported = Assert.IsAssignableFrom<MethodSpec>(new Importer(CreateModule("Target")).ImportOrNull(method));

			var argument = Assert.Single(imported.GenericInstMethodSig!.GenericArguments);
			Assert.IsType<GenericVar>(argument);
			Assert.Equal("Create", imported.Method?.Name);
		}

		[Fact]
		public void GenericMethodDefinitionIsNotAMethodSpec() {
			var method = typeof(GenericMethods).GetMethod(nameof(GenericMethods.Create))!;

			var imported = new Importer(CreateModule("Target")).ImportOrNull(method);

			Assert.IsAssignableFrom<MemberRef>(imported);
		}
	}
}
