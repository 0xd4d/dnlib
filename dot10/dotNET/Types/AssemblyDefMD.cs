using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the Assembly table
	/// </summary>
	sealed class AssemblyDefMD : AssemblyDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawAssemblyRow rawRow;
		UserValue<AssemblyHashAlgorithm> hashAlgId;
		UserValue<Version> version;
		UserValue<AssemblyFlags> flags;
		UserValue<PublicKey> publicKey;
		UserValue<UTF8String> name;
		UserValue<UTF8String> locale;

		/// <inheritdoc/>
		public override AssemblyHashAlgorithm HashAlgId {
			get { return hashAlgId.Value; }
			set {
				hashAlgId.Value = value;
				publicKey.Value.HashAlgorithm = hashAlgId.Value;
			}
		}

		/// <inheritdoc/>
		public override Version Version {
			get { return version.Value; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				version.Value = value;
			}
		}

		/// <inheritdoc/>
		public override AssemblyFlags Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override PublicKey PublicKey {
			get { return publicKey.Value; }
			set {
				publicKey.Value = value ?? new PublicKey();
				publicKey.Value.HashAlgorithm = hashAlgId.Value;
			}
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Locale {
			get { return locale.Value; }
			set { locale.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Assembly</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public AssemblyDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.Assembly).Rows < rid)
				throw new BadImageFormatException(string.Format("Assembly rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			hashAlgId.ReadOriginalValue = () => {
				InitializeRawRow();
				return (AssemblyHashAlgorithm)rawRow.HashAlgId;
			};
			version.ReadOriginalValue = () => {
				InitializeRawRow();
				return new Version(rawRow.MajorVersion, rawRow.MinorVersion, rawRow.BuildNumber, rawRow.RevisionNumber);
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (AssemblyFlags)rawRow.Flags;
			};
			publicKey.ReadOriginalValue = () => {
				InitializeRawRow();
				return new PublicKey(readerModule.BlobStream.Read(rawRow.PublicKey));
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			locale.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Locale);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadAssemblyRow(rid);
		}
	}
}
