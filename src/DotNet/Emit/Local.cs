using System.Diagnostics;

namespace dot10.DotNet.Emit {
	/// <summary>
	/// A method local
	/// </summary>
	[DebuggerDisplay("{typeSig}")]
	public sealed class Local {
		ITypeSig typeSig;

		/// <summary>
		/// Gets/sets the type of the local
		/// </summary>
		public ITypeSig Type {
			get { return typeSig; }
			set { typeSig = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeSig">The type</param>
		public Local(ITypeSig typeSig) {
			this.typeSig = typeSig;
		}
	}
}
