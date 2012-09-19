using System.Diagnostics;

namespace dot10.DotNet {
	/// <summary>
	/// A readonly list that gets initialized lazily
	/// </summary>
	/// <typeparam name="T">A <see cref="ICodedToken"/> type</typeparam>
	[DebuggerDisplay("Count = {Length}")]
	class SimpleLazyList<T> where T : class, IMDTokenProvider {
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		T[] elements;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool[] initialized;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly MFunc<uint, T> readElementByRID;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
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
		/// <returns>The element or <c>null</c> if <paramref name="index"/> is invalid</returns>
		public T this[uint index] {
			get {
				if (index >= length)
					return null;
				if (elements == null) {
					elements = new T[length];
					initialized = new bool[length];
				}
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
