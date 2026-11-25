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
		/// Checks what dnlib read and what the runtime reads. .NET must do what the test expects, another CLR
		/// (.NET Framework) may do something else: the test is then skipped with a message that says what it
		/// did. Mono isn't asked: it stops the process if it doesn't like the metadata.
		/// </summary>
		/// <param name="expectedDnlib">What dnlib must read</param>
		/// <param name="dnlib">What dnlib read</param>
		/// <param name="expectedRuntime">What .NET reads. <c>A|B</c> if .NET versions read A or B.</param>
		/// <param name="readWithTheRuntime">Reads the same with the runtime</param>
		public static void AssertRead(string expectedDnlib, string dnlib, string expectedRuntime, Func<string> readWithTheRuntime) {
			Assert.Equal(expectedDnlib, dnlib);
			if (IsMono)
				return;
			// A CLR can stop the process if it doesn't like a file. On .NET Framework (only Windows CI runs
			// it) the last of these lines in the log then says which test it was.
			if (!IsDotNet)
				Console.Error.WriteLine($"Asking the runtime: {TestContext.Current.Test?.TestDisplayName}");
			string runtime = readWithTheRuntime();
			var allExpected = expectedRuntime.Split('|');
			if (IsDotNet)
				Assert.Contains(runtime, allExpected);
			else
				Assert.SkipUnless(allExpected.Contains(runtime), $"This CLR reads '{runtime}', .NET reads '{expectedRuntime}', dnlib reads '{expectedDnlib}'");
		}
	}
}
