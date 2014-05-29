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

using System.Diagnostics.SymbolStore;
using dnlib.DotNet.MD;
using dnlib.IO;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// Creates a <see cref="ISymbolReader"/> instance
	/// </summary>
	public static class SymbolReaderCreator {
		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="assemblyFileName">Path to assembly</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk or if it's not possible to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(string assemblyFileName) {
			return Dss.SymbolReaderCreator.Create(assemblyFileName);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="metaData">.NET metadata</param>
		/// <param name="pdbFileName">Path to PDB file</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk or if it's not possible to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(IMetaData metaData, string pdbFileName) {
			return Dss.SymbolReaderCreator.Create(metaData, pdbFileName);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="metaData">.NET metadata</param>
		/// <param name="pdbData">PDB file data</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if it's not possible
		/// to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(IMetaData metaData, byte[] pdbData) {
			return Dss.SymbolReaderCreator.Create(metaData, pdbData);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="metaData">.NET metadata</param>
		/// <param name="pdbStream">PDB file stream which is now owned by us</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if it's not possible
		/// to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(IMetaData metaData, IImageStream pdbStream) {
			return Dss.SymbolReaderCreator.Create(metaData, pdbStream);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="mdStream">.NET metadata stream which is now owned by us</param>
		/// <param name="pdbFileName">Path to PDB file</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk or if it's not possible to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(IImageStream mdStream, string pdbFileName) {
			return Dss.SymbolReaderCreator.Create(mdStream, pdbFileName);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="mdStream">.NET metadata stream which is now owned by us</param>
		/// <param name="pdbData">PDB file data</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if it's not possible
		/// to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(IImageStream mdStream, byte[] pdbData) {
			return Dss.SymbolReaderCreator.Create(mdStream, pdbData);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="mdStream">.NET metadata stream which is now owned by us</param>
		/// <param name="pdbStream">PDB file stream which is now owned by us</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if it's not possible
		/// to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(IImageStream mdStream, IImageStream pdbStream) {
			return Dss.SymbolReaderCreator.Create(mdStream, pdbStream);
		}
	}
}
