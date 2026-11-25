// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace dnlib.TestUtils {
	/// <summary>
	/// Real assemblies to test with: the assemblies of the runtime that runs the tests. They're big, were
	/// created by several compilers and tools (C#, VB, the IL linker, ReadyToRun) and are on every machine
	/// that can run the tests, so they don't have to be in the repo.
	/// </summary>
	public static class FrameworkCorpus {
		/// <summary>
		/// Name of the environment variable with more directories (separated by the OS's path separator, ie. ';'
		/// on Windows and ':' elsewhere). All assemblies in them and in their sub directories are added to
		/// <see cref="GetAllFiles"/>.
		/// </summary>
		public const string ExtraDirectoriesVariable = "DNLIB_TEST_CORPUS";

		/// <summary>
		/// The directory of the runtime's core library
		/// </summary>
		public static string RuntimeDirectory => Path.GetDirectoryName(typeof(object).Assembly.Location)!;

		// A few assemblies with different content, for tests that run on every build. Not all of them exist in
		// all runtimes.
		static readonly string[] sampleFileNames = new[] {
			// The core library: the biggest one, uses every feature
			"System.Private.CoreLib.dll",
			"mscorlib.dll",
			// Generics, lambdas, iterators
			"System.Linq.dll",
			"System.Core.dll",
			"System.Collections.dll",
			// Big methods and big switch instructions
			"System.Text.RegularExpressions.dll",
			"System.Linq.Expressions.dll",
			// Compiled by the VB compiler
			"Microsoft.VisualBasic.Core.dll",
			"Microsoft.VisualBasic.dll",
			// P/Invoke methods and marshalling info
			"System.Net.Sockets.dll",
			"System.dll",
			// Only type forwarders
			"System.Runtime.dll",
			"netstandard.dll",
		};

		/// <summary>
		/// Returns the full paths of a few assemblies of the runtime, see the list in this class
		/// </summary>
		public static List<string> GetSampleFiles() {
			var files = new List<string>();
			foreach (var fileName in sampleFileNames) {
				var file = Path.Combine(RuntimeDirectory, fileName);
				if (File.Exists(file))
					files.Add(file);
			}
			return files;
		}

		/// <summary>
		/// Returns the full paths of all assemblies of the runtime and of the directories in the
		/// <see cref="ExtraDirectoriesVariable"/> environment variable
		/// </summary>
		public static List<string> GetAllFiles() {
			var files = new List<string>();
			AddAssemblies(files, RuntimeDirectory, SearchOption.TopDirectoryOnly);
			var extraDirectories = Environment.GetEnvironmentVariable(ExtraDirectoriesVariable);
			if (!string.IsNullOrEmpty(extraDirectories)) {
				foreach (var directory in extraDirectories.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
					AddAssemblies(files, directory, SearchOption.AllDirectories);
			}
			files.Sort(StringComparer.Ordinal);
			return files;
		}

		static void AddAssemblies(List<string> files, string directory, SearchOption searchOption) {
			foreach (var pattern in new[] { "*.dll", "*.exe" }) {
				foreach (var file in Directory.GetFiles(directory, pattern, searchOption)) {
					if (IsAssembly(file))
						files.Add(file);
				}
			}
		}

		// The directories also have native libraries
		static bool IsAssembly(string file) {
			try {
				AssemblyName.GetAssemblyName(file);
				return true;
			}
			catch (BadImageFormatException) {
				return false;
			}
			catch (IOException) {
				return false;
			}
		}
	}
}
