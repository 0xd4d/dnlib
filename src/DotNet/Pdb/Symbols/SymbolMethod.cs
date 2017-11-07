// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace dnlib.DotNet.Pdb.Symbols {
	/// <summary>
	/// A method
	/// </summary>
	public abstract class SymbolMethod {
		/// <summary>
		/// Gets the method token
		/// </summary>
		public abstract int Token { get; }

		/// <summary>
		/// Gets the root scope
		/// </summary>
		public abstract SymbolScope RootScope { get; }

		/// <summary>
		/// Gets all sequence points
		/// </summary>
		public abstract IList<SymbolSequencePoint> SequencePoints { get; }

		/// <summary>
		/// Reads custom debug info
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="body">Method body</param>
		/// <param name="result">Updated with custom debug info</param>
		public abstract void GetCustomDebugInfos(MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result);
	}
}
