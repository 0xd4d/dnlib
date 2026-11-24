// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace dnlib.TestUtils {
	/// <summary>
	/// Compiles C++/CLI code with Microsoft's C++ compiler (<c>cl /clr</c>). The result is a "mixed mode"
	/// assembly for .NET Framework: a PE file with metadata and IL code, and also native code, native data and
	/// the C runtime's startup code. No other compiler creates such files.
	/// </summary>
	/// <remarks>
	/// The compiler is only available on Windows, and only if Visual Studio (or its build tools) is installed
	/// with the component "C++/CLI support". It's found with <c>vswhere.exe</c>, which every Visual Studio
	/// installs at a fixed place.
	/// </remarks>
	public static class CppCliCompiler {
		const string RequiredComponent = "Microsoft.VisualStudio.Component.VC.CLI.Support";

		static readonly Lazy<string?> vcvarsFile = new Lazy<string?>(FindVcvars);

		/// <summary>
		/// <c>null</c> if the compiler can be used, else the reason why it can't
		/// </summary>
		public static string? WhyNotAvailable {
			get {
				if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					return "The C++/CLI compiler only exists on Windows";
				if (vcvarsFile.Value is null)
					return $"Visual Studio with the component {RequiredComponent} wasn't found";
				return null;
			}
		}

		// Returns the batch file that sets up the environment of the 64-bit compiler, or null
		static string? FindVcvars() {
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return null;
			var programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
			if (string.IsNullOrEmpty(programFiles))
				return null;
			var vswhere = Path.Combine(programFiles, "Microsoft Visual Studio", "Installer", "vswhere.exe");
			if (!File.Exists(vswhere))
				return null;
			int exitCode = Run(vswhere, $"-latest -products * -requires {RequiredComponent} -property installationPath", Environment.CurrentDirectory, out var output);
			var installationPath = output.Trim();
			if (exitCode != 0 || installationPath.Length == 0)
				return null;
			var vcvars = Path.Combine(installationPath, "VC", "Auxiliary", "Build", "vcvars64.bat");
			return File.Exists(vcvars) ? vcvars : null;
		}

		/// <summary>
		/// Compiles C++/CLI code to a 64-bit DLL. Throws if the compiler isn't available (see
		/// <see cref="WhyNotAvailable"/>) or reports an error.
		/// </summary>
		/// <param name="source">C++/CLI code</param>
		public static byte[] Compile(string source) {
			if (WhyNotAvailable is string reason)
				throw new InvalidOperationException(reason);

			using (var tempDirectory = new TempDirectory()) {
				File.WriteAllText(tempDirectory.GetPath("test.cpp"), source);
				// /clr: C++/CLI for .NET Framework, /LD: create a DLL, /Zi isn't used: no PDB is needed
				string command = $"\"\"{vcvarsFile.Value}\" >nul && cl /nologo /clr /LD /Fe:test.dll test.cpp\"";
				int exitCode = Run("cmd.exe", "/s /c " + command, tempDirectory.Path, out var output);
				var imageFile = tempDirectory.GetPath("test.dll");
				if (exitCode != 0 || !File.Exists(imageFile))
					throw new InvalidOperationException($"The C++/CLI compiler failed with exit code {exitCode}:{Environment.NewLine}{output}");
				return File.ReadAllBytes(imageFile);
			}
		}

		static int Run(string fileName, string arguments, string workingDirectory, out string output) {
			var startInfo = new ProcessStartInfo(fileName, arguments) {
				WorkingDirectory = workingDirectory,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			using (var process = Process.Start(startInfo)!) {
				// Read stderr on another thread: a full pipe would block the compiler
				string errors = "";
				process.ErrorDataReceived += (sender, e) => errors += e.Data + Environment.NewLine;
				process.BeginErrorReadLine();
				output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();
				output += errors;
				return process.ExitCode;
			}
		}
	}
}
