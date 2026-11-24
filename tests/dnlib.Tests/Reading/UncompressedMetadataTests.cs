// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.IO;
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

		// The fixture written as uncompressed metadata with a #JTD stream. If there's such a stream, the CLR
		// reads every heap offset and every row number in the tables as 4 bytes, also in a small file.
		static byte[] CreateImageWithJtdStream() {
			using (var module = ModuleDefMD.Load(ValidImage)) {
				var options = new ModuleWriterOptions(module);
				options.MetadataOptions.TablesHeapOptions.UseENC = true;
				options.MetadataOptions.TablesHeapOptions.ForceBigColumns = true;
				options.MetadataOptions.CustomHeaps.Add(new DataReaderHeap("#JTD", ByteArrayDataReaderFactory.CreateReader(new byte[4])));
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		}

		[Fact]
		public void MetadataWithAJtdStreamHasBigColumnsAndIsTheSameModule() {
			var image = CreateImageWithJtdStream();
			using (var original = ModuleDefMD.Load(ValidImage))
			using (var module = ModuleDefMD.Load(image)) {
				Assert.False(module.Metadata.IsCompressed);
				Assert.False(module.TablesStream.HasBigBlob);
				foreach (var column in module.TablesStream.MethodTable.TableInfo.Columns)
					Assert.True(column.Size == 4 || column.ColumnSize == ColumnSize.UInt16, column.Name);
				// A row is bigger: RVA (4), ImplFlags (2), Flags (2), Name, Signature, ParamList
				Assert.Equal(14U, original.TablesStream.MethodTable.RowSize);
				Assert.Equal(20U, module.TablesStream.MethodTable.RowSize);

				ModuleComparer.AssertEqual(original, module, new ModuleDumpOptions { MaxStack = true });
			}
			RunUnlessMono(image);
		}

		// Mono crashes if it loads a file with a #JTD stream that dnlib wrote
		static void RunUnlessMono(byte[] image) {
			if (System.Type.GetType("Mono.Runtime") is null)
				Assert.Equal(0, AssemblyRunner.RunEntryPoint(image));
		}

		// Fixed in 8092684a: the CLR reads the 4 bytes of an offset in the #Blob heap but only uses the low 16
		// bits if the heap is small. Mono uses all bits.
		[Fact]
		public void HighBitsOfABlobOffsetAreIgnoredIfThereIsAJtdStream() {
			var patcher = new MetadataPatcher(CreateImageWithJtdStream());
			uint rid = patcher.FindRow(Table.Method, "Name", "Main");
			uint signatureOffset = patcher.GetCell(Table.Method, rid, "Signature");
			Assert.InRange(signatureOffset, 1U, 0xFFFFU);
			var image = patcher.SetCell(Table.Method, rid, "Signature", 0x12340000 | signatureOffset).ToArray();

			using (var module = ModuleDefMD.Load(image))
				Assert.NotNull(module.EntryPoint!.MethodSig);
			using (var module = ModuleDefMD.Load(image, new ModuleCreationOptions(CLRRuntimeReaderKind.Mono)))
				Assert.Null(module.EntryPoint!.MethodSig);
			// The runtime agrees
			RunUnlessMono(image);
		}
	}
}
