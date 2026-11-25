// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.RoundTrips {
	/// <summary>
	/// Mixed mode assemblies: C++/CLI code compiled with Microsoft's C++ compiler. The file has native code and
	/// data next to the metadata, methods whose body is native code, and a table (vtable fixups) through which
	/// native code calls managed code. Only <see cref="NativeModuleWriter"/> can write such a file: it keeps
	/// everything that isn't metadata where it is.
	///
	/// The compiler needs Windows and Visual Studio with C++/CLI support, the tests are skipped without it.
	/// The compiled file is for .NET Framework, so it's only loaded and run by the .NET Framework test runs.
	/// </summary>
	public class CppCliTests {
		static readonly Lazy<byte[]> image = new Lazy<byte[]>(() => CppCliCompiler.Compile(Fixtures.Fixtures.ReadSource("MixedMode.cpp")));

		static void SkipWithoutTheCompiler() =>
			Assert.SkipWhen(CppCliCompiler.WhyNotAvailable is not null, CppCliCompiler.WhyNotAvailable ?? "");

		// The other tests are skipped if the compiler is missing. On the Windows machines of GitHub Actions it
		// must be there, or nothing would say that these tests don't run anymore.
		[Fact]
		public void CompilerIsAvailableOnGitHubActions() {
			bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
			Assert.SkipUnless(isWindows && Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true", "Not a Windows machine of GitHub Actions");

			Assert.Null(CppCliCompiler.WhyNotAvailable);
		}

		[Fact]
		public void CompiledFileIsAMixedModeAssembly() {
			SkipWithoutTheCompiler();
			using (var module = ModuleDefMD.Load(image.Value)) {
				Assert.False(module.IsILOnly);
				Assert.Equal(dnlib.PE.Machine.AMD64, module.Machine);
				Assert.NotNull(module.Assembly);

				var calculator = module.Find("MixedMode.Calculator", isReflectionName: true)!;
				Assert.NotNull(calculator.FindMethod("Add"));
				Assert.True(calculator.FindMethod("Add")!.HasBody);
				// The native functions are global methods without IL code: their RVA is native code
				var nativeMethods = module.GlobalType!.Methods.Where(a => a.IsNative).ToList();
				Assert.NotEmpty(nativeMethods);
				Assert.All(nativeMethods, a => {
					Assert.Null(a.Body);
					Assert.NotNull(a.NativeBody);
					Assert.NotEqual((dnlib.PE.RVA)0, a.NativeBody!.RVA);
				});
				Assert.Contains(nativeMethods, a => a.Name.Contains("NativeAdd"));
			}
		}

		[Fact]
		public void EverythingCanBeRead() {
			SkipWithoutTheCompiler();
			using (var onDemand = ModuleDefMD.Load(image.Value))
			using (var everything = ModuleDefMD.Load(image.Value)) {
				everything.LoadEverything();

				ModuleComparer.AssertEqual(onDemand, everything, new ModuleDumpOptions { MaxStack = true });
				Assert.NotEmpty(new MemberFinder().FindAll(everything).MethodDefs);
			}
		}

		static byte[] NativeWrite(ModuleDefMD module, bool optimizeImageSize) {
			var stream = new MemoryStream();
			module.NativeWrite(stream, new NativeModuleWriterOptions(module, optimizeImageSize));
			return stream.ToArray();
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void ModuleIsTheSameAfterANativeWrite(bool optimizeImageSize) {
			SkipWithoutTheCompiler();
			using (var original = ModuleDefMD.Load(image.Value)) {
				var written = NativeWrite(original, optimizeImageSize);

				using (var writtenModule = ModuleDefMD.Load(written)) {
					ModuleComparer.AssertEqual(original, writtenModule, new ModuleDumpOptions { MaxStack = true });
					Assert.False(writtenModule.IsILOnly);
					// The native code didn't move
					var originalNative = original.GlobalType!.Methods.Where(a => a.IsNative).ToDictionary(a => a.FullName, a => a.NativeBody!.RVA);
					var writtenNative = writtenModule.GlobalType!.Methods.Where(a => a.IsNative).ToDictionary(a => a.FullName, a => a.NativeBody!.RVA);
					Assert.Equal(originalNative, writtenNative);
				}
			}
		}

		// A change that needs new metadata: a renamed method and a new class
		[Fact]
		public void ChangedModuleIsWrittenWithItsChanges() {
			SkipWithoutTheCompiler();
			using (var module = ModuleDefMD.Load(image.Value)) {
				module.Find("MixedMode.Calculator", isReflectionName: true)!.FindMethod("Add")!.Name = "Sum";
				module.Types.Add(new TypeDefUser("MixedMode", "Added", module.CorLibTypes.Object.TypeDefOrRef));

				var written = NativeWrite(module, optimizeImageSize: true);

				using (var writtenModule = ModuleDefMD.Load(written)) {
					var calculator = writtenModule.Find("MixedMode.Calculator", isReflectionName: true)!;
					Assert.NotNull(calculator.FindMethod("Sum"));
					Assert.Null(calculator.FindMethod("Add"));
					Assert.NotNull(writtenModule.Find("MixedMode.Added", isReflectionName: true));
				}
			}
		}

#if NETFRAMEWORK
		// Loads a mixed mode assembly from a file (it can't be loaded from memory) and calls its methods
		static void AssertRuns(byte[] mixedModeImage, string addMethodName) {
			Assert.SkipUnless(Environment.Is64BitProcess, "The file is a 64-bit file");
			// Not deleted: a loaded assembly can't be unloaded on .NET Framework
			var directory = new TempDirectory();
			var file = directory.GetPath("MixedMode.dll");
			File.WriteAllBytes(file, mixedModeImage);
			var assembly = System.Reflection.Assembly.LoadFile(file);
			var calculator = assembly.GetType("MixedMode.Calculator", throwOnError: true);

			Assert.Equal(5, calculator.GetMethod(addMethodName).Invoke(null, new object[] { 2, 3 }));
			Assert.Equal(42, calculator.GetMethod("Counter").Invoke(null, null));
			Assert.Equal("text", calculator.GetMethod("Echo").MakeGenericMethod(typeof(string)).Invoke(null, new object[] { "text" }));
			var rectangle = Activator.CreateInstance(assembly.GetType("MixedMode.Rectangle", throwOnError: true), 3, 4);
			Assert.Equal(12, calculator.GetMethod("AreaOf").Invoke(null, new[] { rectangle }));
			Assert.Equal(-1, calculator.GetMethod("AreaOf").Invoke(null, new object?[] { null }));
		}

		[Fact]
		public void CompiledFileRuns() {
			SkipWithoutTheCompiler();
			AssertRuns(image.Value, "Add");
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void FileRunsAfterANativeWrite(bool optimizeImageSize) {
			SkipWithoutTheCompiler();
			using (var module = ModuleDefMD.Load(image.Value))
				AssertRuns(NativeWrite(module, optimizeImageSize), "Add");
		}

		[Fact]
		public void ChangedFileRuns() {
			SkipWithoutTheCompiler();
			using (var module = ModuleDefMD.Load(image.Value)) {
				module.Find("MixedMode.Calculator", isReflectionName: true)!.FindMethod("Add")!.Name = "Sum";
				AssertRuns(NativeWrite(module, optimizeImageSize: true), "Sum");
			}
		}
#endif
	}
}
