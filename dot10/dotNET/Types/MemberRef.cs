#pragma warning disable 0169	//TODO:

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the MemberRef table
	/// </summary>
	public class MemberRef : IHasCustomAttribute, IMethodDefOrRef, ICustomAttributeType {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column MemberRef.Class
		/// </summary>
		IMemberRefParent @class;

		/// <summary>
		/// From column MemberRef.Name
		/// </summary>
		string name;

		/// <summary>
		/// From column MemberRef.Signature
		/// </summary>
		ISignature signature;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.MemberRef, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 6; }
		}

		/// <inheritdoc/>
		public int MethodDefOrRefTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int CustomAttributeTypeTag {
			get { return 3; }
		}
	}
}
