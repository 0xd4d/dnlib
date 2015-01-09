// dnlib: See LICENSE.txt for more info

using System;
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
	}

	/// <summary>
	/// Simple class using <see cref="Monitor.Enter"/> and <see cref="Monitor.Exit"/>
	/// and just like <c>ReaderWriterLockSlim</c> it prevents recursive locks. It doesn't support
	/// multiple readers. A reader lock is the same as a writer lock.
	/// </summary>
	class Lock {
		readonly object lockObj;
		int recurseCount;

		/// <summary>
		/// Creates a new instance of this class
		/// </summary>
		/// <returns></returns>
		public static Lock Create() {
			return new Lock();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		Lock() {
			this.lockObj = new object();
			this.recurseCount = 0;
		}

		/// <summary>
		/// Enter read mode
		/// </summary>
		public void EnterReadLock() {
			Monitor.Enter(lockObj);
			if (recurseCount != 0) {
				Monitor.Exit(lockObj);
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
			Monitor.Exit(lockObj);
		}

		/// <summary>
		/// Enter write mode
		/// </summary>
		public void EnterWriteLock() {
			Monitor.Enter(lockObj);
			if (recurseCount != 0) {
				Monitor.Exit(lockObj);
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
			Monitor.Exit(lockObj);
		}
	}
#endif
}
