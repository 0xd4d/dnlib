// dnlib: See LICENSE.txt for more info

using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// <see cref="MetadataFlags.PreserveStringsOffsets"/>: the writer keeps the bytes of the original
	/// <c>#Strings</c> heap and adds new strings after them. The first byte of the heap is normally a zero
	/// byte (the empty string is at offset 0), but nothing stops a file from having another byte there.
	/// </summary>
	public class StringsHeapPreservedOffsetsTests {
		// The first byte of the #Strings heap is 'A': the string at offset 0 isn't "" but "A" and the bytes of
		// the string after it (AtOffset0)
		static byte[] CreateImage() {
			var created = new CreatedModule();
			created.AddField("Name", created.Module.CorLibTypes.Int32);
			var block = new MetadataBlock(created.Write());
			var heap = block.GetStream("#Strings");
			Assert.Equal(0, heap.Data[0]);
			heap.Data[0] = (byte)'A';
			return block.Write();
		}

		static string AtOffset0(ModuleDefMD module) {
			string text = module.StringsStream.ReadNoNull(0);
			Assert.StartsWith("A", text);
			return text;
		}

		static byte[] Write(ModuleDefMD module, MetadataFlags flags) {
			var options = new ModuleWriterOptions(module);
			options.MetadataOptions.Flags |= flags;
			var stream = new MemoryStream();
			module.Write(stream, options);
			return stream.ToArray();
		}

		// Fixed in this commit: the writer gave every empty string offset 0, also if the original heap is kept
		// and its string at offset 0 isn't empty. Every empty name was then the string at offset 0 ("A") in
		// the written file. An empty string now gets the offset of a zero byte of the original heap.
		[Theory]
		[InlineData(MetadataFlags.PreserveStringsOffsets)]
		[InlineData(MetadataFlags.PreserveAll)]
		public void EmptyStringIsEmptyIfTheOriginalHeapDoesNotStartWithAZeroByte(MetadataFlags flags) {
			var image = CreateImage();
			using (var module = ModuleDefMD.Load(image)) {
				// <Module> has no namespace: offset 0, which isn't the empty string in this file
				string atOffset0 = AtOffset0(module);
				Assert.Equal(atOffset0, module.GlobalType!.Namespace);
				var type = module.Types.Single(a => a.Name == "Type");
				Assert.Equal("Ns", type.Namespace);
				type.Namespace = UTF8String.Empty;
				module.Assembly!.Culture = UTF8String.Empty;

				var written = Write(module, flags);
				using (var writtenModule = ModuleDefMD.Load(written)) {
					Assert.Equal((byte)'A', writtenModule.StringsStream.CreateReader().ReadByte());
					Assert.Equal("", writtenModule.Types.Single(a => a.Name == "Type").Namespace);
					Assert.Equal("", writtenModule.Assembly!.Culture);
					// The string that was at offset 0 is still the same string
					Assert.Equal(atOffset0, writtenModule.GlobalType!.Namespace);
					Assert.Equal("Name", writtenModule.Types.Single(a => a.Name == "Type").Fields.Single().Name);

					// Written again, it's the same
					using (var writtenAgain = ModuleDefMD.Load(Write(writtenModule, flags))) {
						Assert.Equal("", writtenAgain.Types.Single(a => a.Name == "Type").Namespace);
						Assert.Equal("", writtenAgain.Assembly!.Culture);
						Assert.Equal(atOffset0, writtenAgain.GlobalType!.Namespace);
					}
				}
			}
		}

		// Without the original heap, the written heap starts with a zero byte and the empty string is at offset 0
		[Fact]
		public void EmptyStringIsAtOffset0IfTheHeapIsNotKept() {
			using (var module = ModuleDefMD.Load(CreateImage())) {
				string atOffset0 = AtOffset0(module);
				module.Types.Single(a => a.Name == "Type").Namespace = UTF8String.Empty;

				using (var writtenModule = ModuleDefMD.Load(Write(module, 0))) {
					Assert.Equal(0, writtenModule.StringsStream.CreateReader().ReadByte());
					var type = writtenModule.Types.Single(a => a.Name == "Type");
					Assert.Equal("", type.Namespace);
					Assert.True(writtenModule.TablesStream.TryReadTypeDefRow(type.Rid, out var row));
					Assert.Equal(0U, row.Namespace);
					Assert.Equal(atOffset0, writtenModule.GlobalType!.Namespace);
				}
			}
		}
	}
}
