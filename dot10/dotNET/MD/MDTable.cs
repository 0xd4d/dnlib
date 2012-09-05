using System;
using System.Diagnostics;
using System.IO;
using dot10.IO;

namespace dot10.dotNET.MD {
	/// <summary>
	/// A MD table (eg. Method table)
	/// </summary>
	[DebuggerDisplay("DL:{imageStream.Length} R:{numRows} RS:{tableInfo.RowSize} C:{tableInfo.Columns.Count} {tableInfo.Name}")]
	public sealed class MDTable : IDisposable {
		uint numRows;
		TableInfo tableInfo;
		IImageStream imageStream;

		/// <summary>
		/// Returns total number of rows
		/// </summary>
		public uint Rows {
			get { return numRows; }
		}

		/// <summary>
		/// Returns info about this table
		/// </summary>
		public TableInfo TableInfo {
			get { return tableInfo; }
		}

		/// <summary>
		/// The stream that can access all the rows in this table
		/// </summary>
		internal IImageStream ImageStream {
			get { return imageStream; }
			set {
				if (imageStream != null)
					imageStream.Dispose();
				imageStream = value;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="numRows">Number of rows in this table</param>
		/// <param name="tableInfo">Info about this table</param>
		internal MDTable(uint numRows, TableInfo tableInfo) {
			this.numRows = numRows;
			this.tableInfo = tableInfo;
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (imageStream != null)
				imageStream.Dispose();
			numRows = 0;
			tableInfo = null;
			imageStream = null;
		}
	}
}
