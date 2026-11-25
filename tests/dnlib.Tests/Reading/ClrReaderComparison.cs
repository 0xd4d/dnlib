// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// What the tests need that compare dnlib's metadata reader with the reader of the runtime that runs
	/// the tests
	/// </summary>
	static class ClrReaderComparison {
		// A stream that's only there so that the tests have room for more or bigger streams
		sealed class PaddingHeap : IHeap {
			public string Name => PaddingStreamName;
			public bool IsEmpty => false;
			public FileOffset FileOffset { get; private set; }
			public RVA RVA { get; private set; }
			public void SetReadOnly() { }
			public void SetOffset(FileOffset offset, RVA rva) {
				FileOffset = offset;
				RVA = rva;
			}
			public uint GetFileLength() => 0x4000;
			public uint GetVirtualSize() => GetFileLength();
			public uint CalculateAlignment() => 0;
			public void WriteTo(DataWriter writer) => writer.WriteBytes(new byte[0x4000]);
		}

		public const string PaddingStreamName = "#Pad";

		/// <summary>
		/// Compiles the code and writes it with a big <see cref="PaddingStreamName"/> stream. A test removes
		/// that stream with <see cref="MetadataBlock.RemoveStream"/> and can then add 16 KB to the metadata.
		/// </summary>
		public static byte[] CreateBaseImage(string source, Action<ModuleWriterOptions>? changeOptions = null, CSharpCompilerOptions? compilerOptions = null) {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(source, compilerOptions).Image)) {
				var options = new ModuleWriterOptions(module);
				changeOptions?.Invoke(options);
				options.MetadataOptions.CustomHeaps.Add(new PaddingHeap());
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		}

		public static bool IsMono => Type.GetType("Mono.Runtime") is not null;
		public static bool IsDotNet => typeof(object).Assembly.GetName().Name == "System.Private.CoreLib";

		/// <summary>
		/// true if the runtime isn't asked: Mono stops the process if it doesn't like the metadata. And with a
		/// profiler (the tests run with one when the code coverage is measured) the CLR reads metadata with
		/// its reader for metadata that can be changed, which has other rules than its normal reader: dnlib
		/// reads a file like the CLR without a profiler.
		/// </summary>
		public static bool IsRuntimeNotAsked => IsMono ||
			Environment.GetEnvironmentVariable("CORECLR_ENABLE_PROFILING") == "1" ||
			Environment.GetEnvironmentVariable("COR_ENABLE_PROFILING") == "1";

		/// <summary>
		/// Checks what dnlib read and what the runtime reads. .NET must do what the test expects, another CLR
		/// (.NET Framework) may do something else: the test is then skipped with a message that says what it
		/// did. See <see cref="IsRuntimeNotAsked"/> for when the runtime isn't asked.
		/// </summary>
		/// <param name="expectedDnlib">What dnlib must read</param>
		/// <param name="dnlib">What dnlib read</param>
		/// <param name="expectedRuntime">What .NET reads. <c>A|B</c> if .NET versions read A or B.</param>
		/// <param name="readWithTheRuntime">Reads the same with the runtime</param>
		public static void AssertRead(string expectedDnlib, string dnlib, string expectedRuntime, Func<string> readWithTheRuntime) {
			Assert.Equal(expectedDnlib, dnlib);
			if (IsRuntimeNotAsked)
				return;
			string runtime = ReadOrException(readWithTheRuntime);
			var allExpected = expectedRuntime.Split('|');
			// It's not a difference if the runtime throws another exception than the expected one: which
			// exception it is depends on the CLR and on the operating system
			bool isSame = allExpected.Contains(runtime) || (runtime.StartsWith("throws ") && allExpected.Any(a => a.StartsWith("throws ")));
			if (IsDotNet)
				Assert.True(isSame, $".NET reads '{runtime}', expected '{expectedRuntime}'");
			else
				Assert.SkipUnless(isSame, $"This CLR reads '{runtime}', .NET reads '{expectedRuntime}', dnlib reads '{expectedDnlib}'");
		}

		/// <summary>
		/// Same as <see cref="AssertRead"/>, but only .NET is asked and not .NET Framework. For tests with
		/// metadata that makes .NET Framework stop the process (an AccessViolationException) or never return:
		/// neither can be caught, and the whole test run then fails or hangs.
		/// </summary>
		public static void AssertReadAskOnlyDotNet(string expectedDnlib, string dnlib, string expectedRuntime, Func<string> readWithTheRuntime) {
			if (IsDotNet || Environment.GetEnvironmentVariable("DNLIB_TESTS_ASK_NET_FRAMEWORK") == "1")
				AssertRead(expectedDnlib, dnlib, expectedRuntime, readWithTheRuntime);
			else
				Assert.Equal(expectedDnlib, dnlib);
		}

		/// <summary>
		/// For metadata that only .NET Framework reads (.NET has no code for it, eg. security declarations):
		/// checks what dnlib read on every runtime, and what .NET Framework reads if it runs the tests. See
		/// <see cref="IsRuntimeNotAsked"/> for when the runtime isn't asked.
		/// </summary>
		/// <param name="expectedDnlib">What dnlib must read</param>
		/// <param name="dnlib">What dnlib read</param>
		/// <param name="expectedNetFramework">What .NET Framework reads</param>
		/// <param name="readWithNetFramework">Reads the same with the runtime</param>
		public static void AssertReadOnNetFramework(string expectedDnlib, string dnlib, string expectedNetFramework, Func<string> readWithNetFramework) {
			Assert.Equal(expectedDnlib, dnlib);
			if (IsDotNet || IsRuntimeNotAsked)
				return;
			string runtime = ReadOrException(readWithNetFramework);
			bool isSame = runtime == expectedNetFramework || (runtime.StartsWith("throws ") && expectedNetFramework.StartsWith("throws "));
			Assert.True(isSame, $".NET Framework reads '{runtime}', expected '{expectedNetFramework}'");
		}

		const string AskInAnotherProcessVariable = "DNLIB_TESTS_ASK_CASES_THAT_STOP_THE_RUNTIME";
		const string ChildCaseVariable = "DNLIB_TESTS_CHILD_CASE";
		const string ChildResultVariable = "DNLIB_TESTS_CHILD_RESULT";
		public const string StopsTheProcess = "stops the process";

		/// <summary>
		/// For a case that makes the runtime stop the process (a fatal error or an AccessViolationException): the
		/// runtime is asked in another process, which runs this test again. Only if the environment variable
		/// <c>DNLIB_TESTS_ASK_CASES_THAT_STOP_THE_RUNTIME</c> is 1: a process that crashes can show a dialog or
		/// keep a build machine busy, so it's not done by default.
		/// </summary>
		/// <param name="stopsNetFramework"><c>true</c> if .NET Framework stops, <c>false</c> if .NET stops. The
		/// other runtime isn't asked.</param>
		/// <param name="readWithTheRuntime">Reads with the runtime</param>
		/// <param name="orReadsOtherMemory"><c>true</c> if the runtime reads memory at an address that isn't in the
		/// file: the process stops if there's no memory at that address, else any value is read</param>
		public static void AssertRuntimeStopsTheProcess(bool stopsNetFramework, Func<string> readWithTheRuntime, bool orReadsOtherMemory = false) {
			var test = TestContext.Current.Test;
			string? id = test?.UniqueID;
			if (Environment.GetEnvironmentVariable(ChildCaseVariable) is string childCase) {
				// This is the other process
				if (childCase == id) {
					string result;
					try {
						result = readWithTheRuntime();
					}
					catch (Exception ex) {
						result = "throws " + ex.GetType().Name;
					}
					File.WriteAllText(Environment.GetEnvironmentVariable(ChildResultVariable)!, result);
				}
				return;
			}
			if (Environment.GetEnvironmentVariable(AskInAnotherProcessVariable) != "1" || IsRuntimeNotAsked || IsDotNet == stopsNetFramework || id is null)
				return;

			var testMethod = TestContext.Current.TestMethod!;
			string methodName = testMethod.TestClass.TestClassName + "." + testMethod.MethodName;
			var commandLine = Environment.GetCommandLineArgs();
			string resultFile = Path.GetTempFileName();
			try {
				File.Delete(resultFile);
				// .NET: "dotnet dnlib.Tests.dll", .NET Framework: "dnlib.Tests.exe"
				string program = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
				string arguments = (IsDotNet ? $"\"{commandLine[0]}\" " : "") + $"-method \"{methodName}\"";
				var startInfo = new System.Diagnostics.ProcessStartInfo(program, arguments) {
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				};
				startInfo.EnvironmentVariables[ChildCaseVariable] = id;
				startInfo.EnvironmentVariables[ChildResultVariable] = resultFile;
				using (var process = System.Diagnostics.Process.Start(startInfo)!) {
					process.OutputDataReceived += (sender, e) => { };
					process.ErrorDataReceived += (sender, e) => { };
					process.BeginOutputReadLine();
					process.BeginErrorReadLine();
					if (!process.WaitForExit(120000)) {
						process.Kill();
						Assert.Fail("The other process didn't end");
					}
					process.WaitForExit();
					if (orReadsOtherMemory && File.Exists(resultFile))
						return;
					Assert.False(File.Exists(resultFile), $"The runtime didn't stop the process: it reads '{(File.Exists(resultFile) ? File.ReadAllText(resultFile) : "")}'");
					Assert.NotEqual(0, process.ExitCode);
				}
			}
			finally {
				File.Delete(resultFile);
			}
		}

		// To find the test cases that make a runtime crash or hang (see AssertReadAskOnlyDotNet): if the
		// environment variable DNLIB_TESTS_RUNTIME_LOG is the name of a file, a line "ASK id name" is written to
		// it before the runtime is asked and "DONE id" after it. The last "ASK" without a "DONE" is the case that
		// crashed. The runtime isn't asked for a case whose id is a line of the file DNLIB_TESTS_RUNTIME_SKIP.
		static string? StartLog() {
			string? id = TestContext.Current.Test?.UniqueID;
			if (id is null)
				return null;
			if (Environment.GetEnvironmentVariable("DNLIB_TESTS_RUNTIME_SKIP") is string skipFile && File.Exists(skipFile) && File.ReadAllLines(skipFile).Contains(id))
				return "skip";
			if (Environment.GetEnvironmentVariable("DNLIB_TESTS_RUNTIME_LOG") is string logFile)
				File.AppendAllText(logFile, $"ASK {id} {TestContext.Current.Test?.TestDisplayName}\r\n");
			return id;
		}
		static void EndLog(string? id, string result) {
			if (id is not null && Environment.GetEnvironmentVariable("DNLIB_TESTS_RUNTIME_LOG") is string logFile)
				File.AppendAllText(logFile, $"DONE {id} {result}\r\n");
		}

		/// <summary>
		/// For tests without expected values: dnlib must read what .NET reads. Another CLR (.NET Framework)
		/// may read something else: the test is then skipped with a message that says what it read.
		/// </summary>
		/// <param name="dnlib">What dnlib read</param>
		/// <param name="readWithTheRuntime">Reads the same with the runtime. An exception is "throws" and its name.</param>
		public static void AssertSameAsTheRuntime(string dnlib, Func<string> readWithTheRuntime) {
			if (IsRuntimeNotAsked)
				return;
			string runtime = ReadOrException(readWithTheRuntime);
			if (IsDotNet)
				Assert.Equal(runtime, dnlib);
			else
				Assert.SkipUnless(runtime == dnlib, $"This CLR reads '{runtime}', dnlib reads '{dnlib}'");
		}

		// An exception is "throws" and its name: a CLR can reject a file that another CLR loads
		static string ReadOrException(Func<string> readWithTheRuntime) {
			string? logId = StartLog();
			if (logId == "skip")
				Assert.Skip("The runtime isn't asked: the case is in the file DNLIB_TESTS_RUNTIME_SKIP");
			string result;
			try {
				result = readWithTheRuntime();
			}
			catch (Exception ex) {
				result = "throws " + ex.GetType().Name;
			}
			EndLog(logId, result);
			return result;
		}
	}
}
