// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Signatures {
	/// <summary>
	/// A <see cref="TypeSig"/> has a property for every kind of signature (<c>IsPointer</c>, <c>IsSZArray</c>,
	/// ...): exactly the properties of its kind are true. A generic parameter in a signature (<c>!0</c>,
	/// <c>!!0</c>) only is a number: if the signature was read from a type or method, it also knows that owner.
	/// </summary>
	public class TypeSigKindTests {
		static readonly ModuleDefUser module = new ModuleDefUser("Test.dll");

		static readonly string[] kindProperties = {
			"IsTypeDefOrRef", "IsCorLibType", "IsClassSig", "IsValueTypeSig", "IsGenericParameter", "IsGenericTypeParameter", "IsGenericMethodParameter",
			"IsSentinel", "IsFunctionPointer", "IsGenericInstanceType", "IsPointer", "IsByRef", "IsSingleOrMultiDimensionalArray", "IsArray", "IsSZArray",
			"IsModifier", "IsRequiredModifier", "IsOptionalModifier", "IsPinned", "IsValueArray", "IsModuleSig",
		};

		static TypeSig Create(string kind) {
			var int32 = module.CorLibTypes.Int32;
			var typeRef = new TypeRefUser(module, "Ns", "Type", module.CorLibTypes.AssemblyRef);
			switch (kind) {
			case "core library type": return int32;
			case "class": return new ClassSig(typeRef);
			case "value type": return new ValueTypeSig(typeRef);
			case "type parameter": return new GenericVar(0);
			case "method parameter": return new GenericMVar(0);
			case "sentinel": return new SentinelSig();
			case "function pointer": return new FnPtrSig(MethodSig.CreateStatic(module.CorLibTypes.Void));
			case "generic instance": return new GenericInstSig(new ClassSig(typeRef), int32);
			case "pointer": return new PtrSig(int32);
			case "reference": return new ByRefSig(int32);
			case "array with bounds": return new ArraySig(int32, 2);
			case "array": return new SZArraySig(int32);
			case "required modifier": return new CModReqdSig(typeRef, int32);
			case "optional modifier": return new CModOptSig(typeRef, int32);
			case "pinned": return new PinnedSig(int32);
			case "value array": return new ValueArraySig(int32, 4);
			case "module": return new ModuleSig(1, int32);
			default: throw new ArgumentException(kind);
			}
		}

		[Theory]
		[InlineData("core library type", "IsTypeDefOrRef IsCorLibType")]
		[InlineData("class", "IsTypeDefOrRef IsClassSig")]
		[InlineData("value type", "IsTypeDefOrRef IsValueTypeSig")]
		[InlineData("type parameter", "IsGenericParameter IsGenericTypeParameter")]
		[InlineData("method parameter", "IsGenericParameter IsGenericMethodParameter")]
		[InlineData("sentinel", "IsSentinel")]
		[InlineData("function pointer", "IsFunctionPointer")]
		[InlineData("generic instance", "IsGenericInstanceType")]
		[InlineData("pointer", "IsPointer")]
		[InlineData("reference", "IsByRef")]
		[InlineData("array with bounds", "IsSingleOrMultiDimensionalArray IsArray")]
		[InlineData("array", "IsSingleOrMultiDimensionalArray IsSZArray")]
		[InlineData("required modifier", "IsModifier IsRequiredModifier")]
		[InlineData("optional modifier", "IsModifier IsOptionalModifier")]
		[InlineData("pinned", "IsPinned")]
		[InlineData("value array", "IsValueArray")]
		[InlineData("module", "IsModuleSig")]
		public void OnlyThePropertiesOfTheKindAreTrue(string kind, string expectedProperties) {
			var signature = Create(kind);

			var trueProperties = kindProperties.Where(name => (bool)typeof(TypeSig).GetProperty(name)!.GetValue(signature, null)!).ToArray();

			Assert.Equal(expectedProperties.Split(' ').OrderBy(a => a).ToArray(), trueProperties.OrderBy(a => a).ToArray());
		}

		// The list above has every property of TypeSig that says what kind of signature it is
		[Fact]
		public void EveryKindPropertyIsChecked() {
			var otherProperties = new[] { "IsValueType", "IsPrimitive" };
			var properties = typeof(TypeSig).GetProperties().Where(a => a.PropertyType == typeof(bool) && a.Name.StartsWith("Is", StringComparison.Ordinal)).Select(a => a.Name);

			Assert.Equal(kindProperties.OrderBy(a => a).ToArray(), properties.Except(otherProperties).OrderBy(a => a).ToArray());
		}

		[Fact]
		public void GenericParameterThatWasCreatedHasNoOwner() {
			var typeParameter = new GenericVar(2);
			var methodParameter = new GenericMVar(3);

			Assert.True(typeParameter.IsTypeVar);
			Assert.False(typeParameter.IsMethodVar);
			Assert.True(methodParameter.IsMethodVar);
			Assert.False(methodParameter.IsTypeVar);
			Assert.Equal(2U, typeParameter.Number);
			Assert.Equal(3U, methodParameter.Number);
			Assert.Equal("!2", typeParameter.FullName);
			Assert.Equal("!!3", methodParameter.FullName);
			foreach (var parameter in new GenericSig[] { typeParameter, methodParameter }) {
				Assert.False(parameter.HasOwner);
				Assert.False(parameter.HasOwnerType);
				Assert.False(parameter.HasOwnerMethod);
				Assert.Null(parameter.OwnerType);
				Assert.Null(parameter.OwnerMethod);
				Assert.Null(parameter.GenericParam);
			}
		}

		[Fact]
		public void GenericParameterOfAReadSignatureHasItsOwner() {
			const string source = @"
public class Generic<TFirst, TSecond> {
	public TSecond Field;
	public TMethod Method<TOther, TMethod>(TFirst first, TMethod second) => second;
}";
			using (var compiled = ModuleDefMD.Load(CSharpCompiler.Compile(source).Image)) {
				var type = compiled.Find("Generic`2", isReflectionName: true)!;
				var method = type.FindMethod("Method")!;

				var fieldType = Assert.IsType<GenericVar>(type.FindField("Field")!.FieldType);
				Assert.Equal(1U, fieldType.Number);
				Assert.True(fieldType.HasOwner);
				Assert.True(fieldType.HasOwnerType);
				Assert.False(fieldType.HasOwnerMethod);
				Assert.Same(type, fieldType.OwnerType);
				Assert.Null(fieldType.OwnerMethod);
				Assert.Same(type.GenericParameters[1], fieldType.GenericParam);
				Assert.Equal("TSecond", fieldType.GenericParam!.Name);

				var firstParameter = Assert.IsType<GenericVar>(method.MethodSig!.Params[0]);
				Assert.Same(type, firstParameter.OwnerType);
				Assert.Equal("TFirst", firstParameter.GenericParam!.Name);

				var returnType = Assert.IsType<GenericMVar>(method.MethodSig.RetType);
				Assert.Equal(1U, returnType.Number);
				Assert.True(returnType.HasOwner);
				Assert.True(returnType.HasOwnerMethod);
				Assert.False(returnType.HasOwnerType);
				Assert.Same(method, returnType.OwnerMethod);
				Assert.Same(method.GenericParameters[1], returnType.GenericParam);
				Assert.Equal("TMethod", returnType.GenericParam!.Name);

				// The owner isn't compared: it's the same parameter as one without an owner
				Assert.True(new SigComparer().Equals(returnType, new GenericMVar(1)));
				Assert.False(new SigComparer().Equals(returnType, new GenericMVar(0)));
				Assert.False(new SigComparer().Equals((TypeSig)fieldType, new GenericMVar(1)));
			}
		}

		// A number that the owner has no parameter for
		[Fact]
		public void GenericParameterWithANumberThatTheOwnerDoesNotHave() {
			var type = new TypeDefUser("Ns", "Generic`1");
			type.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));

			Assert.Same(type.GenericParameters[0], new GenericVar(0, type).GenericParam);
			Assert.Null(new GenericVar(1, type).GenericParam);
			Assert.True(new GenericVar(1, type).HasOwnerType);
		}
	}
}
