/*
    Copyright (C) 2012-2014 de4dot@gmail.com

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

﻿namespace dnlib.DotNet.MD {
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
	/// Reads table rows
	/// </summary>
	/// <typeparam name="TRow">Raw row</typeparam>
	public interface IRowReader<TRow> where TRow : class, IRawRow {
		/// <summary>
		/// Reads a table row
		/// </summary>
		/// <param name="rid">Row id</param>
		/// <returns>The table row or <c>null</c> if its row should be read from the original
		/// table</returns>
		TRow ReadRow(uint rid);
	}
}
