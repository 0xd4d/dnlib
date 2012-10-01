using System.Collections.Generic;

namespace dot10.DotNet {
	/// <summary>
	/// Interface to access a lazily initialized list
	/// </summary>
	/// <typeparam name="TValue">Type to store in list</typeparam>
	interface ILazyList<TValue> : IList<TValue> {
		/// <summary>
		/// Checks whether an element at <paramref name="index"/> has been initialized.
		/// </summary>
		/// <param name="index">Index of element</param>
		/// <returns><c>true</c> if the element has been initialized, <c>false</c> otherwise</returns>
		bool IsInitialized(int index);

		/// <summary>
		/// Gets all initialized elements
		/// </summary>
		IEnumerable<TValue> GetInitializedElements();
	}
}
