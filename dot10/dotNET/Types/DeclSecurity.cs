#pragma warning disable 0169	//TODO:

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the DeclSecurity table
	/// </summary>
	public abstract class DeclSecurity : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column DeclSecurity.Action
		/// </summary>
		short action;

		/// <summary>
		/// From column DeclSecurity.Parent
		/// </summary>
		IHasDeclSecurity parent;

		/// <summary>
		/// From column DeclSecurity.PermissionSet
		/// </summary>
		uint permissionSet;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.DeclSecurity, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 8; }
		}
	}

	/// <summary>
	/// A DeclSecurity row created by the user and not present in the original .NET file
	/// </summary>
	public class DeclSecurityUser : DeclSecurity {
	}
}
