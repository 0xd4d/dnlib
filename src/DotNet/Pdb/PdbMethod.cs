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
		readonly ThreadSafe.IList<PdbCustomDebugInfo> customDebugInfos;

		/// <summary>
		/// Gets/sets the root scope. It contains all scopes of the method, using namespaces, variables and constants
		/// </summary>
		public PdbScope Scope { get; set; }

		/// <summary>
		/// Gets/sets the async method info or null if there's none
		/// </summary>
		public PdbAsyncMethod AsyncMethod { get; set; }

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public ThreadSafe.IList<PdbCustomDebugInfo> CustomDebugInfos {
			get { return customDebugInfos; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbMethod() {
			customDebugInfos = ThreadSafeListCreator.Create<PdbCustomDebugInfo>();
		}
	}
}
