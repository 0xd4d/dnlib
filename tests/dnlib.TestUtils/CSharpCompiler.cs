// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace dnlib.TestUtils {
	/// <summary>
	/// What kind of PDB file <see cref="CSharpCompiler"/> creates
	/// </summary>
	public enum PdbKind {
		/// <summary>
		/// No PDB
		/// </summary>
		None,

		/// <summary>
		/// A portable PDB in its own file
		/// </summary>
		Portable,

		/// <summary>
		/// A portable PDB that's stored in the PE file
		/// </summary>
		Embedded,

		/// <summary>
		/// A Windows PDB in its own file. The compiler can only create it on Windows.
		/// </summary>
		Windows,
	}

	/// <summary>
	/// Options for <see cref="CSharpCompiler.Compile(string, CSharpCompilerOptions?)"/>
	/// </summary>
	public sealed class CSharpCompilerOptions {
		public string AssemblyName { get; set; } = "TestAssembly";
		public OutputKind OutputKind { get; set; } = OutputKind.DynamicallyLinkedLibrary;
		public PdbKind PdbKind { get; set; } = PdbKind.None;
		public bool Optimize { get; set; } = true;
		public bool AllowUnsafe { get; set; } = true;
		public Platform Platform { get; set; } = Platform.AnyCpu;

		/// <summary>
		/// <c>true</c> to add the Win32 resources that the command line compiler adds by default: the version
		/// info, and the default manifest if it's an EXE
		/// </summary>
		public bool Win32Resources { get; set; }

		/// <summary>
		/// Embedded resources: name and content
		/// </summary>
		public Dictionary<string, byte[]> Resources { get; } = new Dictionary<string, byte[]>();

		/// <summary>
		/// Extra assemblies the code can use, eg. another test assembly
		/// </summary>
		public List<byte[]> References { get; } = new List<byte[]>();
	}

	/// <summary>
	/// Compiles C# code to an assembly in memory, so a test's input is C# code in the test
	/// and not a binary file in the repo.
	/// The assembly references the runtime that runs the tests, so it can also be loaded and executed.
	/// </summary>
	public static class CSharpCompiler {
		/// <summary>
		/// Compiles C# code. Throws if the code has errors.
		/// </summary>
		/// <param name="source">C# code</param>
		/// <param name="options">Options or <c>null</c> to create a DLL without a PDB</param>
		public static CompiledAssembly Compile(string source, CSharpCompilerOptions? options = null) {
			options ??= new CSharpCompilerOptions();

			// The encoding is required to create a PDB (it stores a checksum of the source file)
			var sourceText = SourceText.From(source, Encoding.UTF8);
			var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
			var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, parseOptions, path: options.AssemblyName + ".cs");

			var compilationOptions = new CSharpCompilationOptions(
				options.OutputKind,
				optimizationLevel: options.Optimize ? OptimizationLevel.Release : OptimizationLevel.Debug,
				allowUnsafe: options.AllowUnsafe,
				platform: options.Platform,
				deterministic: true);

			var references = runtimeReferences.Value.ToList();
			foreach (var reference in options.References)
				references.Add(MetadataReference.CreateFromImage(reference));

			var compilation = CSharpCompilation.Create(options.AssemblyName, new[] { syntaxTree }, references, compilationOptions);

			var imageStream = new MemoryStream();
			MemoryStream? pdbStream = null;
			EmitOptions? emitOptions = null;
			switch (options.PdbKind) {
			case PdbKind.Portable:
				pdbStream = new MemoryStream();
				emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb);
				break;
			case PdbKind.Embedded:
				emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded);
				break;
			case PdbKind.Windows:
				pdbStream = new MemoryStream();
				emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.Pdb);
				break;
			}

			var resources = new List<ResourceDescription>();
			foreach (var resource in options.Resources) {
				var content = resource.Value;
				resources.Add(new ResourceDescription(resource.Key, () => new MemoryStream(content), isPublic: true));
			}

			Stream? win32Resources = null;
			if (options.Win32Resources)
				win32Resources = compilation.CreateDefaultWin32Resources(versionResource: true, noManifest: false, manifestContents: null, iconInIcoFormat: null);

			var result = compilation.Emit(imageStream, pdbStream, win32Resources: win32Resources, manifestResources: resources, options: emitOptions);
			if (!result.Success) {
				var errors = result.Diagnostics.Where(a => a.Severity == DiagnosticSeverity.Error).Select(a => a.ToString());
				throw new InvalidOperationException("The test's C# code doesn't compile:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
			}
			return new CompiledAssembly(imageStream.ToArray(), pdbStream?.ToArray());
		}

		static readonly Lazy<List<MetadataReference>> runtimeReferences = new Lazy<List<MetadataReference>>(GetRuntimeReferences);

		// All assemblies of the runtime that runs the tests: .NET's shared framework, or mscorlib + the System
		// assemblies of .NET Framework/Mono.
		static List<MetadataReference> GetRuntimeReferences() {
			var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
			var references = new List<MetadataReference>();
#if NET
			foreach (var file in Directory.GetFiles(runtimeDirectory, "*.dll")) {
				if (IsManagedAssembly(file))
					references.Add(MetadataReference.CreateFromFile(file));
			}
#else
			foreach (var name in new[] { "mscorlib.dll", "System.dll", "System.Core.dll" })
				references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, name)));
#endif
			return references;
		}

#if NET
		// The shared framework directory also has native libraries
		static bool IsManagedAssembly(string file) {
			try {
				System.Reflection.AssemblyName.GetAssemblyName(file);
				return true;
			}
			catch (BadImageFormatException) {
				return false;
			}
		}
#endif
	}
}
