// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.Threading.Collections {
#if THREAD_SAFE
	/// <summary>
	/// Thread-safe <see cref="System.Collections.Generic.IList{T}"/> interface
	/// </summary>
	/// <typeparam name="T">List type</typeparam>
	public interface IList<T> : System.Collections.Generic.IList<T> {
		/// <summary>
		/// Must only be called when the list lock is held. Gets the index of <paramref name="item"/>
		/// </summary>
		/// <param name="item">Item</param>
		/// <returns>Index of <paramref name="item"/> or <c>-1</c> if it's not present in the list</returns>
		/// <seealso cref="ExecuteLocked"/>
		int IndexOf_NoLock(T item);

		/// <summary>
		/// Must only be called when the list lock is held. Inserts <paramref name="item"/> at index
		/// <paramref name="index"/>
		/// </summary>
		/// <param name="index">Index</param>
		/// <param name="item">Item to insert</param>
		/// <seealso cref="ExecuteLocked"/>
		void Insert_NoLock(int index, T item);

		/// <summary>
		/// Must only be called when the list lock is held. Removes the item at index
		/// <paramref name="index"/>
		/// </summary>
		/// <param name="index"></param>
		/// <seealso cref="ExecuteLocked"/>
		void RemoveAt_NoLock(int index);

		/// <summary>
		/// Must only be called when the list lock is held. Returns the value at a specified index.
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns>Value</returns>
		/// <seealso cref="ExecuteLocked"/>
		T Get_NoLock(int index);

		/// <summary>
		/// Must only be called when the list lock is held. Writes to the list at a specified index.
		/// </summary>
		/// <param name="index">Index</param>
		/// <param name="value">Value</param>
		/// <seealso cref="ExecuteLocked"/>
		void Set_NoLock(int index, T value);

		/// <summary>
		/// Must only be called when the list lock is held. Adds a new element to the end of the
		/// list.
		/// </summary>
		/// <param name="item">Item</param>
		/// <seealso cref="ExecuteLocked"/>
		void Add_NoLock(T item);

		/// <summary>
		/// Must only be called when the list lock is held. Clears the list.
		/// </summary>
		/// <seealso cref="ExecuteLocked"/>
		void Clear_NoLock();

		/// <summary>
		/// Must only be called when the list lock is held. Checks whether <paramref name="item"/>
		/// exists in the list.
		/// </summary>
		/// <param name="item">Item</param>
		/// <returns><c>true</c> if <paramref name="item"/> exists in the list, else <c>false</c></returns>
		/// <seealso cref="ExecuteLocked"/>
		bool Contains_NoLock(T item);

		/// <summary>
		/// Must only be called when the list lock is held. Copies the list to an array.
		/// </summary>
		/// <param name="array">Destination array</param>
		/// <param name="arrayIndex">Destination array index</param>
		/// <seealso cref="ExecuteLocked"/>
		void CopyTo_NoLock(T[] array, int arrayIndex);

		/// <summary>
		/// Must only be called when the list lock is held. Returns the size of the list.
		/// </summary>
		/// <seealso cref="ExecuteLocked"/>
		int Count_NoLock { get; }

		/// <summary>
		/// Must only be called when the list lock is held. Returns <c>true</c> if the list is
		/// read-only, <c>false</c> if it's writable.
		/// </summary>
		/// <seealso cref="ExecuteLocked"/>
		bool IsReadOnly_NoLock { get; }

		/// <summary>
		/// Must only be called when the list lock is held. Removes <paramref name="item"/> from the
		/// list.
		/// </summary>
		/// <param name="item">Item</param>
		/// <returns><c>true</c> if <paramref name="item"/> was removed, <c>false</c> if
		/// <paramref name="item"/> was never inserted in the list.</returns>
		/// <seealso cref="ExecuteLocked"/>
		bool Remove_NoLock(T item);

		/// <summary>
		/// Must only be called when the list lock is held. Gets the enumerator.
		/// </summary>
		/// <returns>A new enumerator instance</returns>
		/// <seealso cref="ExecuteLocked"/>
		IEnumerator<T> GetEnumerator_NoLock();

		/// <summary>
		/// Locks the list and then calls <paramref name="handler"/>. <paramref name="handler"/>
		/// must only call <c>*_NoLock()</c> methods. The list is unlocked once this method returns.
		/// </summary>
		/// <typeparam name="TArgType">Argument type</typeparam>
		/// <typeparam name="TRetType">Return type</typeparam>
		/// <param name="arg">Passed to <paramref name="handler"/></param>
		/// <param name="handler">Handler that should execute when the lock is held</param>
		/// <returns>The value <paramref name="handler"/> returns</returns>
		/// <seealso cref="IndexOf_NoLock"/>
		/// <seealso cref="Insert_NoLock"/>
		/// <seealso cref="RemoveAt_NoLock"/>
		/// <seealso cref="Get_NoLock"/>
		/// <seealso cref="Set_NoLock"/>
		/// <seealso cref="Add_NoLock"/>
		/// <seealso cref="Clear_NoLock"/>
		/// <seealso cref="Contains_NoLock"/>
		/// <seealso cref="CopyTo_NoLock"/>
		/// <seealso cref="Count_NoLock"/>
		/// <seealso cref="IsReadOnly_NoLock"/>
		/// <seealso cref="Remove_NoLock"/>
		/// <seealso cref="GetEnumerator_NoLock"/>
		TRetType ExecuteLocked<TArgType, TRetType>(TArgType arg, ExecuteLockedDelegate<T, TArgType, TRetType> handler);
	}
#endif
}

namespace dnlib.Threading {
	/// <summary>
	/// Passed to <c>ExecuteLocked()</c>
	/// </summary>
	/// <typeparam name="T">Type to store in list</typeparam>
	/// <typeparam name="TArgType">Argument type</typeparam>
	/// <typeparam name="TRetType">Return type</typeparam>
	/// <param name="tsList">A thread-safe list</param>
	/// <param name="arg">The argument</param>
	/// <returns>Any value the user wants to return</returns>
	public delegate TRetType ExecuteLockedDelegate<T, TArgType, TRetType>(ThreadSafe.IList<T> tsList, TArgType arg);

#if THREAD_SAFE
	/// <summary>
	/// Called by <see cref="Extensions.Iterate{T}(ThreadSafe.IList{T},int,int,bool,IterateDelegate{T})"/>
	/// </summary>
	/// <typeparam name="T">Type to store in list</typeparam>
	/// <param name="tsList">A thread-safe list</param>
	/// <param name="index">Index of <paramref name="value"/></param>
	/// <param name="value">Value at <paramref name="index"/> in the list</param>
	/// <returns><c>false</c> to break out of the iterator loop and return</returns>
	public delegate bool IterateDelegate<T>(ThreadSafe.IList<T> tsList, int index, T value);

	/// <summary>
	/// Called by <see cref="Extensions.IterateAll{T}(ThreadSafe.IList{T},IterateAllDelegate{T})"/>
	/// and <see cref="Extensions.IterateAllReverse{T}(ThreadSafe.IList{T},IterateAllDelegate{T})"/>
	/// </summary>
	/// <typeparam name="T">Type to store in list</typeparam>
	/// <param name="tsList">A thread-safe list</param>
	/// <param name="index">Index of <paramref name="value"/></param>
	/// <param name="value">Value at <paramref name="index"/> in the list</param>
	public delegate void IterateAllDelegate<T>(ThreadSafe.IList<T> tsList, int index, T value);
#endif

	/// <summary>
	/// Called by <see cref="Extensions.Iterate{T}(IList{T},int,int,bool,ListIterateDelegate{T})"/>
	/// </summary>
	/// <typeparam name="T">Type to store in list</typeparam>
	/// <param name="list">A list</param>
	/// <param name="index">Index of <paramref name="value"/></param>
	/// <param name="value">Value at <paramref name="index"/> in the list</param>
	/// <returns><c>false</c> to break out of the iterator loop and return</returns>
	public delegate bool ListIterateDelegate<T>(IList<T> list, int index, T value);

	/// <summary>
	/// Called by <see cref="Extensions.IterateAll{T}(IList{T},ListIterateAllDelegate{T})"/>
	/// and <see cref="Extensions.IterateAllReverse{T}(IList{T},ListIterateAllDelegate{T})"/>
	/// </summary>
	/// <typeparam name="T">Type to store in list</typeparam>
	/// <param name="list">A list</param>
	/// <param name="index">Index of <paramref name="value"/></param>
	/// <param name="value">Value at <paramref name="index"/> in the list</param>
	/// <returns><c>false</c> to break out of the iterator loop and return</returns>
	public delegate void ListIterateAllDelegate<T>(IList<T> list, int index, T value);

	/// <summary>
	/// Called by <see cref="Extensions.Iterate{T}(IEnumerable{T},EnumerableIterateDelegate{T})"/>
	/// </summary>
	/// <typeparam name="T">Type stored in enumerable</typeparam>
	/// <param name="index">Index of <paramref name="value"/></param>
	/// <param name="value">Value at <paramref name="index"/> in the collection</param>
	/// <returns><c>false</c> to break out of the iterator loop and return</returns>
	public delegate bool EnumerableIterateDelegate<T>(int index, T value);

	/// <summary>
	/// Called by <see cref="Extensions.IterateAll{T}(IEnumerable{T},EnumerableIterateAllDelegate{T})"/>
	/// </summary>
	/// <typeparam name="T">Type stored in enumerable</typeparam>
	/// <param name="index">Index of <paramref name="value"/></param>
	/// <param name="value">Value at <paramref name="index"/> in the collection</param>
	/// <returns><c>false</c> to break out of the iterator loop and return</returns>
	public delegate void EnumerableIterateAllDelegate<T>(int index, T value);

	public static partial class Extensions {
		/// <summary>
		/// Locks the list and then calls <paramref name="handler"/>. <paramref name="handler"/>
		/// must only call <c>*_NoLock()</c> methods. The list is unlocked once this method returns.
		/// </summary>
		/// <typeparam name="T">List type</typeparam>
		/// <typeparam name="TArgType">Argument type</typeparam>
		/// <typeparam name="TRetType">Return type</typeparam>
		/// <param name="tsList">A list</param>
		/// <param name="arg">Passed to <paramref name="handler"/></param>
		/// <param name="handler">Handler that should execute when the lock is held</param>
		/// <returns>The value <paramref name="handler"/> returns</returns>
		public static TRetType ExecuteLocked<T, TArgType, TRetType>(this ThreadSafe.IList<T> tsList, TArgType arg, ExecuteLockedDelegate<T, TArgType, TRetType> handler) {
#if THREAD_SAFE
			return tsList.ExecuteLocked<TArgType, TRetType>(arg, handler);
#else
			return handler(tsList, arg);
#endif
		}

#if THREAD_SAFE
		/// <summary>
		/// Iterates over elements in <paramref name="tsList"/> and calls <paramref name="handler"/>
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="tsList">A thread-safe list</param>
		/// <param name="handler">Called for each element</param>
		/// <param name="startIndex">Start index</param>
		/// <param name="endIndex">End index. <c>-1</c> means <see cref="ThreadSafe.IList{T}.Count_NoLock"/></param>
		/// <param name="reverseOrder"><c>true</c> if we should iterate in the reverse order</param>
		public static void Iterate<T>(this ThreadSafe.IList<T> tsList, int startIndex, int endIndex, bool reverseOrder, IterateDelegate<T> handler) {
			tsList.ExecuteLocked<object, object>(null, (tsList2, arg) => {
				if (reverseOrder) {
					int i = (endIndex < 0 ? tsList2.Count_NoLock : endIndex) - 1;
					for (; i >= startIndex; i--) {
						if (!handler(tsList2, i, tsList2.Get_NoLock(i)))
							break;
					}
				}
				else {
					// Count property can change so check it each time in the loop
					for (int i = startIndex; i < (endIndex < 0 ? tsList2.Count_NoLock : endIndex); i++) {
						if (!handler(tsList2, i, tsList2.Get_NoLock(i)))
							break;
					}
				}
				return null;
			});
		}

		/// <summary>
		/// Iterates over all elements in <paramref name="tsList"/> and calls <paramref name="handler"/>
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="tsList">A thread-safe list</param>
		/// <param name="handler">Called for each element</param>
		public static void Iterate<T>(this ThreadSafe.IList<T> tsList, IterateDelegate<T> handler) {
			tsList.Iterate(0, -1, false, handler);
		}

		/// <summary>
		/// Iterates over all elements in <paramref name="tsList"/> and calls <paramref name="handler"/>
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="tsList">A thread-safe list</param>
		/// <param name="handler">Called for each element</param>
		public static void IterateAll<T>(this ThreadSafe.IList<T> tsList, IterateAllDelegate<T> handler) {
			tsList.Iterate(0, -1, false, (tsList2, index, value) => {
				handler(tsList2, index, value);
				return true;
			});
		}

		/// <summary>
		/// Iterates over all elements in <paramref name="tsList"/> in the reverse order and calls
		/// <paramref name="handler"/>
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="tsList">A thread-safe list</param>
		/// <param name="handler">Called for each element</param>
		public static void IterateReverse<T>(this ThreadSafe.IList<T> tsList, IterateDelegate<T> handler) {
			tsList.Iterate(0, -1, true, handler);
		}

		/// <summary>
		/// Iterates over all elements in <paramref name="tsList"/> in the reverse order and calls
		/// <paramref name="handler"/>
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="tsList">A thread-safe list</param>
		/// <param name="handler">Called for each element</param>
		public static void IterateAllReverse<T>(this ThreadSafe.IList<T> tsList, IterateAllDelegate<T> handler) {
			tsList.Iterate(0, -1, true, (tsList2, index, value) => {
				handler(tsList2, index, value);
				return true;
			});
		}
#endif

		/// <summary>
		/// Iterates over elements in <paramref name="list"/> and calls <paramref name="handler"/>.
		/// If <paramref name="list"/> implements <see cref="ThreadSafe.IList{T}"/>, only thread safe
		/// methods are called.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">A list</param>
		/// <param name="handler">Called for each element</param>
		/// <param name="startIndex">Start index</param>
		/// <param name="endIndex">End index. <c>-1</c> means <c>Count_NoLock</c></param>
		/// <param name="reverseOrder"><c>true</c> if we should iterate in the reverse order</param>
		public static void Iterate<T>(this IList<T> list, int startIndex, int endIndex, bool reverseOrder, ListIterateDelegate<T> handler) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				tsList.Iterate(startIndex, endIndex, reverseOrder, (tsList2, index, value) => handler(tsList2, index, value));
			else {
#endif
				if (reverseOrder) {
					int i = (endIndex < 0 ? list.Count : endIndex) - 1;
					for (; i >= startIndex; i--) {
						if (!handler(list, i, list[i]))
							break;
					}
				}
				else {
					// Count property can change so check it each time in the loop
					for (int i = startIndex; i < (endIndex < 0 ? list.Count : endIndex); i++) {
						if (!handler(list, i, list[i]))
							break;
					}
				}
#if THREAD_SAFE
			}
#endif
		}

		/// <summary>
		/// Iterates over all elements in <paramref name="list"/> and calls
		/// <paramref name="handler"/>. If <paramref name="list"/> implements
		/// <see cref="ThreadSafe.IList{T}"/>, only thread safe methods are called.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">A list</param>
		/// <param name="handler">Called for each element</param>
		public static void Iterate<T>(this IList<T> list, ListIterateDelegate<T> handler) {
			list.Iterate(0, -1, false, handler);
		}

		/// <summary>
		/// Iterates over all elements in <paramref name="list"/> and calls
		/// <paramref name="handler"/>. If <paramref name="list"/> implements
		/// <see cref="ThreadSafe.IList{T}"/>, only thread safe methods are called.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">A list</param>
		/// <param name="handler">Called for each element</param>
		public static void IterateAll<T>(this IList<T> list, ListIterateAllDelegate<T> handler) {
			list.Iterate(0, -1, false, (list2, index, value) => {
				handler(list2, index, value);
				return true;
			});
		}

		/// <summary>
		/// Iterates over all elements in <paramref name="list"/> in the reverse order and calls
		/// <paramref name="handler"/>. If <paramref name="list"/> implements
		/// <see cref="ThreadSafe.IList{T}"/>, only thread safe methods are called.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">A list</param>
		/// <param name="handler">Called for each element</param>
		public static void IterateReverse<T>(this IList<T> list, ListIterateDelegate<T> handler) {
			list.Iterate(0, -1, true, handler);
		}

		/// <summary>
		/// Iterates over all elements in <paramref name="list"/> in the reverse order and calls
		/// <paramref name="handler"/>. If <paramref name="list"/> implements
		/// <see cref="ThreadSafe.IList{T}"/>, only thread safe methods are called.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">A list</param>
		/// <param name="handler">Called for each element</param>
		public static void IterateAllReverse<T>(this IList<T> list, ListIterateAllDelegate<T> handler) {
			list.Iterate(0, -1, true, (list2, index, value) => {
				handler(list2, index, value);
				return true;
			});
		}

		/// <summary>
		/// Iterates over all elements in <paramref name="list"/> and calls
		/// <paramref name="handler"/>. If <paramref name="list"/> implements
		/// <see cref="ThreadSafe.IList{T}"/>, only thread safe methods are called.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">A list</param>
		/// <param name="handler">Called for each element</param>
		public static void Iterate<T>(this IEnumerable<T> list, EnumerableIterateDelegate<T> handler) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				tsList.Iterate((tsList2, index, value) => handler(index, value));
			else {
#endif
				int i = 0;
				foreach (var value in list) {
					if (!handler(i, value))
						break;
					i++;
				}
#if THREAD_SAFE
			}
#endif
		}

		/// <summary>
		/// Iterates over all elements in <paramref name="list"/> and calls
		/// <paramref name="handler"/>. If <paramref name="list"/> implements
		/// <see cref="ThreadSafe.IList{T}"/>, only thread safe methods are called.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">A list</param>
		/// <param name="handler">Called for each element</param>
		public static void IterateAll<T>(this IEnumerable<T> list, EnumerableIterateAllDelegate<T> handler) {
			list.Iterate((index, value) => {
				handler(index, value);
				return true;
			});
		}

		/// <summary>
		/// Reads an element from the list. If <paramref name="list"/> implements
		/// <see cref="ThreadSafe.IList{T}"/>, only thread safe methods are called.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">A list</param>
		/// <param name="index">Index</param>
		/// <param name="value">Updated with value</param>
		/// <returns><c>true</c> if <paramref name="value"/> was updated with the element in the
		/// list or <c>false</c> if <paramref name="index"/> was invalid.</returns>
		public static bool Get<T>(this IList<T> list, int index, out T value) {
#if THREAD_SAFE
			try {
#endif
				if ((uint)index < (uint)list.Count) {
					value = list[index];
					return true;
				}
#if THREAD_SAFE
			}
			catch (IndexOutOfRangeException) {
			}
			catch (ArgumentOutOfRangeException) {
			}
#endif
			value = default(T);
			return false;
		}

		/// <summary>
		/// Reads an element from the list. If <paramref name="list"/> implements
		/// <see cref="ThreadSafe.IList{T}"/>, only thread safe methods are called.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">A list</param>
		/// <param name="index">Index</param>
		/// <param name="defaultValue">Default value if <paramref name="index"/> is invalid</param>
		/// <returns>The value in the list or <paramref name="defaultValue"/> if
		/// <paramref name="index"/> was invalid</returns>
		public static T Get<T>(this IList<T> list, int index, T defaultValue) {
			T value;
			return list.Get(index, out value) ? value : defaultValue;
		}

		/// <summary>
		/// Writes an element to the list. If <paramref name="list"/> implements
		/// <see cref="ThreadSafe.IList{T}"/>, only thread safe methods are called.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">A list</param>
		/// <param name="index">Index</param>
		/// <param name="value">Value</param>
		/// <returns><c>true</c> if <paramref name="value"/> was written to the list or <c>false</c>
		/// if <paramref name="index"/> was invalid.</returns>
		public static bool Set<T>(this IList<T> list, int index, T value) {
#if THREAD_SAFE
			try {
#endif
				if ((uint)index < (uint)list.Count) {
					list[index] = value;
					return true;
				}
#if THREAD_SAFE
			}
			catch (IndexOutOfRangeException) {
			}
			catch (ArgumentOutOfRangeException) {
			}
#endif
			return false;
		}

#if THREAD_SAFE
		/// <summary>
		/// Calls <see cref="ThreadSafe.IList{T}.Count_NoLock"/>
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="tsList">A thread-safe list</param>
		/// <returns>Number of elements in the list</returns>
		public static int Count_NoLock<T>(this ThreadSafe.IList<T> tsList) {
			return tsList.Count_NoLock;
		}

		/// <summary>
		/// Calls <see cref="ThreadSafe.IList{T}.IsReadOnly_NoLock"/>
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="tsList">A thread-safe list</param>
		public static bool IsReadOnly_NoLock<T>(this ThreadSafe.IList<T> tsList) {
			return tsList.IsReadOnly_NoLock;
		}
#endif

		/// <summary>
		/// Calls the thread-safe <c>IndexOf_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="IList{T}.IndexOf"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		/// <param name="item">Item</param>
		/// <returns>Index of <paramref name="item"/></returns>
		public static int IndexOf_NoLock<T>(this IList<T> list, T item) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				return tsList.IndexOf_NoLock(item);
			else
#endif
				return list.IndexOf(item);
		}

		/// <summary>
		/// Calls the thread-safe <c>Insert_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="IList{T}.Insert"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		/// <param name="index">Index</param>
		/// <param name="item">Item</param>
		public static void Insert_NoLock<T>(this IList<T> list, int index, T item) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				tsList.Insert_NoLock(index, item);
			else
#endif
				list.Insert(index, item);
		}

		/// <summary>
		/// Calls the thread-safe <c>RemoveAt_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="IList{T}.RemoveAt"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		/// <param name="index">Index</param>
		public static void RemoveAt_NoLock<T>(this IList<T> list, int index) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				tsList.RemoveAt_NoLock(index);
			else
#endif
				list.RemoveAt(index);
		}

		/// <summary>
		/// Calls the thread-safe <c>Get_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="IList{T}.get_Item"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		/// <param name="index">Index</param>
		/// <returns>Value at index <paramref name="index"/></returns>
		public static T Get_NoLock<T>(this IList<T> list, int index) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				return tsList.Get_NoLock(index);
			else
#endif
				return list[index];
		}

		/// <summary>
		/// Calls the thread-safe <c>Set_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="IList{T}.set_Item"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		/// <param name="index">Index</param>
		/// <param name="value">Value</param>
		public static void Set_NoLock<T>(this IList<T> list, int index, T value) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				tsList.Set_NoLock(index, value);
			else
#endif
				list[index] = value;
		}

		/// <summary>
		/// Calls the thread-safe <c>Add_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="ICollection{T}.Add"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		/// <param name="item">Item</param>
		public static void Add_NoLock<T>(this ICollection<T> list, T item) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				tsList.Add_NoLock(item);
			else
#endif
				list.Add(item);
		}

		/// <summary>
		/// Calls the thread-safe <c>Clear_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="ICollection{T}.Clear"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		public static void Clear_NoLock<T>(this ICollection<T> list) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				tsList.Clear_NoLock();
			else
#endif
				list.Clear();
		}

		/// <summary>
		/// Calls the thread-safe <c>Contains_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="ICollection{T}.Contains"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		/// <param name="item">Item</param>
		/// <returns><c>true</c> if <paramref name="item"/> is in the list, else <c>false</c></returns>
		public static bool Contains_NoLock<T>(this ICollection<T> list, T item) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				return tsList.Contains_NoLock(item);
			else
#endif
				return list.Contains(item);
		}

		/// <summary>
		/// Calls the thread-safe <c>CopyTo_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="ICollection{T}.CopyTo"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		/// <param name="array">Destination array</param>
		/// <param name="arrayIndex">Destination index</param>
		public static void CopyTo_NoLock<T>(this ICollection<T> list, T[] array, int arrayIndex) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				tsList.CopyTo_NoLock(array, arrayIndex);
			else
#endif
				list.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Calls the thread-safe <c>Count_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="ICollection{T}.Count"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		/// <returns>Number of elements in the list</returns>
		public static int Count_NoLock<T>(this ICollection<T> list) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				return tsList.Count_NoLock;
			else
#endif
				return list.Count;
		}

		/// <summary>
		/// Calls the thread-safe <c>IsReadOnly_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="ICollection{T}.IsReadOnly"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		public static bool IsReadOnly_NoLock<T>(this ICollection<T> list) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				return tsList.IsReadOnly_NoLock;
			else
#endif
				return list.IsReadOnly;
		}

		/// <summary>
		/// Calls the thread-safe <c>Remove_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="ICollection{T}.Remove"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		/// <param name="item">Item</param>
		/// <returns><c>true</c> if <paramref name="item"/> was removed, else <c>false</c></returns>
		public static bool Remove_NoLock<T>(this ICollection<T> list, T item) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				return tsList.Remove_NoLock(item);
			else
#endif
				return list.Remove(item);
		}

		/// <summary>
		/// Calls the thread-safe <c>GetEnumerator_NoLock()</c> method if <paramref name="list"/> implements
		/// a thread-safe list interface, else calls <see cref="IEnumerable{T}.GetEnumerator"/> setter
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		/// <returns>A new <see cref="IEnumerator{T}"/> instance</returns>
		public static IEnumerator<T> GetEnumerator_NoLock<T>(this IEnumerable<T> list) {
#if THREAD_SAFE
			var tsList = list as ThreadSafe.IList<T>;
			if (tsList != null)
				return tsList.GetEnumerator_NoLock();
			else
#endif
				return list.GetEnumerator();
		}

		/// <summary>
		/// Calls <see cref="GetEnumerator_NoLock{T}(IEnumerable{T})"/> to get an
		/// <see cref="IEnumerator{T}"/> which is used to iterate over the whole list. Each item is
		/// then returned to the caller.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">An <see cref="IList{T}"/></param>
		/// <returns>All items of the list</returns>
		public static IEnumerable<T> GetEnumerable_NoLock<T>(this ICollection<T> list) {
			using (var enumerator = list.GetEnumerator_NoLock()) {
				while (enumerator.MoveNext())
					yield return enumerator.Current;
			}
		}

		/// <summary>
		/// Iterates over the whole list but doesn't keep the lock. It doesn't use any enumerator
		/// so no exception can be thrown if another thread modifies the list.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="list">A list</param>
		/// <returns>A list enumerable</returns>
		public static IEnumerable<T> GetSafeEnumerable<T>(this IList<T> list) {
			for (int i = 0; i < list.Count; i++) {
				T value;
#if THREAD_SAFE
				try {
#endif
				value = list[i];
#if THREAD_SAFE
				}
				catch (IndexOutOfRangeException) {
					break;
				}
				catch (ArgumentOutOfRangeException) {
					break;
				}
#endif
				yield return value;
			}
		}

		/// <summary>
		/// Iterates over the whole list but doesn't keep the lock. It doesn't use any enumerator
		/// so no exception can be thrown if another thread modifies the list.
		/// </summary>
		/// <typeparam name="T">Type to store in list</typeparam>
		/// <param name="coll">A collection</param>
		/// <returns>A list enumerable</returns>
		public static IEnumerable<T> GetSafeEnumerable<T>(this IEnumerable<T> coll) {
			var list = coll as IList<T>;
			if (list != null)
				return GetSafeEnumerable(list);

			return coll;
		}
	}
}
