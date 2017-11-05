// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// Iterator method info
	/// </summary>
	public sealed class PdbIteratorMethod {
		/// <summary>
		/// Gets/sets the kickoff method
		/// </summary>
		public MethodDef KickoffMethod { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbIteratorMethod() {
		}
	}
}
