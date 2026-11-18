// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// A PDB method
	/// </summary>
	public sealed class PdbMethod {
		/// <summary>
		/// Gets/sets the root scope. It contains all scopes of the method, using namespaces, variables and constants
		/// </summary>
		public PdbScope? Scope { get; set; }

		/// <summary>
		/// Same as <see cref="Scope"/> but throws an <see cref="InvalidMetadataException"/> if it's <c>null</c>
		/// </summary>
		public PdbScope ScopeOrThrow => OrThrowHelper.NotNull(Scope, nameof(Scope));

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbMethod() {
		}
	}
}
