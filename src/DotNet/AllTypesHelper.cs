using System.Collections.Generic;

namespace dot10.DotNet {
	struct AllTypesHelper {
		Dictionary<TypeDef, bool> visited;
		RecursionCounter recursionCounter;

		/// <summary>
		/// Gets a list of all types and nested types
		/// </summary>
		/// <param name="types">A list of types</param>
		public static IEnumerable<TypeDef> Types(IEnumerable<TypeDef> types) {
			var helper = new AllTypesHelper();
			helper.visited = new Dictionary<TypeDef, bool>();
			return helper.GetTypes(types);
		}

		IEnumerable<TypeDef> GetTypes(IEnumerable<TypeDef> types) {
			if (!recursionCounter.Increment()) {
			}
			else {
				foreach (var type in types) {
					if (visited.ContainsKey(type))
						continue;
					visited[type] = true;
					yield return type;
					if (type.NestedTypes.Count > 0) {
						foreach (var nested in GetTypes(type.NestedTypes))
							yield return nested;
					}
				}
				recursionCounter.Decrement();
			}
		}
	}
}
