// dnlib: See LICENSE.txt for more info

using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// A PDB method
	/// </summary>
	public sealed class PdbMethod {
		/// <summary>
		/// Gets/sets the root scope. It contains all scopes of the method, using namespaces, variables and constants
		/// </summary>
		public PdbScope Scope { get; set; }

		/// <summary>
		/// Gets/sets the async method info or null if there's none
		/// </summary>
		public PdbAsyncMethod AsyncMethod { get; set; }

		/// <summary>
		/// Gets/sets the iterator method info or null if there's none
		/// </summary>
		public PdbIteratorMethod IteratorMethod { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbMethod() {
		}
	}
}
