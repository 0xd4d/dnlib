/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using System.Diagnostics;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// MetaData token
	/// </summary>
	[DebuggerDisplay("{Table} {Rid}")]
	public struct MDToken : IEquatable<MDToken>, IComparable<MDToken> {
		/// <summary>
		/// Mask to get the rid from a raw metadata token
		/// </summary>
		public const uint RID_MASK = 0x00FFFFFF;

		/// <summary>
		/// Max rid value
		/// </summary>
		public const uint RID_MAX = RID_MASK;

		/// <summary>
		/// Number of bits to right shift a raw metadata token to get the table index
		/// </summary>
		public const int TABLE_SHIFT = 24;

		readonly uint token;

		/// <summary>
		/// Returns the table type
		/// </summary>
		public Table Table {
			get { return ToTable(token); }
		}

		/// <summary>
		/// Returns the row id
		/// </summary>
		public uint Rid {
			get { return ToRID(token); }
		}

		/// <summary>
		/// Returns the raw token
		/// </summary>
		public uint Raw {
			get { return token; }
		}

		/// <summary>
		/// Returns <c>true</c> if it's a <c>null</c> token
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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="table">The table type</param>
		/// <param name="rid">Row id</param>
		public MDToken(Table table, uint rid)
			: this(((uint)table << TABLE_SHIFT) | rid) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="table">The table type</param>
		/// <param name="rid">Row id</param>
		public MDToken(Table table, int rid)
			: this(((uint)table << TABLE_SHIFT) | (uint)rid) {
		}

		/// <summary>
		/// Returns the <c>rid</c> (row ID)
		/// </summary>
		/// <param name="token">A raw metadata token</param>
		/// <returns>A <c>rid</c></returns>
		public static uint ToRID(uint token) {
			return token & RID_MASK;
		}

		/// <summary>
		/// Returns the <c>rid</c> (row ID)
		/// </summary>
		/// <param name="token">A raw metadata token</param>
		/// <returns>A <c>rid</c></returns>
		public static uint ToRID(int token) {
			return ToRID((uint)token);
		}

		/// <summary>
		/// Returns the <c>table</c>
		/// </summary>
		/// <param name="token">A raw metadata token</param>
		/// <returns>A metadata table index</returns>
		public static Table ToTable(uint token) {
			return (Table)(token >> TABLE_SHIFT);
		}

		/// <summary>
		/// Returns the <c>table</c>
		/// </summary>
		/// <param name="token">A raw metadata token</param>
		/// <returns>A metadata table index</returns>
		public static Table ToTable(int token) {
			return ToTable((uint)token);
		}

		/// <summary>
		/// Gets the token as a raw 32-bit signed integer
		/// </summary>
		public int ToInt32() {
			return (int)token;
		}

		/// <summary>
		/// Gets the token as a raw 32-bit unsigned integer
		/// </summary>
		public uint ToUInt32() {
			return token;
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
			return string.Format("{0}", token);
		}
	}
}
