// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using dnlib.Threading;

namespace dnlib.Utils {
	/// <summary>
	/// Lazily returns the original value if the user hasn't overwritten the value
	/// </summary>
	/// <typeparam name="TValue">Value type</typeparam>
	[DebuggerDisplay("{value}")]
	struct UserValue<TValue> where TValue : class {
#if THREAD_SAFE
		Lock theLock;
#endif
		Func<TValue>? readOriginalValue;
		TValue value;
		bool isUserValue;
		bool isValueInitialized;

#if THREAD_SAFE
		/// <summary>
		/// Sets the lock that protects the data
		/// </summary>
		public Lock Lock {
			set => theLock = value;
		}
#endif

		/// <summary>
		/// Set a delegate instance that will return the original value
		/// </summary>
		public Func<TValue> ReadOriginalValue {
			set => readOriginalValue = value;
		}

		/// <summary>
		/// Gets/sets the value
		/// </summary>
		/// <remarks>The getter returns the original value if the value hasn't been initialized.</remarks>
		public TValue Value {
			get {
#if THREAD_SAFE
				theLock?.EnterWriteLock(); try {
#endif
				if (!isValueInitialized) {
					value = readOriginalValue!();
					readOriginalValue = null;
					isValueInitialized = true;
				}
				return value;
#if THREAD_SAFE
				} finally { theLock?.ExitWriteLock(); }
#endif
			}
			set {
#if THREAD_SAFE
				theLock?.EnterWriteLock(); try {
#endif
				this.value = value;
				readOriginalValue = null;
				isUserValue = true;
				isValueInitialized = true;
#if THREAD_SAFE
				} finally { theLock?.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// Returns <c>true</c> if the value has been initialized
		/// </summary>
		public bool IsValueInitialized {
#if THREAD_SAFE
			get {
				theLock?.EnterReadLock();
				try {
					return isValueInitialized;
				}
				finally { theLock?.ExitReadLock(); }
			}
#else
			get => isValueInitialized;
#endif
		}

		/// <summary>
		/// Returns <c>true</c> if the value was set by the user
		/// </summary>
		public bool IsUserValue {
#if THREAD_SAFE
			get {
				theLock?.EnterReadLock();
				try {
					return isUserValue;
				}
				finally { theLock?.ExitReadLock(); }
			}
#else
			get => isUserValue;
#endif
		}
	}
}
