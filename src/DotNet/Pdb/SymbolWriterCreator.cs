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

using System.IO;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// Creates a <see cref="ISymbolWriter2"/>
	/// </summary>
	public static class SymbolWriterCreator {
		/// <summary>
		/// Creates a new <see cref="ISymbolWriter2"/> instance
		/// </summary>
		/// <param name="pdbFileName">PDB file name</param>
		/// <returns>A new <see cref="ISymbolWriter2"/> instance</returns>
		public static ISymbolWriter2 Create(string pdbFileName) {
			return Dss.SymbolWriterCreator.Create(pdbFileName);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolWriter2"/> instance
		/// </summary>
		/// <param name="pdbStream">PDB output stream</param>
		/// <param name="pdbFileName">PDB file name</param>
		/// <returns>A new <see cref="ISymbolWriter2"/> instance</returns>
		public static ISymbolWriter2 Create(Stream pdbStream, string pdbFileName) {
			return Dss.SymbolWriterCreator.Create(pdbStream, pdbFileName);
		}
	}
}
