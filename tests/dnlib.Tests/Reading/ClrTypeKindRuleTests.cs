// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;
using FieldAttributes = dnlib.DotNet.FieldAttributes;
using MethodAttributes = dnlib.DotNet.MethodAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// What kind of type a type is (a value type, an enum, a delegate, a nested type), the underlying type
	/// of an enum and which methods are constructors isn't stored in the file: a reader works it out from
	/// the base type, the flags and the names. These tests change a type so that the rules don't agree
	/// (an enum with two instance fields, a base type that is only called System.ValueType, a constructor
	/// without its flag) and read the type with dnlib and with the runtime that runs the tests. If the
	/// runtime loads the type, dnlib must say the same about it.
	/// </summary>
	public class ClrTypeKindRuleTests {
		const string Source = @"
public enum E { A = 1 }
public struct S { }
public delegate void D();
public class C {
	public C() { }
	static C() { }
	public void M() { }
}
public class Outer {
	public class Inner { }
}
";
		static readonly byte[] baseImage = CSharpCompiler.Compile(Source).Image;

		// Changes a type. Returns the name of the type that the test reads.
		static string Change(ModuleDef module, string change) {
			TypeDef Find(string name) => module.Find(name, isReflectionName: true)!;
			var enumType = Find("E");
			var structType = Find("S");
			var delegateType = Find("D");
			var classType = Find("C");
			var inner = Find("Outer+Inner");
			var valueField = enumType.Fields.Single(a => !a.IsStatic);
			var valueTypeRef = structType.BaseType!;
			var enumRef = enumType.BaseType!;
			switch (change) {
			case "enum": return "E";
			case "enum with two instance fields":
				enumType.Fields.Add(new FieldDefUser("second", new FieldSig(module.CorLibTypes.Int64), FieldAttributes.Public));
				return "E";
			case "enum with two instance fields, the other one first":
				enumType.Fields.Insert(0, new FieldDefUser("second", new FieldSig(module.CorLibTypes.Int64), FieldAttributes.Public));
				return "E";
			case "enum, the instance field is after the constant":
				enumType.Fields.Remove(valueField);
				enumType.Fields.Add(valueField);
				return "E";
			case "enum without an instance field":
				enumType.Fields.Remove(valueField);
				return "E";
			case "enum, the instance field has another name":
				valueField.Name = "other";
				return "E";
			case "enum, the instance field has no special name flags":
				valueField.Attributes &= ~(FieldAttributes.SpecialName | FieldAttributes.RTSpecialName);
				return "E";
			case "enum, the instance field is a string":
				valueField.FieldSig = new FieldSig(module.CorLibTypes.String);
				return "E";
			case "enum, the instance field is an Int64":
				valueField.FieldSig = new FieldSig(module.CorLibTypes.Int64);
				return "E";
			case "enum that isn't sealed":
				enumType.Attributes &= ~TypeAttributes.Sealed;
				return "E";
			case "enum that is abstract":
				enumType.Attributes |= TypeAttributes.Abstract;
				return "E";
			case "enum whose base type is System.ValueType":
				enumType.BaseType = valueTypeRef;
				return "E";
			case "struct": return "S";
			case "struct whose base type is System.Enum":
				structType.BaseType = enumRef;
				return "S";
			case "struct whose base type is a class of this file with the name System.ValueType": {
				var fake = new TypeDefUser("System", "ValueType", module.CorLibTypes.Object.TypeDefOrRef);
				fake.Attributes = TypeAttributes.Public | TypeAttributes.Abstract;
				module.Types.Add(fake);
				structType.BaseType = fake;
				return "S";
			}
			case "struct whose base type is System.Object":
				structType.BaseType = module.CorLibTypes.Object.TypeDefOrRef;
				return "S";
			case "struct that isn't sealed":
				structType.Attributes &= ~TypeAttributes.Sealed;
				return "S";
			case "struct that is abstract":
				structType.Attributes |= TypeAttributes.Abstract;
				return "S";
			case "struct with the interface flag":
				structType.Attributes |= TypeAttributes.Interface | TypeAttributes.Abstract;
				return "S";
			case "delegate": return "D";
			case "delegate that is abstract":
				delegateType.Attributes |= TypeAttributes.Abstract;
				return "D";
			case "delegate that isn't sealed":
				delegateType.Attributes &= ~TypeAttributes.Sealed;
				return "D";
			case "class whose base type is System.MulticastDelegate":
				classType.BaseType = delegateType.BaseType;
				return "C";
			case "class": return "C";
			case "class, .ctor without the runtime special name flag":
				classType.Methods.Single(a => a.Name == ".ctor").Attributes &= ~MethodAttributes.RTSpecialName;
				return "C";
			case "class, .ctor without the special name flags":
				classType.Methods.Single(a => a.Name == ".ctor").Attributes &= ~(MethodAttributes.RTSpecialName | MethodAttributes.SpecialName);
				return "C";
			case "class, .cctor without the runtime special name flag":
				classType.Methods.Single(a => a.Name == ".cctor").Attributes &= ~MethodAttributes.RTSpecialName;
				return "C";
			case "class, method with the runtime special name flag":
				classType.Methods.Single(a => a.Name == "M").Attributes |= MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
				return "C";
			case "class, .ctor with another name":
				classType.Methods.Single(a => a.Name == ".ctor").Name = "Ctor";
				return "C";
			case "class, .cctor with the name .ctor":
				classType.Methods.Single(a => a.Name == ".cctor").Name = ".ctor";
				return "C";
			case "class, .ctor with the name .cctor":
				classType.Methods.Single(a => a.Name == ".ctor").Name = ".cctor";
				return "C";
			case "class with a nested visibility that isn't in a type":
				classType.Attributes = (classType.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedPublic;
				return "C";
			case "nested class": return "Outer+Inner";
			case "nested class with the visibility of a type that isn't nested":
				inner.Attributes = (inner.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.Public;
				return "Outer+Inner";
			default:
				throw new ArgumentException(change);
			}
		}

		static string Describe(bool isValueType, bool isEnum, string underlyingType, bool isDelegate, int constructors, bool hasStaticConstructor, bool isNested, string? declaringType) =>
			string.Join(" ", new[] {
				isValueType ? "valuetype" : "class",
				isEnum ? "enum:" + underlyingType : null,
				isDelegate ? "delegate" : null,
				$"ctors:{constructors}",
				hasStaticConstructor ? "cctor" : null,
				isNested ? "nested" : null,
				declaringType is null ? null : "in:" + declaringType,
			}.Where(a => a is not null));

		static string ReadWithDnlib(byte[] image, string typeName) {
			using (var module = ModuleDefMD.Load(image)) {
				var type = module.Find(typeName, isReflectionName: true)!;
				return Describe(type.IsValueType, type.IsEnum, type.GetEnumUnderlyingType()?.TypeName ?? "none", type.IsDelegate,
					type.Methods.Count(a => a.IsInstanceConstructor), type.FindStaticConstructor() is not null, type.IsNested, type.DeclaringType?.Name);
			}
		}

		// "delegate": the base type is System.MulticastDelegate. That's the CLR's rule (MethodTableBuilder).
		static string ReadWithTheRuntime(byte[] image, string typeName) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var type = loaded.Assembly.GetType(typeName, throwOnError: true)!;
					return Describe(type.IsValueType, type.IsEnum, type.IsEnum ? Enum.GetUnderlyingType(type).Name : "none", type.BaseType == typeof(MulticastDelegate),
						type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length, type.TypeInitializer is not null, type.IsNested, type.DeclaringType?.Name);
				}
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		[Theory]
		[InlineData("enum", "valuetype enum:Int32 ctors:0", "valuetype enum:Int32 ctors:0")]
		[InlineData("enum with two instance fields", "valuetype enum:Int32 ctors:0", "throws TypeLoadException")]
		[InlineData("enum with two instance fields, the other one first", "valuetype enum:Int64 ctors:0", "throws TypeLoadException")]
		[InlineData("enum, the instance field is after the constant", "valuetype enum:Int32 ctors:0", "valuetype enum:Int32 ctors:0")]
		[InlineData("enum without an instance field", "valuetype enum:none ctors:0", "throws TypeLoadException")]
		[InlineData("enum, the instance field has another name", "valuetype enum:Int32 ctors:0", "valuetype enum:Int32 ctors:0")]
		[InlineData("enum, the instance field has no special name flags", "valuetype enum:Int32 ctors:0", "valuetype enum:Int32 ctors:0")]
		[InlineData("enum, the instance field is a string", "valuetype enum:String ctors:0", "throws TypeLoadException")]
		[InlineData("enum, the instance field is an Int64", "valuetype enum:Int64 ctors:0", "valuetype enum:Int64 ctors:0")]
		[InlineData("enum that isn't sealed", "valuetype enum:Int32 ctors:0", "valuetype enum:Int32 ctors:0")]
		[InlineData("enum that is abstract", "valuetype enum:Int32 ctors:0", "valuetype enum:Int32 ctors:0")]
		[InlineData("enum whose base type is System.ValueType", "valuetype ctors:0", "valuetype ctors:0")]
		[InlineData("struct", "valuetype ctors:0", "valuetype ctors:0")]
		[InlineData("struct whose base type is System.Enum", "valuetype enum:none ctors:0", "throws TypeLoadException")]
		[InlineData("struct whose base type is a class of this file with the name System.ValueType", "class ctors:0", "throws TypeLoadException")]
		[InlineData("struct whose base type is System.Object", "class ctors:0", "class ctors:0")]
		[InlineData("struct that isn't sealed", "valuetype ctors:0", "valuetype ctors:0")]
		[InlineData("struct that is abstract", "valuetype ctors:0", "valuetype ctors:0")]
		[InlineData("struct with the interface flag", "class ctors:0", "throws TypeLoadException")]
		[InlineData("delegate", "class delegate ctors:1", "class delegate ctors:1")]
		// Fixed in this commit: TypeDef.IsDelegate was false for an abstract delegate. For the CLR a class
		// whose base type is System.MulticastDelegate is a delegate.
		[InlineData("delegate that is abstract", "class delegate ctors:1", "class delegate ctors:1")]
		[InlineData("delegate that isn't sealed", "class delegate ctors:1", "throws TypeLoadException")]
		[InlineData("class whose base type is System.MulticastDelegate", "class delegate ctors:1 cctor", "throws TypeLoadException")]
		[InlineData("class", "class ctors:1 cctor", "class ctors:1 cctor")]
		[InlineData("class, .ctor without the runtime special name flag", "class ctors:0 cctor", "class ctors:0 cctor")]
		[InlineData("class, .ctor without the special name flags", "class ctors:0 cctor", "class ctors:0 cctor")]
		[InlineData("class, .cctor without the runtime special name flag", "class ctors:1", "class ctors:1")]
		[InlineData("class, method with the runtime special name flag", "class ctors:1 cctor", "throws TypeLoadException")]
		[InlineData("class, .ctor with another name", "class ctors:0 cctor", "throws TypeLoadException")]
		[InlineData("class, .cctor with the name .ctor", "class ctors:2", "throws TypeLoadException")]
		[InlineData("class, .ctor with the name .cctor", "class ctors:0 cctor", "throws TypeLoadException")]
		[InlineData("class with a nested visibility that isn't in a type", "class ctors:1 cctor", "throws TypeLoadException")]
		[InlineData("nested class", "class ctors:1 nested in:Outer", "class ctors:1 nested in:Outer")]
		[InlineData("nested class with the visibility of a type that isn't nested", "class ctors:1 nested in:Outer", "throws BadImageFormatException")]
		public void KindOfAType(string change, string expectedDnlib, string expectedRuntime) {
			byte[] image;
			string typeName;
			using (var module = ModuleDefMD.Load(baseImage)) {
				typeName = Change(module, change);
				var stream = new MemoryStream();
				module.Write(stream);
				image = stream.ToArray();
			}

			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image, typeName), expectedRuntime, () => ReadWithTheRuntime(image, typeName));
		}
	}
}
