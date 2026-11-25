// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace dnlib.TestUtils {
	/// <summary>
	/// Runs a program with the Mono runtime (the <c>mono</c> command) in another process: Mono stops the process
	/// if it doesn't like the metadata of a file, so a test can't ask the Mono that runs the tests.
	/// </summary>
	public static class MonoRuntime {
		static readonly Lazy<string?> monoFile = new Lazy<string?>(FindMono);

		/// <summary>
		/// <c>true</c> if the <c>mono</c> command exists
		/// </summary>
		public static bool IsAvailable => monoFile.Value is not null;

		static string? FindMono() {
			var paths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
			return paths.Where(a => a.Length != 0).SelectMany(a => new[] { Path.Combine(a, "mono"), Path.Combine(a, "mono.exe") }).FirstOrDefault(File.Exists);
		}

		/// <summary>
		/// Runs a program and returns what it wrote to the standard output, or <c>null</c> if it didn't end with
		/// the exit code 0 (eg. Mono stopped it) or didn't end in time
		/// </summary>
		/// <param name="program">File name of the program (an EXE for .NET Framework)</param>
		/// <param name="arguments">Its arguments. They are passed as they are: no quotes are added.</param>
		public static string? Run(string program, string arguments) {
			if (monoFile.Value is null)
				throw new InvalidOperationException("The mono command wasn't found");
			var startInfo = new ProcessStartInfo(monoFile.Value, $"\"{program}\" {arguments}") {
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			using (var process = Process.Start(startInfo)!) {
				// Mono's crash report is long: read it on another thread, a full pipe would block Mono
				process.ErrorDataReceived += (sender, e) => { };
				process.BeginErrorReadLine();
				var output = process.StandardOutput.ReadToEndAsync();
				if (!process.WaitForExit(60000)) {
					try {
						process.Kill();
					}
					catch (InvalidOperationException) {
					}
					return null;
				}
				process.WaitForExit();
				return process.ExitCode == 0 ? output.Result : null;
			}
		}
	}
}
