using System;

namespace dot10.PE {
	/// <summary>
	/// Stores an RVA (relative virtual address)
	/// </summary>
	struct RVA : IEquatable<RVA>, IComparable<RVA> {
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
		public static RVA operator +(RVA left, int val) {
			return new RVA(left.val + (uint)val);
		}

		/// <summary>Overloaded operator</summary>
		public static RVA operator +(RVA left, uint val) {
			return new RVA(left.val + val);
		}

		/// <summary>Overloaded operator</summary>
		public static RVA operator -(RVA left, int val) {
			return new RVA(left.val - (uint)val);
		}

		/// <summary>Overloaded operator</summary>
		public static RVA operator -(RVA left, uint val) {
			return new RVA(left.val - val);
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
