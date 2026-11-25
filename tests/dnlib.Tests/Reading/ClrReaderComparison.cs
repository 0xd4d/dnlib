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
		public static byte[] CreateBaseImage(string source, Action<ModuleWriterOptions>? changeOptions = null) {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(source).Image)) {
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
			if (IsDotNet)
				Assert.Contains(runtime, allExpected);
			else {
				// It's not a difference if both CLRs throw, whatever the exception is
				bool isSame = allExpected.Contains(runtime) || (runtime.StartsWith("throws ") && allExpected.Any(a => a.StartsWith("throws ")));
				Assert.SkipUnless(isSame, $"This CLR reads '{runtime}', .NET reads '{expectedRuntime}', dnlib reads '{expectedDnlib}'");
			}
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
			try {
				return readWithTheRuntime();
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}
	}
}
