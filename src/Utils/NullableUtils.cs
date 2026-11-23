// dnlib: See LICENSE.txt for more info

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// These methods are annotated in .NET but not in .NET Framework and .NET Standard 2.0. Call these instead so
// the compiler knows the nullable state of the argument in all builds.

namespace System {
	static class String2 {
		[MethodImpl((MethodImplOptions)256)] // AggressiveInlining
		public static bool IsNullOrEmpty([NotNullWhen(false)] string? value) => string.IsNullOrEmpty(value);
	}
}

namespace System.Diagnostics {
	static class Debug2 {
		[Conditional("DEBUG")]
		public static void Assert([DoesNotReturnIf(false)] bool condition) => Debug.Assert(condition);

		[Conditional("DEBUG")]
		public static void Assert([DoesNotReturnIf(false)] bool condition, string? message) => Debug.Assert(condition, message);
	}
}

namespace System.IO {
	static class File2 {
		[MethodImpl((MethodImplOptions)256)] // AggressiveInlining
		public static bool Exists([NotNullWhen(true)] string? path) => File.Exists(path);
	}
}
