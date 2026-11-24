// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// The writer adds a TypeSpec row by writing its signature, which adds the types in the signature, which adds
	/// their custom attributes. If such an attribute uses the TypeSpec itself, the writer is asked for the
	/// TypeSpec again before it has finished the first time. The test input:
	/// <code>
	/// class Derived : Base&lt;int&gt;          // the base type is the TypeSpec
	/// // The reference to Base`1 has an attribute whose constructor is a method of Base&lt;int&gt;
	/// </code>
	/// </summary>
	public class TypeSpecReentryTests {
		static ModuleDefUser CreateModule(out TypeSpecUser typeSpec) {
			var module = new ModuleDefUser("Test.dll");
			new AssemblyDefUser("Test", new Version(1, 0, 0, 0)).Modules.Add(module);
			var baseRef = new TypeRefUser(module, "Ns", "Base`1", new AssemblyRefUser("Library", new Version(1, 0, 0, 0)));
			typeSpec = new TypeSpecUser(new GenericInstSig(new ClassSig(baseRef), module.CorLibTypes.Int32));
			var constructorOfTheTypeSpec = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void), typeSpec);
			baseRef.CustomAttributes.Add(new CustomAttribute(constructorOfTheTypeSpec));
			module.Types.Add(new TypeDefUser("Ns", "Derived", typeSpec));
			return module;
		}

		static void AssertIsWrittenCorrectly(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				// One TypeSpec row: the base type and the declaring type of the attribute's constructor
				Assert.Equal(1U, module.TablesStream.TypeSpecTable.Rows);
				var baseType = Assert.IsAssignableFrom<TypeSpec>(module.Find("Ns.Derived", isReflectionName: true)!.BaseType);
				Assert.Equal("Ns.Base`1<System.Int32>", baseType.FullName);
				var baseRef = module.GetTypeRefs().Single(a => a.Name == "Base`1");
				var attribute = Assert.Single(baseRef.CustomAttributes);
				Assert.Same(baseType, attribute.Constructor!.DeclaringType);
			}
		}

		// Fixed in 8b44feab: it was the error "TypeSpec has an infinite TypeSig loop"
		[Fact]
		public void TypeSpecThatACustomAttributeOfATypeInItsSignatureUsesIsWritten() {
			var module = CreateModule(out _);
			var stream = new MemoryStream();

			module.Write(stream);

			AssertIsWrittenCorrectly(stream.ToArray());
		}

		// The same with the writer that keeps the tokens of a module that was read from a file
		[Fact]
		public void TheSameWhenTheTokensArePreserved() {
			var stream = new MemoryStream();
			CreateModule(out _).Write(stream);

			using (var module = ModuleDefMD.Load(stream.ToArray())) {
				var options = new ModuleWriterOptions(module);
				options.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
				var writtenAgain = new MemoryStream();
				module.Write(writtenAgain, options);

				AssertIsWrittenCorrectly(writtenAgain.ToArray());
			}
		}

		// A TypeSpec whose signature has the TypeSpec itself is still an error: such a signature has no end
		[Fact]
		public void TypeSpecThatIsInItsOwnSignatureIsAnError() {
			var module = CreateModule(out _);
			var typeSpec = new TypeSpecUser();
			typeSpec.TypeSig = new SZArraySig(new ClassSig(typeSpec));
			module.Types.Add(new TypeDefUser("Ns", "Looping", typeSpec));

			var exception = Assert.Throws<ModuleWriterException>(() => module.Write(new MemoryStream()));

			Assert.Contains("can't have a TypeSpec", exception.Message);
		}
	}
}
