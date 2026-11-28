// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.PE;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// A method with <see cref="MethodDef.ExportInfo"/> is exported like a function of a native DLL when the
	/// module is written: <see cref="ModuleWriter"/> creates the export directory, a vtable and a small native
	/// stub per method.
	/// </summary>
	public class ExportedMethodTests {
		const string Source = @"
public static class Functions {
	public static int First(int a) => a;
	public static int Second(int a) => a + 1;
	public static int NotExported(int a) => a + 2;
}";

		// Compiles the code above, exports First and Second, and writes the module for a CPU
		static byte[] WriteWithExports(Machine machine) =>
			WriteWithExports(machine, functions => {
				// Not in alphabetical order: the export directory must be sorted by name
				functions.FindMethod("Second")!.ExportInfo = new MethodExportInfo("ExportedSecond");
				functions.FindMethod("First")!.ExportInfo = new MethodExportInfo("ExportedFirst", 5);
			});

		// Compiles the code above and writes the module for a CPU
		// setExportInfo: gets the Functions class and sets the ExportInfo of its methods
		// logger: gets the writer's errors, the writer throws if it's null
		static byte[] WriteWithExports(Machine machine, Action<TypeDef> setExportInfo, ILogger? logger = null) {
			var image = CSharpCompiler.Compile(Source).Image;
			using (var module = ModuleDefMD.Load(image)) {
				setExportInfo(module.Find("Functions", isReflectionName: true)!);

				var options = new ModuleWriterOptions(module);
				options.Logger = logger;
				options.PEHeadersOptions.Machine = machine;
				// A module with native stubs isn't IL only
				options.Cor20HeaderOptions.Flags = ComImageFlags.Bit32Required;
				if (machine != Machine.I386) {
					options.Cor20HeaderOptions.Flags = 0;
					options.ModuleKind = ModuleKind.Dll;
				}
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		}

		[Theory, InlineData(Machine.I386), InlineData(Machine.AMD64), InlineData(Machine.ARMNT), InlineData(Machine.ARM64), InlineData(Machine.IA64)]
		public void ExportDirectoryHasTheNamesSorted(Machine machine) {
			var written = WriteWithExports(machine);

			Assert.Equal(new[] { "ExportedFirst", "ExportedSecond" }, PEExports.GetNames(written));
		}

		[Theory, InlineData(Machine.I386), InlineData(Machine.AMD64), InlineData(Machine.ARMNT), InlineData(Machine.ARM64), InlineData(Machine.IA64)]
		public void ExportInfoIsReadFromTheWrittenFile(Machine machine) {
			using (var module = ModuleDefMD.Load(WriteWithExports(machine))) {
				var functions = module.Find("Functions", isReflectionName: true)!;

				Assert.False(module.IsILOnly);
				Assert.Equal(machine, module.Machine);
				var first = functions.FindMethod("First")!.ExportInfo;
				Assert.NotNull(first);
				Assert.Equal("ExportedFirst", first.Name);
				Assert.Equal((ushort)5, first.Ordinal);
				Assert.Equal("ExportedSecond", functions.FindMethod("Second")!.ExportInfo?.Name);
				Assert.Null(functions.FindMethod("NotExported")!.ExportInfo);
				// Every exported method has a vtable slot
				Assert.Equal(2, System.Linq.Enumerable.Sum(module.VTableFixups!.VTables, a => a.Methods.Count));
			}
		}

		[Fact]
		public void NamedExportWithoutAnOrdinalGetsTheNextOrdinal() {
			var written = WriteWithExports(Machine.I386);

			Assert.Equal(new Dictionary<int, string?> { { 5, "ExportedFirst" }, { 6, "ExportedSecond" } }, PEExports.GetFunctions(written));
		}

		// Fixed in 96cbcc25: the ordinal of an export that also has a name was ignored
		[Theory, InlineData(Machine.I386), InlineData(Machine.AMD64), InlineData(Machine.ARMNT), InlineData(Machine.ARM64), InlineData(Machine.IA64)]
		public void ExportsKeepTheirOrdinals(Machine machine) {
			var written = WriteWithExports(machine, functions => {
				functions.FindMethod("First")!.ExportInfo = new MethodExportInfo("ExportedFirst", 9);
				functions.FindMethod("Second")!.ExportInfo = new MethodExportInfo(7);
				functions.FindMethod("NotExported")!.ExportInfo = new MethodExportInfo("ExportedThird", 3);
			});

			Assert.Equal(new Dictionary<int, string?> { { 3, "ExportedThird" }, { 7, null }, { 9, "ExportedFirst" } }, PEExports.GetFunctions(written));
			// Fixed in 96cbcc25: the reader removed the ordinal of an export with a name
			using (var module = ModuleDefMD.Load(written)) {
				var functions = module.Find("Functions", isReflectionName: true)!;
				Assert.Equal((ushort)9, functions.FindMethod("First")!.ExportInfo!.Ordinal);
				Assert.Equal("ExportedFirst", functions.FindMethod("First")!.ExportInfo!.Name);
				Assert.Equal((ushort)7, functions.FindMethod("Second")!.ExportInfo!.Ordinal);
				Assert.Null(functions.FindMethod("Second")!.ExportInfo!.Name);
				Assert.Equal((ushort)3, functions.FindMethod("NotExported")!.ExportInfo!.Ordinal);
			}
		}

		static void ExportTwoMethodsWithTheSameOrdinal(TypeDef functions) {
			functions.FindMethod("First")!.ExportInfo = new MethodExportInfo("ExportedFirst", 4);
			functions.FindMethod("Second")!.ExportInfo = new MethodExportInfo("ExportedSecond", 4);
		}

		// Fixed in 12a8f130: the writer threw InvalidOperationException
		[Fact]
		public void TwoExportsWithTheSameOrdinalAreAWriterError() {
			var exception = Assert.Throws<ModuleWriterException>(() => WriteWithExports(Machine.I386, ExportTwoMethodsWithTheSameOrdinal));

			Assert.Contains("ordinal 4", exception.Message);
		}

		[Fact]
		public void FirstMethodKeepsTheOrdinalIfTheErrorIsIgnored() {
			var written = WriteWithExports(Machine.I386, ExportTwoMethodsWithTheSameOrdinal, DummyLogger.NoThrowInstance);

			Assert.Equal(new Dictionary<int, string?> { { 4, "ExportedFirst" } }, PEExports.GetFunctions(written));
		}

		[Fact]
		public void ModuleWithoutExportedMethodsHasNoExportDirectory() =>
			Assert.Empty(PEExports.GetNames(CSharpCompiler.Compile(Source).Image));

		// The ARM64 stubs have the address of their slot relative to the code, so nothing must be relocated when
		// the file is loaded at another address. The file has no relocations and no section for them: a section
		// can't be empty. Added for issue 165 (ARM64 exports).
		[Fact]
		public void Arm64FileWithExportsHasNoRelocations() {
			var written = WriteWithExports(Machine.ARM64);

			using (var peImage = new PEImage(written)) {
				Assert.DoesNotContain(peImage.ImageSectionHeaders, a => a.DisplayName == ".reloc");
				Assert.Contains(peImage.ImageSectionHeaders, a => a.DisplayName == ".sdata");
				Assert.All(peImage.ImageSectionHeaders, a => Assert.NotEqual(0U, a.VirtualSize));
				var relocations = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[5];
				Assert.Equal(0U, (uint)relocations.VirtualAddress);
				Assert.Equal(0U, relocations.Size);
			}
		}

		[Theory, InlineData(Machine.I386), InlineData(Machine.AMD64), InlineData(Machine.ARMNT), InlineData(Machine.IA64)]
		public void FileWithExportsForOtherProcessorsHasRelocations(Machine machine) {
			var written = WriteWithExports(machine);

			using (var peImage = new PEImage(written)) {
				var section = Assert.Single(peImage.ImageSectionHeaders, a => a.DisplayName == ".reloc");
				var relocations = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[5];
				Assert.Equal(section.VirtualAddress, relocations.VirtualAddress);
				Assert.NotEqual(0U, relocations.Size);
			}
		}

		// A file with exported methods isn't IL only: Windows calls its entry point, which is a stub that jumps
		// to the function of mscoree.dll that the file imports. Its address is the file's only imported address.
		[Theory, InlineData(Machine.I386), InlineData(Machine.AMD64), InlineData(Machine.ARMNT), InlineData(Machine.ARM64), InlineData(Machine.IA64)]
		public void EntryPointOfAFileWithExportsJumpsToTheImportedFunction(Machine machine) {
			var written = WriteWithExports(machine);

			using (var peImage = new PEImage(written)) {
				Assert.True(CpuArch.TryGetCpuArch(machine, out var cpuArch));
				var optionalHeader = peImage.ImageNTHeaders.OptionalHeader;
				var importedAddresses = optionalHeader.DataDirectories[12];
				Assert.NotEqual(0U, (uint)importedAddresses.VirtualAddress);
				Assert.NotEqual(0U, (uint)optionalHeader.AddressOfEntryPoint);
				var reader = peImage.CreateReader();
				reader.Position = (uint)peImage.ToFileOffset(optionalHeader.AddressOfEntryPoint);

				Assert.True(cpuArch.TryGetExportedRvaFromStub(ref reader, peImage, out uint slotRva));

				Assert.Equal((uint)importedAddresses.VirtualAddress, slotRva);
			}
		}

		// An exported method needs a few bytes of native code that jump to it. dnlib has that code for x86, x64,
		// ARM, ARM64 and Itanium, not for the other processors.
		[Fact]
		public void ExportedMethodForAProcessorWithoutNativeStubIsAnError() {
			var exception = Assert.Throws<ModuleWriterException>(() => WriteWithExports(Machine.RISCV64));
			Assert.Contains("the CPU architecture isn't supported", exception.Message);

			// If the error is ignored, the file has no export directory, but it's the same module
			var written = WriteWithExports(Machine.RISCV64, functions => functions.FindMethod("First")!.ExportInfo = new MethodExportInfo("ExportedFirst"),
				DummyLogger.NoThrowInstance);
			Assert.Empty(PEExports.GetNames(written));
			using (var module = ModuleDefMD.Load(written)) {
				Assert.Equal(Machine.RISCV64, module.Machine);
				Assert.NotNull(module.Find("Functions", isReflectionName: true)!.FindMethod("First"));
			}
		}

		// The options of an exported method are the flags of its vtable: methods with other options are in
		// other vtables
		[Fact]
		public void TheOptionsOfAnExportAreTheFlagsOfItsVTable() {
			var written = WriteWithExports(Machine.AMD64, functions => {
				functions.FindMethod("First")!.ExportInfo = new MethodExportInfo("ExportedFirst", null, MethodExportInfoOptions.FromUnmanaged | MethodExportInfoOptions.FromUnmanagedRetainAppDomain);
				functions.FindMethod("Second")!.ExportInfo = new MethodExportInfo("ExportedSecond", null, MethodExportInfoOptions.FromUnmanaged | MethodExportInfoOptions.CallMostDerived);
			});

			using (var module = ModuleDefMD.Load(written)) {
				var vtables = module.VTableFixups!.VTables;
				Assert.Equal(2, vtables.Count);
				Assert.Contains(vtables, a => (a.Flags & VTableFlags.FromUnmanagedRetainAppDomain) != 0);
				Assert.Contains(vtables, a => (a.Flags & VTableFlags.CallMostDerived) != 0);
				var functions = module.Find("Functions", isReflectionName: true)!;
				Assert.Equal(MethodExportInfoOptions.FromUnmanaged | MethodExportInfoOptions.FromUnmanagedRetainAppDomain, functions.FindMethod("First")!.ExportInfo!.Options);
				Assert.Equal(MethodExportInfoOptions.FromUnmanaged | MethodExportInfoOptions.CallMostDerived, functions.FindMethod("Second")!.ExportInfo!.Options);
			}
		}
	}
}
