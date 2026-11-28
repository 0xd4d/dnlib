// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.IO;
using Xunit;

namespace dnlib.Tests.CustomAttributes {
	/// <summary>
	/// <see cref="CustomAttributeReader"/> reads the blob of a custom attribute with the types of its constructor's
	/// parameters. A blob that can't be read (a type that can't be an argument, too much nesting, an enum whose
	/// underlying type isn't an integer) is kept as raw bytes, so it can be written again as it is.
	/// </summary>
	public class CustomAttributeBlobReaderTests {
		readonly ModuleDefUser module = new ModuleDefUser("Test.dll");

		ICorLibTypes Types => module.CorLibTypes;

		MemberRefUser Ctor(params TypeSig?[] parameters) =>
			new MemberRefUser(module, ".ctor", new MethodSig(CallingConvention.HasThis, 0, Types.Void, parameters), new TypeRefUser(module, "Ns", "SomeAttribute", module));

		// The prolog (1, 0), the fixed arguments, and the number of named arguments
		static byte[] Blob(params byte[] arguments) => new byte[] { 1, 0 }.Concat(arguments).ToArray();
		static byte[] SerString(string value) => new[] { (byte)value.Length }.Concat(System.Text.Encoding.UTF8.GetBytes(value)).ToArray();

		CustomAttribute Read(byte[] blob, ICustomAttributeType ctor) => CustomAttributeReader.Read(module, blob, ctor);

		// System.String and System.Object of the core library as a class (not as the element types STRING and
		// OBJECT) are read like those element types
		[Fact]
		public void StringAndObjectAsAClassAreReadLikeTheirElementTypes() {
			var stringClass = new ClassSig(Types.GetTypeRef("System", "String"));
			var objectClass = new ClassSig(Types.GetTypeRef("System", "Object"));
			// "hi", then a boxed int (0x08) 5
			var blob = Blob(SerString("hi").Concat(new byte[] { 0x08, 5, 0, 0, 0, 0, 0 }).ToArray());

			var ca = Read(blob, Ctor(stringClass, objectClass));

			Assert.False(ca.IsRawBlob);
			Assert.Equal("hi", ((UTF8String)ca.ConstructorArguments[0].Value!).String);
			// The argument has the type of the boxed value
			Assert.Equal(5, ca.ConstructorArguments[1].Value);
			Assert.Equal(ElementType.I4, ca.ConstructorArguments[1].Type!.ElementType);
		}

		// A named argument of an enum type has the type name after ENUM (0x55)
		[Fact]
		public void ANamedEnumArgumentHasItsTypeName() {
			// 1 named argument: a field (0x53) of type enum (0x55) 'Ns.Color' named 'Color' with value 2 (an int: the
			// enum can't be resolved, its value has the size of an int)
			var blob = Blob(new byte[] { 1, 0, 0x53, 0x55 }.Concat(SerString("Ns.Color")).Concat(SerString("Color")).Concat(new byte[] { 2, 0, 0, 0 }).ToArray());

			var ca = Read(blob, Ctor());

			var named = Assert.Single(ca.NamedArguments);
			Assert.True(named.IsField);
			Assert.Equal("Color", named.Name);
			Assert.Equal("Ns.Color", named.Type!.FullName);
			Assert.Equal(2, named.Argument.Value);
		}

		// The overloads with a byte array, a DataReader and a generic parameter context read the same
		[Fact]
		public void EveryOverloadReadsTheSameAttribute() {
			var blob = Blob(new byte[] { 7, 0, 0, 0, 0, 0 });
			var ctor = Ctor(Types.Int32);

			var attributes = new[] {
				CustomAttributeReader.Read(module, blob, ctor),
				CustomAttributeReader.Read(module, blob, ctor, new GenericParamContext()),
				CustomAttributeReader.Read(module, ByteArrayDataReaderFactory.CreateReader(blob), ctor),
				CustomAttributeReader.Read(module, ByteArrayDataReaderFactory.CreateReader(blob), ctor, new GenericParamContext()),
			};

			Assert.All(attributes, a => Assert.Equal(7, a.ConstructorArguments[0].Value));
		}

		// Blobs that can't be read with the constructor's parameter types: the attribute keeps the bytes
		[Fact]
		public void ABlobThatCanNotBeReadIsKeptAsItIs() {
			var blob = Blob(new byte[] { 1, 2, 3, 4, 0, 0 });
			var valueTypeEnum = new ValueTypeSig(new TypeRefUser(module, "Ns", "Unknown", module));
			var genericValueType = new GenericInstSig(new ValueTypeSig(new TypeDefUser("Ns", "Struct`1", Types.GetTypeRef("System", "ValueType"))), Types.Int32);

			foreach (var ctor in new[] {
				// A parameter without a type
				Ctor((TypeSig?)null),
				// A type that can't be an argument
				Ctor(new PtrSig(Types.Int32)),
				Ctor(new FnPtrSig(MethodSig.CreateStatic(Types.Void))),
				// A generic instance of a value type that isn't an enum
				Ctor(genericValueType),
			}) {
				var ca = Read(blob, ctor);
				Assert.True(ca.IsRawBlob);
				Assert.Equal(blob, ca.RawData);
				Assert.Same(ctor, ca.Constructor);
			}

			// Without a constructor, the blob can't be read either
			var withoutConstructor = CustomAttributeReader.Read(module, blob, null);
			Assert.True(withoutConstructor.IsRawBlob);
			// An enum that can't be resolved is read as an int
			Assert.Equal(0x04030201, Read(blob, Ctor(valueTypeEnum)).ConstructorArguments[0].Value);
		}

		// An enum whose underlying type isn't an integer (its value__ field is a string)
		[Fact]
		public void AnEnumWhoseUnderlyingTypeIsNotAnIntegerCanNotBeRead() {
			var enumType = new TypeDefUser("Ns", "BadEnum", Types.GetTypeRef("System", "Enum")) { Attributes = TypeAttributes.Public | TypeAttributes.Sealed };
			enumType.Fields.Add(new FieldDefUser("value__", new FieldSig(Types.String), FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName));
			module.Types.Add(enumType);
			var blob = Blob(new byte[] { 1, 0, 0, 0, 0, 0 });

			var ca = Read(blob, Ctor(new ValueTypeSig(enumType)));

			Assert.True(ca.IsRawBlob);
		}

		// object[] { object[] { ... { 1 } } }: an object argument is a type and a value. The type is SZARRAY (0x1D)
		// of boxed objects (0x51), the value is the number of elements and the elements.
		static byte[] NestedArrays(int depth) {
			var arguments = new System.Collections.Generic.List<byte>();
			for (int i = 0; i < depth; i++)
				arguments.AddRange(new byte[] { 0x1D, 0x51, 1, 0, 0, 0 });
			arguments.AddRange(new byte[] { 0x08, 1, 0, 0, 0 });
			return Blob(arguments.Concat(new byte[] { 0, 0 }).ToArray());
		}

		// Nested arrays are read, but not when they're nested too deeply
		[Theory, InlineData(3, false), InlineData(200, true)]
		public void ArgumentsNestedTooDeeplyCanNotBeRead(int depth, bool isRaw) {
			var ca = Read(NestedArrays(depth), Ctor(Types.Object));

			Assert.Equal(isRaw, ca.IsRawBlob);
		}

		// A boxed enum in an object argument: ENUM (0x55), the enum's type name, the value (an int: the enum can't
		// be resolved)
		[Fact]
		public void ABoxedEnumHasItsTypeName() {
			var blob = Blob(new byte[] { 0x55 }.Concat(SerString("Ns.Color")).Concat(new byte[] { 3, 0, 0, 0, 0, 0 }).ToArray());

			var ca = Read(blob, Ctor(Types.Object));

			Assert.False(ca.IsRawBlob);
			var argument = ca.ConstructorArguments[0];
			Assert.Equal("Ns.Color", argument.Type!.FullName);
			Assert.Equal(3, argument.Value);
		}
	}
}
