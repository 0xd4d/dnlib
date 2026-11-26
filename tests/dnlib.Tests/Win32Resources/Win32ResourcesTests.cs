// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.TestUtils;
using dnlib.W32Resources;
using Xunit;

namespace dnlib.Tests.Win32Resources {
	/// <summary>
	/// Win32 resources are the resources of native programs: version info, manifest, icons. They're a tree of
	/// directories with 3 levels (type, name, language). What dnlib writes is read by <see cref="PEWin32Resources"/>.
	/// </summary>
	public class Win32ResourcesTests {
		const string Source = "public class Program { public static void Main() { } }";
		// The C# compiler can add a version resource (type 16), and a manifest (type 24) if it's an EXE
		static readonly Lazy<byte[]> compiledExe = new Lazy<byte[]>(() =>
			CSharpCompiler.Compile(Source, new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication, Win32Resources = true }).Image);

		const int versionType = 16;
		const int manifestType = 24;

		static string[] GetPaths(byte[] image) => PEWin32Resources.Read(image).Select(a => a.Path).ToArray();

		static byte[] Write(ModuleDef module, Action<ModuleWriterOptions>? changeOptions = null) {
			var options = new ModuleWriterOptions(module);
			changeOptions?.Invoke(options);
			var stream = new MemoryStream();
			module.Write(stream, options);
			return stream.ToArray();
		}

		static ResourceData CreateData(ResourceName language, byte[] data) =>
			new ResourceData(language, ByteArrayDataReaderFactory.Create(data, filename: null), 0, (uint)data.Length);

		// Adds a resource to the tree and creates its type and name directories if they don't exist
		static void Add(ResourceDirectory root, ResourceName type, ResourceName name, ResourceData data) {
			var typeDirectory = root.FindDirectory(type);
			if (typeDirectory is null)
				root.Directories.Add(typeDirectory = new ResourceDirectoryUser(type));
			var nameDirectory = typeDirectory.FindDirectory(name);
			if (nameDirectory is null)
				typeDirectory.Directories.Add(nameDirectory = new ResourceDirectoryUser(name));
			nameDirectory.Data.Add(data);
		}

		[Fact]
		public void CompilersResourcesAreRead() {
			Assert.Equal(new[] { "16/1/0", "24/1/0" }, GetPaths(compiledExe.Value));
			var expected = PEWin32Resources.Read(compiledExe.Value);

			using (var module = ModuleDefMD.Load(compiledExe.Value)) {
				var resources = module.Win32Resources!;
				Assert.Equal(new[] { versionType, manifestType }, resources.Root!.Directories.Select(a => a.Name.Id));
				var version = resources.Find(versionType, 1, 0)!;
				Assert.Equal(expected[0].Data, version.CreateReader().ToArray());
				var manifest = module.FindWin32ResourceData(manifestType, 1, 0)!;
				Assert.Equal(expected[1].Data, manifest.CreateReader().ToArray());
				Assert.Contains("<assembly", System.Text.Encoding.UTF8.GetString(manifest.CreateReader().ToArray()));

				Assert.Null(resources.Find(3));
				Assert.Null(resources.Find(versionType, 2));
				Assert.Null(resources.Find(versionType, 1, 1033));
			}
		}

		[Fact]
		public void ResourcesSurviveAWrite() {
			var expected = PEWin32Resources.Read(compiledExe.Value);
			using (var module = ModuleDefMD.Load(compiledExe.Value)) {
				var written = PEWin32Resources.Read(Write(module));

				Assert.Equal(expected.Select(a => a.Path), written.Select(a => a.Path));
				Assert.Equal(expected.Select(a => a.Data), written.Select(a => a.Data));
				Assert.Equal(expected.Select(a => a.CodePage), written.Select(a => a.CodePage));
			}
		}

		[Fact]
		public void NoWin32ResourcesOptionRemovesThem() {
			using (var module = ModuleDefMD.Load(compiledExe.Value))
				Assert.Empty(PEWin32Resources.Read(Write(module, options => options.NoWin32Resources = true)));
		}

		[Fact]
		public void ResourcesInTheOptionsReplaceTheModulesResources() {
			var newResources = new Win32ResourcesUser { Root = new ResourceDirectoryUser(0) };
			Add(newResources.Root, 10, "CUSTOM", CreateData(1033, new byte[] { 1, 2, 3 }));
			using (var module = ModuleDefMD.Load(compiledExe.Value)) {
				var written = PEWin32Resources.Read(Write(module, options => options.Win32Resources = newResources));

				var resource = Assert.Single(written);
				Assert.Equal("10/\"CUSTOM\"/1033", resource.Path);
				Assert.Equal(new byte[] { 1, 2, 3 }, resource.Data);
			}
		}

		// Windows finds a resource with a binary search: in every directory, the entries with a name come first,
		// sorted by name, then the entries with an ID, sorted by ID
		[Fact]
		public void DirectoryEntriesAreSortedNamesFirst() {
			using (var module = ModuleDefMD.Load(compiledExe.Value)) {
				var root = module.Win32Resources!.Root!;
				Add(root, 3, 2, CreateData(0, new byte[] { 1 }));
				Add(root, 3, 1, CreateData(1033, new byte[] { 2 }));
				Add(root, 3, 1, CreateData(1031, new byte[] { 3 }));
				Add(root, 3, "ZNAME", CreateData(0, new byte[] { 4 }));
				Add(root, 3, "ANAME", CreateData(0, new byte[] { 5 }));
				Add(root, "TYPENAME", 1, CreateData(0, new byte[] { 6 }));

				var written = Write(module);

				Assert.Equal(new[] {
					"\"TYPENAME\"/1/0",
					"3/\"ANAME\"/0",
					"3/\"ZNAME\"/0",
					"3/1/1031",
					"3/1/1033",
					"3/2/0",
					"16/1/0",
					"24/1/0",
				}, GetPaths(written));
				using (var writtenModule = ModuleDefMD.Load(written))
					Assert.Equal(new byte[] { 5 }, writtenModule.FindWin32ResourceData(3, "ANAME", 0)!.CreateReader().ToArray());
			}
		}

		// Fixed in b57bea79: the names were compared with the rules of the current culture, so the order
		// depended on the computer's language. In Turkish, "i" and "I" aren't the same letter.
		[Fact]
		public void OrderOfNamesDoesNotDependOnTheCulture() {
			byte[] WriteWithCulture(string cultureName) {
				var oldCulture = CultureInfo.CurrentCulture;
				CultureInfo.CurrentCulture = new CultureInfo(cultureName);
				try {
					using (var module = ModuleDefMD.Load(compiledExe.Value)) {
						var root = module.Win32Resources!.Root!;
						foreach (var name in new[] { "TITLE", "TIP", "Tj", "T_", "ti", "tI" })
							Add(root, 3, name, CreateData(0, new byte[] { 1 }));
						return Write(module);
					}
				}
				finally {
					CultureInfo.CurrentCulture = oldCulture;
				}
			}

			Assert.Equal(GetPaths(WriteWithCulture("en-US")), GetPaths(WriteWithCulture("tr-TR")));
		}

		// Fixed in bd1e676b: entries with the same name (a name's case is ignored) or the same ID aren't valid,
		// but they were written in another order every time
		[Fact]
		public void EntriesWithTheSameNameKeepTheirOrder() {
			const int count = 40;
			using (var module = ModuleDefMD.Load(compiledExe.Value)) {
				var typeDirectory = new ResourceDirectoryUser(3);
				module.Win32Resources!.Root!.Directories.Add(typeDirectory);
				for (int i = 0; i < count; i++) {
					// "SAME" and "same" are the same name
					var nameDirectory = new ResourceDirectoryUser(i % 2 == 0 ? "SAME" : "same");
					nameDirectory.Data.Add(CreateData(0, new byte[] { (byte)i }));
					typeDirectory.Directories.Add(nameDirectory);
				}

				var written = PEWin32Resources.Read(Write(module)).Where(a => a.Path.StartsWith("3/", StringComparison.Ordinal)).ToList();

				Assert.Equal(Enumerable.Range(0, count), written.Select(a => (int)a.Data[0]));
			}
		}

		// Fixed in bb32c99a: the reader creates a ResourceData for every entry, and the writer wrote the data
		// of each of them. A small file whose entries all use the same big data became a huge file.
		[Fact]
		public void DataThatIsUsedByManyEntriesIsWrittenOnce() {
			const int entryCount = 200;
			var bigData = new byte[0x10000];
			for (int i = 0; i < bigData.Length; i++)
				bigData[i] = (byte)(i * 7);

			// All entries use the same bytes of the same data source
			byte[] image;
			using (var module = ModuleDefMD.Load(compiledExe.Value)) {
				var source = ByteArrayDataReaderFactory.Create(bigData, filename: null);
				for (int i = 0; i < entryCount; i++)
					Add(module.Win32Resources!.Root!, 10, i + 1, new ResourceData(0, source, 0, (uint)bigData.Length));
				image = Write(module);
			}
			Assert.True(image.Length < 2 * bigData.Length, $"The file has {image.Length} bytes");

			// And they still do when the file is read and written again
			byte[] writtenAgain;
			using (var module = ModuleDefMD.Load(image))
				writtenAgain = Write(module);

			Assert.True(writtenAgain.Length < 2 * bigData.Length, $"The file has {writtenAgain.Length} bytes");
			var resources = PEWin32Resources.Read(writtenAgain).Where(a => a.Path.StartsWith("10/", StringComparison.Ordinal)).ToList();
			Assert.Equal(entryCount, resources.Count);
			Assert.All(resources, a => Assert.Equal(bigData, a.Data));
			Assert.Single(resources.Select(a => a.DataRva).Distinct());
		}

		static byte[] NativeWrite(ModuleDefMD module, Action<NativeModuleWriterOptions> changeOptions) {
			var options = new NativeModuleWriterOptions(module, optimizeImageSize: true);
			changeOptions(options);
			var stream = new MemoryStream();
			module.NativeWrite(stream, options);
			return stream.ToArray();
		}

		[Fact]
		public void NativeWriterKeepsTheResources() {
			using (var module = ModuleDefMD.Load(compiledExe.Value))
				Assert.Equal(new[] { "16/1/0", "24/1/0" }, GetPaths(NativeWrite(module, options => { })));
		}

		// Fixed in ec6462be: the original resources were still in the file
		[Fact]
		public void NativeWriterRemovesTheResourcesIfNoWin32ResourcesIsSet() {
			using (var module = ModuleDefMD.Load(compiledExe.Value))
				Assert.Empty(GetPaths(NativeWrite(module, options => options.NoWin32Resources = true)));
		}

		[Fact]
		public void KeepWin32ResourcesWinsOverNoWin32Resources() {
			using (var module = ModuleDefMD.Load(compiledExe.Value)) {
				var written = NativeWrite(module, options => {
					options.NoWin32Resources = true;
					options.KeepWin32Resources = true;
				});

				Assert.Equal(new[] { "16/1/0", "24/1/0" }, GetPaths(written));
			}
		}

		[Fact]
		public void ResourceNamesAreComparedWithoutCase() {
			Assert.Equal(new ResourceName("Name"), new ResourceName("NAME"));
			Assert.Equal(new ResourceName("Name").GetHashCode(), new ResourceName("NAME").GetHashCode());
			Assert.NotEqual(new ResourceName("Name"), new ResourceName("Other"));
			Assert.Equal(new ResourceName(5), new ResourceName(5));
			Assert.NotEqual(new ResourceName(5), new ResourceName("5"));
			// Names are before IDs
			Assert.True(new ResourceName("Z") < new ResourceName(0));
			Assert.True(new ResourceName(1) < new ResourceName(2));
			Assert.True(new ResourceName("A") < new ResourceName("b"));
			Assert.True(new ResourceName("a") < new ResourceName("B"));
		}

		[Fact]
		public void ResourcesAreFoundByTypeNameAndLanguage() {
			using (var module = ModuleDefMD.Load(compiledExe.Value)) {
				var resources = module.Win32Resources!;
				Add(resources.Root!, 3, "NAME", CreateData(1033, new byte[] { 1, 2, 3 }));

				Assert.Equal(new ResourceName(3), resources.Find(3)!.Name);
				Assert.Equal(new ResourceName("NAME"), resources.Find(3, "NAME")!.Name);
				Assert.Equal(new byte[] { 1, 2, 3 }, resources.Find(3, "NAME", 1033)!.CreateReader().ToArray());
				Assert.Null(resources.Find(3, "NAME", 1031));
				Assert.Null(resources.Find(3, "OTHER"));
				Assert.Null(resources.Find(3, "OTHER", 1033));
				Assert.Null(resources.Find(99));
				Assert.Null(resources.Find(99, 1));
				Assert.Null(resources.Find(99, 1, 0));
			}
		}

		// After a write, the writer knows where it wrote every directory, every data entry and every name
		[Fact]
		public void WriterKnowsWhereEveryPartOfTheTreeWasWritten() {
			using (var module = ModuleDefMD.Load(compiledExe.Value)) {
				var root = module.Win32Resources!.Root!;
				var data = CreateData(1033, new byte[] { 1, 2, 3, 4, 5 });
				Add(root, 3, "SOMENAME", data);
				var writer = new ModuleWriter(module, new ModuleWriterOptions(module));
				var stream = new MemoryStream();

				writer.Write(stream);

				var image = stream.ToArray();
				var chunk = writer.Win32Resources!;
				// The root directory is the start of the resources. A directory has the number of its entries
				// with a name at offset 12, and the number of those with an ID at offset 14.
				Assert.True(chunk.GetFileOffsetAndRvaOf(root, out var rootOffset, out var rootRva));
				Assert.Equal(chunk.FileOffset, rootOffset);
				Assert.Equal(chunk.RVA, rootRva);
				Assert.Equal(root.Directories.Count, LittleEndian.ToUInt16(image, (int)rootOffset + 12) + LittleEndian.ToUInt16(image, (int)rootOffset + 14));
				var typeDirectory = root.FindDirectory(3)!;
				Assert.True(chunk.GetFileOffset(typeDirectory) > rootOffset);
				Assert.Equal((uint)chunk.GetFileOffset(typeDirectory) - (uint)rootOffset, (uint)chunk.GetRVA(typeDirectory) - (uint)rootRva);
				// The same through the base class of directories and data entries
				Assert.Equal(chunk.GetFileOffset(typeDirectory), chunk.GetFileOffset((ResourceDirectoryEntry)typeDirectory));
				Assert.Equal(chunk.GetRVA(typeDirectory), chunk.GetRVA((ResourceDirectoryEntry)typeDirectory));

				// A data entry is the RVA and the size of its data
				int dataEntryOffset = (int)chunk.GetFileOffset(data);
				Assert.Equal(5U, LittleEndian.ToUInt32(image, dataEntryOffset + 4));
				Assert.Equal(chunk.GetFileOffset(data), chunk.GetFileOffset((ResourceDirectoryEntry)data));
				Assert.NotEqual(0U, (uint)chunk.GetRVA(data));

				// A name is its length and its UTF-16 chars
				int nameOffset = (int)chunk.GetFileOffset("SOMENAME");
				Assert.Equal(8, LittleEndian.ToUInt16(image, nameOffset));
				Assert.Equal("SOMENAME", System.Text.Encoding.Unicode.GetString(image, nameOffset + 2, 16));
				Assert.NotEqual(0U, (uint)chunk.GetRVA("SOMENAME"));

				// Things that aren't in the tree
				Assert.False(chunk.GetFileOffsetAndRvaOf(new ResourceDirectoryUser(5), out var noOffset, out var noRva));
				Assert.Equal(0U, (uint)noOffset + (uint)noRva);
				Assert.False(chunk.GetFileOffsetAndRvaOf(CreateData(0, new byte[1]), out _, out _));
				Assert.False(chunk.GetFileOffsetAndRvaOf("OTHERNAME", out _, out _));
				Assert.False(chunk.GetFileOffsetAndRvaOf((ResourceDirectoryEntry?)null, out _, out _));
				Assert.Equal(0U, (uint)chunk.GetFileOffset("OTHERNAME"));
				Assert.Equal(0U, (uint)chunk.GetRVA((ResourceData?)null));
			}
		}
	}
}
