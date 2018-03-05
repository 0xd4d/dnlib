// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;

namespace dnlib.Utils {
	/// <summary>
	/// Interface to access a lazily initialized list
	/// </summary>
	/// <typeparam name="TValue">Type to store in list</typeparam>
	public interface ILazyList<TValue> : IList<TValue> {
		/// <summary>
		/// Checks whether an element at <paramref name="index"/> has been initialized.
		/// </summary>
		/// <param name="index">Index of element</param>
		/// <returns><c>true</c> if the element has been initialized, <c>false</c> otherwise</returns>
		bool IsInitialized(int index);

		/// <summary>
		/// Gets all initialized elements
		/// </summary>
		/// <param name="clearList"><c>true</c> if the list should be cleared before returning,
		/// <c>false</c> if the list should not cleared.</param>
		List<TValue> GetInitializedElements(bool clearList);
	}

	public static partial class Extensions {
		/// <summary>
		/// Disposes all initialized elements
		/// </summary>
		/// <typeparam name="TValue">Element type</typeparam>
		/// <param name="list">this</param>
		internal static void DisposeAll<TValue>(this ILazyList<TValue> list) where TValue : IDisposable {
			for (int i = 0; i < list.Count; i++) {
				if (list.IsInitialized(i)) {
					var elem = list[i];
					if (elem != null)
						elem.Dispose();
				}
			}
		}
	}
}
