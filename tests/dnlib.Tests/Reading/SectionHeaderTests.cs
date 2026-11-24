// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A section header of a PE file says where the data of the section is in the file (PointerToRawData,
	/// SizeOfRawData) and where it is in memory (VirtualAddress, VirtualSize). The tests change the header of
	/// the last section of a compiled file, which the tests expect to be the relocation section: nothing in
	/// the metadata needs it.
	/// </summary>
	public class SectionHeaderTests {
		static byte[] ValidImage => Fixtures.Fixtures.Features.Image;

		const int sectionHeaderSize = 40;
		const int pointerToRawDataOffset = 20;

		// File offset of the last section header, found without dnlib
		static int GetLastSectionHeaderOffset(byte[] image) {
			using (var peReader = new PEReader(ImmutableArray.Create(image))) {
				var headers = peReader.PEHeaders;
				Assert.Equal(".reloc", headers.SectionHeaders.Last().Name);
				int firstSectionHeader = headers.PEHeaderStartOffset + headers.CoffHeader.SizeOfOptionalHeader;
				return firstSectionHeader + (headers.SectionHeaders.Length - 1) * sectionHeaderSize;
			}
		}

		static byte[] CreateImageWhoseLastSectionStartsAtFileOffset0() {
			var image = (byte[])ValidImage.Clone();
			Array.Clear(image, GetLastSectionHeaderOffset(image) + pointerToRawDataOffset, 4);
			return image;
		}

		// Fixed in f665b145: such a section was read as a section without data in the file. Mono reads its
		// data at the start of the file and loads the file, the CLR doesn't load it.
		[Fact]
		public void SectionWithPointerToRawData0HasTheDataAtTheStartOfTheFile() {
			var image = CreateImageWhoseLastSectionStartsAtFileOffset0();
			using (var peImage = new PEImage(image)) {
				var section = peImage.ImageSectionHeaders.Last();
				Assert.Equal(0U, section.PointerToRawData);
				Assert.NotEqual(0U, section.SizeOfRawData);

				Assert.Equal((FileOffset)0, peImage.ToFileOffset(section.VirtualAddress));
				Assert.Equal((FileOffset)3, peImage.ToFileOffset(section.VirtualAddress + 3));
				Assert.Equal(section.VirtualAddress + 3, peImage.ToRVA((FileOffset)3));
				// The file starts with "MZ"
				var reader = peImage.CreateReader(section.VirtualAddress, 2);
				Assert.Equal(new[] { (byte)'M', (byte)'Z' }, reader.ReadBytes(2));
			}
		}

		[Fact]
		public void ModuleWithSuchASectionIsTheSameModuleAndCanBeWritten() {
			var image = CreateImageWhoseLastSectionStartsAtFileOffset0();
			using (var original = ModuleDefMD.Load(ValidImage))
			using (var module = ModuleDefMD.Load(image)) {
				ModuleComparer.AssertEqual(original, module);

				var stream = new MemoryStream();
				module.NativeWrite(stream, new NativeModuleWriterOptions(module, optimizeImageSize: false));

				// The section has data in the written file: a copy of the bytes it had, the start of the file
				using (var written = new PEImage(stream.ToArray())) {
					// The writer adds its sections after the original ones
					var section = written.ImageSectionHeaders[module.Metadata.PEImage.ImageSectionHeaders.Count - 1];
					Assert.NotEqual(0U, section.PointerToRawData);
					var reader = written.CreateReader(section.VirtualAddress, 2);
					Assert.Equal(new[] { (byte)'M', (byte)'Z' }, reader.ReadBytes(2));
				}
				using (var writtenModule = ModuleDefMD.Load(stream.ToArray()))
					ModuleComparer.AssertEqual(original, writtenModule);
			}
		}
	}
}
