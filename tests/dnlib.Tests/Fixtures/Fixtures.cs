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
		/// <c>Sources/Features.cs</c>: an EXE that uses many metadata features that C# can express (see the file) and whose
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
		/// <c>Sources/Features.cs</c> compiled with a Windows PDB. Only works on Windows.
		/// The code is the same as in <see cref="Features"/>.
		/// </summary>
		public static CompiledAssembly FeaturesWithWindowsPdb => featuresWithWindowsPdb.Value;
		static readonly Lazy<CompiledAssembly> featuresWithWindowsPdb = new Lazy<CompiledAssembly>(() =>
			CSharpCompiler.Compile(ReadSource("Features.cs"), new CSharpCompilerOptions {
				AssemblyName = "Features",
				OutputKind = OutputKind.ConsoleApplication,
				PdbKind = PdbKind.Windows,
			}));

		/// <summary>
		/// <c>Sources/VbFeatures.vb</c> compiled with the Visual Basic compiler: an EXE with a portable PDB.
		/// Its <c>Main</c> method returns 0 if everything works.
		/// </summary>
		public static CompiledAssembly VbFeatures => vbFeatures.Value;
		static readonly Lazy<CompiledAssembly> vbFeatures = new Lazy<CompiledAssembly>(() =>
			VisualBasicCompiler.Compile(ReadSource("VbFeatures.vb"), new VisualBasicCompilerOptions {
				AssemblyName = "VbFeatures",
				OutputKind = OutputKind.ConsoleApplication,
				PdbKind = PdbKind.Portable,
			}));

		/// <summary>
		/// <c>Sources/Exports.il</c>: a DLL that isn't IL only. It has an exported method, a vtable fixup and
		/// a field with data in the PE file.
		/// </summary>
		public static CompiledAssembly Exports => exports.Value;
		static readonly Lazy<CompiledAssembly> exports = new Lazy<CompiledAssembly>(() => IlAssembler.Assemble(ReadSource("Exports.il")));

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
