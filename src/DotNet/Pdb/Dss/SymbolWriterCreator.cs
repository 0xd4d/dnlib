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

using System;
using System.IO;

namespace dnlib.DotNet.Pdb.Dss {
	/// <summary>
	/// Creates a <see cref="ISymbolWriter2"/>
	/// </summary>
	public static class SymbolWriterCreator {
		static readonly Guid CLSID_CorSymWriter_SxS = new Guid(0x0AE2DEB0, 0xF901, 0x478B, 0xBB, 0x9F, 0x88, 0x1E, 0xE8, 0x06, 0x67, 0x88);

		/// <summary>
		/// Creates a <see cref="ISymUnmanagedWriter2"/> instance
		/// </summary>
		/// <returns>A new <see cref="ISymUnmanagedWriter2"/> instance</returns>
		public static ISymUnmanagedWriter2 CreateSymUnmanagedWriter2() {
			return (ISymUnmanagedWriter2)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_CorSymWriter_SxS));
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolWriter2"/> instance
		/// </summary>
		/// <param name="pdbFileName">PDB file name</param>
		/// <returns>A new <see cref="ISymbolWriter2"/> instance</returns>
		public static ISymbolWriter2 Create(string pdbFileName) {
			if (File.Exists(pdbFileName))
				File.Delete(pdbFileName);
			return new SymbolWriter(CreateSymUnmanagedWriter2(), pdbFileName);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolWriter2"/> instance
		/// </summary>
		/// <param name="pdbStream">PDB output stream</param>
		/// <param name="pdbFileName">PDB file name</param>
		/// <returns>A new <see cref="ISymbolWriter2"/> instance</returns>
		public static ISymbolWriter2 Create(Stream pdbStream, string pdbFileName) {
			return new SymbolWriter(CreateSymUnmanagedWriter2(), pdbFileName, pdbStream);
		}
	}
}
