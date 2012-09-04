using System.Diagnostics;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the DeclSecurity table
	/// </summary>
	[DebuggerDisplay("{Action} {Parent}")]
	public abstract class DeclSecurity : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.DeclSecurity, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 8; }
		}

		/// <summary>
		/// From column DeclSecurity.Action
		/// </summary>
		public abstract DeclSecurityAction Action { get; set; }

		/// <summary>
		/// From column DeclSecurity.Parent
		/// </summary>
		public abstract IHasDeclSecurity Parent { get; set; }

		/// <summary>
		/// From column DeclSecurity.PermissionSet
		/// </summary>
		public abstract byte[] PermissionSet { get; set; }
	}

	/// <summary>
	/// A DeclSecurity row created by the user and not present in the original .NET file
	/// </summary>
	public class DeclSecurityUser : DeclSecurity {
		DeclSecurityAction action;
		IHasDeclSecurity parent;
		byte[] permissionSet;

		/// <inheritdoc/>
		public override DeclSecurityAction Action {
			get { return action; }
			set { action = value; }
		}

		/// <inheritdoc/>
		public override IHasDeclSecurity Parent {
			get { return parent; }
			set { parent = value; }
		}

		/// <inheritdoc/>
		public override byte[] PermissionSet {
			get { return permissionSet; }
			set { permissionSet = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public DeclSecurityUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="action">The security action</param>
		/// <param name="permissionSet">The permission set</param>
		public DeclSecurityUser(DeclSecurityAction action, byte[] permissionSet) {
			this.action = action;
			this.permissionSet = permissionSet;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="action">The security action</param>
		/// <param name="permissionSet">The permission set</param>
		/// <param name="parent">Parent</param>
		public DeclSecurityUser(DeclSecurityAction action, byte[] permissionSet, IHasDeclSecurity parent) {
			this.action = action;
			this.permissionSet = permissionSet;
			this.parent = parent;
		}
	}
}
