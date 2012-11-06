namespace dot10.DotNet.MD {
	/// <summary>
	/// Reads metadata table columns
	/// </summary>
	public interface IColumnReader {
		/// <summary>
		/// Reads a column
		/// </summary>
		/// <param name="table">The table to read from</param>
		/// <param name="rid">Table row id</param>
		/// <param name="column">The column to read</param>
		/// <param name="value">Result</param>
		/// <returns><c>true</c> if <paramref name="value"/> was updated, <c>false</c> if
		/// the column should be read from the original table.</returns>
		bool ReadColumn(MDTable table, uint rid, ColumnInfo column, out uint value);
	}

	/// <summary>
	/// Reads <c>Method</c> table rows
	/// </summary>
	public interface IRowReader<T> where T : class {
		/// <summary>
		/// Reads a table row
		/// </summary>
		/// <param name="rid">Row id</param>
		/// <returns>The table row or <c>null</c> if its row should be read from the original
		/// table</returns>
		T ReadRow(uint rid);
	}
}
