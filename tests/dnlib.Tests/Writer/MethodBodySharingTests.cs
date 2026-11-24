// dnlib: See LICENSE.txt for more info

using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// Methods whose bodies are the same bytes (header, code and exception handlers) need the bytes only once
	/// in the file: they get the same RVA. The writer does that by default
	/// (<see cref="ModuleWriterOptionsBase.ShareMethodBodies"/>).
	/// </summary>
	public class MethodBodySharingTests {
		static Instruction[] ReturnNumber(int number) => new[] { Instruction.CreateLdcI4(number), OpCodes.Pop.ToInstruction(), OpCodes.Ret.ToInstruction() };

		// Two methods with the same body, one with another body
		static CreatedModule CreateModule() {
			var created = new CreatedModule();
			created.AddMethod("First", ReturnNumber(1000));
			created.AddMethod("Same", ReturnNumber(1000));
			created.AddMethod("Other", ReturnNumber(2000));
			return created;
		}

		static byte[] Write(ModuleDef module, bool shareMethodBodies) {
			var stream = new MemoryStream();
			module.Write(stream, new ModuleWriterOptions(module) { ShareMethodBodies = shareMethodBodies });
			return stream.ToArray();
		}

		static uint GetRva(ModuleDefMD module, string name) => (uint)CreatedModule.GetType(module).FindMethod(name)!.RVA;

		[Fact]
		public void MethodsWithTheSameBodyHaveTheSameRvaByDefault() {
			var created = CreateModule();
			Assert.True(new ModuleWriterOptions(created.Module).ShareMethodBodies);

			using (var module = created.WriteAndRead()) {
				Assert.Equal(GetRva(module, "First"), GetRva(module, "Same"));
				Assert.NotEqual(GetRva(module, "First"), GetRva(module, "Other"));
				Assert.NotEqual(0U, GetRva(module, "First"));
			}
		}

		[Fact]
		public void EveryMethodHasItsOwnBodyIfBodiesAreNotShared() {
			var created = CreateModule();
			var shared = Write(created.Module, shareMethodBodies: true);
			var notShared = Write(created.Module, shareMethodBodies: false);

			using (var module = ModuleDefMD.Load(notShared)) {
				var rvas = new[] { GetRva(module, "First"), GetRva(module, "Same"), GetRva(module, "Other") };
				Assert.Equal(3, rvas.Distinct().Count());
				using (var sharedModule = ModuleDefMD.Load(shared))
					dnlib.TestUtils.Dumping.ModuleComparer.AssertEqual(sharedModule, module);
			}
		}

		// A body with locals has a header with the token of the locals' signature: the same code with other
		// locals is another body
		[Fact]
		public void SameCodeWithOtherLocalsIsNotShared() {
			var created = new CreatedModule();
			var types = created.Module.CorLibTypes;
			created.AddMethod("Int32Local", ReturnNumber(1000)).Body!.Variables.Add(new Local(types.Int32));
			created.AddMethod("SameLocal", ReturnNumber(1000)).Body!.Variables.Add(new Local(types.Int32));
			created.AddMethod("StringLocal", ReturnNumber(1000)).Body!.Variables.Add(new Local(types.String));
			created.AddMethod("NoLocal", ReturnNumber(1000));

			using (var module = created.WriteAndRead()) {
				Assert.Equal(GetRva(module, "Int32Local"), GetRva(module, "SameLocal"));
				Assert.NotEqual(GetRva(module, "Int32Local"), GetRva(module, "StringLocal"));
				Assert.NotEqual(GetRva(module, "Int32Local"), GetRva(module, "NoLocal"));
			}
		}

		static MethodDef AddMethodWithHandler(CreatedModule created, string name, ExceptionHandlerType handlerType) {
			var method = created.AddMethod(name, OpCodes.Nop.ToInstruction(), OpCodes.Nop.ToInstruction(), OpCodes.Pop.ToInstruction(), OpCodes.Nop.ToInstruction(), OpCodes.Ret.ToInstruction());
			var instructions = method.Body!.Instructions;
			instructions[1] = OpCodes.Leave_S.ToInstruction(instructions[4]);
			instructions[3] = handlerType == ExceptionHandlerType.Catch ? OpCodes.Leave_S.ToInstruction(instructions[4]) : OpCodes.Endfinally.ToInstruction();
			if (handlerType != ExceptionHandlerType.Catch)
				instructions[2] = OpCodes.Nop.ToInstruction();
			method.Body.ExceptionHandlers.Add(new ExceptionHandler(handlerType) {
				TryStart = instructions[0],
				TryEnd = instructions[2],
				HandlerStart = instructions[2],
				HandlerEnd = instructions[4],
				CatchType = handlerType == ExceptionHandlerType.Catch ? created.Module.CorLibTypes.Object.TypeDefOrRef : null,
			});
			return method;
		}

		// The exception handlers are a part of the body
		[Fact]
		public void BodiesWithTheSameExceptionHandlersAreShared() {
			var created = new CreatedModule();
			AddMethodWithHandler(created, "Finally", ExceptionHandlerType.Finally);
			AddMethodWithHandler(created, "SameFinally", ExceptionHandlerType.Finally);
			AddMethodWithHandler(created, "Fault", ExceptionHandlerType.Fault);

			using (var module = created.WriteAndRead()) {
				Assert.Equal(GetRva(module, "Finally"), GetRva(module, "SameFinally"));
				// The same code, but another kind of handler
				Assert.NotEqual(GetRva(module, "Finally"), GetRva(module, "Fault"));
				Assert.Equal(ExceptionHandlerType.Fault, CreatedModule.GetType(module).FindMethod("Fault")!.Body!.ExceptionHandlers.Single().HandlerType);
			}
		}

		// A compiled program whose methods share a body still runs
		[Fact]
		public void ProgramWithSharedBodiesRuns() {
			const string source = @"
public static class Program {
	static int First(int a) => a * 3 + 1;
	static int Second(int a) => a * 3 + 1;
	static int Third(int a) => a * 3 + 2;
	public static int Main() => First(1) == 4 && Second(2) == 7 && Third(1) == 5 ? 0 : 1;
}";
			var image = CSharpCompiler.Compile(source, new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication }).Image;
			using (var original = ModuleDefMD.Load(image)) {
				var shared = Write(original, shareMethodBodies: true);
				var notShared = Write(original, shareMethodBodies: false);

				using (var module = ModuleDefMD.Load(shared)) {
					var program = module.Find("Program", isReflectionName: true)!;
					Assert.Equal(program.FindMethod("First")!.RVA, program.FindMethod("Second")!.RVA);
					Assert.NotEqual(program.FindMethod("First")!.RVA, program.FindMethod("Third")!.RVA);
				}
				using (var module = ModuleDefMD.Load(notShared)) {
					var program = module.Find("Program", isReflectionName: true)!;
					Assert.NotEqual(program.FindMethod("First")!.RVA, program.FindMethod("Second")!.RVA);
				}
				Assert.Equal(0, AssemblyRunner.RunEntryPoint(shared));
				Assert.Equal(0, AssemblyRunner.RunEntryPoint(notShared));
			}
		}
	}
}
