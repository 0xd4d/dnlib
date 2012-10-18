using System.Collections.Generic;

namespace dot10.DotNet.Writer {
	class MDTable<T> {
		Dictionary<T, uint> cachedDict;
		List<T> cached;

		/// <summary>
		/// <c>true</c> if the table is empty
		/// </summary>
		public bool IsEmpty {
			get { return cached.Count == 0; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="equalityComparer">Equality comparer</param>
		public MDTable(IEqualityComparer<T> equalityComparer) {
			cachedDict = new Dictionary<T, uint>(equalityComparer);
			cached = new List<T>();
		}

		/// <summary>
		/// Adds a row. If the row already exists, returns a rid to the existing one, else
		/// it's created and a new rid is returned.
		/// </summary>
		/// <param name="row">The row. It's now owned by us and can NOT be modified by the caller.</param>
		/// <returns>The RID (row ID) of the row</returns>
		public uint Add(T row) {
			uint rid;
			if (cachedDict.TryGetValue(row, out rid))
				return rid;
			return Create(row);
		}

		/// <summary>
		/// Creates a new row even if this row already exists.
		/// </summary>
		/// <param name="row">The row. It's now owned by us and can NOT be modified by the caller.</param>
		/// <returns>The RID (row ID) of the row</returns>
		public uint Create(T row) {
			uint rid = (uint)cached.Count + 1;
			if (!cachedDict.ContainsKey(row))
				cachedDict[row] = rid;
			cached.Add(row);
			return rid;
		}
	}
}
