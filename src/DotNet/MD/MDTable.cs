// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// A MD table (eg. Method table)
	/// </summary>
	[DebuggerDisplay("DL:{dataReader.Length} R:{numRows} RS:{tableInfo.RowSize} C:{Count} {tableInfo.Name}")]
	public sealed class MDTable : IDisposable, IFileSection {
		readonly Table table;
		uint numRows;
		TableInfo tableInfo;
		DataReader dataReader;

		// Fix for VS2015 expression evaluator: "The debugger is unable to evaluate this expression"
		int Count => tableInfo.Columns.Length;

		/// <inheritdoc/>
		public FileOffset StartOffset => (FileOffset)dataReader.StartOffset;

		/// <inheritdoc/>
		public FileOffset EndOffset => (FileOffset)dataReader.EndOffset;

		/// <summary>
		/// Gets the table
		/// </summary>
		public Table Table => table;

		/// <summary>
		/// Gets the name of this table
		/// </summary>
		public string Name => tableInfo.Name;

		/// <summary>
		/// Returns total number of rows
		/// </summary>
		public uint Rows => numRows;

		/// <summary>
		/// Gets the total size in bytes of one row in this table
		/// </summary>
		public uint RowSize => (uint)tableInfo.RowSize;

		/// <summary>
		/// Returns all the columns
		/// </summary>
		public IList<ColumnInfo> Columns => tableInfo.Columns;

		/// <summary>
		/// Returns <c>true</c> if there are no valid rows
		/// </summary>
		public bool IsEmpty => numRows == 0;

		/// <summary>
		/// Returns info about this table
		/// </summary>
		public TableInfo TableInfo => tableInfo;

		internal DataReader DataReader {
			get => dataReader;
			set => dataReader = value;
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

			var columns = tableInfo.Columns;
			int length = columns.Length;
			if (length > 0) Column0 = columns[0];
			if (length > 1) Column1 = columns[1];
			if (length > 2) Column2 = columns[2];
			if (length > 3) Column3 = columns[3];
			if (length > 4) Column4 = columns[4];
			if (length > 5) Column5 = columns[5];
			if (length > 6) Column6 = columns[6];
			if (length > 7) Column7 = columns[7];
			if (length > 8) Column8 = columns[8];
		}

		// So we don't have to call IList<T> indexer
		internal readonly ColumnInfo Column0;
		internal readonly ColumnInfo Column1;
		internal readonly ColumnInfo Column2;
		internal readonly ColumnInfo Column3;
		internal readonly ColumnInfo Column4;
		internal readonly ColumnInfo Column5;
		internal readonly ColumnInfo Column6;
		internal readonly ColumnInfo Column7;
		internal readonly ColumnInfo Column8;

		/// <summary>
		/// Checks whether the row <paramref name="rid"/> exists
		/// </summary>
		/// <param name="rid">Row ID</param>
		public bool IsValidRID(uint rid) => rid != 0 && rid <= numRows;

		/// <summary>
		/// Checks whether the row <paramref name="rid"/> does not exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		public bool IsInvalidRID(uint rid) => rid == 0 || rid > numRows;

		/// <inheritdoc/>
		public void Dispose() {
			numRows = 0;
			tableInfo = null;
			dataReader = default;
		}
	}
}
