// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Microsoft.CodeAnalysis;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The .NET header of a program has the token of the method that the program starts with. These tests
	/// write other tokens (a method that isn't static, a row that doesn't exist, a field, a token of another
	/// table) and flags, and read the entry point with dnlib and with the runtime that runs the tests
	/// (Assembly.EntryPoint). If the runtime has an entry point, dnlib must have the same one.
	/// </summary>
	public class ClrEntryPointRuleTests {
		const string Source = @"
public class Program {
	public static int Field;
	public static void Main() { }
	public static int Other(string[] args) => 0;
	public void Instance() { }
	public static void Generic<T>() { }
}
";
		static readonly byte[] baseImage = CSharpCompiler.Compile(Source, new CSharpCompilerOptions { OutputKind = OutputKind.ConsoleApplication }).Image;

		// The .NET header: its size and the version of the runtime (8 bytes), the address and the size of
		// the metadata (8 bytes), the flags (4 bytes) and the token of the entry point (4 bytes)
		const int FlagsOffset = 16, EntryPointOffset = 20;
		const uint NativeEntryPoint = 0x10;

		static byte[] CreateImage(string entryPoint, uint flagsToSet) {
			int headerOffset;
			using (var peReader = new PEReader(new MemoryStream(baseImage)))
				headerOffset = peReader.PEHeaders.CorHeaderStartOffset;
			var image = (byte[])baseImage.Clone();

			uint token;
			using (var module = ModuleDefMD.Load(baseImage)) {
				var type = module.Find("Program", isReflectionName: true)!;
				switch (entryPoint) {
				case "Main": token = type.FindMethod("Main")!.MDToken.Raw; break;
				case "Other": token = type.FindMethod("Other")!.MDToken.Raw; break;
				case "Instance": token = type.FindMethod("Instance")!.MDToken.Raw; break;
				case "Generic": token = type.FindMethod("Generic")!.MDToken.Raw; break;
				case ".ctor": token = type.FindMethod(".ctor")!.MDToken.Raw; break;
				case "Field": token = type.FindField("Field")!.MDToken.Raw; break;
				case "Program": token = type.MDToken.Raw; break;
				case "method row 0": token = new MDToken(Table.Method, 0).Raw; break;
				case "method row that doesn't exist": token = new MDToken(Table.Method, 1000).Raw; break;
				case "file row that doesn't exist": token = new MDToken(Table.File, 1).Raw; break;
				case "member reference": token = new MDToken(Table.MemberRef, 1).Raw; break;
				case "0": token = 0; break;
				case "table that doesn't exist": token = 0x7F000001; break;
				default: throw new ArgumentException(entryPoint);
				}
			}
			LittleEndian.GetBytes(token).CopyTo(image, headerOffset + EntryPointOffset);
			uint flags = LittleEndian.ToUInt32(image, headerOffset + FlagsOffset) | flagsToSet;
			LittleEndian.GetBytes(flags).CopyTo(image, headerOffset + FlagsOffset);
			return image;
		}

		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image))
				return module.EntryPoint?.Name.String ?? "none";
		}

		static string ReadWithTheRuntime(byte[] image) {
			using (var loaded = new LoadedAssembly(image))
				return loaded.Assembly.EntryPoint?.Name ?? "none";
		}

		[Theory]
		[InlineData("Main", 0, "Main", "Main")]
		[InlineData("Other", 0, "Other", "Other")]
		[InlineData("Instance", 0, "Instance", "Instance")]
		[InlineData("Generic", 0, "Generic", "Generic")]
		[InlineData(".ctor", 0, ".ctor", "throws InvalidCastException")]
		[InlineData("Field", 0, "none", "none")]
		[InlineData("Program", 0, "none", "none")]
		[InlineData("method row 0", 0, "none", "none")]
		[InlineData("method row that doesn't exist", 0, "none", "none")]
		[InlineData("file row that doesn't exist", 0, "none", "throws BadImageFormatException")]
		[InlineData("member reference", 0, "none", "none")]
		[InlineData("0", 0, "none", "none")]
		[InlineData("table that doesn't exist", 0, "none", "none")]
		// With the flag that says that the entry point is the address of native code
		[InlineData("Main", NativeEntryPoint, "none", "throws BadImageFormatException")]
		[InlineData("0", NativeEntryPoint, "none", "throws BadImageFormatException")]
		public void EntryPoint(string entryPoint, uint flagsToSet, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(entryPoint, flagsToSet);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}
	}
}
