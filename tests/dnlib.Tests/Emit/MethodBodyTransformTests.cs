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
