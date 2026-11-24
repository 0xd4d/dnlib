// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Signatures {
	/// <summary>
	/// The extension methods of <see cref="TypeSig"/>: <c>ToClassSig()</c>, <c>ToPtrSig()</c> and so on return
	/// the signature as that kind of signature if it is one (without its custom modifiers and without "pinned"),
	/// and the <c>Get...()</c> methods read a property of a signature that can be null.
	/// </summary>
	public class TypeSigExtensionTests {
		readonly ModuleDefUser module = new ModuleDefUser("Test.dll");
		readonly TypeRefUser classRef;
		readonly TypeRefUser structRef;

		public TypeSigExtensionTests() {
			classRef = new TypeRefUser(module, "Ns", "Class", module);
			structRef = new TypeRefUser(module, "Ns", "Struct", module);
		}

		// A signature of every kind, and the names of the To...() methods that return it
		Dictionary<TypeSig, string[]> CreateSignatures() {
			var int32 = module.CorLibTypes.Int32;
			return new Dictionary<TypeSig, string[]> {
				{ int32, new[] { "ToTypeDefOrRefSig" } },
				{ new ClassSig(classRef), new[] { "ToTypeDefOrRefSig", "ToClassOrValueTypeSig", "ToClassSig" } },
				{ new ValueTypeSig(structRef), new[] { "ToTypeDefOrRefSig", "ToClassOrValueTypeSig", "ToValueTypeSig" } },
				{ new GenericVar(0), new[] { "ToGenericSig", "ToGenericVar" } },
				{ new GenericMVar(1), new[] { "ToGenericSig", "ToGenericMVar" } },
				{ new GenericInstSig(new ClassSig(classRef), int32), new[] { "ToGenericInstSig" } },
				{ new PtrSig(int32), new[] { "ToPtrSig" } },
				{ new ByRefSig(int32), new[] { "ToByRefSig" } },
				{ new ArraySig(int32, 2), new[] { "ToArraySig" } },
				{ new SZArraySig(int32), new[] { "ToSZArraySig" } },
			};
		}

		static readonly string[] conversions = typeof(dnlib.DotNet.Extensions).GetMethods()
			.Where(a => a.Name.StartsWith("To") && a.Name.EndsWith("Sig") || a.Name == "ToGenericVar" || a.Name == "ToGenericMVar")
			.Where(a => a.GetParameters().Length == 1 && a.GetParameters()[0].ParameterType == typeof(TypeSig))
			.Select(a => a.Name).Distinct().OrderBy(a => a, StringComparer.Ordinal).ToArray();

		static TypeSig? Convert(string conversion, TypeSig? signature) =>
			(TypeSig?)typeof(dnlib.DotNet.Extensions).GetMethod(conversion, new[] { typeof(TypeSig) })!.Invoke(null, new object?[] { signature });

		[Fact]
		public void EveryConversionOnlyReturnsASignatureOfItsKind() {
			Assert.Equal(12, conversions.Length);
			var modifier = new TypeRefUser(module, "System.Runtime.CompilerServices", "IsConst", module);

			foreach (var pair in CreateSignatures()) {
				var signature = pair.Key;
				// The signature with a modifier around it, and pinned
				var wrapped = new PinnedSig(new CModOptSig(modifier, new CModReqdSig(modifier, signature)));
				foreach (var conversion in conversions) {
					var expected = pair.Value.Contains(conversion) ? signature : null;
					Assert.True(expected == Convert(conversion, signature), $"{conversion}({signature})");
					Assert.True(expected == Convert(conversion, wrapped), $"{conversion}({wrapped})");
				}
			}
			foreach (var conversion in conversions)
				Assert.Null(Convert(conversion, null));
		}

		[Fact]
		public void PinnedAndModifiersAreRemoved() {
			var modifier = new TypeRefUser(module, "System.Runtime.CompilerServices", "IsConst", module);
			var int32 = module.CorLibTypes.Int32;
			var modified = new CModOptSig(modifier, int32);
			var pinned = new PinnedSig(modified);

			Assert.Same(modified, pinned.RemovePinned());
			Assert.Same(int32, pinned.RemovePinnedAndModifiers());
			Assert.Same(int32, modified.RemoveModifiers());
			Assert.Same(int32, int32.RemovePinned());
			// A pointer to a modified type is still a pointer to a modified type: only the outside is removed
			var pointer = new PtrSig(modified);
			Assert.Same(pointer, pointer.RemovePinnedAndModifiers());
			Assert.Null(((TypeSig?)null).RemovePinned());
			Assert.Null(((TypeSig?)null).RemovePinnedAndModifiers());
		}

		[Fact]
		public void TypeOfAClassOrStructSignature() {
			var typeDef = new TypeDefUser("Ns", "Def");
			var typeSpec = new TypeSpecUser(new SZArraySig(module.CorLibTypes.Int32));
			var pinnedClass = new PinnedSig(new ClassSig(classRef));

			Assert.Same(classRef, pinnedClass.TryGetTypeDefOrRef());
			Assert.Same(classRef, pinnedClass.TryGetTypeRef());
			Assert.Null(pinnedClass.TryGetTypeDef());
			Assert.Null(pinnedClass.TryGetTypeSpec());
			Assert.Same(typeDef, new ClassSig(typeDef).TryGetTypeDef());
			Assert.Null(new ClassSig(typeDef).TryGetTypeRef());
			Assert.Same(typeSpec, new ClassSig(typeSpec).TryGetTypeSpec());
			// A signature that isn't a class or a struct has no such type
			var pointer = new PtrSig(new ClassSig(classRef));
			Assert.Null(pointer.TryGetTypeDefOrRef());
			Assert.Null(pointer.TryGetTypeRef());
			Assert.Null(((TypeSig?)null).TryGetTypeDefOrRef());
			Assert.Null(((TypeSig?)null).TryGetTypeDef());
			Assert.Null(((TypeSig?)null).TryGetTypeSpec());
		}

		[Fact]
		public void PropertiesOfASignatureThatCanBeNull() {
			TypeSig? none = null;
			var array = new SZArraySig(new ValueTypeSig(structRef));

			Assert.Same(array.Next, array.GetNext());
			Assert.Equal(ElementType.SZArray, array.GetElementType());
			Assert.Equal("Ns.Struct[]", array.GetFullName());
			Assert.Equal("Struct[]", array.GetName());
			Assert.Equal("Ns", array.GetNamespace());
			Assert.False(array.GetIsValueType());
			Assert.True(array.Next.GetIsValueType());
			Assert.True(module.CorLibTypes.Int32.GetIsPrimitive());
			Assert.False(array.GetIsPrimitive());

			Assert.Null(none.GetNext());
			Assert.Equal(ElementType.End, none.GetElementType());
			Assert.Equal("", none.GetFullName());
			Assert.Equal("", none.GetName());
			Assert.Equal("", none.GetNamespace());
			Assert.False(none.GetIsValueType());
			Assert.False(none.GetIsPrimitive());
		}
	}
}
