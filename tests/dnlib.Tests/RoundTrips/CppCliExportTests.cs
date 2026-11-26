// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.RoundTrips {
	/// <summary>
	/// A mixed mode DLL (C++/CLI) that exports managed functions: native code of another program can call them.
	/// The export table has the address of a small piece of native code for every function (a stub), the stub
	/// jumps to an address that is in a slot of the file's vtable fixups, and the CLR writes the address of the
	/// method into the slot. dnlib finds the method of an export by reading the stub, which is other code for
	/// every processor: these tests read stubs that Microsoft's compiler wrote, for x64, x86 and ARM64.
	/// Fixed in this commit: the x64 stub of that compiler (it's not the one that ilasm writes) wasn't known,
	/// so no method of a 64-bit C++/CLI file had its export.
	///
	/// The compiler needs Windows and Visual Studio with C++/CLI support, the tests are skipped without it.
	/// </summary>
	public class CppCliExportTests {
		static readonly Lazy<byte[]> image64 = new Lazy<byte[]>(() => CppCliCompiler.Compile(Fixtures.Fixtures.ReadSource("MixedModeExports.cpp")));
		static readonly Lazy<byte[]> image32 = new Lazy<byte[]>(() => CppCliCompiler.Compile(Fixtures.Fixtures.ReadSource("MixedModeExports.cpp"), is32Bit: true));

		static byte[] GetImage(bool is32Bit) {
			Assert.SkipWhen(CppCliCompiler.WhyNotAvailable is not null, CppCliCompiler.WhyNotAvailable ?? "");
			return is32Bit ? image32.Value : image64.Value;
		}

		static readonly string[] managedExports = { "ManagedAdd", "ManagedLength", "ManagedMultiply" };
		static readonly string[] nativeExports = { "NativeCallsManaged", "NativeTwice" };


		// "ordinal name" of every method that is exported
		static List<string> GetExportedMethods(ModuleDef module) =>
			module.GetTypes().SelectMany(a => a.Methods).Where(a => a.ExportInfo is not null)
				.Select(a => $"{a.ExportInfo!.Ordinal} {a.ExportInfo.Name} = {a.Name}").OrderBy(a => a, StringComparer.Ordinal).ToList();

		[Theory, InlineData(false), InlineData(true)]
		public void CompiledFileIsForTheProcessor(bool is32Bit) {
			using (var module = ModuleDefMD.Load(GetImage(is32Bit))) {
				Assert.Equal(is32Bit ? dnlib.PE.Machine.I386 : dnlib.PE.Machine.AMD64, module.Machine);
				Assert.False(module.IsILOnly);
			}
		}

		[Theory, InlineData(false), InlineData(true)]
		public void ExportedManagedFunctionsAreFoundThroughTheirStubs(bool is32Bit) {
			var image = GetImage(is32Bit);
			var functions = PEExports.GetFunctions(image);
			// All five functions are in the export table
			Assert.Equal(managedExports.Concat(nativeExports).OrderBy(a => a, StringComparer.Ordinal),
				functions.Values.Where(a => a is not null).Select(a => a!).OrderBy(a => a, StringComparer.Ordinal));

			using (var module = ModuleDefMD.Load(image)) {
				// The managed ones are methods with the name and the ordinal of the export. A native function is
				// no method with an export: its export is the address of its code.
				var expected = functions.Where(a => managedExports.Contains(a.Value))
					.Select(a => $"{a.Key} {a.Value} = {a.Value}").OrderBy(a => a, StringComparer.Ordinal).ToList();
				Assert.Equal(3, expected.Count);
				Assert.Equal(expected, GetExportedMethods(module));
				Assert.Null(module.GlobalType!.Methods.Single(a => a.Name == "ManagedHelper").ExportInfo);
			}
		}

		[Theory, InlineData(false), InlineData(true)]
		public void VTableFixupsHaveTheExportedMethods(bool is32Bit) {
			using (var module = ModuleDefMD.Load(GetImage(is32Bit))) {
				var fixups = module.VTableFixups;
				Assert.NotNull(fixups);
				Assert.NotEmpty(fixups.VTables);
				var methodsInSlots = fixups.VTables.SelectMany(a => a.Methods).Where(a => a is not null).Select(a => a!.Name.String).ToList();
				foreach (var name in managedExports)
					Assert.Contains(name, methodsInSlots);
				// A slot has the size of a pointer
				Assert.All(fixups.VTables, a => {
					Assert.Equal(is32Bit, a.Is32Bit);
					Assert.Equal(!is32Bit, a.Is64Bit);
					Assert.NotEqual((dnlib.PE.RVA)0, a.RVA);
				});
			}
		}

		static byte[] NativeWrite(ModuleDefMD module, bool optimizeImageSize) {
			var stream = new MemoryStream();
			module.NativeWrite(stream, new NativeModuleWriterOptions(module, optimizeImageSize));
			return stream.ToArray();
		}

		[Theory]
		[InlineData(false, false)]
		[InlineData(false, true)]
		[InlineData(true, false)]
		[InlineData(true, true)]
		public void ExportsAndVTableFixupsAreTheSameAfterANativeWrite(bool is32Bit, bool optimizeImageSize) {
			var image = GetImage(is32Bit);
			using (var original = ModuleDefMD.Load(image)) {
				var written = NativeWrite(original, optimizeImageSize);

				Assert.Equal(PEExports.GetFunctions(image), PEExports.GetFunctions(written));
				using (var writtenModule = ModuleDefMD.Load(written)) {
					ModuleComparer.AssertEqual(original, writtenModule, new ModuleDumpOptions { MaxStack = true });
					Assert.Equal(GetExportedMethods(original), GetExportedMethods(writtenModule));
					Assert.Equal(original.VTableFixups!.VTables.Select(a => $"{a.RVA:X} {a.Flags} {a.Methods.Count}"), writtenModule.VTableFixups!.VTables.Select(a => $"{a.RVA:X} {a.Flags} {a.Methods.Count}"));
				}
			}
		}

		// New metadata (a renamed method, a new class, another assembly name): the stubs and the slots stay where
		// they are and the slots have the new tokens of their methods
		[Theory, InlineData(false), InlineData(true)]
		public void ExportsAreTheSameAfterAChange(bool is32Bit) {
			var image = GetImage(is32Bit);
			using (var module = ModuleDefMD.Load(image)) {
				var expected = GetExportedMethods(module).Select(a => a.Replace("= ManagedAdd", "= Renamed")).ToList();
				Change(module, "Changed");

				var written = NativeWrite(module, optimizeImageSize: true);

				Assert.Equal(PEExports.GetFunctions(image), PEExports.GetFunctions(written));
				using (var writtenModule = ModuleDefMD.Load(written)) {
					Assert.Equal(expected, GetExportedMethods(writtenModule));
					Assert.NotNull(writtenModule.Find("Added.Type", isReflectionName: true));
					Assert.Contains("Renamed", writtenModule.VTableFixups!.VTables.SelectMany(a => a.Methods).Where(a => a is not null).Select(a => a!.Name.String));
				}
			}
		}

		static void Change(ModuleDefMD module, string assemblyName) {
			module.Assembly!.Name = assemblyName;
			module.GlobalType!.Methods.Single(a => a.Name == "ManagedAdd").Name = "Renamed";
			// New types before the other ones: the methods get other tokens
			module.Types.Insert(1, new TypeDefUser("Added", "Type", module.CorLibTypes.Object.TypeDefOrRef));
		}

		// The same DLL for ARM64. Its stubs are other code again, and ilasm has none for ARM64: dnlib writes the
		// stub of this compiler. The compiler is another component of Visual Studio, and the DLL can't be loaded
		// by this process, so the functions aren't called. Added for issue 165: no ARM64 file had its exports.
		static readonly Lazy<byte[]> imageArm64 = new Lazy<byte[]>(() => CppCliCompiler.CompileArm64(Fixtures.Fixtures.ReadSource("MixedModeExports.cpp")));

		static byte[] GetArm64Image() {
			Assert.SkipWhen(CppCliCompiler.WhyArm64NotAvailable is not null, CppCliCompiler.WhyArm64NotAvailable ?? "");
			return imageArm64.Value;
		}

		[Fact]
		public void Arm64ExportedManagedFunctionsAreFoundThroughTheirStubs() {
			var image = GetArm64Image();
			var functions = PEExports.GetFunctions(image);

			using (var module = ModuleDefMD.Load(image)) {
				Assert.Equal(dnlib.PE.Machine.ARM64, module.Machine);
				Assert.False(module.IsILOnly);
				var expected = functions.Where(a => managedExports.Contains(a.Value))
					.Select(a => $"{a.Key} {a.Value} = {a.Value}").OrderBy(a => a, StringComparer.Ordinal).ToList();
				Assert.Equal(3, expected.Count);
				Assert.Equal(expected, GetExportedMethods(module));
				Assert.Null(module.GlobalType!.Methods.Single(a => a.Name == "ManagedHelper").ExportInfo);
				Assert.All(module.VTableFixups!.VTables, a => Assert.True(a.Is64Bit));
			}
		}

		[Theory, InlineData(false), InlineData(true)]
		public void Arm64ExportsAreTheSameAfterANativeWrite(bool optimizeImageSize) {
			var image = GetArm64Image();
			using (var original = ModuleDefMD.Load(image)) {
				var written = NativeWrite(original, optimizeImageSize);

				Assert.Equal(PEExports.GetFunctions(image), PEExports.GetFunctions(written));
				using (var writtenModule = ModuleDefMD.Load(written)) {
					ModuleComparer.AssertEqual(original, writtenModule, new ModuleDumpOptions { MaxStack = true });
					Assert.Equal(3, GetExportedMethods(original).Count);
					Assert.Equal(GetExportedMethods(original), GetExportedMethods(writtenModule));
				}
			}
		}

		[Fact]
		public void Arm64ExportsAreTheSameAfterAChange() {
			var image = GetArm64Image();
			using (var module = ModuleDefMD.Load(image)) {
				var expected = GetExportedMethods(module).Select(a => a.Replace("= ManagedAdd", "= Renamed")).ToList();
				Change(module, "Changed");

				var written = NativeWrite(module, optimizeImageSize: true);

				Assert.Equal(PEExports.GetFunctions(image), PEExports.GetFunctions(written));
				using (var writtenModule = ModuleDefMD.Load(written))
					Assert.Equal(expected, GetExportedMethods(writtenModule));
			}
		}

#if NETFRAMEWORK
		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern IntPtr LoadLibraryW(string fileName);
		[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true, BestFitMapping = false)]
		static extern IntPtr GetProcAddress(IntPtr module, string name);

		[UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
		delegate int TwoNumbers(int a, int b);
		[UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
		delegate int OneNumber(int a);
		[UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		delegate int Text(string text);

		static T GetFunction<T>(IntPtr library, string name) where T : class {
			var address = GetProcAddress(library, name);
			Assert.True(address != IntPtr.Zero, $"The function {name} isn't exported");
			return (T)(object)Marshal.GetDelegateForFunctionPointer(address, typeof(T));
		}

		// Loads the DLL like a native program does and calls its exported functions: the native code of a stub
		// runs and jumps to the managed method
		static void AssertExportsCanBeCalled(byte[] mixedModeImage, string fileName) {
			Assert.SkipUnless(Environment.Is64BitProcess, "The file is a 64-bit file");
			// Not deleted: the DLL stays loaded
			var directory = new TempDirectory();
			var file = directory.GetPath(fileName);
			File.WriteAllBytes(file, mixedModeImage);
			var library = LoadLibraryW(file);
			Assert.True(library != IntPtr.Zero, $"The DLL wasn't loaded: error {Marshal.GetLastWin32Error()}");

			Assert.Equal(8, GetFunction<OneNumber>(library, "NativeTwice")(4));
			Assert.Equal(5, GetFunction<TwoNumbers>(library, "ManagedAdd")(2, 3));
			Assert.Equal(6, GetFunction<TwoNumbers>(library, "ManagedMultiply")(2, 3));
			Assert.Equal(4, GetFunction<Text>(library, "ManagedLength")("text"));
			// Native code of the DLL that calls its managed functions
			Assert.Equal(11, GetFunction<TwoNumbers>(library, "NativeCallsManaged")(2, 3));
		}

		[Fact]
		public void NativeCodeCanCallTheExportsOfTheCompiledFile() =>
			AssertExportsCanBeCalled(GetImage(is32Bit: false), "Compiled.dll");

		[Theory, InlineData(false), InlineData(true)]
		public void NativeCodeCanCallTheExportsAfterANativeWrite(bool optimizeImageSize) {
			using (var module = ModuleDefMD.Load(GetImage(is32Bit: false))) {
				// Two assemblies with the same name can't be loaded
				module.Assembly!.Name = "Written" + optimizeImageSize;
				AssertExportsCanBeCalled(NativeWrite(module, optimizeImageSize), $"Written{optimizeImageSize}.dll");
			}
		}

		[Fact]
		public void NativeCodeCanCallTheExportsAfterAChange() {
			using (var module = ModuleDefMD.Load(GetImage(is32Bit: false))) {
				Change(module, "ChangedAndCalled");
				AssertExportsCanBeCalled(NativeWrite(module, optimizeImageSize: true), "ChangedAndCalled.dll");
			}
		}
#endif
	}
}
