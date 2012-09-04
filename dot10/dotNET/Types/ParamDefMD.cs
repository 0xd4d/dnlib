using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the Param table
	/// </summary>
	sealed class ParamDefMD : ParamDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawParamRow rawRow;

		UserValue<ParamAttributes> flags;
		UserValue<ushort> sequence;
		UserValue<UTF8String> name;

		/// <inheritdoc/>
		public override ParamAttributes Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override ushort Sequence {
			get { return sequence.Value; }
			set { sequence.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Param</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public ParamDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.Param).Rows < rid)
				throw new BadImageFormatException(string.Format("Param rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (ParamAttributes)rawRow.Flags;
			};
			sequence.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.Sequence;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadParamRow(rid);
		}
	}
}
