using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the GenericParam table
	/// </summary>
	sealed class GenericParamMD : GenericParam {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawGenericParamRow rawRow;

		UserValue<ushort> number;
		UserValue<GenericParamAttributes> flags;
		UserValue<ITypeOrMethodDef> owner;
		UserValue<UTF8String> name;
		UserValue<ITypeDefOrRef> kind;

		/// <inheritdoc/>
		public override ushort Number {
			get { return number.Value; }
			set { number.Value = value; }
		}

		/// <inheritdoc/>
		public override GenericParamAttributes Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override ITypeOrMethodDef Owner {
			get { return owner.Value; }
			set { owner.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef Kind {
			get { return kind.Value; }
			set { kind.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>GenericParam</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public GenericParamMD(ModuleDefMD readerModule, uint rid) {
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			this.rid = rid;
			this.readerModule = readerModule;
#if DEBUG
			if (readerModule.TablesStream.Get(Table.GenericParam).Rows < rid)
				throw new BadImageFormatException(string.Format("GenericParam rid {0} does not exist", rid));
#endif
			Initialize();
		}

		void Initialize() {
			number.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.Number;
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (GenericParamAttributes)rawRow.Flags;
			};
			owner.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveTypeOrMethodDef(rawRow.Owner);
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			kind.ReadOriginalValue = () => {
				if (readerModule.TablesStream.Get(Table.GenericParam).TableInfo.Columns.Count != 5)
					return null;
				InitializeRawRow();
				return readerModule.ResolveTypeDefOrRef(rawRow.Kind);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadGenericParamRow(rid);
		}
	}
}
