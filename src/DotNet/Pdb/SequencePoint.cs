/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Diagnostics;
using dnlib.DotNet.Emit;

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
