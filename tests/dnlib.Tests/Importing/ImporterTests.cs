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

		// The source module with an assembly resolver that knows its assembly: the importer resolves a
		// reference to find the definition
		static ModuleDefUser CreateSourceModuleThatResolvesItself(out TypeDefUser type, out MethodDefUser method, out FieldDefUser field) {
			var module = CreateSourceModule(out type, out method, out field);
			var resolver = new AssemblyResolver();
			module.Context = new ModuleContext(resolver, new Resolver(resolver));
			resolver.AddToCache(module);
			return module;
		}

		// References in another module to the things in the target module: a reference to the class Ns.Source,
		// and references to its method and its field
		static void CreateReferencesToTheSourceModule(Version assemblyVersion, out TypeRefUser typeRef, out MemberRefUser methodRef, out MemberRefUser fieldRef) {
			var user = CreateModule("User");
			typeRef = new TypeRefUser(user, "Ns", "Source", new AssemblyRefUser("Source", assemblyVersion));
			methodRef = new MemberRefUser(user, "Method", MethodSig.CreateStatic(user.CorLibTypes.Int32, user.CorLibTypes.String), typeRef);
			fieldRef = new MemberRefUser(user, "Field", new FieldSig(user.CorLibTypes.Int64), typeRef);
		}

		// References to a type of the target module and to its members become the definitions
		[Fact]
		public void ReferencesToTheTargetModuleBecomeItsDefsIfTheOptionIsSet() {
			var module = CreateSourceModuleThatResolvesItself(out var type, out var method, out var field);
			CreateReferencesToTheSourceModule(new Version(1, 2, 3, 4), out var typeRef, out var methodRef, out var fieldRef);

			var importer = new Importer(module, ImporterOptions.TryToUseDefs);

			Assert.Same(type, importer.ImportOrNull(typeRef));
			Assert.Same(method, importer.ImportOrNull((IMethod)methodRef));
			Assert.Same(field, importer.ImportOrNull((IField)fieldRef));
			// Also in a signature
			var signature = Assert.IsType<ClassSig>(importer.ImportOrNull(new ClassSig(typeRef)));
			Assert.Same(type, signature.TypeDefOrRef);
		}

		// The overload that takes a MemberRef returns a MemberRef, so it can't return the definition
		[Fact]
		public void MemberRefOverloadReturnsAMemberRefOfTheTypeDef() {
			var module = CreateSourceModuleThatResolvesItself(out var type, out _, out _);
			CreateReferencesToTheSourceModule(new Version(1, 2, 3, 4), out _, out var methodRef, out var fieldRef);

			var importer = new Importer(module, ImporterOptions.TryToUseDefs);

			Assert.Same(type, importer.ImportOrNull(methodRef)!.Class);
			Assert.Same(type, importer.ImportOrNull(fieldRef)!.Class);
		}

		[Fact]
		public void ReferencesToTheTargetModuleStayReferencesWithoutTheOption() {
			var module = CreateSourceModuleThatResolvesItself(out _, out _, out _);
			CreateReferencesToTheSourceModule(new Version(1, 2, 3, 4), out var typeRef, out var methodRef, out var fieldRef);

			var importer = new Importer(module);

			var imported = Assert.IsAssignableFrom<TypeRef>(importer.ImportOrNull(typeRef));
			// The reference is to the module itself
			Assert.Same(module, imported.ResolutionScope);
			Assert.IsAssignableFrom<TypeRef>(Assert.IsAssignableFrom<MemberRef>(importer.ImportOrNull((IMethod)methodRef)).Class);
			Assert.IsAssignableFrom<TypeRef>(Assert.IsAssignableFrom<MemberRef>(importer.ImportOrNull((IField)fieldRef)).Class);
		}

		// Each option is for one kind of definition. Q11 of the test notes: a MemberRef wasn't replaced at
		// all, and the reflection imports only replaced a member if TryToUseTypeDefs was set too.
		[Theory]
		[InlineData(ImporterOptions.TryToUseTypeDefs, true, false, false)]
		[InlineData(ImporterOptions.TryToUseMethodDefs, false, true, false)]
		[InlineData(ImporterOptions.TryToUseFieldDefs, false, false, true)]
		public void OnlyTheKindOfDefOfTheOptionIsUsed(ImporterOptions options, bool typeDef, bool methodDef, bool fieldDef) {
			var module = CreateSourceModuleThatResolvesItself(out _, out _, out _);
			CreateReferencesToTheSourceModule(new Version(1, 2, 3, 4), out var typeRef, out var methodRef, out var fieldRef);

			var importer = new Importer(module, options);

			Assert.Equal(typeDef, importer.ImportOrNull(typeRef) is TypeDef);
			Assert.Equal(methodDef, importer.ImportOrNull((IMethod)methodRef) is MethodDef);
			Assert.Equal(fieldDef, importer.ImportOrNull((IField)fieldRef) is FieldDef);
		}

		[Fact]
		public void ReferenceToAMemberThatTheTypeDoesNotHaveStaysAReference() {
			var module = CreateSourceModuleThatResolvesItself(out var type, out _, out _);
			CreateReferencesToTheSourceModule(new Version(1, 2, 3, 4), out var typeRef, out _, out _);
			var otherSignature = new MemberRefUser(typeRef.Module, "Method", MethodSig.CreateStatic(module.CorLibTypes.Void), typeRef);
			var otherName = new MemberRefUser(typeRef.Module, "OtherField", new FieldSig(module.CorLibTypes.Int64), typeRef);

			var importer = new Importer(module, ImporterOptions.TryToUseDefs);

			var importedMethod = Assert.IsAssignableFrom<MemberRef>(importer.ImportOrNull((IMethod)otherSignature));
			Assert.Same(type, importedMethod.Class);
			Assert.IsAssignableFrom<MemberRef>(importer.ImportOrNull((IField)otherName));
		}

		// A reference through a derived type to a method of its base type isn't replaced by the method of
		// the base type: it would be a reference with another class
		[Fact]
		public void ReferenceToAnInheritedMethodStaysAReference() {
			var module = CreateSourceModuleThatResolvesItself(out var type, out _, out _);
			var derived = new TypeDefUser("Ns", "Derived", type);
			module.Types.Add(derived);
			CreateReferencesToTheSourceModule(new Version(1, 2, 3, 4), out var typeRef, out var methodRef, out _);
			var derivedRef = new TypeRefUser(typeRef.Module, "Ns", "Derived", typeRef.ResolutionScope);
			var throughDerived = new MemberRefUser(typeRef.Module, "Method", methodRef.MethodSig, derivedRef);

			var imported = new Importer(module, ImporterOptions.TryToUseDefs).ImportOrNull((IMethod)throughDerived);

			Assert.Same(derived, Assert.IsAssignableFrom<MemberRef>(imported).Class);
		}

		// A member of an instantiated generic type must stay a MemberRef: a definition has no generic arguments
		[Fact]
		public void ReferenceToAMemberOfAGenericInstanceStaysAReference() {
			var module = CreateSourceModuleThatResolvesItself(out var type, out _, out _);
			type.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			type.Name = "Source`1";
			var user = CreateModule("User");
			var typeRef = new TypeRefUser(user, "Ns", "Source`1", new AssemblyRefUser("Source", new Version(1, 2, 3, 4)));
			var instance = new TypeSpecUser(new GenericInstSig(new ClassSig(typeRef), user.CorLibTypes.Int32));
			var methodRef = new MemberRefUser(user, "Method", MethodSig.CreateStatic(user.CorLibTypes.Int32, user.CorLibTypes.String), instance);

			var imported = Assert.IsAssignableFrom<MemberRef>(new Importer(module, ImporterOptions.TryToUseDefs).ImportOrNull((IMethod)methodRef));

			var importedInstance = Assert.IsType<GenericInstSig>(Assert.IsAssignableFrom<TypeSpec>(imported.Class).TypeSig);
			Assert.Same(type, importedInstance.GenericType!.TypeDefOrRef);
		}

		// An instantiated generic method: the MethodSpec is of the MethodDef
		[Fact]
		public void MethodSpecOfAReferenceToTheTargetModuleIsAMethodSpecOfTheDef() {
			var module = CreateSourceModuleThatResolvesItself(out var type, out _, out _);
			var genericMethod = new MethodDefUser("Generic", MethodSig.CreateStaticGeneric(1, module.CorLibTypes.Void, new GenericMVar(0)));
			genericMethod.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			type.Methods.Add(genericMethod);
			CreateReferencesToTheSourceModule(new Version(1, 2, 3, 4), out var typeRef, out _, out _);
			var user = typeRef.Module!;
			var reference = new MemberRefUser(user, "Generic", MethodSig.CreateStaticGeneric(1, user.CorLibTypes.Void, new GenericMVar(0)), typeRef);
			var methodSpec = new MethodSpecUser(reference, new GenericInstMethodSig(user.CorLibTypes.Int32));

			var imported = Assert.IsAssignableFrom<MethodSpec>(new Importer(module, ImporterOptions.TryToUseDefs).ImportOrNull((IMethod)methodSpec));

			Assert.Same(genericMethod, imported.Method);
			Assert.Equal("System.Int32", Assert.Single(imported.GenericInstMethodSig!.GenericArguments)!.FullName);
		}

		// A global method is referenced through a ModuleRef with the name of its module
		[Fact]
		public void ReferenceToAGlobalMethodOfTheTargetModuleBecomesItsDef() {
			var module = CreateSourceModuleThatResolvesItself(out _, out _, out _);
			var globalMethod = new MethodDefUser("Global", MethodSig.CreateStatic(module.CorLibTypes.Void), dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static);
			module.GlobalType!.Methods.Add(globalMethod);
			// Another module of the same assembly
			var otherModule = new ModuleDefUser("Other.netmodule");
			module.Assembly!.Modules.Add(otherModule);
			var reference = new MemberRefUser(otherModule, "Global", MethodSig.CreateStatic(otherModule.CorLibTypes.Void), new ModuleRefUser(otherModule, "Source.dll"));
			var toAnotherModule = new MemberRefUser(otherModule, "Global", MethodSig.CreateStatic(otherModule.CorLibTypes.Void), new ModuleRefUser(otherModule, "Third.netmodule"));

			var importer = new Importer(module, ImporterOptions.TryToUseDefs);

			Assert.Same(globalMethod, importer.ImportOrNull((IMethod)reference));
			var imported = Assert.IsAssignableFrom<MemberRef>(importer.ImportOrNull((IMethod)toAnotherModule));
			Assert.Equal("Third.netmodule", Assert.IsAssignableFrom<ModuleRef>(imported.Class).Name);
		}

		// The target module can't reference another version of its own assembly: an assembly with its name is
		// the assembly itself
		[Fact]
		public void ReferenceToAnotherVersionOfTheTargetAssemblyIsAReferenceToTheTargetAssembly() {
			var module = CreateSourceModuleThatResolvesItself(out var type, out _, out _);
			CreateReferencesToTheSourceModule(new Version(9, 9, 9, 9), out var typeRef, out _, out _);

			Assert.Same(type, new Importer(module, ImporterOptions.TryToUseDefs).ImportOrNull(typeRef));
		}

		// The resolver is needed: the definition is found by resolving the reference
		[Fact]
		public void ReferenceStaysAReferenceIfItCanNotBeResolved() {
			var module = CreateSourceModule(out _, out _, out _);
			CreateReferencesToTheSourceModule(new Version(1, 2, 3, 4), out var typeRef, out _, out _);

			Assert.IsAssignableFrom<TypeRef>(new Importer(module, ImporterOptions.TryToUseDefs).ImportOrNull(typeRef));
		}

		[Fact]
		public void ReferenceToANestedTypeOfTheTargetModuleBecomesItsDef() {
			var module = CreateSourceModuleThatResolvesItself(out var type, out _, out _);
			var nested = new TypeDefUser("", "Nested", module.CorLibTypes.Object.TypeDefOrRef) { Attributes = dnlib.DotNet.TypeAttributes.NestedPublic };
			type.NestedTypes.Add(nested);
			CreateReferencesToTheSourceModule(new Version(1, 2, 3, 4), out var typeRef, out _, out _);
			var nestedRef = new TypeRefUser(typeRef.Module, "", "Nested", typeRef);

			Assert.Same(nested, new Importer(module, ImporterOptions.TryToUseDefs).ImportOrNull(nestedRef));
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
