// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Pdb.Symbols {
	/// <summary>
	/// Sequence point
	/// </summary>
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
	}
}
