// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;

namespace dnlib.Utils {
	/// <summary>
	/// Interface to access a lazily initialized list
	/// </summary>
	/// <typeparam name="TValue">Type to store in list</typeparam>
	interface ILazyList<TValue> : IList<TValue> {
	}

	public static partial class Extensions {
		/// <summary>
		/// Disposes all initialized elements
		/// </summary>
		/// <typeparam name="TValue">Element type</typeparam>
		/// <param name="list">this</param>
		internal static void DisposeAll<TValue>(this LazyList<TValue> list) where TValue : class, IDisposable {
			for (int i = 0; i < list.Count; i++) {
				if (list.IsInitialized(i))
					list[i]?.Dispose();
			}
		}
	}
}
