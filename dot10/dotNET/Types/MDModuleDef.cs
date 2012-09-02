using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the Module table
	/// </summary>
	sealed class MDModuleDef : ModuleDef {
		DotNetFile dnFile;

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
			if (dnFile.MetaData.TablesStream.GetTable(Table.Module).Rows < this.rid)
				this.rid = 0;
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
