// dnlib: See LICENSE.txt for more info

using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The tables stream of a normal file is called <c>#~</c>. If it's called <c>#-</c>, the metadata is
	/// "uncompressed" (edit-and-continue metadata): the CLR and dnlib read it with other code that allows
	/// unsorted tables and the extra pointer tables. Compilers don't create such files for released code, but
	/// obfuscators do. The tests rename the stream of a normal file, everything else stays the same.
	/// </summary>
	public class UncompressedMetadataTests {
		static byte[] ValidImage => Fixtures.Fixtures.Features.Image;

		static byte[] CreateUncompressedImage() => new MetadataPatcher(ValidImage).RenameStream("#~", "#-").ToArray();

		[Fact]
		public void RenamedTablesStreamIsReadAsUncompressedMetadata() {
			using (var original = ModuleDefMD.Load(ValidImage))
			using (var uncompressed = ModuleDefMD.Load(CreateUncompressedImage())) {
				Assert.True(original.Metadata.IsCompressed);
				Assert.False(uncompressed.Metadata.IsCompressed);
				Assert.Equal(original.TablesStream.MethodTable.Rows, uncompressed.TablesStream.MethodTable.Rows);
			}
		}

		[Fact]
		public void UncompressedMetadataIsTheSameModule() {
			using (var original = ModuleDefMD.Load(ValidImage))
			using (var uncompressed = ModuleDefMD.Load(CreateUncompressedImage()))
				ModuleComparer.AssertEqual(original, uncompressed, new ModuleDumpOptions { MaxStack = true });
		}

		[Fact]
		public void UncompressedMetadataSurvivesAWrite() {
			var image = CreateUncompressedImage();

			var written = RoundTrip.CheckWithNewMetadata(image);
			RoundTrip.CheckWithPreservedMetadata(image);

			// The writer creates normal metadata
			using (var module = ModuleDefMD.Load(written))
				Assert.True(module.Metadata.IsCompressed);
			Assert.Equal(0, AssemblyRunner.RunEntryPoint(written));
		}
	}
}
