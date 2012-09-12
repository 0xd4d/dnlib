using System;
using System.Diagnostics;
using System.IO;
using dot10.IO;

namespace dot10.DotNet.MD {
	/// <summary>
	/// A MD table (eg. Method table)
	/// </summary>
	[DebuggerDisplay("DL:{imageStream.Length} R:{numRows} RS:{tableInfo.RowSize} C:{tableInfo.Columns.Count} {tableInfo.Name}")]
	public sealed class MDTable : IDisposable {
		readonly Table table;
		uint numRows;
		TableInfo tableInfo;
		IImageStream imageStream;

		/// <summary>
		/// Gets the table
		/// </summary>
		public Table Table {
			get { return table; }
		}

		/// <summary>
		/// Returns total number of rows
		/// </summary>
		public uint Rows {
			get { return numRows; }
		}

		/// <summary>
		/// Returns <c>true</c> if there are no valid rows
		/// </summary>
		public bool IsEmpty {
			get { return numRows == 0; }
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
		/// <param name="table">The table</param>
		/// <param name="numRows">Number of rows in this table</param>
		/// <param name="tableInfo">Info about this table</param>
		internal MDTable(Table table, uint numRows, TableInfo tableInfo) {
			this.table = table;
			this.numRows = numRows;
			this.tableInfo = tableInfo;
		}

		/// <summary>
		/// Checks whether the row <paramref name="rid"/> exists
		/// </summary>
		/// <param name="rid">Row ID</param>
		public bool IsValidRID(uint rid) {
			return rid != 0 && rid <= numRows;
		}

		/// <summary>
		/// Checks whether the row <paramref name="rid"/> does not exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		public bool IsInvalidRID(uint rid) {
			return rid == 0 || rid > numRows;
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
