// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// A MD table (eg. Method table)
	/// </summary>
	[DebuggerDisplay("DL:{imageStream.Length} R:{numRows} RS:{tableInfo.RowSize} C:{Count} {tableInfo.Name}")]
	public sealed class MDTable : IDisposable, IFileSection {
		readonly Table table;
		uint numRows;
		TableInfo tableInfo;
		IImageStream imageStream;

		// Fix for VS2015 expression evaluator: "The debugger is unable to evaluate this expression"
		int Count => tableInfo.Columns.Count;

		/// <inheritdoc/>
		public FileOffset StartOffset => imageStream.FileOffset;

		/// <inheritdoc/>
		public FileOffset EndOffset => imageStream.FileOffset + imageStream.Length;

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

		/// <summary>
		/// The stream that can access all the rows in this table
		/// </summary>
		internal IImageStream ImageStream {
			get => imageStream;
			set {
				var ims = imageStream;
				if (ims == value)
					return;
				if (ims != null)
					ims.Dispose();
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

		internal IImageStream CloneImageStream() => imageStream.Clone();

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
			var ims = imageStream;
			if (ims != null)
				ims.Dispose();
			numRows = 0;
			tableInfo = null;
			imageStream = null;
		}
	}
}
