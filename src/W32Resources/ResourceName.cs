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

namespace dnlib.W32Resources {
	/// <summary>
	/// A Win32 resource name. It can be either an integer or a string.
	/// </summary>
	public struct ResourceName : IComparable<ResourceName>, IEquatable<ResourceName> {
		readonly int id;
		readonly string name;

		/// <summary>
		/// <c>true</c> if <see cref="Id"/> is valid
		/// </summary>
		public bool HasId {
			get { return name == null; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="Name"/> is valid
		/// </summary>
		public bool HasName {
			get { return name != null; }
		}

		/// <summary>
		/// The ID. It's only valid if <see cref="HasId"/> is <c>true</c>
		/// </summary>
		public int Id {
			get { return id; }
		}

		/// <summary>
		/// The name. It's only valid if <see cref="HasName"/> is <c>true</c>
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="id">ID</param>
		public ResourceName(int id) {
			this.id = id;
			this.name = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public ResourceName(string name) {
			this.id = 0;
			this.name = name;
		}

		/// <summary>Converts input to a <see cref="ResourceName"/></summary>
		public static implicit operator ResourceName(int id) {
			return new ResourceName(id);
		}

		/// <summary>Converts input to a <see cref="ResourceName"/></summary>
		public static implicit operator ResourceName(string name) {
			return new ResourceName(name);
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <(ResourceName left, ResourceName right) {
			return left.CompareTo(right) < 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <=(ResourceName left, ResourceName right) {
			return left.CompareTo(right) <= 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >(ResourceName left, ResourceName right) {
			return left.CompareTo(right) > 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >=(ResourceName left, ResourceName right) {
			return left.CompareTo(right) >= 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(ResourceName left, ResourceName right) {
			return left.Equals(right);
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(ResourceName left, ResourceName right) {
			return !left.Equals(right);
		}

		/// <inheritdoc/>
		public int CompareTo(ResourceName other) {
			if (HasId != other.HasId) {
				// Sort names before ids
				return HasName ? -1 : 1;
			}
			if (HasId)
				return id.CompareTo(other.id);
			else
				return name.ToUpperInvariant().CompareTo(other.name.ToUpperInvariant());
		}

		/// <inheritdoc/>
		public bool Equals(ResourceName other) {
			return CompareTo(other) == 0;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if (!(obj is ResourceName))
				return false;
			return Equals((ResourceName)obj);
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			if (HasId)
				return id;
			return name.GetHashCode();
		}

		/// <inheritdoc/>
		public override string ToString() {
			return HasId ? id.ToString() : name;
		}
	}
}
