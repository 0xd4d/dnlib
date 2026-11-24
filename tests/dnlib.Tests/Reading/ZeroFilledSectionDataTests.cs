// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.PE;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A section of a PE file can be bigger in memory (VirtualSize) than its data in the file (SizeOfRawData):
	/// Windows fills the rest with zeros. C++ compilers store zero-initialized data like that, and the initial
	/// value of a field can be there. The test input has a field with 8 bytes of data in the file
	/// (Fixtures/Sources/Exports.il): the tests make the section's data in the file end in or before those bytes.
	/// </summary>
	public class ZeroFilledSectionDataTests {
		static byte[] ValidImage => Fixtures.Fixtures.Exports.Image;
		static readonly byte[] fieldData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 1, 2, 3, 4 };

		const int sectionHeaderSize = 40;
		const int virtualSizeOffset = 8;
		const int sizeOfRawDataOffset = 16;

		static FieldDef GetField(ModuleDefMD module) => module.Find("Exports.Functions", isReflectionName: true)!.FindField("Data")!;

		// The RVA of the field's data, and how far it is from the start of its section
		readonly uint fieldRva;
		readonly uint fieldOffsetInSection;
		// File offset of the header of the field's section, found without dnlib
		readonly int sectionHeaderOffset;

		public ZeroFilledSectionDataTests() {
			using (var module = ModuleDefMD.Load(ValidImage))
				fieldRva = (uint)GetField(module).RVA;
			using (var peReader = new PEReader(ImmutableArray.Create(ValidImage))) {
				var headers = peReader.PEHeaders;
				int sectionIndex = headers.GetContainingSectionIndex((int)fieldRva);
				fieldOffsetInSection = fieldRva - (uint)headers.SectionHeaders[sectionIndex].VirtualAddress;
				sectionHeaderOffset = headers.PEHeaderStartOffset + headers.CoffHeader.SizeOfOptionalHeader + sectionIndex * sectionHeaderSize;
			}
		}

		// The image with another size of the data of the field's section in the file (and in memory)
		byte[] CreateImage(uint sizeOfRawData, uint? virtualSize = null) {
			var image = (byte[])ValidImage.Clone();
			BitConverter.GetBytes(sizeOfRawData).CopyTo(image, sectionHeaderOffset + sizeOfRawDataOffset);
			if (virtualSize is not null)
				BitConverter.GetBytes(virtualSize.Value).CopyTo(image, sectionHeaderOffset + virtualSizeOffset);
			return image;
		}

		[Fact]
		public void DataInTheFileIsRead() {
			using (var module = ModuleDefMD.Load(ValidImage)) {
				Assert.Equal(fieldData, GetField(module).InitialValue);
				Assert.Equal(fieldData, module.ReadDataAt((RVA)fieldRva, 8));
				Assert.Equal(new byte[] { 0xBE, 0xEF, 1 }, module.ReadDataAt((RVA)(fieldRva + 2), 3));
				Assert.Null(module.ReadDataAt((RVA)fieldRva, -1));
			}
		}

		[Fact]
		public void DataThatIsOnlyPartlyInTheFileEndsWithZeros() {
			var image = CreateImage(sizeOfRawData: fieldOffsetInSection + 4);

			using (var module = ModuleDefMD.Load(image)) {
				var expected = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0, 0, 0, 0 };
				Assert.Equal(expected, GetField(module).InitialValue);
				Assert.Equal(expected, module.ReadDataAt((RVA)fieldRva, 8));
			}
		}

		[Fact]
		public void DataThatIsNotInTheFileIsAllZeros() {
			var image = CreateImage(sizeOfRawData: fieldOffsetInSection);

			using (var module = ModuleDefMD.Load(image)) {
				Assert.Equal(new byte[8], GetField(module).InitialValue);
				Assert.Equal(new byte[8], module.ReadDataAt((RVA)fieldRva, 8));
				Assert.Equal(new byte[3], module.ReadDataAt((RVA)(fieldRva + 4), 3));
			}
		}

		// The zeros aren't in the file, so a small file can have a huge section. dnlib doesn't allocate more
		// zeros than the size of the file: real zero-initialized data is small.
		[Fact]
		public void ZeroFilledDataThatIsBiggerThanTheFileIsNotRead() {
			var image = CreateImage(sizeOfRawData: fieldOffsetInSection, virtualSize: 0x10000000);

			using (var module = ModuleDefMD.Load(image)) {
				Assert.Equal(new byte[8], module.ReadDataAt((RVA)fieldRva, 8));
				Assert.Equal(new byte[image.Length], module.ReadDataAt((RVA)fieldRva, image.Length));
				Assert.Null(module.ReadDataAt((RVA)fieldRva, image.Length + 1));
				Assert.Null(module.ReadDataAt((RVA)fieldRva, 0x08000000));
			}
		}
	}
}
