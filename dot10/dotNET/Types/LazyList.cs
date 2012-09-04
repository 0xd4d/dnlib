namespace dot10.dotNET.Types {
	delegate T MFunc<T, U>(U u);

	/// <summary>
	/// A readonly list that gets initialized lazily
	/// </summary>
	/// <typeparam name="T">A <see cref="ICodedToken"/> type</typeparam>
	class LazyList<T> where T : class, ICodedToken {
		T[] elements;
		bool[] initialized;
		readonly MFunc<T, uint> readElementByRID;
		readonly uint length;

		/// <summary>
		/// Gets the length of this list
		/// </summary>
		public uint Length {
			get { return length; }
		}

		/// <summary>
		/// Access the list
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns>The element or null if <paramref name="index"/> is invalid</returns>
		public T this[uint index] {
			get {
				if (elements == null) {
					elements = new T[length];
					initialized = new bool[length];
				}
				if (index >= length)
					return null;
				if (!initialized[index]) {
					elements[index] = readElementByRID(index + 1);
					initialized[index] = true;
				}
				return elements[index];
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Length of the list</param>
		/// <param name="readElementByRID">Delegate instance that lazily reads an element</param>
		public LazyList(uint length, MFunc<T, uint> readElementByRID) {
			this.length = length;
			this.readElementByRID = readElementByRID;
		}
	}
}
