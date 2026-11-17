// dnlib: See LICENSE.txt for more info

#nullable enable

using System.Runtime.CompilerServices;

namespace dnlib.DotNet {
	/// <summary>
	/// Combines values into one hash code. Uses <c>System.HashCode</c> if it's available, else xxHash32.
	/// </summary>
	/// <remarks>
	/// Summing or xor'ing shifted values is weak: rows with different values can easily get the same
	/// hash code, which makes dictionaries slow. These methods mix all bits of all values.
	/// The hash codes can change between processes and target frameworks, so don't store them.
	/// </remarks>
	static class HashCodeCombiner {
#if NETCOREAPP
		public static int Combine(uint v1, uint v2) => System.HashCode.Combine(v1, v2);
		public static int Combine(uint v1, uint v2, uint v3) => System.HashCode.Combine(v1, v2, v3);
		public static int Combine(uint v1, uint v2, uint v3, uint v4) => System.HashCode.Combine(v1, v2, v3, v4);
		public static int Combine(uint v1, uint v2, uint v3, uint v4, uint v5) => System.HashCode.Combine(v1, v2, v3, v4, v5);
		public static int Combine(uint v1, uint v2, uint v3, uint v4, uint v5, uint v6) => System.HashCode.Combine(v1, v2, v3, v4, v5, v6);
		public static int Combine(uint v1, uint v2, uint v3, uint v4, uint v5, uint v6, uint v7, uint v8, uint v9) {
			var hash = new System.HashCode();
			hash.Add(v1);
			hash.Add(v2);
			hash.Add(v3);
			hash.Add(v4);
			hash.Add(v5);
			hash.Add(v6);
			hash.Add(v7);
			hash.Add(v8);
			hash.Add(v9);
			return hash.ToHashCode();
		}
#else
		// xxHash32 with seed 0, where each value is one 32-bit input word. Inputs of 16 bytes or more
		// aren't split into xxHash32's 4 lanes; that only matters for speed, not quality.
		public static int Combine(uint v1, uint v2) =>
			Finish(Add(Add(Prime5 + 2 * 4, v1), v2));
		public static int Combine(uint v1, uint v2, uint v3) =>
			Finish(Add(Add(Add(Prime5 + 3 * 4, v1), v2), v3));
		public static int Combine(uint v1, uint v2, uint v3, uint v4) =>
			Finish(Add(Add(Add(Add(Prime5 + 4 * 4, v1), v2), v3), v4));
		public static int Combine(uint v1, uint v2, uint v3, uint v4, uint v5) =>
			Finish(Add(Add(Add(Add(Add(Prime5 + 5 * 4, v1), v2), v3), v4), v5));
		public static int Combine(uint v1, uint v2, uint v3, uint v4, uint v5, uint v6) =>
			Finish(Add(Add(Add(Add(Add(Add(Prime5 + 6 * 4, v1), v2), v3), v4), v5), v6));
		public static int Combine(uint v1, uint v2, uint v3, uint v4, uint v5, uint v6, uint v7, uint v8, uint v9) =>
			Finish(Add(Add(Add(Add(Add(Add(Add(Add(Add(Prime5 + 9 * 4, v1), v2), v3), v4), v5), v6), v7), v8), v9));

		[MethodImpl(AggressiveInlining)]
		static uint Add(uint hash, uint value) => unchecked(RotateLeft(hash + value * Prime3, 17) * Prime4);
#endif

		// xxHash32 primes and steps, also used by Utils.GetHashCode(byte[])
		internal const uint Prime1 = 2654435761U, Prime2 = 2246822519U, Prime3 = 3266489917U, Prime4 = 668265263U, Prime5 = 374761393U;

		// PERF: These must be inlined. If they're calls, hashing a byte[] is 2x slower.
		// The enum value doesn't exist in all target frameworks (it's ignored by older runtimes).
		const MethodImplOptions AggressiveInlining = (MethodImplOptions)256;

		[MethodImpl(AggressiveInlining)]
		internal static uint Round(uint acc, uint input) => unchecked(RotateLeft(acc + input * Prime2, 13) * Prime1);

		[MethodImpl(AggressiveInlining)]
		internal static uint RotateLeft(uint value, int count) => (value << count) | (value >> (32 - count));

		[MethodImpl(AggressiveInlining)]
		internal static int Finish(uint hash) {
			hash = unchecked((hash ^ (hash >> 15)) * Prime2);
			hash = unchecked((hash ^ (hash >> 13)) * Prime3);
			hash ^= hash >> 16;
			return (int)hash;
		}
	}
}
