// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// Methods to load properties to make sure they're initialized
	/// </summary>
	static class MemberMDInitializer {
		/// <summary>
		/// Read every collection element
		/// </summary>
		/// <typeparam name="T">Collection element type</typeparam>
		/// <param name="coll">Collection</param>
		public static void Initialize<T>(IEnumerable<T> coll) {
			if (coll == null)
				return;
			foreach (var c in coll.GetSafeEnumerable()) {
			}
		}

		/// <summary>
		/// Load the object instance
		/// </summary>
		/// <param name="o">The value (ignored)</param>
		public static void Initialize(object o) {
		}
	}
}
