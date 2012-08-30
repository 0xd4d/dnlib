namespace dot10.dotNET {
	/// <summary>
	/// Info about one column in a MD table
	/// </summary>
	public class ColumnInfo {
		byte offset;
		ColumnSize columnSize;
		byte size;
		string name;

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
		/// <param name="name">The column name</param>
		/// <param name="columnSize">Column size</param>
		public ColumnInfo(string name, ColumnSize columnSize) {
			this.name = name;
			this.columnSize = columnSize;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("{0} {1} {2}", offset, size, name);
		}
	}
}
