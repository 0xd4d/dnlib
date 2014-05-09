/*
    Copyright (C) 2012-2014 de4dot@gmail.com

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

﻿using System.Diagnostics;
using System.Threading;
using dnlib.DotNet;

namespace dnlib.Utils {
	/// <summary>
	/// A readonly list that gets initialized lazily
	/// </summary>
	/// <typeparam name="T">Any class type</typeparam>
	[DebuggerDisplay("Count = {Length}")]
	sealed class SimpleLazyList<T> where T : class {
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		readonly T[] elements;
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
				if (elements[index] == null)
					Interlocked.CompareExchange(ref elements[index], readElementByRID(index + 1), null);
				return elements[index];
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Length of the list</param>
		/// <param name="readElementByRID">Delegate instance that lazily reads an element. It might
		/// be called more than once for each <c>rid</c> in rare cases. It must never return
		/// <c>null</c>.</param>
		public SimpleLazyList(uint length, MFunc<uint, T> readElementByRID) {
			this.length = length;
			this.readElementByRID = readElementByRID;
			this.elements = new T[length];
		}
	}

	/// <summary>
	/// A readonly list that gets initialized lazily
	/// </summary>
	/// <typeparam name="T">Any class type</typeparam>
	[DebuggerDisplay("Count = {Length}")]
	sealed class SimpleLazyList2<T> where T : class, IContainsGenericParameter {
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		readonly T[] elements;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly MFunc<uint, GenericParamContext, T> readElementByRID;
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
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>The element or <c>null</c> if <paramref name="index"/> is invalid</returns>
		public T this[uint index, GenericParamContext gpContext] {
			get {
				if (index >= length)
					return null;
				if (elements[index] == null) {
					var elem = readElementByRID(index + 1, gpContext);
					// Don't cache it if it contains GPs since each GP could hold a reference
					// to the type/method context. These GPs can't be shared.
					if (elem.ContainsGenericParameter)
						return elem;
					Interlocked.CompareExchange(ref elements[index], elem, null);
				}
				return elements[index];
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Length of the list</param>
		/// <param name="readElementByRID">Delegate instance that lazily reads an element. It might
		/// be called more than once for each <c>rid</c>. It must never return <c>null</c>.</param>
		public SimpleLazyList2(uint length, MFunc<uint, GenericParamContext, T> readElementByRID) {
			this.length = length;
			this.readElementByRID = readElementByRID;
			this.elements = new T[length];
		}
	}
}
