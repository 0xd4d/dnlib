// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet.Pdb.Symbols {
	/// <summary>
	/// Reads symbols from a PDB file
	/// </summary>
	public abstract class SymbolReader : IDisposable {
		/// <summary>
		/// Called by the owner module before any other methods and properties are called
		/// </summary>
		/// <param name="module">Owner module</param>
		public abstract void Initialize(ModuleDef module);

		/// <summary>
		/// Gets the PDB file kind
		/// </summary>
		public abstract PdbFileKind PdbFileKind { get; }

		/// <summary>
		/// Gets the user entry point token or 0 if none
		/// </summary>
		public abstract int UserEntryPoint { get; }

		/// <summary>
		/// Gets all documents
		/// </summary>
		public abstract IList<SymbolDocument> Documents { get; }

		/// <summary>
		/// Gets a method or returns null if the method doesn't exist in the PDB file
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="version">Edit and continue version</param>
		/// <returns></returns>
		public abstract SymbolMethod GetMethod(MethodDef method, int version);

		/// <summary>
		/// Reads custom debug info
		/// </summary>
		/// <param name="token">Token of a <see cref="IHasCustomDebugInformation"/> instance</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <param name="result">Updated with custom debug info</param>
		public abstract void GetCustomDebugInfos(int token, GenericParamContext gpContext, IList<PdbCustomDebugInfo> result);

		/// <summary>
		/// Cleans up resources
		/// </summary>
		public virtual void Dispose() {
		}
	}
}
