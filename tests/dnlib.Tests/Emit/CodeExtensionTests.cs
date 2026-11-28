// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Xunit;

namespace dnlib.Tests.Emit {
	/// <summary>
	/// The <see cref="Code"/> of an instruction is its opcode's value. <see cref="CodeExtensions"/> gets the
	/// <see cref="OpCode"/> of a code: one of the one or two byte opcodes, or an experimental opcode
	/// (0xF0xx-0xFBxx) that a <see cref="ModuleContext"/> knows.
	/// </summary>
	public class CodeExtensionTests {
		static readonly OpCode experimental = new OpCode("experimental", 0xF1, 0x02, OperandType.InlineNone, FlowControl.Next, StackBehaviour.Push0, StackBehaviour.Pop0);

		[Fact]
		public void TheOpCodeOfACodeIsFound() {
			Assert.Same(OpCodes.Ret, Code.Ret.ToOpCode());
			Assert.Same(OpCodes.Ceq, Code.Ceq.ToOpCode());
			Assert.Same(OpCodes.Ret, Code.Ret.ToOpCodeOrThrow());
			var context = new ModuleContext();
			Assert.Same(OpCodes.Ret, Code.Ret.ToOpCode(context));
			Assert.Same(OpCodes.Ceq, Code.Ceq.ToOpCode(context));
			Assert.Same(OpCodes.Ceq, Code.Ceq.ToOpCodeOrThrow(context));
		}

		[Fact]
		public void AnExperimentalOpCodeIsOnlyFoundWithTheContextThatKnowsIt() {
			var code = (Code)0xF102;
			var context = new ModuleContext();

			Assert.True(code.IsExperimental());
			Assert.False(Code.Ret.IsExperimental());
			Assert.False(((Code)0xFE01).IsExperimental());
			Assert.False(((Code)0xFC00).IsExperimental());
			Assert.Null(code.ToOpCode());
			Assert.Null(code.ToOpCode(context));
			Assert.Throws<ArgumentOutOfRangeException>(() => code.ToOpCodeOrThrow());
			Assert.Throws<ArgumentOutOfRangeException>(() => code.ToOpCodeOrThrow(context));

			context.RegisterExperimentalOpCode(experimental);
			Assert.Same(experimental, code.ToOpCode(context));
			Assert.Same(experimental, code.ToOpCodeOrThrow(context));
			Assert.Null(code.ToOpCode());
		}

		// A code that is no opcode (eg. a two byte code whose first byte is neither 0xFE nor an experimental one)
		// has no opcode.
		// Fixed in this commit: with a ModuleContext, it threw an IndexOutOfRangeException.
		[Theory, InlineData(0xFC00), InlineData(0xFF01), InlineData(0x0100), InlineData(0xEF00)]
		public void ACodeThatIsNoOpCodeHasNoOpCode(int value) {
			var context = new ModuleContext();

			Assert.Null(((Code)value).ToOpCode());
			Assert.Null(((Code)value).ToOpCode(context));
			Assert.Null(context.GetExperimentalOpCode((byte)(value >> 8), (byte)value));
		}

		// Fixed in this commit: an opcode outside 0xF0xx-0xFBxx threw an IndexOutOfRangeException
		[Fact]
		public void OnlyAnExperimentalOpCodeCanBeRegistered() {
			var context = new ModuleContext();
			var notExperimental = new OpCode("other", 0xFC, 0x02, OperandType.InlineNone, FlowControl.Next, StackBehaviour.Push0, StackBehaviour.Pop0);

			Assert.Throws<ArgumentOutOfRangeException>(() => context.RegisterExperimentalOpCode(notExperimental));
			// There is nothing to clear
			context.ClearExperimentalOpCode(0xFC, 0x02);
		}

		[Fact]
		public void AnExperimentalOpCodeIsCleared() {
			var context = new ModuleContext();
			context.RegisterExperimentalOpCode(experimental);

			context.ClearExperimentalOpCode(0xF1, 0x02);

			Assert.Null(context.GetExperimentalOpCode(0xF1, 0x02));
			Assert.Null(((Code)0xF102).ToOpCode(context));
		}
	}
}
