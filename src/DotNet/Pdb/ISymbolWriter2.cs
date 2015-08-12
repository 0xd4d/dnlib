// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;
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
