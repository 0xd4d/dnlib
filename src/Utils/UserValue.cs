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

﻿using System;
using System.Diagnostics;
using dnlib.Threading;

namespace dnlib.Utils {
	/// <summary>
	/// Lazily returns the original value if the user hasn't overwritten the value
	/// </summary>
	/// <typeparam name="TValue">Value type</typeparam>
	[DebuggerDisplay("{value}")]
	struct UserValue<TValue> {
#if THREAD_SAFE
		Lock theLock;
#endif
		MFunc<TValue> readOriginalValue;
		TValue value;
		bool isUserValue;
		bool isValueInitialized;

#if THREAD_SAFE
		/// <summary>
		/// Sets the lock that protects the data
		/// </summary>
		public Lock Lock {
			set { theLock = value; }
		}
#endif

		/// <summary>
		/// Set a delegate instance that will return the original value
		/// </summary>
		public MFunc<TValue> ReadOriginalValue {
			set { readOriginalValue = value; }
		}

		/// <summary>
		/// Gets/sets the value
		/// </summary>
		/// <remarks>The getter returns the original value if the value hasn't been initialized.</remarks>
		public TValue Value {
			get {
#if THREAD_SAFE
				if (theLock != null) theLock.EnterWriteLock(); try {
#endif
				if (!isValueInitialized) {
					value = readOriginalValue();
					readOriginalValue = null;
					isValueInitialized = true;
				}
				return value;
#if THREAD_SAFE
				} finally { if (theLock != null) theLock.ExitWriteLock(); }
#endif
			}
			set {
#if THREAD_SAFE
				if (theLock != null) theLock.EnterWriteLock(); try {
#endif
				this.value = value;
				readOriginalValue = null;
				isUserValue = true;
				isValueInitialized = true;
#if THREAD_SAFE
				} finally { if (theLock != null) theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Returns <c>true</c> if the value has been initialized
		/// </summary>
		public bool IsValueInitialized {
#if THREAD_SAFE
			get {
				if (theLock != null)
					theLock.EnterReadLock();
				try {
					return isValueInitialized;
				}
				finally { if (theLock != null) theLock.ExitReadLock(); }
			}
#else
			get { return isValueInitialized; }
#endif
		}

		/// <summary>
		/// Returns <c>true</c> if the value was set by the user
		/// </summary>
		public bool IsUserValue {
#if THREAD_SAFE
			get {
				if (theLock != null)
					theLock.EnterReadLock();
				try {
					return isUserValue;
				}
				finally { if (theLock != null) theLock.ExitReadLock(); }
			}
#else
			get { return isUserValue; }
#endif
		}
	}
}
