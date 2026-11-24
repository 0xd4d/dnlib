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
	}
}
