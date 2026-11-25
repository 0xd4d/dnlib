// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.CustomAttributes {
	/// <summary>
	/// A <c>typeof()</c> argument of a custom attribute is stored as the name of the type. The name has no
	/// assembly if the type is in the attribute's own assembly or in the core library: a reader looks in the
	/// own assembly first, like <c>Type.GetType()</c>. The tests read attribute bytes with a type name and check
	/// which assembly the returned type is a type of.
	/// </summary>
	public class AttributeTypeNameScopeTests {
		// Finds the core library that the test created, and nothing else
		sealed class CoreLibraryResolver : IAssemblyResolver {
			public AssemblyDef? CoreLibrary;

			public AssemblyDef? Resolve(IAssembly? assembly, ModuleDef? sourceModule) =>
				assembly is not null && CoreLibrary is not null && assembly.Name == CoreLibrary.Name ? CoreLibrary : null;
		}

		readonly CoreLibraryResolver resolver = new CoreLibraryResolver();
		readonly ModuleDefUser module;

		public AttributeTypeNameScopeTests() {
			// A core library with the two types that the tests need
			var coreLibraryModule = new ModuleDefUser("mscorlib.dll");
			var coreLibrary = new AssemblyDefUser("mscorlib", new Version(4, 0, 0, 0));
			coreLibrary.Modules.Add(coreLibraryModule);
			coreLibraryModule.Types.Add(new TypeDefUser("System", "String"));
			coreLibraryModule.Types.Add(new TypeDefUser("System", "Type"));
			resolver.CoreLibrary = coreLibrary;

			// The module with the attribute, and a type of its own
			module = new ModuleDefUser("Test.dll", null, new AssemblyRefUser(coreLibrary));
			new AssemblyDefUser("Test", new Version(1, 2, 3, 4)).Modules.Add(module);
			module.Context = new ModuleContext(resolver);
			module.Types.Add(new TypeDefUser("Ns", "Own", module.CorLibTypes.Object.TypeDefOrRef));
		}

		// Reads an attribute whose only argument is typeof(a type with this name), and returns the name of the
		// assembly that the type is in
		string ReadScopeOfTypeArgument(string typeName) {
			var systemType = new ClassSig(module.CorLibTypes.GetTypeRef("System", "Type"));
			var constructor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void, systemType),
				new TypeRefUser(module, "Ns", "SomeAttribute", module));
			// 01 00, the name with its length, 00 00 (no named arguments)
			var nameBytes = Encoding.UTF8.GetBytes(typeName);
			var blob = new List<byte> { 1, 0, (byte)nameBytes.Length };
			blob.AddRange(nameBytes);
			blob.AddRange(new byte[] { 0, 0 });

			var attribute = CustomAttributeReader.Read(module, blob.ToArray(), constructor);

			Assert.False(attribute.IsRawBlob);
			var type = Assert.IsAssignableFrom<TypeSig>(attribute.ConstructorArguments.Single().Value);
			Assert.Equal(typeName, type.FullName);
			return type.DefinitionAssembly!.Name;
		}

		[Fact]
		public void CoreLibraryTypeIsInTheCoreLibrary() =>
			Assert.Equal("mscorlib", ReadScopeOfTypeArgument("System.String"));

		[Fact]
		public void TypeOfTheModuleIsInItsAssembly() =>
			Assert.Equal("Test", ReadScopeOfTypeArgument("Ns.Own"));

		// A type that's nowhere must be in the attribute's assembly: only core library types can be somewhere else
		[Fact]
		public void TypeThatIsNotFoundIsInTheAttributesAssembly() =>
			Assert.Equal("Test", ReadScopeOfTypeArgument("No.Such"));

		[Fact]
		public void NameWithAnAssemblyIsInThatAssembly() {
			var blobName = "Other.Type, Other, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
			var systemType = new ClassSig(module.CorLibTypes.GetTypeRef("System", "Type"));
			var constructor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void, systemType),
				new TypeRefUser(module, "Ns", "SomeAttribute", module));
			var blob = new byte[] { 1, 0, (byte)blobName.Length }.Concat(Encoding.UTF8.GetBytes(blobName)).Concat(new byte[] { 0, 0 }).ToArray();

			var attribute = CustomAttributeReader.Read(module, blob, constructor);

			var type = Assert.IsAssignableFrom<TypeSig>(attribute.ConstructorArguments.Single().Value);
			Assert.Equal("Other", type.DefinitionAssembly!.Name);
		}

		// The reader can't know that it's a core library type if the core library isn't found
		[Fact]
		public void CoreLibraryTypeIsInTheAttributesAssemblyIfTheCoreLibraryIsNotFound() {
			resolver.CoreLibrary = null;

			Assert.Equal("Test", ReadScopeOfTypeArgument("System.String"));
		}

		// A module that isn't in an assembly (a .netmodule file that was loaded alone)
		[Fact]
		public void TypeOfAModuleWithoutAnAssemblyIsInTheCurrentAssembly() {
			module.Assembly!.Modules.Remove(module);

			Assert.Equal(AssemblyRef.CurrentAssembly.Name, ReadScopeOfTypeArgument("Ns.Own"));
			Assert.Equal("mscorlib", ReadScopeOfTypeArgument("System.String"));
		}
	}
}
