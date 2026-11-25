// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Loading {
	/// <summary>
	/// A program can hide its code and its strings from tools: the method bodies and strings in the file
	/// aren't the ones that it uses when it runs. A tool that knows how to get the real ones gives
	/// <see cref="ModuleDefMD"/> an <see cref="IMethodDecrypter"/> and an <see cref="IStringDecrypter"/>: they're
	/// asked for every method body and every string before the file is read.
	/// </summary>
	public class DecrypterTests {
		const string Source = @"
public abstract class Program {
	static int Number(int unused) => 1;
	static string Text() => ""original"";
	static string OtherText() => ""other"";
	public abstract void NoBody();
	public static int Main() => Number(0) == 42 && Text() == ""replaced"" && OtherText() == ""other"" ? 0 : 1;
}
";
		static readonly byte[] image = CSharpCompiler.Compile(Source, new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication }).Image;

		// Returns a body with "return 42" for one method, and remembers what it was asked for
		sealed class MethodDecrypter : IMethodDecrypter {
			readonly uint ridToReplace;
			public List<(uint Rid, RVA Rva, int Parameters)> Calls { get; } = new List<(uint, RVA, int)>();

			public MethodDecrypter(uint ridToReplace) => this.ridToReplace = ridToReplace;

			public bool GetMethodBody(uint rid, RVA rva, IList<Parameter> parameters, GenericParamContext gpContext, out MethodBody methodBody) {
				Calls.Add((rid, rva, parameters.Count));
				if (rid != ridToReplace) {
					methodBody = null!;
					return false;
				}
				var body = new CilBody();
				body.Instructions.Add(Instruction.CreateLdcI4(42));
				body.Instructions.Add(OpCodes.Ret.ToInstruction());
				methodBody = body;
				return true;
			}
		}

		// Returns another string for the string "original"
		sealed class StringDecrypter : IStringDecrypter {
			readonly ModuleDefMD module;
			public List<uint> Tokens { get; } = new List<uint>();

			public StringDecrypter(ModuleDefMD module) => this.module = module;

			public string? ReadUserString(uint token) {
				Tokens.Add(token);
				return module.USStream.ReadNoNull(token & 0x00FFFFFF) == "original" ? "replaced" : null;
			}
		}

		static TypeDef GetProgram(ModuleDef module) => module.Find("Program", isReflectionName: true)!;

		[Fact]
		public void ProgramDoesNotWorkAsItIsInTheFile() =>
			Assert.Equal(1, AssemblyRunner.RunEntryPoint(image));

		[Fact]
		public void MethodBodyOfTheDecrypterIsUsed() {
			using (var module = ModuleDefMD.Load(image)) {
				var number = GetProgram(module).FindMethod("Number")!;
				var text = GetProgram(module).FindMethod("Text")!;
				var decrypter = new MethodDecrypter(number.Rid);
				module.MethodDecrypter = decrypter;
				Assert.Same(decrypter, module.MethodDecrypter);

				Assert.Equal(new[] { Code.Ldc_I4_S, Code.Ret }, number.Body!.Instructions.Select(a => a.OpCode.Code).ToArray());
				Assert.Equal(42, number.Body.Instructions[0].GetLdcI4Value());
				// The decrypter said that it has no body for this method: it's read from the file
				Assert.Equal(new[] { Code.Ldstr, Code.Ret }, text.Body!.Instructions.Select(a => a.OpCode.Code).ToArray());

				// It was asked once for each method, with the method's row and what the row says
				Assert.Equal(new[] { (number.Rid, number.RVA, 1), (text.Rid, text.RVA, 0) }, decrypter.Calls.ToArray());
				Assert.NotEqual((RVA)0, number.RVA);
			}
		}

		// A method without code in the file (RVA 0) gets a body too if the decrypter has one
		[Fact]
		public void DecrypterIsAskedForMethodsWithoutABodyToo() {
			using (var module = ModuleDefMD.Load(image)) {
				var noBody = GetProgram(module).FindMethod("NoBody")!;
				Assert.Equal((RVA)0, noBody.RVA);
				module.MethodDecrypter = new MethodDecrypter(noBody.Rid);

				Assert.NotNull(noBody.Body);
				Assert.Equal(2, noBody.Body.Instructions.Count);
			}
			using (var module = ModuleDefMD.Load(image))
				Assert.Null(GetProgram(module).FindMethod("NoBody")!.Body);
		}

		[Fact]
		public void StringOfTheDecrypterIsUsed() {
			using (var module = ModuleDefMD.Load(image)) {
				var decrypter = new StringDecrypter(module);
				module.StringDecrypter = decrypter;
				Assert.Same(decrypter, module.StringDecrypter);
				var text = GetProgram(module).FindMethod("Text")!;
				var otherText = GetProgram(module).FindMethod("OtherText")!;

				Assert.Equal("replaced", text.Body!.Instructions[0].Operand);
				// The decrypter returned null: the string of the file
				Assert.Equal("other", otherText.Body!.Instructions[0].Operand);

				// The tokens are the ones of the ldstr instructions: table 0x70 and the offset in the #US heap
				Assert.Equal(2, decrypter.Tokens.Count);
				Assert.All(decrypter.Tokens, token => Assert.Equal(0x70U, token >> 24));
				Assert.Equal("replaced", module.ReadUserString(decrypter.Tokens[0]));
				Assert.Equal("other", module.ReadUserString(decrypter.Tokens[1]));
				Assert.Equal("original", module.USStream.ReadNoNull(decrypter.Tokens[0] & 0x00FFFFFF));
			}
		}

		// What the decrypters returned is what is written: the new file works
		[Fact]
		public void WrittenProgramHasTheDecryptedBodiesAndStrings() {
			var stream = new MemoryStream();
			using (var module = ModuleDefMD.Load(image)) {
				module.MethodDecrypter = new MethodDecrypter(GetProgram(module).FindMethod("Number")!.Rid);
				module.StringDecrypter = new StringDecrypter(module);
				module.Write(stream);
			}

			Assert.Equal(0, AssemblyRunner.RunEntryPoint(stream.ToArray()));
			using (var written = ModuleDefMD.Load(stream.ToArray())) {
				Assert.Equal("replaced", GetProgram(written).FindMethod("Text")!.Body!.Instructions[0].Operand);
				Assert.Equal(42, GetProgram(written).FindMethod("Number")!.Body!.Instructions[0].GetLdcI4Value());
			}
		}

		// A body that was read before the decrypter was set isn't read again
		[Fact]
		public void DecrypterMustBeSetBeforeTheBodiesAreRead() {
			using (var module = ModuleDefMD.Load(image)) {
				var number = GetProgram(module).FindMethod("Number")!;
				var bodyOfTheFile = number.Body;
				var decrypter = new MethodDecrypter(number.Rid);
				module.MethodDecrypter = decrypter;

				Assert.Same(bodyOfTheFile, number.Body);
				Assert.Empty(decrypter.Calls);
			}
		}
	}
}
