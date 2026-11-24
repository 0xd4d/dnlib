// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.Tests.Writer;
using Xunit;

namespace dnlib.Tests.CustomAttributes {
	/// <summary>
	/// An enum argument of a custom attribute is stored as the value of the enum's underlying type, without
	/// anything that says which type that is: a reader must find the enum type to know the size of the value.
	/// If the enum is in an assembly that can't be found, the writer uses the type of the value it was given, and
	/// the reader can't read the arguments: the attribute only has its bytes.
	/// </summary>
	public class UnresolvedEnumArgumentTests {
		public static IEnumerable<object[]> ValuesOfEveryUnderlyingType() => new[] {
			new object[] { true, new byte[] { 1 } },
			new object[] { 'A', new byte[] { 0x41, 0 } },
			new object[] { (sbyte)-2, new byte[] { 0xFE } },
			new object[] { (byte)0x12, new byte[] { 0x12 } },
			new object[] { (short)-2, new byte[] { 0xFE, 0xFF } },
			new object[] { (ushort)0x1234, new byte[] { 0x34, 0x12 } },
			new object[] { -2, new byte[] { 0xFE, 0xFF, 0xFF, 0xFF } },
			new object[] { 0x12345678U, new byte[] { 0x78, 0x56, 0x34, 0x12 } },
			new object[] { -2L, new byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF } },
			new object[] { 0x123456789ABCDEF0UL, new byte[] { 0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x12 } },
		};

		// Writes a module with an attribute that has one argument: 'value' as an enum of an assembly that
		// doesn't exist
		static ModuleDefMD WriteAndRead(object value) {
			var created = new CreatedModule();
			var module = created.Module;
			var missingAssembly = new AssemblyRefUser("MissingAssembly", new System.Version(1, 0, 0, 0));
			var enumType = new ValueTypeSig(new TypeRefUser(module, "Missing", "SomeEnum", missingAssembly));
			var constructor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void, enumType),
				new TypeRefUser(module, "Missing", "SomeAttribute", missingAssembly));
			var attribute = new CustomAttribute(constructor);
			attribute.ConstructorArguments.Add(new CAArgument(enumType, value));
			created.Type.CustomAttributes.Add(attribute);
			// It's not a writer error
			return created.WriteAndRead();
		}

		[Theory]
		[MemberData(nameof(ValuesOfEveryUnderlyingType))]
		public void ValueOfAnEnumThatCanNotBeFoundIsWrittenAsTheTypeOfTheValue(object value, byte[] expectedBytes) {
			using (var written = WriteAndRead(value)) {
				// The bytes in the file (the only row of the CustomAttribute table)
				Assert.True(written.TablesStream.TryReadCustomAttributeRow(1, out var row));
				var blob = written.BlobStream.Read(row.Value);

				// 01 00 starts every custom attribute, and the number of named arguments (00 00) ends it
				Assert.Equal(new byte[] { 1, 0 }.Concat(expectedBytes).Concat(new byte[] { 0, 0 }), blob);
			}
		}

		// The reader assumes that the enum is an Int32, like most enums
		[Theory]
		[InlineData(-2)]
		[InlineData(0x12345678)]
		public void ReaderReadsA32BitValueOfAnEnumThatCanNotBeFound(int value) {
			using (var written = WriteAndRead(value)) {
				var read = Assert.Single(CreatedModule.GetType(written).CustomAttributes);

				Assert.False(read.IsRawBlob);
				Assert.Equal(value, Assert.Single(read.ConstructorArguments).Value);
			}
		}

		// With 1 or 8 bytes, that assumption is wrong: too few bytes, or bytes are left over. The attribute's
		// arguments aren't read, it only has its bytes, and they're written back as they are.
		[Theory]
		[InlineData((byte)0x12, 1)]
		[InlineData(-2L, 8)]
		public void ReaderKeepsTheBytesIfTheValueIsNot32Bits(object value, int valueSize) {
			byte[] blob;
			using (var written = WriteAndRead(value)) {
				var read = Assert.Single(CreatedModule.GetType(written).CustomAttributes);
				Assert.True(read.IsRawBlob);
				Assert.Equal(2 + valueSize + 2, read.RawData.Length);
				blob = read.RawData;

				var stream = new System.IO.MemoryStream();
				written.Write(stream);
				using (var writtenAgain = ModuleDefMD.Load(stream.ToArray()))
					Assert.Equal(blob, Assert.Single(CreatedModule.GetType(writtenAgain).CustomAttributes).RawData);
			}
		}
	}
}
