using System;

namespace dot10.dotNET.Types {
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
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			this.rid = rid;
			this.readerModule = readerModule;
#if DEBUG
			if (readerModule.TablesStream.Get(Table.GenericParamConstraint).Rows < rid)
				throw new BadImageFormatException(string.Format("GenericParamConstraint rid {0} does not exist", rid));
#endif
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
