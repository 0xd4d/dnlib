// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using dnlib.IO;
using dnlib.PE;
using dnlib.TestUtils;
using dnlib.W32Resources;
using Xunit;

namespace dnlib.Tests.Loading {
	/// <summary>
	/// <see cref="PEImage"/> reads the headers of a PE file (an EXE or DLL) and converts between addresses in
	/// memory (RVAs) and offsets in the file. It can be created from a file, from the bytes of a file, or from
	/// memory, where the file is as it is on disk or as the operating system has loaded it (every section at
	/// its RVA). The tests check that all of these give the same image, with System.Reflection.Metadata's
	/// <see cref="PEReader"/> as the second opinion.
	/// </summary>
	public sealed class PEImageTests : IDisposable {
		static byte[] Image => Fixtures.Fixtures.Features.Image;

		readonly TempDirectory directory = new TempDirectory();
		readonly List<IntPtr> allocatedMemory = new List<IntPtr>();

		public void Dispose() {
			foreach (var memory in allocatedMemory)
				Marshal.FreeHGlobal(memory);
			directory.Dispose();
		}

		IntPtr CopyToMemory(byte[] data) {
			var memory = Marshal.AllocHGlobal(data.Length);
			allocatedMemory.Add(memory);
			Marshal.Copy(data, 0, memory, data.Length);
			return memory;
		}

		// What the OS does when it loads a PE file: the headers stay at the start, and every section is copied
		// to its address (RVA)
		static byte[] CreateMemoryLayout(byte[] fileImage) {
			using (var peReader = new PEReader(ImmutableArray.Create(fileImage))) {
				var headers = peReader.PEHeaders;
				var memoryImage = new byte[headers.PEHeader!.SizeOfImage];
				Array.Copy(fileImage, memoryImage, headers.PEHeader.SizeOfHeaders);
				foreach (var section in headers.SectionHeaders)
					Array.Copy(fileImage, section.PointerToRawData, memoryImage, section.VirtualAddress, Math.Min(section.SizeOfRawData, section.VirtualSize));
				return memoryImage;
			}
		}

		// The values of the headers that every way of creating the image must agree on
		static string DescribeHeaders(IPEImage image) {
			var optionalHeader = image.ImageNTHeaders.OptionalHeader;
			var sections = image.ImageSectionHeaders.Select(a => $"{a.DisplayName} {(uint)a.VirtualAddress:X} {a.VirtualSize:X} {a.PointerToRawData:X} {a.SizeOfRawData:X}");
			return $"{image.ImageNTHeaders.FileHeader.Machine} entry {(uint)optionalHeader.AddressOfEntryPoint:X} image size {optionalHeader.SizeOfImage:X} " +
				$"directories {optionalHeader.DataDirectories.Length} | " + string.Join(" | ", sections);
		}

		static string DescribeHeadersWithSrm() {
			using (var peReader = new PEReader(ImmutableArray.Create(Image))) {
				var headers = peReader.PEHeaders;
				var sections = headers.SectionHeaders.Select(a => $"{a.Name} {a.VirtualAddress:X} {a.VirtualSize:X} {a.PointerToRawData:X} {a.SizeOfRawData:X}");
				return $"{(dnlib.PE.Machine)(ushort)headers.CoffHeader.Machine} entry {headers.PEHeader!.AddressOfEntryPoint:X} image size {headers.PEHeader.SizeOfImage:X} " +
					$"directories 16 | " + string.Join(" | ", sections);
			}
		}

		[Fact]
		public void ImageFromBytesHasTheHeadersOfTheFile() {
			var expected = DescribeHeadersWithSrm();

			foreach (var image in new[] {
				new PEImage(Image),
				new PEImage(Image, verify: true),
				new PEImage(Image, "name.dll"),
				new PEImage(Image, "name.dll", verify: false),
				new PEImage(Image, ImageLayout.File, verify: true),
				new PEImage(Image, "name.dll", ImageLayout.File, verify: true),
			}) {
				using (image) {
					Assert.Equal(expected, DescribeHeaders(image));
					Assert.True(image.IsFileImageLayout);
					Assert.False(image.MayHaveInvalidAddresses);
					// The DOS header has the offset of the PE header at 0x3C
					Assert.Equal(BitConverter.ToUInt32(Image, 0x3C), image.ImageDosHeader.NTHeadersOffset);
				}
			}
			using (var named = new PEImage(Image, "name.dll"))
				Assert.Equal("name.dll", named.Filename);
			using (var unnamed = new PEImage(Image))
				Assert.Null(unnamed.Filename);
		}

		[Fact]
		public void ImageFromAFileHasTheHeadersOfTheFile() {
			var file = directory.GetPath("Image.dll");
			File.WriteAllBytes(file, Image);

			foreach (var image in new[] { new PEImage(file), new PEImage(file, verify: true) }) {
				using (image) {
					Assert.Equal(DescribeHeadersWithSrm(), DescribeHeaders(image));
					Assert.Equal(file, image.Filename);
					Assert.True(image.IsFileImageLayout);
					Assert.Equal((uint)Image.Length, image.CreateReader().Length);
				}
			}
		}

		[Fact]
		public void ImageFromMemoryHasTheHeadersOfTheFile() {
			var expected = DescribeHeadersWithSrm();
			var memoryLayout = CreateMemoryLayout(Image);
			var loaded = CopyToMemory(memoryLayout);
			var fileInMemory = CopyToMemory(Image);

			// A loaded image: the layout is the memory layout unless the caller says something else
			foreach (var image in new[] {
				new PEImage(loaded),
				new PEImage(loaded, verify: true),
				new PEImage(loaded, ImageLayout.Memory, verify: true),
				new PEImage(loaded, (uint)memoryLayout.Length),
				new PEImage(loaded, (uint)memoryLayout.Length, verify: true),
				new PEImage(loaded, (uint)memoryLayout.Length, ImageLayout.Memory, verify: true),
			}) {
				using (image) {
					Assert.Equal(expected, DescribeHeaders(image));
					Assert.False(image.IsFileImageLayout);
					Assert.Null(image.Filename);
					// Without a length, the image has the size that its headers say
					Assert.Equal((uint)memoryLayout.Length, image.CreateReader().Length);
				}
			}
			foreach (var image in new[] {
				new PEImage(fileInMemory, ImageLayout.File, verify: true),
				new PEImage(fileInMemory, (uint)Image.Length, ImageLayout.File, verify: true),
			}) {
				using (image) {
					Assert.Equal(expected, DescribeHeaders(image));
					Assert.True(image.IsFileImageLayout);
				}
			}
		}

		// An RVA is an address in a loaded file, a file offset is a position in the file on disk
		[Fact]
		public void AddressesAndFileOffsetsAreConvertedInBothDirections() {
			using (var image = new PEImage(Image))
			using (var peReader = new PEReader(ImmutableArray.Create(Image))) {
				foreach (var section in peReader.PEHeaders.SectionHeaders) {
					// The first and the last byte of the section's data in the file
					foreach (int position in new[] { 0, Math.Min(section.SizeOfRawData, section.VirtualSize) - 1 }) {
						var rva = (RVA)(section.VirtualAddress + position);
						var offset = (FileOffset)(section.PointerToRawData + position);

						Assert.Equal(offset, image.ToFileOffset(rva));
						Assert.Equal(rva, image.ToRVA(offset));
						Assert.Equal(Image[(int)offset], image.CreateReader(rva).ReadByte());
						Assert.Equal(Image[(int)offset], image.CreateReader(offset).ReadByte());
					}
				}
				// The headers are at the same place in the file and in memory
				Assert.Equal((FileOffset)0x40, image.ToFileOffset((RVA)0x40));
				Assert.Equal((RVA)0x40, image.ToRVA((FileOffset)0x40));
				// An address after the image, and an offset after the last section, are nowhere
				Assert.Equal((FileOffset)0, image.ToFileOffset((RVA)0x7FFFFFFF));
				Assert.Equal((RVA)0, image.ToRVA((FileOffset)(Image.Length + 0x1000)));
			}
		}

		// In a loaded image, everything is at its RVA
		[Fact]
		public void AddressesAreTheOffsetsInALoadedImage() {
			using (var image = new PEImage(CreateMemoryLayout(Image), ImageLayout.Memory, verify: true)) {
				var section = image.ImageSectionHeaders[0];

				Assert.Equal((FileOffset)(uint)section.VirtualAddress, image.ToFileOffset(section.VirtualAddress));
				Assert.Equal(section.VirtualAddress, image.ToRVA((FileOffset)(uint)section.VirtualAddress));
			}
		}

		[Fact]
		public void ReadersCanBeLimitedToALength() {
			using (var image = new PEImage(Image)) {
				var section = image.ImageSectionHeaders[0];

				var byRva = image.CreateReader(section.VirtualAddress, 16);
				var byOffset = image.CreateReader((FileOffset)section.PointerToRawData, 16);

				Assert.Equal(16U, byRva.Length);
				Assert.Equal(byRva.ToArray(), byOffset.ToArray());
				Assert.Equal(Image.Skip((int)section.PointerToRawData).Take(16), byRva.ToArray());
				Assert.Equal((uint)Image.Length, image.DataReaderFactory.Length);
			}
		}

		// The image owns its Win32 resources: the old ones are disposed when they're replaced
		[Fact]
		public void Win32ResourcesCanBeReplaced() {
			using (var image = new PEImage(Image)) {
				var original = image.Win32Resources;
				var replacement = new Win32ResourcesUser();

				image.Win32Resources = replacement;
				Assert.Same(replacement, image.Win32Resources);
				// The same object again changes nothing
				image.Win32Resources = replacement;
				Assert.Same(replacement, image.Win32Resources);
				image.Win32Resources = null;
				Assert.Null(image.Win32Resources);
				Assert.NotSame(replacement, original);
			}
		}

		[Fact]
		public void FileThatIsNotAPEFileIsRejected() {
			var notAPEFile = new byte[0x200];

			Assert.Throws<BadImageFormatException>(() => new PEImage(notAPEFile));
			Assert.Throws<BadImageFormatException>(() => new PEImage(new byte[10]));
			Assert.Throws<BadImageFormatException>(() => new PEImage(new byte[0]));
		}

		// The file header says how many section headers follow the optional header. If the number is too big,
		// dnlib reads the headers that have room before the data of the first section (like Mono): the real
		// ones and then the zero bytes after them, as sections without a name and without data.
		// System.Reflection.Metadata rejects a number of headers that the file has no room for.
		[Theory]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(100)]
		[InlineData(0xFFFF - 2)]
		public void NumberOfSectionsThatIsTooBigIsLimitedToTheSizeOfTheHeaders(int extraSections) {
			var image = (byte[])Image.Clone();
			string expected;
			int extraSectionsRead;
			using (var peReader = new PEReader(ImmutableArray.Create(Image))) {
				var headers = peReader.PEHeaders;
				Assert.Equal(2, headers.SectionHeaders.Length);
				// The section headers start after the optional header and have 0x28 bytes each
				int sectionHeadersOffset = headers.PEHeaderStartOffset + headers.CoffHeader.SizeOfOptionalHeader;
				int room = (headers.SectionHeaders[0].PointerToRawData - sectionHeadersOffset) / 0x28;
				Assert.Equal(3, room);
				extraSectionsRead = Math.Min(extraSections, room - 2);
				// The file header: the machine and then the number of sections
				BitConverter.GetBytes((ushort)(2 + extraSections)).CopyTo(image, headers.CoffHeaderStartOffset + 2);
			}
			using (var original = new PEImage(Image))
				expected = DescribeHeaders(original);
			for (int i = 0; i < extraSectionsRead; i++)
				expected += " |  0 0 0 0";

			using (var peImage = new PEImage(image)) {
				Assert.Equal(2 + extraSections, peImage.ImageNTHeaders.FileHeader.NumberOfSections);
				Assert.Equal(expected, DescribeHeaders(peImage));
			}
			using (var original = dnlib.DotNet.ModuleDefMD.Load(Image))
			using (var module = dnlib.DotNet.ModuleDefMD.Load(image))
				Assert.Equal(original.GetTypes().Select(a => a.FullName), module.GetTypes().Select(a => a.FullName));
		}

		// A file that ends in its headers
		[Theory]
		[InlineData("in the DOS header")]
		[InlineData("in the file header")]
		[InlineData("in the optional header")]
		[InlineData("in the section headers")]
		public void FileThatEndsInItsHeadersIsRejected(string where) {
			int length;
			using (var peReader = new PEReader(ImmutableArray.Create(Image))) {
				var headers = peReader.PEHeaders;
				switch (where) {
				case "in the DOS header": length = 0x20; break;
				case "in the file header": length = headers.CoffHeaderStartOffset + 10; break;
				case "in the optional header": length = headers.PEHeaderStartOffset + 0x40; break;
				case "in the section headers": length = headers.PEHeaderStartOffset + headers.CoffHeader.SizeOfOptionalHeader + 0x28 + 0x10; break;
				default: throw new ArgumentException(where);
				}
			}
			var image = new byte[length];
			Array.Copy(Image, image, length);

			var exception = Record.Exception(() => new PEImage(image));
			Assert.True(exception is BadImageFormatException || exception is System.IO.IOException, exception?.ToString());
		}
	}
}
