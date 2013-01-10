/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace dnlib.Utils {
	/// <summary>
	/// Gets notified of list events
	/// </summary>
	/// <typeparam name="TListValue">List value</typeparam>
	interface IListListener<TListValue> {
		/// <summary>
		/// Called before a new value is lazily added to the list.
		/// </summary>
		/// <param name="index">Index where the value will be added</param>
		/// <param name="value">Value that will be added to the list. It can be modified by
		/// the callee.</param>
		void OnLazyAdd(int index, ref TListValue value);

		/// <summary>
		/// Called before a new value is added to the list.
		/// </summary>
		/// <param name="index">Index where the value will be added</param>
		/// <param name="value">Value that will be added to the list</param>
		void OnAdd(int index, TListValue value);

		/// <summary>
		/// Called before a value is removed from the list. If all elements are removed,
		/// <see cref="OnClear()"/> is called, and this method is not called.
		/// </summary>
		/// <param name="index">Index of value</param>
		/// <param name="value">The value that will be removed</param>
		void OnRemove(int index, TListValue value);

		/// <summary>
		/// Called after the list has been resized (eg. an element has been added/removed). It's not
		/// called when an element is replaced.
		/// </summary>
		/// <param name="index">Index where the change occurred.</param>
		void OnResize(int index);

		/// <summary>
		/// Called before the whole list is cleared.
		/// </summary>
		void OnClear();
	}

	/// <summary>
	/// Implements a <see cref="IList{T}"/> that is lazily initialized
	/// </summary>
	/// <typeparam name="TValue">Type to store in list</typeparam>
	[DebuggerDisplay("Count = {Count}")]
	public class LazyList<TValue> : ILazyList<TValue> where TValue : class {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		object context;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		MFunc<object, uint, TValue> readOriginalValue;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		List<Element> list;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int id = 0;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IListListener<TValue> listener;

		/// <summary>
		/// Stores a simple value
		/// </summary>
		class Element {
			protected TValue value;

			/// <summary>
			/// <c>true</c> if it has been initialized, <c>false</c> otherwise
			/// </summary>
			public virtual bool IsInitialized {
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
			public virtual TValue GetValue(int index) {
				return value;
			}

			/// <summary>
			/// Sets the value
			/// </summary>
			/// <param name="index">Index in the list</param>
			/// <param name="value">New value</param>
			public virtual void SetValue(int index, TValue value) {
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
			internal uint origIndex;
			LazyList<TValue> lazyList;

			/// <inheritdoc/>
			public override bool IsInitialized {
				get { return lazyList == null; }
			}

			/// <inheritdoc/>
			public override TValue GetValue(int index) {
				if (lazyList != null) {
					value = lazyList.ReadOriginalValue(index, origIndex);
					lazyList = null;
				}
				return value;
			}

			/// <inheritdoc/>
			public override void SetValue(int index, TValue value) {
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
					value = lazyList.ReadOriginalValue(this);
					lazyList = null;
				}
				return value == null ? string.Empty : value.ToString();
			}
		}

		/// <inheritdoc/>
		[DebuggerBrowsableAttribute(DebuggerBrowsableState.Never)]
		public int Count {
			get { return list.Count; }
		}

		/// <inheritdoc/>
		[DebuggerBrowsableAttribute(DebuggerBrowsableState.Never)]
		public bool IsReadOnly {
			get { return false; }
		}

		/// <inheritdoc/>
		public TValue this[int index] {
			get { return list[index].GetValue(index); }
			set {
				if (listener != null) {
					listener.OnRemove(index, list[index].GetValue(index));
					listener.OnAdd(index, value);
				}
				list[index].SetValue(index, value);
				id++;
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		internal LazyList()
			: this(null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="listener">List listener</param>
		internal LazyList(IListListener<TValue> listener) {
			this.listener = listener;
			this.list = new List<Element>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="context">Context passed to <paramref name="readOriginalValue"/></param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		internal LazyList(int length, object context, MFunc<object, uint, TValue> readOriginalValue)
			: this(length, null, context, readOriginalValue) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="listener">List listener</param>
		/// <param name="context">Context passed to <paramref name="readOriginalValue"/></param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		internal LazyList(int length, IListListener<TValue> listener, object context, MFunc<object, uint, TValue> readOriginalValue) {
			this.listener = listener;
			this.context = context;
			this.readOriginalValue = readOriginalValue;
			this.list = new List<Element>(length);
			for (int i = 0; i < length; i++)
				list.Add(new LazyElement(i, this));
		}

		TValue ReadOriginalValue(LazyElement elem) {
			return ReadOriginalValue(list.IndexOf(elem), elem.origIndex);
		}

		TValue ReadOriginalValue(int index, uint origIndex) {
			var newValue = readOriginalValue(context, origIndex);
			if (listener != null)
				listener.OnLazyAdd(index, ref newValue);
			return newValue;
		}

		/// <inheritdoc/>
		public int IndexOf(TValue item) {
			for (int i = 0; i < list.Count; i++) {
				if (list[i].GetValue(i) == item)
					return i;
			}
			return -1;
		}

		/// <inheritdoc/>
		public void Insert(int index, TValue item) {
			if (listener != null)
				listener.OnAdd(index, item);
			list.Insert(index, new Element(item));
			if (listener != null)
				listener.OnResize(index);
			id++;
		}

		/// <inheritdoc/>
		public void RemoveAt(int index) {
			if (listener != null)
				listener.OnRemove(index, list[index].GetValue(index));
			list.RemoveAt(index);
			if (listener != null)
				listener.OnResize(index);
			id++;
		}

		/// <inheritdoc/>
		public void Add(TValue item) {
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
		public void CopyTo(TValue[] array, int arrayIndex) {
			for (int i = 0; i < list.Count; i++)
				array[arrayIndex + i] = list[i].GetValue(i);
		}

		/// <inheritdoc/>
		public bool Remove(TValue item) {
			int index = IndexOf(item);
			if (index < 0)
				return false;
			RemoveAt(index);
			return true;
		}

		/// <inheritdoc/>
		public bool IsInitialized(int index) {
			if ((uint)index >= (uint)list.Count)
				return false;
			return list[index].IsInitialized;
		}

		/// <inheritdoc/>
		public IEnumerator<TValue> GetEnumerator() {
			int id2 = id;
			for (int i = 0; i < list.Count; i++) {
				if (id != id2)
					throw new InvalidOperationException("List was modified");
				yield return list[i].GetValue(i);
			}
		}

		/// <inheritdoc/>
		public IEnumerable<TValue> GetInitializedElements() {
			int id2 = id;
			for (int i = 0; i < list.Count; i++) {
				if (id != id2)
					throw new InvalidOperationException("List was modified");
				var elem = list[i];
				if (!elem.IsInitialized)
					continue;
				yield return elem.GetValue(i);
			}
		}

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
