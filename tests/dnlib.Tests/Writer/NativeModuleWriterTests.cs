// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// <see cref="NativeModuleWriter"/> writes modules that aren't "IL only" (eg. C++/CLI assemblies). It keeps
	/// the original PE file with its native code and data, and only replaces the .NET metadata and method bodies.
	/// The test input is Fixtures/Sources/Exports.il.
	/// </summary>
	public class NativeModuleWriterTests {
		static byte[] Image => Fixtures.Fixtures.Exports.Image;

		static byte[] NativeWrite(ModuleDefMD module, bool optimizeImageSize) {
			var stream = new MemoryStream();
			module.NativeWrite(stream, new NativeModuleWriterOptions(module, optimizeImageSize));
			return stream.ToArray();
		}

		[Fact]
		public void TheFixtureIsNotILOnly() {
			using (var module = ModuleDefMD.Load(Image)) {
				Assert.False(module.IsILOnly);
				var vtable = Assert.Single(module.VTableFixups!.VTables);
				Assert.True(vtable.Is32Bit);
				var add = module.Find("Exports.Functions", isReflectionName: true)!.FindMethod("Add")!;
				Assert.Same(add, Assert.Single(vtable.Methods));
				Assert.Equal("Add", add.ExportInfo!.Name);
				var data = module.Find("Exports.Functions", isReflectionName: true)!.FindField("Data")!;
				Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 1, 2, 3, 4 }, data.InitialValue);
			}
			Assert.Equal(new[] { "Add" }, PEExports.GetNames(Image));
		}

		[Theory, InlineData(false), InlineData(true)]
		public void ModuleIsTheSameAfterANativeWrite(bool optimizeImageSize) {
			using (var original = ModuleDefMD.Load(Image)) {
				var writtenImage = NativeWrite(original, optimizeImageSize);

				using (var written = ModuleDefMD.Load(writtenImage)) {
					ModuleComparer.AssertEqual(original, written, new ModuleDumpOptions { MaxStack = true });
					Assert.False(written.IsILOnly);
					Assert.Equal(original.VTableFixups!.VTables.Count, written.VTableFixups!.VTables.Count);
					Assert.Equal("Add", Assert.Single(written.VTableFixups.VTables[0].Methods)!.Name);
				}
				// The export directory is native data that's kept as it is
				Assert.Equal(new[] { "Add" }, PEExports.GetNames(writtenImage));
			}
		}

		[Fact]
		public void WrittenFileSurvivesAnotherNativeWrite() {
			byte[] writtenOnce, writtenTwice;
			using (var original = ModuleDefMD.Load(Image))
				writtenOnce = NativeWrite(original, optimizeImageSize: true);
			using (var module = ModuleDefMD.Load(writtenOnce))
				writtenTwice = NativeWrite(module, optimizeImageSize: true);

			using (var original = ModuleDefMD.Load(Image))
			using (var written = ModuleDefMD.Load(writtenTwice))
				ModuleComparer.AssertEqual(original, written, new ModuleDumpOptions { MaxStack = true });
		}

		[Fact]
		public void ChangedMethodBodyIsWritten() {
			using (var module = ModuleDefMD.Load(Image)) {
				var notExported = module.Find("Exports.Functions", isReflectionName: true)!.FindMethod("NotExported")!;
				// Make the method bigger, so it doesn't fit where it was
				for (int i = 0; i < 100; i++)
					notExported.Body!.Instructions.Insert(0, dnlib.DotNet.Emit.OpCodes.Nop.ToInstruction());

				using (var written = ModuleDefMD.Load(NativeWrite(module, optimizeImageSize: true))) {
					var writtenMethod = written.Find("Exports.Functions", isReflectionName: true)!.FindMethod("NotExported")!;
					Assert.Equal(102, writtenMethod.Body!.Instructions.Count);
					// The other method wasn't changed
					Assert.Equal(4, written.Find("Exports.Functions", isReflectionName: true)!.FindMethod("Add")!.Body!.Instructions.Count);
				}
			}
		}

		[Fact]
		public void NativeWriteOfAnILOnlyModuleWorksToo() {
			var image = Fixtures.Fixtures.Features.Image;
			using (var original = ModuleDefMD.Load(image)) {
				var writtenImage = NativeWrite(original, optimizeImageSize: true);

				using (var written = ModuleDefMD.Load(writtenImage))
					ModuleComparer.AssertEqual(original, written, new ModuleDumpOptions { MaxStack = true });
				Assert.Equal(0, AssemblyRunner.RunEntryPoint(writtenImage));
			}
		}

		// The offsets of fields in the PE optional header (PE32, the fixture is a 32-bit file)
		const int fileAlignmentOffset = 36;
		const int numberOfRvaAndSizesOffset = 92;

		static int GetOptionalHeaderOffset(byte[] image) {
			using (var peReader = new PEReader(ImmutableArray.Create(image))) {
				Assert.Equal(PEMagic.PE32, peReader.PEHeaders.PEHeader!.Magic);
				return peReader.PEHeaders.PEHeaderStartOffset;
			}
		}

		static uint ReadNumberOfRvaAndSizes(byte[] image) => LittleEndian.ToUInt32(image, GetOptionalHeaderOffset(image) + numberOfRvaAndSizesOffset);

		// The fixture with 15 instead of 16 data directories in its PE header. The last one isn't used by any
		// file, and the .NET directory is number 15.
		static byte[] CreateImageWith15DataDirectories() {
			var image = (byte[])Image.Clone();
			int offset = GetOptionalHeaderOffset(image) + numberOfRvaAndSizesOffset;
			Assert.Equal(16U, LittleEndian.ToUInt32(image, offset));
			image[offset] = 15;
			return image;
		}

		// Fixed in a4fd4ae4: the writer keeps the original headers but always wrote 16
		[Fact]
		public void NumberOfDataDirectoriesIsKept() {
			var image = CreateImageWith15DataDirectories();
			using (var module = ModuleDefMD.Load(image)) {
				var written = NativeWrite(module, optimizeImageSize: true);

				Assert.Equal(15U, ReadNumberOfRvaAndSizes(written));
				using (var writtenModule = ModuleDefMD.Load(written))
					ModuleComparer.AssertEqual(module, writtenModule, new ModuleDumpOptions { MaxStack = true });
			}
		}

		[Theory]
		// The .NET directory must be one of them
		[InlineData(14U)]
		// There's only room for 16
		[InlineData(17U)]
		public void InvalidNumberOfDataDirectoriesIsAnError(uint count) {
			using (var module = ModuleDefMD.Load(Image)) {
				var options = new NativeModuleWriterOptions(module, optimizeImageSize: true);
				options.PEHeadersOptions.NumberOfRvaAndSizes = count;

				Assert.Throws<ModuleWriterException>(() => module.NativeWrite(new MemoryStream(), options));
			}
		}

		// Fixed in 1bdc408a: the writer threw OverflowException
		[Fact]
		public void FileAlignmentThatIsNotAPowerOfTwoIsAnError() {
			var image = (byte[])Image.Clone();
			int offset = GetOptionalHeaderOffset(image) + fileAlignmentOffset;
			Assert.Equal(0x200U, LittleEndian.ToUInt32(image, offset));
			image[offset] = 3;

			using (var module = ModuleDefMD.Load(image))
				Assert.Throws<ModuleWriterException>(() => NativeWrite(module, optimizeImageSize: true));
		}
	}
}
