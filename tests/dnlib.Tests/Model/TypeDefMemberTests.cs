// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// The methods of <see cref="TypeDef"/> that find and remove its members, and the properties that say
	/// what kind of type it is
	/// </summary>
	public sealed class TypeDefMemberTests : IDisposable {
		const string Source = @"
using System;

public class Base {
	public int BaseField;
	public event EventHandler BaseEvent;
	public string BaseProperty { get; set; }
	public void BaseMethod() { BaseEvent?.Invoke(this, EventArgs.Empty); }
}

public class Derived : Base {
	static readonly int created = 1;
	public int Field;
	public string Text;
	public event EventHandler Changed;
	public int Number { get; set; }
	public string this[int index] { get { return Text; } }
	public Derived() { }
	public Derived(int number) { Number = number + created; }
	public void Method() { Changed?.Invoke(this, EventArgs.Empty); }
	public void Method(int value) { }
	public int Method(string value) { return 0; }
}

public class NoConstructors { }
public static class StaticClass { }
public struct Struct { public int Value; }
public enum ByteEnum : byte { A, B }
public interface IInterface { }
public delegate void Callback();
public abstract class AbstractClass { }
public sealed class SealedClass { public class Nested { } }
";

		readonly ModuleDefMD module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image);

		public void Dispose() => module.Dispose();

		TypeDef Find(string name) => module.Find(name, isReflectionName: true)!;
		ICorLibTypes CorLibTypes => module.CorLibTypes;

		[Fact]
		public void MethodIsFoundByNameAndSignature() {
			var derived = Find("Derived");

			Assert.Equal(3, derived.FindMethods("Method").Count());
			Assert.Equal("Method", derived.FindMethod("Method")!.Name);
			var withInt = derived.FindMethod("Method", MethodSig.CreateInstance(CorLibTypes.Void, CorLibTypes.Int32))!;
			Assert.Equal("System.Void Derived::Method(System.Int32)", withInt.FullName);
			var withString = derived.FindMethod("Method", MethodSig.CreateInstance(CorLibTypes.Int32, CorLibTypes.String))!;
			Assert.Equal("System.Int32 Derived::Method(System.String)", withString.FullName);

			// Another return type, another name, a static method
			Assert.Null(derived.FindMethod("Method", MethodSig.CreateInstance(CorLibTypes.Int32, CorLibTypes.Int32)));
			Assert.Null(derived.FindMethod("Missing", MethodSig.CreateInstance(CorLibTypes.Void)));
			Assert.Null(derived.FindMethod("Method", MethodSig.CreateStatic(CorLibTypes.Void)));
			Assert.Null(derived.FindMethod("Missing"));
			Assert.Null(derived.FindMethod(null));
			// The return type is ignored if the option is set
			Assert.NotNull(derived.FindMethod("Method", MethodSig.CreateInstance(CorLibTypes.Int32, CorLibTypes.Int32), SigComparerOptions.DontCompareReturnType));
		}

		// The methods that return every member with a name: only methods can have the same name in C#
		[Fact]
		public void AllMembersWithANameAreFound() {
			var derived = Find("Derived");

			Assert.Equal(3, derived.FindMethods("Method").Count());
			Assert.All(derived.FindMethods("Method"), a => Assert.Equal("Method", a.Name));
			Assert.Equal(new[] { derived.FindField("Field") }, derived.FindFields("Field"));
			Assert.Equal(new[] { derived.FindField("Field")! }, derived.GetFields("Field"));
			Assert.Same(derived.FindField("Field"), derived.GetField("Field"));
			Assert.Equal(new[] { derived.FindProperty("Number") }, derived.FindProperties("Number"));
			Assert.Equal(new[] { derived.FindEvent("Changed") }, derived.FindEvents("Changed"));

			// Not the members of the base type, and no member has no name
			Assert.Empty(derived.FindMethods("BaseMethod"));
			Assert.Empty(derived.FindFields("BaseField"));
			Assert.Empty(derived.GetFields("BaseField"));
			Assert.Null(derived.GetField("BaseField"));
			Assert.Empty(derived.FindProperties("BaseProperty"));
			Assert.Empty(derived.FindEvents("BaseEvent"));
			Assert.Empty(derived.GetFields(null));
			Assert.Empty(derived.FindMethods(null));
		}

		[Fact]
		public void MethodOfABaseTypeIsOnlyFoundIfTheBaseTypesAreChecked() {
			var derived = Find("Derived");
			var signature = MethodSig.CreateInstance(CorLibTypes.Void);

			Assert.Null(derived.FindMethod("BaseMethod"));
			Assert.Null(derived.FindMethod("BaseMethod", signature));
			Assert.Same(Find("Base").FindMethod("BaseMethod"), derived.FindMethodCheckBaseType("BaseMethod"));
			Assert.Same(Find("Base").FindMethod("BaseMethod"), derived.FindMethodCheckBaseType("BaseMethod", signature));
			Assert.Same(derived.FindMethod("Method", signature), derived.FindMethodCheckBaseType("Method", signature));
			Assert.Null(derived.FindMethodCheckBaseType("Missing"));
		}

		[Fact]
		public void Constructors() {
			var derived = Find("Derived");

			Assert.Equal(2, derived.FindInstanceConstructors().Count());
			Assert.Equal(3, derived.FindConstructors().Count());
			Assert.Empty(derived.FindDefaultConstructor()!.MethodSig!.Params);
			var staticConstructor = derived.FindStaticConstructor()!;
			Assert.Equal(".cctor", staticConstructor.Name);
			Assert.True(staticConstructor.IsStatic);
			Assert.Same(staticConstructor, derived.FindOrCreateStaticConstructor());

			// The compiler adds a default constructor to a class without constructors, but not to a struct
			Assert.NotNull(Find("NoConstructors").FindDefaultConstructor());
			Assert.Null(Find("Struct").FindDefaultConstructor());
			Assert.Null(Find("NoConstructors").FindStaticConstructor());
		}

		[Fact]
		public void StaticConstructorIsCreatedIfThereIsNone() {
			var type = Find("NoConstructors");
			int methodCount = type.Methods.Count;

			var created = type.FindOrCreateStaticConstructor();

			Assert.Equal(methodCount + 1, type.Methods.Count);
			Assert.Equal(".cctor", created.Name);
			Assert.True(created.IsStatic && created.IsSpecialName && created.IsRuntimeSpecialName);
			Assert.Equal(dnlib.DotNet.Emit.Code.Ret, created.Body!.Instructions.Single().OpCode.Code);
			Assert.Same(created, type.FindStaticConstructor());
		}

		[Fact]
		public void FieldsPropertiesAndEventsAreFoundByNameAndType() {
			var derived = Find("Derived");

			Assert.Equal("Field", derived.FindField("Field")!.Name);
			Assert.NotNull(derived.FindField("Field", new FieldSig(CorLibTypes.Int32)));
			Assert.Null(derived.FindField("Field", new FieldSig(CorLibTypes.String)));
			Assert.Single(derived.FindFields("Text"));
			Assert.Same(derived.FindField("Field"), derived.GetField("Field"));
			Assert.Null(derived.FindField("BaseField"));
			Assert.Same(Find("Base").FindField("BaseField"), derived.FindFieldCheckBaseType("BaseField"));
			Assert.Same(Find("Base").FindField("BaseField"), derived.FindFieldCheckBaseType("BaseField", new FieldSig(CorLibTypes.Int32)));

			Assert.Equal("Number", derived.FindProperty("Number")!.Name);
			Assert.NotNull(derived.FindProperty("Number", PropertySig.CreateInstance(CorLibTypes.Int32)));
			Assert.Null(derived.FindProperty("Number", PropertySig.CreateInstance(CorLibTypes.String)));
			// The indexer has a parameter
			Assert.NotNull(derived.FindProperty("Item", PropertySig.CreateInstance(CorLibTypes.String, CorLibTypes.Int32)));
			Assert.Single(derived.FindProperties("Item"));
			Assert.Null(derived.FindProperty("BaseProperty"));
			Assert.Same(Find("Base").FindProperty("BaseProperty"), derived.FindPropertyCheckBaseType("BaseProperty"));

			var changed = derived.FindEvent("Changed")!;
			Assert.Equal("System.EventHandler", changed.EventType!.FullName);
			Assert.Same(changed, derived.FindEvent("Changed", changed.EventType));
			Assert.Null(derived.FindEvent("Changed", CorLibTypes.Int32.TypeDefOrRef));
			Assert.Single(derived.FindEvents("Changed"));
			Assert.Null(derived.FindEvent("BaseEvent"));
			Assert.Same(Find("Base").FindEvent("BaseEvent"), derived.FindEventCheckBaseType("BaseEvent"));
		}

		[Fact]
		public void RemovingAMethodAlsoRemovesItFromItsPropertyOrEvent() {
			var derived = Find("Derived");
			var number = derived.FindProperty("Number")!;
			var changed = derived.FindEvent("Changed")!;
			var getter = number.GetMethod;
			var setter = number.SetMethod;

			derived.Remove(getter);
			Assert.DoesNotContain(getter, derived.Methods);
			Assert.Null(number.GetMethod);
			Assert.Same(setter, number.SetMethod);
			Assert.Contains(number, derived.Properties);

			// The property has no methods anymore: it's removed too if that's asked for
			derived.Remove(setter, removeEmptyPropertiesEvents: true);
			Assert.DoesNotContain(number, derived.Properties);
			Assert.Contains(changed, derived.Events);

			derived.Remove(changed.AddMethod);
			Assert.Null(changed.AddMethod);
			derived.Remove(changed.RemoveMethod, removeEmptyPropertiesEvents: true);
			Assert.DoesNotContain(changed, derived.Events);

			derived.Remove(null);
		}

		// An event's invoke method (C# has none, but IL has .fire) is removed from the event too
		[Fact]
		public void RemovingAnInvokeMethodRemovesItFromItsEvent() {
			var derived = Find("Derived");
			var changed = derived.FindEvent("Changed")!;
			var raise = derived.FindMethod("Method", MethodSig.CreateInstance(CorLibTypes.Void))!;
			changed.InvokeMethod = raise;

			derived.Remove(raise);

			Assert.Null(changed.InvokeMethod);
			Assert.Contains(changed, derived.Events);
		}

		[Fact]
		public void KindsOfTypes() {
			Assert.True(Find("Derived").IsClass);
			Assert.False(Find("Derived").IsValueType);
			Assert.True(Find("Struct").IsValueType);
			Assert.False(Find("Struct").IsEnum);
			Assert.True(Find("ByteEnum").IsEnum);
			Assert.True(Find("ByteEnum").IsValueType);
			Assert.True(Find("IInterface").IsInterface);
			Assert.False(Find("IInterface").IsClass);
			Assert.True(Find("Callback").IsDelegate);
			Assert.False(Find("Derived").IsDelegate);
			Assert.True(Find("AbstractClass").IsAbstract);
			Assert.True(Find("SealedClass").IsSealed);
			// A static class is abstract and sealed
			Assert.True(Find("StaticClass").IsAbstract && Find("StaticClass").IsSealed);
			Assert.True(Find("SealedClass+Nested").IsNested);
			Assert.True(Find("SealedClass+Nested").IsNestedPublic);
			Assert.Same(Find("SealedClass"), Find("SealedClass+Nested").DeclaringType);
			Assert.False(Find("SealedClass").IsNested);
		}

		[Fact]
		public void UnderlyingTypeOfAnEnum() {
			Assert.Equal(ElementType.U1, Find("ByteEnum").GetEnumUnderlyingType()!.ElementType);
			// It's the type of the first instance field, the method doesn't check that the type is an enum
			Assert.Equal(ElementType.I4, Find("Struct").GetEnumUnderlyingType()!.ElementType);
			Assert.Null(Find("StaticClass").GetEnumUnderlyingType());
		}

		[Fact]
		public void BaseTypes() {
			var derived = Find("Derived");

			Assert.Equal("Base", derived.BaseType!.FullName);
			Assert.Same(Find("Base"), derived.BaseType.ResolveTypeDef());
			Assert.Equal("System.Object", Find("Base").BaseType!.FullName);
			Assert.Equal("System.ValueType", Find("Struct").BaseType!.FullName);
			Assert.Equal("System.Enum", Find("ByteEnum").BaseType!.FullName);
			Assert.Null(Find("IInterface").BaseType);
			Assert.Equal("System.MulticastDelegate", Find("Callback").BaseType!.FullName);
		}

		// Members are also found in a base type; the variants without a type or signature only use the name
		[Fact]
		public void MembersAreFoundInABaseType() {
			var derived = Find("Derived");
			var handler = Find("Base").FindEvent("BaseEvent")!.EventType;

			Assert.Equal("BaseField", derived.FindFieldCheckBaseType("BaseField")!.Name);
			Assert.Null(derived.FindFieldCheckBaseType("DoesNotExist"));
			Assert.Equal("BaseProperty", derived.FindPropertyCheckBaseType("BaseProperty")!.Name);
			Assert.Null(derived.FindPropertyCheckBaseType("DoesNotExist"));
			Assert.Equal("BaseEvent", derived.FindEventCheckBaseType("BaseEvent", handler)!.Name);
			Assert.Equal("BaseEvent", derived.FindEventCheckBaseType("BaseEvent", handler, SigComparerOptions.DontCompareTypeScope)!.Name);
			Assert.Null(derived.FindEventCheckBaseType("DoesNotExist", handler));
		}

		// A null name or signature finds nothing
		[Fact]
		public void NothingIsFoundWithoutANameOrASignature() {
			var derived = Find("Derived");
			var method = derived.FindMethod("Method")!;
			var field = derived.FindField("Field")!;

			Assert.Null(derived.FindMethod(null, method.MethodSig, 0, null));
			Assert.Null(derived.FindField(null, field.FieldSig, 0, null));
			Assert.Null(derived.FindEvent(null, Find("Base").FindEvent("BaseEvent")!.EventType, 0, null));
			Assert.Null(derived.FindProperty(null, derived.FindProperty("Number")!.PropertySig, 0, null));
			Assert.Null(derived.Resolve(null, 0));
		}

		// A MemberRef with a signature that is neither a method nor a field signature resolves to nothing
		[Fact]
		public void AMemberRefWithAnotherSignatureIsNotResolved() {
			var derived = Find("Derived");
			var memberRef = new MemberRefUser(module, "Field", (FieldSig?)null, derived);

			Assert.Null(derived.Resolve(memberRef));
			memberRef.Signature = derived.FindField("Field")!.FieldSig;
			Assert.Same(derived.FindField("Field"), derived.Resolve(memberRef));
		}

		[Fact]
		public void GetTypesReturnsAllNestedTypes() {
			var sealedClass = Find("SealedClass");

			Assert.Equal(new[] { "SealedClass/Nested" }, sealedClass.GetTypes().Select(a => a.FullName));
			Assert.Empty(Find("Struct").GetTypes());
		}

		[Fact]
		public void NumberOfGenericParametersOfAType() {
			var type = new TypeDefUser("Generic`2");
			type.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			type.GenericParameters.Add(new GenericParamUser(1, GenericParamAttributes.NonVariant, "U"));

			Assert.Equal(2, ((IGenericParameterProvider)type).NumberOfGenericParameters);
			Assert.Equal(0, ((IGenericParameterProvider)Find("Struct")).NumberOfGenericParameters);
			Assert.Equal(0, ((ITypeOrMethodDef)type).TypeOrMethodDefTag);
		}

		// Setting the packing size or the class size creates a ClassLayout row
		[Fact]
		public void SettingTheLayoutCreatesAClassLayout() {
			var type = new TypeDefUser("Type");
			Assert.Null(type.ClassLayout);
			Assert.Equal(ushort.MaxValue, type.PackingSize);

			type.PackingSize = 4;
			Assert.NotNull(type.ClassLayout);
			Assert.Equal(4, type.PackingSize);
			Assert.Equal(0U, type.ClassSize);

			var other = new TypeDefUser("Other");
			other.ClassSize = 12;
			Assert.Equal(12U, other.ClassSize);
			Assert.Equal(0, other.PackingSize);
		}

		// A type in the core library itself derives from a TypeDef, not a TypeRef
		[Fact]
		public void EnumsAndDelegatesOfTheCoreLibrary() {
			var corLib = new ModuleDefUser("mscorlib.dll");
			new AssemblyDefUser("mscorlib", new Version(4, 0, 0, 0)).Modules.Add(corLib);
			var systemObject = new TypeDefUser("System", "Object");
			var systemEnum = new TypeDefUser("System", "Enum", systemObject);
			var multicastDelegate = new TypeDefUser("System", "MulticastDelegate", systemObject);
			var anEnum = new TypeDefUser("System", "AnEnum", systemEnum) { Attributes = TypeAttributes.Sealed };
			var aDelegate = new TypeDefUser("System", "ADelegate", multicastDelegate) { Attributes = TypeAttributes.Sealed };
			var other = new TypeDefUser("System", "Other", systemObject);
			foreach (var type in new[] { systemObject, systemEnum, multicastDelegate, anEnum, aDelegate, other })
				corLib.Types.Add(type);

			Assert.True(anEnum.IsEnum);
			Assert.False(anEnum.IsDelegate);
			Assert.True(aDelegate.IsDelegate);
			Assert.False(aDelegate.IsEnum);
			Assert.False(other.IsEnum);
			Assert.False(other.IsDelegate);
		}

		// A base type that's a TypeSpec (eg. a generic instance) isn't System.Enum or System.MulticastDelegate
		[Fact]
		public void ATypeSpecBaseTypeIsNeitherAnEnumNorADelegate() {
			var enumRef = module.CorLibTypes.GetTypeRef("System", "Enum");
			var type = new TypeDefUser("Ns", "Type", new TypeSpecUser(new ClassSig(enumRef)));
			module.Types.Add(type);

			Assert.False(type.IsEnum);
			Assert.False(type.IsDelegate);
		}
	}
}
