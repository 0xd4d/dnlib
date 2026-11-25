// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Resolving {
	/// <summary>
	/// A type can move to another assembly: the old assembly keeps an entry in its ExportedType table that says
	/// where the type is now (a "type forwarder"), and the new assembly can forward it again. A reference to the
	/// type in the old assembly must resolve to the type's definition. The tests create the assemblies in memory.
	/// </summary>
	public class ForwarderResolutionTests {
		// Finds the assemblies that a test has created, by their name
		sealed class CreatedAssemblyResolver : IAssemblyResolver {
			public readonly Dictionary<string, AssemblyDef> Assemblies = new Dictionary<string, AssemblyDef>();

			public AssemblyDef? Resolve(IAssembly? assembly, ModuleDef? sourceModule) =>
				assembly is not null && Assemblies.TryGetValue(assembly.Name, out var found) ? found : null;
		}

		readonly CreatedAssemblyResolver assemblyResolver = new CreatedAssemblyResolver();
		readonly ModuleContext context;

		public ForwarderResolutionTests() {
			context = new ModuleContext(assemblyResolver);
			context.Resolver = new Resolver(assemblyResolver);
		}

		ModuleDefUser CreateAssembly(string name) {
			var module = new ModuleDefUser(name + ".dll") { Context = context };
			var assembly = new AssemblyDefUser(name, new Version(1, 0, 0, 0));
			assembly.Modules.Add(module);
			assemblyResolver.Assemblies.Add(name, assembly);
			return module;
		}

		static TypeDefUser AddType(ModuleDef module, string name) {
			var type = new TypeDefUser("Ns", name, module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(type);
			return type;
		}

		static ExportedTypeUser AddForwarder(ModuleDef module, string typeName, string targetAssembly) {
			var forwarder = new ExportedTypeUser(module, 0, "Ns", typeName, TypeAttributes.Public | TypeAttributes.Forwarder, new AssemblyRefUser(targetAssembly, new Version(1, 0, 0, 0)));
			module.ExportedTypes.Add(forwarder);
			return forwarder;
		}

		// A reference from a new assembly "User" to a type in another assembly
		TypeRefUser CreateReference(string typeName, string assemblyName) {
			var user = assemblyResolver.Assemblies.TryGetValue("User", out var existing) ? existing.ManifestModule! : CreateAssembly("User");
			return new TypeRefUser(user, "Ns", typeName, new AssemblyRefUser(assemblyName, new Version(1, 0, 0, 0)));
		}

		[Fact]
		public void ReferenceToAForwardedTypeResolvesToItsDefinition() {
			var moved = AddType(CreateAssembly("New"), "Moved");
			var forwarder = AddForwarder(CreateAssembly("Old"), "Moved", "New");

			Assert.Same(moved, CreateReference("Moved", "Old").Resolve());
			Assert.Same(moved, forwarder.Resolve());
			// The reference to the new assembly needs no forwarder
			Assert.Same(moved, CreateReference("Moved", "New").Resolve());
		}

		[Fact]
		public void TypeThatWasForwardedTwiceResolves() {
			var moved = AddType(CreateAssembly("Newest"), "Moved");
			AddForwarder(CreateAssembly("Newer"), "Moved", "Newest");
			AddForwarder(CreateAssembly("Old"), "Moved", "Newer");

			Assert.Same(moved, CreateReference("Moved", "Old").Resolve());
			Assert.Same(moved, CreateReference("Moved", "Newer").Resolve());
		}

		// The definition in an assembly is used even if the assembly also has a forwarder for the name
		[Fact]
		public void DefinitionIsFoundBeforeAForwarder() {
			var old = CreateAssembly("Old");
			var definition = AddType(old, "Moved");
			AddForwarder(old, "Moved", "New");
			AddType(CreateAssembly("New"), "Moved");

			Assert.Same(definition, CreateReference("Moved", "Old").Resolve());
		}

		[Fact]
		public void NestedTypeOfAForwardedTypeResolves() {
			var moved = AddType(CreateAssembly("New"), "Moved");
			var nested = new TypeDefUser("", "Nested", moved.Module!.CorLibTypes.Object.TypeDefOrRef) { Attributes = TypeAttributes.NestedPublic };
			moved.NestedTypes.Add(nested);
			var old = CreateAssembly("Old");
			var forwarder = AddForwarder(old, "Moved", "New");
			old.ExportedTypes.Add(new ExportedTypeUser(old, 0, "", "Nested", TypeAttributes.NestedPublic, forwarder));

			var reference = new TypeRefUser(CreateAssembly("User"), "", "Nested", CreateReference("Moved", "Old"));

			Assert.Same(nested, reference.Resolve());
		}

		[Fact]
		public void ForwarderToAnAssemblyThatIsNotFoundDoesNotResolve() {
			var forwarder = AddForwarder(CreateAssembly("Old"), "Moved", "Missing");

			Assert.Null(CreateReference("Moved", "Old").Resolve());
			Assert.Null(forwarder.Resolve());
		}

		[Fact]
		public void ForwarderToAnAssemblyThatDoesNotHaveTheTypeDoesNotResolve() {
			AddType(CreateAssembly("New"), "AnotherType");
			AddForwarder(CreateAssembly("Old"), "Moved", "New");

			Assert.Null(CreateReference("Moved", "Old").Resolve());
		}

		// Two assemblies that forward the type to each other: the resolver gives up
		[Fact]
		public void ForwardersThatFormACycleDoNotResolveAndDoNotHang() {
			AddForwarder(CreateAssembly("First"), "Moved", "Second");
			AddForwarder(CreateAssembly("Second"), "Moved", "First");
			AddForwarder(CreateAssembly("Self"), "Moved", "Self");

			Assert.Null(CreateReference("Moved", "First").Resolve());
			Assert.Null(CreateReference("Moved", "Self").Resolve());
		}

		// A global method or field of another module of the assembly is referenced through a ModuleRef
		[Fact]
		public void MemberReferenceInAModuleRefResolvesToTheGlobalMemberOfThatModule() {
			var manifestModule = CreateAssembly("Asm");
			var otherModule = new ModuleDefUser("other.netmodule") { Context = context };
			manifestModule.Assembly!.Modules.Add(otherModule);
			MethodDefUser AddGlobalMethod(ModuleDef module) {
				var method = new MethodDefUser("Global", MethodSig.CreateStatic(module.CorLibTypes.Void), MethodAttributes.Static | MethodAttributes.Public);
				module.GlobalType!.Methods.Add(method);
				return method;
			}
			var ownMethod = AddGlobalMethod(manifestModule);
			var otherMethod = AddGlobalMethod(otherModule);
			var signature = MethodSig.CreateStatic(manifestModule.CorLibTypes.Void);

			var toOwnModule = new MemberRefUser(manifestModule, "Global", signature, new ModuleRefUser(manifestModule, "Asm.dll"));
			var toOtherModule = new MemberRefUser(manifestModule, "Global", signature, new ModuleRefUser(manifestModule, "other.netmodule"));
			var toMissingModule = new MemberRefUser(manifestModule, "Global", signature, new ModuleRefUser(manifestModule, "missing.netmodule"));
			// A call of a method with a variable number of arguments references the method itself
			var toMethod = new MemberRefUser(manifestModule, "Global", signature, ownMethod);

			Assert.Same(ownMethod, toOwnModule.ResolveMethod());
			Assert.Same(otherMethod, toOtherModule.ResolveMethod());
			Assert.Null(toMissingModule.ResolveMethod());
			Assert.Same(ownMethod, toMethod.ResolveMethod());
		}

		[Fact]
		public void TypeReferenceInAModuleRefResolvesToTheTypeOfThatModule() {
			var manifestModule = CreateAssembly("Asm");
			var type = AddType(manifestModule, "InManifestModule");

			Assert.Same(type, new TypeRefUser(manifestModule, "Ns", "InManifestModule", new ModuleRefUser(manifestModule, "Asm.dll")).Resolve());
			Assert.Null(new TypeRefUser(manifestModule, "Ns", "InManifestModule", new ModuleRefUser(manifestModule, "missing.netmodule")).Resolve());
		}
	}
}
