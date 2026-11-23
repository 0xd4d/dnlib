// dnlib: See LICENSE.txt for more info

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
		static byte[] WriteWithExports(Machine machine) {
			var image = CSharpCompiler.Compile(Source).Image;
			using (var module = ModuleDefMD.Load(image)) {
				var functions = module.Find("Functions", isReflectionName: true)!;
				// Not in alphabetical order: the export directory must be sorted by name
				functions.FindMethod("Second")!.ExportInfo = new MethodExportInfo("ExportedSecond");
				functions.FindMethod("First")!.ExportInfo = new MethodExportInfo("ExportedFirst", 5);

				var options = new ModuleWriterOptions(module);
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

		[Theory, InlineData(Machine.I386), InlineData(Machine.AMD64), InlineData(Machine.ARMNT)]
		public void ExportDirectoryHasTheNamesSorted(Machine machine) {
			var written = WriteWithExports(machine);

			Assert.Equal(new[] { "ExportedFirst", "ExportedSecond" }, PEExports.GetNames(written));
		}

		[Theory, InlineData(Machine.I386), InlineData(Machine.AMD64), InlineData(Machine.ARMNT)]
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
		public void ModuleWithoutExportedMethodsHasNoExportDirectory() =>
			Assert.Empty(PEExports.GetNames(CSharpCompiler.Compile(Source).Image));
	}
}
