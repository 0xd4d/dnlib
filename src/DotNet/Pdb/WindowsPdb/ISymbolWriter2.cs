// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.WindowsPdb {
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

	/// <summary>
	/// Implements <see cref="ISymbolWriter"/> and adds a few extra methods we need that are part of
	/// <c>ISymUnmanagedWriter</c> and <c>ISymUnmanagedWriter2</c> but not present in
	/// <see cref="ISymbolWriter"/>.
	/// </summary>
	public interface ISymbolWriter3 : ISymbolWriter2 {
		/// <summary>
		/// Defines a constant
		/// </summary>
		/// <param name="name">Name of constant</param>
		/// <param name="value">Constant value</param>
		/// <param name="sigToken">StandAloneSig token of constant field type</param>
		void DefineConstant2(string name, object value, uint sigToken);

		/// <summary>
		/// true if it supports <see cref="DefineKickoffMethod(uint)"/>, <see cref="DefineCatchHandlerILOffset(uint)"/>
		/// and <see cref="DefineAsyncStepInfo(uint[], uint[], uint[])"/>
		/// </summary>
		bool SupportsAsyncMethods { get; }

		/// <summary>
		/// Defines an async kickoff method
		/// </summary>
		/// <param name="kickoffMethod">Kickoff method token</param>
		void DefineKickoffMethod(uint kickoffMethod);

		/// <summary>
		/// Defines an async catch handler
		/// </summary>
		/// <param name="catchHandlerOffset">Catch handler IL offset</param>
		void DefineCatchHandlerILOffset(uint catchHandlerOffset);

		/// <summary>
		/// Defines async step info
		/// </summary>
		/// <param name="yieldOffsets">Yield IL offsets</param>
		/// <param name="breakpointOffset">Breakpoint method IL offset</param>
		/// <param name="breakpointMethod">Breakpoint method</param>
		void DefineAsyncStepInfo(uint[] yieldOffsets, uint[] breakpointOffset, uint[] breakpointMethod);
	}
}
