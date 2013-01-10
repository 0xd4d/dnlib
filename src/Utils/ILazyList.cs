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

﻿using System;
using System.Collections.Generic;

namespace dnlib.Utils {
	/// <summary>
	/// Interface to access a lazily initialized list
	/// </summary>
	/// <typeparam name="TValue">Type to store in list</typeparam>
	public interface ILazyList<TValue> : IList<TValue> {
		/// <summary>
		/// Checks whether an element at <paramref name="index"/> has been initialized.
		/// </summary>
		/// <param name="index">Index of element</param>
		/// <returns><c>true</c> if the element has been initialized, <c>false</c> otherwise</returns>
		bool IsInitialized(int index);

		/// <summary>
		/// Gets all initialized elements
		/// </summary>
		IEnumerable<TValue> GetInitializedElements();
	}

	public static partial class Extensions {
		/// <summary>
		/// Disposes all initialized elements
		/// </summary>
		/// <typeparam name="TValue">Element type</typeparam>
		/// <param name="list">this</param>
		public static void DisposeAll<TValue>(this ILazyList<TValue> list) where TValue : IDisposable {
			for (int i = 0; i < list.Count; i++) {
				if (list.IsInitialized(i))
					list[i].Dispose();
			}
		}
	}
}
