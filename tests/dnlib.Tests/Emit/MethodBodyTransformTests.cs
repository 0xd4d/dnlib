// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Emit {
	/// <summary>
	/// <see cref="CilBody.SimplifyMacros"/> replaces the short forms of instructions (<c>ldarg.0</c>,
	/// <c>ldc.i4.5</c>, <c>ldloc.s</c>) with the general form that has an operand, and
	/// <see cref="CilBody.SimplifyBranches"/> replaces short branches with long ones, so code that edits a method
	/// only has to handle one form. <see cref="CilBody.OptimizeMacros"/> and
	/// <see cref="CilBody.OptimizeBranches"/> do the opposite before the method is written.
	/// </summary>
	public class MethodBodyTransformTests {
		static ModuleDefMD LoadFeatures() => ModuleDefMD.Load(Fixtures.Fixtures.Features.Image);

		static IEnumerable<MethodDef> GetMethodsWithBody(ModuleDef module) =>
			module.GetTypes().SelectMany(a => a.Methods).Where(a => a.Body is not null);

		static byte[] Write(ModuleDef module) {
			var stream = new MemoryStream();
			module.Write(stream);
			return stream.ToArray();
		}

		[Fact]
		public void SimplifyMacrosReplacesShortFormsWithTheGeneralForm() {
			var module = new ModuleDefUser("Test.dll");
			var method = new MethodDefUser("M", MethodSig.CreateStatic(module.CorLibTypes.Void, module.CorLibTypes.Int32, module.CorLibTypes.Int32),
				MethodAttributes.Public | MethodAttributes.Static);
			var body = new CilBody();
			method.Body = body;
			var local0 = body.Variables.Add(new Local(module.CorLibTypes.Int32));
			var local1 = body.Variables.Add(new Local(module.CorLibTypes.Int32));
			body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
			body.Instructions.Add(OpCodes.Stloc_0.ToInstruction());
			body.Instructions.Add(OpCodes.Ldloc_1.ToInstruction());
			body.Instructions.Add(OpCodes.Ldc_I4_M1.ToInstruction());
			body.Instructions.Add(OpCodes.Ldc_I4_8.ToInstruction());
			body.Instructions.Add(new Instruction(OpCodes.Ldc_I4_S, (sbyte)-100));
			body.Instructions.Add(OpCodes.Ret.ToInstruction());

			body.SimplifyMacros(method.Parameters);

			var instructions = body.Instructions;
			Assert.Equal(Code.Ldarg, instructions[0].OpCode.Code);
			Assert.Same(method.Parameters[1], instructions[0].Operand);
			Assert.Equal(Code.Stloc, instructions[1].OpCode.Code);
			Assert.Same(local0, instructions[1].Operand);
			Assert.Equal(Code.Ldloc, instructions[2].OpCode.Code);
			Assert.Same(local1, instructions[2].Operand);
			Assert.Equal(Code.Ldc_I4, instructions[3].OpCode.Code);
			Assert.Equal(-1, instructions[3].Operand);
			Assert.Equal(8, instructions[4].Operand);
			Assert.Equal(-100, instructions[5].Operand);
			Assert.Equal(Code.Ldc_I4, instructions[5].OpCode.Code);
		}

		[Fact]
		public void OptimizeMacrosUsesTheShortestForm() {
			var module = new ModuleDefUser("Test.dll");
			var method = new MethodDefUser("M", MethodSig.CreateStatic(module.CorLibTypes.Void, module.CorLibTypes.Int32),
				MethodAttributes.Public | MethodAttributes.Static);
			var body = new CilBody();
			method.Body = body;
			var locals = Enumerable.Range(0, 300).Select(a => body.Variables.Add(new Local(module.CorLibTypes.Int32))).ToList();
			body.Instructions.Add(OpCodes.Ldarg.ToInstruction(method.Parameters[0]));
			body.Instructions.Add(OpCodes.Ldloc.ToInstruction(locals[3]));
			body.Instructions.Add(OpCodes.Ldloc.ToInstruction(locals[4]));
			body.Instructions.Add(OpCodes.Ldloc.ToInstruction(locals[255]));
			body.Instructions.Add(OpCodes.Ldloc.ToInstruction(locals[256]));
			body.Instructions.Add(Instruction.CreateLdcI4(8));
			body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, 9));
			body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, 127));
			body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, 128));
			body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, -128));
			body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, -129));

			body.OptimizeMacros();

			Assert.Equal(new[] {
				Code.Ldarg_0, Code.Ldloc_3, Code.Ldloc_S, Code.Ldloc_S, Code.Ldloc,
				Code.Ldc_I4_8, Code.Ldc_I4_S, Code.Ldc_I4_S, Code.Ldc_I4, Code.Ldc_I4_S, Code.Ldc_I4,
			}, body.Instructions.Select(a => a.OpCode.Code).ToArray());
		}

		// Every short branch instruction and its long form: the opcodes with the names "x.s" and "x"
		public static TheoryData<string, string> ShortAndLongBranches {
			get {
				var data = new TheoryData<string, string>();
				var all = typeof(OpCodes).GetFields().Where(a => a.FieldType == typeof(OpCode)).Select(a => (OpCode)a.GetValue(null)!).ToList();
				foreach (var shortBranch in all.Where(a => a.OperandType == OperandType.ShortInlineBrTarget)) {
					var longBranch = all.Single(a => a.Name + ".s" == shortBranch.Name);
					data.Add(shortBranch.Name, longBranch.Name);
				}
				return data;
			}
		}

		static OpCode GetOpCode(string name) =>
			typeof(OpCodes).GetFields().Where(a => a.FieldType == typeof(OpCode)).Select(a => (OpCode)a.GetValue(null)!).Single(a => a.Name == name);

		[Theory, MemberData(nameof(ShortAndLongBranches))]
		public void EveryShortBranchHasALongFormThatTheTransformsUse(string shortName, string longName) {
			var body = new CilBody();
			var target = OpCodes.Ret.ToInstruction();
			var branch = new Instruction(GetOpCode(shortName), target);
			body.Instructions.Add(branch);
			body.Instructions.Add(target);

			body.SimplifyBranches();
			Assert.Equal(longName, branch.OpCode.Name);
			Assert.Same(target, branch.Operand);

			body.OptimizeBranches();
			Assert.Equal(shortName, branch.OpCode.Name);
			Assert.Same(target, branch.Operand);

			// SimplifyMacros() also replaces the short branches
			body.Instructions.SimplifyMacros(new List<Local>(), new List<Parameter>());
			Assert.Equal(longName, branch.OpCode.Name);
		}

		// The operand of ldc.i4.s is an sbyte, of ldc.i4 an int: both get the shortest form
		[Theory, InlineData(1, "ldc.i4.1"), InlineData(-1, "ldc.i4.m1"), InlineData(100, "ldc.i4.s")]
		public void AnSByteOperandUsesTheShortestForm(int value, string expectedName) {
			var instructions = new List<Instruction> { new Instruction(OpCodes.Ldc_I4_S, (sbyte)value), OpCodes.Ret.ToInstruction() };

			instructions.OptimizeMacros();

			Assert.Equal(expectedName, instructions[0].OpCode.Name);
		}

		[Fact]
		public void ThereAre14ShortBranches() =>
			Assert.Equal(14, ShortAndLongBranches.Count);

		// Every instruction that loads or stores an argument or a local, with an argument or local at the indexes
		// where the shortest form changes: 0-3 have their own instructions, up to 255 the index is a byte
		[Theory]
		[InlineData(0, "ldarg.0", "ldarga.s", "starg.s", "ldloc.0", "ldloca.s", "stloc.0")]
		[InlineData(1, "ldarg.1", "ldarga.s", "starg.s", "ldloc.1", "ldloca.s", "stloc.1")]
		[InlineData(2, "ldarg.2", "ldarga.s", "starg.s", "ldloc.2", "ldloca.s", "stloc.2")]
		[InlineData(3, "ldarg.3", "ldarga.s", "starg.s", "ldloc.3", "ldloca.s", "stloc.3")]
		[InlineData(4, "ldarg.s", "ldarga.s", "starg.s", "ldloc.s", "ldloca.s", "stloc.s")]
		[InlineData(255, "ldarg.s", "ldarga.s", "starg.s", "ldloc.s", "ldloca.s", "stloc.s")]
		[InlineData(256, "ldarg", "ldarga", "starg", "ldloc", "ldloca", "stloc")]
		public void ArgumentAndLocalInstructionsUseTheShortestFormForTheIndex(int index, params string[] expectedShortForms) {
			var module = new ModuleDefUser("Test.dll");
			var parameterTypes = Enumerable.Repeat<TypeSig>(module.CorLibTypes.Int32, 300).ToArray();
			var method = new MethodDefUser("M", MethodSig.CreateStatic(module.CorLibTypes.Void, parameterTypes), MethodAttributes.Public | MethodAttributes.Static);
			var body = new CilBody();
			method.Body = body;
			var locals = Enumerable.Range(0, 300).Select(a => body.Variables.Add(new Local(module.CorLibTypes.Int32))).ToList();
			var parameter = method.Parameters[index];
			var local = locals[index];
			body.Instructions.Add(OpCodes.Ldarg.ToInstruction(parameter));
			body.Instructions.Add(OpCodes.Ldarga.ToInstruction(parameter));
			body.Instructions.Add(OpCodes.Starg.ToInstruction(parameter));
			body.Instructions.Add(OpCodes.Ldloc.ToInstruction(local));
			body.Instructions.Add(OpCodes.Ldloca.ToInstruction(local));
			body.Instructions.Add(OpCodes.Stloc.ToInstruction(local));

			body.OptimizeMacros();
			Assert.Equal(expectedShortForms, body.Instructions.Select(a => a.OpCode.Name));

			// And back: the general form with the same argument or local
			body.SimplifyMacros(method.Parameters);
			Assert.Equal(new[] { "ldarg", "ldarga", "starg", "ldloc", "ldloca", "stloc" }, body.Instructions.Select(a => a.OpCode.Name));
			Assert.All(body.Instructions.Take(3), a => Assert.Same(parameter, a.Operand));
			Assert.All(body.Instructions.Skip(3), a => Assert.Same(local, a.Operand));
		}

		// ldc.i4.m1 and ldc.i4.0 - ldc.i4.8 have no operand, ldc.i4.s has a signed byte
		[Theory]
		[InlineData(-129, "ldc.i4")]
		[InlineData(-128, "ldc.i4.s")]
		[InlineData(-2, "ldc.i4.s")]
		[InlineData(-1, "ldc.i4.m1")]
		[InlineData(0, "ldc.i4.0")]
		[InlineData(1, "ldc.i4.1")]
		[InlineData(2, "ldc.i4.2")]
		[InlineData(3, "ldc.i4.3")]
		[InlineData(4, "ldc.i4.4")]
		[InlineData(5, "ldc.i4.5")]
		[InlineData(6, "ldc.i4.6")]
		[InlineData(7, "ldc.i4.7")]
		[InlineData(8, "ldc.i4.8")]
		[InlineData(9, "ldc.i4.s")]
		[InlineData(127, "ldc.i4.s")]
		[InlineData(128, "ldc.i4")]
		[InlineData(int.MaxValue, "ldc.i4")]
		public void Int32ConstantUsesTheShortestForm(int value, string expectedShortForm) {
			var body = new CilBody();
			var instruction = new Instruction(OpCodes.Ldc_I4, value);
			body.Instructions.Add(instruction);

			body.OptimizeMacros();
			Assert.Equal(expectedShortForm, instruction.OpCode.Name);
			Assert.Equal(value, instruction.GetLdcI4Value());

			body.SimplifyMacros(new List<Parameter>());
			Assert.Equal("ldc.i4", instruction.OpCode.Name);
			Assert.Equal(value, instruction.Operand);
		}

		[Fact]
		public void BranchTransformsUseShortBranchesOnlyIfTheTargetIsNear() {
			var body = new CilBody();
			var target = OpCodes.Ret.ToInstruction();
			var nearBranch = OpCodes.Br.ToInstruction(target);
			var farBranch = OpCodes.Br.ToInstruction(target);
			body.Instructions.Add(farBranch);
			for (int i = 0; i < 200; i++)
				body.Instructions.Add(OpCodes.Nop.ToInstruction());
			body.Instructions.Add(nearBranch);
			body.Instructions.Add(target);

			body.OptimizeBranches();
			// The target of a short branch must be within -128..127 bytes
			Assert.Equal(Code.Br, farBranch.OpCode.Code);
			Assert.Equal(Code.Br_S, nearBranch.OpCode.Code);

			body.SimplifyBranches();
			Assert.Equal(Code.Br, nearBranch.OpCode.Code);
			Assert.Same(target, nearBranch.Operand);
		}

		[Fact]
		public void UpdateInstructionOffsetsReturnsTheCodeSize() {
			var body = new CilBody();
			body.Instructions.Add(OpCodes.Nop.ToInstruction());
			body.Instructions.Add(Instruction.CreateLdcI4(1000));
			body.Instructions.Add(OpCodes.Ceq.ToInstruction());
			body.Instructions.Add(OpCodes.Ret.ToInstruction());

			// 1 (nop) + 5 (ldc.i4) + 2 (ceq) + 1 (ret)
			Assert.Equal(9U, body.UpdateInstructionOffsets());
			Assert.Equal(new uint[] { 0, 1, 6, 8 }, body.Instructions.Select(a => a.Offset).ToArray());
		}

		// The compiler already uses the shortest forms, so simplifying and optimizing its code again must give
		// the same instructions
		[Fact]
		public void SimplifyThenOptimizeGivesTheCompilersCodeAgain() {
			using (var original = LoadFeatures())
			using (var transformed = LoadFeatures()) {
				foreach (var method in GetMethodsWithBody(transformed)) {
					method.Body!.SimplifyMacros(method.Parameters);
					method.Body.SimplifyBranches();
					method.Body.OptimizeMacros();
					method.Body.OptimizeBranches();
				}

				ModuleComparer.AssertEqual(original, transformed, new ModuleDumpOptions { MaxStack = true });
			}
		}

		[Fact]
		public void SimplifiedCodeHasNoShortForms() {
			using (var module = LoadFeatures()) {
				foreach (var method in GetMethodsWithBody(module)) {
					method.Body!.SimplifyMacros(method.Parameters);
					method.Body.SimplifyBranches();

					foreach (var instruction in method.Body.Instructions) {
						Assert.NotEqual(OperandType.ShortInlineBrTarget, instruction.OpCode.OperandType);
						Assert.NotEqual(OperandType.ShortInlineVar, instruction.OpCode.OperandType);
						// Instructions like ldarg.0 and ldc.i4.5 have no operand, the value is a part of the opcode
						var name = instruction.OpCode.Name;
						Assert.False(name.StartsWith("ldarg.") || name.StartsWith("ldloc.") || name.StartsWith("stloc.") || name.StartsWith("ldc.i4."), $"{name} wasn't simplified");
					}
				}
			}
		}

		// Code in its long form and code that was made short again must still do the same thing: the fixture's
		// Main() checks its own code
		[Theory, InlineData(false), InlineData(true)]
		public void TransformedCodeStillRuns(bool optimizeAgain) {
			using (var module = LoadFeatures()) {
				foreach (var method in GetMethodsWithBody(module)) {
					method.Body!.SimplifyMacros(method.Parameters);
					method.Body.SimplifyBranches();
					if (optimizeAgain) {
						method.Body.OptimizeMacros();
						method.Body.OptimizeBranches();
					}
				}

				Assert.Equal(0, AssemblyRunner.RunEntryPoint(Write(module)));
			}
		}

		// The compiler's max stack value is an upper limit: it's allowed to be bigger than needed
		[Fact]
		public void CalculatedMaxStackIsNeverBiggerThanTheCompilersValue() {
			using (var module = LoadFeatures()) {
				int methods = 0;
				foreach (var method in GetMethodsWithBody(module)) {
					var body = method.Body!;

					Assert.True(MaxStackCalculator.GetMaxStack(body.Instructions, body.ExceptionHandlers, out uint maxStack), $"{method.FullName}: the max stack can't be calculated");
					// A tiny header has no max stack field, the value is always 8
					if (body.HeaderSize != 1)
						Assert.True(maxStack <= body.MaxStack, $"{method.FullName}: calculated {maxStack}, the compiler wrote {body.MaxStack}");
					else
						Assert.True(maxStack <= 8, $"{method.FullName}: calculated {maxStack} for a method with a tiny header");
					methods++;
				}
				Assert.True(methods > 50, $"Only {methods} methods were checked");
			}
		}

		[Fact]
		public void MaxStackOfSimpleCode() {
			// Pushes 3 values, then pops them
			var instructions = new List<Instruction> {
				Instruction.CreateLdcI4(1),
				Instruction.CreateLdcI4(2),
				Instruction.CreateLdcI4(3),
				OpCodes.Add.ToInstruction(),
				OpCodes.Add.ToInstruction(),
				OpCodes.Pop.ToInstruction(),
				OpCodes.Ret.ToInstruction(),
			};

			Assert.Equal(3U, MaxStackCalculator.GetMaxStack(instructions, new List<ExceptionHandler>()));
		}

		[Fact]
		public void MaxStackCanNotBeCalculatedIfTheCodePopsFromAnEmptyStack() {
			var instructions = new List<Instruction> { OpCodes.Pop.ToInstruction(), OpCodes.Ret.ToInstruction() };

			Assert.False(MaxStackCalculator.GetMaxStack(instructions, new List<ExceptionHandler>(), out _));
		}
	}
}
