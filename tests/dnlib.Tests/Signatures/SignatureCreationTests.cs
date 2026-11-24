// dnlib: See LICENSE.txt for more info

using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Signatures {
	/// <summary>
	/// Creating signatures with the API: the <c>Create</c> methods of <see cref="MethodSig"/> and
	/// <see cref="PropertySig"/>, the properties of a calling convention, and the methods that convert between
	/// a type reference (<see cref="ITypeDefOrRef"/>) and a type signature (<see cref="TypeSig"/>)
	/// </summary>
	public class SignatureCreationTests {
		static readonly ModuleDefUser module = new ModuleDefUser("Test.dll");
		static ICorLibTypes Types => module.CorLibTypes;

		static string[] ParameterNames(MethodBaseSig signature) => signature.Params.Select(a => a!.FullName).ToArray();

		[Fact]
		public void StaticAndInstanceMethodSignatures() {
			var staticSignature = MethodSig.CreateStatic(Types.Void);
			Assert.False(staticSignature.HasThis);
			Assert.Equal(CallingConvention.Default, staticSignature.CallingConvention);
			Assert.Empty(staticSignature.Params);
			Assert.Same(Types.Void, staticSignature.RetType);

			var instanceSignature = MethodSig.CreateInstance(Types.Int32);
			Assert.True(instanceSignature.HasThis);
			Assert.False(instanceSignature.ExplicitThis);
			Assert.Equal(CallingConvention.HasThis, instanceSignature.CallingConvention);
		}

		// There's an overload for 0, 1, 2 and 3 parameters and one for any number
		[Fact]
		public void MethodSignatureParameters() {
			Assert.Equal(new[] { "System.Int32" }, ParameterNames(MethodSig.CreateStatic(Types.Void, Types.Int32)));
			Assert.Equal(new[] { "System.Int32", "System.String" }, ParameterNames(MethodSig.CreateStatic(Types.Void, Types.Int32, Types.String)));
			Assert.Equal(new[] { "System.Int32", "System.String", "System.Byte" }, ParameterNames(MethodSig.CreateStatic(Types.Void, Types.Int32, Types.String, Types.Byte)));
			Assert.Equal(4, MethodSig.CreateStatic(Types.Void, Types.Int32, Types.String, Types.Byte, Types.Char).Params.Count);
			Assert.Equal(new[] { "System.Int32" }, ParameterNames(MethodSig.CreateInstance(Types.Void, Types.Int32)));
			Assert.Equal(new[] { "System.Int32", "System.String" }, ParameterNames(MethodSig.CreateInstance(Types.Void, Types.Int32, Types.String)));
			Assert.Equal(new[] { "System.Int32", "System.String", "System.Byte" }, ParameterNames(MethodSig.CreateInstance(Types.Void, Types.Int32, Types.String, Types.Byte)));
			Assert.Equal(4, MethodSig.CreateInstance(Types.Void, Types.Int32, Types.String, Types.Byte, Types.Char).Params.Count);
		}

		[Fact]
		public void GenericMethodSignatures() {
			var staticSignature = MethodSig.CreateStaticGeneric(2, Types.Void, new GenericMVar(0), new GenericMVar(1));
			Assert.True(staticSignature.Generic);
			Assert.False(staticSignature.HasThis);
			Assert.Equal(2U, staticSignature.GenParamCount);
			Assert.Equal(CallingConvention.Generic, staticSignature.CallingConvention);
			Assert.Equal(2, staticSignature.Params.Count);

			var instanceSignature = MethodSig.CreateInstanceGeneric(1, Types.Void);
			Assert.True(instanceSignature.Generic);
			Assert.True(instanceSignature.HasThis);
			Assert.Equal(1U, instanceSignature.GenParamCount);
			Assert.Single(MethodSig.CreateInstanceGeneric(1, Types.Void, Types.Int32).Params);
			Assert.Equal(3, MethodSig.CreateInstanceGeneric(1, Types.Void, Types.Int32, Types.Int32, Types.Int32).Params.Count);
			Assert.Equal(3, MethodSig.CreateStaticGeneric(1, Types.Void, Types.Int32, Types.Int32, Types.Int32).Params.Count);
		}

		[Fact]
		public void PropertySignatures() {
			var staticProperty = PropertySig.CreateStatic(Types.Int32);
			Assert.False(staticProperty.HasThis);
			Assert.True(staticProperty.IsProperty);
			Assert.Same(Types.Int32, staticProperty.RetType);

			var indexer = PropertySig.CreateInstance(Types.String, Types.Int32, Types.Int32);
			Assert.True(indexer.HasThis);
			Assert.Equal(CallingConvention.Property | CallingConvention.HasThis, indexer.CallingConvention);
			Assert.Equal(new[] { "System.Int32", "System.Int32" }, ParameterNames(indexer));
			Assert.Single(PropertySig.CreateStatic(Types.String, Types.Int32).Params);
			Assert.Equal(3, PropertySig.CreateInstance(Types.String, Types.Int32, Types.Int32, Types.Int32).Params.Count);
		}

		[Theory]
		[InlineData(CallingConvention.Default)]
		[InlineData(CallingConvention.C)]
		[InlineData(CallingConvention.StdCall)]
		[InlineData(CallingConvention.ThisCall)]
		[InlineData(CallingConvention.FastCall)]
		[InlineData(CallingConvention.VarArg)]
		[InlineData(CallingConvention.Unmanaged)]
		[InlineData(CallingConvention.NativeVarArg)]
		public void KindOfCallingConventionIsInTheLowBits(CallingConvention kind) {
			// The flags don't change the kind
			var signature = new MethodSig(kind | CallingConvention.HasThis | CallingConvention.Generic);

			Assert.Equal(kind == CallingConvention.Default, signature.IsDefault);
			Assert.Equal(kind == CallingConvention.C, signature.IsC);
			Assert.Equal(kind == CallingConvention.StdCall, signature.IsStdCall);
			Assert.Equal(kind == CallingConvention.ThisCall, signature.IsThisCall);
			Assert.Equal(kind == CallingConvention.FastCall, signature.IsFastCall);
			Assert.Equal(kind == CallingConvention.VarArg, signature.IsVarArg);
			Assert.Equal(kind == CallingConvention.Unmanaged, signature.IsUnmanaged);
			Assert.Equal(kind == CallingConvention.NativeVarArg, signature.IsNativeVarArg);
			Assert.True(signature.HasThis);
			Assert.True(signature.Generic);
			Assert.False(signature.IsField);
			Assert.False(signature.IsProperty);
		}

		[Fact]
		public void FlagsOfACallingConventionCanBeChanged() {
			var signature = MethodSig.CreateStatic(Types.Void);

			signature.HasThis = true;
			signature.ExplicitThis = true;
			signature.Generic = true;
			Assert.Equal(CallingConvention.HasThis | CallingConvention.ExplicitThis | CallingConvention.Generic, signature.CallingConvention);

			signature.HasThis = false;
			Assert.Equal(CallingConvention.ExplicitThis | CallingConvention.Generic, signature.CallingConvention);
			Assert.True(signature.ImplicitThis == false);
		}

		[Fact]
		public void KindsOfSignatures() {
			Assert.True(new FieldSig(Types.Int32).IsField);
			Assert.True(new LocalSig(Types.Int32, Types.String).IsLocalSig);
			Assert.Equal(2, new LocalSig(Types.Int32, Types.String).Locals.Count);
			Assert.True(new GenericInstMethodSig(Types.Int32).IsGenericInst);
			Assert.Equal("System.Int32", new GenericInstMethodSig(Types.Int32, Types.String).GenericArguments[0]!.FullName);
			Assert.True(PropertySig.CreateStatic(Types.Int32).IsProperty);
		}

		[Fact]
		public void CloneCopiesAMethodSignature() {
			var original = MethodSig.CreateInstanceGeneric(1, Types.Int32, Types.String);
			original.ParamsAfterSentinel = new System.Collections.Generic.List<TypeSig?> { Types.Byte };

			var clone = original.Clone();

			Assert.NotSame(original, clone);
			Assert.NotSame(original.Params, clone.Params);
			Assert.Equal(original.CallingConvention, clone.CallingConvention);
			Assert.Equal(1U, clone.GenParamCount);
			Assert.Same(original.RetType, clone.RetType);
			Assert.Equal(ParameterNames(original), ParameterNames(clone));
			Assert.Equal("System.Byte", clone.ParamsAfterSentinel!.Single()!.FullName);
			Assert.True(new SigComparer().Equals(original, clone));
		}

		[Fact]
		public void TypeRefBecomesAClassOrValueTypeSignature() {
			var classRef = new TypeRefUser(module, "Ns", "SomeClass", Types.AssemblyRef);

			// Nothing can be resolved here, so the type is assumed to be a class
			var classSignature = classRef.ToTypeSig();
			Assert.IsType<ClassSig>(classSignature);
			Assert.Same(classRef, classSignature.ToTypeDefOrRef());
			Assert.Same(classSignature, classRef.ToTypeSig().ToTypeDefOrRef().ToTypeSig() is ClassSig ? classSignature : null);

			var valueTypeDef = new TypeDefUser("Ns", "SomeStruct", Types.GetTypeRef("System", "ValueType"));
			module.Types.Add(valueTypeDef);
			Assert.IsType<ValueTypeSig>(valueTypeDef.ToTypeSig());
			Assert.IsType<ClassSig>(new TypeDefUser("Ns", "SomeClass", Types.Object.TypeDefOrRef).ToTypeSig());
		}

		// A primitive type is its own short signature (eg. 'int32'), not 'valuetype System.Int32'
		[Fact]
		public void CoreLibraryTypeRefBecomesTheCorLibSignature() {
			var int32Ref = Types.GetTypeRef("System", "Int32");

			Assert.Same(Types.Int32, int32Ref.ToTypeSig());
			Assert.Same(Types.String, Types.GetTypeRef("System", "String").ToTypeSig());
			Assert.Equal("System.Int32", Types.Int32.ToTypeDefOrRef()!.FullName);
		}

		[Fact]
		public void TypeSpecBecomesItsSignature() {
			var arraySignature = new SZArraySig(Types.Int32);
			var typeSpec = new TypeSpecUser(arraySignature);

			Assert.Same(arraySignature, typeSpec.ToTypeSig());
			Assert.Same(arraySignature, typeSpec.TryGetSZArraySig());
			Assert.Null(typeSpec.TryGetArraySig());
			Assert.Null(typeSpec.TryGetPtrSig());
			Assert.Null(typeSpec.TryGetGenericInstSig());

			Assert.NotNull(new TypeSpecUser(new PtrSig(Types.Int32)).TryGetPtrSig());
			Assert.NotNull(new TypeSpecUser(new ByRefSig(Types.Int32)).TryGetByRefSig());
			Assert.NotNull(new TypeSpecUser(new ArraySig(Types.Int32, 2)).TryGetArraySig());
			Assert.NotNull(new TypeSpecUser(new GenericVar(0)).TryGetGenericVar());
			Assert.NotNull(new TypeSpecUser(new GenericVar(0)).TryGetGenericSig());
			Assert.Null(new TypeSpecUser(new GenericVar(0)).TryGetGenericMVar());
			Assert.NotNull(new TypeSpecUser(new GenericMVar(0)).TryGetGenericMVar());
			var listRef = new TypeRefUser(module, "System.Collections.Generic", "List`1", Types.AssemblyRef);
			Assert.NotNull(new TypeSpecUser(new GenericInstSig(new ClassSig(listRef), Types.Int32)).TryGetGenericInstSig());

			// A signature that isn't a class or value type becomes a new TypeSpec
			var converted = Assert.IsAssignableFrom<TypeSpec>(arraySignature.ToTypeDefOrRef());
			Assert.Same(arraySignature, converted.TypeSig);
		}

		// The TryGetXxxSig() methods return the signature of a TypeSpec. A TypeDef or TypeRef has none.
		[Fact]
		public void TryGetMethodsOnlyReturnTheSignatureOfATypeSpec() {
			var typeRef = new TypeRefUser(module, "Ns", "SomeClass", Types.AssemblyRef);
			var typeSpec = new TypeSpecUser(new ClassSig(typeRef));

			Assert.IsType<ClassSig>(typeSpec.TryGetTypeDefOrRefSig());
			Assert.IsType<ClassSig>(typeSpec.TryGetClassOrValueTypeSig());
			Assert.NotNull(typeSpec.TryGetClassSig());
			Assert.Null(typeSpec.TryGetValueTypeSig());
			Assert.NotNull(new TypeSpecUser(new ValueTypeSig(typeRef)).TryGetValueTypeSig());

			Assert.Null(typeRef.TryGetTypeDefOrRefSig());
			Assert.Null(typeRef.TryGetClassSig());
			Assert.Null(typeRef.TryGetSZArraySig());
			Assert.Null(((ITypeDefOrRef?)null).ToTypeSig());
			Assert.Null(((TypeSig?)null).ToTypeDefOrRef());
		}

		[Fact]
		public void ParametersOfAMethodReference() {
			var method = new MemberRefUser(module, "Method", MethodSig.CreateStatic(Types.Void, Types.Int32, Types.String), Types.GetTypeRef("System", "Console"));

			Assert.Equal(2, method.GetParamCount());
			Assert.True(method.HasParams());
			Assert.Equal("System.String", method.GetParam(1)!.FullName);
			Assert.Null(method.GetParam(2));
			Assert.Null(method.GetParam(-1));
			Assert.Equal(2, method.GetParams()!.Count);
			Assert.Equal(0, ((IMethod?)null).GetParamCount());
			Assert.False(((IMethod?)null).HasParams());
		}
	}
}
