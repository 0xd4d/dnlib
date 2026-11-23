// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Xunit;

namespace dnlib.Tests.Emit {
	/// <summary>
	/// <see cref="Instruction"/>: creating instructions, their size, what they do to the evaluation stack, and
	/// the helper methods that find the local or parameter an instruction uses
	/// </summary>
	public class InstructionTests {
		static readonly ModuleDefUser module = new ModuleDefUser("Test.dll");
		static ICorLibTypes CorLibTypes => module.CorLibTypes;

		// The size of the operand of every operand type, ECMA-335 III.1.2 - III.1.9
		static int GetOperandSize(OperandType operandType) {
			switch (operandType) {
			case OperandType.InlineNone:
				return 0;
			case OperandType.ShortInlineBrTarget:
			case OperandType.ShortInlineI:
			case OperandType.ShortInlineVar:
				return 1;
			case OperandType.InlineVar:
				return 2;
			case OperandType.InlineI8:
			case OperandType.InlineR:
				return 8;
			default:
				// A 32-bit number or branch offset, a float, or a token
				return 4;
			}
		}

		// Without the entries of the tables that aren't opcodes (unused values and prefix bytes)
		static IEnumerable<OpCode> GetAllOpCodes() =>
			OpCodes.OneByteOpCodes.Concat(OpCodes.TwoByteOpCodes).Where(a => a.OpCodeType != OpCodeType.Nternal);

		[Fact]
		public void SizeIsTheOpCodeAndItsOperand() {
			int count = 0;
			foreach (var opCode in GetAllOpCodes()) {
				// A switch has a variable size, see below
				if (opCode.OperandType == OperandType.InlineSwitch || opCode.OperandType == OperandType.InlinePhi)
					continue;
				var instruction = new Instruction(opCode);

				Assert.Equal(opCode.Size + GetOperandSize(opCode.OperandType), instruction.GetSize());
				count++;
			}
			Assert.True(count >= 200, $"Only {count} opcodes were checked");
		}

		[Fact]
		public void SizeOfASwitchDependsOnItsTargets() {
			var target = OpCodes.Nop.ToInstruction();

			// The opcode, the number of targets and 4 bytes for each target
			Assert.Equal(1 + 4, Instruction.Create(OpCodes.Switch, new List<Instruction>()).GetSize());
			Assert.Equal(1 + 4 + 3 * 4, Instruction.Create(OpCodes.Switch, new[] { target, target, target }).GetSize());
			Assert.Equal(1 + 4, new Instruction(OpCodes.Switch, null).GetSize());
		}

		[Fact]
		public void CreateChecksTheOperandType() {
			Assert.Equal(5, Instruction.Create(OpCodes.Ldc_I4, 5).Operand);
			Assert.Equal((sbyte)-5, Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)-5).Operand);
			Assert.Equal(5L, Instruction.Create(OpCodes.Ldc_I8, 5L).Operand);
			Assert.Equal(1.5f, Instruction.Create(OpCodes.Ldc_R4, 1.5f).Operand);
			Assert.Equal(1.5, Instruction.Create(OpCodes.Ldc_R8, 1.5).Operand);
			Assert.Equal("text", Instruction.Create(OpCodes.Ldstr, "text").Operand);
			Assert.Null(Instruction.Create(OpCodes.Ret).Operand);

			// An operand of another type than the opcode's
			Assert.Throws<System.ArgumentException>(() => Instruction.Create(OpCodes.Ldc_I4));
			Assert.Throws<System.ArgumentException>(() => Instruction.Create(OpCodes.Ret, 5));
			Assert.Throws<System.ArgumentException>(() => Instruction.Create(OpCodes.Ldc_I4, 5L));
			Assert.Throws<System.ArgumentException>(() => Instruction.Create(OpCodes.Ldstr, 1.5));
			Assert.Throws<System.ArgumentException>(() => Instruction.Create(OpCodes.Ldc_I4, "text"));
			Assert.Throws<System.ArgumentException>(() => Instruction.Create(OpCodes.Br, new List<Instruction>()));
		}

		[Theory]
		[InlineData(-1, Code.Ldc_I4_M1)]
		[InlineData(0, Code.Ldc_I4_0)]
		[InlineData(8, Code.Ldc_I4_8)]
		[InlineData(9, Code.Ldc_I4_S)]
		[InlineData(-2, Code.Ldc_I4_S)]
		[InlineData(127, Code.Ldc_I4_S)]
		[InlineData(-128, Code.Ldc_I4_S)]
		[InlineData(128, Code.Ldc_I4)]
		[InlineData(-129, Code.Ldc_I4)]
		[InlineData(int.MaxValue, Code.Ldc_I4)]
		public void CreateLdcI4UsesTheShortestInstruction(int value, Code expectedCode) {
			var instruction = Instruction.CreateLdcI4(value);

			Assert.Equal(expectedCode, instruction.OpCode.Code);
			Assert.True(instruction.IsLdcI4());
			Assert.Equal(value, instruction.GetLdcI4Value());
		}

		// The number of values an instruction removes from the stack and puts on it
		static (int pushes, int pops) GetStackUsage(Instruction instruction, bool methodHasReturnValue = false) {
			instruction.CalculateStackUsage(methodHasReturnValue, out int pushes, out int pops);
			return (pushes, pops);
		}

		[Fact]
		public void StackUsageOfSimpleInstructions() {
			Assert.Equal((0, 0), GetStackUsage(OpCodes.Nop.ToInstruction()));
			Assert.Equal((1, 0), GetStackUsage(OpCodes.Ldc_I4_0.ToInstruction()));
			Assert.Equal((1, 2), GetStackUsage(OpCodes.Add.ToInstruction()));
			Assert.Equal((2, 1), GetStackUsage(OpCodes.Dup.ToInstruction()));
			Assert.Equal((0, 1), GetStackUsage(OpCodes.Pop.ToInstruction()));
			Assert.Equal((0, 3), GetStackUsage(OpCodes.Stelem_I4.ToInstruction()));
			Assert.Equal((0, 2), GetStackUsage(OpCodes.Beq.ToInstruction(OpCodes.Nop.ToInstruction())));
		}

		[Fact]
		public void StackUsageOfRetDependsOnTheMethod() {
			Assert.Equal((0, 0), GetStackUsage(OpCodes.Ret.ToInstruction(), methodHasReturnValue: false));
			Assert.Equal((0, 1), GetStackUsage(OpCodes.Ret.ToInstruction(), methodHasReturnValue: true));
		}

		// -1 means: the instruction removes everything from the stack
		[Fact]
		public void LeaveAndEndfinallyEmptyTheStack() {
			Assert.Equal((0, -1), GetStackUsage(OpCodes.Leave.ToInstruction(OpCodes.Nop.ToInstruction())));
			Assert.Equal((0, -1), GetStackUsage(OpCodes.Endfinally.ToInstruction()));

			int stack = 5;
			OpCodes.Leave.ToInstruction(OpCodes.Nop.ToInstruction()).UpdateStack(ref stack);
			Assert.Equal(0, stack);
		}

		static MemberRefUser CreateMethodRef(MethodSig signature) =>
			new MemberRefUser(module, "Method", signature, CorLibTypes.GetTypeRef("System", "Console"));

		// A call removes the arguments (and the object of an instance method) and puts the return value on the stack
		[Fact]
		public void StackUsageOfCallsDependsOnTheSignature() {
			var staticVoid = CreateMethodRef(MethodSig.CreateStatic(CorLibTypes.Void, CorLibTypes.Int32, CorLibTypes.String));
			var instanceWithResult = CreateMethodRef(MethodSig.CreateInstance(CorLibTypes.Int32, CorLibTypes.Int32));

			Assert.Equal((0, 2), GetStackUsage(OpCodes.Call.ToInstruction(staticVoid)));
			Assert.Equal((1, 2), GetStackUsage(OpCodes.Call.ToInstruction(instanceWithResult)));
			Assert.Equal((1, 2), GetStackUsage(OpCodes.Callvirt.ToInstruction(instanceWithResult)));
			// newobj creates the object: it isn't an argument, and it's the result
			Assert.Equal((1, 1), GetStackUsage(OpCodes.Newobj.ToInstruction(instanceWithResult)));
			// calli also removes the address of the function
			Assert.Equal((0, 3), GetStackUsage(OpCodes.Calli.ToInstruction(staticVoid.MethodSig!)));
			// No signature: nothing is known
			Assert.Equal((0, 0), GetStackUsage(new Instruction(OpCodes.Call, null)));
		}

		[Fact]
		public void UpdateStackAddsPushesAndRemovesPops() {
			int stack = 0;
			OpCodes.Ldc_I4_1.ToInstruction().UpdateStack(ref stack);
			OpCodes.Ldc_I4_2.ToInstruction().UpdateStack(ref stack);
			Assert.Equal(2, stack);
			OpCodes.Add.ToInstruction().UpdateStack(ref stack);
			Assert.Equal(1, stack);
			OpCodes.Ret.ToInstruction().UpdateStack(ref stack, methodHasReturnValue: true);
			Assert.Equal(0, stack);
		}

		[Fact]
		public void BranchKinds() {
			var target = OpCodes.Nop.ToInstruction();

			Assert.True(OpCodes.Br.ToInstruction(target).IsBr());
			Assert.True(OpCodes.Br_S.ToInstruction(target).IsBr());
			Assert.False(OpCodes.Brtrue.ToInstruction(target).IsBr());
			Assert.True(OpCodes.Brtrue_S.ToInstruction(target).IsBrtrue());
			Assert.True(OpCodes.Brfalse.ToInstruction(target).IsBrfalse());
			Assert.True(OpCodes.Leave_S.ToInstruction(target).IsLeave());
			Assert.True(OpCodes.Blt_Un.ToInstruction(target).IsConditionalBranch());
			Assert.True(OpCodes.Brtrue.ToInstruction(target).IsConditionalBranch());
			Assert.False(OpCodes.Br.ToInstruction(target).IsConditionalBranch());
			Assert.False(OpCodes.Leave.ToInstruction(target).IsConditionalBranch());
			Assert.False(OpCodes.Switch.ToInstruction(new[] { target }).IsConditionalBranch());
		}

		[Fact]
		public void LocalOfLdlocAndStlocInstructions() {
			var locals = new LocalList();
			for (int i = 0; i < 6; i++)
				locals.Add(new Local(CorLibTypes.Int32));

			Assert.Same(locals[0], OpCodes.Ldloc_0.ToInstruction().GetLocal(locals));
			Assert.Same(locals[3], OpCodes.Stloc_3.ToInstruction().GetLocal(locals));
			Assert.Same(locals[5], OpCodes.Ldloc_S.ToInstruction(locals[5]).GetLocal(locals));
			Assert.Same(locals[4], OpCodes.Ldloca.ToInstruction(locals[4]).GetLocal(locals));
			Assert.Null(OpCodes.Ldarg_0.ToInstruction().GetLocal(locals));
			// The method has fewer locals than the instruction needs
			Assert.Null(OpCodes.Ldloc_3.ToInstruction().GetLocal(new LocalList()));

			Assert.True(OpCodes.Ldloc_1.ToInstruction().IsLdloc());
			Assert.True(OpCodes.Ldloc.ToInstruction(locals[0]).IsLdloc());
			Assert.False(OpCodes.Ldloca.ToInstruction(locals[0]).IsLdloc());
			Assert.True(OpCodes.Stloc_S.ToInstruction(locals[0]).IsStloc());
			Assert.False(OpCodes.Ldloc_0.ToInstruction().IsStloc());
		}

		[Fact]
		public void ParameterOfLdargAndStargInstructions() {
			// An instance method: parameter 0 is 'this'
			var method = new MethodDefUser("Method", MethodSig.CreateInstance(CorLibTypes.Void, CorLibTypes.Int32, CorLibTypes.String));
			var type = new TypeDefUser("Ns", "Type");
			type.Methods.Add(method);
			var parameters = method.Parameters;
			Assert.Equal(3, parameters.Count);

			Assert.Same(parameters[0], OpCodes.Ldarg_0.ToInstruction().GetParameter(parameters));
			Assert.Same(parameters[2], OpCodes.Ldarg_2.ToInstruction().GetParameter(parameters));
			Assert.Same(parameters[1], OpCodes.Starg.ToInstruction(parameters[1]).GetParameter(parameters));
			Assert.Null(OpCodes.Ldarg_3.ToInstruction().GetParameter(parameters));
			Assert.Null(OpCodes.Ldloc_0.ToInstruction().GetParameter(parameters));

			Assert.Equal(2, OpCodes.Ldarg_2.ToInstruction().GetParameterIndex());
			Assert.Equal(1, OpCodes.Ldarga_S.ToInstruction(parameters[1]).GetParameterIndex());
			Assert.Equal(-1, OpCodes.Nop.ToInstruction().GetParameterIndex());

			Assert.True(OpCodes.Ldarg_1.ToInstruction().IsLdarg());
			Assert.True(OpCodes.Ldarg_S.ToInstruction(parameters[1]).IsLdarg());
			Assert.True(OpCodes.Starg_S.ToInstruction(parameters[1]).IsStarg());
			Assert.False(OpCodes.Ldarga.ToInstruction(parameters[1]).IsLdarg());

			// The type of the argument: 'this' is the declaring type, the others are in the signature
			Assert.Equal("System.String", OpCodes.Ldarg_2.ToInstruction().GetArgumentType(method.MethodSig, type)!.FullName);
			Assert.Equal("Ns.Type", OpCodes.Ldarg_0.ToInstruction().GetArgumentType(method.MethodSig, type)!.FullName);
			Assert.Null(OpCodes.Ldarg_3.ToInstruction().GetArgumentType(method.MethodSig, type));
		}

		[Fact]
		public void ExtensionMethodsAcceptNull() {
			Instruction? none = null;

			Assert.Same(OpCodes.Nop, none.GetOpCode());
			Assert.Null(none.GetOperand());
			Assert.Equal(0U, none.GetOffset());
			Assert.Null(none.GetSequencePoint());
		}

		[Fact]
		public void TextOfAnInstruction() {
			var target = OpCodes.Nop.ToInstruction();
			target.Offset = 0x1A;
			var method = CreateMethodRef(MethodSig.CreateStatic(CorLibTypes.Void, CorLibTypes.String));

			Assert.Equal("IL_001A: nop", target.ToString());
			Assert.Equal("IL_0000: br IL_001A", OpCodes.Br.ToInstruction(target).ToString());
			Assert.Equal("IL_0000: ldc.i4 123", OpCodes.Ldc_I4.ToInstruction(123).ToString());
			Assert.Equal("IL_0000: ldstr \"a\\tb\"", OpCodes.Ldstr.ToInstruction("a\tb").ToString());
			Assert.Equal("IL_0000: call System.Void System.Console::Method(System.String)", OpCodes.Call.ToInstruction(method).ToString());
			Assert.Equal("IL_0000: switch(IL_001A,null)", new Instruction(OpCodes.Switch, new Instruction?[] { target, null }).ToString());
		}

		[Fact]
		public void CloneCopiesEverythingButSharesTheOperand() {
			var original = OpCodes.Ldstr.ToInstruction("text");
			original.Offset = 12;

			var clone = original.Clone();

			Assert.NotSame(original, clone);
			Assert.Same(original.OpCode, clone.OpCode);
			Assert.Same(original.Operand, clone.Operand);
			Assert.Equal(12U, clone.Offset);
		}
	}
}
