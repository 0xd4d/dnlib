/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// A MD table (eg. Method table)
	/// </summary>
	[DebuggerDisplay("DL:{imageStream.Length} R:{numRows} RS:{tableInfo.RowSize} C:{tableInfo.Columns.Count} {tableInfo.Name}")]
	public sealed class MDTable : IDisposable, IFileSection {
		readonly Table table;
		uint numRows;
		TableInfo tableInfo;
		IImageStream imageStream;

		/// <inheritdoc/>
		public FileOffset StartOffset {
			get { return imageStream.FileOffset; }
		}

		/// <inheritdoc/>
		public FileOffset EndOffset {
			get { return imageStream.FileOffset + imageStream.Length; }
		}

		/// <summary>
		/// Gets the table
		/// </summary>
		public Table Table {
			get { return table; }
		}

		/// <summary>
		/// Gets the name of this table
		/// </summary>
		public string Name {
			get { return tableInfo.Name; }
		}

		/// <summary>
		/// Returns total number of rows
		/// </summary>
		public uint Rows {
			get { return numRows; }
		}

		/// <summary>
		/// Gets the total size in bytes of one row in this table
		/// </summary>
		public uint RowSize {
			get { return (uint)tableInfo.RowSize; }
		}

		/// <summary>
		/// Returns all the columns
		/// </summary>
		public IList<ColumnInfo> Columns {
			get { return tableInfo.Columns; }
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
