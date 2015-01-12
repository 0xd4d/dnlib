// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.Threading {
#if THREAD_SAFE
	/// <summary>
	/// Protects an <see cref="IList{T}"/> from being accessed by multiple threads at the same time
	/// </summary>
	/// <typeparam name="T">List type</typeparam>
	[DebuggerDisplay("Count = {Count}")]
	sealed class ThreadSafeListWrapper<T> : ThreadSafe.IList<T> {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly Lock theLock = Lock.Create();

		readonly IList<T> list;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="list">A list</param>
		public ThreadSafeListWrapper(IList<T> list) {
			if (list == null)
				throw new ArgumentNullException("list");
			this.list = list;
		}

		/// <inheritdoc/>
		public int IndexOf(T item) {
			// We need a write lock since we don't know whether 'list' modifies any internal fields.
			theLock.EnterWriteLock(); try {
				return list.IndexOf(item);
			} finally { theLock.ExitWriteLock(); }
		}

		/// <inheritdoc/>
		public void Insert(int index, T item) {
			theLock.EnterWriteLock(); try {
				list.Insert(index, item);
			} finally { theLock.ExitWriteLock(); }
		}

		/// <inheritdoc/>
		public void RemoveAt(int index) {
			theLock.EnterWriteLock(); try {
				list.RemoveAt(index);
			} finally { theLock.ExitWriteLock(); }
		}

		/// <inheritdoc/>
		public T this[int index] {
			get {
				theLock.EnterWriteLock(); try {
					return list[index];
				} finally { theLock.ExitWriteLock(); }
			}
			set {
				theLock.EnterWriteLock(); try {
					list[index] = value;
				} finally { theLock.ExitWriteLock(); }
			}
		}

		/// <inheritdoc/>
		public void Add(T item) {
			theLock.EnterWriteLock(); try {
				list.Add(item);
			} finally { theLock.ExitWriteLock(); }
		}

		/// <inheritdoc/>
		public void Clear() {
			theLock.EnterWriteLock(); try {
				list.Clear();
			} finally { theLock.ExitWriteLock(); }
		}

		/// <inheritdoc/>
		public bool Contains(T item) {
			theLock.EnterWriteLock(); try {
				return list.Contains(item);
			} finally { theLock.ExitWriteLock(); }
		}

		/// <inheritdoc/>
		public void CopyTo(T[] array, int arrayIndex) {
			theLock.EnterWriteLock(); try {
				list.CopyTo(array, arrayIndex);
			} finally { theLock.ExitWriteLock(); }
		}

		/// <inheritdoc/>
		public int Count {
			get {
				theLock.EnterWriteLock(); try {
					return list.Count;
				} finally { theLock.ExitWriteLock(); }
			}
		}

		/// <inheritdoc/>
		public bool IsReadOnly {
			get {
				theLock.EnterWriteLock(); try {
					return list.IsReadOnly;
				} finally { theLock.ExitWriteLock(); }
			}
		}

		/// <inheritdoc/>
		public bool Remove(T item) {
			theLock.EnterWriteLock(); try {
				return list.Remove(item);
			} finally { theLock.ExitWriteLock(); }
		}

		/// <inheritdoc/>
		public IEnumerator<T> GetEnumerator() {
			theLock.EnterWriteLock(); try {
				return list.GetEnumerator();
			} finally { theLock.ExitWriteLock(); }
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		/// <inheritdoc/>
		public int Count_NoLock {
			get { return list.Count; }
		}

		/// <inheritdoc/>
		public bool IsReadOnly_NoLock {
			get { return list.IsReadOnly; }
		}

		/// <inheritdoc/>
		public int IndexOf_NoLock(T item) {
			return list.IndexOf(item);
		}

		/// <inheritdoc/>
		public void Insert_NoLock(int index, T item) {
			list.Insert(index, item);
		}

		/// <inheritdoc/>
		public void RemoveAt_NoLock(int index) {
			list.RemoveAt(index);
		}

		/// <inheritdoc/>
		public T Get_NoLock(int index) {
			return list[index];
		}

		/// <inheritdoc/>
		public void Set_NoLock(int index, T value) {
			list[index] = value;
		}

		/// <inheritdoc/>
		public void Add_NoLock(T item) {
			list.Add(item);
		}

		/// <inheritdoc/>
		public void Clear_NoLock() {
			list.Clear();
		}

		/// <inheritdoc/>
		public bool Contains_NoLock(T item) {
			return list.Contains(item);
		}

		/// <inheritdoc/>
		public void CopyTo_NoLock(T[] array, int arrayIndex) {
			list.CopyTo(array, arrayIndex);
		}

		/// <inheritdoc/>
		public bool Remove_NoLock(T item) {
			return list.Remove(item);
		}

		/// <inheritdoc/>
		public IEnumerator<T> GetEnumerator_NoLock() {
			return list.GetEnumerator();
		}

		/// <inheritdoc/>
		public TRetType ExecuteLocked<TArgType, TRetType>(TArgType arg, ExecuteLockedDelegate<T, TArgType, TRetType> handler) {
			theLock.EnterWriteLock(); try {
				return handler(this, arg);
			} finally { theLock.ExitWriteLock(); }
		}
	}
#endif
}
