// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.CustomAttributes {
	/// <summary>
	/// Custom attributes that are created with dnlib's API: every kind of argument (numbers, strings, enums,
	/// types, arrays, values in an <c>object</c>) and named arguments (fields and properties). dnlib writes
	/// them, and the runtime creates the attribute objects from what was written: the test reads their values
	/// with reflection.
	/// </summary>
	public sealed class CustomAttributeValueTests : IDisposable {
		const string Source = @"
using System;

public enum Level : short { Low = -1, None, High }

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class ValuesAttribute : Attribute {
	public object[] ConstructorArguments;
	public int IntField;
	public string StringField;
	public Level EnumField;
	public Type TypeField;
	public object ObjectField;
	public int[] IntArrayField;
	public object[] ObjectArrayField;
	public string StringProperty { get; set; }
	public Type[] TypeArrayProperty { get; set; }
	public Level[] EnumArrayProperty { get; set; }
	public object ObjectProperty { get; set; }

	public ValuesAttribute() { ConstructorArguments = new object[0]; }
	public ValuesAttribute(bool a, char b, sbyte c, byte d, short e, ushort f, int g, uint h, long i, ulong j, float k, double l, string m) {
		ConstructorArguments = new object[] { a, b, c, d, e, f, g, h, i, j, k, l, m };
	}
	public ValuesAttribute(Level level, Type type, object boxed) { ConstructorArguments = new object[] { level, type, boxed }; }
	public ValuesAttribute(int[] numbers, string[] strings, object[] objects) { ConstructorArguments = new object[] { numbers, strings, objects }; }
}

public class Target { }
";

		readonly ModuleDefMD module;
		readonly TypeDef attributeType;
		readonly TypeDef target;

		public CustomAttributeValueTests() {
			module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image);
			attributeType = module.Find("ValuesAttribute", isReflectionName: true)!;
			target = module.Find("Target", isReflectionName: true)!;
		}

		public void Dispose() => module.Dispose();

		ICorLibTypes Types => module.CorLibTypes;
		TypeSig SystemType => new ClassSig(Types.GetTypeRef("System", "Type"));
		TypeSig LevelType => module.Find("Level", isReflectionName: true).ToTypeSig()!;

		// The constructor of ValuesAttribute whose first parameter has this type (null: no parameters)
		MethodDef GetConstructor(string? firstParameterType) =>
			attributeType.FindConstructors().Single(a => a.MethodSig!.Params.FirstOrDefault()?.FullName == firstParameterType);

		CAArgument Argument(TypeSig type, object? value) => new CAArgument(type, value);
		CAArgument Text(string? value) => new CAArgument(Types.String, value is null ? null : new UTF8String(value));
		// An array's value is a list of arguments
		CAArgument Array(TypeSig elementType, params CAArgument[] elements) => new CAArgument(new SZArraySig(elementType), elements.ToList());
		// A value for an 'object' parameter, field or array element is an argument with the type of the value:
		// the parameter's type (object) is in the constructor's signature, or in the named argument
		static CAArgument Boxed(CAArgument value) => value;

		// Adds the attribute to the Target class, writes the module, loads it and returns the attribute object
		// that the runtime creates
		void WriteAndRead(CustomAttribute attribute, Action<object> check) {
			target.CustomAttributes.Add(attribute);
			var stream = new MemoryStream();
			module.Write(stream);

			using (var loaded = new LoadedAssembly(stream.ToArray())) {
				var created = loaded.Assembly.GetType("Target")!.GetCustomAttributes(inherit: false).Single();
				check(created);
			}
		}

		static object? GetMember(object attribute, string name) {
			var type = attribute.GetType();
			return type.GetField(name) is { } field ? field.GetValue(attribute) : type.GetProperty(name)!.GetValue(attribute, null);
		}

		static object[] GetConstructorArguments(object attribute) => (object[])GetMember(attribute, "ConstructorArguments")!;

		[Fact]
		public void PrimitiveAndStringArguments() {
			var attribute = new CustomAttribute(GetConstructor("System.Boolean"));
			attribute.ConstructorArguments.Add(Argument(Types.Boolean, true));
			attribute.ConstructorArguments.Add(Argument(Types.Char, '€'));
			attribute.ConstructorArguments.Add(Argument(Types.SByte, (sbyte)-5));
			attribute.ConstructorArguments.Add(Argument(Types.Byte, (byte)250));
			attribute.ConstructorArguments.Add(Argument(Types.Int16, (short)-300));
			attribute.ConstructorArguments.Add(Argument(Types.UInt16, (ushort)60000));
			attribute.ConstructorArguments.Add(Argument(Types.Int32, int.MinValue));
			attribute.ConstructorArguments.Add(Argument(Types.UInt32, uint.MaxValue));
			attribute.ConstructorArguments.Add(Argument(Types.Int64, long.MinValue));
			attribute.ConstructorArguments.Add(Argument(Types.UInt64, ulong.MaxValue));
			attribute.ConstructorArguments.Add(Argument(Types.Single, 1.5f));
			attribute.ConstructorArguments.Add(Argument(Types.Double, -2.25));
			attribute.ConstructorArguments.Add(Text("text € \0 end"));

			WriteAndRead(attribute, created =>
				Assert.Equal(new object[] { true, '€', (sbyte)-5, (byte)250, (short)-300, (ushort)60000, int.MinValue, uint.MaxValue, long.MinValue, ulong.MaxValue, 1.5f, -2.25, "text € \0 end" },
					GetConstructorArguments(created)));
		}

		[Fact]
		public void EnumTypeAndBoxedArguments() {
			var attribute = new CustomAttribute(GetConstructor("Level"));
			// An enum's value is a value of its underlying type
			attribute.ConstructorArguments.Add(Argument(LevelType, (short)1));
			attribute.ConstructorArguments.Add(Argument(SystemType, new SZArraySig(Types.Int32)));
			attribute.ConstructorArguments.Add(Boxed(Argument(Types.Int64, 123L)));

			WriteAndRead(attribute, created => {
				var arguments = GetConstructorArguments(created);
				Assert.Equal("High", arguments[0].ToString());
				Assert.Equal(typeof(int[]), arguments[1]);
				Assert.Equal(123L, arguments[2]);
			});
		}

		[Fact]
		public void NullArguments() {
			var attribute = new CustomAttribute(GetConstructor("Level"));
			attribute.ConstructorArguments.Add(Argument(LevelType, (short)0));
			attribute.ConstructorArguments.Add(Argument(SystemType, null));
			// A null in an 'object' is a null string
			attribute.ConstructorArguments.Add(Boxed(Text(null)));

			WriteAndRead(attribute, created => {
				var arguments = GetConstructorArguments(created);
				Assert.Null(arguments[1]);
				Assert.Null(arguments[2]);
			});
		}

		[Fact]
		public void ArrayArguments() {
			var attribute = new CustomAttribute(GetConstructor("System.Int32[]"));
			attribute.ConstructorArguments.Add(Array(Types.Int32, Argument(Types.Int32, 1), Argument(Types.Int32, 2), Argument(Types.Int32, 3)));
			attribute.ConstructorArguments.Add(Array(Types.String, Text("a"), Text(null), Text("")));
			// Every element of an object[] has its own type
			attribute.ConstructorArguments.Add(Array(Types.Object,
				Boxed(Argument(Types.Int32, 5)),
				Boxed(Text("five")),
				Boxed(Argument(SystemType, Types.String)),
				Boxed(Argument(LevelType, (short)-1)),
				Boxed(Array(Types.Byte, Argument(Types.Byte, (byte)1), Argument(Types.Byte, (byte)2)))));

			WriteAndRead(attribute, created => {
				var arguments = GetConstructorArguments(created);
				Assert.Equal(new[] { 1, 2, 3 }, arguments[0]);
				Assert.Equal(new[] { "a", null, "" }, arguments[1]);
				var objects = (object[])arguments[2];
				Assert.Equal(5, objects[0]);
				Assert.Equal("five", objects[1]);
				Assert.Equal(typeof(string), objects[2]);
				Assert.Equal("Low", objects[3].ToString());
				Assert.Equal(new byte[] { 1, 2 }, objects[4]);
			});
		}

		[Fact]
		public void NullAndEmptyArrays() {
			var attribute = new CustomAttribute(GetConstructor("System.Int32[]"));
			// A null array has no list of elements
			attribute.ConstructorArguments.Add(new CAArgument(new SZArraySig(Types.Int32), null));
			attribute.ConstructorArguments.Add(Array(Types.String));
			attribute.ConstructorArguments.Add(Array(Types.Object));

			WriteAndRead(attribute, created => {
				var arguments = GetConstructorArguments(created);
				Assert.Null(arguments[0]);
				Assert.Empty((string[])arguments[1]);
				Assert.Empty((object[])arguments[2]);
			});
		}

		// declaredType: the type of the field or property if it isn't the type of the value (it's 'object')
		CANamedArgument Field(string name, CAArgument argument, TypeSig? declaredType = null) => new CANamedArgument(isField: true, declaredType ?? argument.Type, name, argument);
		CANamedArgument Property(string name, CAArgument argument, TypeSig? declaredType = null) => new CANamedArgument(isField: false, declaredType ?? argument.Type, name, argument);

		[Fact]
		public void NamedFieldsAndProperties() {
			var attribute = new CustomAttribute(GetConstructor(null));
			attribute.NamedArguments.Add(Field("IntField", Argument(Types.Int32, 42)));
			attribute.NamedArguments.Add(Field("StringField", Text("field")));
			attribute.NamedArguments.Add(Field("EnumField", Argument(LevelType, (short)1)));
			attribute.NamedArguments.Add(Field("TypeField", Argument(SystemType, target.ToTypeSig())));
			attribute.NamedArguments.Add(Field("ObjectField", Boxed(Argument(Types.Double, 0.5)), Types.Object));
			attribute.NamedArguments.Add(Field("IntArrayField", Array(Types.Int32, Argument(Types.Int32, 7))));
			attribute.NamedArguments.Add(Field("ObjectArrayField", Array(Types.Object, Boxed(Text("x")), Boxed(Argument(Types.Boolean, true)))));
			attribute.NamedArguments.Add(Property("StringProperty", Text("property")));
			attribute.NamedArguments.Add(Property("TypeArrayProperty", Array(SystemType, Argument(SystemType, Types.Int32), Argument(SystemType, null))));
			attribute.NamedArguments.Add(Property("EnumArrayProperty", Array(LevelType, Argument(LevelType, (short)-1), Argument(LevelType, (short)1))));
			attribute.NamedArguments.Add(Property("ObjectProperty", Boxed(Argument(LevelType, (short)1)), Types.Object));

			WriteAndRead(attribute, created => {
				Assert.Equal(42, GetMember(created, "IntField"));
				Assert.Equal("field", GetMember(created, "StringField"));
				Assert.Equal("High", GetMember(created, "EnumField")!.ToString());
				Assert.Equal("Target", ((Type)GetMember(created, "TypeField")!).FullName);
				Assert.Equal(0.5, GetMember(created, "ObjectField"));
				Assert.Equal(new[] { 7 }, GetMember(created, "IntArrayField"));
				Assert.Equal(new object[] { "x", true }, GetMember(created, "ObjectArrayField"));
				Assert.Equal("property", GetMember(created, "StringProperty"));
				Assert.Equal(new Type?[] { typeof(int), null }, GetMember(created, "TypeArrayProperty"));
				Assert.Equal(new[] { "Low", "High" }, ((Array)GetMember(created, "EnumArrayProperty")!).Cast<object>().Select(a => a.ToString()));
				Assert.Equal("High", GetMember(created, "ObjectProperty")!.ToString());
			});
		}

		// What dnlib reads from the file it wrote is what was added
		[Fact]
		public void WrittenAttributeIsReadWithTheSameArguments() {
			var attribute = new CustomAttribute(GetConstructor("Level"));
			attribute.ConstructorArguments.Add(Argument(LevelType, (short)1));
			attribute.ConstructorArguments.Add(Argument(SystemType, new SZArraySig(Types.Int32)));
			attribute.ConstructorArguments.Add(Boxed(Array(Types.Int32, Argument(Types.Int32, 1), Argument(Types.Int32, 2))));
			attribute.NamedArguments.Add(Property("StringProperty", Text("property")));
			target.CustomAttributes.Add(attribute);
			var stream = new MemoryStream();
			module.Write(stream);

			using (var written = ModuleDefMD.Load(stream.ToArray())) {
				var read = written.Find("Target", isReflectionName: true)!.CustomAttributes.Single();

				Assert.Equal("ValuesAttribute", read.TypeFullName);
				Assert.Equal(3, read.ConstructorArguments.Count);
				Assert.Equal((short)1, read.ConstructorArguments[0].Value);
				Assert.Equal("Level", read.ConstructorArguments[0].Type!.FullName);
				Assert.Equal("System.Int32[]", Assert.IsAssignableFrom<TypeSig>(read.ConstructorArguments[1].Value).FullName);
				// The value in the 'object' has the type of the value
				var boxedArray = read.ConstructorArguments[2];
				Assert.Equal("System.Int32[]", boxedArray.Type!.FullName);
				Assert.Equal(new object[] { 1, 2 }, Assert.IsAssignableFrom<IList<CAArgument>>(boxedArray.Value).Select(a => a.Value));
				var named = Assert.Single(read.NamedArguments);
				Assert.True(named.IsProperty);
				Assert.Equal("StringProperty", named.Name);
				Assert.Equal("property", named.Value!.ToString());
				Assert.Same(named, read.GetProperty("StringProperty"));
				Assert.Null(read.GetField("StringProperty"));
			}
		}
	}
}
