#pragma warning disable 0169	//TODO:

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the ModuleRef table
	/// </summary>
	public class ModuleRef : IHasCustomAttribute, IMemberRefParent, IResolutionScope {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column ModuleRef.Name
		/// </summary>
		string name;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.ModuleRef, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 12; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int ResolutionScopeTag {
			get { return 1; }
		}
	}
}
