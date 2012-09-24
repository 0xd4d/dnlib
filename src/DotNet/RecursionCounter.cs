using System;

namespace dot10.DotNet {
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
