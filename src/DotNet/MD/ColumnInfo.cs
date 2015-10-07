// dnlib: See LICENSE.txt for more info

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
		/// Constructor
		/// </summary>
		/// <param name="index">Column index</param>
		/// <param name="name">The column name</param>
		/// <param name="columnSize">Column size</param>
		/// <param name="offset">Offset of column</param>
		/// <param name="size">Size of column</param>
		public ColumnInfo(byte index, string name, ColumnSize columnSize, byte offset, byte size) {
			this.index = index;
			this.name = name;
			this.columnSize = columnSize;
			this.offset = offset;
			this.size = size;
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
