// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using Xunit;
using FileAttributes = dnlib.DotNet.FileAttributes;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// A reference to a type says where the type is. A <see cref="TypeRef"/> has a resolution scope: another
	/// assembly (AssemblyRef), another module of the same assembly (ModuleRef) or its own module. An
	/// <see cref="ExportedType"/> has an implementation: a file of its assembly (FileDef) or another assembly
	/// (AssemblyRef). <see cref="SigComparer"/> must see that two of them mean the same place.
	///
	/// The type in these tests is <c>Ns.Type</c> in <c>Lib2.netmodule</c>, a module of the assembly
	/// <c>Lib</c> whose main module is <c>Lib.dll</c>.
	/// </summary>
	public class TypeScopeComparisonTests {
		readonly ModuleDefUser libMain = new ModuleDefUser("Lib.dll");
		readonly ModuleDefUser libSecond = new ModuleDefUser("Lib2.netmodule");
		readonly ModuleDefUser app = CreateAssembly("App");
		readonly ModuleDefUser facade = CreateAssembly("Facade");

		public TypeScopeComparisonTests() {
			var lib = new AssemblyDefUser("Lib", new Version(1, 0, 0, 0));
			lib.Modules.Add(libMain);
			lib.Modules.Add(libSecond);
		}

		static ModuleDefUser CreateAssembly(string name) {
			var module = new ModuleDefUser(name + ".dll");
			new AssemblyDefUser(name, new Version(1, 0, 0, 0)).Modules.Add(module);
			return module;
		}

		static AssemblyRefUser AssemblyReference(string name) => new AssemblyRefUser(name, new Version(1, 0, 0, 0));

		// The references to Ns.Type
		TypeRef FromAnotherAssembly => new TypeRefUser(app, "Ns", "Type", AssemblyReference("Lib"));
		TypeRef FromTheMainModule => new TypeRefUser(libMain, "Ns", "Type", new ModuleRefUser(libMain, "Lib2.netmodule"));
		TypeRef FromItsOwnModule => new TypeRefUser(libSecond, "Ns", "Type", libSecond);
		ExportedType ExportedByTheMainModule => new ExportedTypeUser(libMain, 0, "Ns", "Type", TypeAttributes.Public, new FileDefUser("Lib2.netmodule", FileAttributes.ContainsMetadata, null));
		ExportedType ForwardedByAnotherAssembly => new ExportedTypeUser(facade, 0, "Ns", "Type", TypeAttributes.Public, AssemblyReference("Lib"));

		static void AssertEqual(bool expected, IType a, IType b) {
			Assert.Equal(expected, new SigComparer().Equals(a, b));
			Assert.Equal(expected, new SigComparer().Equals(b, a));
			if (expected)
				Assert.Equal(new SigComparer().GetHashCode(a), new SigComparer().GetHashCode(b));
			// The names are the same, so they're always equal if the scope isn't compared
			Assert.True(new SigComparer(SigComparerOptions.DontCompareTypeScope).Equals(a, b));
		}

		[Fact]
		public void TypeRefsWithDifferentKindsOfScopesAreEqual() {
			AssertEqual(true, FromAnotherAssembly, FromTheMainModule);
			AssertEqual(true, FromAnotherAssembly, FromItsOwnModule);
			AssertEqual(true, FromTheMainModule, FromItsOwnModule);
			AssertEqual(true, FromTheMainModule, FromTheMainModule);
			AssertEqual(true, FromItsOwnModule, FromItsOwnModule);
		}

		[Fact]
		public void TypeRefsToAnotherModuleOrAssemblyAreNotEqual() {
			var toAnotherModule = new TypeRefUser(libMain, "Ns", "Type", new ModuleRefUser(libMain, "Lib3.netmodule"));
			var toAModuleOfAnotherAssembly = new TypeRefUser(app, "Ns", "Type", new ModuleRefUser(app, "Lib2.netmodule"));
			var toAnotherAssembly = new TypeRefUser(app, "Ns", "Type", AssemblyReference("Other"));

			AssertEqual(false, toAnotherModule, FromTheMainModule);
			AssertEqual(false, toAnotherModule, FromItsOwnModule);
			AssertEqual(false, toAModuleOfAnotherAssembly, FromAnotherAssembly);
			AssertEqual(false, toAModuleOfAnotherAssembly, FromItsOwnModule);
			AssertEqual(false, toAnotherAssembly, FromAnotherAssembly);
			AssertEqual(false, toAnotherAssembly, FromTheMainModule);
			AssertEqual(false, toAnotherAssembly, FromItsOwnModule);
		}

		[Fact]
		public void ExportedTypesWithDifferentKindsOfImplementationsAreEqual() {
			AssertEqual(true, ExportedByTheMainModule, ForwardedByAnotherAssembly);
			AssertEqual(true, ExportedByTheMainModule, ExportedByTheMainModule);
			AssertEqual(true, ForwardedByAnotherAssembly, ForwardedByAnotherAssembly);
		}

		[Fact]
		public void ExportedTypesInAnotherFileOrAssemblyAreNotEqual() {
			var inAnotherFile = new ExportedTypeUser(libMain, 0, "Ns", "Type", TypeAttributes.Public, new FileDefUser("Lib3.netmodule", FileAttributes.ContainsMetadata, null));
			var inAFileOfAnotherAssembly = new ExportedTypeUser(app, 0, "Ns", "Type", TypeAttributes.Public, new FileDefUser("Lib2.netmodule", FileAttributes.ContainsMetadata, null));
			var forwardedToAnotherAssembly = new ExportedTypeUser(facade, 0, "Ns", "Type", TypeAttributes.Public, AssemblyReference("Other"));

			AssertEqual(false, inAnotherFile, ExportedByTheMainModule);
			AssertEqual(false, inAFileOfAnotherAssembly, ForwardedByAnotherAssembly);
			AssertEqual(false, forwardedToAnotherAssembly, ForwardedByAnotherAssembly);
			AssertEqual(false, forwardedToAnotherAssembly, ExportedByTheMainModule);
		}

		[Fact]
		public void TypeRefsAndExportedTypesAreEqual() {
			foreach (var typeRef in new[] { FromAnotherAssembly, FromTheMainModule, FromItsOwnModule }) {
				AssertEqual(true, typeRef, ExportedByTheMainModule);
				AssertEqual(true, typeRef, ForwardedByAnotherAssembly);
			}
		}

		[Fact]
		public void TypeRefAndExportedTypeInAnotherPlaceAreNotEqual() {
			var forwardedToAnotherAssembly = new ExportedTypeUser(facade, 0, "Ns", "Type", TypeAttributes.Public, AssemblyReference("Other"));
			var inAnotherFile = new ExportedTypeUser(libMain, 0, "Ns", "Type", TypeAttributes.Public, new FileDefUser("Lib3.netmodule", FileAttributes.ContainsMetadata, null));

			foreach (var typeRef in new[] { FromAnotherAssembly, FromTheMainModule, FromItsOwnModule })
				AssertEqual(false, typeRef, forwardedToAnotherAssembly);
			AssertEqual(false, FromTheMainModule, inAnotherFile);
			AssertEqual(false, FromItsOwnModule, inAnotherFile);
		}

		// A nested type has its enclosing type as scope / implementation
		[Fact]
		public void NestedTypeRefAndNestedExportedTypeAreEqual() {
			var nestedRef = new TypeRefUser(app, "", "Nested", FromAnotherAssembly);
			var nestedInOtherRef = new TypeRefUser(app, "", "Nested", new TypeRefUser(app, "Ns", "OtherType", AssemblyReference("Lib")));
			var nestedExported = new ExportedTypeUser(libMain, 0, "", "Nested", TypeAttributes.NestedPublic, ExportedByTheMainModule);
			var nestedForwarded = new ExportedTypeUser(facade, 0, "", "Nested", TypeAttributes.NestedPublic, ForwardedByAnotherAssembly);
			var notNested = new ExportedTypeUser(facade, 0, "", "Nested", TypeAttributes.Public, AssemblyReference("Lib"));

			AssertEqual(true, nestedRef, nestedExported);
			AssertEqual(true, nestedRef, nestedForwarded);
			AssertEqual(true, nestedExported, nestedForwarded);
			Assert.False(new SigComparer().Equals(nestedInOtherRef, nestedExported));
			Assert.False(new SigComparer().Equals(nestedRef, notNested));
			Assert.False(new SigComparer().Equals(notNested, nestedExported));
		}

		// References to another assembly that forwards the type (a facade) are the same type as references to the
		// type's assembly if both resolve to the same type
		[Fact]
		public void ReferencesThatResolveToTheSameTypeAreEqual() {
			var type = new TypeDefUser("Ns", "Type", libSecond.CorLibTypes.Object.TypeDefOrRef) { Attributes = TypeAttributes.Public };
			libSecond.Types.Add(type);
			libMain.ExportedTypes.Add(ExportedByTheMainModule);
			facade.ExportedTypes.Add(ForwardedByAnotherAssembly);
			var resolver = new AssemblyResolver { UseGAC = false };
			resolver.AddToCache(libMain);
			resolver.AddToCache(facade);
			// The forwarder in the facade is resolved with the facade's context
			foreach (var module in new[] { app, facade, libMain, libSecond })
				module.Context = new ModuleContext(resolver);
			var throughTheFacade = new TypeRefUser(app, "Ns", "Type", AssemblyReference("Facade"));
			var exportedThroughTheFacade = new ExportedTypeUser(app, 0, "Ns", "Type", TypeAttributes.Public, AssemblyReference("Facade"));
			var exportedFromLib = new ExportedTypeUser(app, 0, "Ns", "Type", TypeAttributes.Public, AssemblyReference("Lib"));
			Assert.Same(type, throughTheFacade.Resolve());

			Assert.True(new SigComparer().Equals(throughTheFacade, FromAnotherAssembly));
			Assert.True(new SigComparer().Equals(exportedThroughTheFacade, exportedFromLib));
			Assert.True(new SigComparer().Equals(throughTheFacade, exportedFromLib));
		}
	}
}
