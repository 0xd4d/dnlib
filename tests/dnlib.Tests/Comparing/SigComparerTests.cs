// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// <see cref="SigComparer"/> compares types, members and signatures by name and signature, like the CLR does
	/// when it resolves a reference. The same type can be a <see cref="TypeDef"/>, a <see cref="TypeRef"/> in
	/// another module, a <see cref="TypeSig"/> etc, and they must all be equal and have the same hash code.
	/// The comparer is used as the key comparer of dictionaries, so equal things must have equal hash codes.
	/// See also <see cref="Reflection.ReflectionComparisonTests"/>.
	/// </summary>
	public class SigComparerTests {
		static ModuleDefUser CreateModule(string assemblyName) {
			var module = new ModuleDefUser(assemblyName + ".dll");
			new AssemblyDefUser(assemblyName, new Version(1, 0, 0, 0)).Modules.Add(module);
			return module;
		}

		static TypeDefUser AddType(ModuleDef module, string @namespace, string name) {
			var type = new TypeDefUser(@namespace, name, module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(type);
			return type;
		}

		static MethodDefUser AddMethod(TypeDef type, string name, MethodSig signature) {
			// Not "private scope" (no access flags): such a method can't be referenced, so it never equals a MemberRef
			var method = new MethodDefUser(name, signature, MethodAttributes.Public | MethodAttributes.Static);
			type.Methods.Add(method);
			return method;
		}

		static PropertyDefUser AddProperty(TypeDef type, string name, TypeSig propertyType) {
			var property = new PropertyDefUser(name, PropertySig.CreateStatic(propertyType));
			type.Properties.Add(property);
			return property;
		}

		static TypeRefUser CreateTypeRef(ModuleDef module, string @namespace, string name, string assemblyName) =>
			new TypeRefUser(module, @namespace, name, new AssemblyRefUser(assemblyName, new Version(1, 0, 0, 0)));

		[Fact]
		public void TypeDefEqualsATypeRefToIt() {
			var type = AddType(CreateModule("A"), "Ns", "Type");
			var typeRef = CreateTypeRef(CreateModule("B"), "Ns", "Type", "A");
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(type, typeRef));
			Assert.True(comparer.Equals(typeRef, type));
			Assert.Equal(comparer.GetHashCode(type), comparer.GetHashCode(typeRef));
		}

		[Theory]
		[InlineData("Ns", "Other", "A")]
		[InlineData("Other", "Type", "A")]
		[InlineData("Ns", "Type", "Other")]
		[InlineData("ns", "type", "A")]
		public void TypeDefDoesNotEqualATypeRefWithAnotherNameOrAssembly(string @namespace, string name, string assemblyName) {
			var type = AddType(CreateModule("A"), "Ns", "Type");
			var typeRef = CreateTypeRef(CreateModule("B"), @namespace, name, assemblyName);

			Assert.False(new SigComparer().Equals(type, typeRef));
		}

		[Fact]
		public void AssemblyIsNotComparedWithDontCompareTypeScope() {
			var type = AddType(CreateModule("A"), "Ns", "Type");
			var typeRef = CreateTypeRef(CreateModule("B"), "Ns", "Type", "Other");
			var comparer = new SigComparer(SigComparerOptions.DontCompareTypeScope);

			Assert.True(comparer.Equals(type, typeRef));
			Assert.Equal(comparer.GetHashCode(type), comparer.GetHashCode(typeRef));
		}

		[Fact]
		public void NamesAreComparedWithoutCaseWithCaseInsensitiveAll() {
			var type = AddType(CreateModule("A"), "Ns", "Type");
			var typeRef = CreateTypeRef(CreateModule("B"), "NS", "TYPE", "A");
			var comparer = new SigComparer(SigComparerOptions.CaseInsensitiveAll);

			Assert.True(comparer.Equals(type, typeRef));
			Assert.Equal(comparer.GetHashCode(type), comparer.GetHashCode(typeRef));
		}

		[Fact]
		public void TypeSigEqualsItsType() {
			var type = AddType(CreateModule("A"), "Ns", "Type");
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(new ClassSig(type), type));
			Assert.Equal(comparer.GetHashCode(new ClassSig(type)), comparer.GetHashCode(type));
			Assert.False(comparer.Equals(new SZArraySig(new ClassSig(type)), type));
		}

		[Fact]
		public void MethodSignaturesAreComparedByCallingConventionReturnTypeAndParameters() {
			var corLib = CreateModule("A").CorLibTypes;
			var comparer = new SigComparer();
			var signature = MethodSig.CreateStatic(corLib.Int32, corLib.String);

			Assert.True(comparer.Equals(signature, MethodSig.CreateStatic(corLib.Int32, corLib.String)));
			Assert.Equal(comparer.GetHashCode(signature), comparer.GetHashCode(MethodSig.CreateStatic(corLib.Int32, corLib.String)));
			Assert.False(comparer.Equals(signature, MethodSig.CreateStatic(corLib.Int64, corLib.String)));
			Assert.False(comparer.Equals(signature, MethodSig.CreateStatic(corLib.Int32, corLib.Object)));
			Assert.False(comparer.Equals(signature, MethodSig.CreateStatic(corLib.Int32, corLib.String, corLib.String)));
			Assert.False(comparer.Equals(signature, MethodSig.CreateInstance(corLib.Int32, corLib.String)));
		}

		[Fact]
		public void ReturnTypeIsNotComparedWithDontCompareReturnType() {
			var corLib = CreateModule("A").CorLibTypes;
			var comparer = new SigComparer(SigComparerOptions.DontCompareReturnType);
			var first = MethodSig.CreateStatic(corLib.Int32, corLib.String);
			var second = MethodSig.CreateStatic(corLib.Int64, corLib.String);

			Assert.True(comparer.Equals(first, second));
			Assert.Equal(comparer.GetHashCode(first), comparer.GetHashCode(second));
		}

		// Fixed in caf2a30f: the property's type was still hashed
		[Fact]
		public void PropertyTypeIsNotComparedOrHashedWithDontCompareReturnType() {
			var module = CreateModule("A");
			var first = AddProperty(AddType(module, "Ns", "First"), "P", module.CorLibTypes.Int32);
			var second = AddProperty(AddType(module, "Ns", "Second"), "P", module.CorLibTypes.String);

			Assert.False(new SigComparer().Equals(first, second));
			var comparer = new SigComparer(SigComparerOptions.DontCompareReturnType);
			Assert.True(comparer.Equals(first, second));
			Assert.Equal(comparer.GetHashCode(first), comparer.GetHashCode(second));
		}

		[Fact]
		public void MethodsInDifferentTypesAreOnlyEqualIfDeclaringTypesAreNotCompared() {
			var module = CreateModule("A");
			var first = AddMethod(AddType(module, "Ns", "First"), "M", MethodSig.CreateStatic(module.CorLibTypes.Void));
			var second = AddMethod(AddType(module, "Ns", "Second"), "M", MethodSig.CreateStatic(module.CorLibTypes.Void));

			Assert.True(new SigComparer().Equals(first, second));
			Assert.False(new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType).Equals(first, second));
		}

		// A method without access flags is "private scope" (compiler controlled): it can only be referenced
		// by its token, never by name, so a reference by name isn't a reference to it
		[Fact]
		public void PrivateScopeMethodOnlyEqualsAReferenceWithPrivateScopeIsComparable() {
			var module = CreateModule("A");
			var type = AddType(module, "Ns", "Type");
			var method = AddMethod(type, "M", MethodSig.CreateStatic(module.CorLibTypes.Void));
			method.Attributes = MethodAttributes.Static;
			Assert.True(method.IsPrivateScope);
			var reference = new MemberRefUser(module, "M", MethodSig.CreateStatic(module.CorLibTypes.Void), type);

			Assert.False(new SigComparer().Equals(method, reference));
			Assert.True(new SigComparer(SigComparerOptions.PrivateScopeIsComparable).Equals(method, reference));
		}

		[Fact]
		public void ModifiersAreOnlyIgnoredWithIgnoreModifiers() {
			var module = CreateModule("A");
			var modifier = CreateTypeRef(module, "Ns", "Modifier", "B");
			TypeSig plain = module.CorLibTypes.Int32;
			TypeSig modified = new CModOptSig(modifier, module.CorLibTypes.Int32);

			Assert.False(new SigComparer().Equals(plain, modified));
			var comparer = new SigComparer(SigComparerOptions.IgnoreModifiers);
			Assert.True(comparer.Equals(plain, modified));
			Assert.Equal(comparer.GetHashCode(plain), comparer.GetHashCode(modified));
		}

		// Fixed in 2481fb96: the global type has its own hash code, but Equals() only compared the names
		[Fact]
		public void GlobalTypeDoesNotEqualAnotherTypeWithItsName() {
			var module = CreateModule("A");
			var globalType = module.GlobalType!;
			var other = AddType(module, globalType.Namespace, globalType.Name);
			var comparer = new SigComparer();

			Assert.False(comparer.Equals(globalType, other));
			Assert.True(comparer.Equals(globalType, globalType));
		}

		// Fixed in dc9a964b: the def and the reference were equal but had different hash codes
		[Fact]
		public void GlobalMethodAndAReferenceToItHaveTheSameHashCodeWithTypeRefCanReferenceGlobalType() {
			var module = CreateModule("A");
			var method = AddMethod(module.GlobalType!, "M", MethodSig.CreateStatic(module.CorLibTypes.Void));
			var globalTypeRef = CreateTypeRef(CreateModule("B"), "", "<Module>", "A");
			var reference = new MemberRefUser(module, "M", MethodSig.CreateStatic(module.CorLibTypes.Void), globalTypeRef);
			var comparer = new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType | SigComparerOptions.TypeRefCanReferenceGlobalType);

			Assert.True(comparer.Equals(method, reference));
			Assert.Equal(comparer.GetHashCode(method), comparer.GetHashCode(reference));
		}

		// Fixed in fb71cd18: same as above for properties
		[Fact]
		public void EqualPropertiesHaveTheSameHashCodeWithTypeRefCanReferenceGlobalType() {
			var module = CreateModule("A");
			var globalProperty = AddProperty(module.GlobalType!, "P", module.CorLibTypes.Int32);
			var otherProperty = AddProperty(AddType(module, "", "<Module>"), "P", module.CorLibTypes.Int32);
			var comparer = new SigComparer(SigComparerOptions.ComparePropertyDeclaringType | SigComparerOptions.TypeRefCanReferenceGlobalType);

			if (comparer.Equals(globalProperty, otherProperty))
				Assert.Equal(comparer.GetHashCode(globalProperty), comparer.GetHashCode(otherProperty));
		}

		// Fixed in 9ebf15de: the global type and a reference to it were equal but had different hash codes, so
		// a dictionary with this comparer (eg. the cache of TypeDefFinder) didn't find the global type
		[Fact]
		public void GlobalTypeAndAReferenceToItHaveTheSameHashCodeWithTypeRefCanReferenceGlobalType() {
			var module = CreateModule("A");
			var globalType = module.GlobalType!;
			var globalTypeRef = CreateTypeRef(CreateModule("B"), "", "<Module>", "A");
			var comparer = new SigComparer(SigComparerOptions.TypeRefCanReferenceGlobalType);

			Assert.True(comparer.Equals(globalType, globalTypeRef));
			Assert.Equal(comparer.GetHashCode(globalType), comparer.GetHashCode(globalTypeRef));

			var types = new Dictionary<IType, string>(new TypeEqualityComparer(SigComparerOptions.TypeRefCanReferenceGlobalType)) {
				[globalType] = "found",
			};
			Assert.True(types.ContainsKey(globalTypeRef));
		}

		// 8bd4a5f0 made a null name equal to an empty name (the hash codes were equal, Equals() was false).
		// Since then the types store null as an empty name, so there's no null name to compare anymore.
		[Fact]
		public void NullNamespaceEqualsAnEmptyNamespace() {
			var module = CreateModule("A");
			var noNamespace = new TypeRefUser(module, null, "Type", module.Assembly.ToAssemblyRef());
			var emptyNamespace = new TypeRefUser(module, "", "Type", module.Assembly.ToAssemblyRef());
			Assert.Equal(UTF8String.Empty, noNamespace.Namespace);
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(noNamespace, emptyNamespace));
			Assert.True(comparer.Equals(emptyNamespace, noNamespace));
			Assert.Equal(comparer.GetHashCode(noNamespace), comparer.GetHashCode(emptyNamespace));
			Assert.True(new SigComparer(SigComparerOptions.CaseInsensitiveAll).Equals(noNamespace, emptyNamespace));
		}

		class GenericOwner<T> {
			public static void Method<TFirst, TSecond>(TFirst first, TSecond second, T third) { }
		}

		class Caller {
			public static void Call<TA, TB>() => GenericOwner<TA>.Method<TB, TA>(default!, default!, default!);
		}

		// Fixed in d5b88e2a: the call in Caller.Call() is to GenericOwner<!!0>::Method<!!1, !!0>. Its !!0 in the
		// declaring type is the caller's TA. The hash code used the method's generic arguments for the
		// declaring type too (GenericOwner<!!1>), Equals() didn't.
		[Fact]
		public void MethodSpecWhoseDeclaringTypeUsesTheCallersGenericParameterHasTheHashCodeOfTheReflectionMethod() {
			var callerArguments = typeof(Caller).GetMethod(nameof(Caller.Call))!.GetGenericArguments();
			var reflectionMethod = typeof(GenericOwner<>).MakeGenericType(callerArguments[0]).GetMethod("Method")!
				.MakeGenericMethod(callerArguments[1], callerArguments[0]);
			using (var module = ModuleDefMD.Load(typeof(SigComparerTests).Module)) {
				var methodSpec = Assert.IsAssignableFrom<MethodSpec>(new Importer(module).Import(reflectionMethod));
				Assert.Equal("System.Void dnlib.Tests.Comparing.SigComparerTests/GenericOwner`1<!!0>::Method<!!1,!!0>(!!1,!!0,!!0)", methodSpec.FullName);
				var comparer = new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType);

				Assert.True(comparer.Equals(methodSpec, reflectionMethod));
				Assert.Equal(comparer.GetHashCode(reflectionMethod), comparer.GetHashCode(methodSpec));
			}
		}

		// Fixed in fb5145e0: the reference of a call to a vararg method can have the method itself as its parent
		[Fact]
		public void MemberRefWhoseParentIsTheMethodEqualsTheMethod() {
			var module = CreateModule("A");
			var type = AddType(module, "Ns", "Type");
			var method = AddMethod(type, "M", MethodSig.CreateStatic(module.CorLibTypes.Void));
			var referenceWithMethodParent = new MemberRefUser(module, "M", MethodSig.CreateStatic(module.CorLibTypes.Void), method);
			var referenceWithTypeParent = new MemberRefUser(module, "M", MethodSig.CreateStatic(module.CorLibTypes.Void), type);
			var comparer = new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType);

			Assert.True(comparer.Equals(method, referenceWithMethodParent));
			Assert.True(comparer.Equals(referenceWithMethodParent, referenceWithTypeParent));
			Assert.Equal(comparer.GetHashCode(method), comparer.GetHashCode(referenceWithMethodParent));
			Assert.Equal(comparer.GetHashCode(referenceWithTypeParent), comparer.GetHashCode(referenceWithMethodParent));
		}

		// Fixed in 61e763f1: all instantiations of a generic method had the same hash code, which made
		// dictionaries slow
		[Fact]
		public void InstantiationsOfAGenericMethodAreDifferent() {
			var module = CreateModule("A");
			var method = AddMethod(AddType(module, "Ns", "Type"), "M", MethodSig.CreateStaticGeneric(1, module.CorLibTypes.Void));
			var ofInt = new MethodSpecUser(method, new GenericInstMethodSig(module.CorLibTypes.Int32));
			var ofString = new MethodSpecUser(method, new GenericInstMethodSig(module.CorLibTypes.String));
			var ofIntAgain = new MethodSpecUser(method, new GenericInstMethodSig(module.CorLibTypes.Int32));
			var comparer = new SigComparer();

			Assert.False(comparer.Equals(ofInt, ofString));
			Assert.NotEqual(comparer.GetHashCode(ofInt), comparer.GetHashCode(ofString));
			Assert.True(comparer.Equals(ofInt, ofIntAgain));
			Assert.Equal(comparer.GetHashCode(ofInt), comparer.GetHashCode(ofIntAgain));
		}

		// Fixed in e37ea833: all nested types with the same name had the same hash code
		[Fact]
		public void NestedTypesWithTheSameNameInDifferentTypesAreDifferent() {
			var module = CreateModule("A");
			var first = new TypeDefUser("Nested");
			AddType(module, "Ns", "First").NestedTypes.Add(first);
			var second = new TypeDefUser("Nested");
			AddType(module, "Ns", "Second").NestedTypes.Add(second);
			var comparer = new SigComparer();

			Assert.False(comparer.Equals(first, second));
			Assert.NotEqual(comparer.GetHashCode(first), comparer.GetHashCode(second));
		}

		// Fixed in 282c22ab: like the CLR, DontCompareReturnType is only for the method that's compared
		[Fact]
		public void ReturnTypeOfAFunctionPointerIsAlwaysCompared() {
			var corLib = CreateModule("A").CorLibTypes;
			var returnsVoid = new FnPtrSig(MethodSig.CreateStatic(corLib.Void));
			var returnsInt = new FnPtrSig(MethodSig.CreateStatic(corLib.Int32));

			Assert.False(new SigComparer(SigComparerOptions.DontCompareReturnType).Equals(returnsVoid, returnsInt));
		}

		// Fixed in 0dca9b52: the CLR compares all arguments of a function pointer
		[Fact]
		public void ParametersAfterTheSentinelOfAFunctionPointerAreAlwaysCompared() {
			var corLib = CreateModule("A").CorLibTypes;
			FnPtrSig Create(TypeSig? afterSentinel) {
				var signature = MethodSig.CreateStatic(corLib.Void);
				signature.CallingConvention |= CallingConvention.VarArg;
				if (afterSentinel is not null)
					signature.ParamsAfterSentinel = new List<TypeSig?> { afterSentinel };
				return new FnPtrSig(signature);
			}
			var comparer = new SigComparer();

			Assert.False(comparer.Equals(Create(corLib.Int32), Create(corLib.String)));
			Assert.False(comparer.Equals(Create(corLib.Int32), Create(null)));
			Assert.True(comparer.Equals(Create(corLib.Int32), Create(corLib.Int32)));
		}

		[Fact]
		public void ParametersAfterTheSentinelOfAMethodAreOnlyComparedWithCompareSentinelParams() {
			var corLib = CreateModule("A").CorLibTypes;
			MethodSig Create(TypeSig afterSentinel) {
				var signature = MethodSig.CreateStatic(corLib.Void);
				signature.CallingConvention |= CallingConvention.VarArg;
				signature.ParamsAfterSentinel = new List<TypeSig?> { afterSentinel };
				return signature;
			}

			// A call site (with the extra arguments) must match the method (without them)
			Assert.True(new SigComparer().Equals(Create(corLib.Int32), Create(corLib.String)));
			Assert.False(new SigComparer(SigComparerOptions.CompareSentinelParams).Equals(Create(corLib.Int32), Create(corLib.String)));
		}

		// Fixed in 20acbd9b: the CLR ignores this bit (MetaSig::CompareMethodSigs). The hash code already did.
		[Fact]
		public void CallingConventionBitThatIsReservedByTheClrIsIgnored() {
			var corLib = CreateModule("A").CorLibTypes;
			var plain = MethodSig.CreateStatic(corLib.Void);
			var withBit = MethodSig.CreateStatic(corLib.Void);
			withBit.CallingConvention |= CallingConvention.ReservedByCLR;
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(plain, withBit));
			Assert.Equal(comparer.GetHashCode(plain), comparer.GetHashCode(withBit));
		}

		// Fixed in c0432ee1: the unmanaged calling convention can also be a modifier of the return type
		[Fact]
		public void UnmanagedCallingConventionsOfFunctionPointersAreEqualWithIgnoreModifiers() {
			var corLib = CreateModule("A").CorLibTypes;
			FnPtrSig Create(CallingConvention callingConvention) => new FnPtrSig(new MethodSig(callingConvention, 0, corLib.Void));

			Assert.False(new SigComparer().Equals(Create(CallingConvention.C), Create(CallingConvention.StdCall)));
			var comparer = new SigComparer(SigComparerOptions.IgnoreModifiers);
			Assert.True(comparer.Equals(Create(CallingConvention.C), Create(CallingConvention.StdCall)));
			Assert.Equal(comparer.GetHashCode(Create(CallingConvention.C)), comparer.GetHashCode(Create(CallingConvention.StdCall)));
			// A managed function pointer is still something else
			Assert.False(comparer.Equals(Create(CallingConvention.C), Create(CallingConvention.Default)));
		}

		// Fixed in bc9c83f1: the CLR looks for a TypeRef without a scope in the TypeRef's own assembly
		[Fact]
		public void TypeRefWithoutAScopeIsATypeOfItsOwnAssembly() {
			var moduleA = CreateModule("A");
			var moduleB = CreateModule("B");
			var typeInA = AddType(moduleA, "Ns", "Type");
			var noScopeInA = new TypeRefUser(moduleA, "Ns", "Type");
			var noScopeInB = new TypeRefUser(moduleB, "Ns", "Type");
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(noScopeInA, typeInA));
			Assert.Equal(comparer.GetHashCode(noScopeInA), comparer.GetHashCode(typeInA));
			Assert.False(comparer.Equals(noScopeInB, typeInA));
			Assert.False(comparer.Equals(noScopeInA, noScopeInB));
			Assert.True(comparer.Equals(noScopeInA, new TypeRefUser(moduleA, "Ns", "Type")));
		}

		// Fixed in 4cf4d295: a missing scope made the compare fail before the option was checked
		[Fact]
		public void TypeRefWithoutAScopeEqualsAnyTypeRefWithItsNameWithDontCompareTypeScope() {
			var noScope = new TypeRefUser(CreateModule("A"), "Ns", "Type");
			var withScope = CreateTypeRef(CreateModule("B"), "Ns", "Type", "C");
			var comparer = new SigComparer(SigComparerOptions.DontCompareTypeScope);

			Assert.True(comparer.Equals(noScope, withScope));
			Assert.Equal(comparer.GetHashCode(noScope), comparer.GetHashCode(withScope));
		}

		// Fixed in 7deb112b: the type definition List<T> equalled typeof(List<int>)
		[Fact]
		public void GenericTypeDefinitionDoesNotEqualAGenericInstanceType() {
			using (var coreLibrary = ModuleDefMD.Load(File.ReadAllBytes(typeof(object).Assembly.Location))) {
				var list = coreLibrary.Find("System.Collections.Generic.List`1", isReflectionName: true);
				var comparer = new SigComparer();

				Assert.True(comparer.Equals(list, typeof(List<>)));
				Assert.False(comparer.Equals(list, typeof(List<int>)));
				Assert.False(comparer.Equals(list, typeof(List<>).GetGenericArguments()[0]));
			}
		}

		// The sizes and the lower bounds of the dimensions are a part of an array type
		[Fact]
		public void ArraysWithOtherBoundsAreDifferent() {
			var module = CreateModule("A");
			ArraySig CreateArray(uint[] sizes, int[] lowerBounds) => new ArraySig(module.CorLibTypes.Int32, 2U, sizes, lowerBounds);
			var array = CreateArray(new uint[] { 3, 4 }, new[] { 0, 1 });
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(array, CreateArray(new uint[] { 3, 4 }, new[] { 0, 1 })));
			Assert.Equal(comparer.GetHashCode(array), comparer.GetHashCode(CreateArray(new uint[] { 3, 4 }, new[] { 0, 1 })));
			Assert.False(comparer.Equals(array, CreateArray(new uint[] { 3, 5 }, new[] { 0, 1 })));
			Assert.False(comparer.Equals(array, CreateArray(new uint[] { 3 }, new[] { 0, 1 })));
			Assert.False(comparer.Equals(array, CreateArray(new uint[] { 3, 4 }, new[] { 0, 2 })));
			Assert.False(comparer.Equals(array, CreateArray(new uint[] { 3, 4 }, new[] { 0 })));
			Assert.False(comparer.Equals(array, new ArraySig(module.CorLibTypes.Int32, 3U, new uint[] { 3, 4 }, new[] { 0, 1 })));
		}

		// Fixed in this commit: the three kinds of types of the runtime's own signatures had no TypeSig class.
		// The runtime never says that one of them is the same as the normal type when it compares signatures
		// (a method isn't found, measured with .NET 10): a generic parameter by the rid of its row isn't the
		// generic parameter by its number, System.__Canon isn't a reference to the class System.__Canon.
		// The runtime can't compare two of them at all (the file is bad), dnlib says that they're equal.
		[Fact]
		public void TypesOfTheRuntimesOwnSignaturesAreOnlyEqualToTheSameKind() {
			var module = CreateModule("A");
			var int32 = module.CorLibTypes.Int32;
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(new VarZapSig(1), new VarZapSig(1)));
			Assert.Equal(comparer.GetHashCode(new VarZapSig(1)), comparer.GetHashCode(new VarZapSig(1)));
			Assert.False(comparer.Equals(new VarZapSig(1), new VarZapSig(2)));
			Assert.False(comparer.Equals((TypeSig)new VarZapSig(1), new GenericVar(0)));
			Assert.False(comparer.Equals((TypeSig)new VarZapSig(1), new GenericVar(1)));
			Assert.False(comparer.Equals((TypeSig)new GenericMVar(1), new VarZapSig(1)));

			Assert.True(comparer.Equals(new NativeValueTypeZapSig(int32), new NativeValueTypeZapSig(int32)));
			Assert.Equal(comparer.GetHashCode(new NativeValueTypeZapSig(int32)), comparer.GetHashCode(new NativeValueTypeZapSig(int32)));
			Assert.False(comparer.Equals(new NativeValueTypeZapSig(int32), new NativeValueTypeZapSig(module.CorLibTypes.String)));
			Assert.False(comparer.Equals((TypeSig)new NativeValueTypeZapSig(int32), int32));
			Assert.False(comparer.Equals((TypeSig)int32, new NativeValueTypeZapSig(int32)));
			Assert.False(comparer.Equals(new NativeValueTypeZapSig(int32), new NativeValueTypeZapSig(new NativeValueTypeZapSig(int32))));
			Assert.False(comparer.Equals((TypeSig)new NativeValueTypeZapSig(int32), new PinnedSig(int32)));

			var canonRef = new TypeRefUser(module, "System", "__Canon", module.CorLibTypes.AssemblyRef);
			Assert.True(comparer.Equals(new CanonZapSig(), new CanonZapSig()));
			Assert.Equal(comparer.GetHashCode(new CanonZapSig()), comparer.GetHashCode(new CanonZapSig()));
			Assert.False(comparer.Equals((TypeSig)new CanonZapSig(), new ClassSig(canonRef)));
			Assert.False(comparer.Equals((TypeSig)new ClassSig(canonRef), new CanonZapSig()));
			Assert.False(comparer.Equals((IType)canonRef, new CanonZapSig()));
			Assert.False(comparer.Equals((TypeSig)new CanonZapSig(), module.CorLibTypes.Object));

			// A method with one of them isn't the method with the normal type
			var withRid = MethodSig.CreateStatic(module.CorLibTypes.Void, new VarZapSig(1));
			Assert.True(comparer.Equals(withRid, MethodSig.CreateStatic(module.CorLibTypes.Void, new VarZapSig(1))));
			Assert.False(comparer.Equals(withRid, MethodSig.CreateStatic(module.CorLibTypes.Void, new GenericVar(0))));
		}

		// Two element types that only the runtime itself uses: a fixed size array of values, and a type in
		// another module of the assembly. dnlib reads them, so it compares them too.
		[Fact]
		public void ValueArraysAndModuleTypesAreComparedByTheirNumberAndType() {
			var module = CreateModule("A");
			var int32 = module.CorLibTypes.Int32;
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(new ValueArraySig(int32, 4), new ValueArraySig(int32, 4)));
			Assert.Equal(comparer.GetHashCode(new ValueArraySig(int32, 4)), comparer.GetHashCode(new ValueArraySig(int32, 4)));
			Assert.False(comparer.Equals(new ValueArraySig(int32, 4), new ValueArraySig(int32, 5)));
			Assert.False(comparer.Equals(new ValueArraySig(int32, 4), new ValueArraySig(module.CorLibTypes.String, 4)));

			Assert.True(comparer.Equals(new ModuleSig(1, int32), new ModuleSig(1, int32)));
			Assert.Equal(comparer.GetHashCode(new ModuleSig(1, int32)), comparer.GetHashCode(new ModuleSig(1, int32)));
			Assert.False(comparer.Equals(new ModuleSig(1, int32), new ModuleSig(2, int32)));
			Assert.False(comparer.Equals(new ModuleSig(1, int32), new ModuleSig(1, module.CorLibTypes.String)));
			Assert.False(comparer.Equals((TypeSig)new ModuleSig(4, int32), new ValueArraySig(int32, 4)));
		}

		// There's a limit on how deeply the comparer follows nested types. Past it, types aren't equal: no
		// stack overflow.
		[Fact]
		public void TypesThatAreNestedTooDeeplyAreNotEqual() {
			var module = CreateModule("A");
			TypeSig Nest(int depth) {
				TypeSig type = module.CorLibTypes.Int32;
				for (int i = 0; i < depth; i++)
					type = new SZArraySig(type);
				return type;
			}
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(Nest(50), Nest(50)));
			Assert.False(comparer.Equals(Nest(50), Nest(51)));
			Assert.False(comparer.Equals(Nest(5000), Nest(5000)));
			// Hashing doesn't fail either, and the comparer can be used again
			comparer.GetHashCode(Nest(5000));
			Assert.True(comparer.Equals(Nest(50), Nest(50)));
		}

		// Every kind of signature through the overload that takes any signature
		[Fact]
		public void HashCodeOfEveryKindOfSignature() {
			var module = CreateModule("A");
			var types = module.CorLibTypes;
			var comparer = new SigComparer();
			var pairs = new (CallingConventionSig First, CallingConventionSig Same, CallingConventionSig Other)[] {
				(MethodSig.CreateStatic(types.Void, types.Int32), MethodSig.CreateStatic(types.Void, types.Int32), MethodSig.CreateStatic(types.Void, types.String)),
				(new FieldSig(types.Int32), new FieldSig(types.Int32), new FieldSig(types.String)),
				(new LocalSig(types.Int32, types.String), new LocalSig(types.Int32, types.String), new LocalSig(types.Int32)),
				(new GenericInstMethodSig(types.Int32), new GenericInstMethodSig(types.Int32), new GenericInstMethodSig(types.String)),
				(PropertySig.CreateInstance(types.Int32), PropertySig.CreateInstance(types.Int32), PropertySig.CreateInstance(types.String)),
			};

			foreach (var pair in pairs) {
				Assert.True(comparer.Equals(pair.First, pair.Same), pair.First.GetType().Name);
				Assert.Equal(comparer.GetHashCode(pair.First), comparer.GetHashCode(pair.Same));
				Assert.False(comparer.Equals(pair.First, pair.Other), pair.First.GetType().Name);
			}
			// Signatures of different kinds
			Assert.False(comparer.Equals((CallingConventionSig)new FieldSig(types.Int32), new LocalSig(types.Int32)));
			Assert.Equal(0, comparer.GetHashCode((CallingConventionSig?)null));
		}

		// A global method can be referenced through its module (a ModuleRef) or through the global type
		[Fact]
		public void GlobalMethodReferencesThroughAModuleRefAndThroughTheGlobalTypeAreEqual() {
			var module = CreateModule("A");
			var signature = MethodSig.CreateStatic(module.CorLibTypes.Void);
			var throughGlobalType = new MemberRefUser(module, "M", signature, module.GlobalType);
			var throughModule = new MemberRefUser(module, "M", signature, new ModuleRefUser(module, module.Name));
			var throughOtherModule = new MemberRefUser(module, "M", signature, new ModuleRefUser(module, "Other.netmodule"));
			var comparer = new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType);

			Assert.True(comparer.Equals(throughGlobalType, throughModule));
			Assert.True(comparer.Equals(throughModule, throughGlobalType));
			Assert.Equal(comparer.GetHashCode(throughGlobalType), comparer.GetHashCode(throughModule));
			Assert.False(comparer.Equals(throughGlobalType, throughOtherModule));
			Assert.False(comparer.Equals(throughModule, throughOtherModule));
			Assert.True(comparer.Equals(throughOtherModule, new MemberRefUser(module, "M", signature, new ModuleRefUser(module, "OTHER.netmodule"))));
			// A normal type isn't the module
			var typeMember = new MemberRefUser(module, "M", signature, AddType(module, "Ns", "Type"));
			Assert.False(comparer.Equals(typeMember, throughModule));
		}

		// A field of the test class, for the reflection overloads
		public int SampleField;
		public int SampleProperty { get; set; }
		public event EventHandler? SampleEvent;
		void RaiseSampleEvent() => SampleEvent?.Invoke(this, EventArgs.Empty);

		// An object of every type that an Equals() method of SigComparer has a parameter of
		static object[] CreateSamples() {
			var module = CreateModule("A");
			var int32 = module.CorLibTypes.Int32;
			var typeRef = new TypeRefUser(module, "Ns", "Type", module);
			var methodSig = MethodSig.CreateStatic(module.CorLibTypes.Void);
			var genericMethodRef = new MemberRefUser(module, "Generic", MethodSig.CreateStaticGeneric(1, module.CorLibTypes.Void), typeRef);
			return new object[] {
				new TypeDefUser("Ns", "Type"), typeRef, new TypeSpecUser(new SZArraySig(int32)), int32,
				new ExportedTypeUser(module, 0, "Ns", "Type", dnlib.DotNet.TypeAttributes.Public, new AssemblyRefUser("Other")),
				new MethodDefUser("Method", methodSig), new FieldDefUser("Field", new FieldSig(int32)),
				new MemberRefUser(module, "Method", methodSig, typeRef), new MethodSpecUser(genericMethodRef, new GenericInstMethodSig(int32)),
				new PropertyDefUser("Property", PropertySig.CreateStatic(int32)), new EventDefUser("Event", typeRef),
				methodSig, new FieldSig(int32), PropertySig.CreateStatic(int32), new LocalSig(int32), new GenericInstMethodSig(int32),
				new List<TypeSig?> { int32 },
				typeof(SigComparerTests), typeof(SigComparerTests).GetMethod(nameof(CreateSamples), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!,
				typeof(SigComparerTests).GetField(nameof(SampleField))!, typeof(SigComparerTests).GetProperty(nameof(SampleProperty))!,
				typeof(SigComparerTests).GetEvent(nameof(SampleEvent))!,
			};
		}

		// SigComparer has more than 70 Equals() methods, one for every pair of things that can be compared. For each of
		// them: null equals null, and nothing else equals null.
		[Fact]
		public void NullIsOnlyEqualToNullInEveryEqualsMethod() {
			var samples = CreateSamples();
			var equalsMethods = typeof(SigComparer).GetMethods().Where(a => a.Name == "Equals" && a.GetParameters().Length == 2 && a.ReturnType == typeof(bool)).ToList();
			Assert.True(equalsMethods.Count >= 70, $"Only {equalsMethods.Count} Equals() methods were found");

			foreach (var equals in equalsMethods) {
				var parameters = equals.GetParameters();
				// object.Equals(object, object) is a static method. A null Type isn't "nothing": see the next test.
				if (equals.IsStatic || parameters.Any(a => a.ParameterType == typeof(Type)))
					continue;
				var first = samples.FirstOrDefault(a => parameters[0].ParameterType.IsInstanceOfType(a));
				var second = samples.FirstOrDefault(a => parameters[1].ParameterType.IsInstanceOfType(a));
				Assert.True(first is not null && second is not null, $"The test has no sample for {equals}");
				// A new comparer every time: it's a struct, and Invoke() would change a copy
				bool Equals(object? a, object? b) => (bool)equals.Invoke(new SigComparer(), new[] { a, b })!;

				Assert.True(Equals(null, null), equals.ToString());
				Assert.False(Equals(first, null), equals.ToString());
				Assert.False(Equals(null, second), equals.ToString());
			}
		}

		// Reflection has no type for the global methods and fields of a module: their DeclaringType is null. So a
		// null Type is the global type (<Module>) when it's compared with a dnlib type.
		[Fact]
		public void NullReflectionTypeIsTheGlobalType() {
			var module = CreateModule("A");
			var globalType = module.GlobalType!;
			var otherType = new TypeDefUser("Ns", "Type");
			module.Types.Add(otherType);
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(globalType, (Type?)null));
			Assert.True(comparer.Equals((Type?)null, globalType));
			Assert.True(comparer.Equals((IType)globalType, (Type?)null));
			Assert.False(comparer.Equals(otherType, (Type?)null));
			Assert.False(comparer.Equals((IType)otherType, (Type?)null));
			// No dnlib type isn't the global type
			Assert.False(comparer.Equals((TypeDef?)null, (Type?)null));
			Assert.False(comparer.Equals((IType?)null, (Type?)null));
			Assert.False(comparer.Equals((TypeDef?)null, typeof(string)));
		}
	}
}
