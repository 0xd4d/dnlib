using System;
using System.Diagnostics;

namespace dot10.IO {
	/// <summary>
	/// Stores a file offset
	/// </summary>
	[DebuggerDisplay("{val}")]
	public struct FileOffset : IEquatable<FileOffset>, IComparable<FileOffset> {
		/// <summary>
		/// The zero file offset
		/// </summary>
		public static readonly FileOffset Zero = new FileOffset(0);

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
		public static FileOffset operator +(FileOffset left, int right) {
			return new FileOffset(left.val + right);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator +(FileOffset left, uint right) {
			return new FileOffset(left.val + right);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator +(FileOffset left, long right) {
			return new FileOffset(left.val + right);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator +(FileOffset left, ulong right) {
			return new FileOffset(left.val + (long)right);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator +(int left, FileOffset right) {
			return new FileOffset(left + right.val);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator +(uint left, FileOffset right) {
			return new FileOffset(left + right.val);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator +(long left, FileOffset right) {
			return new FileOffset(left + right.val);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator +(ulong left, FileOffset right) {
			return new FileOffset((long)left + right.val);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator -(FileOffset left, int right) {
			return new FileOffset(left.val - right);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator -(FileOffset left, uint right) {
			return new FileOffset(left.val - right);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator -(FileOffset left, long right) {
			return new FileOffset(left.val - right);
		}

		/// <summary>Overloaded operator</summary>
		public static FileOffset operator -(FileOffset left, ulong right) {
			return new FileOffset(left.val - (long)right);
		}

		/// <summary>Overloaded operator</summary>
		public static long operator -(FileOffset left, FileOffset right) {
			return left.val - right.val;
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
	}
}
