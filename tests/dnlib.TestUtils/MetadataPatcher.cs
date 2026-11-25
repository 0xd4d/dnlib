// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace dnlib.TestUtils {
	/// <summary>
	/// Changes values in the metadata of a valid PE file to create a file with invalid or unusual metadata.
	/// A test that reads invalid metadata then shows what's wrong with its input, eg.
	/// <code>
	/// var image = new MetadataPatcher(valid.Image)
	///     .SetCell(Table.TypeDef, rid: 2, "Extends", 0xFFFF)
	///     .ToArray();
	/// </code>
	/// The patches never change the size of anything, so all offsets stay the same.
	/// </summary>
	/// <remarks>
	/// The locations of the tables and columns come from dnlib itself (<see cref="TablesStream"/>). If dnlib
	/// got them wrong, it couldn't read any valid file either, and those tests would fail.
	/// </remarks>
	public sealed class MetadataPatcher {
		readonly byte[] image;
		// The layout of the original file. It's still correct after patching since no patch moves anything.
		readonly Metadata metadata;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="image">A valid PE file with .NET metadata. The array isn't modified.</param>
		public MetadataPatcher(byte[] image) {
			this.image = (byte[])image.Clone();
			metadata = MetadataFactory.CreateMetadata(new PEImage((byte[])image.Clone()));
		}

		/// <summary>
		/// Returns the patched PE file
		/// </summary>
		public byte[] ToArray() => (byte[])image.Clone();

		/// <summary>
		/// Returns the number of rows of a table
		/// </summary>
		public uint GetRowCount(Table table) => GetTable(table).Rows;

		/// <summary>
		/// Returns the names of a table's columns, as in ECMA-335
		/// </summary>
		public List<string> GetColumnNames(Table table) {
			var names = new List<string>();
			foreach (var columnInfo in GetTable(table).Columns)
				names.Add(columnInfo.Name);
			return names;
		}

		/// <summary>
		/// Returns the biggest value a column can have in this file (its size is 1, 2 or 4 bytes)
		/// </summary>
		public uint GetMaxCellValue(Table table, string column) {
			GetCellOffset(table, 1, column, out int size);
			return size == 4 ? uint.MaxValue : (1U << (size * 8)) - 1;
		}

		/// <summary>
		/// Reads a column of a table row
		/// </summary>
		/// <param name="table">Table</param>
		/// <param name="rid">Row ID, the first row is 1</param>
		/// <param name="column">Column name as in ECMA-335, eg. <c>Extends</c></param>
		public uint GetCell(Table table, uint rid, string column) {
			int offset = GetCellOffset(table, rid, column, out int size);
			uint value = 0;
			for (int i = 0; i < size; i++)
				value |= (uint)image[offset + i] << (i * 8);
			return value;
		}

		/// <summary>
		/// Writes a column of a table row. Throws if the value doesn't fit in the column.
		/// </summary>
		/// <param name="table">Table</param>
		/// <param name="rid">Row ID, the first row is 1</param>
		/// <param name="column">Column name as in ECMA-335, eg. <c>Extends</c></param>
		/// <param name="value">New value. For a coded token column it's the encoded value, see <see cref="CodedToken"/></param>
		public MetadataPatcher SetCell(Table table, uint rid, string column, uint value) {
			int offset = GetCellOffset(table, rid, column, out int size);
			if (size < 4 && (value >> (size * 8)) != 0)
				throw new ArgumentOutOfRangeException(nameof(value), $"0x{value:X} doesn't fit in {table}.{column}, which is {size} byte(s) in this file");
			for (int i = 0; i < size; i++)
				image[offset + i] = (byte)(value >> (i * 8));
			return this;
		}

		/// <summary>
		/// Returns the rid of the first row whose string column (eg. <c>Name</c>) has a certain value.
		/// Throws if there's no such row.
		/// </summary>
		/// <param name="table">Table</param>
		/// <param name="column">Name of a column that is an offset in the <c>#Strings</c> heap</param>
		/// <param name="value">The string to find</param>
		public uint FindRow(Table table, string column, string value) {
			var mdTable = GetTable(table);
			for (uint rid = 1; rid <= mdTable.Rows; rid++) {
				var offset = GetCell(table, rid, column);
				if (metadata.StringsStream.ReadNoNull(offset) == value)
					return rid;
			}
			throw new ArgumentException($"No {table} row has {column} == \"{value}\"");
		}

		/// <summary>
		/// Sorts the rows of a table by a column (rows with the same value stay in their order). Some tables
		/// must be sorted, eg. the Constant table by its parent: a reader doesn't find all rows if a test
		/// changed that column and the table is no longer sorted.
		/// </summary>
		/// <param name="table">Table</param>
		/// <param name="column">Name of the column to sort by</param>
		public MetadataPatcher SortRows(Table table, string column) {
			var columns = GetColumnNames(table);
			int sortColumn = columns.IndexOf(column);
			var rows = Enumerable.Range(1, (int)GetRowCount(table))
				.Select(rid => columns.Select(a => GetCell(table, (uint)rid, a)).ToArray())
				.OrderBy(row => row[sortColumn]).ToArray();
			for (int i = 0; i < rows.Length; i++) {
				for (int j = 0; j < columns.Count; j++)
					SetCell(table, (uint)i + 1, columns[j], rows[i][j]);
			}
			return this;
		}

		/// <summary>
		/// Overwrites bytes of the file
		/// </summary>
		/// <param name="fileOffset">Offset in the file of the first byte</param>
		/// <param name="bytes">New bytes</param>
		public MetadataPatcher SetBytes(long fileOffset, params byte[] bytes) {
			if (fileOffset < 0 || fileOffset + bytes.Length > image.Length)
				throw new ArgumentOutOfRangeException(nameof(fileOffset));
			Array.Copy(bytes, 0, image, fileOffset, bytes.Length);
			return this;
		}

		/// <summary>
		/// Renames a metadata stream, eg. <c>#~</c> to <c>#-</c>. The new name must have the same length since
		/// a longer or shorter name could change the size of the stream header.
		/// </summary>
		/// <param name="name">Current name of the stream. If several streams have this name, the first one is renamed.</param>
		/// <param name="newName">New name</param>
		public MetadataPatcher RenameStream(string name, string newName) {
			if (Encoding.ASCII.GetByteCount(newName) != Encoding.ASCII.GetByteCount(name))
				throw new ArgumentException("The new name must have the same length as the current name");
			foreach (var header in metadata.MetadataHeader.StreamHeaders) {
				if (header.Name != name)
					continue;
				// A stream header is: uint32 offset, uint32 size, zero terminated name
				const int nameOffsetInHeader = 8;
				return SetBytes((long)header.StartOffset + nameOffsetInHeader, Encoding.ASCII.GetBytes(newName));
			}
			throw new ArgumentException($"There's no metadata stream called {name}");
		}

		/// <summary>
		/// Changes the size of a metadata stream in its stream header. The stream's data isn't changed or moved.
		/// </summary>
		/// <param name="name">Name of the stream, eg. <c>#GUID</c>. If several streams have this name, the first one is changed.</param>
		/// <param name="size">New size in bytes</param>
		public MetadataPatcher SetStreamSize(string name, uint size) {
			foreach (var header in metadata.MetadataHeader.StreamHeaders) {
				if (header.Name != name)
					continue;
				// A stream header is: uint32 offset, uint32 size, zero terminated name
				const int sizeOffsetInHeader = 4;
				return SetBytes((long)header.StartOffset + sizeOffsetInHeader, (byte)size, (byte)(size >> 8), (byte)(size >> 16), (byte)(size >> 24));
			}
			throw new ArgumentException($"There's no metadata stream called {name}");
		}

		/// <summary>
		/// Returns the offset in the file of a metadata stream's data
		/// </summary>
		/// <param name="name">Name of the stream, eg. <c>#Blob</c>. If several streams have this name, it's the first one.</param>
		public long GetStreamFileOffset(string name) {
			foreach (var header in metadata.MetadataHeader.StreamHeaders) {
				if (header.Name == name)
					return (long)metadata.MetadataHeader.StartOffset + header.Offset;
			}
			throw new ArgumentException($"There's no metadata stream called {name}");
		}

		/// <summary>
		/// Changes the number of rows of a table in the header of the tables stream. The rows aren't changed
		/// or moved. The table must have rows.
		/// </summary>
		/// <param name="table">The table</param>
		/// <param name="rowCount">New number of rows</param>
		public MetadataPatcher SetRowCount(Table table, uint rowCount) {
			var tablesStream = metadata.TablesStream;
			if (GetTable(table).Rows == 0)
				throw new ArgumentException($"{table} has no rows, so the header has no row count for it");
			// The header is 24 bytes (the last 16 are two bit masks: the tables that have rows, the sorted
			// tables), followed by a uint32 row count for every table that has rows
			const int rowCountsOffset = 24;
			int index = 0;
			for (int i = 0; i < (int)table; i++) {
				if ((tablesStream.ValidMask & (1UL << i)) != 0)
					index++;
			}
			long offset = (long)tablesStream.StartOffset + rowCountsOffset + index * 4;
			return SetBytes(offset, (byte)rowCount, (byte)(rowCount >> 8), (byte)(rowCount >> 16), (byte)(rowCount >> 24));
		}

		MDTable GetTable(Table table) =>
			metadata.TablesStream.Get(table) ?? throw new ArgumentException($"Unknown table {table}");

		int GetCellOffset(Table table, uint rid, string column, out int size) {
			var mdTable = GetTable(table);
			if (mdTable.IsInvalidRID(rid))
				throw new ArgumentOutOfRangeException(nameof(rid), $"{table} has {mdTable.Rows} row(s), rid {rid} doesn't exist");
			foreach (var columnInfo in mdTable.Columns) {
				if (columnInfo.Name != column)
					continue;
				size = columnInfo.Size;
				return checked((int)((long)mdTable.StartOffset + (rid - 1) * mdTable.RowSize + columnInfo.Offset));
			}
			throw new ArgumentException($"{table} has no column called {column}");
		}
	}
}
