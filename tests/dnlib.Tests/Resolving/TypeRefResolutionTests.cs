// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Resolving {
	/// <summary>
	/// Finding the type that a <see cref="TypeRef"/> refers to, in assemblies that are created with the API
	/// (nothing is loaded from disk)
	/// </summary>
	public class TypeRefResolutionTests {
		// An assembly with two modules. Each module has one type.
		static AssemblyDefUser CreateTwoModuleAssembly(out ModuleDefUser manifestModule, out ModuleDefUser otherModule) {
			var assembly = new AssemblyDefUser("Asm", new Version(1, 0, 0, 0));
			manifestModule = new ModuleDefUser("Asm.dll");
			otherModule = new ModuleDefUser("other.netmodule");
			assembly.Modules.Add(manifestModule);
			assembly.Modules.Add(otherModule);
			manifestModule.Types.Add(new TypeDefUser("Ns", "InManifestModule", manifestModule.CorLibTypes.Object.TypeDefOrRef));
			otherModule.Types.Add(new TypeDefUser("Ns", "InOtherModule", otherModule.CorLibTypes.Object.TypeDefOrRef));
			// Nothing is loaded from disk: every assembly is unknown except the ones the tests create
			var context = new ModuleContext(new NullAssemblyResolver());
			context.Resolver = new Resolver(context.AssemblyResolver);
			manifestModule.Context = context;
			otherModule.Context = context;
			return assembly;
		}

		sealed class NullAssemblyResolver : IAssemblyResolver {
			public AssemblyDef? Resolve(IAssembly? assembly, ModuleDef? sourceModule) => null;
		}

		[Fact]
		public void TypeRefToTheModuleItselfResolves() {
			CreateTwoModuleAssembly(out var manifestModule, out _);
			var typeRef = new TypeRefUser(manifestModule, "Ns", "InManifestModule", manifestModule);

			Assert.Same(manifestModule.Types[1], typeRef.Resolve());
		}

		[Fact]
		public void TypeRefToAnotherModuleOfTheAssemblyResolves() {
			CreateTwoModuleAssembly(out var manifestModule, out var otherModule);
			var typeRef = new TypeRefUser(manifestModule, "Ns", "InOtherModule", new ModuleRefUser(manifestModule, "other.netmodule"));

			Assert.Same(otherModule.Types[1], typeRef.Resolve());
		}

		// Fixed in 218242e8: the CLR looks for a TypeRef without a scope in all modules of the assembly. Only the
		// TypeRef's own module was searched.
		[Fact]
		public void TypeRefWithoutAScopeIsFoundInAnyModuleOfTheAssembly() {
			CreateTwoModuleAssembly(out var manifestModule, out var otherModule);
			var inOwnModule = new TypeRefUser(otherModule, "Ns", "InOtherModule");
			var inAnotherModule = new TypeRefUser(otherModule, "Ns", "InManifestModule");

			Assert.Same(otherModule.Types[1], inOwnModule.Resolve());
			Assert.Same(manifestModule.Types[1], inAnotherModule.Resolve());
		}

		[Fact]
		public void TypeRefToATypeThatDoesNotExistDoesNotResolve() {
			CreateTwoModuleAssembly(out var manifestModule, out _);
			var typeRef = new TypeRefUser(manifestModule, "Ns", "NoSuchType", manifestModule);

			Assert.Null(typeRef.Resolve());
			Assert.Throws<TypeResolveException>(() => typeRef.ResolveOrThrow());
		}

		[Fact]
		public void TypeRefToAnUnknownAssemblyDoesNotResolve() {
			CreateTwoModuleAssembly(out var manifestModule, out _);
			var typeRef = new TypeRefUser(manifestModule, "Ns", "Type", new AssemblyRefUser("Unknown"));

			Assert.Null(typeRef.Resolve());
		}

		[Fact]
		public void NestedTypeRefResolvesThroughItsDeclaringType() {
			CreateTwoModuleAssembly(out var manifestModule, out _);
			var nested = new TypeDefUser("Nested");
			manifestModule.Types[1].NestedTypes.Add(nested);
			var declaringTypeRef = new TypeRefUser(manifestModule, "Ns", "InManifestModule", manifestModule);
			var nestedRef = new TypeRefUser(manifestModule, "", "Nested", declaringTypeRef);

			Assert.Same(nested, nestedRef.Resolve());
		}
	}
}
