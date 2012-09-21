using System.Collections.Generic;
using System.Text;
using dot10.DotNet.MD;

namespace dot10.DotNet.Emit {
	static class InstructionPrinter {
		public static string ToString(Instruction instr) {
			if (instr == null)
				return string.Empty;

			var sb = new StringBuilder();

			sb.Append(string.Format("IL_{0:X4}: ", instr.Offset));
			sb.Append(instr.OpCode.Name);

			switch (instr.OpCode.OperandType) {
			case OperandType.InlineBrTarget:
			case OperandType.ShortInlineBrTarget:
				sb.Append(' ');
				AddInstructionTarget(sb, instr.Operand as Instruction);
				break;

			case OperandType.InlineField:
			case OperandType.InlineMethod:
			case OperandType.InlineTok:
			case OperandType.InlineType:
				sb.Append(' ');
				if (instr.Operand is IFullName)
					sb.Append((instr.Operand as IFullName).FullName);
				else if (instr.Operand != null)
					sb.Append("<<<UNKNOWN>>>");
				else
					sb.Append("null");
				break;

			case OperandType.InlineI:
			case OperandType.InlineI8:
			case OperandType.InlineR:
			case OperandType.ShortInlineI:
			case OperandType.ShortInlineR:
				sb.Append(string.Format(" {0}", instr.Operand));
				break;

			case OperandType.InlineSig:
				sb.Append(' ');
				sb.Append(FullNameCreator.MethodFullName(null, (UTF8String)null, instr.Operand as MethodSig));
				break;

			case OperandType.InlineString:
				sb.Append(' ');
				EscapeString(sb, instr.Operand as string, true);
				break;

			case OperandType.InlineSwitch:
				var targets = instr.Operand as IList<Instruction>;
				if (targets == null)
					sb.Append("null");
				else {
					sb.Append('(');
					for (int i = 0; i < targets.Count; i++) {
						if (i != 0)
							sb.Append(',');
						AddInstructionTarget(sb, targets[i]);
					}
					sb.Append(')');
				}
				break;

			case OperandType.InlineVar:
			case OperandType.ShortInlineVar:
				sb.Append(' ');
				if (instr.Operand == null)
					sb.Append("null");
				else
					sb.Append(instr.Operand.ToString());
				break;

			case OperandType.InlineNone:
			case OperandType.InlinePhi:
			default:
				break;
			}

			return sb.ToString();
		}

		static void AddInstructionTarget(StringBuilder sb, Instruction targetInstr) {
			if (targetInstr == null)
				sb.Append("null");
			else
				sb.Append(string.Format("IL_{0:X4}", targetInstr.Offset));
		}

		static void EscapeString(StringBuilder sb, string s, bool addQuotes) {
			if (s == null) {
				sb.Append("null");
				return;
			}

			if (addQuotes)
				sb.Append('"');

			foreach (var c in s) {
				if ((int)c < 0x20) {
					switch (c) {
					case '\a': sb.Append(@"\a"); break;
					case '\b': sb.Append(@"\b"); break;
					case '\f': sb.Append(@"\f"); break;
					case '\n': sb.Append(@"\n"); break;
					case '\r': sb.Append(@"\r"); break;
					case '\t': sb.Append(@"\t"); break;
					case '\v': sb.Append(@"\v"); break;
					default:
						sb.Append(string.Format(@"\u{0:X4}", (int)c));
						break;
					}
				}
				else if (c == '\\' || c == '"') {
					sb.Append('\\');
					sb.Append(c);
				}
				else
					sb.Append(c);
			}

			if (addQuotes)
				sb.Append('"');
		}
	}
}
