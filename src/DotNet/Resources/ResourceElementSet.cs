// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Resource element set
	/// </summary>
	public sealed class ResourceElementSet {
		readonly Dictionary<string, ResourceElement> dict = new Dictionary<string, ResourceElement>(StringComparer.Ordinal);

		/// <summary>
		/// Gets the number of elements in the set
		/// </summary>
		public int Count {
			get { return dict.Count; }
		}

		/// <summary>
		/// Gets all resource elements
		/// </summary>
		public IEnumerable<ResourceElement> ResourceElements {
			get { return dict.Values; }
		}

		/// <summary>
		/// Adds a new resource to the set, overwriting any existing resource
		/// </summary>
		/// <param name="elem"></param>
		public void Add(ResourceElement elem) {
			dict[elem.Name] = elem;
		}
	}
}
