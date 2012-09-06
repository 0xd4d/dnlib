namespace dot10.dotNET.Hi {
	delegate U MFunc<T, U>(T t);

	/// <summary>
	/// A readonly list that gets initialized lazily
	/// </summary>
	/// <typeparam name="T">A <see cref="ICodedToken"/> type</typeparam>
	class SimpleLazyList<T> where T : class, ICodedToken {
		T[] elements;
		bool[] initialized;
		readonly MFunc<uint, T> readElementByRID;
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
		public SimpleLazyList(uint length, MFunc<uint, T> readElementByRID) {
			this.length = length;
			this.readElementByRID = readElementByRID;
		}
	}
}
