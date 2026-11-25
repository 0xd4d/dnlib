// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace dnlib.TestUtils {
	/// <summary>
	/// Options for <see cref="IlAssembler.Assemble(string, IlAssemblerOptions?)"/>
	/// </summary>
	public sealed class IlAssemblerOptions {
		/// <summary>
		/// <c>true</c> to create a DLL, <c>false</c> to create an EXE
		/// </summary>
		public bool Dll { get; set; } = true;

		/// <summary>
		/// <c>true</c> to also create a portable PDB
		/// </summary>
		public bool Pdb { get; set; }

		/// <summary>
		/// Extra ilasm options, eg. <c>/PE64</c>, <c>/X64</c>, <c>/MDV=v1.1.4322</c>, <c>/MSV=1.0</c>, <c>/FLAGS=2</c>.
		/// Run <c>ilasm</c> without arguments to see all of them.
		/// </summary>
		public List<string> ExtraArguments { get; } = new List<string>();
	}

	/// <summary>
	/// Assembles IL code with Microsoft's ilasm. It's used when the C# compiler can't create what a test needs,
	/// eg. global methods, custom modifiers, vararg methods, unusual exception handlers, exported methods or
	/// old metadata versions. The test's input is then IL code in the repo and not a binary file.
	/// </summary>
	/// <remarks>
	/// ilasm comes from the <c>Microsoft.NETCore.ILAsm</c> NuGet package (see dnlib.TestUtils.csproj).
	/// </remarks>
	public static class IlAssembler {
		/// <summary>
		/// Assembles IL code. Throws if ilasm reports an error.
		/// </summary>
		/// <param name="source">IL code</param>
		/// <param name="options">Options or <c>null</c> to create a DLL without a PDB</param>
		public static CompiledAssembly Assemble(string source, IlAssemblerOptions? options = null) {
			options ??= new IlAssemblerOptions();

			using (var tempDirectory = new TempDirectory()) {
				var sourceFile = tempDirectory.GetPath("test.il");
				var imageFile = tempDirectory.GetPath(options.Dll ? "test.dll" : "test.exe");
				var pdbFile = tempDirectory.GetPath("test.pdb");
				File.WriteAllText(sourceFile, source);

				// ilasm accepts '-' options on all operating systems, but '/' options only on Windows
				var arguments = new List<string> {
					"-NOLOGO",
					"-QUIET",
					// The same input always gives the same file
					"-DET",
					options.Dll ? "-DLL" : "-EXE",
					"-OUTPUT=" + imageFile,
				};
				if (options.Pdb)
					arguments.Add("-PDB");
				foreach (var argument in options.ExtraArguments)
					arguments.Add("-" + argument.TrimStart('/', '-'));
				arguments.Add(sourceFile);

				var output = Run(arguments, out int exitCode);
				if (exitCode != 0 || !File.Exists(imageFile))
					throw new InvalidOperationException("ilasm failed:" + Environment.NewLine + output);

				var image = File.ReadAllBytes(imageFile);
				var pdb = options.Pdb && File.Exists(pdbFile) ? File.ReadAllBytes(pdbFile) : null;
				return new CompiledAssembly(image, pdb);
			}
		}

		static string Run(List<string> arguments, out int exitCode) {
			var startInfo = new ProcessStartInfo(GetIlasmPath()) {
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				Arguments = QuoteArguments(arguments),
			};
			using (var process = Process.Start(startInfo)!) {
				// ilasm prints little, so reading one stream after the other can't block it
				var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
				process.WaitForExit();
				exitCode = process.ExitCode;
				return output;
			}
		}

		static string QuoteArguments(List<string> arguments) {
			var quoted = new List<string>();
			foreach (var argument in arguments) {
				if (argument.Contains("\""))
					throw new ArgumentException("An ilasm argument can't contain a quote: " + argument);
				quoted.Add("\"" + argument + "\"");
			}
			return string.Join(" ", quoted);
		}

		static string GetIlasmPath() {
			var path = BuildUnderTest.GetMetadata("IlasmPath");
			if (!File.Exists(path))
				throw new FileNotFoundException("ilasm wasn't found. Restore the NuGet packages of dnlib.TestUtils.csproj.", path);
			MakeExecutable(path);
			return path;
		}

		// NuGet doesn't keep the file mode when it extracts a package, so the file isn't executable on Unix
		static void MakeExecutable(string path) {
#if NET
			if (!OperatingSystem.IsWindows()) {
				var mode = File.GetUnixFileMode(path);
				var executable = mode | UnixFileMode.UserExecute;
				if (mode != executable)
					File.SetUnixFileMode(path, executable);
			}
#else
			// .NET Framework only runs on Windows. Under Mono, run the net10.0 tests once first: they set the mode.
#endif
		}
	}
}
