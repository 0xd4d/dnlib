// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// The parent of a <see cref="MemberRef"/> (<see cref="MemberRef.Class"/>) is a type, a method of the module
	/// (a call of a method with a variable number of arguments) or a ModuleRef (a global method or field of
	/// another module of the assembly). <see cref="MemberRef.DeclaringType"/> and the names must work for all.
	/// </summary>
	public class MemberRefParentTests {
		readonly ModuleDefUser module = new ModuleDefUser("Asm.dll");
		readonly ModuleDefUser otherModule = new ModuleDefUser("other.netmodule");
		readonly MethodSig signature;

		public MemberRefParentTests() {
			var assembly = new AssemblyDefUser("Asm", new Version(1, 0, 0, 0));
			assembly.Modules.Add(module);
			assembly.Modules.Add(otherModule);
			signature = MethodSig.CreateStatic(module.CorLibTypes.Void);
		}

		[Fact]
		public void ParentIsAType() {
			var type = new TypeRefUser(module, "Ns", "Type", module);
			var method = new MemberRefUser(module, "Method", signature, type);

			Assert.Same(type, method.DeclaringType);
			Assert.Equal("System.Void Ns.Type::Method()", method.FullName);
		}

		[Fact]
		public void ParentIsAMethod() {
			var type = new TypeDefUser("Ns", "Type", module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(type);
			var definition = new MethodDefUser("Method", signature, MethodAttributes.Static);
			type.Methods.Add(definition);
			var method = new MemberRefUser(module, "Method", signature, definition);

			Assert.Same(type, method.DeclaringType);
			Assert.Equal("System.Void Ns.Type::Method()", method.FullName);
			// A method that isn't in a type yet
			var inNoType = new MemberRefUser(module, "Method", signature, new MethodDefUser("Method", signature));
			Assert.Null(inNoType.DeclaringType);
		}

		// The declaring type is a reference to the global type of the module that the ModuleRef names
		[Theory]
		[InlineData("Asm.dll")]
		[InlineData("other.netmodule")]
		[InlineData("missing.netmodule")]
		public void ParentIsAModuleRef(string moduleName) {
			var moduleRef = new ModuleRefUser(module, moduleName);
			var method = new MemberRefUser(module, "Global", signature, moduleRef);
			var field = new MemberRefUser(module, "GlobalField", new FieldSig(module.CorLibTypes.Int32), moduleRef);

			var declaringType = Assert.IsAssignableFrom<TypeRef>(method.DeclaringType);

			Assert.Equal("<Module>", declaringType.Name);
			Assert.Same(moduleRef, declaringType.ResolutionScope);
			// The full name has the name of the module
			Assert.Equal($"System.Void [module:{moduleName}]<Module>::Global()", method.FullName);
			Assert.Equal($"System.Int32 [module:{moduleName}]<Module>::GlobalField", field.FullName);
			Assert.True(method.IsMethodRef && field.IsFieldRef);
		}

		// The global type of a module can have another name than <Module>: it's the module's first type
		[Fact]
		public void GlobalTypeOfTheReferencedModuleHasItsRealName() {
			otherModule.GlobalType!.Name = "Renamed";
			var method = new MemberRefUser(module, "Global", signature, new ModuleRefUser(module, "other.netmodule"));

			Assert.Equal("Renamed", method.DeclaringType!.Name);
			// The name in FullName is always <Module>
			Assert.Equal("System.Void [module:other.netmodule]<Module>::Global()", method.FullName);
		}

		[Fact]
		public void NoParent() {
			var method = new MemberRefUser(module, "Method", signature);

			Assert.Null(method.DeclaringType);
			Assert.Equal("System.Void Method()", method.FullName);
			Assert.Throws<NullValueException>(() => method.DeclaringTypeOrThrow);
		}
	}
}
