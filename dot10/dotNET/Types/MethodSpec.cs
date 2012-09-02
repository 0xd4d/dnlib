#pragma warning disable 0169	//TODO:

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the MethodSpec table
	/// </summary>
	public class MethodSpec : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column MethodSpec.Method
		/// </summary>
		IMethodDefOrRef method;

		/// <summary>
		/// From column MethodSpec.Instantiation
		/// </summary>
		ISignature instantiation;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.MethodSpec, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 21; }
		}
	}
}
