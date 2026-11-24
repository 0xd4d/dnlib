// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Resolving {
	/// <summary>
	/// Finding the method or field that a reference (<see cref="MemberRef"/>) refers to, and finding members by
	/// name and signature in a type and its base types. dnlib follows the CLR's rules (MemberLoader::FindMethod()
	/// and FindField() in the runtime's memberload.cpp).
	/// </summary>
	public class MemberResolutionTests : IDisposable {
		const string Source = @"
public class Base<T> {
	public int Field;
	public T GenericField;
	public T Property { get; set; }
	public event System.Action<T> Changed;
	public Base() { }
	public Base(T value) { }
	public void Method(T value) { }
	public unsafe void Shapes(T[] a, T[,] b, System.Collections.Generic.List<T> c, ref T d, delegate*<T, T[]> e, System.Func<T[], T> f) { }
	public virtual void Virtual() { }
	public static void Static() { }
}
public class Derived : Base<int> {
	public void OwnMethod() { }
}
public class MostDerived : Derived { }
public struct Struct {
	public override string ToString() => null;
}
";
		readonly ModuleDefMD module;
		readonly TypeDef baseType, derived, mostDerived;

		public MemberResolutionTests() {
			module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image);
			baseType = module.Find("Base`1", isReflectionName: true)!;
			derived = module.Find("Derived", isReflectionName: true)!;
			mostDerived = module.Find("MostDerived", isReflectionName: true)!;
		}

		public void Dispose() => module.Dispose();

		ICorLibTypes CorLib => module.CorLibTypes;

		[Fact]
		public void MethodIsFoundInTheTypeItself() {
			var found = derived.FindMethodCheckBaseType("OwnMethod", MethodSig.CreateInstance(CorLib.Void));

			Assert.Same(derived.FindMethod("OwnMethod"), found);
		}

		[Fact]
		public void MethodIsFoundInABaseType() {
			var found = mostDerived.FindMethodCheckBaseType("Virtual", MethodSig.CreateInstance(CorLib.Void));

			Assert.Same(baseType.FindMethod("Virtual"), found);
		}

		[Fact]
		public void MethodWithAnotherSignatureIsNotFound() {
			Assert.Null(derived.FindMethodCheckBaseType("OwnMethod", MethodSig.CreateInstance(CorLib.Int32)));
			Assert.Null(derived.FindMethodCheckBaseType("NoSuchMethod", MethodSig.CreateInstance(CorLib.Void)));
		}

		// Fixed in 9abb86f2: Derived's base type is Base<int>, so Base<T>.Method(T) is Method(int) in Derived.
		// It was only found with the signature Method(!0), which is what the CLR doesn't find.
		[Fact]
		public void MethodOfAGenericBaseTypeIsFoundWithTheGenericArgumentsOfTheBaseType() {
			var expected = baseType.FindMethod("Method");

			Assert.Same(expected, derived.FindMethodCheckBaseType("Method", MethodSig.CreateInstance(CorLib.Void, CorLib.Int32)));
			Assert.Same(expected, mostDerived.FindMethodCheckBaseType("Method", MethodSig.CreateInstance(CorLib.Void, CorLib.Int32)));
			Assert.Null(derived.FindMethodCheckBaseType("Method", MethodSig.CreateInstance(CorLib.Void, new GenericVar(0))));
			Assert.Null(derived.FindMethodCheckBaseType("Method", MethodSig.CreateInstance(CorLib.Void, CorLib.String)));
		}

		// T can be anywhere in a signature: in arrays, generic instances, by reference types and function pointers.
		// Shapes(T[], T[,], List<T>, ref T, delegate*<T, T[]>, Func<T[], T>) is found with int in every place.
		[Fact]
		public void GenericArgumentIsUsedInEveryPartOfASignature() {
			var expected = baseType.FindMethod("Shapes")!;
			var original = expected.MethodSig!.Params;

			// The same signature with another type instead of T
			MethodSig CreateSignature(TypeSig t) {
				var array = (ArraySig)original[1]!;
				var list = ((GenericInstSig)original[2]!).GenericType;
				var functionPointer = ((FnPtrSig)original[4]!).MethodSig!;
				var func = ((GenericInstSig)original[5]!).GenericType;
				return MethodSig.CreateInstance(CorLib.Void,
					new SZArraySig(t),
					new ArraySig(t, array.Rank, array.Sizes, array.LowerBounds),
					new GenericInstSig(list, t),
					new ByRefSig(t),
					new FnPtrSig(new MethodSig(functionPointer.CallingConvention, 0, new SZArraySig(t), t)),
					new GenericInstSig(func, new SZArraySig(t), t));
			}

			// The signature that the test creates is the method's own signature if nothing is replaced
			Assert.Same(expected, baseType.FindMethod("Shapes", CreateSignature(new GenericVar(0))));
			Assert.Same(expected, mostDerived.FindMethodCheckBaseType("Shapes", CreateSignature(CorLib.Int32)));
			Assert.Null(mostDerived.FindMethodCheckBaseType("Shapes", CreateSignature(new GenericVar(0))));
			Assert.Null(mostDerived.FindMethodCheckBaseType("Shapes", CreateSignature(CorLib.String)));
		}

		[Fact]
		public void MethodOfTheGenericTypeItselfIsFoundWithItsGenericParameter() =>
			Assert.Same(baseType.FindMethod("Method"), baseType.FindMethodCheckBaseType("Method", MethodSig.CreateInstance(CorLib.Void, new GenericVar(0))));

		[Fact]
		public void FieldIsFoundInABaseType() =>
			Assert.Same(baseType.FindField("Field"), mostDerived.FindFieldCheckBaseType("Field", new FieldSig(CorLib.Int32)));

		// The same for fields, properties and events: T is int in Derived and MostDerived
		[Fact]
		public void OtherMembersOfAGenericBaseTypeAreFoundWithTheGenericArgumentsOfTheBaseType() {
			var genericParameter = new GenericVar(0);
			TypeSpec ActionOf(TypeSig argument) => new TypeSpecUser(new GenericInstSig(new ClassSig(CorLib.GetTypeRef("System", "Action`1")), argument));

			Assert.Same(baseType.FindField("GenericField"), mostDerived.FindFieldCheckBaseType("GenericField", new FieldSig(CorLib.Int32)));
			Assert.Null(mostDerived.FindFieldCheckBaseType("GenericField", new FieldSig(genericParameter)));
			Assert.Null(mostDerived.FindFieldCheckBaseType("GenericField", new FieldSig(CorLib.String)));

			Assert.Same(baseType.FindProperty("Property"), mostDerived.FindPropertyCheckBaseType("Property", PropertySig.CreateInstance(CorLib.Int32)));
			Assert.Null(mostDerived.FindPropertyCheckBaseType("Property", PropertySig.CreateInstance(genericParameter)));
			Assert.Null(mostDerived.FindPropertyCheckBaseType("Property", PropertySig.CreateInstance(CorLib.String)));
			Assert.Null(mostDerived.FindProperty("Property", PropertySig.CreateInstance(CorLib.Int32)));

			Assert.Same(baseType.FindEvent("Changed"), mostDerived.FindEventCheckBaseType("Changed", ActionOf(CorLib.Int32)));
			Assert.Null(mostDerived.FindEventCheckBaseType("Changed", ActionOf(genericParameter)));
			Assert.Null(mostDerived.FindEventCheckBaseType("Changed", ActionOf(CorLib.String)));
			Assert.Null(mostDerived.FindEvent("Changed", ActionOf(CorLib.Int32)));

			// In the generic type itself, T is its generic parameter
			Assert.Same(baseType.FindProperty("Property"), baseType.FindPropertyCheckBaseType("Property", PropertySig.CreateInstance(genericParameter)));
			Assert.Same(baseType.FindEvent("Changed"), baseType.FindEventCheckBaseType("Changed", ActionOf(genericParameter)));
		}

		// Fixed in ff3b620f
		[Fact]
		public void BaseTypesThatFormACycleDoNotHang() {
			var a = new TypeDefUser("A");
			var b = new TypeDefUser("B", a);
			a.BaseType = b;
			var cycleModule = new ModuleDefUser("Cycle.dll");
			cycleModule.Types.Add(a);
			cycleModule.Types.Add(b);

			Assert.Null(a.FindMethodCheckBaseType("M", MethodSig.CreateInstance(cycleModule.CorLibTypes.Void)));
			Assert.Null(a.FindFieldCheckBaseType("F", new FieldSig(cycleModule.CorLibTypes.Int32)));
			Assert.Null(a.FindMethodCheckBaseType("M"));
		}

		MemberRefUser CreateMethodRef(TypeDef type, string name, MethodSig signature) => new MemberRefUser(module, name, signature, type);

		[Fact]
		public void MemberRefResolvesToAMethodOfItsType() {
			var reference = CreateMethodRef(derived, "OwnMethod", MethodSig.CreateInstance(CorLib.Void));

			Assert.Same(derived.FindMethod("OwnMethod"), derived.Resolve(reference));
		}

		[Fact]
		public void MemberRefResolvesToAnInheritedMethod() {
			var reference = CreateMethodRef(mostDerived, "Virtual", MethodSig.CreateInstance(CorLib.Void));

			Assert.Same(baseType.FindMethod("Virtual"), mostDerived.Resolve(reference));
		}

		[Fact]
		public void MemberRefResolvesToAnInheritedStaticMethod() {
			var reference = CreateMethodRef(derived, "Static", MethodSig.CreateStatic(CorLib.Void));

			Assert.Same(baseType.FindMethod("Static"), derived.Resolve(reference));
		}

		// Fixed in a6e6c94d: the CLR doesn't look for instance constructors in base types
		[Fact]
		public void MemberRefDoesNotResolveToAnInheritedConstructor() {
			// Base<int> has .ctor(int), Derived doesn't
			var reference = CreateMethodRef(derived, ".ctor", MethodSig.CreateInstance(CorLib.Void, CorLib.Int32));

			Assert.Null(derived.Resolve(reference));
		}

		[Fact]
		public void MemberRefResolvesToTheTypesOwnConstructor() {
			var reference = CreateMethodRef(derived, ".ctor", MethodSig.CreateInstance(CorLib.Void));

			Assert.Same(derived.FindDefaultConstructor(), derived.Resolve(reference));
		}

		// Fixed in c5d308de: the CLR also finds a method without access flags. Using it fails later, with an
		// access check, if the reference is in another module.
		[Fact]
		public void MemberRefResolvesToAPrivateScopeMethod() {
			var method = derived.FindMethod("OwnMethod")!;
			method.Access = MethodAttributes.PrivateScope;
			var reference = CreateMethodRef(derived, "OwnMethod", MethodSig.CreateInstance(CorLib.Void));

			Assert.Same(method, derived.Resolve(reference));
		}

		[Fact]
		public void MemberRefWithAnotherSignatureDoesNotResolve() {
			var reference = CreateMethodRef(derived, "OwnMethod", MethodSig.CreateInstance(CorLib.Int32));

			Assert.Null(derived.Resolve(reference));
		}

		[Fact]
		public void FieldMemberRefResolvesToAFieldOfItsType() {
			var reference = new MemberRefUser(module, "Field", new FieldSig(CorLib.Int32), baseType);

			Assert.Same(baseType.FindField("Field"), baseType.Resolve(reference));
		}

		// The names are compared exactly unless the options say that the case of a name doesn't matter
		[Fact]
		public void MembersAreFoundWithoutCaseIfTheOptionsSaySo() {
			const SigComparerOptions methodsAndFields = SigComparerOptions.CaseInsensitiveMethodFieldNames;
			var methodSig = MethodSig.CreateInstance(CorLib.Void);
			var fieldSig = new FieldSig(CorLib.Int32);
			var propertySig = PropertySig.CreateInstance(new GenericVar(0));
			var eventType = baseType.FindEvent("Changed")!.EventType;

			Assert.Null(baseType.FindMethod("VIRTUAL", methodSig));
			Assert.Null(baseType.FindMethod("VIRTUAL", methodSig, SigComparerOptions.CaseInsensitivePropertyNames));
			Assert.Same(baseType.FindMethod("Virtual"), baseType.FindMethod("VIRTUAL", methodSig, methodsAndFields));
			Assert.Same(baseType.FindMethod("Virtual"), baseType.FindMethod("VIRTUAL", methodSig, methodsAndFields, module));

			Assert.Null(baseType.FindField("FIELD", fieldSig));
			Assert.Same(baseType.FindField("Field"), baseType.FindField("FIELD", fieldSig, methodsAndFields));
			Assert.Same(baseType.FindField("Field"), baseType.FindField("FIELD", fieldSig, methodsAndFields, module));

			Assert.Null(baseType.FindProperty("PROPERTY", propertySig));
			Assert.Null(baseType.FindProperty("PROPERTY", propertySig, methodsAndFields));
			Assert.Same(baseType.FindProperty("Property"), baseType.FindProperty("PROPERTY", propertySig, SigComparerOptions.CaseInsensitivePropertyNames));
			Assert.Same(baseType.FindProperty("Property"), baseType.FindProperty("PROPERTY", propertySig, SigComparerOptions.CaseInsensitivePropertyNames, module));

			Assert.Null(baseType.FindEvent("CHANGED", eventType));
			Assert.Same(baseType.FindEvent("Changed"), baseType.FindEvent("Changed", eventType));
			Assert.Same(baseType.FindEvent("Changed"), baseType.FindEvent("CHANGED", eventType, SigComparerOptions.CaseInsensitiveEventNames));
			Assert.Same(baseType.FindEvent("Changed"), baseType.FindEvent("CHANGED", eventType, SigComparerOptions.CaseInsensitiveEventNames, module));
		}

		[Fact]
		public void MembersOfABaseTypeAreFoundWithoutCaseIfTheOptionsSaySo() {
			const SigComparerOptions methodsAndFields = SigComparerOptions.CaseInsensitiveMethodFieldNames;
			var methodSig = MethodSig.CreateInstance(CorLib.Void);
			var fieldSig = new FieldSig(CorLib.Int32);
			// Derived is a Base<int>: the type of the property is int there
			var propertySig = PropertySig.CreateInstance(CorLib.Int32);

			Assert.Null(mostDerived.FindMethodCheckBaseType("VIRTUAL", methodSig));
			Assert.Same(baseType.FindMethod("Virtual"), mostDerived.FindMethodCheckBaseType("VIRTUAL", methodSig, methodsAndFields));
			Assert.Same(derived.FindMethod("OwnMethod"), mostDerived.FindMethodCheckBaseType("ownmethod", methodSig, methodsAndFields, module));

			Assert.Null(mostDerived.FindFieldCheckBaseType("FIELD", fieldSig));
			Assert.Same(baseType.FindField("Field"), mostDerived.FindFieldCheckBaseType("FIELD", fieldSig, methodsAndFields));
			Assert.Same(baseType.FindField("Field"), mostDerived.FindFieldCheckBaseType("FIELD", fieldSig, methodsAndFields, module));

			Assert.Null(mostDerived.FindPropertyCheckBaseType("PROPERTY", propertySig));
			Assert.Same(baseType.FindProperty("Property"), mostDerived.FindPropertyCheckBaseType("Property", propertySig));
			Assert.Same(baseType.FindProperty("Property"), mostDerived.FindPropertyCheckBaseType("PROPERTY", propertySig, SigComparerOptions.CaseInsensitivePropertyNames));
			Assert.Same(baseType.FindProperty("Property"), mostDerived.FindPropertyCheckBaseType("PROPERTY", propertySig, SigComparerOptions.CaseInsensitivePropertyNames, module));

			Assert.Same(baseType.FindEvent("Changed"), mostDerived.FindEventCheckBaseType("Changed"));
			Assert.Null(mostDerived.FindEventCheckBaseType("CHANGED"));
		}
	}
}
