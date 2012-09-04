using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the DeclSecurity table
	/// </summary>
	sealed class DeclSecurityMD : DeclSecurity {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawDeclSecurityRow rawRow;

		UserValue<DeclSecurityAction> action;
		UserValue<IHasDeclSecurity> parent;
		UserValue<byte[]> permissionSet;

		/// <inheritdoc/>
		public override DeclSecurityAction Action {
			get { return action.Value; }
			set { action.Value = value; }
		}

		/// <inheritdoc/>
		public override IHasDeclSecurity Parent {
			get { return parent.Value; }
			set { parent.Value = value; }
		}

		/// <inheritdoc/>
		public override byte[] PermissionSet {
			get { return permissionSet.Value; }
			set { permissionSet.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>DeclSecurity</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public DeclSecurityMD(ModuleDefMD readerModule, uint rid) {
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			this.rid = rid;
			this.readerModule = readerModule;
#if DEBUG
			if (readerModule.TablesStream.Get(Table.DeclSecurity).Rows < rid)
				throw new BadImageFormatException(string.Format("DeclSecurity rid {0} does not exist", rid));
#endif
			Initialize();
		}

		void Initialize() {
			action.ReadOriginalValue = () => {
				InitializeRawRow();
				return (DeclSecurityAction)rawRow.Action;
			};
			parent.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveHasDeclSecurity(rawRow.Parent);
			};
			permissionSet.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.BlobStream.Read(rawRow.PermissionSet);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadDeclSecurityRow(rid);
		}
	}
}
