// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Reflection;
using dnlib.DotNet.Emit;
using Xunit;
using ReflectionOpCode = System.Reflection.Emit.OpCode;
using ReflectionOpCodes = System.Reflection.Emit.OpCodes;

namespace dnlib.Tests.Emit {
	/// <summary>
	/// dnlib's table of CIL opcodes (<see cref="OpCodes"/>) checked against the runtime's own table,
	/// <see cref="System.Reflection.Emit.OpCodes"/>
	/// </summary>
	public class OpCodeTableTests {
		static IEnumerable<ReflectionOpCode> GetReflectionOpCodes() {
			foreach (var field in typeof(ReflectionOpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)) {
				if (field.GetValue(null) is ReflectionOpCode opCode)
					yield return opCode;
			}
		}

		// The opcode with the same encoding: one byte, or 0xFE and a second byte
		static OpCode? FindDnlibOpCode(ReflectionOpCode reflectionOpCode) {
			ushort value = (ushort)reflectionOpCode.Value;
			if (reflectionOpCode.Size == 1)
				return OpCodes.OneByteOpCodes[value];
			Assert.Equal(0xFE, value >> 8);
			return OpCodes.TwoByteOpCodes[value & 0xFF];
		}

		[Fact]
		public void EveryOpCodeOfTheRuntimeExistsWithTheSameProperties() {
			int count = 0;
			foreach (var expected in GetReflectionOpCodes()) {
				var actual = FindDnlibOpCode(expected);

				Assert.True(actual is not null, $"dnlib has no opcode {expected.Name}");
				Assert.Equal(expected.Name, actual.Name);
				Assert.Equal(expected.Value, actual.Value);
				Assert.Equal(expected.Size, actual.Size);
				// The enums have the same member names in dnlib and in System.Reflection.Emit
				Assert.Equal(expected.OperandType.ToString(), actual.OperandType.ToString());
				Assert.Equal(expected.FlowControl.ToString(), actual.FlowControl.ToString());
				Assert.Equal(expected.OpCodeType.ToString(), actual.OpCodeType.ToString());
				Assert.Equal(expected.StackBehaviourPush.ToString(), actual.StackBehaviourPush.ToString());
				Assert.Equal(GetExpectedPop(expected), actual.StackBehaviourPop.ToString());
				count++;
			}
			// All opcodes of ECMA-335. Newer runtimes can add some.
			Assert.True(count >= 219, $"Only {count} opcodes were compared");
		}

		// dnlib has one stack behaviour that System.Reflection.Emit doesn't have: PopAll, for the instructions
		// that empty the evaluation stack. System.Reflection.Emit says they pop nothing.
		static string GetExpectedPop(ReflectionOpCode opCode) {
			if (opCode == ReflectionOpCodes.Leave || opCode == ReflectionOpCodes.Leave_S || opCode == ReflectionOpCodes.Endfinally)
				return nameof(StackBehaviour.PopAll);
			return opCode.StackBehaviourPop.ToString();
		}

		[Fact]
		public void OpCodeIsAtTheIndexOfItsValueInTheTables() {
			for (int i = 0; i < 0x100; i++) {
				Assert.Equal(i, (ushort)OpCodes.OneByteOpCodes[i].Value);
				Assert.Equal(1, OpCodes.OneByteOpCodes[i].Size);
				Assert.Equal(0xFE00 | i, (ushort)OpCodes.TwoByteOpCodes[i].Value);
				Assert.Equal(2, OpCodes.TwoByteOpCodes[i].Size);
			}
		}

		[Fact]
		public void CodeEnumHasTheValueOfTheOpCode() {
			Assert.Equal(0x00, (int)Code.Nop);
			Assert.Equal(0x2A, (int)Code.Ret);
			Assert.Equal(0xFE01, (int)Code.Ceq);
			Assert.Same(OpCodes.Ret, Code.Ret.ToOpCode());
			Assert.Same(OpCodes.Ceq, Code.Ceq.ToOpCode());
		}
	}
}
