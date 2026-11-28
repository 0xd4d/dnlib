// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.IO;
using dnlib.TestUtils;
using dnlib.Tests.Writer;
using Xunit;

namespace dnlib.Tests.Emit {
	/// <summary>
	/// Reads method bodies from bytes that the tests create: the header (tiny or fat), the IL code and the
	/// exception handler table. The cases are the ones a compiler doesn't create but the CLR reads.
	/// </summary>
	public class MethodBodyReaderTests {
		// Any module: the test bodies have no tokens except the ones that are meant to be invalid
		static readonly Lazy<ModuleDefMD> module = new Lazy<ModuleDefMD>(() => ModuleDefMD.Load(CSharpCompiler.Compile("public class C { }").Image));

		const byte nop = 0x00;
		const byte ret = 0x2A;
		static readonly byte[] sixNopsAndRet = { nop, nop, nop, nop, nop, nop, ret };

		static CilBody Read(byte[] body, MethodBodyReaderOptions options = MethodBodyReaderOptions.None, uint startOffset = 0) {
			var reader = ByteArrayDataReaderFactory.CreateReader(body);
			reader.Position = startOffset;
			return MethodBodyReader.CreateCilBody(module.Value, reader, null, new List<Parameter>(), new GenericParamContext(), null, options);
		}

		static byte[] Concat(params byte[][] parts) => parts.SelectMany(a => a).ToArray();

		static byte[] UInt16(int value) => LittleEndian.GetBytes((ushort)value);
		static byte[] UInt32(uint value) => LittleEndian.GetBytes(value);

		// A fat header: flags and the header's size in dwords (in the 4 high bits), max stack, code size and
		// the token of the locals' signature
		static byte[] FatHeader(int codeSize, int headerDwords = 3, bool hasMoreSections = false, bool initLocals = false, int maxStack = 5) {
			const int fatFormat = 3, moreSections = 8, initLocalsFlag = 0x10;
			int flags = fatFormat | (hasMoreSections ? moreSections : 0) | (initLocals ? initLocalsFlag : 0) | (headerDwords << 12);
			return Concat(UInt16(flags), UInt16(maxStack), UInt32((uint)codeSize), UInt32(0));
		}

		// The code is followed by zeros up to the next multiple of 4, then by the exception handler table
		static byte[] FatBodyWithHandlers(byte[] code, byte[] handlerTable) {
			var header = FatHeader(code.Length, hasMoreSections: true);
			int padding = (4 - (header.Length + code.Length) % 4) % 4;
			return Concat(header, code, new byte[padding], handlerTable);
		}

		// A fat exception handler table: its kind, its size in 3 bytes, and 24 bytes for every handler
		static byte[] FatHandlerTable(uint sizeInHeader, params byte[][] handlers) {
			const uint exceptionTable = 1, fatFormat = 0x40;
			return Concat(UInt32(exceptionTable | fatFormat | (sizeInHeader << 8)), Concat(handlers));
		}

		static byte[] FatHandler(ExceptionHandlerType type, uint tryStart, uint tryLength, uint handlerStart, uint handlerLength, uint tokenOrFilterStart = 0) =>
			Concat(UInt32((uint)type), UInt32(tryStart), UInt32(tryLength), UInt32(handlerStart), UInt32(handlerLength), UInt32(tokenOrFilterStart));

		// A small exception handler table: its kind, its size in 1 byte, 2 unused bytes, and 12 bytes for every handler
		static byte[] SmallHandlerTable(params byte[][] handlers) {
			const byte exceptionTable = 1;
			return Concat(new byte[] { exceptionTable, (byte)(4 + 12 * handlers.Length), 0, 0 }, Concat(handlers));
		}

		static byte[] SmallHandler(ExceptionHandlerType type, int tryStart, byte tryLength, int handlerStart, byte handlerLength, uint tokenOrFilterStart = 0) =>
			Concat(UInt16((int)type), UInt16(tryStart), new[] { tryLength }, UInt16(handlerStart), new[] { handlerLength }, UInt32(tokenOrFilterStart));

		[Fact]
		public void TinyHeader() {
			// A tiny header is one byte: the code size and the 2 bits that say it's tiny
			const int tinyFormat = 2;
			var body = Read(new byte[] { (2 << 2) | tinyFormat, nop, ret });

			Assert.Equal(new[] { OpCodes.Nop, OpCodes.Ret }, body.Instructions.Select(a => a.OpCode));
			Assert.Equal(8, body.MaxStack);
			Assert.Empty(body.Variables);
			Assert.Empty(body.ExceptionHandlers);
		}

		[Fact]
		public void FatHeaderWithoutExceptionHandlers() {
			var body = Read(Concat(FatHeader(2, initLocals: true, maxStack: 3), new byte[] { nop, ret }));

			Assert.Equal(new[] { OpCodes.Nop, OpCodes.Ret }, body.Instructions.Select(a => a.OpCode));
			Assert.Equal(3, body.MaxStack);
			Assert.True(body.InitLocals);
		}

		[Fact]
		public void FatExceptionHandlers() {
			var body = Read(FatBodyWithHandlers(sixNopsAndRet, FatHandlerTable(4 + 2 * 24,
				FatHandler(ExceptionHandlerType.Finally, 0, 2, 2, 2),
				FatHandler(ExceptionHandlerType.Filter, 0, 1, 3, 2, tokenOrFilterStart: 1))));

			var instructions = body.Instructions;
			Assert.Equal(7, instructions.Count);
			Assert.Equal(2, body.ExceptionHandlers.Count);
			var finallyHandler = body.ExceptionHandlers[0];
			Assert.Equal(ExceptionHandlerType.Finally, finallyHandler.HandlerType);
			Assert.Same(instructions[0], finallyHandler.TryStart);
			Assert.Same(instructions[2], finallyHandler.TryEnd);
			Assert.Same(instructions[2], finallyHandler.HandlerStart);
			Assert.Same(instructions[4], finallyHandler.HandlerEnd);
			var filterHandler = body.ExceptionHandlers[1];
			Assert.Equal(ExceptionHandlerType.Filter, filterHandler.HandlerType);
			Assert.Same(instructions[1], filterHandler.FilterStart);
			Assert.Same(instructions[3], filterHandler.HandlerStart);
			Assert.Same(instructions[5], filterHandler.HandlerEnd);
		}

		[Fact]
		public void SmallExceptionHandlers() {
			var header = FatHeader(sixNopsAndRet.Length, hasMoreSections: true);
			var table = SmallHandlerTable(
				SmallHandler(ExceptionHandlerType.Fault, 0, 2, 2, 2),
				SmallHandler(ExceptionHandlerType.Filter, 0, 1, 3, 2, tokenOrFilterStart: 1));
			var body = Read(Concat(header, sixNopsAndRet, new byte[1], table));

			var instructions = body.Instructions;
			Assert.Equal(2, body.ExceptionHandlers.Count);
			Assert.Equal(ExceptionHandlerType.Fault, body.ExceptionHandlers[0].HandlerType);
			Assert.Same(instructions[4], body.ExceptionHandlers[0].HandlerEnd);
			Assert.Same(instructions[1], body.ExceptionHandlers[1].FilterStart);
		}

		// A handler that ends at the end of the code has no end instruction
		[Fact]
		public void HandlerThatEndsAtTheEndOfTheCodeHasANullEnd() {
			var body = Read(FatBodyWithHandlers(sixNopsAndRet, FatHandlerTable(4 + 24, FatHandler(ExceptionHandlerType.Finally, 0, 2, 2, 5))));

			var handler = Assert.Single(body.ExceptionHandlers);
			Assert.Same(body.Instructions[2], handler.HandlerStart);
			Assert.Null(handler.HandlerEnd);
		}

		// A catch handler whose token isn't a type has no catch type
		[Fact]
		public void CatchHandlerWithAnInvalidTypeToken() {
			var body = Read(FatBodyWithHandlers(sixNopsAndRet, FatHandlerTable(4 + 24, FatHandler(ExceptionHandlerType.Catch, 0, 2, 2, 2, tokenOrFilterStart: 0x02FFFFFF))));

			var handler = Assert.Single(body.ExceptionHandlers);
			Assert.Equal(ExceptionHandlerType.Catch, handler.HandlerType);
			Assert.Null(handler.CatchType);
		}

		// Fixed in 1e6f772f: the number of handlers is the table's size / 24, and the CLR only uses the low
		// 16 bits of that number. 0x10001 handlers are 1 handler.
		[Fact]
		public void OnlyTheLow16BitsOfTheHandlerCountAreUsed() {
			uint sizeOf0x10001Handlers = 4 + 0x10001 * 24;
			var body = Read(FatBodyWithHandlers(sixNopsAndRet, FatHandlerTable(sizeOf0x10001Handlers, FatHandler(ExceptionHandlerType.Finally, 0, 2, 2, 2))));

			Assert.Single(body.ExceptionHandlers);
			Assert.Equal(7, body.Instructions.Count);
		}

		// Fixed in 2a25d94e: the reader threw InvalidMethodException. Now it's like every other method body that
		// can't be read: the body is empty.
		[Fact]
		public void HandlerTableWithMoreHandlersThanTheDataHasIsAnEmptyBody() {
			var tableOfThreeHandlersWithTwo = FatHandlerTable(4 + 3 * 24,
				FatHandler(ExceptionHandlerType.Finally, 0, 2, 2, 2),
				FatHandler(ExceptionHandlerType.Fault, 0, 1, 3, 2));
			var body = Read(FatBodyWithHandlers(sixNopsAndRet, tableOfThreeHandlersWithTwo));

			Assert.Empty(body.Instructions);
			Assert.Empty(body.ExceptionHandlers);
		}

		// Fixed in 124487e6: a fat header with a size of less than 3 dwords. The CLR rejects it in a file but
		// reads it if the IL comes from somewhere else (eg. a profiler): the code starts inside the 12 bytes of
		// the header, and there's no exception handler table. dnlib reads it by default.
		[Fact]
		public void CodeCanStartInsideTheFatHeader() {
			// The header says it's 2 dwords (8 bytes), so the 2 bytes of code are where the locals' token would be
			var bytes = Concat(FatHeader(2, headerDwords: 2, hasMoreSections: true).Take(8).ToArray(), new byte[] { nop, ret, 0, 0 });

			var body = Read(bytes);
			Assert.Equal(new[] { OpCodes.Nop, OpCodes.Ret }, body.Instructions.Select(a => a.OpCode));
			Assert.Empty(body.ExceptionHandlers);

			var rejected = Read(bytes, MethodBodyReaderOptions.DontAllowCodeInsideFatHeader);
			Assert.Empty(rejected.Instructions);
		}

		// Fixed in 124487e6: a fat header must start at an offset that's a multiple of 4. The 32-bit CLR and
		// Mono read it anyway, and so does dnlib by default.
		[Fact]
		public void FatHeaderThatIsNotAlignedIsRead() {
			var bytes = Concat(new byte[1], FatHeader(2), new byte[] { nop, ret });

			var body = Read(bytes, startOffset: 1);
			Assert.Equal(new[] { OpCodes.Nop, OpCodes.Ret }, body.Instructions.Select(a => a.OpCode));

			var rejected = Read(bytes, MethodBodyReaderOptions.DontAllowUnalignedFatHeader, startOffset: 1);
			Assert.Empty(rejected.Instructions);
		}

		[Fact]
		public void BodyWithMoreCodeThanDataIsEmpty() {
			var body = Read(Concat(FatHeader(100), new byte[] { nop, ret }));

			Assert.Empty(body.Instructions);
		}

		[Fact]
		public void UnknownHeaderFormatIsAnEmptyBody() {
			// The 2 low bits are 0 or 1: neither tiny nor fat
			Assert.Empty(Read(new byte[] { 0x00, nop, ret }).Instructions);
			Assert.Empty(Read(new byte[] { 0x01, nop, ret }).Instructions);
			Assert.Empty(Read(new byte[0]).Instructions);
		}

		// Fixed in e36ac476: the operand of calli is a StandAloneSig row. The CLR reads its signature as a
		// method signature even if the first byte says it's something else (here: a field signature), dnlib
		// returned null and wrote an invalid token.
		[Fact]
		public void CalliSignatureWithAnotherCallingConventionIsReadAsAMethodSignature() {
			var created = new CreatedModule();
			var int64 = created.Module.CorLibTypes.Int64;
			var signature = MethodSig.CreateStatic(int64, int64, int64, int64);
			created.AddMethod("Method",
				OpCodes.Ldc_I8.ToInstruction(1L),
				OpCodes.Ldc_I8.ToInstruction(2L),
				OpCodes.Ldc_I8.ToInstruction(3L),
				// The address of the function that's called. This code is only read, it never runs.
				OpCodes.Ldc_I4_0.ToInstruction(),
				OpCodes.Conv_I.ToInstruction(),
				OpCodes.Calli.ToInstruction(signature),
				OpCodes.Pop.ToInstruction(),
				OpCodes.Ret.ToInstruction());
			var image = created.Write();

			// The signature in the #Blob heap: its length, the calling convention (0 = default), 3 parameters,
			// then the return type and the parameter types (0x0A = int64)
			var signatureBlob = new byte[] { 6, 0, 3, 0x0A, 0x0A, 0x0A, 0x0A };
			int offset = FindOnlyMatch(image, signatureBlob);
			const byte fieldCallingConvention = 6;
			image[offset + 1] = fieldCallingConvention;

			using (var module = ModuleDefMD.Load(image)) {
				var method = CreatedModule.GetType(module).FindMethod("Method")!;
				var calli = method.Body!.Instructions.Single(a => a.OpCode == OpCodes.Calli);
				var operand = Assert.IsType<MethodSig>(calli.Operand);
				Assert.Equal(3, operand.Params.Count);
				Assert.Equal(ElementType.I8, operand.RetType!.ElementType);
			}
		}

		static int FindOnlyMatch(byte[] data, byte[] pattern) {
			var matches = new List<int>();
			for (int i = 0; i + pattern.Length <= data.Length; i++) {
				if (data.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
					matches.Add(i);
			}
			return Assert.Single(matches);
		}

		// Every overload reads the same body: from the whole body (header, code, exception handlers), with a reader
		// or a byte array and with the exception handlers in another array, or from the code with the values of a
		// header
		[Fact]
		public void EveryOverloadReadsTheSameBody() {
			var opResolver = module.Value;
			var method = new MethodDefUser("M", MethodSig.CreateStatic(opResolver.CorLibTypes.Void));
			var parameters = method.Parameters;
			var gpContext = new GenericParamContext();
			// A tiny header (code size 7)
			var body = Concat(new byte[] { (7 << 2) | 2 }, sixNopsAndRet);
			DataReader Reader() => ByteArrayDataReaderFactory.CreateReader(body);

			var bodies = new[] {
				MethodBodyReader.CreateCilBody(opResolver, Reader(), method),
				MethodBodyReader.CreateCilBody(opResolver, Reader(), method, gpContext),
				MethodBodyReader.CreateCilBody(opResolver, Reader(), parameters),
				MethodBodyReader.CreateCilBody(opResolver, Reader(), parameters, gpContext),
				MethodBodyReader.CreateCilBody(opResolver, Reader(), parameters, gpContext, null),
				MethodBodyReader.CreateCilBody(opResolver, body, null, parameters),
				MethodBodyReader.CreateCilBody(opResolver, Reader(), null, parameters),
				MethodBodyReader.CreateCilBody(opResolver, Reader(), null, parameters, gpContext),
				MethodBodyReader.CreateCilBody(opResolver, Reader(), null, parameters, gpContext, null),
				MethodBodyReader.CreateCilBody(opResolver, sixNopsAndRet, null, parameters, 0x0013, 8, 7, 0),
				MethodBodyReader.CreateCilBody(opResolver, sixNopsAndRet, null, parameters, 0x0013, 8, 7, 0, gpContext),
			};
			Assert.All(bodies, a => Assert.Equal(7, a.Instructions.Count));

			var readers = new[] {
				new MethodBodyReader(opResolver, Reader(), method),
				new MethodBodyReader(opResolver, Reader(), method, gpContext),
				new MethodBodyReader(opResolver, Reader(), parameters),
				new MethodBodyReader(opResolver, Reader(), parameters, gpContext),
				new MethodBodyReader(opResolver, Reader(), null, parameters),
				new MethodBodyReader(opResolver, Reader(), null, parameters, gpContext),
			};
			foreach (var reader in readers) {
				Assert.True(reader.Read());
				Assert.Equal(7, reader.CreateCilBody().Instructions.Count);
			}
			// Code with more bytes than a method can have is an empty body
			Assert.Empty(MethodBodyReader.CreateCilBody(opResolver, sixNopsAndRet, null, parameters, 0x0013, 8, 0x40000000, 0).Instructions);
			// A token is resolved without a generic context too
			Assert.Null(((IInstructionOperandResolver)opResolver).ResolveToken(0x70FFFFFF));
		}

		// A header that isn't a method header can't be read
		[Fact]
		public void ABodyThatIsNotAMethodBodyIsNotRead() {
			var reader = new MethodBodyReader(module.Value, ByteArrayDataReaderFactory.CreateReader(new byte[] { 0 }), new List<Parameter>());

			Assert.False(reader.Read());
		}

		// ldarg with a 2-byte index (the long form, InlineVar), and the 'unaligned.' prefix with its 1-byte operand
		[Fact]
		public void LongArgumentIndexAndUnalignedPrefixAreRead() {
			// ldarg 0, unaligned. 1, ldind.i1, pop, ret
			var code = new byte[] { 0xFE, 0x09, 0x00, 0x00, 0xFE, 0x12, 0x01, 0x46, 0x26, 0x2A };
			var body = Concat(new byte[] { (byte)((code.Length << 2) | 2) }, code);
			var method = new MethodDefUser("M", MethodSig.CreateStatic(module.Value.CorLibTypes.Void, new PtrSig(module.Value.CorLibTypes.SByte)));

			var read = MethodBodyReader.CreateCilBody(module.Value, ByteArrayDataReaderFactory.CreateReader(body), method.Parameters);

			Assert.Equal(new[] { Code.Ldarg, Code.Unaligned, Code.Ldind_I1, Code.Pop, Code.Ret }, read.Instructions.Select(a => a.OpCode.Code).ToArray());
			Assert.Same(method.Parameters[0], read.Instructions[0].Operand);
			Assert.Equal((byte)1, read.Instructions[1].Operand);
		}
	}
}
