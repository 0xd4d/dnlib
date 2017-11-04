// dnlib: See LICENSE.txt for more info

using System.Collections.ObjectModel;

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
		public abstract ReadOnlyCollection<SymbolSequencePoint> SequencePoints { get; }

		/// <summary>
		/// true if this is an async method
		/// </summary>
		public abstract bool IsAsyncMethod { get; }

		/// <summary>
		/// Gets the kick off method if it's an async method (<see cref="IsAsyncMethod"/>)
		/// </summary>
		public abstract int KickoffMethod { get; }

		/// <summary>
		/// Gets the catch handler IL offset if it's an async method (<see cref="IsAsyncMethod"/>)
		/// </summary>
		public abstract uint? CatchHandlerILOffset { get; }

		/// <summary>
		/// Gets the async step infos if it's an async method (<see cref="IsAsyncMethod"/>)
		/// </summary>
		public abstract ReadOnlyCollection<SymbolAsyncStepInfo> AsyncStepInfos { get; }
	}
}
