// dnlib: See LICENSE.txt for more info

#nullable enable

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
		public PdbDocument? Document { get; set; }

		/// <summary>
		/// Same as <see cref="Document"/> but throws an <see cref="InvalidMetadataException"/> if it's <c>null</c>
		/// </summary>
		public PdbDocument DocumentOrThrow => OrThrowHelper.NotNull(Document, nameof(Document));

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
		/// Clones this instance
		/// </summary>
		/// <returns>A new cloned instance</returns>
		public SequencePoint Clone() {
			return new SequencePoint() {
				Document = Document,
				StartLine = StartLine,
				StartColumn = StartColumn,
				EndLine = EndLine,
				EndColumn = EndColumn,
			};
		}
	}
}
