// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Threading;

namespace dnlib.DotNet.Pdb.Portable {
	static class ListCache<T> {
		static volatile List<T> cachedList;
		public static List<T> AllocList() {
			return Interlocked.Exchange(ref cachedList, null) ?? new List<T>();
		}
		public static void Free(ref List<T> list) {
			list.Clear();
			cachedList = list;
		}
		public static T[] FreeAndToArray(ref List<T> list) {
			var res = list.ToArray();
			Free(ref list);
			return res;
		}
	}
}
