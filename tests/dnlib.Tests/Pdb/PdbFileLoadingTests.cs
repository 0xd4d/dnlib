// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// Where the PDB of a module comes from. By default it's a file in the directory of the module: the file
	/// with the name that's in the module's debug directory, or else the name of the module with the extension
	/// <c>pdb</c>. The options and <c>LoadPdb()</c> take a file name, the bytes of a PDB or a stream. The writer
	/// creates the PDB file next to the file it writes.
	/// </summary>
	public sealed class PdbFileLoadingTests : IDisposable {
		const string Source = @"
public static class Program {
	public static int Main() {
		int value = 5;
		return value - 5;
	}
}
";

		static readonly CompiledAssembly compiled = CSharpCompiler.Compile(Source,
			new CSharpCompilerOptions { PdbKind = PdbKind.Portable, OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication, Optimize = false, SourcePath = "/src/Program.cs" });

		// The name of the PDB file that the compiler stored in the PE file
		static string GetPdbNameInTheDebugDirectory(byte[] image) {
			using (var peReader = new PEReader(new MemoryStream(image))) {
				var entry = peReader.ReadDebugDirectory().Single(a => a.Type == DebugDirectoryEntryType.CodeView);
				return peReader.ReadCodeViewDebugDirectoryData(entry).Path;
			}
		}

		readonly TempDirectory directory = new TempDirectory();

		public void Dispose() => directory.Dispose();

		string PathOf(string name) => Path.Combine(directory.Path, name);

		string WriteFile(string name, byte[] content) {
			File.WriteAllBytes(PathOf(name), content);
			return PathOf(name);
		}

		static void AssertHasDebugInfo(ModuleDefMD module) {
			Assert.NotNull(module.PdbState);
			Assert.Equal(PdbFileKind.PortablePDB, module.PdbState.PdbFileKind);
			Assert.Equal("/src/Program.cs", Assert.Single(module.PdbState.Documents).Url);
			var main = module.Find("Program", isReflectionName: true)!.FindMethod("Main")!;
			Assert.Contains(main.Body!.Instructions, a => a.SequencePoint is not null);
			Assert.Contains(main.Body.Variables, a => a.Name == "value");
		}

		[Fact]
		public void PdbWithTheNameInTheDebugDirectoryIsLoaded() {
			string pdbName = GetPdbNameInTheDebugDirectory(compiled.Image);
			Assert.Equal("TestAssembly.pdb", pdbName);
			string modulePath = WriteFile("Renamed.exe", compiled.Image);
			WriteFile(pdbName, compiled.Pdb!);

			using (var module = ModuleDefMD.Load(modulePath))
				AssertHasDebugInfo(module);
		}

		// The CodeView entry of the debug directory: "RSDS", a GUID, the age and then the name of the PDB file,
		// 24 bytes before the name. Returns the file with another size of that data, and another name if
		// newName isn't null (it's written over the start of the old name; it's shorter).
		static byte[] ChangeCodeViewData(int newSize, byte[]? newName = null) {
			var image = (byte[])compiled.Image.Clone();
			using (var peReader = new PEReader(new MemoryStream(compiled.Image))) {
				var entries = peReader.ReadDebugDirectory();
				int index = entries.IndexOf(entries.Single(a => a.Type == DebugDirectoryEntryType.CodeView));
				Assert.True(newSize <= entries[index].DataSize);
				Assert.True(peReader.PEHeaders.TryGetDirectoryOffset(peReader.PEHeaders.PEHeader!.DebugTableDirectory, out int tableOffset));
				// An entry has 28 bytes, the size of its data is at offset 16
				BitConverter.GetBytes(newSize).CopyTo(image, tableOffset + index * 28 + 16);
				newName?.CopyTo(image, entries[index].DataPointer + CodeViewNameOffset);
			}
			return image;
		}
		const int CodeViewNameOffset = 24;
		const string CodeViewName = "TestAssembly.pdb";

		// Fixed in 0d73bed1: dnlib needed a zero byte after the name. Without it, it read
		// no PDB file at all, also not one that was passed to it. Like System.Reflection.Metadata, the name now
		// ends where the data ends.
		[Fact]
		public void NameInTheDebugDirectoryCanEndWithoutAZeroByte() {
			var image = ChangeCodeViewData(CodeViewNameOffset + CodeViewName.Length);
			Assert.Equal(CodeViewName, GetPdbNameInTheDebugDirectory(image));
			string modulePath = WriteFile("Renamed.exe", image);
			WriteFile(CodeViewName, compiled.Pdb!);

			using (var module = ModuleDefMD.Load(modulePath))
				AssertHasDebugInfo(module);
			using (var module = ModuleDefMD.Load(image, new ModuleCreationOptions { PdbFileOrData = compiled.Pdb!, TryToLoadPdbFromDisk = false }))
				AssertHasDebugInfo(module);
		}

		// The name ends at the first zero byte, whatever follows it
		[Fact]
		public void BytesAfterTheNameInTheDebugDirectoryAreIgnored() {
			var image = ChangeCodeViewData(CodeViewNameOffset + CodeViewName.Length + 1, new byte[] { (byte)'x', 0, 0x20, 0xFF });
			Assert.Equal("x", GetPdbNameInTheDebugDirectory(image));
			string modulePath = WriteFile("Renamed.exe", image);
			WriteFile("x", compiled.Pdb!);

			using (var module = ModuleDefMD.Load(modulePath))
				AssertHasDebugInfo(module);
		}

		// No name at all: the PDB file has the name of the module
		[Fact]
		public void DebugDirectoryWithoutANameOfThePdbFile() {
			var image = ChangeCodeViewData(CodeViewNameOffset);
			Assert.Equal("", GetPdbNameInTheDebugDirectory(image));
			string modulePath = WriteFile("Renamed.exe", image);
			WriteFile("Renamed.pdb", compiled.Pdb!);

			using (var module = ModuleDefMD.Load(modulePath))
				AssertHasDebugInfo(module);
		}

		// The data ends in the GUID or in the age: without them, dnlib can't know that a PDB file is the
		// module's file, and reads none
		[Theory]
		[InlineData(CodeViewNameOffset - 1)]
		[InlineData(4)]
		[InlineData(1)]
		public void NoPdbIsLoadedIfTheDebugDirectoryHasNoIdOfThePdbFile(int size) {
			var image = ChangeCodeViewData(size);
			string modulePath = WriteFile("Renamed.exe", image);
			WriteFile("Renamed.pdb", compiled.Pdb!);
			WriteFile(CodeViewName, compiled.Pdb!);

			using (var module = ModuleDefMD.Load(modulePath))
				Assert.Null(module.PdbState);
			using (var module = ModuleDefMD.Load(image, new ModuleCreationOptions { PdbFileOrData = compiled.Pdb!, TryToLoadPdbFromDisk = false }))
				Assert.Null(module.PdbState);
		}

		[Fact]
		public void PdbWithTheNameOfTheModuleIsLoaded() {
			string modulePath = WriteFile("Renamed.exe", compiled.Image);
			WriteFile("Renamed.pdb", compiled.Pdb!);

			using (var module = ModuleDefMD.Load(modulePath))
				AssertHasDebugInfo(module);
		}

		[Fact]
		public void ModuleWithoutAPdbFileHasNoDebugInfo() {
			string modulePath = WriteFile("Renamed.exe", compiled.Image);
			WriteFile("Other.pdb", compiled.Pdb!);

			using (var module = ModuleDefMD.Load(modulePath)) {
				Assert.Null(module.PdbState);
				Assert.Equal(0, module.Find("Program", isReflectionName: true)!.FindMethod("Main")!.Body!.Variables.Count(a => a.Name == "value"));
			}
		}

		[Fact]
		public void PdbFileIsNotLoadedIfTryToLoadPdbFromDiskIsFalse() {
			string modulePath = WriteFile("TestAssembly.exe", compiled.Image);
			WriteFile("TestAssembly.pdb", compiled.Pdb!);

			using (var module = ModuleDefMD.Load(modulePath, new ModuleCreationOptions { TryToLoadPdbFromDisk = false })) {
				Assert.Null(module.PdbState);

				// LoadPdb() without a file name looks for the same file
				module.LoadPdb();
				AssertHasDebugInfo(module);
			}
		}

		// A file that isn't a PDB, and an empty file
		[Theory]
		[InlineData(new byte[0])]
		[InlineData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
		public void FileThatIsNotAPdbIsNotLoaded(byte[] content) {
			string modulePath = WriteFile("TestAssembly.exe", compiled.Image);
			WriteFile("TestAssembly.pdb", content);

			using (var module = ModuleDefMD.Load(modulePath))
				Assert.Null(module.PdbState);
		}

		[Fact]
		public void PdbFileOfTheOptionsIsLoaded() {
			string modulePath = WriteFile("TestAssembly.exe", compiled.Image);
			Directory.CreateDirectory(PathOf("symbols"));
			string pdbPath = WriteFile(Path.Combine("symbols", "Some.pdb"), compiled.Pdb!);

			using (var module = ModuleDefMD.Load(modulePath, new ModuleCreationOptions { PdbFileOrData = pdbPath }))
				AssertHasDebugInfo(module);
			using (var module = ModuleDefMD.Load(compiled.Image, new ModuleCreationOptions { PdbFileOrData = pdbPath }))
				AssertHasDebugInfo(module);
			// A file that doesn't exist
			using (var module = ModuleDefMD.Load(modulePath, new ModuleCreationOptions { PdbFileOrData = PathOf("DoesNotExist.pdb") }))
				Assert.Null(module.PdbState);
		}

		// If the file of the options doesn't exist, the PDB file next to the module is still loaded
		[Fact]
		public void PdbNextToTheModuleIsLoadedIfTheFileOfTheOptionsDoesNotExist() {
			string modulePath = WriteFile("TestAssembly.exe", compiled.Image);
			WriteFile("TestAssembly.pdb", compiled.Pdb!);

			using (var module = ModuleDefMD.Load(modulePath, new ModuleCreationOptions { PdbFileOrData = PathOf("DoesNotExist.pdb") }))
				AssertHasDebugInfo(module);
			using (var module = ModuleDefMD.Load(modulePath, new ModuleCreationOptions { PdbFileOrData = PathOf("DoesNotExist.pdb"), TryToLoadPdbFromDisk = false }))
				Assert.Null(module.PdbState);
		}

		[Fact]
		public void PdbStreamOfTheOptionsIsLoaded() {
			var options = new ModuleCreationOptions { PdbFileOrData = ByteArrayDataReaderFactory.Create(compiled.Pdb!, filename: null) };

			using (var module = ModuleDefMD.Load(compiled.Image, options))
				AssertHasDebugInfo(module);
		}

		public static TheoryData<string> LoadPdbOverloads => new TheoryData<string> { "file name", "bytes", "stream", "file name and options", "bytes and options", "stream and options" };

		[Theory, MemberData(nameof(LoadPdbOverloads))]
		public void PdbCanBeLoadedAfterTheModule(string overload) {
			string pdbPath = WriteFile("Some.pdb", compiled.Pdb!);
			using (var module = ModuleDefMD.Load(compiled.Image)) {
				Assert.Null(module.PdbState);

				switch (overload) {
				case "file name": module.LoadPdb(pdbPath); break;
				case "bytes": module.LoadPdb(compiled.Pdb!); break;
				case "stream": module.LoadPdb(ByteArrayDataReaderFactory.Create(compiled.Pdb!, filename: null)); break;
				case "file name and options": module.LoadPdb(PdbReaderOptions.NoDiaSymReader, pdbPath); break;
				case "bytes and options": module.LoadPdb(PdbReaderOptions.NoDiaSymReader, compiled.Pdb!); break;
				case "stream and options": module.LoadPdb(PdbReaderOptions.NoDiaSymReader, ByteArrayDataReaderFactory.Create(compiled.Pdb!, filename: null)); break;
				default: throw new ArgumentException(overload);
				}

				AssertHasDebugInfo(module);
				// A module has one PDB
				Assert.Throws<InvalidOperationException>(() => module.LoadPdb(compiled.Pdb!));
			}
		}

		[Fact]
		public void PdbThatCanNotBeLoadedLaterIsIgnored() {
			using (var module = ModuleDefMD.Load(compiled.Image)) {
				module.LoadPdb(PathOf("DoesNotExist.pdb"));
				Assert.Null(module.PdbState);
				module.LoadPdb(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
				Assert.Null(module.PdbState);
				module.LoadPdb((dnlib.DotNet.Pdb.Symbols.SymbolReader?)null);
				Assert.Null(module.PdbState);
				// No file name is known for a module that was loaded from bytes
				module.LoadPdb();
				Assert.Null(module.PdbState);

				module.LoadPdb(compiled.Pdb!);
				AssertHasDebugInfo(module);
			}
		}

		// The files of a module that was disposed can be deleted (Windows doesn't delete a file that's open)
		[Fact]
		public void DisposedModuleHasClosedItsPdbFile() {
			string modulePath = WriteFile("TestAssembly.exe", compiled.Image);
			string pdbPath = WriteFile("TestAssembly.pdb", compiled.Pdb!);

			using (var module = ModuleDefMD.Load(modulePath))
				AssertHasDebugInfo(module);

			File.Delete(pdbPath);
			File.Delete(modulePath);
			Assert.False(File.Exists(pdbPath));
			Assert.False(File.Exists(modulePath));
		}

		[Fact]
		public void WriterCreatesThePdbFileNextToTheWrittenFile() {
			string writtenPath = PathOf("Written.exe");
			using (var module = RoundTrip.LoadWithPdb(compiled.Image, compiled.Pdb!))
				module.Write(writtenPath, new ModuleWriterOptions(module) { WritePdb = true });

			Assert.True(File.Exists(PathOf("Written.pdb")));
			Assert.Equal(PathOf("Written.pdb"), GetPdbNameInTheDebugDirectory(File.ReadAllBytes(writtenPath)));
			using (var written = ModuleDefMD.Load(writtenPath))
				AssertHasDebugInfo(written);
			Assert.Equal(new[] { "Written.exe", "Written.pdb" }, Directory.GetFiles(directory.Path).Select(Path.GetFileName).OrderBy(a => a).ToArray());
		}

		[Fact]
		public void WriterCreatesThePdbFileOfTheOptions() {
			string writtenPath = PathOf("Written.exe");
			string pdbPath = PathOf("Symbols.pdb");
			using (var module = RoundTrip.LoadWithPdb(compiled.Image, compiled.Pdb!)) {
				var options = new ModuleWriterOptions(module) { WritePdb = true, PdbFileName = pdbPath, PdbFileNameInDebugDirectory = "InDebugDirectory.pdb" };
				module.Write(writtenPath, options);
			}

			Assert.True(File.Exists(pdbPath));
			Assert.False(File.Exists(PathOf("Written.pdb")));
			Assert.Equal("InDebugDirectory.pdb", GetPdbNameInTheDebugDirectory(File.ReadAllBytes(writtenPath)));
			// Neither "InDebugDirectory.pdb" nor "Written.pdb" exists
			using (var written = ModuleDefMD.Load(writtenPath))
				Assert.Null(written.PdbState);
			using (var written = ModuleDefMD.Load(writtenPath, new ModuleCreationOptions { PdbFileOrData = pdbPath }))
				AssertHasDebugInfo(written);
		}

		// No PDB file is created if the module has no debug info or if WritePdb isn't set
		[Fact]
		public void WriterCreatesNoPdbFileWithoutDebugInfo() {
			using (var module = ModuleDefMD.Load(compiled.Image))
				module.Write(PathOf("NoDebugInfo.exe"), new ModuleWriterOptions(module) { WritePdb = true });
			using (var module = RoundTrip.LoadWithPdb(compiled.Image, compiled.Pdb!))
				module.Write(PathOf("NoWritePdb.exe"));

			Assert.Equal(new[] { "NoDebugInfo.exe", "NoWritePdb.exe" }, Directory.GetFiles(directory.Path).Select(Path.GetFileName).OrderBy(a => a).ToArray());
		}
	}
}
