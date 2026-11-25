// dnlib: See LICENSE.txt for more info

using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// The metadata is a list of named streams ("heaps"): the tables (<c>#~</c>) and the data they reference
	/// (<c>#Strings</c>, <c>#US</c>, <c>#GUID</c>, <c>#Blob</c>). A file can have other streams that the CLR
	/// ignores, and the streams can be in any order. The writer can add streams and keep the order of a file.
	/// </summary>
	public class MetadataHeapTests {
		const string Source = "public class C { public static string M() => \"text\"; }";
		static byte[] CompiledImage => CSharpCompiler.Compile(Source).Image;

		// A stream with a name and some bytes
		sealed class CustomHeap : IHeap {
			readonly byte[] data;

			public CustomHeap(string name, byte[] data) {
				Name = name;
				this.data = data;
			}

			public string Name { get; }
			public bool IsEmpty => false;
			public FileOffset FileOffset { get; private set; }
			public RVA RVA { get; private set; }

			public void SetReadOnly() { }

			public void SetOffset(FileOffset offset, RVA rva) {
				FileOffset = offset;
				RVA = rva;
			}

			public uint GetFileLength() => (uint)data.Length;
			public uint GetVirtualSize() => GetFileLength();
			public uint CalculateAlignment() => 0;
			public void WriteTo(DataWriter writer) => writer.WriteBytes(data);
		}

		static string[] GetStreamNames(ModuleDefMD module) => module.Metadata.MetadataHeader.StreamHeaders.Select(a => a.Name).ToArray();

		static byte[] Write(ModuleDefMD module, System.Action<ModuleWriterOptions> changeOptions) {
			var options = new ModuleWriterOptions(module);
			changeOptions(options);
			var stream = new MemoryStream();
			module.Write(stream, options);
			return stream.ToArray();
		}

		static readonly byte[] customData = { 1, 2, 3, 4, 5, 6, 7, 8 };

		// The compiled file with two more streams, and all streams in the reverse order
		static byte[] CreateImageWithCustomHeapsInAnotherOrder() {
			using (var module = ModuleDefMD.Load(CompiledImage)) {
				return Write(module, options => {
					options.MetadataOptions.CustomHeaps.Add(new CustomHeap("#First", customData));
					options.MetadataOptions.CustomHeaps.Add(new CustomHeap("#Second", new byte[] { 9, 9, 9, 9 }));
					options.MetadataOptions.MetadataHeapsAdded += (sender, e) => e.Heaps.Reverse();
				});
			}
		}

		[Fact]
		public void DefaultOrderOfTheStreams() {
			using (var module = ModuleDefMD.Load(CompiledImage))
			using (var written = ModuleDefMD.Load(Write(module, options => { })))
				Assert.Equal(new[] { "#~", "#Strings", "#US", "#GUID", "#Blob" }, GetStreamNames(written));
		}

		[Fact]
		public void CustomHeapsAreWrittenAndTheOrderCanBeChanged() {
			var image = CreateImageWithCustomHeapsInAnotherOrder();

			using (var module = ModuleDefMD.Load(image)) {
				Assert.Equal(new[] { "#Second", "#First", "#Blob", "#GUID", "#US", "#Strings", "#~" }, GetStreamNames(module));
				var custom = module.Metadata.AllStreams.Single(a => a.Name == "#First");
				Assert.Equal(customData, custom.CreateReader().ToArray());
				// The module is read like any other
				Assert.Equal("text", module.Find("C", isReflectionName: true)!.FindMethod("M")!.Body!.Instructions[0].Operand);
			}
			using (var original = ModuleDefMD.Load(CompiledImage))
			using (var module = ModuleDefMD.Load(image))
				ModuleComparer.AssertEqual(original, module);
		}

		// Without PreserveHeapOrder(), the writer creates the default streams in the default order
		[Fact]
		public void CustomHeapsAndTheOrderAreNotKeptByDefault() {
			using (var module = ModuleDefMD.Load(CreateImageWithCustomHeapsInAnotherOrder()))
			using (var written = ModuleDefMD.Load(Write(module, options => { })))
				Assert.Equal(new[] { "#~", "#Strings", "#US", "#GUID", "#Blob" }, GetStreamNames(written));
		}

		[Theory, InlineData(false), InlineData(true)]
		public void PreserveHeapOrderKeepsTheOrderAndTheCustomHeaps(bool addCustomHeaps) {
			using (var module = ModuleDefMD.Load(CreateImageWithCustomHeapsInAnotherOrder())) {
				var written = Write(module, options => options.MetadataOptions.PreserveHeapOrder(module, addCustomHeaps));

				using (var writtenModule = ModuleDefMD.Load(written)) {
					var expected = addCustomHeaps ?
						new[] { "#Second", "#First", "#Blob", "#GUID", "#US", "#Strings", "#~" } :
						new[] { "#Blob", "#GUID", "#US", "#Strings", "#~" };
					Assert.Equal(expected, GetStreamNames(writtenModule));
					if (addCustomHeaps)
						Assert.Equal(customData, writtenModule.Metadata.AllStreams.Single(a => a.Name == "#First").CreateReader().ToArray());
					ModuleComparer.AssertEqual(module, writtenModule);
				}
			}
		}

		// Fixed in ed37d6b4: in "uncompressed" metadata (the tables stream is called #-), the CLR uses the first
		// stream with a known name. A second stream with such a name was written as a custom heap, and since the
		// writer creates normal metadata (#~, where the last one is used) it replaced the real stream.
		[Fact]
		public void DuplicateOfAKnownStreamIsNotKeptIfTheMetadataWasUncompressed() {
			byte[] image;
			using (var module = ModuleDefMD.Load(CompiledImage)) {
				image = Write(module, options => {
					// A second #Strings stream with other content, after the real one
					options.MetadataOptions.CustomHeaps.Add(new CustomHeap("#Strings", new byte[] { 0, (byte)'X', 0, 0 }));
				});
			}
			image = new MetadataPatcher(image).RenameStream("#~", "#-").ToArray();

			using (var module = ModuleDefMD.Load(image)) {
				Assert.False(module.Metadata.IsCompressed);
				Assert.Equal(2, GetStreamNames(module).Count(a => a == "#Strings"));
				Assert.NotNull(module.Find("C", isReflectionName: true));

				var written = Write(module, options => options.MetadataOptions.PreserveHeapOrder(module, addCustomHeaps: true));

				using (var writtenModule = ModuleDefMD.Load(written)) {
					Assert.Single(GetStreamNames(writtenModule), "#Strings");
					Assert.NotNull(writtenModule.Find("C", isReflectionName: true));
					ModuleComparer.AssertEqual(module, writtenModule);
				}
			}
		}
	}
}
