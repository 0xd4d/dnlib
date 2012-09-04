namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the GenericParamConstraint table
	/// </summary>
	public abstract class GenericParamConstraint : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.GenericParamConstraint, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 20; }
		}

		/// <summary>
		/// From column GenericParamConstraint.Owner
		/// </summary>
		public abstract GenericParam Owner { get; set; }

		/// <summary>
		/// From column GenericParamConstraint.Constraint
		/// </summary>
		public abstract ITypeDefOrRef Constraint { get; set; }
	}

	/// <summary>
	/// A GenericParamConstraintAssembly row created by the user and not present in the original .NET file
	/// </summary>
	public class GenericParamConstraintUser : GenericParamConstraint {
		GenericParam owner;
		ITypeDefOrRef constraint;

		/// <inheritdoc/>
		public override GenericParam Owner {
			get { return owner; }
			set { owner = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef Constraint {
			get { return constraint; }
			set { constraint = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public GenericParamConstraintUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="constraint">The constraint</param>
		public GenericParamConstraintUser(ITypeDefOrRef constraint) {
			this.constraint = constraint;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="owner">The owner</param>
		/// <param name="constraint">The constraint</param>
		public GenericParamConstraintUser(GenericParam owner, ITypeDefOrRef constraint) {
			this.owner = owner;
			this.constraint = constraint;
		}
	}
}
