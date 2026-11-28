// dnlib: See LICENSE.txt for more info

using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// A <see cref="TypeSpec"/> is a row with a type signature. Its name, declaring type etc are the ones of the
	/// type in the signature (the scope type), eg. <c>List`1</c> for <c>List&lt;int&gt;</c>.
	/// </summary>
	public class TypeSpecTests {
		readonly ModuleDefUser module = new ModuleDefUser("Test.dll");
		readonly TypeDefUser outer;
		readonly TypeDefUser nested;

		public TypeSpecTests() {
			outer = new TypeDefUser("Ns", "Outer", module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(outer);
			nested = new TypeDefUser("", "Nested`1", module.CorLibTypes.Object.TypeDefOrRef);
			outer.NestedTypes.Add(nested);
		}

		[Fact]
		public void TheDeclaringTypeIsTheOneOfTheTypeInTheSignature() {
			var instance = new TypeSpecUser(new GenericInstSig(new ClassSig(nested), module.CorLibTypes.Int32));
			var modified = new TypeSpecUser(new CModOptSig(outer, new ClassSig(nested)));
			var nestedRef = new TypeRefUser(module, "", "Nested`1", new TypeRefUser(module, "Ns", "Outer", module));

			Assert.Same(outer, ((IMemberRef)instance).DeclaringType);
			Assert.Same(outer, ((IMemberRef)modified).DeclaringType);
			Assert.Equal("Ns.Outer", ((IMemberRef)new TypeSpecUser(new ClassSig(nestedRef))).DeclaringType!.FullName);
			Assert.Same(outer, ((IMemberRef)instance).DeclaringTypeOrThrow);
			// The type in a TypeSpec in a TypeSpec isn't used: an invalid file can have a TypeSpec that contains itself
			Assert.Null(((IMemberRef)new TypeSpecUser(new ClassSig(instance))).DeclaringType);
			// A signature that isn't a type (eg. an array) has no declaring type
			Assert.Null(((IMemberRef)new TypeSpecUser(new SZArraySig(new ClassSig(nested)))).DeclaringType);
			Assert.Null(((IMemberRef)new TypeSpecUser((TypeSig?)null)).DeclaringType);
		}

		// The name is IFullName.Name. Setting it changes the name of the type in the signature
		[Fact]
		public void TheNameIsTheNameOfTheTypeInTheSignature() {
			var typeSpec = new TypeSpecUser(new GenericInstSig(new ClassSig(nested), module.CorLibTypes.Int32));

			Assert.Equal("Nested`1", ((IFullName)typeSpec).Name);
			((IFullName)typeSpec).Name = "Renamed`1";
			Assert.Equal("Renamed`1", nested.Name);

			// Without a type in the signature, there is no name to set
			IFullName withoutType = new TypeSpecUser((TypeSig?)null);
			withoutType.Name = "Ignored";
			Assert.Equal(UTF8String.Empty, withoutType.Name);
		}

		[Fact]
		public void NamesAndTheGenericParameterCount() {
			var typeSpec = new TypeSpecUser(new GenericInstSig(new ClassSig(nested), module.CorLibTypes.Int32));

			Assert.Equal("Nested`1", typeSpec.ReflectionName);
			// A nested type has no namespace
			Assert.Equal("", ((IType)typeSpec).Namespace);
			Assert.Equal("Ns", ((IType)new TypeSpecUser(new ClassSig(outer))).Namespace);
			Assert.Equal(1, ((IGenericParameterProvider)typeSpec).NumberOfGenericParameters);
			Assert.Equal(0, ((IGenericParameterProvider)new TypeSpecUser(module.CorLibTypes.Int32)).NumberOfGenericParameters);
			Assert.Equal(0, ((IGenericParameterProvider)new TypeSpecUser((TypeSig?)null)).NumberOfGenericParameters);
		}

		// The bytes after the signature in the blob (only an odd file has them) are kept so they can be written again
		[Fact]
		public void ExtraDataIsKept() {
			var typeSpec = new TypeSpecUser(module.CorLibTypes.Int32);
			Assert.Null(typeSpec.ExtraData);

			typeSpec.ExtraData = new byte[] { 1, 2 };

			Assert.Equal(new byte[] { 1, 2 }, typeSpec.ExtraData);
		}
	}
}
