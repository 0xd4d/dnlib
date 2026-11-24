// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.IO;
using dnlib.Tests.Writer;
using Xunit;

namespace dnlib.Tests.Emit {
	/// <summary>
	/// <see cref="MethodBodyReaderBase"/> is the base class of the readers that turn IL bytes into instructions.
	/// Other programs derive from it to read code that isn't in the normal place or format (eg. a deobfuscator
	/// that has decrypted a method): the class reads the instructions, the derived class says what the tokens
	/// are and adds the locals and the exception handlers. The tests use such a derived class.
	/// </summary>
	public class CustomMethodBodyReaderTests {
		/// <summary>
		/// Reads IL code that has no header. Every token in the code is the index of an object in a list.
		/// </summary>
		sealed class ListTokenReader : MethodBodyReaderBase {
			readonly IList<object> operands;

			public ListTokenReader(byte[] code, IList<object> operands, IList<Parameter>? parameters = null)
				: base(ByteArrayDataReaderFactory.CreateReader(code), parameters) => this.operands = operands;

			object? GetOperand(uint token) => token < (uint)operands.Count ? operands[(int)token] : null;

			protected override IField? ReadInlineField(Instruction instr) => GetOperand(reader.ReadUInt32()) as IField;
			protected override IMethod? ReadInlineMethod(Instruction instr) => GetOperand(reader.ReadUInt32()) as IMethod;
			protected override MethodSig? ReadInlineSig(Instruction instr) => GetOperand(reader.ReadUInt32()) as MethodSig;
			protected override string ReadInlineString(Instruction instr) => GetOperand(reader.ReadUInt32()) as string ?? string.Empty;
			protected override ITokenOperand? ReadInlineTok(Instruction instr) => GetOperand(reader.ReadUInt32()) as ITokenOperand;
			protected override ITypeDefOrRef? ReadInlineType(Instruction instr) => GetOperand(reader.ReadUInt32()) as ITypeDefOrRef;

			public void ReadCode(int codeSize) => ReadInstructionsNumBytes((uint)codeSize);
			public void ReadSomeInstructions(int count) => ReadInstructions(count);
			public void SetLocalTypes(IList<TypeSig?>? types) => SetLocals(types);
			public void CopyLocals(IList<Local>? locals) => SetLocals(locals);
			public Instruction? FindInstruction(uint offset) => GetInstruction(offset);
			public Instruction FindInstructionOrThrow(uint offset) => GetInstructionOrThrow(offset);
			public bool AddHandler(ExceptionHandler handler) => Add(handler);
		}

		readonly ModuleDefUser module = new ModuleDefUser("Test.dll");
		ICorLibTypes Types => module.CorLibTypes;

		const byte nop = 0x00, ldloc1 = 0x07, ldstr = 0x72, call = 0x28, pop = 0x26, brS = 0x2B, ret = 0x2A;

		[Fact]
		public void InstructionsAreReadWithTheOperandsOfTheDerivedClass() {
			var method = new MemberRefUser(module, "Method", MethodSig.CreateStatic(Types.Void, Types.String), Types.GetTypeRef("System", "Console"));
			// ldstr (token 0); call (token 1); ldloc.1; pop; br.s to the ret; nop; ret
			var code = new byte[] { ldstr, 0, 0, 0, 0, call, 1, 0, 0, 0, ldloc1, pop, brS, 1, nop, ret };
			var reader = new ListTokenReader(code, new object[] { "text", method });
			reader.SetLocalTypes(new TypeSig?[] { Types.Int32, Types.String });

			reader.ReadCode(code.Length);

			var instructions = reader.Instructions;
			Assert.Equal(new[] { Code.Ldstr, Code.Call, Code.Ldloc_1, Code.Pop, Code.Br_S, Code.Nop, Code.Ret }, instructions.Select(a => a.OpCode.Code).ToArray());
			Assert.Equal(new uint[] { 0, 5, 10, 11, 12, 14, 15 }, instructions.Select(a => a.Offset));
			Assert.Equal("text", instructions[0].Operand);
			Assert.Same(method, instructions[1].Operand);
			// The target of a branch is the instruction, not its offset
			Assert.Same(instructions[6], instructions[4].Operand);
			Assert.Equal(new[] { "System.Int32", "System.String" }, reader.Locals.Select(a => a.Type!.FullName));
			Assert.Same(instructions[2], reader.FindInstruction(10));
			Assert.Same(instructions[2], reader.FindInstructionOrThrow(10));
			// Offset 6 is in the middle of the call instruction
			Assert.Null(reader.FindInstruction(6));
			Assert.Null(reader.FindInstruction(100));
			Assert.Throws<InvalidOperationException>(() => reader.FindInstructionOrThrow(6));
		}

		[Fact]
		public void NumberOfInstructionsCanBeGivenInsteadOfTheSizeOfTheCode() {
			var code = new byte[] { nop, nop, nop, ret, nop };
			var reader = new ListTokenReader(code, new object[0]);

			reader.ReadSomeInstructions(4);

			Assert.Equal(new[] { Code.Nop, Code.Nop, Code.Nop, Code.Ret }, reader.Instructions.Select(a => a.OpCode.Code).ToArray());
			// More instructions than the code has: the reader stops at the end of the code
			var readerOfAll = new ListTokenReader(code, new object[0]);
			readerOfAll.ReadSomeInstructions(100);
			Assert.Equal(5, readerOfAll.Instructions.Count);
		}

		[Fact]
		public void LocalsAreCopiedOrCreatedFromTypes() {
			var reader = new ListTokenReader(new byte[] { ret }, new object[0]);
			var original = new Local(Types.Int32, "named");

			reader.CopyLocals(new[] { original });
			var copy = Assert.Single(reader.Locals);
			Assert.NotSame(original, copy);
			Assert.Same(Types.Int32, copy.Type);

			reader.SetLocalTypes(new TypeSig?[] { Types.String, null });
			Assert.Equal(2, reader.Locals.Count);
			Assert.Null(reader.Locals[1].Type);
			reader.SetLocalTypes(null);
			Assert.Empty(reader.Locals);
			reader.CopyLocals(null);
			Assert.Empty(reader.Locals);
		}

		// The base class only adds an exception handler that can be valid: the protected code and the handler
		// aren't empty and don't overlap, and a filter starts before its handler
		[Theory]
		[InlineData(0, 2, 2, 4, -1, true)]      // try 0-2, handler 2-4
		[InlineData(0, 2, 4, 6, 2, true)]       // with a filter at 2
		[InlineData(2, 2, 3, 4, -1, false)]     // empty try
		[InlineData(3, 2, 4, 5, -1, false)]     // try ends before it starts
		[InlineData(0, 2, 3, 3, -1, false)]     // empty handler
		[InlineData(1, 3, 0, 2, -1, false)]     // the handler contains the start of the try
		[InlineData(0, 2, 1, 3, -1, false)]     // the handler contains the end of the try
		[InlineData(0, 4, 1, 2, -1, false)]     // the try contains the handler
		[InlineData(0, 3, 2, 5, -1, false)]     // the try contains the start of the handler
		[InlineData(0, 2, 4, 6, 4, false)]      // the filter doesn't start before the handler
		public void OnlyHandlersThatCanBeValidAreAdded(int tryStart, int tryEnd, int handlerStart, int handlerEnd, int filterStart, bool isAdded) {
			var reader = new ListTokenReader(new byte[] { nop, nop, nop, nop, nop, nop, ret }, new object[0]);
			reader.ReadCode(7);
			var instructions = reader.Instructions;
			var handler = new ExceptionHandler(filterStart >= 0 ? ExceptionHandlerType.Filter : ExceptionHandlerType.Finally) {
				TryStart = instructions[tryStart],
				TryEnd = instructions[tryEnd],
				HandlerStart = instructions[handlerStart],
				HandlerEnd = instructions[handlerEnd],
				FilterStart = filterStart >= 0 ? instructions[filterStart] : null,
			};

			Assert.Equal(isAdded, reader.AddHandler(handler));

			Assert.Equal(isAdded ? 1 : 0, reader.ExceptionHandlers.Count);
		}

		[Fact]
		public void FilterWithoutAStartAndHandlerUpToTheEndOfTheCode() {
			var reader = new ListTokenReader(new byte[] { nop, nop, nop, ret }, new object[0]);
			reader.ReadCode(4);
			var instructions = reader.Instructions;

			Assert.False(reader.AddHandler(new ExceptionHandler(ExceptionHandlerType.Filter) {
				TryStart = instructions[0],
				TryEnd = instructions[1],
				HandlerStart = instructions[2],
				HandlerEnd = instructions[3],
			}));
			// No end instruction: the handler ends at the end of the code
			Assert.True(reader.AddHandler(new ExceptionHandler(ExceptionHandlerType.Finally) {
				TryStart = instructions[0],
				TryEnd = instructions[2],
				HandlerStart = instructions[2],
				HandlerEnd = null,
			}));
		}

		// What was read replaces the body of a method
		[Fact]
		public void ReadBodyCanBePutIntoAMethod() {
			var created = new CreatedModule();
			var method = created.AddMethod("M", OpCodes.Ldc_I4_0.ToInstruction(), OpCodes.Pop.ToInstruction(), OpCodes.Ret.ToInstruction());
			method.Body!.Variables.Add(new Local(created.Module.CorLibTypes.Int64));
			var reader = new ListTokenReader(new byte[] { nop, nop, nop, ret }, new object[0]);
			reader.SetLocalTypes(new TypeSig?[] { created.Module.CorLibTypes.Int32 });
			reader.ReadCode(4);
			Assert.True(reader.AddHandler(new ExceptionHandler(ExceptionHandlerType.Finally) {
				TryStart = reader.Instructions[0],
				TryEnd = reader.Instructions[1],
				HandlerStart = reader.Instructions[1],
				HandlerEnd = reader.Instructions[2],
			}));

			reader.RestoreMethod(method);

			Assert.Equal(new[] { Code.Nop, Code.Nop, Code.Nop, Code.Ret }, method.Body.Instructions.Select(a => a.OpCode.Code).ToArray());
			Assert.Equal("System.Int32", Assert.Single(method.Body.Variables).Type!.FullName);
			Assert.Single(method.Body.ExceptionHandlers);
			// A method without a body can't get one this way
			var noBody = new MethodDefUser("NoBody", MethodSig.CreateStatic(created.Module.CorLibTypes.Void));
			Assert.ThrowsAny<Exception>(() => reader.RestoreMethod(noBody));
		}

		[Fact]
		public void ArgumentInstructionsUseTheParametersThatTheReaderGot() {
			var methodDef = new MethodDefUser("M", MethodSig.CreateStatic(Types.Void, Types.Int32, Types.String), MethodAttributes.Static);
			const byte ldarg1 = 0x03, ldargS = 0x0E;
			var reader = new ListTokenReader(new byte[] { ldarg1, pop, ldargS, 0, pop, ldargS, 9, pop, ret }, new object[0], methodDef.Parameters);

			reader.ReadCode(9);

			Assert.Same(methodDef.Parameters, reader.Parameters);
			Assert.Same(methodDef.Parameters[0], reader.Instructions[2].Operand);
			// There's no parameter 9
			Assert.Null(reader.Instructions[5].Operand);
		}
	}
}
