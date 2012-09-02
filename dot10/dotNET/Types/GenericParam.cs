#pragma warning disable 0169	//TODO:

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the GenericParam table
	/// </summary>
	public class GenericParam : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column GenericParam.Number
		/// </summary>
		ushort number;

		/// <summary>
		/// From column GenericParam.Flags
		/// </summary>
		ushort flags;

		/// <summary>
		/// From column GenericParam.Owner
		/// </summary>
		ITypeOrMethodDef owner;

		/// <summary>
		/// From column GenericParam.Name
		/// </summary>
		string name;

		/// <summary>
		/// From column GenericParam.Kind (v1.1 only)
		/// </summary>
		ITypeDefOrRef kind;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.GenericParam, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 19; }
		}
	}
}
