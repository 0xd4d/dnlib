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
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices.ComTypes;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// IMAGE_DEBUG_DIRECTORY
	/// </summary>
	public struct IMAGE_DEBUG_DIRECTORY {
#pragma warning disable 1591
		public uint Characteristics;
		public uint TimeDateStamp;
		public ushort MajorVersion;
		public ushort MinorVersion;
		public uint Type;
		public uint SizeOfData;
		public uint AddressOfRawData;
		public uint PointerToRawData;
#pragma warning restore 1591
	}

	/// <summary>
	/// Implements <see cref="ISymbolWriter"/> and adds a few extra methods we need that are part of
	/// <c>ISymUnmanagedWriter</c> and <c>ISymUnmanagedWriter2</c> but not present in
	/// <see cref="ISymbolWriter"/>.
	/// </summary>
	public interface ISymbolWriter2 : ISymbolWriter, IDisposable {
		/// <summary>
		/// Same as <see cref="ISymbolWriter.DefineSequencePoints"/> except that this method has an
		/// extra <paramref name="arraySize"/> that specifies the size of all the arrays.
		/// </summary>
		/// <param name="document">Document</param>
		/// <param name="arraySize">Size of the arrays</param>
		/// <param name="offsets">Offsets</param>
		/// <param name="lines">Start lines</param>
		/// <param name="columns">Start columns</param>
		/// <param name="endLines">End lines</param>
		/// <param name="endColumns">End columns</param>
		void DefineSequencePoints(ISymbolDocumentWriter document, uint arraySize, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns);

		/// <summary>
		/// Gets debug info. See <c>ISymUnmanagedWriter.GetDebugInfo()</c>
		/// </summary>
		/// <param name="pIDD">Updated by writer</param>
		/// <returns>Debug data for the symbol store</returns>
		byte[] GetDebugInfo(out IMAGE_DEBUG_DIRECTORY pIDD);

		/// <summary>
		/// Define a local. See <c>ISymUnmanagedWriter2.DefineLocalVariable2()</c>
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="attributes">Attributes</param>
		/// <param name="sigToken">Signature token</param>
		/// <param name="addrKind">Address kind</param>
		/// <param name="addr1">Address #1</param>
		/// <param name="addr2">Address #2</param>
		/// <param name="addr3">Address #3</param>
		/// <param name="startOffset">Start offset</param>
		/// <param name="endOffset">End offset</param>
		void DefineLocalVariable2(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3, uint startOffset, uint endOffset);

		/// <summary>
		/// Initializes this instance. This must be called before any other method.
		/// </summary>
		/// <param name="metaData">Metadata</param>
		void Initialize(MetaData metaData);
	}
}
