// dnlib: See LICENSE.txt for more info

#nullable enable

using System;
using System.Runtime.Serialization;
using System.Threading;

namespace dnlib.Threading {
#if THREAD_SAFE
	[Serializable]
	class LockException : Exception {
		public LockException() {
		}

		public LockException(string msg)
			: base(msg) {
		}

		protected LockException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}

	/// <summary>
	/// Simple class using Monitor.Enter() and Monitor.Exit()
	/// and just like <c>ReaderWriterLockSlim</c> it prevents recursive locks. It doesn't support
	/// multiple readers. A reader lock is the same as a writer lock.
	/// </summary>
	sealed class Lock {
		// It locks itself to save one object per lock. That's safe since this class is internal
		// and instances are never given to code outside of dnlib.
		int recurseCount;

		/// <summary>
		/// Creates a new instance of this class
		/// </summary>
		/// <returns></returns>
		public static Lock Create() => new Lock();

		/// <summary>
		/// Constructor
		/// </summary>
		Lock() {
		}

		/// <summary>
		/// Enter read mode
		/// </summary>
		public void EnterReadLock() {
			Monitor.Enter(this);
			if (recurseCount != 0) {
				Monitor.Exit(this);
				throw new LockException("Recursive locks aren't supported");
			}
			recurseCount++;
		}

		/// <summary>
		/// Exit read mode
		/// </summary>
		public void ExitReadLock() {
			if (recurseCount <= 0)
				throw new LockException("Too many exit lock method calls");
			recurseCount--;
			Monitor.Exit(this);
		}

		/// <summary>
		/// Enter write mode
		/// </summary>
		public void EnterWriteLock() {
			Monitor.Enter(this);
			if (recurseCount != 0) {
				Monitor.Exit(this);
				throw new LockException("Recursive locks aren't supported");
			}
			recurseCount--;
		}

		/// <summary>
		/// Exit write mode
		/// </summary>
		public void ExitWriteLock() {
			if (recurseCount >= 0)
				throw new LockException("Too many exit lock method calls");
			recurseCount++;
			Monitor.Exit(this);
		}
	}
#endif
}
