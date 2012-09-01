using System;

namespace dot10.dotNET {
	/// <summary>
	/// MetaData token
	/// </summary>
	struct MDToken : IEquatable<MDToken>, IComparable<MDToken> {
		uint token;

		/// <summary>
		/// Returns the table type
		/// </summary>
		public Table Table {
			get { return (Table)(token >> 24); }
		}

		/// <summary>
		/// Returns the row id
		/// </summary>
		public uint Rid {
			get { return token & 0x00FFFFFF; }
		}

		/// <summary>
		/// Returns the raw token
		/// </summary>
		public uint Raw {
			get { return token; }
		}

		/// <summary>
		/// Returns true if it's a null token
		/// </summary>
		public bool IsNull {
			get { return Rid == 0; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="token">Raw token</param>
		public MDToken(uint token) {
			this.token = token;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="token">Raw token</param>
		public MDToken(int token)
			: this((uint)token) {
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(MDToken left, MDToken right) {
			return left.CompareTo(right) == 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(MDToken left, MDToken right) {
			return left.CompareTo(right) != 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <(MDToken left, MDToken right) {
			return left.CompareTo(right) < 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >(MDToken left, MDToken right) {
			return left.CompareTo(right) > 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <=(MDToken left, MDToken right) {
			return left.CompareTo(right) <= 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >=(MDToken left, MDToken right) {
			return left.CompareTo(right) >= 0;
		}

		/// <inheritdoc/>
		public int CompareTo(MDToken other) {
			return token.CompareTo(other.token);
		}

		/// <inheritdoc/>
		public bool Equals(MDToken other) {
			return CompareTo(other) == 0;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if (!(obj is MDToken))
				return false;
			return Equals((MDToken)obj);
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return (int)token;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("{0} {1:X6}", Table.ToString(), Rid);
		}
	}
}
