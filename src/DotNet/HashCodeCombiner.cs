// dnlib: See LICENSE.txt for more info

using System.Runtime.CompilerServices;

namespace dnlib.DotNet {
	/// <summary>
	/// xxHash32 helpers used to create hash codes
	/// </summary>
	static class HashCodeCombiner {
		// xxHash32 primes and steps, used by Utils.GetHashCode(byte[])
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
