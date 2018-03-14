// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;

namespace dnlib.Utils {
	/// <summary>
	/// Interface to access a lazily initialized list
	/// </summary>
	/// <typeparam name="TValue">Type to store in list</typeparam>
	interface ILazyList<TValue> : IList<TValue> {
	}
}
