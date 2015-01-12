// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.Utils {
	/// <summary>
	/// Interface to access a lazily initialized list
	/// </summary>
	/// <typeparam name="TValue">Type to store in list</typeparam>
	public interface ILazyList<TValue> : ThreadSafe.IList<TValue> {
		/// <summary>
		/// Checks whether an element at <paramref name="index"/> has been initialized.
		/// </summary>
		/// <param name="index">Index of element</param>
		/// <returns><c>true</c> if the element has been initialized, <c>false</c> otherwise</returns>
		bool IsInitialized(int index);

		/// <summary>
		/// Checks whether an element at <paramref name="index"/> has been initialized.
		/// </summary>
		/// <param name="index">Index of element</param>
		/// <returns><c>true</c> if the element has been initialized, <c>false</c> otherwise</returns>
		bool IsInitialized_NoLock(int index);

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
		public static void DisposeAll<TValue>(this ILazyList<TValue> list) where TValue : IDisposable {
			list.ExecuteLocked<TValue, object, object>(null, (tsList, arg) => {
				for (int i = 0; i < list.Count_NoLock(); i++) {
					if (list.IsInitialized_NoLock(i)) {
						var elem = list.Get_NoLock(i);
						if (elem != null)
							elem.Dispose();
					}
				}
				return null;
			});
		}
	}
}
