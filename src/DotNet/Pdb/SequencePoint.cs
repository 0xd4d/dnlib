// dnlib: See LICENSE.txt for more info

using System.Diagnostics;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// PDB sequence point
	/// </summary>
	[DebuggerDisplay("({StartLine}, {StartColumn}) - ({EndLine}, {EndColumn}) {Document.Url}")]
	public sealed class SequencePoint {
		/// <summary>
		/// PDB document
		/// </summary>
		public PdbDocument Document { get; set; }

		/// <summary>
		/// Start line
		/// </summary>
		public int StartLine { get; set; }

		/// <summary>
		/// Start column
		/// </summary>
		public int StartColumn { get; set; }

		/// <summary>
		/// End line
		/// </summary>
		public int EndLine { get; set; }

		/// <summary>
		/// End column
		/// </summary>
		public int EndColumn { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="document">PDB document</param>
		public SequencePoint(PdbDocument document) => Document = document;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="document">PDB document</param>
		/// <param name="startLine">Start line</param>
		/// <param name="startColumn">Start column</param>
		/// <param name="endLine">End line</param>
		/// <param name="endColumn">End column</param>
		public SequencePoint(PdbDocument document, int startLine, int startColumn, int endLine, int endColumn) {
			Document = document;
			StartLine = startLine;
			StartColumn = startColumn;
			EndLine = endLine;
			EndColumn = endColumn;
		}

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns>A new cloned instance</returns>
		public SequencePoint Clone() => new SequencePoint(Document, StartLine, StartColumn, EndLine, EndColumn);
	}
}
