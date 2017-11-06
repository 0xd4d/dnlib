// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;

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
		/// true if this is an iterator method
		/// </summary>
		public bool IsIteratorMethod {
			get { return IteratorKickoffMethod != 0; }
		}

		/// <summary>
		/// Gets the kick off method if it's an iterator method, otherwise 0
		/// </summary>
		public abstract int IteratorKickoffMethod { get; }

		/// <summary>
		/// true if this is an async method
		/// </summary>
		public bool IsAsyncMethod {
			get { return AsyncKickoffMethod != 0; }
		}

		/// <summary>
		/// Gets the kick off method if it's an async method, otherwise 0
		/// </summary>
		public abstract int AsyncKickoffMethod { get; }

		/// <summary>
		/// Gets the catch handler IL offset if it's an async method (<see cref="IsAsyncMethod"/>)
		/// </summary>
		public abstract uint? AsyncCatchHandlerILOffset { get; }

		/// <summary>
		/// Gets the async step infos if it's an async method (<see cref="IsAsyncMethod"/>)
		/// </summary>
		public abstract IList<SymbolAsyncStepInfo> AsyncStepInfos { get; }
	}
}
