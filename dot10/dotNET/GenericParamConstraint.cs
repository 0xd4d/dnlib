using System;
using dot10.dotNET.MD;

namespace dot10.dotNET {
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

	/// <summary>
	/// Created from a row in the GenericParamConstraint table
	/// </summary>
	sealed class GenericParamConstraintMD : GenericParamConstraint {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawGenericParamConstraintRow rawRow;

		UserValue<GenericParam> owner;
		UserValue<ITypeDefOrRef> constraint;

		/// <inheritdoc/>
		public override GenericParam Owner {
			get { return owner.Value; }
			set { owner.Value = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef Constraint {
			get { return constraint.Value; }
			set { constraint.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>GenericParamConstraint</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public GenericParamConstraintMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.GenericParamConstraint).Rows < rid)
				throw new BadImageFormatException(string.Format("GenericParamConstraint rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			owner.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveGenericParam(rawRow.Owner);
			};
			constraint.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveTypeDefOrRef(rawRow.Constraint);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadGenericParamConstraintRow(rid);
		}
	}
}
