// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// A deterministic file only depends on what's in the module: writing the same module again creates the
	/// same file. Its timestamp in the PE header isn't a time, it's calculated from a hash of the file.
	/// The C# compiler creates deterministic files (the tests' compiler always does), and dnlib writes a module
	/// that was read from such a file in the same way.
	/// </summary>
	public class DeterministicWriteTests {
		const string Source = "public class C { public static int M() => 123; }";
		static readonly Lazy<byte[]> compiledImage = new Lazy<byte[]>(() => CSharpCompiler.Compile(Source).Image);

		static byte[] Write(Action<ModuleDefMD>? change = null, Action<ModuleWriterOptions>? changeOptions = null, Stream? stream = null) {
			using (var module = ModuleDefMD.Load(compiledImage.Value)) {
				change?.Invoke(module);
				var options = new ModuleWriterOptions(module);
				changeOptions?.Invoke(options);
				var memoryStream = new MemoryStream();
				module.Write(stream ?? memoryStream, options);
				return memoryStream.ToArray();
			}
		}

		static int ReadTimestamp(byte[] image) {
			using (var peReader = new PEReader(ImmutableArray.Create(image)))
				return peReader.PEHeaders.CoffHeader.TimeDateStamp;
		}

		[Fact]
		public void ModuleFromADeterministicFileIsWrittenAsADeterministicFile() {
			using (var module = ModuleDefMD.Load(compiledImage.Value))
				Assert.True((new ModuleWriterOptions(module).PdbOptions & PdbWriterOptions.Deterministic) != 0);
		}

		[Fact]
		public void SameModuleIsTheSameFile() => Assert.Equal(Write(), Write());

		// Fixed in 83b46924: the timestamp was the original file's timestamp, or the current time for a new module
		[Fact]
		public void TimestampDependsOnTheContent() {
			var unchanged = Write();
			var changed = Write(module => module.Find("C", isReflectionName: true)!.Name = "D");

			Assert.NotEqual(ReadTimestamp(compiledImage.Value), ReadTimestamp(unchanged));
			Assert.NotEqual(ReadTimestamp(unchanged), ReadTimestamp(changed));
		}

		// Fixed in 83b46924
		[Fact]
		public void TimestampInTheOptionsIsNotUsed() {
			var written = Write(changeOptions: options => options.PEHeadersOptions.TimeDateStamp = 0x12345678);

			Assert.Equal(Write(), written);
		}

		[Fact]
		public void TimestampInTheOptionsIsUsedIfTheFileIsNotDeterministic() {
			var written = Write(changeOptions: options => {
				options.PdbOptions &= ~PdbWriterOptions.Deterministic;
				options.PEHeadersOptions.TimeDateStamp = 0x12345678;
			});

			Assert.Equal(0x12345678, ReadTimestamp(written));
		}

		// Fixed in 83b46924
		[Fact]
		public void NewModuleCanBeWrittenAsADeterministicFile() {
			var module = new ModuleDefUser("New.dll");
			new AssemblyDefUser("New").Modules.Add(module);
			module.Types.Add(new TypeDefUser("Namespace", "Class", module.CorLibTypes.Object.TypeDefOrRef));

			byte[] WriteNewModule() {
				var options = new ModuleWriterOptions(module);
				options.PdbOptions |= PdbWriterOptions.Deterministic;
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}

			Assert.Equal(WriteNewModule(), WriteNewModule());
		}

		// Fixed in 3f10eb11: the writer reads the file it wrote to calculate the timestamp, and threw if the
		// stream can only be written to
		[Fact]
		public void DeterministicFileCanBeWrittenToAStreamThatCanNotSeek() {
			var stream = new WriteOnlyStream();

			Write(stream: stream);

			Assert.Equal(Write(), stream.GetWrittenData());
		}
	}
}
