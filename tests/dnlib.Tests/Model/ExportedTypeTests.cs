// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using Xunit;
using FileAttributes = dnlib.DotNet.FileAttributes;

namespace dnlib.Tests.Model {
	/// <summary>
	/// An <see cref="ExportedType"/> row says that a type with some name is not in this module: it's in
	/// another assembly (a "type forwarder", the type was moved) or in another file of the same assembly.
	/// The tests resolve such rows to the type, also through a second forwarder, and convert them to a
	/// <see cref="TypeRef"/>.
	///
	/// <c>Library</c> has the class <c>Ns.Type</c> with the nested class <c>Nested</c>. <c>Middle</c> forwards
	/// it to <c>Library</c>, and <c>Facade</c> forwards it to <c>Middle</c>.
	/// </summary>
	public class ExportedTypeTests {
		readonly ModuleDefUser library;
		readonly ModuleDefUser middle;
		readonly ModuleDefUser facade;
		readonly TypeDefUser type;
		readonly TypeDefUser nested;
		readonly ExportedTypeUser forwarder;
		readonly ExportedTypeUser nestedForwarder;

		public ExportedTypeTests() {
			var resolver = new AssemblyResolver();
			var context = new ModuleContext(resolver, new Resolver(resolver));
			library = CreateModule("Library", context, resolver);
			middle = CreateModule("Middle", context, resolver);
			facade = CreateModule("Facade", context, resolver);

			type = new TypeDefUser("Ns", "Type", library.CorLibTypes.Object.TypeDefOrRef) { Attributes = TypeAttributes.Public };
			library.Types.Add(type);
			nested = new TypeDefUser("", "Nested", library.CorLibTypes.Object.TypeDefOrRef) { Attributes = TypeAttributes.NestedPublic };
			type.NestedTypes.Add(nested);

			AddForwarders(middle, "Library", out _, out _);
			AddForwarders(facade, "Middle", out forwarder, out nestedForwarder);
		}

		static ModuleDefUser CreateModule(string assemblyName, ModuleContext context, AssemblyResolver resolver) {
			var module = new ModuleDefUser(assemblyName + ".dll") { Context = context };
			new AssemblyDefUser(assemblyName, new Version(1, 0, 0, 0)).Modules.Add(module);
			resolver.AddToCache(module);
			return module;
		}

		// Adds forwarders of Ns.Type and Ns.Type/Nested to another assembly
		static void AddForwarders(ModuleDef module, string targetAssembly, out ExportedTypeUser forwarder, out ExportedTypeUser nestedForwarder) {
			var assembly = new AssemblyRefUser(targetAssembly, new Version(1, 0, 0, 0));
			forwarder = new ExportedTypeUser(module, 0, "Ns", "Type", TypeAttributes.Public | TypeAttributes.Forwarder, assembly);
			nestedForwarder = new ExportedTypeUser(module, 0, "", "Nested", TypeAttributes.NestedPublic, forwarder);
			module.ExportedTypes.Add(forwarder);
			module.ExportedTypes.Add(nestedForwarder);
		}

		[Fact]
		public void ForwarderHasTheNamesAndTheAssemblyOfItsTarget() {
			Assert.Equal("Ns.Type", forwarder.FullName);
			Assert.Equal("Ns.Type/Nested", nestedForwarder.FullName);
			Assert.Equal("Ns.Type+Nested", nestedForwarder.ReflectionFullName);
			Assert.Same(forwarder, nestedForwarder.DeclaringType);
			Assert.Null(forwarder.DeclaringType);
			Assert.True(nestedForwarder.IsNested);
			Assert.False(forwarder.IsNested);

			Assert.True(forwarder.IsForwarder);
			Assert.True(forwarder.MovedToAnotherAssembly);
			// A nested type has no forwarder flag, its outer type has it
			Assert.False(nestedForwarder.IsForwarder);
			Assert.True(nestedForwarder.MovedToAnotherAssembly);
			Assert.Equal("Middle", forwarder.DefinitionAssembly!.Name);
			Assert.Equal("Middle", nestedForwarder.DefinitionAssembly!.Name);
			Assert.Same(facade, forwarder.Module);
		}

		// Middle doesn't have the type either: its forwarder is followed
		[Fact]
		public void ForwarderResolvesToTheTypeThroughAnotherForwarder() {
			Assert.Same(type, forwarder.Resolve());
			Assert.Same(type, forwarder.ResolveOrThrow());
			Assert.Same(nested, nestedForwarder.Resolve());
			Assert.Same(type, forwarder.Resolve(facade));
		}

		[Fact]
		public void ForwarderToAnUnknownAssemblyOrTypeDoesNotResolve() {
			var toUnknownAssembly = new ExportedTypeUser(facade, 0, "Ns", "Type", TypeAttributes.Forwarder, new AssemblyRefUser("Unknown", new Version(1, 0, 0, 0)));
			var toUnknownType = new ExportedTypeUser(facade, 0, "Ns", "OtherType", TypeAttributes.Forwarder, new AssemblyRefUser("Library", new Version(1, 0, 0, 0)));
			var withoutAModule = new ExportedTypeUser(null, 0, "Ns", "Type", TypeAttributes.Forwarder, new AssemblyRefUser("Library", new Version(1, 0, 0, 0)));

			Assert.Null(toUnknownAssembly.Resolve());
			Assert.Null(toUnknownType.Resolve());
			Assert.Null(withoutAModule.Resolve());
			Assert.Throws<TypeResolveException>(() => toUnknownAssembly.ResolveOrThrow());
			Assert.Throws<TypeResolveException>(() => toUnknownType.ResolveOrThrow());
		}

		// Two forwarders that point at each other
		[Fact]
		public void ForwardersThatFormACycleDoNotResolve() {
			var first = CreateModule("First", facade.Context, (AssemblyResolver)facade.Context.AssemblyResolver);
			var second = CreateModule("Second", facade.Context, (AssemblyResolver)facade.Context.AssemblyResolver);
			AddForwarders(first, "Second", out var firstForwarder, out _);
			AddForwarders(second, "First", out _, out _);

			Assert.Null(firstForwarder.Resolve());
		}

		[Fact]
		public void ForwarderAsATypeRef() {
			var typeRef = nestedForwarder.ToTypeRef();

			Assert.Equal("Ns.Type/Nested", typeRef.FullName);
			Assert.Same(facade, typeRef.Module);
			var outerRef = Assert.IsAssignableFrom<TypeRef>(typeRef.ResolutionScope);
			Assert.Equal("Ns.Type", outerRef.FullName);
			Assert.Equal("Middle", Assert.IsAssignableFrom<AssemblyRef>(outerRef.ResolutionScope).Name);
		}

		// A type in another file of the same assembly: the TypeRef's scope is that module, if it's loaded
		[Fact]
		public void ExportedTypeInAnotherFileAsATypeRef() {
			var otherModule = new ModuleDefUser("Other.netmodule");
			facade.Assembly!.Modules.Add(otherModule);
			var inOtherFile = new ExportedTypeUser(facade, 0, "Ns", "InFile", TypeAttributes.Public, new FileDefUser("Other.netmodule", FileAttributes.ContainsMetadata, null));
			var inMissingFile = new ExportedTypeUser(facade, 0, "Ns", "InFile", TypeAttributes.Public, new FileDefUser("Missing.netmodule", FileAttributes.ContainsMetadata, null));
			var inItsOwnFile = new ExportedTypeUser(facade, 0, "Ns", "InFile", TypeAttributes.Public, new FileDefUser("Facade.dll", FileAttributes.ContainsMetadata, null));

			Assert.False(inOtherFile.MovedToAnotherAssembly);
			Assert.Same(otherModule, inOtherFile.ToTypeRef().ResolutionScope);
			Assert.Null(inMissingFile.ToTypeRef().ResolutionScope);
			Assert.Same(facade, inItsOwnFile.ToTypeRef().ResolutionScope);
		}
	}
}
