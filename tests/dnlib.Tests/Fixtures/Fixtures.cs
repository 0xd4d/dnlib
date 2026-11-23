// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.TestUtils;
using Microsoft.CodeAnalysis;

namespace dnlib.Tests.Fixtures {
	/// <summary>
	/// Test assemblies that are used by many tests. Their source code is in the <c>Sources</c> directory.
	/// They're compiled when they're first used.
	/// </summary>
	static class Fixtures {
		/// <summary>
		/// <c>Sources/Features.cs</c>: an EXE that uses as many metadata features as C# can express and whose
		/// <c>Main()</c> returns 0 if its code works
		/// </summary>
		public static CompiledAssembly Features => features.Value;
		static readonly Lazy<CompiledAssembly> features = new Lazy<CompiledAssembly>(() =>
			CSharpCompiler.Compile(ReadSource("Features.cs"), new CSharpCompilerOptions {
				AssemblyName = "Features",
				OutputKind = OutputKind.ConsoleApplication,
				PdbKind = PdbKind.Portable,
			}));

		/// <summary>
		/// Returns the text of a file in the <c>Sources</c> directory
		/// </summary>
		public static string ReadSource(string fileName) {
			// The files are embedded in the test assembly, see dnlib.Tests.csproj
			var resourceName = "Fixtures/Sources/" + fileName;
			using (var stream = typeof(Fixtures).Assembly.GetManifestResourceStream(resourceName)) {
				if (stream is null)
					throw new ArgumentException($"There's no fixture source file called {fileName}");
				using (var reader = new StreamReader(stream))
					return reader.ReadToEnd();
			}
		}
	}
}
