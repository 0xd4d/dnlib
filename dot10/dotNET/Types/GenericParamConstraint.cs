#pragma warning disable 0169	//TODO:

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the GenericParamConstraint table
	/// </summary>
	public class GenericParamConstraint : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column GenericParamConstraint.Owner
		/// </summary>
		GenericParam owner;

		/// <summary>
		/// From column GenericParamConstraint.Constraint
		/// </summary>
		ITypeDefOrRef constraint;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.GenericParamConstraint, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 20; }
		}
	}
}
