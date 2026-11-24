// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// A module that's loaded from a file isn't read into memory: the file is memory mapped and dnlib reads from
	/// it when something is needed, also while the module is written. Writing the module to the file it was
	/// loaded from would truncate the file that's being read (a crash on Unix, an error on Windows), so the
	/// writer throws an <see cref="IOException"/> before it changes the file.
	/// </summary>
	public sealed class WriteToLoadedFileTests : IDisposable {
		readonly TempDirectory directory = new TempDirectory();
		readonly string fileName;

		public WriteToLoadedFileTests() {
			fileName = directory.GetPath("Loaded.dll");
			File.WriteAllBytes(fileName, Fixtures.Fixtures.Features.Image);
		}

		public void Dispose() => directory.Dispose();

		void AssertFileIsUnchanged() =>
			Assert.Equal(Fixtures.Fixtures.Features.Image, File.ReadAllBytes(fileName));

		[Fact]
		public void WritingToTheLoadedFileThrows() {
			using (var module = ModuleDefMD.Load(fileName)) {
				var exception = Assert.Throws<IOException>(() => module.Write(fileName));

				Assert.Contains("memory mapped", exception.Message);
				// The module can still be used
				Assert.Contains(module.GetTypes(), a => a.Methods.Count != 0);
			}
			AssertFileIsUnchanged();
		}

		[Fact]
		public void NativeWriteToTheLoadedFileThrows() {
			using (var module = ModuleDefMD.Load(fileName))
				Assert.Throws<IOException>(() => module.NativeWrite(fileName));
			AssertFileIsUnchanged();
		}

		// The same file with another name: a path with "..", and a relative path
		[Fact]
		public void OtherPathOfTheLoadedFileThrowsToo() {
			var withDots = Path.Combine(Path.Combine(Path.Combine(directory.Path, ".."), Path.GetFileName(directory.Path)), "Loaded.dll");
			var relative = GetRelativePath(fileName);
			Assert.False(Path.IsPathRooted(relative));

			using (var module = ModuleDefMD.Load(fileName)) {
				Assert.Throws<IOException>(() => module.Write(withDots));
				Assert.Throws<IOException>(() => module.Write(relative));
			}
			using (var module = ModuleDefMD.Load(relative))
				Assert.Throws<IOException>(() => module.Write(fileName));
			AssertFileIsUnchanged();
		}

		// The path of a file from the current directory, eg. "..\..\tmp\Loaded.dll"
		static string GetRelativePath(string fullPath) {
			var relative = new Uri(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar).MakeRelativeUri(new Uri(fullPath));
			return Uri.UnescapeDataString(relative.ToString()).Replace('/', Path.DirectorySeparatorChar);
		}

		// The names of two files can differ only in case if the file system is case sensitive. dnlib doesn't know
		// if it is, and refuses to write the file if a file with the other name exists too.
		[Fact]
		public void NameThatOnlyDiffersInCaseIsTheLoadedFileIfItExists() {
			var otherCase = directory.GetPath("LOADED.DLL");
			using (var module = ModuleDefMD.Load(fileName)) {
				if (File.Exists(otherCase)) {
					// Case insensitive file system (Windows, macOS): it's the loaded file
					Assert.Throws<IOException>(() => module.Write(otherCase));
				}
				else {
					// It's a new file
					module.Write(otherCase);
					Assert.Throws<IOException>(() => module.Write(otherCase));
				}
			}
			AssertFileIsUnchanged();
		}

		// Returns false if this user can't create symbolic links (Windows)
		static bool TryCreateSymbolicLink(string link, string target) {
#if NET
			try {
				File.CreateSymbolicLink(link, target);
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) {
				return false;
			}
#else
			// .NET Framework has no method that creates one. Mono runs these tests on Unix.
			if (Path.DirectorySeparatorChar != '/')
				return false;
			using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ln", $"-s \"{target}\" \"{link}\"") { UseShellExecute = false }))
				process.WaitForExit();
#endif
			return File.Exists(link);
		}

		// Without the check, this crashes the process on Unix
		[Fact]
		public void SymbolicLinkToTheLoadedFileThrowsToo() {
			var link = directory.GetPath("Link.dll");
			Assert.SkipUnless(TryCreateSymbolicLink(link, fileName), "A symbolic link can't be created");

			using (var module = ModuleDefMD.Load(fileName))
				Assert.Throws<IOException>(() => module.Write(link));
			using (var module = ModuleDefMD.Load(link))
				Assert.Throws<IOException>(() => module.Write(fileName));
			AssertFileIsUnchanged();
		}

		[Fact]
		public void WritingToAnotherFileWorks() {
			var otherFile = directory.GetPath("Other.dll");
			using (var module = ModuleDefMD.Load(fileName)) {
				module.Write(otherFile);

				using (var written = ModuleDefMD.Load(File.ReadAllBytes(otherFile)))
					ModuleComparer.AssertEqual(module, written);
			}
			AssertFileIsUnchanged();
		}

		// A module that isn't memory mapped can be written to the file it came from: one that's loaded from the
		// file's bytes, or after the file was read into memory
		[Theory, InlineData(false), InlineData(true)]
		public void ModuleThatIsNotMemoryMappedCanOverwriteItsFile(bool loadFromBytes) {
			using (var module = loadFromBytes ? ModuleDefMD.Load(File.ReadAllBytes(fileName)) : ModuleDefMD.Load(fileName)) {
				if (!loadFromBytes)
					((dnlib.PE.IInternalPEImage)module.Metadata.PEImage).UnsafeDisableMemoryMappedIO();
				module.Name = "Renamed.dll";

				module.Write(fileName);
			}

			using (var written = ModuleDefMD.Load(File.ReadAllBytes(fileName)))
			using (var expected = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image)) {
				expected.Name = "Renamed.dll";
				ModuleComparer.AssertEqual(expected, written);
			}
		}

		// The file is created first and deleted again if the module can't be written
		[Fact]
		public void FileIsDeletedIfWritingFails() {
			var otherFile = directory.GetPath("Failed.dll");
			using (var module = ModuleDefMD.Load(fileName)) {
				var options = new ModuleWriterOptions(module);
				options.WriterEvent += (sender, e) => {
					if (e.Event == ModuleWriterEvent.EndWriteChunks)
						throw new InvalidOperationException("Stop");
				};

				Assert.Throws<InvalidOperationException>(() => module.Write(otherFile, options));
			}
			Assert.False(File.Exists(otherFile));
		}
	}
}
