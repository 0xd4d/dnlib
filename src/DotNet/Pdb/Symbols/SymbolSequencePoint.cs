// dnlib: See LICENSE.txt for more info

using System.Diagnostics;
using System.Text;

namespace dnlib.DotNet.Pdb.Symbols {
	/// <summary>
	/// Sequence point
	/// </summary>
	[DebuggerDisplay("{GetDebuggerString(),nq}")]
	public struct SymbolSequencePoint {
		/// <summary>
		/// IL offset
		/// </summary>
		public int Offset;

		/// <summary>
		/// Document
		/// </summary>
		public SymbolDocument Document;

		/// <summary>
		/// Start line
		/// </summary>
		public int Line;

		/// <summary>
		/// Start column
		/// </summary>
		public int Column;

		/// <summary>
		/// End line
		/// </summary>
		public int EndLine;

		/// <summary>
		/// End column
		/// </summary>
		public int EndColumn;

		string GetDebuggerString() {
			var sb = new StringBuilder();
			if (Line == 0xFEEFEE && EndLine == 0xFEEFEE)
				sb.Append("<hidden>");
			else {
				sb.Append("(");
				sb.Append(Line);
				sb.Append(",");
				sb.Append(Column);
				sb.Append(")-(");
				sb.Append(EndLine);
				sb.Append(",");
				sb.Append(EndColumn);
				sb.Append(")");
			}
			sb.Append(": ");
			sb.Append(Document.URL);
			return sb.ToString();
		}
	}
}
