// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using dnlib.IO;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// <see cref="MethodBodyWriter"/> writes the bytes of one method body (the header, the IL code and the
	/// exception handlers) without writing a module. The caller decides which token every operand gets
	/// (<see cref="ITokenProvider"/>). <see cref="MethodBodyReader"/> is the opposite. The tests write every
	/// method body of the test input and read it again.
	/// </summary>
	public sealed class StandaloneMethodBodyWriterTests : IDisposable {
		readonly ModuleDefMD module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image);

		public void Dispose() => module.Dispose();

		/// <summary>
		/// Gives everything the token it has in the module, and a string a token with its number in a list. It
		/// also finds them again by their tokens.
		/// </summary>
		sealed class Tokens : ITokenProvider, IInstructionOperandResolver {
			readonly ModuleDefMD module;
			readonly List<string> strings = new List<string>();
			public readonly List<string> Errors = new List<string>();
			const uint stringTokenBase = 0x70000000;

			public Tokens(ModuleDefMD module) => this.module = module;

			public MDToken GetToken(object? o) {
				if (o is string s) {
					strings.Add(s);
					return new MDToken(stringTokenBase + (uint)strings.Count);
				}
				if (o is IMDTokenProvider tokenProvider && tokenProvider.MDToken.Rid != 0)
					return tokenProvider.MDToken;
				// The signature of a calli instruction: a StandAloneSig row has it
				if (o is MethodSig methodSig) {
					for (uint rid = 1; rid <= module.TablesStream.StandAloneSigTable.Rows; rid++) {
						if (new SigComparer().Equals(module.ResolveStandAloneSig(rid)?.MethodSig, methodSig))
							return new MDToken(dnlib.DotNet.MD.Table.StandAloneSig, rid);
					}
				}
				Errors.Add("No token: " + o);
				return default;
			}

			// The body's locals keep the token they have in the module
			public MDToken GetToken(IList<TypeSig?> locals, uint origToken) => new MDToken(origToken);

			public void Error(string message) => Errors.Add(message);

			public IMDTokenProvider? ResolveToken(uint token, GenericParamContext gpContext) => module.ResolveToken(token, gpContext);

			public string ReadUserString(uint token) => strings[(int)(token - stringTokenBase) - 1];
		}

		// Everything that the reader and the writer must keep
		static string Describe(CilBody body) {
			var lines = new List<string> { $"max stack {body.MaxStack}, init locals {body.InitLocals}" };
			lines.AddRange(body.Variables.Select(a => $"local {a.Index}: {a.Type}"));
			lines.AddRange(body.Instructions.Select(a => a.ToString()!));
			string Offset(Instruction? instruction) => instruction is null ? "end" : instruction.Offset.ToString("X4");
			lines.AddRange(body.ExceptionHandlers.Select(a =>
				$"{a.HandlerType} try {Offset(a.TryStart)}-{Offset(a.TryEnd)} handler {Offset(a.HandlerStart)}-{Offset(a.HandlerEnd)} filter {Offset(a.FilterStart)} catch {a.CatchType}"));
			return string.Join("\n", lines);
		}

		IEnumerable<MethodDef> MethodsWithBodies => module.GetTypes().SelectMany(a => a.Methods).Where(a => a.HasBody);

		[Fact]
		public void EveryWrittenBodyIsReadAsTheSameBody() {
			int bodiesWithHandlers = 0, tinyBodies = 0;
			foreach (var method in MethodsWithBodies) {
				var body = method.Body!;
				var tokens = new Tokens(module);
				var writer = new MethodBodyWriter(tokens, method, keepMaxStack: true);

				writer.Write();

				Assert.Empty(tokens.Errors);
				Assert.Equal(body.LocalVarSigTok, writer.LocalVarSigTok);
				Assert.Equal(body.HasExceptionHandlers, writer.ExtraSections is not null);
				var read = MethodBodyReader.CreateCilBody(tokens, writer.Code, writer.ExtraSections, method.Parameters, GenericParamContext.Create(method));
				Assert.True(Describe(body) == Describe(read), method.FullName);

				// The same bytes in one array: the exception handlers are after the code, at an offset that's a
				// multiple of 4
				var fullBody = writer.GetFullMethodBody();
				var readFromOneArray = MethodBodyReader.CreateCilBody(tokens, ByteArrayDataReaderFactory.CreateReader(fullBody), method.Parameters, GenericParamContext.Create(method));
				Assert.True(Describe(body) == Describe(readFromOneArray), method.FullName);

				if (body.HasExceptionHandlers)
					bodiesWithHandlers++;
				// A tiny header is one byte: the size of the code and the value 2
				if ((writer.Code[0] & 3) == 2)
					tinyBodies++;
			}
			Assert.True(bodiesWithHandlers >= 3 && tinyBodies > 10, $"{bodiesWithHandlers} bodies with exception handlers, {tinyBodies} tiny bodies");
		}

		// The max stack value is calculated from the instructions unless the caller wants to keep the body's value
		[Fact]
		public void MaxStackIsCalculatedOrKept() {
			var method = MethodsWithBodies.First(a => a.Body!.Variables.Count != 0 && !a.Body.HasExceptionHandlers);
			ushort calculated = method.Body!.MaxStack;
			method.Body.MaxStack = 123;

			ushort Write(MethodBodyWriter writer) {
				writer.Write();
				// A body with locals has a fat header: flags and size (2 bytes), then max stack (2 bytes)
				return BitConverter.ToUInt16(writer.Code, 2);
			}

			Assert.Equal(123, Write(new MethodBodyWriter(new Tokens(module), method, keepMaxStack: true)));
			Assert.Equal(123, Write(new MethodBodyWriter(new Tokens(module), method.Body, keepMaxStack: true)));
			Assert.Equal(calculated, Write(new MethodBodyWriter(new Tokens(module), method)));
			Assert.Equal(calculated, Write(new MethodBodyWriter(new Tokens(module), method.Body)));
		}

		// A reader can also get the values of the header from the caller: the code array then only has the IL code.
		// That's how the bodies of dynamic methods are read.
		[Fact]
		public void BodyIsReadFromCodeWithoutAHeader() {
			foreach (var method in MethodsWithBodies) {
				var body = method.Body!;
				var tokens = new Tokens(module);
				var writer = new MethodBodyWriter(tokens, method, keepMaxStack: true);
				writer.Write();
				bool isTiny = (writer.Code[0] & 3) == 2;
				int headerSize = isTiny ? 1 : 12;
				var codeOnly = writer.Code.Skip(headerSize).ToArray();
				ushort flags = isTiny ? (ushort)2 : BitConverter.ToUInt16(writer.Code, 0);

				var read = MethodBodyReader.CreateCilBody(tokens, codeOnly, writer.ExtraSections, method.Parameters, flags, body.MaxStack,
					(uint)codeOnly.Length, body.LocalVarSigTok, GenericParamContext.Create(method));

				Assert.True(Describe(body) == Describe(read), method.FullName);
			}
		}

		[Fact]
		public void OperandWithoutATokenIsReportedToTheTokenProvider() {
			var method = MethodsWithBodies.First(a => a.Body!.Instructions.Any(b => b.OpCode.Code == Code.Call));
			var call = method.Body!.Instructions.First(a => a.OpCode.Code == Code.Call);
			// A reference that isn't in the module
			call.Operand = new MemberRefUser(module, "Missing", MethodSig.CreateStatic(module.CorLibTypes.Void), module.CorLibTypes.Object.TypeDefOrRef);
			var tokens = new Tokens(module);

			new MethodBodyWriter(tokens, method).Write();

			Assert.Contains(tokens.Errors, a => a.Contains("Missing"));
		}
	}
}
