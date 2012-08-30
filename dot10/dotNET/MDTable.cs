using System.IO;

namespace dot10.dotNET {
	/// <summary>
	/// A MD table (eg. Method table)
	/// </summary>
	public class MDTable {
		uint numRows;
		TableInfo tableInfo;
		BinaryReader reader;

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
		/// The reader that can access all the rows in this table
		/// </summary>
		internal BinaryReader Reader {
			set { reader = value; }
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
		public override string ToString() {
			return string.Format("DL:{0:X8} R:{1} RS:{2} C:{3} {4}", reader.BaseStream.Length, numRows, tableInfo.RowSize, tableInfo.Columns.Count, tableInfo.Name);
		}
	}
}
