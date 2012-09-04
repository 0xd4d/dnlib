using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the AssemblyRef table
	/// </summary>
	sealed class AssemblyRefMD : AssemblyRef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawAssemblyRefRow rawRow;

		UserValue<Version> version;
		UserValue<AssemblyFlags> flags;
		UserValue<PublicKeyBase> publicKeyOrToken;
		UserValue<UTF8String> name;
		UserValue<UTF8String> locale;
		UserValue<byte[]> hashValue;

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
			set { flags.Value = (value & ~AssemblyFlags.PublicKey) | (flags.Value & AssemblyFlags.PublicKey); }
		}

		/// <inheritdoc/>
		public override PublicKeyBase PublicKeyOrToken {
			get { return publicKeyOrToken.Value; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				if (value is PublicKey)
					flags.Value |= AssemblyFlags.PublicKey;
				else
					flags.Value &= ~AssemblyFlags.PublicKey;
				publicKeyOrToken.Value = value;
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

		/// <inheritdoc/>
		public override byte[] HashValue {
			get { return hashValue.Value; }
			set { hashValue.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>AssemblyRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public AssemblyRefMD(ModuleDefMD readerModule, uint rid) {
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			this.rid = rid;
			this.readerModule = readerModule;
#if DEBUG
			if (readerModule.TablesStream.Get(Table.AssemblyRef).Rows < rid)
				throw new BadImageFormatException(string.Format("AssemblyRef rid {0} does not exist", rid));
#endif
			Initialize();
		}

		void Initialize() {
			version.ReadOriginalValue = () => {
				InitializeRawRow();
				return new Version(rawRow.MajorVersion, rawRow.MinorVersion, rawRow.BuildNumber, rawRow.RevisionNumber);
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (AssemblyFlags)rawRow.Flags;
			};
			publicKeyOrToken.ReadOriginalValue = () => {
				InitializeRawRow();
				var pkData = readerModule.BlobStream.Read(rawRow.PublicKeyOrToken);
				if ((rawRow.Flags & (uint)AssemblyFlags.PublicKey) != 0)
					return new PublicKey(pkData);
				return new PublicKeyToken(pkData);
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			locale.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Locale);
			};
			hashValue.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.BlobStream.Read(rawRow.HashValue);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadAssemblyRefRow(rid);
		}
	}
}
