using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the TypeRef table
	/// </summary>
	sealed class TypeRefMD : TypeRef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawTypeRefRow rawRow;

		UserValue<IResolutionScope> resolutionScope;
		UserValue<UTF8String> name;
		UserValue<UTF8String> @namespace;

		/// <inheritdoc/>
		public override IResolutionScope ResolutionScope {
			get { return resolutionScope.Value; }
			set { resolutionScope.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Namespace {
			get { return @namespace.Value; }
			set { @namespace.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>TypeRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public TypeRefMD(ModuleDefMD readerModule, uint rid) {
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			this.rid = rid;
			this.readerModule = readerModule;
#if DEBUG
			if (readerModule.TablesStream.Get(Table.TypeRef).Rows < rid)
				throw new BadImageFormatException(string.Format("TypeRef rid {0} does not exist", rid));
#endif
			Initialize();
		}

		void Initialize() {
			resolutionScope.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveResolutionScope(rawRow.ResolutionScope);
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			@namespace.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Namespace);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadTypeRefRow(rid);
		}
	}
}
