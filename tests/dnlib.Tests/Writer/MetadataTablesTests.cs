// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// Checks the metadata tables that dnlib writes. They're read with System.Reflection.Metadata, see
	/// <see cref="MetadataTableDumper"/>: neither dnlib's reader nor its own idea of the file format is used.
	/// </summary>
	public class MetadataTablesTests {
		static byte[] WriteWithPreservedMetadata(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var options = new ModuleWriterOptions(module);
				options.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		}

		static void AssertSameTablesAfterAWrite(byte[] image) =>
			Assert.Equal(MetadataTableDumper.Dump(image), MetadataTableDumper.Dump(WriteWithPreservedMetadata(image)));

		// If the tokens are preserved, dnlib must write the same rows, strings and blobs as the compiler did:
		// every signature, custom attribute and constant has the same bytes
		[Fact]
		public void FixtureHasTheSameTablesAfterAWrite() => AssertSameTablesAfterAWrite(Fixtures.Fixtures.Features.Image);

		[Fact]
		public void FixtureWithNativeExportsHasTheSameTablesAfterAWrite() => AssertSameTablesAfterAWrite(Fixtures.Fixtures.Exports.Image);

		public static TheoryData<string> SampleFiles => new TheoryData<string>(FrameworkCorpus.GetSampleFiles());
		public static TheoryData<string> AllFiles => new TheoryData<string>(FrameworkCorpus.GetAllFiles());

		[Theory, MemberData(nameof(SampleFiles))]
		public void SampleHasTheSameTablesAfterAWrite(string file) => AssertSameTablesAfterAWrite(File.ReadAllBytes(file));

		[Theory, MemberData(nameof(AllFiles)), Trait(TestCategories.Name, TestCategories.Slow)]
		public void EveryAssemblyHasTheSameTablesAfterAWrite(string file) => AssertSameTablesAfterAWrite(File.ReadAllBytes(file));
	}
}
