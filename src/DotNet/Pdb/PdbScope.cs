// dnlib: See LICENSE.txt for more info

using System.Diagnostics;
using dnlib.DotNet.Emit;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// A PDB scope
	/// </summary>
	[DebuggerDisplay("{Start} - {End}")]
	public sealed class PdbScope : IHasCustomDebugInformation {
		readonly ThreadSafe.IList<PdbScope> scopes = ThreadSafeListCreator.Create<PdbScope>();
		readonly ThreadSafe.IList<PdbLocal> locals = ThreadSafeListCreator.Create<PdbLocal>();
		readonly ThreadSafe.IList<string> namespaces = ThreadSafeListCreator.Create<string>();
		readonly ThreadSafe.IList<PdbConstant> constants = ThreadSafeListCreator.Create<PdbConstant>();

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbScope() {
		}

		/// <summary>
		/// Gets/sets the first instruction
		/// </summary>
		public Instruction Start { get; set; }

		/// <summary>
		/// Gets/sets the last instruction. It's <c>null</c> if it ends at the end of the method.
		/// </summary>
		public Instruction End { get; set; }

		/// <summary>
		/// Gets all child scopes
		/// </summary>
		public ThreadSafe.IList<PdbScope> Scopes => scopes;

		/// <summary>
		/// <c>true</c> if <see cref="Scopes"/> is not empty
		/// </summary>
		public bool HasScopes => scopes.Count > 0;

		/// <summary>
		/// Gets all locals in this scope
		/// </summary>
		public ThreadSafe.IList<PdbLocal> Variables => locals;

		/// <summary>
		/// <c>true</c> if <see cref="Variables"/> is not empty
		/// </summary>
		public bool HasVariables => locals.Count > 0;

		/// <summary>
		/// Gets all namespaces (Windows PDBs). Portable PDBs use <see cref="ImportScope"/>
		/// </summary>
		public ThreadSafe.IList<string> Namespaces => namespaces;

		/// <summary>
		/// <c>true</c> if <see cref="Namespaces"/> is not empty
		/// </summary>
		public bool HasNamespaces => namespaces.Count > 0;

		/// <summary>
		/// Gets/sets the import scope (Portable PDBs). Windows PDBs use <see cref="Namespaces"/>
		/// </summary>
		public PdbImportScope ImportScope { get; set; }

		/// <summary>
		/// Gets all constants
		/// </summary>
		public ThreadSafe.IList<PdbConstant> Constants => constants;

		/// <summary>
		/// <c>true</c> if <see cref="Constants"/> is not empty
		/// </summary>
		public bool HasConstants => constants.Count > 0;

		/// <inheritdoc/>
		public int HasCustomDebugInformationTag => 23;

		/// <inheritdoc/>
		public bool HasCustomDebugInfos => CustomDebugInfos.Count > 0;

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public ThreadSafe.IList<PdbCustomDebugInfo> CustomDebugInfos => customDebugInfos;
		readonly ThreadSafe.IList<PdbCustomDebugInfo> customDebugInfos = ThreadSafeListCreator.Create<PdbCustomDebugInfo>();
	}
}
