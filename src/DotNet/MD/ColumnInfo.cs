// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using dnlib.DotNet.Writer;
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
		public int Index => index;

		/// <summary>
		/// Returns the column offset within the table row
		/// </summary>
		public int Offset {
			get => offset;
			internal set => offset = (byte)value;
		}

		/// <summary>
		/// Returns the column size
		/// </summary>
		public int Size {
			get => size;
			internal set => size = (byte)value;
		}

		/// <summary>
		/// Returns the column name
		/// </summary>
		public string Name => name;

		/// <summary>
		/// Returns the ColumnSize enum value
		/// </summary>
		public ColumnSize ColumnSize => columnSize;

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
		public uint Read(ref DataReader reader) =>
			size switch {
				1 => reader.ReadByte(),
				2 => reader.ReadUInt16(),
				4 => reader.ReadUInt32(),
				_ => throw new InvalidOperationException("Invalid column size"),
			};

		internal uint Unsafe_Read24(ref DataReader reader) {
			Debug.Assert(size == 2 || size == 4);
			return size == 2 ? reader.Unsafe_ReadUInt16() : reader.Unsafe_ReadUInt32();
		}

		/// <summary>
		/// Writes a column
		/// </summary>
		/// <param name="writer">The writer position on this column</param>
		/// <param name="value">The column value</param>
		public void Write(DataWriter writer, uint value) {
			switch (size) {
			case 1: writer.WriteByte((byte)value); break;
			case 2: writer.WriteUInt16((ushort)value); break;
			case 4: writer.WriteUInt32(value); break;
			default: throw new InvalidOperationException("Invalid column size");
			}
		}

		internal void Write24(DataWriter writer, uint value) {
			Debug.Assert(size == 2 || size == 4);
			if (size == 2)
				writer.WriteUInt16((ushort)value);
			else
				writer.WriteUInt32(value);
		}
	}
}
