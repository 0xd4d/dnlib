#pragma warning disable 0169	//TODO:

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the InterfaceImpl table
	/// </summary>
	public class InterfaceImpl : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column InterfaceImpl.Class
		/// </summary>
		TypeDef @class;

		/// <summary>
		/// From column InterfaceImpl.Interface
		/// </summary>
		ITypeDefOrRef @interface;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.InterfaceImpl, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 5; }
		}
	}
}
