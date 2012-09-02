using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the Assembly table
	/// </summary>
	sealed class AssemblyDefMD : AssemblyDef {
		/// <summary>The .NET metadata where this instance is located</summary>
		IMetaData metaData;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawAssemblyRow rawRow;
		UserValue<AssemblyHashAlgorithm> hashAlgId;
		UserValue<Version> version;
		UserValue<AssemblyFlags> flags;
		UserValue<byte[]> publicKey;
		UserValue<UTF8String> name;
		UserValue<UTF8String> locale;

		/// <inheritdoc/>
		public override AssemblyHashAlgorithm HashAlgId {
			get { return hashAlgId.Value; }
			set { hashAlgId.Value = value; }
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
		public override byte[] PublicKey {
			get { return publicKey.Value; }
			set { publicKey.Value = value; }
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

		public AssemblyDefMD(IMetaData metaData, uint rid) {
			if (metaData == null)
				throw new ArgumentException("metaData");
			if (rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			this.rid = rid;
			this.metaData = metaData;
			if (metaData.TablesStream.GetTable(Table.Assembly).Rows < rid)
				throw new BadImageFormatException(string.Format("Assembly rid {0} does not exist", rid));
			Initialize();
		}

		void Initialize() {
			hashAlgId = new UserValue<AssemblyHashAlgorithm> {
				ReadOriginalValue = () => {
					InitializeRawRow();
					return (AssemblyHashAlgorithm)rawRow.HashAlgId;
				}
			};
			version = new UserValue<Version> {
				ReadOriginalValue = () => {
					InitializeRawRow();
					return new Version(rawRow.MajorVersion, rawRow.MinorVersion, rawRow.BuildNumber, rawRow.RevisionNumber);
				}
			};
			flags = new UserValue<AssemblyFlags> {
				ReadOriginalValue = () => {
					InitializeRawRow();
					return (AssemblyFlags)rawRow.Flags;
				}
			};
			publicKey = new UserValue<byte[]> {
				ReadOriginalValue = () => {
					InitializeRawRow();
					return metaData.BlobStream.Read(rawRow.PublicKey);
				}
			};
			name = new UserValue<UTF8String> {
				ReadOriginalValue = () => {
					InitializeRawRow();
					return metaData.StringsStream.Read(rawRow.Name);
				}
			};
			locale = new UserValue<UTF8String> {
				ReadOriginalValue = () => {
					InitializeRawRow();
					return metaData.StringsStream.Read(rawRow.Locale);
				}
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = metaData.TablesStream.ReadAssemblyRow(rid);
		}
	}
}
