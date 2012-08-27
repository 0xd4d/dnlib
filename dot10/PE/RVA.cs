using System;

namespace dot10.PE {
	/// <summary>
	/// Stores an RVA (relative virtual address)
	/// </summary>
	public struct RVA : IEquatable<RVA>, IComparable<RVA> {
		/// <summary>
		/// The zero RVA
		/// </summary>
		public static readonly RVA Zero = new RVA(0);

		readonly uint val;

		/// <summary>
		/// The RVA as a UInt32
		/// </summary>
		public uint Value {
			get { return val; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="val">RVA value</param>
		public RVA(uint val) {
			this.val = val;
		}

		/// <inheritdoc/>
		public int CompareTo(RVA other) {
			return val.CompareTo(other.val);
		}

		/// <summary>Overloaded operator</summary>
		public static RVA operator +(RVA left, int right) {
			return new RVA(left.val + (uint)right);
		}

		/// <summary>Overloaded operator</summary>
		public static RVA operator +(RVA left, uint right) {
			return new RVA(left.val + right);
		}

		/// <summary>Overloaded operator</summary>
		public static RVA operator +(int left, RVA right) {
			return new RVA((uint)left + right.val);
		}

		/// <summary>Overloaded operator</summary>
		public static RVA operator +(uint left, RVA right) {
			return new RVA(left + right.val);
		}

		/// <summary>Overloaded operator</summary>
		public static RVA operator -(RVA left, int right) {
			return new RVA(left.val - (uint)right);
		}

		/// <summary>Overloaded operator</summary>
		public static RVA operator -(RVA left, uint right) {
			return new RVA(left.val - right);
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <(RVA left, RVA right) {
			return left.CompareTo(right) < 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >(RVA left, RVA right) {
			return left.CompareTo(right) > 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <=(RVA left, RVA right) {
			return left.CompareTo(right) <= 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >=(RVA left, RVA right) {
			return left.CompareTo(right) >= 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(RVA left, RVA right) {
			return left.CompareTo(right) == 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(RVA left, RVA right) {
			return left.CompareTo(right) != 0;
		}

		/// <inheritdoc/>
		public bool Equals(RVA other) {
			return CompareTo(other) == 0;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if (!(obj is RVA))
				return false;
			return Equals((RVA)obj);
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return (int)val;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("{0:X8}h", val);
		}
	}
}
