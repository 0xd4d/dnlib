// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Resolving {
	/// <summary>
	/// A reference to a Windows Runtime assembly has the content type "Windows Runtime" in its flags. Its file
	/// is a <c>.winmd</c> file and not a DLL: the resolver looks for it in the search paths and in the directory
	/// of Windows that has the metadata of Windows itself (<c>System32\WinMetadata</c>).
	/// </summary>
	public sealed class WinMDResolutionTests : IDisposable {
		readonly TempDirectory directory = new TempDirectory();

		public void Dispose() => directory.Dispose();

		static AssemblyRefUser CreateReference(string name, bool isWindowsRuntime) =>
			new AssemblyRefUser(name, new Version(255, 255, 255, 255)) {
				Attributes = isWindowsRuntime ? AssemblyAttributes.ContentType_WindowsRuntime : AssemblyAttributes.None,
			};

		// Resolves a reference and returns the file that was found, or null
		static string? Resolve(AssemblyResolver resolver, IAssembly reference) {
			resolver.DefaultModuleContext = new ModuleContext(resolver);
			try {
				return resolver.Resolve(reference, sourceModule: null)?.ManifestModule!.Location;
			}
			finally {
				// The resolver keeps the files open
				foreach (var module in resolver.GetCachedAssemblies().Distinct().SelectMany(a => a.Modules).ToList())
					module.Dispose();
				resolver.Clear();
			}
		}

		// The same assembly as a DLL and as a .winmd file in a search path
		[Theory]
		[InlineData(true, "Library.winmd")]
		[InlineData(false, "Library.dll")]
		public void WindowsRuntimeReferenceIsAWinmdFile(bool isWindowsRuntime, string expectedFile) {
			var image = CSharpCompiler.Compile("public class C { }", new CSharpCompilerOptions { AssemblyName = "Library" }).Image;
			File.WriteAllBytes(directory.GetPath("Library.dll"), image);
			File.WriteAllBytes(directory.GetPath("Library.winmd"), image);
			var resolver = new AssemblyResolver();
			resolver.PreSearchPaths.Add(directory.Path);

			var file = Resolve(resolver, CreateReference("Library", isWindowsRuntime));

			Assert.Equal(directory.GetPath(expectedFile), file);
		}

		[Theory, InlineData(true), InlineData(false)]
		public void WinmdFileIsNotFoundIfOnlyTheOtherFileExists(bool isWindowsRuntime) {
			var image = CSharpCompiler.Compile("public class C { }", new CSharpCompilerOptions { AssemblyName = "Library" }).Image;
			File.WriteAllBytes(directory.GetPath(isWindowsRuntime ? "Library.dll" : "Library.winmd"), image);
			var resolver = new AssemblyResolver();
			resolver.PreSearchPaths.Add(directory.Path);

			Assert.Null(Resolve(resolver, CreateReference("Library", isWindowsRuntime)));
		}

		// Windows.Foundation.winmd is a file of every Windows since Windows 10
		[Fact]
		public void MetadataOfWindowsIsFoundInItsDirectory() {
			Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Only Windows has its metadata files");
			var expectedFile = Path.Combine(Path.Combine(Environment.SystemDirectory, "WinMetadata"), "Windows.Foundation.winmd");
			Assert.SkipUnless(File.Exists(expectedFile), expectedFile + " doesn't exist");
			var resolver = new AssemblyResolver();

			var file = Resolve(resolver, CreateReference("Windows.Foundation", isWindowsRuntime: true));

			Assert.Equal(expectedFile, file, ignoreCase: true);
			// It isn't found without the content type: then it's the name of a DLL
			Assert.Null(Resolve(resolver, CreateReference("Windows.Foundation", isWindowsRuntime: false)));
		}

		// A name with a directory is no file name: no file of another directory is opened
		[Fact]
		public void NameWithADirectoryIsNotFoundInTheDirectoryOfWindows() {
			Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Only Windows has its metadata files");
			var windowsFile = Path.Combine(Path.Combine(Environment.SystemDirectory, "WinMetadata"), "Windows.Foundation.winmd");
			Assert.SkipUnless(File.Exists(windowsFile), windowsFile + " doesn't exist");
			Directory.CreateDirectory(directory.GetPath("sub"));
			File.Copy(windowsFile, Path.Combine(directory.GetPath("sub"), "Windows.Foundation.winmd"));
			var resolver = new AssemblyResolver();
			resolver.PreSearchPaths.Add(directory.Path);

			Assert.Null(Resolve(resolver, CreateReference("sub\\Windows.Foundation", isWindowsRuntime: true)));
			Assert.Null(Resolve(resolver, CreateReference("..\\WinMetadata\\Windows.Foundation", isWindowsRuntime: true)));
		}
	}
}
