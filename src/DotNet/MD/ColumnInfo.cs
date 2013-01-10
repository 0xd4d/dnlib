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
using System.Diagnostics;
using System.IO;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Info about one column in a MD table
	/// </summary>
	[DebuggerDisplay("{offset} {size} {name}")]
	public sealed class ColumnInfo {
		readonly byte index;
		byte offset;
		readonly ColumnSize columnSize;
		byte size;
		readonly string name;

		/// <summary>
		/// Gets the column index
		/// </summary>
		public int Index {
			get { return index; }
		}

		/// <summary>
		/// Returns the column offset within the table row
		/// </summary>
		public int Offset {
			get { return offset; }
			internal set { offset = (byte)value; }
		}

		/// <summary>
		/// Returns the column size
		/// </summary>
		public int Size {
			get { return size; }
			internal set { size = (byte)value; }
		}

		/// <summary>
		/// Returns the column name
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Returns the ColumnSize enum value
		/// </summary>
		public ColumnSize ColumnSize {
			get { return columnSize; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="index">Column index</param>
		/// <param name="name">The column name</param>
		/// <param name="columnSize">Column size</param>
		public ColumnInfo(byte index, string name, ColumnSize columnSize) {
			this.index = index;
			this.name = name;
			this.columnSize = columnSize;
		}

		/// <summary>
		/// Reads the column
		/// </summary>
		/// <param name="reader">A reader positioned on this column</param>
		/// <returns>The column value</returns>
		public uint Read(IBinaryReader reader) {
			switch (size) {
			case 1: return reader.ReadByte();
			case 2: return reader.ReadUInt16();
			case 4: return reader.ReadUInt32();
			default: throw new InvalidOperationException("Invalid column size");
			}
		}

		/// <summary>
		/// Writes a column
		/// </summary>
		/// <param name="writer">The writer position on this column</param>
		/// <param name="value">The column value</param>
		public void Write(BinaryWriter writer, uint value) {
			switch (size) {
			case 1: writer.Write((byte)value); break;
			case 2: writer.Write((ushort)value); break;
			case 4: writer.Write(value); break;
			default: throw new InvalidOperationException("Invalid column size");
			}
		}
	}
}
