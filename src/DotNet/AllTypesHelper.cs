/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System.Collections.Generic;

namespace dnlib.DotNet {
	/// <summary>
	/// Returns types without getting stuck in an infinite loop
	/// </summary>
	public struct AllTypesHelper {
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
