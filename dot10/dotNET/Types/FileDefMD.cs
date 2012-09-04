using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the File table
	/// </summary>
	sealed class FileDefMD : FileDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawFileRow rawRow;

		UserValue<FileFlags> flags;
		UserValue<UTF8String> name;
		UserValue<byte[]> hashValue;

		/// <inheritdoc/>
		public override FileFlags Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override byte[] HashValue {
			get { return hashValue.Value; }
			set { hashValue.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>File</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public FileDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.File).Rows < rid)
				throw new BadImageFormatException(string.Format("File rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (FileFlags)rawRow.Flags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			hashValue.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.BlobStream.Read(rawRow.HashValue);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadFileRow(rid);
		}
	}
}
