// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	/// <summary>
	/// An address in the image
	/// </summary>
	[DebuggerDisplay("{Section}:{Offset}")]
	struct PdbAddress : IEquatable<PdbAddress>, IComparable<PdbAddress> {
		/// <summary>
		/// Section
		/// </summary>
		public readonly ushort Section;

		/// <summary>
		/// Offset in <see cref="Section"/>
		/// </summary>
		public readonly uint Offset;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="section">Section</param>
		/// <param name="offset">Offset in <paramref name="section"/></param>
		public PdbAddress(ushort section, int offset) {
			this.Section = section;
			this.Offset = (uint)offset;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="section">Section</param>
		/// <param name="offset">Offset in <paramref name="section"/></param>
		public PdbAddress(ushort section, uint offset) {
			this.Section = section;
			this.Offset = offset;
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="a"/> is less than or equal to <paramref name="b"/>
		/// </summary>
		/// <param name="a">First <see cref="PdbAddress"/></param>
		/// <param name="b">Second <see cref="PdbAddress"/></param>
		/// <returns></returns>
		public static bool operator <=(PdbAddress a, PdbAddress b) {
			return a.CompareTo(b) <= 0;
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="a"/> is less than <paramref name="b"/>
		/// </summary>
		/// <param name="a">First <see cref="PdbAddress"/></param>
		/// <param name="b">Second <see cref="PdbAddress"/></param>
		/// <returns></returns>
		public static bool operator <(PdbAddress a, PdbAddress b) {
			return a.CompareTo(b) < 0;
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="a"/> is greater than or equal to <paramref name="b"/>
		/// </summary>
		/// <param name="a">First <see cref="PdbAddress"/></param>
		/// <param name="b">Second <see cref="PdbAddress"/></param>
		/// <returns></returns>
		public static bool operator >=(PdbAddress a, PdbAddress b) {
			return a.CompareTo(b) >= 0;
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="a"/> is greater than <paramref name="b"/>
		/// </summary>
		/// <param name="a">First <see cref="PdbAddress"/></param>
		/// <param name="b">Second <see cref="PdbAddress"/></param>
		/// <returns></returns>
		public static bool operator >(PdbAddress a, PdbAddress b) {
			return a.CompareTo(b) > 0;
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="a"/> is equal to <paramref name="b"/>
		/// </summary>
		/// <param name="a">First <see cref="PdbAddress"/></param>
		/// <param name="b">Second <see cref="PdbAddress"/></param>
		/// <returns></returns>
		public static bool operator ==(PdbAddress a, PdbAddress b) {
			return a.Equals(b);
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="a"/> is not equal to <paramref name="b"/>
		/// </summary>
		/// <param name="a">First <see cref="PdbAddress"/></param>
		/// <param name="b">Second <see cref="PdbAddress"/></param>
		/// <returns></returns>
		public static bool operator !=(PdbAddress a, PdbAddress b) {
			return !a.Equals(b);
		}

		/// <summary>
		/// Compares this instance with <paramref name="other"/> and returns less than 0 if it's
		/// less than <paramref name="other"/>, 0 if it's equal to <paramref name="other"/> and
		/// greater than 0 if it's greater than <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public int CompareTo(PdbAddress other) {
			if (Section != other.Section)
				return Section.CompareTo(other.Section);
			return Offset.CompareTo(other.Offset);
		}

		/// <summary>
		/// Compares this to another instance
		/// </summary>
		/// <param name="other">The other one</param>
		/// <returns><c>true</c> if they're equal</returns>
		public bool Equals(PdbAddress other) {
			return Section == other.Section &&
					Offset == other.Offset;
		}

		/// <summary>
		/// Compares this to another instance
		/// </summary>
		/// <param name="obj">The other one</param>
		/// <returns><c>true</c> if they're equal</returns>
		public override bool Equals(object obj) {
			if (!(obj is PdbAddress))
				return false;
			return Equals((PdbAddress)obj);
		}

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns>Hash code</returns>
		public override int GetHashCode() {
			return (Section << 16) ^ (int)Offset;
		}

		/// <summary>
		/// ToString() override
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return string.Format("{0:X4}:{1:X8}", Section, Offset);
		}

		/// <summary>
		/// Reads a 32-bit offset followed by a 16-bit section and creates a new <see cref="PdbAddress"/>
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <returns></returns>
		public static PdbAddress ReadAddress(IBinaryReader reader) {
			uint offs = reader.ReadUInt32();
			return new PdbAddress(reader.ReadUInt16(), offs);
		}
	}
}
