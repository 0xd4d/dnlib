// dnlib: See LICENSE.txt for more info

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.Threading;

namespace dnlib.Utils {
	/// <summary>
	/// Gets notified of list events
	/// </summary>
	/// <typeparam name="TListValue">List value</typeparam>
	public interface IListListener<TListValue> {
		/// <summary>
		/// Called before a new value is lazily added to the list.
		/// </summary>
		/// <remarks>If you must access this list, you can only call <c>_NoLock()</c> methods
		/// since a write lock is now held by this thread.</remarks>
		/// <param name="index">Index where the value will be added</param>
		/// <param name="value">Value that will be added to the list. It can be modified by
		/// the callee.</param>
		void OnLazyAdd(int index, ref TListValue value);

		/// <summary>
		/// Called before a new value is added to the list.
		/// </summary>
		/// <remarks>If you must access this list, you can only call <c>_NoLock()</c> methods
		/// since a write lock is now held by this thread.</remarks>
		/// <param name="index">Index where the value will be added</param>
		/// <param name="value">Value that will be added to the list</param>
		void OnAdd(int index, TListValue value);

		/// <summary>
		/// Called before a value is removed from the list. If all elements are removed,
		/// <see cref="OnClear()"/> is called, and this method is not called.
		/// </summary>
		/// <remarks>If you must access this list, you can only call <c>_NoLock()</c> methods
		/// since a write lock is now held by this thread.</remarks>
		/// <param name="index">Index of value</param>
		/// <param name="value">The value that will be removed</param>
		void OnRemove(int index, TListValue value);

		/// <summary>
		/// Called after the list has been resized (eg. an element has been added/removed). It's not
		/// called when an element is replaced.
		/// </summary>
		/// <remarks>If you must access this list, you can only call <c>_NoLock()</c> methods
		/// since a write lock is now held by this thread.</remarks>
		/// <param name="index">Index where the change occurred.</param>
		void OnResize(int index);

		/// <summary>
		/// Called before the whole list is cleared.
		/// </summary>
		/// <remarks>If you must access this list, you can only call <c>_NoLock()</c> methods
		/// since a write lock is now held by this thread.</remarks>
		void OnClear();
	}

	/// <summary>
	/// Implements a <see cref="IList{T}"/> that is lazily initialized
	/// </summary>
	/// <typeparam name="TValue">Type to store in list</typeparam>
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
	public class LazyList<TValue> : ILazyList<TValue> where TValue : class {
		private protected readonly List<Element> list;
		int id = 0;
		private protected readonly IListListener<TValue> listener;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <summary>
		/// Stores a simple value
		/// </summary>
		private protected class Element {
			protected TValue value;

			/// <summary>
			/// <c>true</c> if it has been initialized, <c>false</c> otherwise
			/// </summary>
			public virtual bool IsInitialized_NoLock => true;

			/// <summary>
			/// Default constructor
			/// </summary>
			protected Element() {
			}

			/// <summary>
			/// Constructor that should be used when new elements are inserted into <see cref="LazyList{T}"/>
			/// </summary>
			/// <param name="data">User data</param>
			public Element(TValue data) => value = data;

			/// <summary>
			/// Gets the value
			/// </summary>
			/// <param name="index">Index in the list</param>
			public virtual TValue GetValue_NoLock(int index) => value;

			/// <summary>
			/// Sets the value
			/// </summary>
			/// <param name="index">Index in the list</param>
			/// <param name="value">New value</param>
			public virtual void SetValue_NoLock(int index, TValue value) => this.value = value;

			/// <inheritdoc/>
			public override string ToString() => value?.ToString() ?? string.Empty;
		}

		/// <inheritdoc/>
		public int Count {
			get {
#if THREAD_SAFE
				theLock.EnterReadLock(); try {
#endif
				return Count_NoLock;
#if THREAD_SAFE
				} finally { theLock.ExitReadLock(); }
#endif
			}
		}

		/// <inheritdoc/>
		internal int Count_NoLock => list.Count;

		/// <inheritdoc/>
		public bool IsReadOnly => false;

		/// <inheritdoc/>
		public TValue this[int index] {
			get {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				return Get_NoLock(index);
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				Set_NoLock(index, value);
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		internal TValue Get_NoLock(int index) => list[index].GetValue_NoLock(index);

		void Set_NoLock(int index, TValue value) {
			if (!(listener is null)) {
				listener.OnRemove(index, list[index].GetValue_NoLock(index));
				listener.OnAdd(index, value);
			}
			list[index].SetValue_NoLock(index, value);
			id++;
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public LazyList()
			: this(null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="listener">List listener</param>
		public LazyList(IListListener<TValue> listener) {
			this.listener = listener;
			list = new List<Element>();
		}

		private protected LazyList(int length, IListListener<TValue> listener) {
			this.listener = listener;
			list = new List<Element>(length);
		}

		/// <inheritdoc/>
		public int IndexOf(TValue item) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			return IndexOf_NoLock(item);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		int IndexOf_NoLock(TValue item) {
			for (int i = 0; i < list.Count; i++) {
				if (list[i].GetValue_NoLock(i) == item)
					return i;
			}
			return -1;
		}

		/// <inheritdoc/>
		public void Insert(int index, TValue item) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			Insert_NoLock(index, item);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		void Insert_NoLock(int index, TValue item) {
			if (!(listener is null))
				listener.OnAdd(index, item);
			list.Insert(index, new Element(item));
			if (!(listener is null))
				listener.OnResize(index);
			id++;
		}

		/// <inheritdoc/>
		public void RemoveAt(int index) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			RemoveAt_NoLock(index);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		void RemoveAt_NoLock(int index) {
			if (!(listener is null))
				listener.OnRemove(index, list[index].GetValue_NoLock(index));
			list.RemoveAt(index);
			if (!(listener is null))
				listener.OnResize(index);
			id++;
		}

		/// <inheritdoc/>
		public void Add(TValue item) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			Add_NoLock(item);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		void Add_NoLock(TValue item) {
			int index = list.Count;
			if (!(listener is null))
				listener.OnAdd(index, item);
			list.Add(new Element(item));
			if (!(listener is null))
				listener.OnResize(index);
			id++;
		}

		/// <inheritdoc/>
		public void Clear() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			Clear_NoLock();
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		void Clear_NoLock() {
			if (!(listener is null))
				listener.OnClear();
			list.Clear();
			if (!(listener is null))
				listener.OnResize(0);
			id++;
		}

		/// <inheritdoc/>
		public bool Contains(TValue item) => IndexOf(item) >= 0;

		/// <inheritdoc/>
		public void CopyTo(TValue[] array, int arrayIndex) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			CopyTo_NoLock(array, arrayIndex);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		void CopyTo_NoLock(TValue[] array, int arrayIndex) {
			for (int i = 0; i < list.Count; i++)
				array[arrayIndex + i] = list[i].GetValue_NoLock(i);
		}

		/// <inheritdoc/>
		public bool Remove(TValue item) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			return Remove_NoLock(item);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		bool Remove_NoLock(TValue item) {
			int index = IndexOf_NoLock(item);
			if (index < 0)
				return false;
			RemoveAt_NoLock(index);
			return true;
		}

		internal bool IsInitialized(int index) {
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
#endif
			return IsInitialized_NoLock(index);
#if THREAD_SAFE
			} finally { theLock.ExitReadLock(); }
#endif
		}

		bool IsInitialized_NoLock(int index) {
			if ((uint)index >= (uint)list.Count)
				return false;
			return list[index].IsInitialized_NoLock;
		}

		/// <summary>
		/// Enumerator
		/// </summary>
		public struct Enumerator : IEnumerator<TValue> {
			readonly LazyList<TValue> list;
			readonly int id;
			int index;
			TValue current;

			internal Enumerator(LazyList<TValue> list) {
				this.list = list;
				index = 0;
				current = default;
#if THREAD_SAFE
				list.theLock.EnterReadLock(); try {
#endif
				id = list.id;
#if THREAD_SAFE
				} finally { list.theLock.ExitReadLock(); }
#endif
			}

			/// <summary>
			/// Gets the current value
			/// </summary>
			public TValue Current => current;
			object IEnumerator.Current => current;

			/// <summary>
			/// Moves to the next element in the collection
			/// </summary>
			/// <returns></returns>
			public bool MoveNext() {
#if THREAD_SAFE
				list.theLock.EnterWriteLock(); try {
#endif
				if (list.id == id && index < list.Count_NoLock) {
					current = list.list[index].GetValue_NoLock(index);
					index++;
					return true;
				}
				else
					return MoveNextDoneOrThrow_NoLock();
#if THREAD_SAFE
				} finally { list.theLock.ExitWriteLock(); }
#endif
			}

			bool MoveNextDoneOrThrow_NoLock() {
				if (list.id != id)
					throw new InvalidOperationException("List was modified");
				current = default;
				return false;
			}

			/// <summary>
			/// Disposes the enumerator
			/// </summary>
			public void Dispose() { }

			void IEnumerator.Reset() => throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the list enumerator
		/// </summary>
		/// <returns></returns>
		public Enumerator GetEnumerator() => new Enumerator(this);

		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

		internal IEnumerable<TValue> GetEnumerable_NoLock() {
			int id2 = id;
			for (int i = 0; i < list.Count; i++) {
				if (id != id2)
					throw new InvalidOperationException("List was modified");
				yield return list[i].GetValue_NoLock(i);
			}
		}

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// Implements a <see cref="IList{T}"/> that is lazily initialized
	/// </summary>
	/// <typeparam name="TValue">Type to store in list</typeparam>
	/// <typeparam name="TContext">Type of the context passed to the read-value delegate</typeparam>
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(CollectionDebugView<,>))]
	public class LazyList<TValue, TContext> : LazyList<TValue>, ILazyList<TValue> where TValue : class {
		/*readonly*/ TContext context;
		readonly Func<TContext, int, TValue> readOriginalValue;

		/// <summary>
		/// Stores data and keeps track of the original index and whether the data has been
		/// initialized or not.
		/// </summary>
		sealed class LazyElement : Element {
			internal readonly int origIndex;
			LazyList<TValue, TContext> lazyList;

			/// <inheritdoc/>
			public override bool IsInitialized_NoLock => lazyList is null;

			/// <inheritdoc/>
			public override TValue GetValue_NoLock(int index) {
				if (!(lazyList is null)) {
					value = lazyList.ReadOriginalValue_NoLock(index, origIndex);
					lazyList = null;
				}
				return value;
			}

			/// <inheritdoc/>
			public override void SetValue_NoLock(int index, TValue value) {
				this.value = value;
				lazyList = null;
			}

			/// <summary>
			/// Constructor that should only be called when <see cref="LazyList{TValue, TContext}"/> is initialized.
			/// </summary>
			/// <param name="origIndex">Original index of this element</param>
			/// <param name="lazyList">LazyList instance</param>
			public LazyElement(int origIndex, LazyList<TValue, TContext> lazyList) {
				this.origIndex = origIndex;
				this.lazyList = lazyList;
			}

			/// <inheritdoc/>
			public override string ToString() {
				if (!(lazyList is null)) {
					value = lazyList.ReadOriginalValue_NoLock(this);
					lazyList = null;
				}
				return value is null ? string.Empty : value.ToString();
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public LazyList() : this(null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="listener">List listener</param>
		public LazyList(IListListener<TValue> listener) : base(listener) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="context">Context passed to <paramref name="readOriginalValue"/></param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		public LazyList(int length, TContext context, Func<TContext, int, TValue> readOriginalValue)
			: this(length, null, context, readOriginalValue) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="listener">List listener</param>
		/// <param name="context">Context passed to <paramref name="readOriginalValue"/></param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		public LazyList(int length, IListListener<TValue> listener, TContext context, Func<TContext, int, TValue> readOriginalValue) : base(length, listener) {
			this.context = context;
			this.readOriginalValue = readOriginalValue;
			for (int i = 0; i < length; i++)
				list.Add(new LazyElement(i, this));
		}

		TValue ReadOriginalValue_NoLock(LazyElement elem) => ReadOriginalValue_NoLock(list.IndexOf(elem), elem.origIndex);

		TValue ReadOriginalValue_NoLock(int index, int origIndex) {
			var newValue = readOriginalValue(context, origIndex);
			listener?.OnLazyAdd(index, ref newValue);
			return newValue;
		}
	}
}
