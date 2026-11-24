// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// The writer's options for the fields of the metadata header (<see cref="MetadataHeaderOptions"/>) and of
	/// the header of the tables (<see cref="TablesHeapOptions"/>). Every value is written as it is, also the
	/// ones that the CLR doesn't accept. The CLR runs a program whose headers have unusual values that it
	/// accepts.
	/// </summary>
	public class MetadataHeaderOptionsTests {
		const string Source = @"
public static class Program {
	static string Text() => ""text"";
	public static int Main() => Text().Length == 4 ? 0 : 1;
}
";
		static readonly byte[] compiledImage = CSharpCompiler.Compile(Source, new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication }).Image;

		static byte[] Write(Action<ModuleWriterOptions> changeOptions) {
			using (var module = ModuleDefMD.Load(compiledImage)) {
				var options = new ModuleWriterOptions(module);
				changeOptions(options);
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		}

		// Mono has other rules for the headers
		static void AssertRunsOnTheClr(byte[] image) {
			if (Type.GetType("Mono.Runtime") is null)
				Assert.Equal(0, AssemblyRunner.RunEntryPoint(image));
		}

		static bool IsDotNet => typeof(object).Assembly.GetName().Name == "System.Private.CoreLib";

		[Theory, InlineData(false), InlineData(true)]
		public void ExtraDataOfTheTablesHeaderIsWritten(bool uncompressed) {
			var written = Write(options => {
				options.MetadataOptions.TablesHeapOptions.ExtraData = 0x12345678;
				options.MetadataOptions.TablesHeapOptions.UseENC = uncompressed;
			});

			using (var module = ModuleDefMD.Load(written)) {
				Assert.Equal(!uncompressed, module.Metadata.IsCompressed);
				Assert.True(module.TablesStream.HasExtraData);
				Assert.Equal(MDStreamFlags.ExtraData, module.TablesStream.Flags & MDStreamFlags.ExtraData);
				Assert.Equal(0x12345678U, module.TablesStream.ExtraData);
				using (var original = ModuleDefMD.Load(compiledImage))
					dnlib.TestUtils.Dumping.ModuleComparer.AssertEqual(original, module);
			}
			AssertRunsOnTheClr(written);
			// The data has no meaning: it's only written if the options say so, not because the file had it
			using (var module = ModuleDefMD.Load(written)) {
				var stream = new MemoryStream();
				module.Write(stream);
				using (var writtenAgain = ModuleDefMD.Load(stream.ToArray())) {
					Assert.False(writtenAgain.TablesStream.HasExtraData);
					Assert.Equal(0U, writtenAgain.TablesStream.ExtraData);
				}
			}
		}

		[Fact]
		public void ReservedFieldsAreWritten() {
			var written = Write(options => {
				options.MetadataOptions.MetadataHeaderOptions.Reserved1 = 0x11223344;
				options.MetadataOptions.MetadataHeaderOptions.Reserved2 = 0x55;
				options.MetadataOptions.TablesHeapOptions.Reserved1 = 0x66778899;
				options.MetadataOptions.TablesHeapOptions.Log2Rid = 17;
			});

			using (var module = ModuleDefMD.Load(written)) {
				Assert.Equal(0x11223344U, module.Metadata.MetadataHeader.Reserved1);
				Assert.Equal(0x55, module.Metadata.MetadataHeader.Reserved2);
				Assert.Equal(0x66778899U, module.TablesStream.Reserved1);
				Assert.Equal(17, module.TablesStream.Log2Rid);
			}
			AssertRunsOnTheClr(written);
		}

		// Version 1.0 is the format of .NET Framework 1.x. The CLR reads it if the file has no generics.
		[Theory]
		[InlineData(2, 0, true)]
		[InlineData(1, 0, true)]
		[InlineData(1, 1, false)]
		[InlineData(3, 7, false)]
		public void VersionOfTheTablesIsWritten(byte major, byte minor, bool clrLoadsIt) {
			var written = Write(options => {
				options.MetadataOptions.TablesHeapOptions.MajorVersion = major;
				options.MetadataOptions.TablesHeapOptions.MinorVersion = minor;
			});

			using (var module = ModuleDefMD.Load(written)) {
				Assert.Equal((ushort)((major << 8) | minor), module.TablesStream.Version);
				Assert.Equal((ushort)((major << 8) | minor), module.TablesHeaderVersion);
				Assert.NotNull(module.Find("Program", isReflectionName: true));
			}
			if (clrLoadsIt)
				AssertRunsOnTheClr(written);
			else if (IsDotNet)
				Assert.Throws<BadImageFormatException>(() => AssemblyRunner.RunEntryPoint(written));
		}

		[Fact]
		public void VersionAndSignatureOfTheMetadataHeaderAreWritten() {
			var written = Write(options => {
				options.MetadataOptions.MetadataHeaderOptions.MajorVersion = 3;
				options.MetadataOptions.MetadataHeaderOptions.MinorVersion = 4;
			});
			var block = new MetadataBlock(written);
			Assert.Equal((3, 4), (block.MajorVersion, block.MinorVersion));
			using (var module = ModuleDefMD.Load(written)) {
				Assert.Equal((3, 4), (module.Metadata.MetadataHeader.MajorVersion, module.Metadata.MetadataHeader.MinorVersion));
				Assert.NotNull(module.Find("Program", isReflectionName: true));
			}
			if (IsDotNet)
				Assert.Throws<BadImageFormatException>(() => AssemblyRunner.RunEntryPoint(written));

			// A file with another signature isn't a .NET file anymore
			var withOtherSignature = Write(options => options.MetadataOptions.MetadataHeaderOptions.Signature = 0x11223344);
			Assert.Equal(0x11223344U, new MetadataBlock(withOtherSignature).Signature);
			Assert.Throws<BadImageFormatException>(() => ModuleDefMD.Load(withOtherSignature));
		}

		[Fact]
		public void StorageFlagsAreWritten() {
			var written = Write(options => options.MetadataOptions.MetadataHeaderOptions.StorageFlags = (StorageFlags)0x82);

			Assert.Equal(0x82, new MetadataBlock(written).StorageFlags);
			using (var module = ModuleDefMD.Load(written)) {
				Assert.Equal((StorageFlags)0x82, module.Metadata.MetadataHeader.Flags);
				Assert.NotNull(module.Find("Program", isReflectionName: true));
			}
			// The CLR only loads a file whose flags are 0
			if (IsDotNet)
				Assert.Throws<BadImageFormatException>(() => AssemblyRunner.RunEntryPoint(written));
		}

		// The flag says that rows can be marked as deleted (by a name that starts with "_Deleted"). It's only
		// used in uncompressed metadata.
		[Theory, InlineData(false), InlineData(true)]
		public void HasDeletedRowsFlagIsWritten(bool uncompressed) {
			var written = Write(options => {
				options.MetadataOptions.TablesHeapOptions.HasDeletedRows = true;
				options.MetadataOptions.TablesHeapOptions.UseENC = uncompressed;
			});

			using (var module = ModuleDefMD.Load(written)) {
				Assert.True(module.TablesStream.HasDelete);
				Assert.NotNull(module.Find("Program", isReflectionName: true));
			}
			AssertRunsOnTheClr(written);
		}
	}
}
