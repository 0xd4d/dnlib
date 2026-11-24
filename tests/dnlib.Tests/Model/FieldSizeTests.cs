// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Microsoft.CodeAnalysis;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// <see cref="FieldDef.GetFieldSize()"/> is the number of bytes of a field whose type has a size that the
	/// metadata says: primitive types, pointers, enums, structs with automatic layout and one field, and structs
	/// with a size in their layout. (The writer needs it for the initial value of a field.) The sizes are compared with
	/// <c>sizeof</c> in the compiled program itself. <see cref="ModuleDef.GetPointerSize()"/> is the size
	/// of a pointer in the process that the module is made for.
	/// </summary>
	public class FieldSizeTests {
		const string Source = @"
using System;
using System.Runtime.InteropServices;

public enum ByteEnum : byte { A }
public enum ShortEnum : short { A }
public enum IntEnum { A }
public enum LongEnum : long { A }
[StructLayout(LayoutKind.Auto)] public struct Wrapper { public short Value; public const int Constant = 1; public static long Static; }
[StructLayout(LayoutKind.Auto)] public struct WrapperOfWrapper { public Wrapper Value; }
[StructLayout(LayoutKind.Auto)] public struct WrapperOfEnum { public LongEnum Value; }
[StructLayout(LayoutKind.Sequential, Size = 24)] public struct SequentialWithSize { public int A; public int B; }
[StructLayout(LayoutKind.Explicit, Size = 40)] public struct ExplicitWithSize { [FieldOffset(0)] public int A; [FieldOffset(8)] public long B; }
[StructLayout(LayoutKind.Auto)] public struct TwoFields { public int A; public int B; }
// The layout of a C# struct is sequential by default, and the compiler gives an empty struct the size 1
public struct SequentialWrapper { public short Value; }
public struct Empty { }

public unsafe class Fields {
	public bool Boolean; public char Char; public sbyte SByte; public byte Byte; public short Int16; public ushort UInt16;
	public int Int32; public uint UInt32; public long Int64; public ulong UInt64; public float Single; public double Double;
	public IntPtr IntPtr; public UIntPtr UIntPtr; public int* Pointer; public delegate*<int, void> FunctionPointer;
	public ByteEnum ByteEnum; public ShortEnum ShortEnum; public IntEnum IntEnum; public LongEnum LongEnum;
	public Wrapper Wrapper; public WrapperOfWrapper WrapperOfWrapper; public WrapperOfEnum WrapperOfEnum;
	public SequentialWithSize SequentialWithSize; public ExplicitWithSize ExplicitWithSize; public Empty Empty;
	public volatile int Volatile;

	// Types without a size that dnlib knows
	public TwoFields TwoFields; public SequentialWrapper SequentialWrapper; public string String; public object Object; public int[] Array; public Guid Guid;

	public static int SizeOf(string field) {
		switch (field) {
		case ""Boolean"": return sizeof(bool);
		case ""Char"": return sizeof(char);
		case ""SByte"": return sizeof(sbyte);
		case ""Byte"": return sizeof(byte);
		case ""Int16"": return sizeof(short);
		case ""UInt16"": return sizeof(ushort);
		case ""Int32"": return sizeof(int);
		case ""UInt32"": return sizeof(uint);
		case ""Int64"": return sizeof(long);
		case ""UInt64"": return sizeof(ulong);
		case ""Single"": return sizeof(float);
		case ""Double"": return sizeof(double);
		case ""IntPtr"": return sizeof(IntPtr);
		case ""UIntPtr"": return sizeof(UIntPtr);
		case ""Pointer"": return sizeof(int*);
		case ""FunctionPointer"": return sizeof(delegate*<int, void>);
		case ""ByteEnum"": return sizeof(ByteEnum);
		case ""ShortEnum"": return sizeof(ShortEnum);
		case ""IntEnum"": return sizeof(IntEnum);
		case ""LongEnum"": return sizeof(LongEnum);
		case ""Wrapper"": return sizeof(Wrapper);
		case ""WrapperOfWrapper"": return sizeof(WrapperOfWrapper);
		case ""WrapperOfEnum"": return sizeof(WrapperOfEnum);
		case ""SequentialWithSize"": return sizeof(SequentialWithSize);
		case ""ExplicitWithSize"": return sizeof(ExplicitWithSize);
		case ""Empty"": return sizeof(Empty);
		case ""Volatile"": return sizeof(int);
		default: return -1;
		}
	}
}
";
		static readonly string[] fieldsWithoutAKnownSize = { "TwoFields", "SequentialWrapper", "String", "Object", "Array", "Guid" };

		static byte[] Compile(Platform platform) => CSharpCompiler.Compile(Source, new CSharpCompilerOptions { Platform = platform }).Image;

		[Fact]
		public void FieldSizeIsWhatSizeofReturnsInTheProgram() {
			Assert.SkipUnless(System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64, "The program is compiled for x64");

			var image = Compile(Platform.X64);
			using (var loaded = new LoadedAssembly(image))
			using (var module = ModuleDefMD.Load(image)) {
				var sizeOf = loaded.Assembly.GetType("Fields")!.GetMethod("SizeOf")!;
				int count = 0;
				foreach (var field in module.Find("Fields", isReflectionName: true)!.Fields) {
					int expected = (int)sizeOf.Invoke(null, new object[] { field.Name.String })!;
					if (expected < 0)
						continue;
					Assert.True(field.GetFieldSize(out uint size), field.Name);
					Assert.True((uint)expected == size, $"{field.Name}: sizeof is {expected}, GetFieldSize() returns {size}");
					Assert.Equal(size, field.GetFieldSize());
					count++;
				}
				Assert.Equal(27, count);
			}
		}

		[Fact]
		public void FieldOfATypeWithoutAKnownSizeHasNoSize() {
			using (var module = ModuleDefMD.Load(Compile(Platform.X64))) {
				var type = module.Find("Fields", isReflectionName: true)!;
				foreach (var name in fieldsWithoutAKnownSize) {
					var field = type.FindField(name)!;
					Assert.False(field.GetFieldSize(out uint size), name);
					Assert.Equal(0U, size);
					Assert.Equal(0U, field.GetFieldSize());
				}
				Assert.Equal(0U, new FieldDefUser("NoSignature").GetFieldSize());
			}
		}

		// Only the size of a pointer depends on the processor
		[Theory]
		[InlineData(Platform.X86, 4)]
		[InlineData(Platform.Arm, 4)]
		[InlineData(Platform.X64, 8)]
		[InlineData(Platform.Arm64, 8)]
		public void PointerFieldsHaveThePointerSizeOfTheModule(Platform platform, uint pointerSize) {
			using (var module = ModuleDefMD.Load(Compile(platform))) {
				var type = module.Find("Fields", isReflectionName: true)!;

				Assert.Equal((int)pointerSize, module.GetPointerSize());
				foreach (var name in new[] { "IntPtr", "UIntPtr", "Pointer", "FunctionPointer" })
					Assert.Equal(pointerSize, type.FindField(name)!.GetFieldSize());
				Assert.Equal(4U, type.FindField("Int32")!.GetFieldSize());
				Assert.Equal(8U, type.FindField("Int64")!.GetFieldSize());
				Assert.Equal(24U, type.FindField("SequentialWithSize")!.GetFieldSize());
			}
		}

		// A module for any processor is loaded into a 32-bit or a 64-bit process: the caller says what it
		// wants to assume. "32-bit preferred" is a module that runs as 32-bit code if it's the program.
		[Theory]
		[InlineData(Platform.AnyCpu, 4, 8, 8)]
		[InlineData(Platform.AnyCpu32BitPreferred, 4, 8, 4)]
		[InlineData(Platform.X86, 4, 4, 4)]
		[InlineData(Platform.X64, 8, 8, 8)]
		public void PointerSizeOfAModuleForAnyProcessorIsTheCallersDefault(Platform platform, int expectedDefault, int expectedWithDefault8, int expectedWithDefault8Prefer4) {
			var options = new CSharpCompilerOptions { Platform = platform, OutputKind = OutputKind.ConsoleApplication };
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile("public static class Program { public static void Main() { } }", options).Image)) {
				Assert.Equal(expectedDefault, module.GetPointerSize());
				Assert.Equal(expectedWithDefault8, module.GetPointerSize(8));
				Assert.Equal(expectedWithDefault8Prefer4, module.GetPointerSize(8, 4));
			}
		}

		[Fact]
		public void NewModuleHasThePointerSizeOfItsMachine() {
			var module = new ModuleDefUser("Test.dll");

			Assert.Equal(4, module.GetPointerSize());
			Assert.Equal(8, module.GetPointerSize(8));
			module.Machine = dnlib.PE.Machine.AMD64;
			Assert.Equal(8, module.GetPointerSize());
			module.Machine = dnlib.PE.Machine.ARMNT;
			Assert.Equal(4, module.GetPointerSize(8));
		}

		// A struct whose only field is of its own type (invalid, but the API allows it) has no size
		[Fact]
		public void StructThatContainsItselfHasNoSize() {
			var module = new ModuleDefUser("Test.dll");
			var valueType = new TypeRefUser(module, "System", "ValueType", module.CorLibTypes.AssemblyRef);
			var type = new TypeDefUser("Ns", "Self", valueType);
			module.Types.Add(type);
			var field = new FieldDefUser("Value", new FieldSig(new ValueTypeSig(type)), dnlib.DotNet.FieldAttributes.Public);
			type.Fields.Add(field);

			Assert.False(field.GetFieldSize(out _));
		}
	}
}
