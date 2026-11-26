// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.IO;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The header of a method body and the sections after its code (the exception handlers) can be written in
	/// more ways than a compiler does. Every test writes the bytes of a body to a file, reads the method with
	/// dnlib and calls it with the runtime that runs the tests: the method must return what the code that
	/// dnlib read returns.
	///
	/// What the tests show: the code starts after the size that the fat header has (3 or more 32-bit words).
	/// Only the first section after the code is used, and only if it has exception handlers. The number of
	/// handlers is the size of the section / 12 (small) or / 24 (fat), with or without the 4 bytes of the
	/// section header in the size. Where the CLR doesn't accept a body (a fat header that isn't at a
	/// multiple of 4, a header smaller than 3 words, a max stack that's too small), dnlib still reads it.
	/// </summary>
	public class ClrMethodBodyRuleTests {
		const string Source = @"
public static class Marker {
	public static int Value() => 0;
}
";
		// The body is written to the padding stream of the file: it's in a section that is loaded
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source);
		static readonly uint methodRid, objectToken;
		static readonly long bodyOffset;
		static readonly uint bodyRva;

		static ClrMethodBodyRuleTests() {
			using (var module = ModuleDefMD.Load(baseImage)) {
				methodRid = module.Find("Marker", isReflectionName: true)!.FindMethod("Value")!.Rid;
				objectToken = module.GetTypeRefs().Single(a => a.FullName == "System.Object").MDToken.Raw;
				bodyOffset = new MetadataPatcher(baseImage).GetStreamFileOffset(ClrReaderComparison.PaddingStreamName) + 16;
				bodyRva = (uint)module.Metadata.PEImage.ToRVA((FileOffset)bodyOffset);
				Assert.Equal(0U, bodyRva % 4);
			}
		}

		// Writes the body to the file. The RVA of the method is the address of the body + rvaChange.
		static byte[] CreateImage(byte[] body, int rvaChange = 0) =>
			new MetadataPatcher(baseImage)
				.SetBytes(bodyOffset + Math.Max(rvaChange, 0), body)
				.SetCell(Table.Method, methodRid, "RVA", (uint)(bodyRva + rvaChange))
				.ToArray();

		static byte[] Concat(params byte[][] parts) => parts.SelectMany(a => a).ToArray();
		static byte[] Int32(uint value) => LittleEndian.GetBytes(value);
		static byte[] Int16(int value) => LittleEndian.GetBytes((ushort)value);

		const byte nop = 0x00, ldc_i4_0 = 0x16, ldc_i4_1 = 0x17, ldc_i4 = 0x20, pop = 0x26, ret = 0x2A, div = 0x5B, leave_s = 0xDE;

		// ldc.i4 value; ret
		static byte[] Return(uint value) => Concat(new[] { ldc_i4 }, Int32(value), new[] { ret });

		// The first byte(s) of a fat header: the flags and (in the 4 high bits) the size of the header in
		// 32-bit words
		const int Fat = 0x3, MoreSections = 0x8, InitLocals = 0x10, HeaderSize3 = 0x3000;

		static byte[] FatHeader(int flags, int maxStack, uint codeSize, uint localVarSigToken = 0) =>
			Concat(Int16(flags), Int16(maxStack), Int32(codeSize), Int32(localVarSigToken));

		static string Hex(object? value) => value is int number ? "returns 0x" + number.ToString("X") : "returns " + value;

		// What the method returns if it is 'ldc.i4 X; ret' with any number of nops before it
		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var method = module.ResolveMethod(methodRid)!;
				if (method.Body is null)
					return "no body";
				var instructions = method.Body.Instructions.SkipWhile(a => a.OpCode == OpCodes.Nop).ToArray();
				if (instructions.Length == 0)
					return "empty body";
				if (instructions.Length >= 2 && instructions[0].IsLdcI4() && instructions[1].OpCode == OpCodes.Ret)
					return Hex(instructions[0].GetLdcI4Value());
				return "other code";
			}
		}

		static string CallWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var method = loaded.Assembly.ManifestModule.ResolveMethod(new MDToken(Table.Method, methodRid).ToInt32())!;
					return Hex(method.Invoke(null, null));
				}
			}
			catch (TargetInvocationException ex) {
				return "throws " + ex.InnerException!.GetType().Name;
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		static byte[] CreateImageWithHeader(string header) {
			var code = Return(0x1234);
			switch (header) {
			case "tiny":
				// The size of the code in the 6 high bits
				return CreateImage(Concat(new[] { (byte)(code.Length << 2 | 2) }, code));
			case "tiny, code size 0":
				return CreateImage(Concat(new byte[] { 2 }, code));
			case "tiny, code size 63":
				return CreateImage(Concat(new byte[] { 63 << 2 | 2 }, new byte[63 - code.Length], code));
			case "fat":
				return CreateImage(Concat(FatHeader(HeaderSize3 | Fat, 8, 6), code));
			case "fat, address isn't a multiple of 4":
				return CreateImage(Concat(FatHeader(HeaderSize3 | Fat, 8, 6), code), rvaChange: 1);
			case "fat, header size 4":
				// 4 more bytes in the header. If they aren't skipped, the method returns 1.
				return CreateImage(Concat(FatHeader(0x4000 | Fat, 8, 6), new byte[] { ldc_i4_1, ret, nop, nop }, code));
			case "fat, header size 15":
				return CreateImage(Concat(FatHeader(0xF000 | Fat, 8, 6), new byte[48], code));
			case "fat, header size 2":
				// The code starts at the last 4 bytes of the header (the token of the locals: 0 = 4 nops)
				return CreateImage(Concat(FatHeader(0x2000 | Fat, 8, 10), code));
			case "fat, header size 0":
				return CreateImage(Concat(FatHeader(Fat, 8, 6), code));
			case "fat, max stack 0":
				return CreateImage(Concat(FatHeader(HeaderSize3 | Fat, 0, 6), code));
			case "fat, max stack 0xFFFF":
				return CreateImage(Concat(FatHeader(HeaderSize3 | Fat, 0xFFFF, 6), code));
			case "fat, code size 0":
				return CreateImage(Concat(FatHeader(HeaderSize3 | Fat, 8, 0), code));
			case "fat, code that ends after the file":
				return CreateImage(Concat(FatHeader(HeaderSize3 | Fat, 8, 0x100000), code));
			case "fat, init locals without locals":
				return CreateImage(Concat(FatHeader(HeaderSize3 | InitLocals | Fat, 8, 6), code));
			case "fat, flag 4":
				return CreateImage(Concat(FatHeader(HeaderSize3 | 4 | Fat, 8, 6), code));
			case "fat, unknown flags":
				return CreateImage(Concat(FatHeader(HeaderSize3 | 0xFE0 | Fat, 8, 6), code));
			case "fat, locals token of a type":
				return CreateImage(Concat(FatHeader(HeaderSize3 | Fat, 8, 6, objectToken), code));
			case "fat, locals token of a row that doesn't exist":
				return CreateImage(Concat(FatHeader(HeaderSize3 | Fat, 8, 6, 0x11000099), code));
			case "fat, more sections flag without a section":
				// Zero bytes after the code
				return CreateImage(Concat(FatHeader(HeaderSize3 | MoreSections | Fat, 8, 6), code, new byte[32]));
			case "format 0":
				return CreateImage(Concat(new byte[] { 0x18 }, code));
			case "format 1":
				return CreateImage(Concat(new byte[] { 0x19 }, code));
			case "RVA 0":
				return new MetadataPatcher(baseImage).SetCell(Table.Method, methodRid, "RVA", 0).ToArray();
			case "RVA that isn't in the file":
				return new MetadataPatcher(baseImage).SetCell(Table.Method, methodRid, "RVA", 0x12345678).ToArray();
			default:
				throw new ArgumentException(header);
			}
		}

		[Theory]
		[InlineData("tiny", "returns 0x1234", "returns 0x1234")]
		[InlineData("tiny, code size 0", "empty body", "throws InvalidProgramException")]
		[InlineData("tiny, code size 63", "returns 0x1234", "returns 0x1234")]
		[InlineData("fat", "returns 0x1234", "returns 0x1234")]
		[InlineData("fat, address isn't a multiple of 4", "returns 0x1234", "throws InvalidProgramException")]
		[InlineData("fat, header size 4", "returns 0x1234", "returns 0x1234")]
		[InlineData("fat, header size 15", "returns 0x1234", "returns 0x1234")]
		[InlineData("fat, header size 2", "returns 0x1234", "throws BadImageFormatException")]
		[InlineData("fat, header size 0", "other code", "throws BadImageFormatException")]
		[InlineData("fat, max stack 0", "returns 0x1234", "throws InvalidProgramException")]
		[InlineData("fat, max stack 0xFFFF", "returns 0x1234", "returns 0x1234")]
		[InlineData("fat, code size 0", "empty body", "throws InvalidProgramException")]
		[InlineData("fat, code that ends after the file", "empty body", "throws BadImageFormatException")]
		[InlineData("fat, init locals without locals", "returns 0x1234", "returns 0x1234")]
		[InlineData("fat, flag 4", "empty body", "throws BadImageFormatException")]
		[InlineData("fat, unknown flags", "returns 0x1234", "returns 0x1234")]
		[InlineData("fat, locals token of a type", "returns 0x1234", "throws BadImageFormatException")]
		[InlineData("fat, locals token of a row that doesn't exist", "returns 0x1234", "throws BadImageFormatException")]
		[InlineData("fat, more sections flag without a section", "returns 0x1234", "throws BadImageFormatException")]
		[InlineData("format 0", "empty body", "throws BadImageFormatException")]
		[InlineData("format 1", "empty body", "throws BadImageFormatException")]
		[InlineData("RVA 0", "no body", "throws BadImageFormatException")]
		[InlineData("RVA that isn't in the file", "empty body", "throws BadImageFormatException")]
		public void Header(string header, string expectedDnlib, string expectedRuntime) {
			var image = CreateImageWithHeader(header);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => CallWithTheRuntime(image));
		}

		// Code that divides by zero in a try block. With the first catch block the method returns 0xCA,
		// without it the method throws. The second try block does nothing.
		//
		//	 0: ldc.i4.1, ldc.i4.0, div, pop
		//	 4: leave.s 9
		//	 6: pop, leave.s 21				(the first catch block: 3 bytes)
		//	 9: nop, leave.s 15				(the second try block: 3 bytes)
		//	12: pop, leave.s 15				(the second catch block: 3 bytes)
		//	15: ldc.i4 0, ret
		//	21: ldc.i4 0xCA, ret
		static readonly byte[] codeWithTryBlocks = Concat(
			new byte[] { ldc_i4_1, ldc_i4_0, div, pop, leave_s, 3 },
			new byte[] { pop, leave_s, 12 },
			new byte[] { nop, leave_s, 3 },
			new byte[] { pop, leave_s, 0 },
			Return(0),
			Return(0xCA),
			// The sections start at an address that is a multiple of 4 (the code has 27 bytes)
			new byte[1]);

		const int CodeSize = 27;

		// A section starts with its kind and its size (the header and the handlers). Small: sizes and offsets
		// have 1 or 2 bytes, a handler has 12 bytes. Fat: they have 4 bytes, a handler has 24 bytes.
		const byte ExceptionHandlers = 0x01, FatSection = 0x40, MoreSectionsFollow = 0x80;

		// A catch block: 0 (it's a catch block), the offset and the size of the try block, the offset and the
		// size of the catch block, and the token of the exception's type
		static byte[] SmallHandler(int tryOffset = 0, int trySize = 6, int handlerOffset = 6) =>
			Concat(Int16(0), Int16(tryOffset), new byte[] { (byte)trySize }, Int16(handlerOffset), new byte[] { 3 }, Int32(objectToken));
		static byte[] FatHandler(uint tryOffset = 0, uint trySize = 6, uint handlerOffset = 6) =>
			Concat(Int32(0), Int32(tryOffset), Int32(trySize), Int32(handlerOffset), Int32(3), Int32(objectToken));
		static byte[] SecondSmallHandler() => SmallHandler(9, 3, 12);
		static byte[] SecondFatHandler() => FatHandler(9, 3, 12);
		static byte[] CreateSmallSection(int kind, int size, params byte[][] handlers) => Concat(new byte[] { (byte)kind, (byte)size, 0, 0 }, Concat(handlers));
		static byte[] CreateFatSection(int kind, int size, params byte[][] handlers) => Concat(new byte[] { (byte)kind, (byte)size, (byte)(size >> 8), (byte)(size >> 16) }, Concat(handlers));

		static byte[] CreateImageWithSections(string sections) {
			var header = FatHeader(HeaderSize3 | MoreSections | InitLocals | Fat, 2, CodeSize);
			switch (sections) {
			case "small":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers, 16, SmallHandler())));
			case "fat":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateFatSection(ExceptionHandlers | FatSection, 28, FatHandler())));
			case "small, header doesn't say that there are sections":
				return CreateImage(Concat(FatHeader(HeaderSize3 | InitLocals | Fat, 2, CodeSize), codeWithTryBlocks, CreateSmallSection(ExceptionHandlers, 16, SmallHandler())));
			case "small, size without the 4 bytes of the header":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers, 12, SmallHandler())));
			case "small, size 15":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers, 15, SmallHandler())));
			case "small, size 27, zero bytes after the handler":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers, 27, SmallHandler()), new byte[12]));
			// The handler of the try block that does nothing is the first one: the method only returns 0xCA if
			// the second handler is read too
			case "small, 2 handlers":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers, 28, SecondSmallHandler(), SmallHandler())));
			case "small, 2 handlers, size 27":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers, 27, SecondSmallHandler(), SmallHandler())));
			case "small, 2 handlers, size 24":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers, 24, SecondSmallHandler(), SmallHandler())));
			case "small, 2 handlers, size 23":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers, 23, SecondSmallHandler(), SmallHandler())));
			case "fat, 2 handlers":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateFatSection(ExceptionHandlers | FatSection, 52, SecondFatHandler(), FatHandler())));
			case "fat, 2 handlers, size 48":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateFatSection(ExceptionHandlers | FatSection, 48, SecondFatHandler(), FatHandler())));
			case "fat, 2 handlers, size 47":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateFatSection(ExceptionHandlers | FatSection, 47, SecondFatHandler(), FatHandler())));
			case "small, size 0":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers, 0, SmallHandler())));
			case "small, reserved bytes aren't 0":
				return CreateImage(Concat(header, codeWithTryBlocks, new byte[] { ExceptionHandlers, 16, 0xFF, 0xFF }, SmallHandler()));
			case "fat, size without the 4 bytes of the header":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateFatSection(ExceptionHandlers | FatSection, 24, FatHandler())));
			case "fat, size 27":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateFatSection(ExceptionHandlers | FatSection, 27, FatHandler())));
			case "fat, size 0":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateFatSection(ExceptionHandlers | FatSection, 0, FatHandler())));
			case "fat, kind without the exception handler bit":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateFatSection(FatSection, 28, FatHandler())));
			case "small, kind 0":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(0, 16, SmallHandler())));
			case "small, kind 2":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(2, 16, SmallHandler())));
			case "small, unknown bits in the kind":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers | 0x3E, 16, SmallHandler())));
			case "small, not at a multiple of 4":
				// Without the bytes after the code
				return CreateImage(Concat(header, codeWithTryBlocks.Take(CodeSize).ToArray(), CreateSmallSection(ExceptionHandlers, 16, SmallHandler())));
			case "small, more sections follow but there are none":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers | MoreSectionsFollow, 16, SmallHandler()), new byte[32]));
			case "section of kind 2, then small":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(2 | MoreSectionsFollow, 8, new byte[4]), CreateSmallSection(ExceptionHandlers, 16, SmallHandler())));
			case "section of kind 0, then small":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(MoreSectionsFollow, 8, new byte[4]), CreateSmallSection(ExceptionHandlers, 16, SmallHandler())));
			case "empty small section, then small":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers | MoreSectionsFollow, 4), CreateSmallSection(ExceptionHandlers, 16, SmallHandler())));
			case "small, then a section of kind 2":
				return CreateImage(Concat(header, codeWithTryBlocks, CreateSmallSection(ExceptionHandlers | MoreSectionsFollow, 16, SmallHandler()), CreateSmallSection(2, 8, new byte[4])));
			default:
				throw new ArgumentException(sections);
			}
		}

		static string ReadHandlersWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var body = module.ResolveMethod(methodRid)!.Body;
				if (body is null)
					return "no body";
				if (body.Instructions.Count == 0)
					return "empty body";
				if (body.ExceptionHandlers.Count == 0)
					return "no handler";
				// Every handler as "try block, catch block"
				return string.Join(" ", body.ExceptionHandlers.Select(a => {
					Assert.Equal(ExceptionHandlerType.Catch, a.HandlerType);
					Assert.Equal("System.Object", a.CatchType!.FullName);
					return $"{a.TryStart!.Offset}-{a.TryEnd!.Offset},{a.HandlerStart!.Offset}-{a.HandlerEnd!.Offset}";
				}));
			}
		}

		// "returns 0xCA": the runtime read the handler
		[Theory]
		[InlineData("small", "0-6,6-9", "returns 0xCA")]
		[InlineData("fat", "0-6,6-9", "returns 0xCA")]
		[InlineData("small, header doesn't say that there are sections", "no handler", "throws DivideByZeroException")]
		[InlineData("small, size without the 4 bytes of the header", "0-6,6-9", "returns 0xCA")]
		[InlineData("small, size 15", "0-6,6-9", "returns 0xCA")]
		[InlineData("small, size 27, zero bytes after the handler", "0-6,6-9", "throws InvalidProgramException")]
		[InlineData("small, 2 handlers", "9-12,12-15 0-6,6-9", "returns 0xCA")]
		[InlineData("small, 2 handlers, size 27", "9-12,12-15 0-6,6-9", "returns 0xCA")]
		[InlineData("small, 2 handlers, size 24", "9-12,12-15 0-6,6-9", "returns 0xCA")]
		[InlineData("small, 2 handlers, size 23", "9-12,12-15", "throws DivideByZeroException")]
		[InlineData("fat, 2 handlers", "9-12,12-15 0-6,6-9", "returns 0xCA")]
		[InlineData("fat, 2 handlers, size 48", "9-12,12-15 0-6,6-9", "returns 0xCA")]
		[InlineData("fat, 2 handlers, size 47", "9-12,12-15", "throws DivideByZeroException")]
		[InlineData("small, size 0", "no handler", "throws DivideByZeroException")]
		[InlineData("small, reserved bytes aren't 0", "0-6,6-9", "returns 0xCA")]
		[InlineData("fat, size without the 4 bytes of the header", "0-6,6-9", "returns 0xCA")]
		[InlineData("fat, size 27", "0-6,6-9", "returns 0xCA")]
		[InlineData("fat, size 0", "no handler", "throws DivideByZeroException")]
		[InlineData("fat, kind without the exception handler bit", "no handler", "throws DivideByZeroException")]
		[InlineData("small, kind 0", "no handler", "throws DivideByZeroException")]
		[InlineData("small, kind 2", "no handler", "throws DivideByZeroException")]
		[InlineData("small, unknown bits in the kind", "no handler", "throws DivideByZeroException")]
		[InlineData("small, not at a multiple of 4", "no handler", "throws BadImageFormatException")]
		[InlineData("small, more sections follow but there are none", "0-6,6-9", "throws BadImageFormatException")]
		[InlineData("section of kind 2, then small", "no handler", "throws DivideByZeroException")]
		[InlineData("section of kind 0, then small", "no handler", "throws DivideByZeroException")]
		[InlineData("empty small section, then small", "no handler", "throws DivideByZeroException")]
		[InlineData("small, then a section of kind 2", "0-6,6-9", "returns 0xCA")]
		public void ExceptionHandlerSections(string sections, string expectedDnlib, string expectedRuntime) {
			var image = CreateImageWithSections(sections);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadHandlersWithDnlib(image), expectedRuntime, () => CallWithTheRuntime(image));
		}
	}
}
