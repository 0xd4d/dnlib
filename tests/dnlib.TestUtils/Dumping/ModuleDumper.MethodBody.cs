// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnlib.TestUtils.Dumping {
	public sealed partial class ModuleDumper {
		// Branch targets, exception handler ranges and scopes are shown as instruction indexes: the writer can
		// change the offsets (eg. when it uses a shorter instruction). These are the indexes of the instructions
		// of the method that's being written.
		readonly Dictionary<Instruction, int> instructionIndexes = new Dictionary<Instruction, int>();

		void SetCurrentMethodBody(CilBody? body) {
			instructionIndexes.Clear();
			if (body is null)
				return;
			var instructions = body.Instructions;
			for (int i = 0; i < instructions.Count; i++)
				instructionIndexes[instructions[i]] = i;
		}

		void WriteMethodBody(CilBody body) {
			var instructions = body.Instructions;
			bool hasLocalloc = false;
			foreach (var instruction in instructions) {
				if (instruction.OpCode.Code == Code.Localloc)
					hasLocalloc = true;
			}

			var header = "body";
			// Not compared: InitLocals of a method without locals and without localloc. The flag does nothing
			// in such a method. A tiny method header can't store it, and the reader returns true for tiny headers.
			if (body.Variables.Count != 0 || hasLocalloc)
				header += " initlocals=" + body.InitLocals;
			// Not compared: a max stack value of 8 or less. A tiny method header has no max stack field, the
			// value is always 8, and the writer uses a tiny header whenever it can.
			if (options.MaxStack)
				header += body.MaxStack <= 8 ? " maxstack<=8" : " maxstack=" + body.MaxStack.ToString(CultureInfo.InvariantCulture);
			writer.Line(header);
			writer.Indent();

			foreach (var local in body.Variables) {
				var text = "local " + local.Index.ToString(CultureInfo.InvariantCulture) + " " + ReferenceFormatter.Format(local.Type);
				// The name and attributes come from the PDB
				if (options.Pdb)
					text += " " + DumpText.Quote(local.Name) + " " + local.Attributes;
				writer.Line(text);
			}

			foreach (var instruction in instructions) {
				var text = instruction.OpCode.Name + FormatOperand(instruction.Operand);
				if (options.Pdb)
					text += FormatSequencePoint(instruction.SequencePoint);
				writer.Line(text);
			}

			foreach (var handler in body.ExceptionHandlers) {
				writer.Line("exceptionhandler " + handler.HandlerType +
					" try=" + FormatTarget(handler.TryStart) + "-" + FormatTarget(handler.TryEnd) +
					" filter=" + FormatTarget(handler.FilterStart) +
					" handler=" + FormatTarget(handler.HandlerStart) + "-" + FormatTarget(handler.HandlerEnd) +
					" catch=" + ReferenceFormatter.Format(handler.CatchType));
			}
			WritePdbMethod(body.PdbMethod);
			writer.Unindent();
		}

		string FormatOperand(object? operand) {
			switch (operand) {
			case null:
				return string.Empty;
			case Instruction target:
				return " -> " + FormatTarget(target);
			case IList<Instruction> targets:
				var text = new StringBuilder(" ->");
				foreach (var target in targets)
					text.Append(' ').Append(FormatTarget(target));
				return text.ToString();
			case Local local:
				return " local " + local.Index.ToString(CultureInfo.InvariantCulture);
			case Parameter parameter:
				return " parameter " + parameter.Index.ToString(CultureInfo.InvariantCulture);
			case string or float or double or System.IConvertible:
				return " " + DumpText.Value(operand);
			default:
				return " " + ReferenceFormatter.Format(operand);
			}
		}

		// null: the end of the method (eg. a try block that ends at the last instruction), or an invalid target
		string FormatTarget(Instruction? target) {
			if (target is null)
				return "null";
			if (instructionIndexes.TryGetValue(target, out int index))
				return "#" + index.ToString(CultureInfo.InvariantCulture);
			return "<not in this method>";
		}
	}
}
