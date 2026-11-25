// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// A portable PDB can be stored in the PE file instead of in its own file: the debug directory then has an
	/// entry with the compressed PDB (the C# compiler's <c>-debug:embedded</c>). dnlib reads such a PDB when it
	/// loads the module and writes it into the new PE file. System.Reflection.Metadata reads the written PDB.
	/// </summary>
	public class EmbeddedPortablePdbTests {
		const string Source = @"
using System;
public static class Program {
	public static int Main() {
		int sum = 0;
		for (int i = 0; i < 4; i++) {
			int square = i * i;
			sum += square;
		}
		Func<int, int> twice = a => a * 2;
		return twice(sum) == 28 ? 0 : 1;
	}

	public static void Throw(string message) {
		throw new InvalidOperationException(message);
	}
}
";

		static CompiledAssembly Compile(PdbKind pdbKind) =>
			CSharpCompiler.Compile(Source, new CSharpCompilerOptions { PdbKind = pdbKind, OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication, Optimize = false, SourcePath = "/src/Program.cs" });

		static readonly byte[] imageWithEmbeddedPdb = Compile(PdbKind.Embedded).Image;

		// The PDB in the PE file or null if it has none: the entry's data is "MPDB", the size of the PDB and
		// the PDB compressed with Deflate
		static byte[]? GetEmbeddedPdb(byte[] image) {
			using (var peReader = new PEReader(new MemoryStream(image))) {
				var entries = peReader.ReadDebugDirectory().Where(a => a.Type == DebugDirectoryEntryType.EmbeddedPortablePdb).ToArray();
				if (entries.Length == 0)
					return null;
				var entry = Assert.Single(entries);
				Assert.Equal("MPDB", System.Text.Encoding.ASCII.GetString(image, entry.DataPointer, 4));
				int size = BitConverter.ToInt32(image, entry.DataPointer + 4);
				var pdb = new MemoryStream();
				using (var deflate = new DeflateStream(new MemoryStream(image, entry.DataPointer + 8, entry.DataSize - 8), CompressionMode.Decompress))
					deflate.CopyTo(pdb);
				Assert.Equal(size, pdb.Length);
				return pdb.ToArray();
			}
		}

		static byte[] Write(ModuleDef module, Action<ModuleWriterOptions>? changeOptions = null) {
			var options = new ModuleWriterOptions(module) { WritePdb = true };
			changeOptions?.Invoke(options);
			var stream = new MemoryStream();
			module.Write(stream, options);
			return stream.ToArray();
		}

		[Fact]
		public void EmbeddedPdbIsReadWhenTheModuleIsLoaded() {
			var withPdbFile = Compile(PdbKind.Portable);
			using (var expected = RoundTrip.LoadWithPdb(withPdbFile.Image, withPdbFile.Pdb!))
			using (var module = ModuleDefMD.Load(imageWithEmbeddedPdb)) {
				Assert.NotNull(module.PdbState);
				Assert.Equal(PdbFileKind.EmbeddedPortablePDB, module.PdbState.PdbFileKind);
				Assert.Equal(PdbFileKind.PortablePDB, expected.PdbState!.PdbFileKind);
				var document = Assert.Single(module.PdbState.Documents);
				Assert.Equal("/src/Program.cs", document.Url);

				// The same debug info as in the PDB file of the same program
				var main = module.Find("Program", isReflectionName: true)!.FindMethod("Main")!;
				var expectedMain = expected.Find("Program", isReflectionName: true)!.FindMethod("Main")!;
				Assert.Equal(ModuleDumper.DumpType(expectedMain.DeclaringType!, new ModuleDumpOptions { Pdb = true }),
					ModuleDumper.DumpType(main.DeclaringType!, new ModuleDumpOptions { Pdb = true }));
				Assert.Contains(main.Body!.Instructions, a => a.SequencePoint is not null);
				Assert.Contains(main.Body.Variables, a => a.Name == "square");
			}
		}

		// The data of the entry: "MPDB", the size of the PDB (4 bytes) and the compressed PDB. If the size isn't
		// the real size, if the signature is another one or if the data can't be decompressed, the module is
		// loaded without debug info. These are the cases that System.Reflection.Metadata tests (it throws).
		// Fixed in the commit that added this test: a size that was smaller than the real size was accepted,
		// and dnlib read the start of the PDB.
		[Theory]
		[InlineData("size is 0")]
		[InlineData("size is too small by 1")]
		[InlineData("size is half the real size")]
		[InlineData("size is too big by 1")]
		[InlineData("size is 0x7FFFFFFF")]
		[InlineData("size is 0xFFFFFFFF")]
		[InlineData("another signature")]
		[InlineData("invalid compressed data")]
		[InlineData("compressed data is cut off")]
		[InlineData("no compressed data")]
		[InlineData("only the signature")]
		public void EmbeddedPdbThatCanNotBeDecompressedIsNotRead(string change) {
			var image = (byte[])imageWithEmbeddedPdb.Clone();
			int realSize = GetEmbeddedPdb(image)!.Length;
			using (var peReader = new PEReader(new MemoryStream(imageWithEmbeddedPdb))) {
				var entries = peReader.ReadDebugDirectory();
				int index = entries.IndexOf(entries.Single(a => a.Type == DebugDirectoryEntryType.EmbeddedPortablePdb));
				int dataOffset = entries[index].DataPointer;
				Assert.True(peReader.PEHeaders.TryGetDirectoryOffset(peReader.PEHeaders.PEHeader!.DebugTableDirectory, out int tableOffset));
				void SetSize(uint size) => BitConverter.GetBytes(size).CopyTo(image, dataOffset + 4);
				// An entry of the debug directory has 28 bytes, the size of its data is at offset 16
				void SetDataSize(int size) => BitConverter.GetBytes(size).CopyTo(image, tableOffset + index * 28 + 16);
				switch (change) {
				case "size is 0": SetSize(0); break;
				case "size is too small by 1": SetSize((uint)realSize - 1); break;
				case "size is half the real size": SetSize((uint)realSize / 2); break;
				case "size is too big by 1": SetSize((uint)realSize + 1); break;
				case "size is 0x7FFFFFFF": SetSize(0x7FFFFFFF); break;
				case "size is 0xFFFFFFFF": SetSize(0xFFFFFFFF); break;
				case "another signature": image[dataOffset + 3] = (byte)'C'; break;
				case "invalid compressed data": new byte[] { 0xEF, 0xFF, 0x4F, 0xFF }.CopyTo(image, dataOffset + 8); break;
				case "compressed data is cut off": SetDataSize(8 + (entries[index].DataSize - 8) / 2); break;
				case "no compressed data": SetDataSize(8); break;
				case "only the signature": SetDataSize(4); break;
				default: throw new ArgumentException(change);
				}
			}

			using (var module = ModuleDefMD.Load(image)) {
				Assert.Null(module.PdbState);
				Assert.NotNull(module.Find("Program", isReflectionName: true)!.FindMethod("Main")!.Body);
			}
		}

		// The option is for both: a PDB file on the disk and a PDB in the PE file
		[Fact]
		public void EmbeddedPdbIsNotReadIfTryToLoadPdbFromDiskIsFalse() {
			var options = new ModuleCreationOptions { TryToLoadPdbFromDisk = false };
			using (var module = ModuleDefMD.Load(imageWithEmbeddedPdb, options)) {
				Assert.Null(module.PdbState);
				var program = module.Find("Program", isReflectionName: true)!;
				var bodyReadWithoutPdb = program.FindMethod("Throw")!.Body!;
				Assert.DoesNotContain(bodyReadWithoutPdb.Instructions, a => a.SequencePoint is not null);

				// It can be loaded later. Only the method bodies that are read after that have debug info.
				module.LoadPdb();

				Assert.Equal(PdbFileKind.EmbeddedPortablePDB, module.PdbState!.PdbFileKind);
				Assert.Contains(program.FindMethod("Main")!.Body!.Instructions, a => a.SequencePoint is not null);
				Assert.DoesNotContain(bodyReadWithoutPdb.Instructions, a => a.SequencePoint is not null);
				Assert.Throws<InvalidOperationException>(() => module.LoadPdb());
			}
		}

		[Fact]
		public void EmbeddedPdbReaderCanBeCreated() {
			using (var module = ModuleDefMD.Load(imageWithEmbeddedPdb, new ModuleCreationOptions { TryToLoadPdbFromDisk = false }))
			using (var reader = SymbolReaderFactory.TryCreateEmbeddedPdbReader(PdbReaderOptions.None, module.Metadata)) {
				Assert.NotNull(reader);
				Assert.Equal(PdbFileKind.EmbeddedPortablePDB, reader.PdbFileKind);
			}
			// A file without an embedded PDB
			var withPdbFile = Compile(PdbKind.Portable);
			using (var module = ModuleDefMD.Load(withPdbFile.Image, new ModuleCreationOptions { TryToLoadPdbFromDisk = false }))
				Assert.Null(SymbolReaderFactory.TryCreateEmbeddedPdbReader(PdbReaderOptions.None, module.Metadata));
		}

		// The embedded PDB is the PDB of the file: another PDB that's passed in isn't used
		[Fact]
		public void EmbeddedPdbIsPreferredToAPdbThatIsPassedIn() {
			var otherPdb = CSharpCompiler.Compile("public class Other { public void M() { } }", new CSharpCompilerOptions { PdbKind = PdbKind.Portable, SourcePath = "/src/Other.cs" }).Pdb;
			var options = new ModuleCreationOptions { PdbFileOrData = otherPdb, TryToLoadPdbFromDisk = false };

			using (var module = ModuleDefMD.Load(imageWithEmbeddedPdb, options)) {
				Assert.Equal(PdbFileKind.EmbeddedPortablePDB, module.PdbState!.PdbFileKind);
				Assert.Equal("/src/Program.cs", Assert.Single(module.PdbState.Documents).Url);
			}
		}

		// System.Reflection.Metadata tries the PDB file first and uses the embedded PDB if that file can't be
		// read. dnlib tries the embedded PDB first, so a PDB file that can't be read changes nothing.
		[Theory]
		[InlineData("empty")]
		[InlineData("not a PDB")]
		[InlineData("only the signature of a portable PDB")]
		[InlineData("only the signature of a Windows PDB")]
		[InlineData("file that doesn't exist")]
		public void EmbeddedPdbIsReadIfThePdbThatIsPassedInCanNotBeRead(string pdb) {
			object pdbFileOrData = pdb switch {
				"empty" => new byte[0],
				"not a PDB" => System.Text.Encoding.ASCII.GetBytes("This isn't a PDB file"),
				"only the signature of a portable PDB" => new byte[] { 0x42, 0x53, 0x4A, 0x42 },
				"only the signature of a Windows PDB" => System.Text.Encoding.ASCII.GetBytes("Microsoft C/C++ MSF 7.00\r\n\u001ADS\0"),
				"file that doesn't exist" => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "no.pdb"),
				_ => throw new ArgumentException(pdb),
			};
			var options = new ModuleCreationOptions { PdbFileOrData = pdbFileOrData, TryToLoadPdbFromDisk = false };

			using (var module = ModuleDefMD.Load(imageWithEmbeddedPdb, options)) {
				Assert.Equal(PdbFileKind.EmbeddedPortablePDB, module.PdbState!.PdbFileKind);
				Assert.Equal("/src/Program.cs", Assert.Single(module.PdbState.Documents).Url);
			}
		}

		[Fact]
		public void EmbeddedPdbIsWrittenIntoTheNewFile() {
			using (var original = ModuleDefMD.Load(imageWithEmbeddedPdb)) {
				var written = Write(original);

				var pdb = GetEmbeddedPdb(written);
				Assert.NotNull(pdb);
				using (var writtenModule = ModuleDefMD.Load(written)) {
					Assert.Equal(PdbFileKind.EmbeddedPortablePDB, writtenModule.PdbState!.PdbFileKind);
					ModuleComparer.AssertEqual(original, writtenModule, new ModuleDumpOptions { Pdb = true });
				}
				// A stream for a PDB file isn't used
				var pdbStream = new MemoryStream();
				Assert.NotNull(GetEmbeddedPdb(Write(original, options => options.PdbStream = pdbStream)));
				Assert.Equal(0, pdbStream.Length);
				Assert.Equal(0, AssemblyRunner.RunEntryPoint(written));
			}
		}

		// The other entries of the debug directory are the same as for a PDB file: the id of the PDB and its checksum
		[Fact]
		public void WrittenFileHasTheIdOfTheEmbeddedPdb() {
			byte[] written;
			using (var original = ModuleDefMD.Load(imageWithEmbeddedPdb))
				written = Write(original, options => options.PdbOptions |= PdbWriterOptions.PdbChecksum);

			var pdb = GetEmbeddedPdb(written)!;
			using (var peReader = new PEReader(new MemoryStream(written)))
			using (var pdbProvider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(pdb))) {
				var entries = peReader.ReadDebugDirectory();
				var codeViewEntry = Assert.Single(entries, a => a.Type == DebugDirectoryEntryType.CodeView);
				Assert.True(codeViewEntry.IsPortableCodeView);
				var codeView = peReader.ReadCodeViewDebugDirectoryData(codeViewEntry);
				var pdbId = new BlobContentId(pdbProvider.GetMetadataReader().DebugMetadataHeader!.Id);
				Assert.Equal(pdbId.Guid, codeView.Guid);
				Assert.Equal(pdbId.Stamp, codeViewEntry.Stamp);
				Assert.Equal(1, codeView.Age);

				var embeddedEntry = Assert.Single(entries, a => a.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
				Assert.Equal(0x0100, embeddedEntry.MajorVersion);
				Assert.Equal(0x0100, embeddedEntry.MinorVersion);
				Assert.Equal(0U, embeddedEntry.Stamp);
				// System.Reflection.Metadata reads it from the PE file too
				using (var embeddedProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedEntry))
					Assert.Single(embeddedProvider.GetMetadataReader().Documents);

				// The checksum is the hash of the PDB with an id of zeroes
				var checksumEntry = Assert.Single(entries, a => a.Type == DebugDirectoryEntryType.PdbChecksum);
				var checksum = peReader.ReadPdbChecksumDebugDirectoryData(checksumEntry);
				Assert.Equal("SHA256", checksum.AlgorithmName);
				var pdbWithoutId = (byte[])pdb.Clone();
				int idOffset = FindId(pdb, pdbId);
				Array.Clear(pdbWithoutId, idOffset, 20);
				using (var sha256 = System.Security.Cryptography.SHA256.Create())
					Assert.Equal(sha256.ComputeHash(pdbWithoutId), checksum.Checksum.ToArray());
			}
		}

		// The offset of the 20 bytes of the PDB id (a GUID and a 32-bit stamp) in the PDB
		static int FindId(byte[] pdb, BlobContentId id) {
			var idBytes = id.Guid.ToByteArray().Concat(BitConverter.GetBytes(id.Stamp)).ToArray();
			for (int i = 0; i + idBytes.Length <= pdb.Length; i++) {
				if (pdb.Skip(i).Take(idBytes.Length).SequenceEqual(idBytes))
					return i;
			}
			throw new InvalidOperationException("The PDB id isn't in the PDB");
		}

		[Fact]
		public void PdbFileCanBeWrittenAsAnEmbeddedPdb() {
			var withPdbFile = Compile(PdbKind.Portable);
			using (var original = RoundTrip.LoadWithPdb(withPdbFile.Image, withPdbFile.Pdb!)) {
				original.PdbState!.PdbFileKind = PdbFileKind.EmbeddedPortablePDB;
				var pdbStream = new MemoryStream();

				var written = Write(original, options => options.PdbStream = pdbStream);

				// Nothing is written to the stream of the PDB file
				Assert.Equal(0, pdbStream.Length);
				Assert.NotNull(GetEmbeddedPdb(written));
				using (var writtenModule = ModuleDefMD.Load(written)) {
					Assert.Equal(PdbFileKind.EmbeddedPortablePDB, writtenModule.PdbState!.PdbFileKind);
					ModuleComparer.AssertEqual(original, writtenModule, new ModuleDumpOptions { Pdb = true });
				}
			}
		}

		[Fact]
		public void EmbeddedPdbCanBeWrittenAsAPdbFile() {
			using (var original = ModuleDefMD.Load(imageWithEmbeddedPdb)) {
				original.PdbState!.PdbFileKind = PdbFileKind.PortablePDB;

				var written = RoundTrip.WriteWithPortablePdb(original);

				Assert.Null(GetEmbeddedPdb(written.Image));
				Assert.NotEmpty(written.Pdb!);
				// The same PDB as the one that's written into the PE file
				original.PdbState.PdbFileKind = PdbFileKind.EmbeddedPortablePDB;
				var embeddedPdb = GetEmbeddedPdb(Write(original))!;
				Assert.Equal(PortablePdbTableDumper.Dump(written.Pdb!), PortablePdbTableDumper.Dump(embeddedPdb));
				original.PdbState.PdbFileKind = PdbFileKind.PortablePDB;
				using (var writtenModule = RoundTrip.LoadWithPdb(written.Image, written.Pdb!)) {
					Assert.Equal(PdbFileKind.PortablePDB, writtenModule.PdbState!.PdbFileKind);
					ModuleComparer.AssertEqual(original, writtenModule, new ModuleDumpOptions { Pdb = true });
				}
			}
		}

		[Fact]
		public void EmbeddedPdbIsNotWrittenWithoutWritePdb() {
			using (var original = ModuleDefMD.Load(imageWithEmbeddedPdb)) {
				var written = Write(original, options => options.WritePdb = false);

				Assert.Null(GetEmbeddedPdb(written));
				using (var writtenModule = ModuleDefMD.Load(written))
					Assert.Null(writtenModule.PdbState);
			}
		}

		// The name in the CodeView entry isn't needed to find an embedded PDB. It's the name of the options
		// without its directory, or the name of the module with the extension "pdb".
		[Theory]
		[InlineData(null, "TestAssembly.pdb")]
		[InlineData("/some/directory/Name.pdb", "Name.pdb")]
		public void NameOfTheEmbeddedPdbInTheDebugDirectory(string? pdbFileName, string expected) {
			byte[] written;
			using (var original = ModuleDefMD.Load(imageWithEmbeddedPdb))
				written = Write(original, options => options.PdbFileName = pdbFileName);

			using (var peReader = new PEReader(new MemoryStream(written))) {
				var codeViewEntry = Assert.Single(peReader.ReadDebugDirectory(), a => a.Type == DebugDirectoryEntryType.CodeView);
				Assert.Equal(expected, peReader.ReadCodeViewDebugDirectoryData(codeViewEntry).Path);
			}
		}

		// The runtime uses the embedded PDB that dnlib wrote for the line numbers of a stack trace
		[Fact]
		public void RuntimeReadsTheWrittenEmbeddedPdb() {
			Assert.SkipUnless(typeof(object).Assembly.GetName().Name == "System.Private.CoreLib", "Only .NET reads an embedded PDB of an assembly that's loaded from memory");

			byte[] written;
			using (var original = ModuleDefMD.Load(imageWithEmbeddedPdb))
				written = Write(original);

			using (var loaded = new LoadedAssembly(written)) {
				var method = loaded.Assembly.GetType("Program")!.GetMethod("Throw")!;
				var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(null, new object[] { "message" }));
				Assert.Contains("Program.cs:line 15", exception.InnerException!.StackTrace);
			}
		}
	}
}
