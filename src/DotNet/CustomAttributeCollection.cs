// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
using dnlib.Utils;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// Stores <see cref="CustomAttribute"/>s
	/// </summary>
	public class CustomAttributeCollection : LazyList<CustomAttribute> {
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
		public CustomAttributeCollection(int length, object context, MFunc<object, uint, CustomAttribute> readOriginalValue)
			: base(length, context, readOriginalValue) {
		}

		/// <summary>
		/// Checks whether a custom attribute is present
		/// </summary>
		/// <param name="fullName">Full name of custom attribute type</param>
		/// <returns><c>true</c> if the custom attribute type is present, <c>false</c> otherwise</returns>
		public bool IsDefined(string fullName) {
			return Find(fullName) != null;
		}

		/// <summary>
		/// Removes all custom attributes of a certain type
		/// </summary>
		/// <param name="fullName">Full name of custom attribute type that should be removed</param>
		public void RemoveAll(string fullName) {
			this.IterateAllReverse((tsList, index, value) => {
				if (value.TypeFullName == fullName)
					RemoveAt_NoLock(index);
			});
		}

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="fullName">Full name of custom attribute type</param>
		/// <returns>A <see cref="CustomAttribute"/> or <c>null</c> if it wasn't found</returns>
		public CustomAttribute Find(string fullName) {
			foreach (var ca in this.GetSafeEnumerable()) {
				if (ca != null && ca.TypeFullName == fullName)
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
			foreach (var ca in this.GetSafeEnumerable()) {
				if (ca != null && ca.TypeFullName == fullName)
					yield return ca;
			}
		}

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attrType">Custom attribute type</param>
		/// <returns>The first <see cref="CustomAttribute"/> found or <c>null</c> if none found</returns>
		public CustomAttribute Find(IType attrType) {
			return Find(attrType, 0);
		}

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="attrType">Custom attribute type</param>
		/// <param name="options">Attribute type comparison flags</param>
		/// <returns>The first <see cref="CustomAttribute"/> found or <c>null</c> if none found</returns>
		public CustomAttribute Find(IType attrType, SigComparerOptions options) {
			var comparer = new SigComparer(options);
			foreach (var ca in this.GetSafeEnumerable()) {
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
		public IEnumerable<CustomAttribute> FindAll(IType attrType) {
			return FindAll(attrType, 0);
		}

		/// <summary>
		/// Finds all custom attributes of a certain type
		/// </summary>
		/// <param name="attrType">Custom attribute type</param>
		/// <param name="options">Attribute type comparison flags</param>
		/// <returns>All <see cref="CustomAttribute"/>s of the requested type</returns>
		public IEnumerable<CustomAttribute> FindAll(IType attrType, SigComparerOptions options) {
			var comparer = new SigComparer(options);
			foreach (var ca in this.GetSafeEnumerable()) {
				if (comparer.Equals(ca.AttributeType, attrType))
					yield return ca;
			}
		}
	}
}
