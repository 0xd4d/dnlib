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

namespace dnlib.DotNet {
	[Serializable]
	class InfiniteRecursionException : Exception {
		/// <summary>
		/// Default constructor
		/// </summary>
		public InfiniteRecursionException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="msg">Error message</param>
		public InfiniteRecursionException(string msg)
			: base(msg) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="msg">Error message</param>
		/// <param name="innerException">Other exception</param>
		public InfiniteRecursionException(string msg, Exception innerException)
			: base(msg, innerException) {
		}
	}

	struct RecursionCounter {
		/// <summary>
		/// Max recursion count. If this is reached, we won't continue, and will use a default value.
		/// </summary>
		public const int MAX_RECURSION_COUNT = 100;
		int counter;

		/// <summary>
		/// Gets the recursion counter
		/// </summary>
		public int Counter {
			get { return counter; }
		}

		/// <summary>
		/// Increments <see cref="counter"/> if it's not too high. <c>ALL</c> instance methods
		/// that can be called recursively must call this method and <see cref="Decrement"/>
		/// (if this method returns <c>true</c>)
		/// </summary>
		/// <returns><c>true</c> if it was incremented and caller can continue, <c>false</c> if
		/// it was <c>not</c> incremented and the caller must return to its caller.</returns>
		public bool Increment() {
			if (counter >= MAX_RECURSION_COUNT)
				return false;
			counter++;
			return true;
		}

		/// <summary>
		/// Increments <see cref="counter"/> if it's not too high. <c>ALL</c> instance methods
		/// that can be called recursively must call this method and <see cref="Decrement"/>
		/// (if this method returns <c>true</c>)
		/// </summary>
		/// <exception cref="InfiniteRecursionException"><see cref="IncrementThrow"/>
		/// has been called too many times</exception>
		public void IncrementThrow() {
			if (counter >= MAX_RECURSION_COUNT)
				throw new InfiniteRecursionException("Infinite recursion");
			counter++;
		}

		/// <summary>
		/// Must be called before returning to caller if <see cref="Increment"/>
		/// returned <c>true</c> or if <see cref="IncrementThrow"/> didn't throw an
		/// exception.
		/// </summary>
		public void Decrement() {
#if DEBUG
			if (counter <= 0)
				throw new InvalidOperationException("recursionCounter <= 0");
#endif
			counter--;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return counter.ToString();
		}
	}
}
