// dnlib: See LICENSE.txt for more info

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

		[Fact]
		public void BranchToAnInstructionThatIsNotInTheMethodIsAnError() {
			var created = new CreatedModule();
			created.AddMethod("M", OpCodes.Br.ToInstruction(OpCodes.Nop.ToInstruction()), OpCodes.Ret.ToInstruction());

			Assert.Throws<ModuleWriterException>(() => created.Write());
		}
	}
}
