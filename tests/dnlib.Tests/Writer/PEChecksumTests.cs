// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.PE;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// The writers store a checksum of the file in the PE header if <see cref="ModuleWriterOptionsBase.AddCheckSum"/>
	/// is set. <see cref="PEChecksum"/> calculates the expected value.
	/// </summary>
	public class PEChecksumTests {
		const string Source = "public class C { public static int M() => 123; }";
		static readonly Lazy<byte[]> compiledImage = new Lazy<byte[]>(() => CSharpCompiler.Compile(Source).Image);

		static void AssertHasValidChecksum(byte[] image) {
			Assert.NotEqual(0U, PEChecksum.Read(image));
			Assert.Equal(PEChecksum.Calculate(image), PEChecksum.Read(image));
		}

		[Fact]
		public void CompilerDoesNotStoreAChecksum() => Assert.Equal(0U, PEChecksum.Read(compiledImage.Value));

		[Fact]
		public void ChecksumIsNotAddedByDefault() {
			using (var module = ModuleDefMD.Load(compiledImage.Value)) {
				var stream = new MemoryStream();
				module.Write(stream);

				Assert.Equal(0U, PEChecksum.Read(stream.ToArray()));
			}
		}

		[Fact]
		public void ModuleWriterAddsTheChecksum() {
			using (var module = ModuleDefMD.Load(compiledImage.Value)) {
				var stream = new MemoryStream();
				module.Write(stream, new ModuleWriterOptions(module) { AddCheckSum = true });

				AssertHasValidChecksum(stream.ToArray());
			}
		}

		[Fact]
		public void NativeModuleWriterAddsTheChecksum() {
			using (var module = ModuleDefMD.Load(compiledImage.Value)) {
				var stream = new MemoryStream();
				module.NativeWrite(stream, new NativeModuleWriterOptions(module, optimizeImageSize: false) { AddCheckSum = true });

				AssertHasValidChecksum(stream.ToArray());
			}
		}

		// A file that has a checksum gets a new one when it's written again
		[Fact]
		public void FileWithAChecksumGetsANewOneByDefault() {
			byte[] imageWithChecksum;
			using (var module = ModuleDefMD.Load(compiledImage.Value)) {
				var stream = new MemoryStream();
				module.Write(stream, new ModuleWriterOptions(module) { AddCheckSum = true });
				imageWithChecksum = stream.ToArray();
			}

			using (var module = ModuleDefMD.Load(imageWithChecksum)) {
				module.Find("C", isReflectionName: true)!.Name = "Renamed";
				var stream = new MemoryStream();
				module.Write(stream);

				AssertHasValidChecksum(stream.ToArray());
				Assert.NotEqual(PEChecksum.Read(imageWithChecksum), PEChecksum.Read(stream.ToArray()));
			}
		}

		// Fixed in 284d2a19: the bytes before the file were part of the checksum, and the 4 bytes that were
		// skipped (the stored checksum) were at the wrong place
		[Theory, InlineData(false), InlineData(true)]
		public void ChecksumIsValidIfTheFileIsNotAtTheStartOfTheStream(bool nativeWrite) {
			using (var module = ModuleDefMD.Load(compiledImage.Value)) {
				var stream = new MemoryStream();
				var dataBeforeFile = Enumerable.Range(1, 77).Select(a => (byte)a).ToArray();
				stream.Write(dataBeforeFile, 0, dataBeforeFile.Length);

				if (nativeWrite)
					module.NativeWrite(stream, new NativeModuleWriterOptions(module, optimizeImageSize: false) { AddCheckSum = true });
				else
					module.Write(stream, new ModuleWriterOptions(module) { AddCheckSum = true });

				var all = stream.ToArray();
				Assert.Equal(dataBeforeFile, all.Take(dataBeforeFile.Length));
				AssertHasValidChecksum(all.Skip(dataBeforeFile.Length).ToArray());
			}
		}

		// NativeModuleWriter keeps the data after the last section. It's followed by zeros up to the next
		// multiple of the file alignment (512 bytes).
		[Theory, InlineData(1), InlineData(2), InlineData(7)]
		public void ChecksumIsValidIfTheFileHasExtraDataAtTheEnd(int extraLength) {
			const int fileAlignment = 512;
			var extraData = Enumerable.Range(0x80, extraLength).Select(a => (byte)a).ToArray();
			var image = compiledImage.Value.Concat(extraData).ToArray();
			using (var module = ModuleDefMD.Load(image)) {
				var stream = new MemoryStream();
				module.NativeWrite(stream, new NativeModuleWriterOptions(module, optimizeImageSize: false) { AddCheckSum = true, KeepExtraPEData = true });

				var written = stream.ToArray();
				Assert.Equal(extraData, written.Skip(written.Length - fileAlignment).Take(extraLength));
				AssertHasValidChecksum(written);
			}
		}

		// Fixed in 4635e677: the file's length is odd if its file alignment is 1 and it has extra data at the
		// end, and NativeModuleWriter threw 'Invalid PE length'. This calls the method that calculates the
		// checksum: the last byte is added like a 16-bit number.
		[Theory, InlineData(1), InlineData(3), InlineData(4)]
		public void ChecksumOfAFileWithExtraBytes(int extraLength) {
			var image = compiledImage.Value.Concat(Enumerable.Range(0x80, extraLength).Select(a => (byte)a)).ToArray();

			var checksum = new MemoryStream(image).CalculatePECheckSum(image.Length, PEChecksum.GetChecksumOffset(image));

			Assert.Equal(PEChecksum.Calculate(image), checksum);
		}
	}
}
