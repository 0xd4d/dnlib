// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Loading {
	/// <summary>
	/// A module that is loaded from a file is read through a memory mapped view of the file. The tests load
	/// files that can't be mapped or that are in use.
	/// </summary>
	public sealed class FileAccessTests : IDisposable {
		readonly TempDirectory directory = new TempDirectory();

		public void Dispose() => directory.Dispose();

		static readonly byte[] image = CSharpCompiler.Compile("public class C { }").Image;

		string WriteFile(byte[] content, string name = "File.dll") {
			var file = directory.GetPath(name);
			File.WriteAllBytes(file, content);
			return file;
		}

		// An empty file can't be mapped: it's read like any other file that's too short
		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(63)]
		[InlineData(64)]
		public void FileThatIsTooShortIsNotAModule(int length) {
			var file = WriteFile(image.Take(length).ToArray());

			Assert.Throws<BadImageFormatException>(() => ModuleDefMD.Load(file));
			Assert.Throws<BadImageFormatException>(() => AssemblyDef.Load(file));
			// The file isn't open or mapped after that
			File.Delete(file);
		}

		[Fact]
		public void FileThatDoesNotExistThrows() {
			Assert.ThrowsAny<IOException>(() => ModuleDefMD.Load(directory.GetPath("NoSuchFile.dll")));
			Assert.ThrowsAny<IOException>(() => ModuleDefMD.Load(Path.Combine(directory.GetPath("NoSuchDirectory"), "File.dll")));
			// A directory isn't a file
			Assert.ThrowsAny<Exception>(() => ModuleDefMD.Load(directory.Path));
		}

		// Other code that only reads the file doesn't keep it from being loaded
		[Fact]
		public void FileThatIsOpenForReadingIsLoaded() {
			var file = WriteFile(image);
			using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (var module = ModuleDefMD.Load(file))
					Assert.NotNull(module.Find("C", isReflectionName: true));
			}
		}

		// The file is opened for reading and other code may read it too, but not write to it: opening it fails if
		// nothing else may open it, and on Windows also if it's open for writing (other systems don't check that)
		[Theory]
		[InlineData(FileAccess.ReadWrite, FileShare.ReadWrite, false)]
		[InlineData(FileAccess.Read, FileShare.None, true)]
		public void FileThatIsInUseThrowsAnIOException(FileAccess access, FileShare share, bool throwsOnEverySystem) {
			var file = WriteFile(image);
			using (new FileStream(file, FileMode.Open, access, share)) {
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || (throwsOnEverySystem && !Reading.ClrReaderComparison.IsMono))
					Assert.ThrowsAny<IOException>(() => ModuleDefMD.Load(file));
				else if (!throwsOnEverySystem) {
					using (var module = ModuleDefMD.Load(file))
						Assert.NotNull(module.Find("C", isReflectionName: true));
				}
				else {
					// Mono doesn't always know that the file is in use: it's loaded or it's an IOException
					try {
						ModuleDefMD.Load(file).Dispose();
					}
					catch (IOException) {
					}
				}
			}

			// It's loaded when the other code has closed it
			using (var module = ModuleDefMD.Load(file))
				Assert.NotNull(module.Find("C", isReflectionName: true));
		}

		// The view of the file is closed when the module is disposed: the file can then be deleted
		[Fact]
		public void FileIsNotInUseAfterTheModuleIsDisposed() {
			var file = WriteFile(image);
			var module = ModuleDefMD.Load(file);
			Assert.NotNull(module.Find("C", isReflectionName: true));

			module.Dispose();

			File.WriteAllBytes(file, new byte[1]);
			File.Delete(file);
			Assert.False(File.Exists(file));
		}

		// A PE file can't be bigger than 4 GB, but a file can have other data after it. The file of this test has no
		// data there (a "sparse" file: the file system doesn't store it), which Windows would still reserve room for.
		// Fixed in this commit: only the low 32 bits of the file's size were used, so a file with 4 GB and a few
		// bytes was a file with a few bytes.
		[Theory]
		[InlineData(0x100000000L + 16)]
		[InlineData(0x100000000L)]
		[InlineData(0xFFFFFFFFL)]
		[InlineData(0x180000000L)]
		public void FileWithMoreThan4GBIsLoaded(long fileSize) {
			// Set DNLIB_TESTS_BIG_FILES=1 to run it on Windows too
			Assert.SkipWhen(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.GetEnvironmentVariable("DNLIB_TESTS_BIG_FILES") != "1", "The file would use 4 GB of the disk on Windows");
			Assert.SkipWhen(Reading.ClrReaderComparison.IsMono, "Mono can't map a file of this size");
			var file = WriteFile(image);
			using (var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
				stream.SetLength(fileSize);

			using (var module = ModuleDefMD.Load(file))
				Assert.NotNull(module.Find("C", isReflectionName: true));
		}
	}
}
