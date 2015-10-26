// dnlib: See LICENSE.txt for more info

﻿using System;
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
	public class LazyList<TValue> : ILazyList<TValue> where TValue : class {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly object context;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly MFunc<object, uint, TValue> readOriginalValue;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		readonly List<Element> list;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int id = 0;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly IListListener<TValue> listener;

#if THREAD_SAFE
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly Lock theLock = Lock.Create();
#endif

		/// <summary>
		/// Stores a simple value
		/// </summary>
		class Element {
			protected TValue value;

			/// <summary>
			/// <c>true</c> if it has been initialized, <c>false</c> otherwise
			/// </summary>
			public virtual bool IsInitialized_NoLock {
				get { return true; }
			}

			/// <summary>
			/// Default constructor
			/// </summary>
			protected Element() {
			}

			/// <summary>
			/// Constructor that should be used when new elements are inserted into <see cref="LazyList{T}"/>
			/// </summary>
			/// <param name="data">User data</param>
			public Element(TValue data) {
				this.value = data;
			}

			/// <summary>
			/// Gets the value
			/// </summary>
			/// <param name="index">Index in the list</param>
			public virtual TValue GetValue_NoLock(int index) {
				return value;
			}

			/// <summary>
			/// Sets the value
			/// </summary>
			/// <param name="index">Index in the list</param>
			/// <param name="value">New value</param>
			public virtual void SetValue_NoLock(int index, TValue value) {
				this.value = value;
			}

			/// <inheritdoc/>
			public override string ToString() {
				return value == null ? string.Empty : value.ToString();
			}
		}

		/// <summary>
		/// Stores data and keeps track of the original index and whether the data has been
		/// initialized or not.
		/// </summary>
		sealed class LazyElement : Element {
			internal readonly uint origIndex;
			LazyList<TValue> lazyList;

			/// <inheritdoc/>
			public override bool IsInitialized_NoLock {
				get { return lazyList == null; }
			}

			/// <inheritdoc/>
			public override TValue GetValue_NoLock(int index) {
				if (lazyList != null) {
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
			/// Constructor that should only be called when <see cref="LazyList{T}"/> is initialized.
			/// </summary>
			/// <param name="origIndex">Original index of this element</param>
			/// <param name="lazyList">LazyList instance</param>
			public LazyElement(int origIndex, LazyList<TValue> lazyList) {
				this.origIndex = (uint)origIndex;
				this.lazyList = lazyList;
			}

			/// <inheritdoc/>
			public override string ToString() {
				if (lazyList != null) {
					value = lazyList.ReadOriginalValue_NoLock(this);
					lazyList = null;
				}
				return value == null ? string.Empty : value.ToString();
			}
		}

		/// <inheritdoc/>
		[DebuggerBrowsableAttribute(DebuggerBrowsableState.Never)]
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
		[DebuggerBrowsableAttribute(DebuggerBrowsableState.Never)]
		public int Count_NoLock {
			get { return list.Count; }
		}

		/// <inheritdoc/>
		[DebuggerBrowsableAttribute(DebuggerBrowsableState.Never)]
		public bool IsReadOnly {
			get { return false; }
		}

		/// <inheritdoc/>
		[DebuggerBrowsableAttribute(DebuggerBrowsableState.Never)]
		public bool IsReadOnly_NoLock {
			get { return false; }
		}

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

		/// <inheritdoc/>
		public TValue Get_NoLock(int index) {
			return list[index].GetValue_NoLock(index);
		}

		/// <inheritdoc/>
		public void Set_NoLock(int index, TValue value) {
			if (listener != null) {
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
			this.list = new List<Element>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="context">Context passed to <paramref name="readOriginalValue"/></param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		public LazyList(int length, object context, MFunc<object, uint, TValue> readOriginalValue)
			: this(length, null, context, readOriginalValue) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="listener">List listener</param>
		/// <param name="context">Context passed to <paramref name="readOriginalValue"/></param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		public LazyList(int length, IListListener<TValue> listener, object context, MFunc<object, uint, TValue> readOriginalValue) {
			this.listener = listener;
			this.context = context;
			this.readOriginalValue = readOriginalValue;
			this.list = new List<Element>(length);
			for (int i = 0; i < length; i++)
				list.Add(new LazyElement(i, this));
		}

		TValue ReadOriginalValue_NoLock(LazyElement elem) {
			return ReadOriginalValue_NoLock(list.IndexOf(elem), elem.origIndex);
		}

		TValue ReadOriginalValue_NoLock(int index, uint origIndex) {
			var newValue = readOriginalValue(context, origIndex);
			if (listener != null)
				listener.OnLazyAdd(index, ref newValue);
			return newValue;
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

		/// <inheritdoc/>
		public int IndexOf_NoLock(TValue item) {
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

		/// <inheritdoc/>
		public void Insert_NoLock(int index, TValue item) {
			if (listener != null)
				listener.OnAdd(index, item);
			list.Insert(index, new Element(item));
			if (listener != null)
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

		/// <inheritdoc/>
		public void RemoveAt_NoLock(int index) {
			if (listener != null)
				listener.OnRemove(index, list[index].GetValue_NoLock(index));
			list.RemoveAt(index);
			if (listener != null)
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

		/// <inheritdoc/>
		public void Add_NoLock(TValue item) {
			int index = list.Count;
			if (listener != null)
				listener.OnAdd(index, item);
			list.Add(new Element(item));
			if (listener != null)
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

		/// <inheritdoc/>
		public void Clear_NoLock() {
			if (listener != null)
				listener.OnClear();
			list.Clear();
			if (listener != null)
				listener.OnResize(0);
			id++;
		}

		/// <inheritdoc/>
		public bool Contains(TValue item) {
			return IndexOf(item) >= 0;
		}

		/// <inheritdoc/>
		public bool Contains_NoLock(TValue item) {
			return IndexOf_NoLock(item) >= 0;
		}

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

		/// <inheritdoc/>
		public void CopyTo_NoLock(TValue[] array, int arrayIndex) {
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

		/// <inheritdoc/>
		public bool Remove_NoLock(TValue item) {
			int index = IndexOf_NoLock(item);
			if (index < 0)
				return false;
			RemoveAt_NoLock(index);
			return true;
		}

		/// <inheritdoc/>
		public bool IsInitialized(int index) {
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
#endif
			return IsInitialized_NoLock(index);
#if THREAD_SAFE
			} finally { theLock.ExitReadLock(); }
#endif
		}

		/// <inheritdoc/>
		public bool IsInitialized_NoLock(int index) {
			if ((uint)index >= (uint)list.Count)
				return false;
			return list[index].IsInitialized_NoLock;
		}

		/// <inheritdoc/>
		public IEnumerator<TValue> GetEnumerator() {
			int id2;
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
#endif
			id2 = id;
#if THREAD_SAFE
			} finally { theLock.ExitReadLock(); }
#endif
			for (int i = 0; ; i++) {
				TValue value;
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				if (id != id2)
					throw new InvalidOperationException("List was modified");
				if (i >= list.Count)
					break;
				value = list[i].GetValue_NoLock(i);
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
				yield return value;
			}
		}

		/// <inheritdoc/>
		public IEnumerator<TValue> GetEnumerator_NoLock() {
			int id2 = id;
			for (int i = 0; i < list.Count; i++) {
				if (id != id2)
					throw new InvalidOperationException("List was modified");
				yield return list[i].GetValue_NoLock(i);
			}
		}

		/// <inheritdoc/>
		public List<TValue> GetInitializedElements(bool clearList) {
			List<TValue> newList;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			newList = new List<TValue>(list.Count);
			int id2 = id;
			for (int i = 0; i < list.Count; i++) {
				if (id != id2)
					throw new InvalidOperationException("List was modified");
				var elem = list[i];
				if (!elem.IsInitialized_NoLock)
					continue;
				newList.Add(elem.GetValue_NoLock(i));
			}
			if (clearList)
				Clear_NoLock();
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
			return newList;
		}

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

#if THREAD_SAFE
		/// <inheritdoc/>
		public TRetType ExecuteLocked<TArgType, TRetType>(TArgType arg, ExecuteLockedDelegate<TValue, TArgType, TRetType> handler) {
			theLock.EnterWriteLock(); try {
				return handler(this, arg);
			} finally { theLock.ExitWriteLock(); }
		}
#endif
	}
}
