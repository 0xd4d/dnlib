// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// The constants of a method (<c>const int x = 1;</c>) only exist in the PDB: a name, a type and a value.
	/// In a portable PDB the type and the value are one blob. The tests add constants of every kind of type
	/// to a method, write a portable PDB and read it again.
	/// </summary>
	public class LocalConstantTests {
		sealed class Constant {
			public readonly string Name;
			public readonly TypeSig Type;
			public readonly object? Value;

			public Constant(string name, TypeSig type, object? value) {
				Name = name;
				Type = type;
				Value = value;
			}
		}

		// Adds the constants to the method Sum, writes the module and its PDB, and returns the constants of
		// the method that are read from the written files (name -> constant)
		static Dictionary<string, PdbConstant> WriteAndRead(Func<ModuleDef, IEnumerable<Constant>> createConstants, out ModuleDefMD writtenModule) {
			CompiledAssembly written;
			using (var module = DebugInfoSource.Load()) {
				var constants = DebugInfoSource.GetRootScope(module, "Sum").Constants;
				foreach (var constant in createConstants(module))
					constants.Add(new PdbConstant(constant.Name, constant.Type, constant.Value));
				written = RoundTrip.WriteWithPortablePdb(module, preserveMetadata: true);
			}
			writtenModule = RoundTrip.LoadWithPdb(written.Image, written.Pdb!);
			return DebugInfoSource.GetRootScope(writtenModule, "Sum").Constants.ToDictionary(a => a.Name!);
		}

		static TypeSig CoreLibraryType(ModuleDef module, string name, bool isValueType) {
			var type = module.CorLibTypes.GetTypeRef("System", name);
			return isValueType ? new ValueTypeSig(type) : (TypeSig)new ClassSig(type);
		}

		[Fact]
		public void ConstantsOfTypesWithAValue() {
			var constants = WriteAndRead(module => {
				var types = module.CorLibTypes;
				return new[] {
					new Constant("Bool", types.Boolean, true),
					new Constant("Char", types.Char, 'x'),
					new Constant("SByte", types.SByte, (sbyte)-1),
					new Constant("Byte", types.Byte, (byte)200),
					new Constant("Short", types.Int16, (short)-2),
					new Constant("UShort", types.UInt16, (ushort)60000),
					new Constant("Int", types.Int32, -3),
					new Constant("UInt", types.UInt32, 4000000000),
					new Constant("Long", types.Int64, -4L),
					new Constant("ULong", types.UInt64, ulong.MaxValue),
					new Constant("Float", types.Single, 1.5f),
					new Constant("Double", types.Double, -2.25),
					new Constant("Text", types.String, "text"),
					new Constant("EmptyText", types.String, ""),
					new Constant("Decimal", CoreLibraryType(module, "Decimal", isValueType: true), -12.345m),
					new Constant("Date", CoreLibraryType(module, "DateTime", isValueType: true), new DateTime(2001, 2, 3, 4, 5, 6)),
					// A native int constant is written with the type of its value: the blob of a native int has
					// no value (see PortablePdbWriterTests.NativeIntConstantIsWrittenAsItsValuesType)
					new Constant("NativeInt", types.IntPtr, 5),
					new Constant("NativeUInt", types.UIntPtr, 6U),
					// An int with a custom modifier
					new Constant("Modified", new CModOptSig(types.GetTypeRef("System.Runtime.CompilerServices", "IsConst"), types.Int32), 7),
				};
			}, out var writtenModule);

			using (writtenModule) {
				Assert.Equal(true, constants["Bool"].Value);
				Assert.Equal('x', constants["Char"].Value);
				Assert.Equal((sbyte)-1, constants["SByte"].Value);
				Assert.Equal((byte)200, constants["Byte"].Value);
				Assert.Equal((short)-2, constants["Short"].Value);
				Assert.Equal((ushort)60000, constants["UShort"].Value);
				Assert.Equal(-3, constants["Int"].Value);
				Assert.Equal(4000000000, constants["UInt"].Value);
				Assert.Equal(-4L, constants["Long"].Value);
				Assert.Equal(ulong.MaxValue, constants["ULong"].Value);
				Assert.Equal(1.5f, constants["Float"].Value);
				Assert.Equal(-2.25, constants["Double"].Value);
				Assert.Equal("text", constants["Text"].Value);
				Assert.Equal("", constants["EmptyText"].Value);
				Assert.Equal(-12.345m, constants["Decimal"].Value);
				Assert.Equal("System.Decimal", constants["Decimal"].Type!.FullName);
				Assert.Equal(new DateTime(2001, 2, 3, 4, 5, 6), constants["Date"].Value);
				Assert.Equal(5, constants["NativeInt"].Value);
				Assert.Equal(ElementType.I4, constants["NativeInt"].Type!.ElementType);
				Assert.Equal(6U, constants["NativeUInt"].Value);
				Assert.Equal(ElementType.U4, constants["NativeUInt"].Type!.ElementType);
				Assert.Equal(7, constants["Modified"].Value);
				Assert.Equal("System.Int32 modopt(System.Runtime.CompilerServices.IsConst)", constants["Modified"].Type!.FullName);
			}
		}

		// A constant of a reference type can only be null (except a string)
		[Fact]
		public void NullConstantsKeepTheirType() {
			var constants = WriteAndRead(module => {
				var types = module.CorLibTypes;
				var list = new ClassSig(types.GetTypeRef("System.Collections.Generic", "List`1"));
				return new[] {
					new Constant("NullText", types.String, null),
					new Constant("NullObject", types.Object, null),
					new Constant("NullClass", CoreLibraryType(module, "Exception", isValueType: false), null),
					new Constant("NullArray", new SZArraySig(types.Int32), null),
					new Constant("NullMatrix", new ArraySig(types.Int32, 2), null),
					new Constant("NullGeneric", new GenericInstSig(list, types.String), null),
					new Constant("NullPointer", new PtrSig(types.Byte), null),
					new Constant("NullTypeParameter", new GenericVar(0), null),
					new Constant("NullMethodTypeParameter", new GenericMVar(0), null),
				};
			}, out var writtenModule);

			using (writtenModule) {
				Assert.All(constants.Where(a => a.Key.StartsWith("Null")), a => Assert.Null(a.Value.Value));
				Assert.Equal("System.String", constants["NullText"].Type!.FullName);
				Assert.Equal("System.Object", constants["NullObject"].Type!.FullName);
				Assert.Equal("System.Exception", constants["NullClass"].Type!.FullName);
				Assert.Equal("System.Int32[]", constants["NullArray"].Type!.FullName);
				Assert.Equal("System.Int32[,]", constants["NullMatrix"].Type!.FullName);
				Assert.Equal("System.Collections.Generic.List`1<System.String>", constants["NullGeneric"].Type!.FullName);
				Assert.Equal("System.Byte*", constants["NullPointer"].Type!.FullName);
				Assert.Equal(ElementType.Var, constants["NullTypeParameter"].Type!.ElementType);
				Assert.Equal(ElementType.MVar, constants["NullMethodTypeParameter"].Type!.ElementType);
			}
		}

		// The default value of a struct (default(Guid)) has no value in the blob, a reader gets null
		[Fact]
		public void DefaultValueOfAStruct() {
			var constants = WriteAndRead(module => new[] {
				new Constant("DefaultStruct", CoreLibraryType(module, "Guid", isValueType: true), null),
				new Constant("DefaultNullable", new GenericInstSig(new ValueTypeSig(module.CorLibTypes.GetTypeRef("System", "Nullable`1")), module.CorLibTypes.Int32), null),
			}, out var writtenModule);

			using (writtenModule) {
				Assert.Null(constants["DefaultStruct"].Value);
				Assert.Equal("System.Guid", constants["DefaultStruct"].Type!.FullName);
				Assert.Null(constants["DefaultNullable"].Value);
				Assert.Equal("System.Nullable`1<System.Int32>", constants["DefaultNullable"].Type!.FullName);
			}
		}

		public static IEnumerable<object?[]> ValuesOfEveryConstantType() => new[] {
			new object?[] { true, "System.Boolean" },
			new object?[] { 'c', "System.Char" },
			new object?[] { (sbyte)-1, "System.SByte" },
			new object?[] { (byte)1, "System.Byte" },
			new object?[] { (short)-2, "System.Int16" },
			new object?[] { (ushort)2, "System.UInt16" },
			new object?[] { -3, "System.Int32" },
			new object?[] { 3U, "System.UInt32" },
			new object?[] { -4L, "System.Int64" },
			new object?[] { 4UL, "System.UInt64" },
			new object?[] { 1.5f, "System.Single" },
			new object?[] { 2.5, "System.Double" },
			new object?[] { "text", "System.String" },
			new object?[] { 12.34m, "System.Decimal" },
			new object?[] { new DateTime(2001, 2, 3, 4, 5, 6), "System.DateTime" },
			new object?[] { null, "System.Object" },
		};

		// A constant from a Windows PDB can have no type. A portable PDB needs one: the type of the value.
		[Theory]
		[MemberData(nameof(ValuesOfEveryConstantType))]
		public void ConstantWithoutATypeGetsTheTypeOfItsValue(object? value, string expectedType) {
			CompiledAssembly written;
			using (var module = DebugInfoSource.Load()) {
				DebugInfoSource.GetRootScope(module, "Sum").Constants.Add(new PdbConstant("NoType", null, value));
				written = RoundTrip.WriteWithPortablePdb(module, preserveMetadata: true);
			}

			using (var writtenModule = RoundTrip.LoadWithPdb(written.Image, written.Pdb!)) {
				var constant = DebugInfoSource.GetRootScope(writtenModule, "Sum").Constants.Single(a => a.Name == "NoType");
				Assert.Equal(expectedType, constant.Type!.FullName);
				Assert.Equal(value, constant.Value);
			}
		}

		public static IEnumerable<object?[]> ValuesOfTheWrongType() => new[] {
			new object?[] { "Int32", "text", "Expected an Int32 constant" },
			new object?[] { "Boolean", 1, "Expected a Boolean constant" },
			new object?[] { "Single", 1.5, "Expected a Single constant" },
			new object?[] { "Double", 1.5f, "Expected a Double constant" },
			new object?[] { "String", 5, "Expected a String constant" },
			new object?[] { "Object", 5, "Expected a null constant" },
		};

		// A value that doesn't have the constant's type is an error. If the logger ignores it, the PDB is
		// still written.
		[Theory]
		[MemberData(nameof(ValuesOfTheWrongType))]
		public void ValueOfAnotherTypeThanTheConstantIsAnError(string typeName, object value, string expectedMessage) {
			using (var module = DebugInfoSource.Load()) {
				var type = module.CorLibTypes.GetCorLibTypeSig(module.CorLibTypes.GetTypeRef("System", typeName))!;
				DebugInfoSource.GetRootScope(module, "Sum").Constants.Add(new PdbConstant("Wrong", type, value));

				var exception = Assert.Throws<ModuleWriterException>(() => RoundTrip.WriteWithPortablePdb(module, preserveMetadata: true));
				Assert.Contains(expectedMessage, exception.Message);

				var written = RoundTrip.WriteWithPortablePdb(module, preserveMetadata: true, options => options.Logger = DummyLogger.NoThrowInstance);
				using (var writtenModule = RoundTrip.LoadWithPdb(written.Image, written.Pdb!))
					Assert.Contains(DebugInfoSource.GetRootScope(writtenModule, "Sum").Constants, a => a.Name == "Wrong");
			}
		}
	}
}
