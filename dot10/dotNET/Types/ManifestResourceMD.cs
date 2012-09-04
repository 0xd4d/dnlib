using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the ManifestResource table
	/// </summary>
	sealed class ManifestResourceMD : ManifestResource {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawManifestResourceRow rawRow;

		UserValue<uint> offset;
		UserValue<ManifestResourceAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<IImplementation> implementation;

		/// <inheritdoc/>
		public override uint Offset {
			get { return offset.Value; }
			set { offset.Value = value; }
		}

		/// <inheritdoc/>
		public override ManifestResourceAttributes Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override IImplementation Implementation {
			get { return implementation.Value; }
			set { implementation.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ManifestResource</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public ManifestResourceMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.ManifestResource).Rows < rid)
				throw new BadImageFormatException(string.Format("ManifestResource rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			offset.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.Offset;
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (ManifestResourceAttributes)rawRow.Flags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			implementation.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveImplementation(rawRow.Implementation);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadManifestResourceRow(rid);
		}
	}
}
