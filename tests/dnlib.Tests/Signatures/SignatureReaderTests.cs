// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Signatures {
	/// <summary>
	/// <see cref="SignatureReader"/> reads the signature blobs of ECMA-335 II.23.2 (the types of fields, methods,
	/// locals etc). The tests give it the bytes of a signature and check what it returns. Invalid signatures
	/// are read like the CLR reads them where the CLR accepts them.
	/// </summary>
	public class SignatureReaderTests : IDisposable {
		// The element types and calling conventions that are used below, see ECMA-335 II.23.1.16 and II.23.2.3
		const byte Void = 0x01, I4 = 0x08, String = 0x0E, Ptr = 0x0F, ByRef = 0x10, ValueType = 0x11, Class = 0x12,
			Var = 0x13, Array = 0x14, GenericInst = 0x15, FnPtr = 0x1B, SZArray = 0x1D, MVar = 0x1E, CModReqd = 0x1F,
			CModOpt = 0x20, Internal = 0x21, CModInternal = 0x22, VarZapSig = 0x3B, NativeValueTypeZapSig = 0x3D, CanonZapSig = 0x3E, Module = 0x3F, Sentinel = 0x41, Pinned = 0x45, ValueArray = 0x17;
		const byte DefaultCall = 0x00, VarArgCall = 0x05, FieldCall = 0x06, LocalCall = 0x07, PropertyCall = 0x08,
			GenericInstCall = 0x0A, AsyncCall = 0x0C, GenericFlag = 0x10, HasThisFlag = 0x20;

		readonly ModuleDefMD module;
		// Coded TypeDefOrRef tokens (ECMA-335 II.23.2.8) of a class and a struct in the module
		readonly byte classToken;
		readonly byte structToken;

		public SignatureReaderTests() {
			module = ModuleDefMD.Load(CSharpCompiler.Compile("public class MyClass { } public struct MyStruct { }").Image);
			classToken = GetCodedToken("MyClass");
			structToken = GetCodedToken("MyStruct");
		}

		public void Dispose() => module.Dispose();

		byte GetCodedToken(string typeName) {
			var type = module.Find(typeName, isReflectionName: true)!;
			Assert.True(CodedToken.TypeDefOrRef.Encode(type.MDToken, out uint codedToken));
			// One byte when it's compressed
			Assert.True(codedToken < 0x80);
			return (byte)codedToken;
		}

		TypeSig? ReadType(params byte[] signature) => SignatureReader.ReadTypeSig(module, signature);
		CallingConventionSig? ReadSig(params byte[] signature) => SignatureReader.ReadSig(module, signature);

		[Fact]
		public void PrimitiveType() {
			var type = Assert.IsType<CorLibTypeSig>(ReadType(I4));

			Assert.Equal(ElementType.I4, type.ElementType);
			Assert.Equal("System.Int32", type.FullName);
		}

		[Fact]
		public void ClassAndValueType() {
			Assert.Equal("MyClass", Assert.IsType<ClassSig>(ReadType(Class, classToken)).FullName);
			Assert.Equal("MyStruct", Assert.IsType<ValueTypeSig>(ReadType(ValueType, structToken)).FullName);
		}

		[Fact]
		public void TypesMadeOfAnotherType() {
			Assert.Equal("System.Int32[]", Assert.IsType<SZArraySig>(ReadType(SZArray, I4)).FullName);
			Assert.Equal("System.Int32*", Assert.IsType<PtrSig>(ReadType(Ptr, I4)).FullName);
			Assert.Equal("System.Int32&", Assert.IsType<ByRefSig>(ReadType(ByRef, I4)).FullName);
			Assert.Equal("System.Int32", Assert.IsType<PinnedSig>(ReadType(Pinned, I4)).Next?.FullName);
			Assert.Equal("System.String[][]", ReadType(SZArray, SZArray, String)?.FullName);
		}

		[Fact]
		public void GenericParameters() {
			Assert.Equal(3U, Assert.IsType<GenericVar>(ReadType(Var, 3)).Number);
			Assert.Equal(4U, Assert.IsType<GenericMVar>(ReadType(MVar, 4)).Number);
		}

		[Fact]
		public void CustomModifiers() {
			var required = Assert.IsType<CModReqdSig>(ReadType(CModReqd, classToken, I4));
			Assert.Equal("MyClass", required.Modifier?.FullName);
			Assert.Equal("System.Int32", required.Next?.FullName);

			var optional = Assert.IsType<CModOptSig>(ReadType(CModOpt, structToken, String));
			Assert.Equal("MyStruct", optional.Modifier?.FullName);
		}

		[Fact]
		public void GenericInstance() {
			// MyClass<int, string>
			var type = Assert.IsType<GenericInstSig>(ReadType(GenericInst, Class, classToken, 2, I4, String));

			Assert.Equal("MyClass", Assert.IsType<ClassSig>(type.GenericType).FullName);
			Assert.Equal(new[] { "System.Int32", "System.String" }, type.GenericArguments.Select(a => a?.FullName).ToArray());
		}

		// Fixed in 0910a498: the CLR reads a token after the byte after GENERICINST, whatever the byte is. dnlib
		// read a whole type there, returned a generic instance without a generic type and misread the rest.
		// The type is a value type, see GenericInstanceWithAnotherByteThanClassOrValueTypeIsAValueType().
		[Fact]
		public void GenericInstanceWhoseTypeDoesNotStartWithClassOrValueType() {
			var type = Assert.IsType<GenericInstSig>(ReadType(GenericInst, Ptr, structToken, 1, I4));

			Assert.Equal("MyStruct", Assert.IsType<ValueTypeSig>(type.GenericType).FullName);
			Assert.Equal("System.Int32", Assert.Single(type.GenericArguments)?.FullName);
		}

		[Fact]
		public void MultiDimensionalArray() {
			// int[,] with a size of 3 for the first dimension and lower bounds 0 and -1 (compressed signed: 0x7F)
			var type = Assert.IsType<ArraySig>(ReadType(Array, I4, 2, 1, 3, 2, 0, 0x7F));

			Assert.Equal(2U, type.Rank);
			Assert.Equal(new uint[] { 3 }, type.Sizes);
			Assert.Equal(new[] { 0, -1 }, type.LowerBounds);
			Assert.Equal("System.Int32", type.Next?.FullName);
		}

		// Fixed in 908bc407: the CLR only reads the element type and the rank. dnlib returned null.
		[Fact]
		public void ArrayWithTruncatedLowerBoundsIsStillAnArray() {
			// Rank 3, no sizes, 3 lower bounds but only one is there
			var type = Assert.IsType<ArraySig>(ReadType(Array, I4, 3, 0, 3, 0));

			Assert.Equal(3U, type.Rank);
			Assert.Equal("System.Int32", type.Next?.FullName);
			Assert.Equal(new[] { 0 }, type.LowerBounds);
		}

		[Fact]
		public void ArrayWithTruncatedSizesIsStillAnArray() {
			// Rank 2, 2 sizes but only one is there
			var type = Assert.IsType<ArraySig>(ReadType(Array, I4, 2, 2, 5));

			Assert.Equal(2U, type.Rank);
			Assert.Equal(new uint[] { 5 }, type.Sizes);
		}

		// Fixed in 87be1cdc: the CLR doesn't compare the counts with the rank
		[Fact]
		public void ArrayWithMoreSizesAndLowerBoundsThanDimensions() {
			var type = Assert.IsType<ArraySig>(ReadType(Array, I4, 1, 2, 3, 4, 2, 0, 2));

			Assert.Equal(1U, type.Rank);
			Assert.Equal(new uint[] { 3, 4 }, type.Sizes);
			Assert.Equal(new[] { 0, 1 }, type.LowerBounds);
		}

		[Fact]
		public void FunctionPointer() {
			// static void(int)
			var type = Assert.IsType<FnPtrSig>(ReadType(FnPtr, DefaultCall, 1, Void, I4));

			var signature = Assert.IsType<MethodSig>(type.Signature);
			Assert.Equal("System.Void", signature.RetType?.FullName);
			Assert.Equal("System.Int32", Assert.Single(signature.Params)?.FullName);
		}

		// Fixed in e0ecf4be: the CLR skips one sentinel before any type (SigParser::SkipAnyVASentinel())
		[Fact]
		public void SentinelBeforeATypeIsSkipped() =>
			Assert.Equal("System.Int32", Assert.IsType<CorLibTypeSig>(ReadType(Sentinel, I4)).FullName);

		// Fixed in 9f903b57: a modifier that is a pointer to a runtime type. It can't be converted to a type
		// when a file is read, so it's ignored like the CLR does.
		[Fact]
		public void InternalModifierIsIgnored() {
			var signature = new byte[2 + IntPtr.Size + 1];
			signature[0] = CModInternal;
			// 1 = required modifier, then the pointer (zeros), then the type
			signature[1] = 1;
			signature[signature.Length - 1] = I4;

			Assert.Equal("System.Int32", Assert.IsType<CorLibTypeSig>(ReadType(signature)).FullName);
		}

		[Theory]
		[InlineData(new byte[0])]
		[InlineData(new byte[] { SZArray })]
		[InlineData(new byte[] { GenericInst, Class })]
		[InlineData(new byte[] { FnPtr })]
		// An element type that doesn't exist
		[InlineData(new byte[] { 0x7E })]
		public void TruncatedOrUnknownTypeIsNull(byte[] signature) =>
			Assert.Null(ReadType(signature));

		[Fact]
		public void ClassWithoutATokenHasNoType() {
			var type = Assert.IsType<ClassSig>(ReadType(Class));

			Assert.Null(type.TypeDefOrRef);
		}

		// Fixed in 03599a1c: there's a limit on how deeply types can be nested. Only the type at the limit is
		// null, the types around it are still read.
		[Fact]
		public void TypeNestedTooDeeplyIsNullAtTheLimit() {
			// 1000 times "array of", then int
			var signature = Enumerable.Repeat(SZArray, 1000).Concat(new[] { I4 }).ToArray();

			var type = ReadType(signature);

			int arrays = 0;
			for (; type is SZArraySig; type = type.Next)
				arrays++;
			Assert.Null(type);
			Assert.InRange(arrays, 10, 999);
		}

		// The types that can be at the end of the too deeply nested type of the next test
		public static IEnumerable<object[]> InnermostTypes() {
			const byte classToken = 0x08;   // TypeDef 2 as a coded token
			yield return new object[] { "int", new byte[] { I4 } };
			yield return new object[] { "class", new byte[] { Class, classToken } };
			yield return new object[] { "generic instance", new byte[] { GenericInst, Class, classToken, 2, I4, String } };
			// int[,] with one size (3) and one lower bound (0)
			yield return new object[] { "array", new byte[] { Array, I4, 2, 1, 3, 1, 0 } };
			// static void(int, string)
			yield return new object[] { "function pointer", new byte[] { FnPtr, DefaultCall, 2, Void, I4, String } };
			// A generic function pointer has the number of generic parameters before the number of parameters
			yield return new object[] { "generic function pointer", new byte[] { FnPtr, GenericFlag, 1, 1, Void, I4 } };
			yield return new object[] { "modifier", new byte[] { CModOpt, classToken, CModReqd, classToken, I4 } };
			yield return new object[] { "pointer", new byte[] { Ptr, Void } };
			yield return new object[] { "by reference", new byte[] { ByRef, I4 } };
			yield return new object[] { "generic parameters", new byte[] { GenericInst, Class, classToken, 2, Var, 0, MVar, 1 } };
			yield return new object[] { "pinned", new byte[] { Pinned, I4 } };
			// The types that only the runtime uses: a fixed size array (type, size), a type of another module
			// (module number, type), and a type or a modifier that's the address of the runtime's own type
			yield return new object[] { "value array", new byte[] { ValueArray, I4, 0x81, 0x00 } };
			yield return new object[] { "module", new byte[] { Module, 0x81, 0x00, I4 } };
			// A generic parameter by the rid of its row, the native view of a type, System.__Canon
			yield return new object[] { "generic parameter row", new byte[] { VarZapSig, 0x81, 0x00 } };
			yield return new object[] { "native view", new byte[] { NativeValueTypeZapSig, I4 } };
			yield return new object[] { "canonical type", new byte[] { CanonZapSig } };
			yield return new object[] { "internal", new byte[] { Internal }.Concat(new byte[IntPtr.Size]).ToArray() };
			yield return new object[] { "internal modifier", new byte[] { CModInternal, 1 }.Concat(new byte[IntPtr.Size]).Concat(new byte[] { I4 }).ToArray() };
			// The byte before the generic type's token isn't CLASS or VALUETYPE (the CLR ignores it)
			yield return new object[] { "generic instance of an int", new byte[] { GenericInst, I4, classToken, 1, I4 } };
			yield return new object[] { "array without dimensions", new byte[] { Array, I4, 0 } };
		}

		// A type that's nested too deeply isn't read, but the reader must still know where it ends: the types
		// that follow it in the signature are read. The method is 'static void(TooDeep, string)'.
		[Theory]
		[MemberData(nameof(InnermostTypes))]
		public void TypeAfterATypeThatIsNestedTooDeeplyIsRead(string description, byte[] innermostType) {
			Assert.NotEmpty(description);
			var signature = new List<byte> { DefaultCall, 2, Void };
			signature.AddRange(Enumerable.Repeat(SZArray, 300));
			signature.AddRange(innermostType);
			signature.Add(String);

			var methodSig = Assert.IsType<MethodSig>(ReadSig(signature.ToArray()));

			Assert.Equal(2, methodSig.Params.Count);
			Assert.Equal(ElementType.String, methodSig.Params[1]!.ElementType);
			// The first parameter is arrays of arrays of ... of nothing
			var type = methodSig.Params[0];
			while (type is SZArraySig)
				type = type.Next;
			Assert.Null(type);
		}

		// Fixed in this commit: these three element types weren't read. The runtime uses them in its own
		// signatures, but it also reads them in a method signature of a file. The type was null and the bytes
		// of a generic parameter's rid or of the native view's type were read as the next parameter.
		[Fact]
		public void ElementTypesOfTheRuntimesOwnSignaturesAreRead() {
			// static void(native int, generic parameter row 0x81, System.__Canon, string)
			var signature = Assert.IsType<MethodSig>(ReadSig(DefaultCall, 4, Void, NativeValueTypeZapSig, I4, VarZapSig, 0x80, 0x81, CanonZapSig, String));

			Assert.Equal(4, signature.Params.Count);
			var native = Assert.IsType<NativeValueTypeZapSig>(signature.Params[0]);
			Assert.Equal(ElementType.NativeValueTypeZapSig, native.ElementType);
			Assert.Equal(ElementType.I4, native.Next!.ElementType);
			var genericParam = Assert.IsType<VarZapSig>(signature.Params[1]);
			Assert.Equal(ElementType.VarZapSig, genericParam.ElementType);
			Assert.Equal(0x81U, genericParam.GenericParamRid);
			Assert.Null(genericParam.Next);
			var canon = Assert.IsType<CanonZapSig>(signature.Params[2]);
			Assert.Equal(ElementType.CanonZapSig, canon.ElementType);
			Assert.Null(canon.Next);
			Assert.Equal(ElementType.String, signature.Params[3]!.ElementType);
		}

		[Fact]
		public void NativeViewCanBeNestedAndBeTheElementOfOtherTypes() {
			// native native int*[]
			var type = ReadType(SZArray, Ptr, NativeValueTypeZapSig, NativeValueTypeZapSig, I4);

			var native = Assert.IsType<NativeValueTypeZapSig>(Assert.IsType<PtrSig>(Assert.IsType<SZArraySig>(type).Next).Next);
			Assert.Equal(ElementType.I4, Assert.IsType<NativeValueTypeZapSig>(native.Next).Next!.ElementType);
		}

		[Fact]
		public void GenericParameterRowWithoutItsRidIsNotRead() {
			Assert.Null(ReadType(VarZapSig));
			// The rid is a compressed number: 0x80 needs a second byte
			Assert.Null(ReadType(VarZapSig, 0x80));
		}

		// Fixed in this commit: it was read as a class, so TypeSig.IsValueType was false and CLASS was written
		// (which the CLR doesn't load if the type is a value type). The CLR only checks whether the byte before
		// the generic type's token is CLASS: with any other byte, the type must be a value type.
		[Fact]
		public void GenericInstanceWithAnotherByteThanClassOrValueTypeIsAValueType() {
			// MyStruct<int>, with "int" where VALUETYPE should be
			var type = Assert.IsType<GenericInstSig>(ReadType(GenericInst, I4, structToken, 1, I4));

			Assert.IsType<ValueTypeSig>(type.GenericType);
			Assert.Equal("MyStruct", type.GenericType!.TypeName);
			Assert.True(type.IsValueType);
			Assert.Equal(ElementType.I4, type.GenericArguments.Single()!.ElementType);
			// The normal forms
			Assert.True(ReadType(GenericInst, ValueType, structToken, 1, I4)!.IsValueType);
			Assert.False(ReadType(GenericInst, Class, classToken, 1, I4)!.IsValueType);
		}

		[Fact]
		public void StaticMethodSignature() {
			// static string(int, MyClass)
			var signature = Assert.IsType<MethodSig>(ReadSig(DefaultCall, 2, String, I4, Class, classToken));

			Assert.False(signature.HasThis);
			Assert.Equal("System.String", signature.RetType?.FullName);
			Assert.Equal(new[] { "System.Int32", "MyClass" }, signature.Params.Select(a => a?.FullName).ToArray());
			Assert.Null(signature.ParamsAfterSentinel);
		}

		[Fact]
		public void InstanceMethodSignature() {
			var signature = Assert.IsType<MethodSig>(ReadSig(HasThisFlag, 0, Void));

			Assert.True(signature.HasThis);
			Assert.Empty(signature.Params);
		}

		[Fact]
		public void GenericMethodSignature() {
			// 2 generic parameters: !!1 M<,>(!!0)
			var signature = Assert.IsType<MethodSig>(ReadSig(GenericFlag, 2, 1, MVar, 1, MVar, 0));

			Assert.True(signature.Generic);
			Assert.Equal(2U, signature.GenParamCount);
			Assert.IsType<GenericMVar>(signature.RetType);
		}

		[Fact]
		public void VarArgCallSiteSignatureHasTheParametersAfterTheSentinel() {
			// void(int, ..., string, int)
			var signature = Assert.IsType<MethodSig>(ReadSig(VarArgCall, 3, Void, I4, Sentinel, String, I4));

			Assert.Equal("System.Int32", Assert.Single(signature.Params)?.FullName);
			Assert.NotNull(signature.ParamsAfterSentinel);
			Assert.Equal(new[] { "System.String", "System.Int32" }, signature.ParamsAfterSentinel.Select(a => a?.FullName).ToArray());
		}

		// Fixed in 1c386599: the calling convention of runtime async methods, added in .NET 10
		[Fact]
		public void AsyncCallingConventionIsAMethodSignature() {
			var signature = Assert.IsType<MethodSig>(ReadSig(AsyncCall, 1, Void, I4));

			Assert.Equal((CallingConvention)AsyncCall, signature.CallingConvention & CallingConvention.Mask);
			Assert.Equal("System.Int32", Assert.Single(signature.Params)?.FullName);
		}

		[Fact]
		public void FieldSignature() =>
			Assert.Equal("System.Int32[]", Assert.IsType<FieldSig>(ReadSig(FieldCall, SZArray, I4)).Type?.FullName);

		[Fact]
		public void PropertySignature() {
			// An indexer: instance string this[int]
			var signature = Assert.IsType<PropertySig>(ReadSig(PropertyCall | HasThisFlag, 1, String, I4));

			Assert.True(signature.HasThis);
			Assert.Equal("System.String", signature.RetType?.FullName);
			Assert.Equal("System.Int32", Assert.Single(signature.Params)?.FullName);
		}

		[Fact]
		public void LocalSignature() {
			var signature = Assert.IsType<LocalSig>(ReadSig(LocalCall, 2, I4, Pinned, String));

			Assert.Equal(2, signature.Locals.Count);
			Assert.Equal("System.Int32", signature.Locals[0]?.FullName);
			Assert.IsType<PinnedSig>(signature.Locals[1]);
		}

		[Fact]
		public void GenericMethodInstantiationSignature() {
			var signature = Assert.IsType<GenericInstMethodSig>(ReadSig(GenericInstCall, 2, I4, String));

			Assert.Equal(new[] { "System.Int32", "System.String" }, signature.GenericArguments.Select(a => a?.FullName).ToArray());
		}

		// Fixed in 6c76fd18: the CLR's limit. The count is the compressed integer 0x10000.
		[Fact]
		public void MethodSignatureWithMoreThan65535ParametersIsNull() =>
			Assert.Null(ReadSig(DefaultCall, 0xC0, 0x01, 0x00, 0x00, Void));

		[Theory]
		[InlineData(new byte[0])]
		[InlineData(new byte[] { DefaultCall })]
		[InlineData(new byte[] { DefaultCall, 1 })]
		[InlineData(new byte[] { FieldCall })]
		public void TruncatedSignatureIsNull(byte[] signature) =>
			Assert.Null(ReadSig(signature));

		[Fact]
		public void MethodSignatureWithAMissingParameterHasANullParameter() {
			// 2 parameters, but only one is there
			var signature = ReadSig(DefaultCall, 2, Void, I4);

			// dnlib keeps what it could read: the caller sees which parameter is invalid
			if (signature is MethodSig methodSig) {
				Assert.Equal(2, methodSig.Params.Count);
				Assert.Equal("System.Int32", methodSig.Params[0]?.FullName);
				Assert.Null(methodSig.Params[1]);
			}
		}
	}
}
