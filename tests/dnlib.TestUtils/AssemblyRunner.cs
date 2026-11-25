// dnlib: See LICENSE.txt for more info

using System;
using System.Reflection;

namespace dnlib.TestUtils {
	/// <summary>
	/// Loads an assembly into the runtime that runs the tests and executes its code. If an assembly that dnlib
	/// wrote still runs and does what it did before, the runtime accepted the file and the code wasn't changed:
	/// a check of the writer that doesn't use dnlib's reader.
	/// </summary>
	public static class AssemblyRunner {
		/// <summary>
		/// Calls the entry point, which must be a <c>static int Main()</c> without parameters, and returns
		/// what it returns
		/// </summary>
		/// <param name="image">An EXE that only references the runtime's assemblies</param>
		public static int RunEntryPoint(byte[] image) {
			using (var loaded = new LoadedAssembly(image))
				return CallEntryPoint(loaded.Assembly);
		}

		static int CallEntryPoint(Assembly assembly) {
			var entryPoint = assembly.EntryPoint ?? throw new ArgumentException("The assembly has no entry point");
			try {
				return (int)entryPoint.Invoke(null, null)!;
			}
			catch (TargetInvocationException ex) when (ex.InnerException is not null) {
				// Show the test what the code threw (eg. InvalidProgramException) and not the reflection wrapper
				System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				throw;
			}
		}
	}
}
