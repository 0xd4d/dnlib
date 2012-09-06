using System;
using System.Collections;
using System.Collections.Generic;

namespace dot10.dotNET.Hi {
	/// <summary>
	/// Implements a <see cref="IList{T}"/> that is lazily initialized
	/// </summary>
	/// <typeparam name="TValue">Type to store in list</typeparam>
	class LazyList<TValue> : IList<TValue> where TValue : class {
		uint indexBase;
		MFunc<uint, TValue> readOriginalValue;
		List<Element> list;
		int id = 0;

		/// <summary>
		/// Stores data and keeps track of the original index and whether the data has been
		/// initialized or not.
		/// </summary>
		class Element {
			const uint NOT_INITIALIZED = 0x80000000;
			uint origIndex;
			TValue value;

			/// <summary>
			/// Gets the original index. This is valid iff <see cref="IsInitialized"/> is <c>false</c>
			/// </summary>
			public uint OrigIndex {
				get { return origIndex & ~NOT_INITIALIZED; }
			}

			/// <summary>
			/// Gets/sets the value
			/// </summary>
			public TValue Value {
				get {
#if DEBUG
					if (!IsInitialized)
						throw new InvalidOperationException("Data isn't initialized yet");
#endif
					return value;
				}
				set {
					this.value = value;
					origIndex = 0;
				}
			}

			/// <summary>
			/// Returns true if <see cref="Value"/> has been initialized
			/// </summary>
			public bool IsInitialized {
				get { return (origIndex & NOT_INITIALIZED) == 0; }
			}

			/// <summary>
			/// Constructor that should only be called when <see cref="LazyList{T}"/> is initialized.
			/// </summary>
			/// <param name="origIndex">Original index of this element</param>
			public Element(int origIndex) {
				this.origIndex = (uint)origIndex | NOT_INITIALIZED;
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
				return value == null ? string.Empty : value.ToString();
			}
		}

		/// <inheritdoc/>
		public int Count {
			get { return list.Count; }
		}

		/// <inheritdoc/>
		public bool IsReadOnly {
			get { return false; }
		}

		/// <inheritdoc/>
		public TValue this[int index] {
			get { return InitializeElem(index).Value; }
			set { list[index].Value = value; id++; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		public LazyList(int length, MFunc<uint, TValue> readOriginalValue)
			: this(length, 0, readOriginalValue) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="indexBase">Value to add to element index when calling
		/// <paramref name="readOriginalValue"/>, eg. <c>1</c> or <c>0x06000001</c></param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		public LazyList(int length, uint indexBase, MFunc<uint, TValue> readOriginalValue) {
			this.indexBase = indexBase;
			this.readOriginalValue = readOriginalValue;
			this.list = new List<Element>(length);
			for (int i = 0; i < length; i++)
				list.Add(new Element(i));
		}

		Element InitializeElem(int index) {
			var elem = list[index];
			if (!elem.IsInitialized)
				elem.Value = readOriginalValue((uint)(indexBase + index));
			return elem;
		}

		/// <inheritdoc/>
		public int IndexOf(TValue item) {
			for (int i = 0; i < list.Count; i++) {
				if (InitializeElem(i).Value == item)
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
				array[arrayIndex + i] = InitializeElem(i).Value;
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
				yield return InitializeElem(i).Value;
			}
		}

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
