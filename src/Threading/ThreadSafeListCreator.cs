// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.Threading {
	/// <summary>
	/// Creates thread-safe lists
	/// </summary>
	public static class ThreadSafeListCreator {
		/// <summary>
		/// Creates a thread safe <see cref="IList{T}"/>
		/// </summary>
		/// <typeparam name="T">List type</typeparam>
		/// <returns>A new thread-safe list instance</returns>
		public static ThreadSafe.IList<T> Create<T>() {
			var list = new List<T>();
#if THREAD_SAFE
			return new ThreadSafeListWrapper<T>(list);
#else
			return list;
#endif
		}

		/// <summary>
		/// Creates a thread safe <see cref="IList{T}"/>
		/// </summary>
		/// <typeparam name="T">List type</typeparam>
		/// <param name="value">Value to add to the list</param>
		/// <returns>A new thread-safe list instance</returns>
		public static ThreadSafe.IList<T> Create<T>(T value) {
			var list = new List<T>() { value };
#if THREAD_SAFE
			return new ThreadSafeListWrapper<T>(list);
#else
			return list;
#endif
		}

		/// <summary>
		/// Creates a thread safe <see cref="IList{T}"/>
		/// </summary>
		/// <typeparam name="T">List type</typeparam>
		/// <param name="value1">Value #1 to add to the list</param>
		/// <param name="value2">Value #2 to add to the list</param>
		/// <returns>A new thread-safe list instance</returns>
		public static ThreadSafe.IList<T> Create<T>(T value1, T value2) {
			var list = new List<T>() { value1, value2 };
#if THREAD_SAFE
			return new ThreadSafeListWrapper<T>(list);
#else
			return list;
#endif
		}

		/// <summary>
		/// Creates a thread safe <see cref="IList{T}"/>
		/// </summary>
		/// <typeparam name="T">List type</typeparam>
		/// <param name="value1">Value #1 to add to the list</param>
		/// <param name="value2">Value #2 to add to the list</param>
		/// <param name="value3">Value #3 to add to the list</param>
		/// <returns>A new thread-safe list instance</returns>
		public static ThreadSafe.IList<T> Create<T>(T value1, T value2, T value3) {
			var list = new List<T>() { value1, value2, value3 };
#if THREAD_SAFE
			return new ThreadSafeListWrapper<T>(list);
#else
			return list;
#endif
		}

		/// <summary>
		/// Creates a thread safe <see cref="IList{T}"/>
		/// </summary>
		/// <typeparam name="T">List type</typeparam>
		/// <param name="args">Values to add to the list</param>
		/// <returns>A new thread-safe list instance</returns>
		public static ThreadSafe.IList<T> Create<T>(params T[] args) {
			var list = new List<T>(args);
#if THREAD_SAFE
			return new ThreadSafeListWrapper<T>(list);
#else
			return list;
#endif
		}

		/// <summary>
		/// Creates a thread safe <see cref="IList{T}"/>
		/// </summary>
		/// <typeparam name="T">List type</typeparam>
		/// <param name="capacity">List capacity</param>
		/// <returns>A new thread-safe list instance</returns>
		public static ThreadSafe.IList<T> Create<T>(int capacity) {
			var list = new List<T>(capacity);
#if THREAD_SAFE
			return new ThreadSafeListWrapper<T>(list);
#else
			return list;
#endif
		}

		/// <summary>
		/// Creates a thread safe <see cref="IList{T}"/>
		/// </summary>
		/// <typeparam name="T">List type</typeparam>
		/// <param name="collection">Values to copy to the new list</param>
		/// <returns>A new thread-safe list instance</returns>
		public static ThreadSafe.IList<T> Create<T>(IEnumerable<T> collection) {
			var list = new List<T>(collection);
#if THREAD_SAFE
			return new ThreadSafeListWrapper<T>(list);
#else
			return list;
#endif
		}

		/// <summary>
		/// Makes a list thread-safe by using a thread-safe wrapper list
		/// </summary>
		/// <typeparam name="T">List type</typeparam>
		/// <param name="list">The list that should be made thread-safe</param>
		/// <returns>A thread-safe list using <paramref name="list"/> as the underlying list</returns>
		public static ThreadSafe.IList<T> MakeThreadSafe<T>(IList<T> list) {
#if THREAD_SAFE
			if (list == null)
				return null;
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				return tsList;
			return new ThreadSafeListWrapper<T>(list);
#else
			return list;
#endif
		}
	}
}
