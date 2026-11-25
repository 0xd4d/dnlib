// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;

namespace dnlib.TestUtils {
	/// <summary>
	/// Options of <see cref="VisualBasicCompiler"/>
	/// </summary>
	public sealed class VisualBasicCompilerOptions {
		/// <summary>
		/// Name of the assembly and of the source file (+ <c>.vb</c>)
		/// </summary>
		public string AssemblyName { get; set; } = "TestAssembly";

		/// <summary>
		/// DLL (the default) or EXE
		/// </summary>
		public OutputKind OutputKind { get; set; } = OutputKind.DynamicallyLinkedLibrary;

		/// <summary>
		/// The PDB to create, the default is none
		/// </summary>
		public PdbKind PdbKind { get; set; } = PdbKind.None;

		/// <summary>
		/// <c>true</c> (the default) to optimize the code
		/// </summary>
		public bool Optimize { get; set; } = true;
	}

	/// <summary>
	/// Compiles Visual Basic code to an assembly in memory. The VB compiler creates metadata and debug info
	/// that the C# compiler doesn't: modules, static locals, <c>Date</c> constants, other custom debug info.
	/// </summary>
	public static class VisualBasicCompiler {
		/// <summary>
		/// Compiles Visual Basic code. Throws if the code has errors.
		/// </summary>
		/// <param name="source">Visual Basic code</param>
		/// <param name="options">Options or <c>null</c> to create a DLL without a PDB</param>
		public static CompiledAssembly Compile(string source, VisualBasicCompilerOptions? options = null) {
			options ??= new VisualBasicCompilerOptions();

			// The encoding is required to create a PDB (it stores a checksum of the source file)
			var sourceText = SourceText.From(source, Encoding.UTF8);
			var syntaxTree = VisualBasicSyntaxTree.ParseText(sourceText, new VisualBasicParseOptions(LanguageVersion.Latest), path: options.AssemblyName + ".vb");

			var compilationOptions = new VisualBasicCompilationOptions(
				options.OutputKind,
				optimizationLevel: options.Optimize ? OptimizationLevel.Release : OptimizationLevel.Debug,
				optionStrict: OptionStrict.On,
				// The helper classes of the VB runtime that the code needs are added to the assembly, so it
				// doesn't reference Microsoft.VisualBasic.dll
				embedVbCoreRuntime: true,
				deterministic: true);

			var compilation = VisualBasicCompilation.Create(options.AssemblyName, new[] { syntaxTree }, CSharpCompiler.RuntimeReferences, compilationOptions);

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

			var result = compilation.Emit(imageStream, pdbStream, options: emitOptions);
			if (!result.Success) {
				var errors = result.Diagnostics.Where(a => a.Severity == DiagnosticSeverity.Error).Select(a => a.ToString());
				throw new InvalidOperationException("The test's Visual Basic code doesn't compile:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
			}
			return new CompiledAssembly(imageStream.ToArray(), pdbStream?.ToArray());
		}
	}
}
