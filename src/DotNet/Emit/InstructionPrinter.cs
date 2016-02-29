// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
using System.Text;
using dnlib.Threading;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Converts instructions to strings
	/// </summary>
	public static class InstructionPrinter {
		/// <summary>
		/// Converts an instruction to a string
		/// </summary>
		/// <param name="instr">The instruction</param>
		/// <returns>The result</returns>
		public static string ToString(Instruction instr) {
			if (instr == null)
				return string.Empty;

			var sb = new StringBuilder();

			sb.Append(string.Format("IL_{0:X4}: ", instr.Offset));
			sb.Append(instr.OpCode.Name);
			AddOperandString(sb, instr, " ");

			return sb.ToString();
		}

		/// <summary>
		/// Gets the instruction's operand as a string
		/// </summary>
		/// <param name="instr">The instruction</param>
		/// <returns>The operand as a string</returns>
		public static string GetOperandString(Instruction instr) {
			var sb = new StringBuilder();
			AddOperandString(sb, instr, string.Empty);
			return sb.ToString();
		}

		/// <summary>
		/// Add an instruction's operand to <paramref name="sb"/>
		/// </summary>
		/// <param name="sb">Place result here</param>
		/// <param name="instr">The instruction</param>
		public static void AddOperandString(StringBuilder sb, Instruction instr) {
			AddOperandString(sb, instr, string.Empty);
		}

		/// <summary>
		/// Add an instruction's operand to <paramref name="sb"/>
		/// </summary>
		/// <param name="sb">Place result here</param>
		/// <param name="instr">The instruction</param>
		/// <param name="extra">A string that will be added before the operand, if there's
		/// an operand.</param>
		public static void AddOperandString(StringBuilder sb, Instruction instr, string extra) {
			var op = instr.Operand;
			switch (instr.OpCode.OperandType) {
			case OperandType.InlineBrTarget:
			case OperandType.ShortInlineBrTarget:
				sb.Append(extra);
				AddInstructionTarget(sb, op as Instruction);
				break;

			case OperandType.InlineField:
			case OperandType.InlineMethod:
			case OperandType.InlineTok:
			case OperandType.InlineType:
				sb.Append(extra);
				if (op is IFullName)
					sb.Append((op as IFullName).FullName);
				else if (op != null)
					sb.Append(op.ToString());
				else
					sb.Append("null");
				break;

			case OperandType.InlineI:
			case OperandType.InlineI8:
			case OperandType.InlineR:
			case OperandType.ShortInlineI:
			case OperandType.ShortInlineR:
				sb.Append(string.Format("{0}{1}", extra, op));
				break;

			case OperandType.InlineSig:
				sb.Append(extra);
				sb.Append(FullNameCreator.MethodFullName(null, (UTF8String)null, op as MethodSig, null, null, null, null));
				break;

			case OperandType.InlineString:
				sb.Append(extra);
				EscapeString(sb, op as string, true);
				break;

			case OperandType.InlineSwitch:
				var targets = op as IList<Instruction>;
				if (targets == null)
					sb.Append("null");
				else {
					sb.Append('(');
					for (int i = 0; i < targets.Count; i++) {
						if (i != 0)
							sb.Append(',');
						AddInstructionTarget(sb, targets.Get(i, null));
					}
					sb.Append(')');
				}
				break;

			case OperandType.InlineVar:
			case OperandType.ShortInlineVar:
				sb.Append(extra);
				if (op == null)
					sb.Append("null");
				else
					sb.Append(op.ToString());
				break;

			case OperandType.InlineNone:
			case OperandType.InlinePhi:
			default:
				break;
			}
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
