// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

		// Returns the batch file that sets up the environment of the compilers (its argument says which one), or null
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
			var vcvars = Path.Combine(installationPath, "VC", "Auxiliary", "Build", "vcvarsall.bat");
			return File.Exists(vcvars) ? vcvars : null;
		}

		/// <summary>
		/// Compiles C++/CLI code to a DLL. Throws if the compiler isn't available (see
		/// <see cref="WhyNotAvailable"/>) or reports an error.
		/// </summary>
		/// <param name="source">C++/CLI code</param>
		/// <param name="is32Bit"><c>true</c> to create a 32-bit (x86) DLL, <c>false</c> to create a 64-bit (x64) DLL</param>
		public static byte[] Compile(string source, bool is32Bit = false) {
			if (WhyNotAvailable is string reason)
				throw new InvalidOperationException(reason);
			return Compile(source, is32Bit ? "x64_x86" : "x64", "");
		}

		static readonly Lazy<string?> arm64LibraryDirectory = new Lazy<string?>(FindArm64LibraryDirectory);

		/// <summary>
		/// <c>null</c> if the compiler can create ARM64 files (see <see cref="CompileArm64(string)"/>), else the
		/// reason why it can't. It's another component of Visual Studio than the x64/x86 compiler, and the
		/// libraries are in the .NET Framework 4.8.1 SDK, the first version for ARM64.
		/// </summary>
		public static string? WhyArm64NotAvailable {
			get {
				if (WhyNotAvailable is string reason)
					return reason;
				if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.OSArchitecture != Architecture.X64)
					return "The ARM64 C++/CLI compiler that's used is the one that runs on x64 Windows";
				var tools = Path.Combine(Path.GetDirectoryName(vcvarsFile.Value!)!, "..", "..", "Tools", "MSVC");
				if (!Directory.Exists(tools) || Directory.GetFiles(tools, "cl.exe", SearchOption.AllDirectories).All(a => !a.EndsWith(Path.Combine("Hostx64", "arm64", "cl.exe"), StringComparison.OrdinalIgnoreCase)))
					return "Visual Studio's component Microsoft.VisualStudio.Component.VC.Tools.ARM64 wasn't found";
				if (arm64LibraryDirectory.Value is null)
					return "The .NET Framework 4.8.1 SDK (Visual Studio's component Microsoft.Net.Component.4.8.1.SDK) wasn't found";
				return null;
			}
		}

		// The directory with the ARM64 mscoree.lib, or null
		static string? FindArm64LibraryDirectory() {
			var programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
			if (string.IsNullOrEmpty(programFiles))
				return null;
			var directory = Path.Combine(programFiles, "Windows Kits", "NETFXSDK", "4.8.1", "Lib", "um", "arm64");
			return File.Exists(Path.Combine(directory, "mscoree.lib")) ? directory : null;
		}

		/// <summary>
		/// Compiles C++/CLI code to an ARM64 DLL. Throws if the compiler isn't available (see
		/// <see cref="WhyArm64NotAvailable"/>) or reports an error. The file can only be loaded by an ARM64 Windows.
		/// </summary>
		/// <param name="source">C++/CLI code</param>
		public static byte[] CompileArm64(string source) {
			if (WhyArm64NotAvailable is string reason)
				throw new InvalidOperationException(reason);
			// The environment has the libraries of an older SDK if one is installed, and it has no ARM64 libraries
			return Compile(source, "x64_arm64", $" /link /LIBPATH:\"{arm64LibraryDirectory.Value}\"");
		}

		// compiler: the argument of vcvarsall.bat: the compiler's processor and the processor of its files
		static byte[] Compile(string source, string compiler, string linkerOptions) {
			using (var tempDirectory = new TempDirectory()) {
				File.WriteAllText(tempDirectory.GetPath("test.cpp"), source);
				// /clr: C++/CLI for .NET Framework, /LD: create a DLL, /Zi isn't used: no PDB is needed
				string command = $"\"\"{vcvarsFile.Value}\" {compiler} >nul && cl /nologo /clr /LD /Fe:test.dll test.cpp{linkerOptions}\"";
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
