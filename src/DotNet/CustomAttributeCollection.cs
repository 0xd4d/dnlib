using System.Collections.Generic;

namespace dot10.DotNet {
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
		internal CustomAttributeCollection(int length, object context, MFunc<object, uint, CustomAttribute> readOriginalValue)
			: base(length, context, readOriginalValue) {
		}

		/// <summary>
		/// Finds a custom attribute
		/// </summary>
		/// <param name="fullName">Full name of custom attribute type</param>
		/// <returns>A <see cref="CustomAttribute"/> or <c>null</c> if it wasn't found</returns>
		public CustomAttribute Find(string fullName) {
			foreach (var ca in this) {
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
			foreach (var ca in this) {
				if (ca != null && ca.TypeFullName == fullName)
					yield return ca;
			}
		}
	}
}
