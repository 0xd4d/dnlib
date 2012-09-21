using System;

namespace dot10.DotNet {
	struct RecursionCounter {
		/// <summary>
		/// Max recursion count. If this is reached, we won't continue, and will use a default value.
		/// </summary>
		const int MAX_RECURSION_COUNT = 100;
		int counter;

		/// <summary>
		/// Increments <see cref="counter"/> if it's not too high. <c>ALL</c> instance methods
		/// that can be called recursively must call this method and <see cref="DecrementRecursionCounter"/>
		/// (if this method returns <c>true</c>)
		/// </summary>
		/// <returns><c>true</c> if it was incremented and caller can continue, <c>false</c> if
		/// it was <c>not</c> incremented and the caller must return to its caller.</returns>
		public bool IncrementRecursionCounter() {
			if (counter >= MAX_RECURSION_COUNT)
				return false;
			counter++;
			return true;
		}

		/// <summary>
		/// Must be called before returning to caller if <see cref="IncrementRecursionCounter"/>
		/// returned <c>true</c>
		/// </summary>
		public void DecrementRecursionCounter() {
#if DEBUG
			if (counter <= 0)
				throw new InvalidOperationException("recursionCounter <= 0");
#endif
			counter--;
		}
	}
}
