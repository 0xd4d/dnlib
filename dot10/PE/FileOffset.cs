using System;

namespace dot10.PE {
	/// <summary>
	/// Stores a file offset
	/// </summary>
	public struct FileOffset : IEquatable<FileOffset>, IComparable<FileOffset> {
		readonly long val;

		/// <summary>
		/// The file offset as an Int64
		/// </summary>
		public long Value {
			get { return val; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="val">File offset value</param>
		public FileOffset(long val) {
			this.val = val;
		}

		/// <inheritdoc/>
		public int CompareTo(FileOffset other) {
			return val.CompareTo(other.val);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator +(FileOffset left, int val) {
			return new FileOffset(left.val + val);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator +(FileOffset left, uint val) {
			return new FileOffset(left.val + val);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator +(FileOffset left, long val) {
			return new FileOffset(left.val + val);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator +(FileOffset left, ulong val) {
			return new FileOffset(left.val + (long)val);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator -(FileOffset left, int val) {
			return new FileOffset(left.val - val);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator -(FileOffset left, uint val) {
			return new FileOffset(left.val - val);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator -(FileOffset left, long val) {
			return new FileOffset(left.val - val);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator -(FileOffset left, ulong val) {
			return new FileOffset(left.val - (long)val);
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <(FileOffset left, FileOffset right) {
			return left.CompareTo(right) < 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >(FileOffset left, FileOffset right) {
			return left.CompareTo(right) > 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <=(FileOffset left, FileOffset right) {
			return left.CompareTo(right) <= 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >=(FileOffset left, FileOffset right) {
			return left.CompareTo(right) >= 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(FileOffset left, FileOffset right) {
			return left.CompareTo(right) == 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(FileOffset left, FileOffset right) {
			return left.CompareTo(right) != 0;
		}

		/// <inheritdoc/>
		public bool Equals(FileOffset other) {
			return CompareTo(other) == 0;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if (!(obj is FileOffset))
				return false;
			return Equals((FileOffset)obj);
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return (int)val ^ (int)(val >> 32);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("{0:X8}h", (ulong)val);
		}
	}
}
