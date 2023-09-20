// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using dnlib.Utils;
using System;

namespace dnlib.DotNet {
	/// <summary>
	/// Stores <see cref="CustomAttribute"/>s
	/// </summary>
	public class CustomAttributeCollection : LazyList<CustomAttribute, object> {
		/// <summary>
		/// Default constructor
		/// </summary>
		public CustomAttributeCollection() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="context">Context passed to <paramref name="readOriginalValue"/></param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		public CustomAttributeCollection(int length, object context, Func<object, int, CustomAttribute> readOriginalValue)
			: base(length, context, readOriginalValue) {
		}

		/// <summary>
		/// Checks whether a custom attribute is present
		/// </summary>
		/// <param name="fullName">Full name of custom attribute type</param>
		/// <returns><c>true</c> if the custom attribute type is present, <c>false</c> otherwise</returns>
		public bool IsDefined(string fullName) => Find(fullName) is not null;

		/// <summary>
		/// Removes all custom attributes of a certain type
		/// </summary>
		/// <param name="fullName">Full name of custom attribute type that should be removed</param>
		public void RemoveAll(string fullName) {
			if (fullName == null)
				return;

			for (int i = Count - 1; i >= 0; i--) {
				var ca = this[i];
				if (ca is not null && fullName.EndsWith(ca.TypeName, StringComparison.Ordinal) && ca.TypeFullName == fullName)
					RemoveAt(i);
			}
		}

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="fullName">Full name of custom attribute type</param>
		/// <returns>A <see cref="CustomAttribute"/> or <c>null</c> if it wasn't found</returns>
		public CustomAttribute Find(string fullName) {
			if (fullName == null)
				return null;

			foreach (var ca in this) {
				if (ca is not null && fullName.EndsWith(ca.TypeName, StringComparison.Ordinal) && ca.TypeFullName == fullName)
					return ca;
			}

			return null;
		}

		/// <summary>
		/// Finds all custom attributes of a certain type
		/// </summary>
		/// <param name="fullName">Full name of custom attribute type</param>
		/// <returns>All <see cref="CustomAttribute"/>s of the requested type</returns>
		public IEnumerable<CustomAttribute> FindAll(string fullName) {
			if (fullName == null)
				yield break;

			foreach (var ca in this) {
				if (ca is not null && fullName.EndsWith(ca.TypeName, StringComparison.Ordinal) && ca.TypeFullName == fullName)
					yield return ca;
			}
		}

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attrType">Custom attribute type</param>
		/// <returns>The first <see cref="CustomAttribute"/> found or <c>null</c> if none found</returns>
		public CustomAttribute Find(IType attrType) => Find(attrType, 0);

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attrType">Custom attribute type</param>
		/// <param name="options">Attribute type comparison flags</param>
		/// <returns>The first <see cref="CustomAttribute"/> found or <c>null</c> if none found</returns>
		public CustomAttribute Find(IType attrType, SigComparerOptions options) {
			var comparer = new SigComparer(options);
			foreach (var ca in this) {
				if (comparer.Equals(ca.AttributeType, attrType))
					return ca;
			}
			return null;
		}

		/// <summary>
		/// Finds all custom attributes of a certain type
		/// </summary>
		/// <param name="attrType">Custom attribute type</param>
		/// <returns>All <see cref="CustomAttribute"/>s of the requested type</returns>
		public IEnumerable<CustomAttribute> FindAll(IType attrType) => FindAll(attrType, 0);

		/// <summary>
		/// Finds all custom attributes of a certain type
		/// </summary>
		/// <param name="attrType">Custom attribute type</param>
		/// <param name="options">Attribute type comparison flags</param>
		/// <returns>All <see cref="CustomAttribute"/>s of the requested type</returns>
		public IEnumerable<CustomAttribute> FindAll(IType attrType, SigComparerOptions options) {
			var comparer = new SigComparer(options);
			foreach (var ca in this) {
				if (comparer.Equals(ca.AttributeType, attrType))
					yield return ca;
			}
		}
	}
}
