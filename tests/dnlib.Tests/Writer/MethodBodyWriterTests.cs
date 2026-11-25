// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// How method bodies are written: the header (tiny or fat), the instructions and the exception handlers
	/// </summary>
	public class MethodBodyWriterTests {
		const int TinyHeaderSize = 1;
		const int FatHeaderSize = 12;

		static CilBody WriteAndReadBody(CreatedModule created, string methodName, out ModuleDefMD module) {
			module = created.WriteAndRead();
			var body = CreatedModule.GetType(module).FindMethod(methodName)!.Body;
			Assert.NotNull(body);
			return body;
		}

		[Fact]
		public void SmallMethodHasATinyHeader() {
			var created = new CreatedModule();
			created.AddMethod("M", OpCodes.Ret.ToInstruction());

			var body = WriteAndReadBody(created, "M", out var module);
			using (module) {
				Assert.Equal(TinyHeaderSize, body.HeaderSize);
				Assert.Equal(Code.Ret, Assert.Single(body.Instructions).OpCode.Code);
			}
		}

		[Fact]
		public void MethodWithLocalsHasAFatHeader() {
			var created = new CreatedModule();
			var method = created.AddMethod("M", OpCodes.Ret.ToInstruction());
			method.Body!.Variables.Add(new Local(created.Module.CorLibTypes.Int32));
			method.Body.InitLocals = true;

			var body = WriteAndReadBody(created, "M", out var module);
			using (module) {
				Assert.Equal(FatHeaderSize, body.HeaderSize);
				Assert.True(body.InitLocals);
				Assert.Equal("System.Int32", Assert.Single(body.Variables).Type?.FullName);
			}
		}

		[Fact]
		public void MethodWith64BytesOfCodeHasAFatHeader() {
			// A tiny header stores the code size in 6 bits
			var created = new CreatedModule();
			var instructions = new Instruction[64];
			for (int i = 0; i < 63; i++)
				instructions[i] = OpCodes.Nop.ToInstruction();
			instructions[63] = OpCodes.Ret.ToInstruction();
			created.AddMethod("Size64", instructions);
			var shorter = new Instruction[63];
			System.Array.Copy(instructions, 1, shorter, 0, 63);
			var created63 = new CreatedModule();
			for (int i = 0; i < shorter.Length; i++)
				shorter[i] = new Instruction(shorter[i].OpCode);
			created63.AddMethod("Size63", shorter);

			var body64 = WriteAndReadBody(created, "Size64", out var module64);
			var body63 = WriteAndReadBody(created63, "Size63", out var module63);
			using (module64)
			using (module63) {
				Assert.Equal(FatHeaderSize, body64.HeaderSize);
				Assert.Equal(TinyHeaderSize, body63.HeaderSize);
			}
		}

		// Fixed in 49bff1ff: a body without instructions wasn't written, the method lost its body
		[Fact]
		public void EmptyBodyIsWritten() {
			var created = new CreatedModule();
			created.AddMethod("Empty");

			using (var module = created.WriteAndRead()) {
				var method = CreatedModule.GetType(module).FindMethod("Empty")!;

				Assert.NotEqual(0U, (uint)method.RVA);
				Assert.NotNull(method.Body);
				Assert.Empty(method.Body.Instructions);
			}
		}

		[Fact]
		public void MethodWithoutBodyHasNoRva() {
			var created = new CreatedModule();
			var method = created.AddMethod("NoBody");
			method.Body = null;

			using (var module = created.WriteAndRead()) {
				var written = CreatedModule.GetType(module).FindMethod("NoBody")!;

				Assert.Equal(0U, (uint)written.RVA);
				Assert.Null(written.Body);
			}
		}

		// Fixed in da49fdd2: a tiny header has no InitLocals bit, so the localloc memory wasn't zeroed anymore
		[Fact]
		public void MethodWithInitLocalsAndLocallocKeepsAFatHeader() {
			var created = new CreatedModule();
			var method = created.AddMethod("M",
				Instruction.CreateLdcI4(16), OpCodes.Conv_U.ToInstruction(), OpCodes.Localloc.ToInstruction(), OpCodes.Pop.ToInstruction(), OpCodes.Ret.ToInstruction());
			method.Body!.InitLocals = true;

			var body = WriteAndReadBody(created, "M", out var module);
			using (module) {
				Assert.Equal(FatHeaderSize, body.HeaderSize);
				Assert.True(body.InitLocals);
			}
		}

		[Fact]
		public void MethodWithLocallocButWithoutInitLocalsHasATinyHeader() {
			var created = new CreatedModule();
			var method = created.AddMethod("M",
				Instruction.CreateLdcI4(16), OpCodes.Conv_U.ToInstruction(), OpCodes.Localloc.ToInstruction(), OpCodes.Pop.ToInstruction(), OpCodes.Ret.ToInstruction());
			method.Body!.InitLocals = false;

			var body = WriteAndReadBody(created, "M", out var module);
			using (module) {
				Assert.Equal(TinyHeaderSize, body.HeaderSize);
				// The reader knows that a tiny method with a localloc can't have InitLocals
				Assert.False(body.InitLocals);
			}
		}

		// A method with a try/finally: nop; try { nop; leave } finally { endfinally }; ret
		static MethodDefUser AddMethodWithHandler(CreatedModule created, out ExceptionHandler handler) {
			var ret = OpCodes.Ret.ToInstruction();
			var tryStart = OpCodes.Nop.ToInstruction();
			var handlerStart = OpCodes.Endfinally.ToInstruction();
			var method = created.AddMethod("M", OpCodes.Nop.ToInstruction(), tryStart, OpCodes.Leave.ToInstruction(ret), handlerStart, ret);
			handler = new ExceptionHandler(ExceptionHandlerType.Finally) {
				TryStart = tryStart,
				TryEnd = handlerStart,
				HandlerStart = handlerStart,
				HandlerEnd = ret,
			};
			method.Body!.ExceptionHandlers.Add(handler);
			return method;
		}

		[Fact]
		public void ExceptionHandlerIsWritten() {
			var created = new CreatedModule();
			AddMethodWithHandler(created, out _);

			var body = WriteAndReadBody(created, "M", out var module);
			using (module) {
				var handler = Assert.Single(body.ExceptionHandlers);
				Assert.Equal(ExceptionHandlerType.Finally, handler.HandlerType);
				Assert.Same(body.Instructions[1], handler.TryStart);
				Assert.Same(body.Instructions[3], handler.TryEnd);
				Assert.Same(body.Instructions[3], handler.HandlerStart);
				Assert.Same(body.Instructions[4], handler.HandlerEnd);
			}
		}

		// Fixed in 2f821c55: a small exception clause has 16-bit flags, the upper bits were lost
		[Fact]
		public void ExceptionHandlerWithFlagsThatNeedMoreThan16BitsIsWritten() {
			var created = new CreatedModule();
			AddMethodWithHandler(created, out var handler);
			handler.HandlerType = ExceptionHandlerType.Finally | (ExceptionHandlerType)0x10000;

			var body = WriteAndReadBody(created, "M", out var module);
			using (module)
				Assert.Equal(ExceptionHandlerType.Finally | (ExceptionHandlerType)0x10000, Assert.Single(body.ExceptionHandlers).HandlerType);
		}

		// Fixed in bb8408d9: null means the end of the method, but such a handler looked empty and was dropped
		[Fact]
		public void ExceptionHandlerThatEndsAtTheEndOfTheMethodIsWritten() {
			var created = new CreatedModule();
			// try { nop; leave } finally { endfinally } with nothing after the handler
			var tryStart = OpCodes.Nop.ToInstruction();
			var handlerStart = OpCodes.Endfinally.ToInstruction();
			var method = created.AddMethod("M", tryStart, OpCodes.Leave.ToInstruction(tryStart), handlerStart);
			method.Body!.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally) {
				TryStart = tryStart,
				TryEnd = handlerStart,
				HandlerStart = handlerStart,
				HandlerEnd = null,
			});

			var body = WriteAndReadBody(created, "M", out var module);
			using (module) {
				var handler = Assert.Single(body.ExceptionHandlers);
				Assert.Same(body.Instructions[2], handler.HandlerStart);
				Assert.Null(handler.HandlerEnd);
			}
		}

		// Adds: nops; try { nops; leave } finally { nops; endfinally }. The leave has 5 bytes, the endfinally 1.
		static void AddTryFinally(List<Instruction> instructions, List<ExceptionHandler> handlers, int nopsBefore, int tryLength, int handlerLength) {
			Instruction Nop() {
				var nop = OpCodes.Nop.ToInstruction();
				instructions.Add(nop);
				return nop;
			}
			for (int i = 0; i < nopsBefore; i++)
				Nop();
			var after = OpCodes.Nop.ToInstruction();
			var leave = OpCodes.Leave.ToInstruction(after);
			var tryStart = tryLength == 5 ? leave : Nop();
			for (int i = 1; i < tryLength - 5; i++)
				Nop();
			instructions.Add(leave);
			var endfinally = OpCodes.Endfinally.ToInstruction();
			var handlerStart = handlerLength == 1 ? endfinally : Nop();
			for (int i = 1; i < handlerLength - 1; i++)
				Nop();
			instructions.Add(endfinally);
			instructions.Add(after);
			handlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally) { TryStart = tryStart, TryEnd = handlerStart, HandlerStart = handlerStart, HandlerEnd = after });
		}

		// The first byte of the exception handler section of a method with a fat header: 0x01 = it has the
		// exception handlers, 0x40 = they're in the big format
		static bool HasFatExceptionClauses(byte[] image, MethodDef method) {
			using (var peImage = new dnlib.PE.PEImage(image)) {
				int offset = (int)peImage.ToFileOffset(method.RVA);
				Assert.Equal(0x3003, System.BitConverter.ToUInt16(image, offset) & 0xF003);
				int codeSize = System.BitConverter.ToInt32(image, offset + 4);
				int sectionOffset = (offset + FatHeaderSize + codeSize + 3) & ~3;
				Assert.Equal(1, image[sectionOffset] & 0x3F);
				return (image[sectionOffset] & 0x40) != 0;
			}
		}

		// An exception handler is written in the small format (12 bytes) or in the big one (24 bytes). All
		// handlers of a method have the same format. The small one has 2 bytes for an offset, 1 byte for a
		// length, and its section has 1 byte for its size: that's enough for 20 handlers.
		[Theory]
		[InlineData(1, 0, 5, 1, false)]
		[InlineData(20, 0, 5, 1, false)]
		[InlineData(21, 0, 5, 1, true)]
		[InlineData(1, 0, 0xFF, 1, false)]          // the length of the try block
		[InlineData(1, 0, 0x100, 1, true)]
		[InlineData(1, 0, 5, 0xFF, false)]          // the length of the handler
		[InlineData(1, 0, 5, 0x100, true)]
		[InlineData(1, 0xFFFF, 5, 1, true)]         // the offset of the try block is 0xFFFF, of the handler 0x10004
		[InlineData(1, 0xFFFF - 5, 5, 1, false)]    // 0xFFFA and 0xFFFF
		[InlineData(1, 0xFFFF - 4, 5, 1, true)]     // 0xFFFB and 0x10000
		[InlineData(1, 0x10000, 5, 1, true)]
		[InlineData(20, 0x100, 0xFF, 0xFF, false)]
		public void ExceptionHandlersAreSmallIfTheirOffsetsLengthsAndCountAllowIt(int handlerCount, int nopsBefore, int tryLength, int handlerLength, bool fat) {
			var created = new CreatedModule();
			var instructions = new List<Instruction>();
			var handlers = new List<ExceptionHandler>();
			for (int i = 0; i < handlerCount; i++)
				AddTryFinally(instructions, handlers, i == 0 ? nopsBefore : 0, tryLength, handlerLength);
			instructions.Add(OpCodes.Ret.ToInstruction());
			var method = created.AddMethod("M", instructions.ToArray());
			foreach (var handler in handlers)
				method.Body!.ExceptionHandlers.Add(handler);

			var image = created.Write();

			using (var module = ModuleDefMD.Load(image)) {
				var written = CreatedModule.GetType(module).FindMethod("M")!;
				Assert.Equal(fat, HasFatExceptionClauses(image, written));
				var body = written.Body!;
				Assert.Equal(handlerCount, body.ExceptionHandlers.Count);
				body.UpdateInstructionOffsets();
				method.Body!.UpdateInstructionOffsets();
				for (int i = 0; i < handlerCount; i++) {
					var expected = method.Body.ExceptionHandlers[i];
					var actual = body.ExceptionHandlers[i];
					Assert.Equal(ExceptionHandlerType.Finally, actual.HandlerType);
					Assert.Equal((expected.TryStart!.Offset, expected.TryEnd!.Offset, expected.HandlerStart!.Offset, expected.HandlerEnd!.Offset),
						(actual.TryStart!.Offset, actual.TryEnd!.Offset, actual.HandlerStart!.Offset, actual.HandlerEnd!.Offset));
				}
				Assert.Equal((uint)nopsBefore, body.ExceptionHandlers[0].TryStart!.Offset);
				Assert.Equal((uint)tryLength, body.ExceptionHandlers[0].TryEnd!.Offset - body.ExceptionHandlers[0].TryStart!.Offset);
				Assert.Equal((uint)handlerLength, body.ExceptionHandlers[0].HandlerEnd!.Offset - body.ExceptionHandlers[0].HandlerStart!.Offset);
			}
		}

		[Fact]
		public void BranchToAnInstructionThatIsNotInTheMethodIsAnError() {
			var created = new CreatedModule();
			created.AddMethod("M", OpCodes.Br.ToInstruction(OpCodes.Nop.ToInstruction()), OpCodes.Ret.ToInstruction());

			Assert.Throws<ModuleWriterException>(() => created.Write());
		}

		// The instructions of a method that can't be written: the operand isn't what the instruction needs
		static Instruction[] CreateInvalidInstructions(ModuleDef module, string what) {
			var ret = OpCodes.Ret.ToInstruction();
			var pop = OpCodes.Pop.ToInstruction();
			// A local that isn't a local of the method, with any index
			Local LocalWithIndex(int index) => new Local(module.CorLibTypes.Int32, null, index);
			switch (what) {
			case "ldc.i4 with a string": return new[] { new Instruction(OpCodes.Ldc_I4, "text"), pop, ret };
			case "ldc.i8 with an int": return new[] { new Instruction(OpCodes.Ldc_I8, 5), pop, ret };
			case "ldc.r4 with a double": return new[] { new Instruction(OpCodes.Ldc_R4, 1.5), pop, ret };
			case "ldc.r8 with a float": return new[] { new Instruction(OpCodes.Ldc_R8, 1.5f), pop, ret };
			case "ldloc with a number": return new[] { new Instruction(OpCodes.Ldloc, 0), pop, ret };
			case "ldloc.s with a number": return new[] { new Instruction(OpCodes.Ldloc_S, (byte)0), pop, ret };
			case "ldloc.s with local 256": return new[] { new Instruction(OpCodes.Ldloc_S, LocalWithIndex(256)), pop, ret };
			case "ldloc with local 65536": return new[] { new Instruction(OpCodes.Ldloc, LocalWithIndex(65536)), pop, ret };
			case "switch with one instruction": return new[] { OpCodes.Ldc_I4_0.ToInstruction(), new Instruction(OpCodes.Switch, ret), ret };
			case "branch without a target": return new[] { new Instruction(OpCodes.Br, null), ret };
			case "short branch to an instruction that is 200 bytes away": {
				var instructions = new List<Instruction> { new Instruction(OpCodes.Br_S, ret) };
				instructions.AddRange(Enumerable.Range(0, 200).Select(a => OpCodes.Nop.ToInstruction()));
				instructions.Add(ret);
				return instructions.ToArray();
			}
			default: throw new ArgumentException(what);
			}
		}

		[Theory]
		[InlineData("ldc.i4 with a string", "Operand is not an Int32")]
		[InlineData("ldc.i8 with an int", "Operand is not an Int64")]
		[InlineData("ldc.r4 with a double", "Operand is not a Single")]
		[InlineData("ldc.r8 with a float", "Operand is not a Double")]
		[InlineData("ldloc with a number", "Operand is not a local/arg")]
		[InlineData("ldloc.s with a number", "Operand is not a local/arg")]
		[InlineData("ldloc.s with local 256", "Local/arg index doesn't fit in a Byte")]
		[InlineData("ldloc with local 65536", "Local/arg index doesn't fit in a UInt16")]
		[InlineData("switch with one instruction", "switch operand is not a list of instructions")]
		// The first error is that the size of the stack can't be calculated
		[InlineData("branch without a target", "Error calculating max stack value")]
		[InlineData("short branch to an instruction that is 200 bytes away", "Target instruction is too far away for a short branch")]
		public void InstructionWithAnOperandItCanNotHaveIsAnError(string what, string expectedError) {
			var created = new CreatedModule();
			created.AddMethod("M", CreateInvalidInstructions(created.Module, what));

			var exception = Assert.Throws<ModuleWriterException>(() => created.Write());
			Assert.Contains(expectedError, exception.Message);

			// With a logger that doesn't throw, the method is written with some other operand
			var stream = new MemoryStream();
			created.Module.Write(stream, new ModuleWriterOptions(created.Module) { Logger = DummyLogger.NoThrowInstance });
			using (var module = ModuleDefMD.Load(stream.ToArray()))
				Assert.Equal(Code.Ret, CreatedModule.GetType(module).FindMethod("M")!.Body!.Instructions.Last().OpCode.Code);
		}

		// The operand of an instruction can be a signature: it's written as a row of the StandAloneSig table.
		// A method signature is the one of a calli instruction, a field signature is only used by obfuscated code:
		// dnlib reads no operand for it.
		[Theory]
		[InlineData(false, false)]
		[InlineData(true, false)]
		[InlineData(false, true)]
		[InlineData(true, true)]
		public void SignatureOperandIsARowOfTheStandAloneSigTable(bool isFieldSignature, bool preserveTokens) {
			var created = new CreatedModule();
			var types = created.Module.CorLibTypes;
			CallingConventionSig signature = isFieldSignature ? new FieldSig(types.String) : MethodSig.CreateStatic(types.Int32, types.String);
			created.AddMethod("Method", OpCodes.Ldnull.ToInstruction(), OpCodes.Ldnull.ToInstruction(), new Instruction(OpCodes.Calli, signature), OpCodes.Pop.ToInstruction(), OpCodes.Ret.ToInstruction());
			var stream = new MemoryStream();
			var options = new ModuleWriterOptions(created.Module);
			if (preserveTokens)
				options.MetadataOptions.Flags |= MetadataFlags.PreserveAll;

			created.Module.Write(stream, options);

			using (var module = ModuleDefMD.Load(stream.ToArray())) {
				Assert.Equal(1U, module.TablesStream.StandAloneSigTable.Rows);
				var standAloneSig = module.ResolveStandAloneSig(1)!;
				Assert.Equal(isFieldSignature ? "System.String" : "System.Int32 (System.String)", isFieldSignature ? (standAloneSig.Signature as FieldSig)?.Type?.FullName : standAloneSig.MethodSig?.ToString());
				var instruction = CreatedModule.GetType(module).FindMethod("Method")!.Body!.Instructions[2];
				// The operand of the instruction is the signature of that row if it's a method signature
				if (isFieldSignature)
					Assert.Null(instruction.Operand);
				else
					Assert.True(new SigComparer().Equals(signature, instruction.Operand as CallingConventionSig), $"The operand is {instruction.Operand}");
			}
		}
	}
}
