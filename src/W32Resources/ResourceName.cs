// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.W32Resources {
	/// <summary>
	/// A Win32 resource name. It can be either an integer or a string.
	/// </summary>
	public readonly struct ResourceName : IComparable<ResourceName>, IEquatable<ResourceName> {
		readonly int id;
		readonly string name;

		/// <summary>
		/// <c>true</c> if <see cref="Id"/> is valid
		/// </summary>
		public bool HasId => name is null;

		/// <summary>
		/// <c>true</c> if <see cref="Name"/> is valid
		/// </summary>
		public bool HasName => !(name is null);

		/// <summary>
		/// The ID. It's only valid if <see cref="HasId"/> is <c>true</c>
		/// </summary>
		public int Id => id;

		/// <summary>
		/// The name. It's only valid if <see cref="HasName"/> is <c>true</c>
		/// </summary>
		public string Name => name;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="id">ID</param>
		public ResourceName(int id) {
			this.id = id;
			name = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public ResourceName(string name) {
			id = 0;
			this.name = name;
		}

		/// <summary>Converts input to a <see cref="ResourceName"/></summary>
		public static implicit operator ResourceName(int id) => new ResourceName(id);

		/// <summary>Converts input to a <see cref="ResourceName"/></summary>
		public static implicit operator ResourceName(string name) => new ResourceName(name);

		/// <summary>Overloaded operator</summary>
		public static bool operator <(ResourceName left, ResourceName right) => left.CompareTo(right) < 0;

		/// <summary>Overloaded operator</summary>
		public static bool operator <=(ResourceName left, ResourceName right) => left.CompareTo(right) <= 0;

		/// <summary>Overloaded operator</summary>
		public static bool operator >(ResourceName left, ResourceName right) => left.CompareTo(right) > 0;

		/// <summary>Overloaded operator</summary>
		public static bool operator >=(ResourceName left, ResourceName right) => left.CompareTo(right) >= 0;

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(ResourceName left, ResourceName right) => left.Equals(right);

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(ResourceName left, ResourceName right) => !left.Equals(right);

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
		public bool Equals(ResourceName other) => CompareTo(other) == 0;

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
		public override string ToString() => HasId ? id.ToString() : name;
	}
}
