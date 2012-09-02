using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the Module table
	/// </summary>
	sealed class MDModuleDef : ModuleDef {
		DotNetFile dnFile;
		RawModuleRow rawRow;
		UserValue<ushort> generation;
		UserValue<string> name;
		UserValue<Guid?> mvid;
		UserValue<Guid?> encId;
		UserValue<Guid?> encBaseId;

		/// <inheritdoc/>
		public override ushort Generation {
			get { return generation.Value; }
			set { generation.Value = value; }
		}

		/// <inheritdoc/>
		public override string Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override Guid? Mvid {
			get { return mvid.Value; }
			set { mvid.Value = value; }
		}

		/// <inheritdoc/>
		public override Guid? EncId {
			get { return encId.Value; }
			set { encId.Value = value; }
		}

		/// <inheritdoc/>
		public override Guid? EncBaseId {
			get { return encBaseId.Value; }
			set { encBaseId.Value = value; }
		}

		/// <summary>
		/// Creates a <see cref="MDModuleDef"/> instance from a file
		/// </summary>
		/// <param name="fileName">File name of an existing .NET module/assembly</param>
		/// <returns>A new <see cref="MDModuleDef"/> instance</returns>
		public new static MDModuleDef Load(string fileName) {
			DotNetFile dnFile = null;
			try {
				return Load(dnFile = DotNetFile.Load(fileName));
			}
			catch {
				if (dnFile != null)
					dnFile.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates a <see cref="MDModuleDef"/> instance from a byte[]
		/// </summary>
		/// <param name="data">Contents of a .NET module/assembly</param>
		/// <returns>A new <see cref="MDModuleDef"/> instance</returns>
		public new static MDModuleDef Load(byte[] data) {
			DotNetFile dnFile = null;
			try {
				return Load(dnFile = DotNetFile.Load(data));
			}
			catch {
				if (dnFile != null)
					dnFile.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates a <see cref="MDModuleDef"/> instance from a memory location
		/// </summary>
		/// <param name="addr">Address of a .NET module/assembly</param>
		/// <returns>A new <see cref="MDModuleDef"/> instance</returns>
		public new static MDModuleDef Load(IntPtr addr) {
			DotNetFile dnFile = null;
			try {
				return Load(dnFile = DotNetFile.Load(addr));
			}
			catch {
				if (dnFile != null)
					dnFile.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates a <see cref="MDModuleDef"/> instance from a <see cref="DotNetFile"/>
		/// </summary>
		/// <param name="dnFile">The loaded .NET file</param>
		/// <returns>A new <see cref="MDModuleDef"/> instance that now owns <paramref name="dnFile"/></returns>
		public new static MDModuleDef Load(DotNetFile dnFile) {
			return new MDModuleDef(dnFile);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dnFile">The loaded .NET file</param>
		/// <exception cref="ArgumentNullException">If <paramref name="dnFile"/> is null</exception>
		MDModuleDef(DotNetFile dnFile) {
			if (dnFile == null)
				throw new ArgumentNullException("dnFile");

			this.dnFile = dnFile;
			this.rid = 1;

			this.generation = new UserValue<ushort> {
				ReadOriginalValue = () => {
					InitializeRawRow();
					return rawRow.Generation;
				}
			};
			this.name = new UserValue<string> {
				ReadOriginalValue = () => {
					InitializeRawRow();
					return dnFile.MetaData.StringsStream.Read(rawRow.Name);
				}
			};
			this.mvid = new UserValue<Guid?> {
				ReadOriginalValue = () => {
					InitializeRawRow();
					return dnFile.MetaData.GuidStream.Read(rawRow.Mvid);
				}
			};
			this.encId = new UserValue<Guid?> {
				ReadOriginalValue = () => {
					InitializeRawRow();
					return dnFile.MetaData.GuidStream.Read(rawRow.EncId);
				}
			};
			this.encBaseId = new UserValue<Guid?> {
				ReadOriginalValue = () => {
					InitializeRawRow();
					return dnFile.MetaData.GuidStream.Read(rawRow.EncBaseId);
				}
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = dnFile.MetaData.TablesStream.ReadModuleRow(rid) ?? new RawModuleRow();
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				if (dnFile != null)
					dnFile.Dispose();
				dnFile = null;
			}
			base.Dispose(disposing);
		}
	}
}
