// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnlib.Utils {
	class CollectionDebugView<TValue> {
		readonly ICollection<TValue> list;
		public CollectionDebugView(ICollection<TValue> list) => this.list = list ?? throw new ArgumentNullException(nameof(list));

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public TValue[] Items {
			get {
				var array = new TValue[list.Count];
				list.CopyTo(array, 0);
				return array;
			}
		}
	}

	class CollectionDebugView<TValue, TOther> : CollectionDebugView<TValue> {
		public CollectionDebugView(ICollection<TValue> list) : base(list) { }
	}

	sealed class LocalList_CollectionDebugView : CollectionDebugView<Local> {
		public LocalList_CollectionDebugView(LocalList list) : base(list) { }
	}

	sealed class ParameterList_CollectionDebugView : CollectionDebugView<Parameter> {
		public ParameterList_CollectionDebugView(ParameterList list) : base(list) { }
	}
}
