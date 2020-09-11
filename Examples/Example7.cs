using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnlib.Examples {
	public class Example7 {
		public static void Run() => new Example7().DoIt();

		void DoIt() {
			var test1 = new OpCode(
				"test1", 0xf0, 0x00, OperandType.InlineNone, FlowControl.Next, StackBehaviour.Push0, StackBehaviour.Pop0);
			var test2 = new OpCode(
				"test2", 0xf0, 0x01, OperandType.InlineBrTarget, FlowControl.Branch, StackBehaviour.Push0, StackBehaviour.Pop0);
			var test3 = new OpCode(
				"test3", 0xf0, 0x02, OperandType.InlineString, FlowControl.Next, StackBehaviour.Push1, StackBehaviour.Pop0);

			var ctx = new ModuleContext();

			ctx.RegisterExperimentalOpCode(test1);
			ctx.RegisterExperimentalOpCode(test2);
			ctx.RegisterExperimentalOpCode(test3);

			var mod = ModuleDefMD.Load(typeof(Example7).Module, ctx);
			var body = mod.Types.Single(x => x.Name == nameof(Example7)).Methods.Single(x => x.Name == nameof(CustomCil)).Body;

			Console.WriteLine("Original:");
			foreach (var insn in body.Instructions)
				Console.WriteLine("{0} (0x{1:X4})", insn, insn.OpCode.Value);
			Console.WriteLine();

			var code = body.Instructions;

			code.Clear();

			var label = OpCodes.Nop.ToInstruction();

			code.Add(test1.ToInstruction());
			code.Add(OpCodes.Nop.ToInstruction());
			code.Add(label);
			code.Add(OpCodes.Ret.ToInstruction());
			code.Add(test2.ToInstruction(label));
			code.Add(test3.ToInstruction("foo"));

			Console.WriteLine("Modified:");
			foreach (var insn in body.Instructions)
				Console.WriteLine("{0} (0x{1:X4})", insn, insn.OpCode.Value);
			Console.WriteLine();

			using (var stream = new MemoryStream()) {
				mod.Write(stream);

				stream.Position = 0;

				mod = ModuleDefMD.Load(stream, ctx);
				body = mod.Types.Single(x => x.Name == nameof(Example7)).Methods.Single(x => x.Name == nameof(CustomCil)).Body;

				Console.WriteLine("Roundtripped:");
				foreach (var insn in body.Instructions)
					Console.WriteLine("{0} (0x{1:X4})", insn, insn.OpCode.Value);
				Console.WriteLine();
			}
		}

		void CustomCil() {
		}
	}
}
