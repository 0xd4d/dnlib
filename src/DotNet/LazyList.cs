using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace dot10.DotNet {
	/// <summary>
	/// Implements a <see cref="IList{T}"/> that is lazily initialized
	/// </summary>
	/// <typeparam name="TValue">Type to store in list</typeparam>
	[DebuggerDisplay("Count = {Count}")]
	class LazyList<TValue> : IList<TValue> where TValue : class {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		object context;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		MFunc<object, uint, TValue> readOriginalValue;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		List<Element> list;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int id = 0;

		/// <summary>
		/// Stores data and keeps track of the original index and whether the data has been
		/// initialized or not.
		/// </summary>
		class Element {
			uint origIndex;
			TValue value;
			LazyList<TValue> lazyList;

			/// <summary>
			/// Gets/sets the value
			/// </summary>
			public TValue Value {
				get {
					if (lazyList != null) {
						value = lazyList.readOriginalValue(lazyList.context, origIndex);
						lazyList = null;
					}
					return value;
				}
				set {
					this.value = value;
					lazyList = null;
				}
			}

			/// <summary>
			/// Constructor that should only be called when <see cref="LazyList{T}"/> is initialized.
			/// </summary>
			/// <param name="origIndex">Original index of this element</param>
			/// <param name="lazyList">LazyList instance</param>
			public Element(int origIndex, LazyList<TValue> lazyList) {
				this.origIndex = (uint)origIndex;
				this.lazyList = lazyList;
			}

			/// <summary>
			/// Constructor that should be used when new elements are inserted into <see cref="LazyList{T}"/>
			/// </summary>
			/// <param name="data">User data</param>
			public Element(TValue data) {
				this.value = data;
			}

			/// <inheritdoc/>
			public override string ToString() {
				if (lazyList != null) {
					value = lazyList.readOriginalValue(lazyList.context, origIndex);
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
			get { return list[index].Value; }
			set { list[index].Value = value; id++; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		public LazyList(int length, MFunc<object, uint, TValue> readOriginalValue)
			: this(length, 0, readOriginalValue) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="context">Context passed to <paramref name="readOriginalValue"/></param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		public LazyList(int length, object context, MFunc<object, uint, TValue> readOriginalValue) {
			this.context = context;
			this.readOriginalValue = readOriginalValue;
			this.list = new List<Element>(length);
			for (int i = 0; i < length; i++)
				list.Add(new Element(i, this));
		}

		/// <inheritdoc/>
		public int IndexOf(TValue item) {
			for (int i = 0; i < list.Count; i++) {
				if (list[i].Value == item)
					return i;
			}
			return -1;
		}

		/// <inheritdoc/>
		public void Insert(int index, TValue item) {
			list.Insert(index, new Element(item));
			id++;
		}

		/// <inheritdoc/>
		public void RemoveAt(int index) {
			list.RemoveAt(index);
			id++;
		}

		/// <inheritdoc/>
		public void Add(TValue item) {
			list.Add(new Element(item));
			id++;
		}

		/// <inheritdoc/>
		public void Clear() {
			list.Clear();
			id++;
		}

		/// <inheritdoc/>
		public bool Contains(TValue item) {
			return IndexOf(item) >= 0;
		}

		/// <inheritdoc/>
		public void CopyTo(TValue[] array, int arrayIndex) {
			for (int i = 0; i < list.Count; i++)
				array[arrayIndex + i] = list[i].Value;
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
		public IEnumerator<TValue> GetEnumerator() {
			int id2 = id;
			for (int i = 0; i < list.Count; i++) {
				if (id != id2)
					throw new InvalidOperationException("List was modified");
				yield return list[i].Value;
			}
		}

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
