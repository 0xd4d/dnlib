// dnlib: See LICENSE.txt for more info

using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.PE;
using dnlib.TestUtils;
using Microsoft.CodeAnalysis;
using Xunit;

namespace dnlib.Tests.Loading {
	/// <summary>
	/// Every field of the headers of a PE file that dnlib reads (the file header, the optional header with its
	/// data directories, the section headers and the .NET header) must have the value that
	/// System.Reflection.Metadata's <see cref="PEHeaders"/> reads. A 32-bit file has another optional header
	/// (PE32) than a 64-bit file (PE32+).
	/// </summary>
	public class PEHeaderFieldTests {
		const string Source = "public static class Program { public static int Main() => 0; }";

		static byte[] Compile(Platform platform, OutputKind outputKind) =>
			CSharpCompiler.Compile(Source, new CSharpCompilerOptions { Platform = platform, OutputKind = outputKind, Win32Resources = true }).Image;

		public static TheoryData<Platform, OutputKind> Files => new TheoryData<Platform, OutputKind> {
			{ Platform.AnyCpu, OutputKind.DynamicallyLinkedLibrary },
			{ Platform.AnyCpu, OutputKind.ConsoleApplication },
			{ Platform.X86, OutputKind.ConsoleApplication },
			{ Platform.X86, OutputKind.WindowsApplication },
			{ Platform.X64, OutputKind.ConsoleApplication },
			{ Platform.X64, OutputKind.DynamicallyLinkedLibrary },
			{ Platform.Arm64, OutputKind.ConsoleApplication },
		};

		[Theory, MemberData(nameof(Files))]
		public void FileHeaderHasTheValuesOfTheFile(Platform platform, OutputKind outputKind) {
			var image = Compile(platform, outputKind);
			using (var peImage = new PEImage(image))
			using (var peReader = new PEReader(new MemoryStream(image))) {
				var expected = peReader.PEHeaders.CoffHeader;
				var header = peImage.ImageNTHeaders.FileHeader;

				Assert.Equal(0x4550U, peImage.ImageNTHeaders.Signature);
				Assert.Equal((uint)peReader.PEHeaders.PEHeaderStartOffset - 4 - 20, peImage.ImageDosHeader.NTHeadersOffset);
				Assert.Equal((ushort)expected.Machine, (ushort)header.Machine);
				Assert.Equal(expected.NumberOfSections, header.NumberOfSections);
				Assert.Equal((uint)expected.TimeDateStamp, header.TimeDateStamp);
				Assert.Equal((uint)expected.PointerToSymbolTable, header.PointerToSymbolTable);
				Assert.Equal((uint)expected.NumberOfSymbols, header.NumberOfSymbols);
				Assert.Equal((uint)expected.SizeOfOptionalHeader, header.SizeOfOptionalHeader);
				Assert.Equal((ushort)expected.Characteristics, (ushort)header.Characteristics);
			}
		}

		[Theory, MemberData(nameof(Files))]
		public void OptionalHeaderHasTheValuesOfTheFile(Platform platform, OutputKind outputKind) {
			var image = Compile(platform, outputKind);
			using (var peImage = new PEImage(image))
			using (var peReader = new PEReader(new MemoryStream(image))) {
				var expected = peReader.PEHeaders.PEHeader!;
				var header = peImage.ImageNTHeaders.OptionalHeader;
				bool is64Bit = platform == Platform.X64 || platform == Platform.Arm64;

				if (is64Bit)
					Assert.IsType<ImageOptionalHeader64>(header);
				else
					Assert.IsType<ImageOptionalHeader32>(header);
				Assert.Equal(is64Bit ? 0x20B : 0x10B, header.Magic);
				Assert.Equal((ushort)expected.Magic, header.Magic);
				Assert.Equal(expected.MajorLinkerVersion, header.MajorLinkerVersion);
				Assert.Equal(expected.MinorLinkerVersion, header.MinorLinkerVersion);
				Assert.Equal((uint)expected.SizeOfCode, header.SizeOfCode);
				Assert.Equal((uint)expected.SizeOfInitializedData, header.SizeOfInitializedData);
				Assert.Equal((uint)expected.SizeOfUninitializedData, header.SizeOfUninitializedData);
				Assert.Equal((uint)expected.AddressOfEntryPoint, (uint)header.AddressOfEntryPoint);
				Assert.Equal((uint)expected.BaseOfCode, (uint)header.BaseOfCode);
				// Only a 32-bit header has this field
				Assert.Equal(is64Bit ? 0U : (uint)expected.BaseOfData, (uint)header.BaseOfData);
				Assert.Equal(expected.ImageBase, header.ImageBase);
				Assert.Equal((uint)expected.SectionAlignment, header.SectionAlignment);
				Assert.Equal((uint)expected.FileAlignment, header.FileAlignment);
				Assert.Equal(expected.MajorOperatingSystemVersion, header.MajorOperatingSystemVersion);
				Assert.Equal(expected.MinorOperatingSystemVersion, header.MinorOperatingSystemVersion);
				Assert.Equal(expected.MajorImageVersion, header.MajorImageVersion);
				Assert.Equal(expected.MinorImageVersion, header.MinorImageVersion);
				Assert.Equal(expected.MajorSubsystemVersion, header.MajorSubsystemVersion);
				Assert.Equal(expected.MinorSubsystemVersion, header.MinorSubsystemVersion);
				Assert.Equal(0U, header.Win32VersionValue);
				Assert.Equal((uint)expected.SizeOfImage, header.SizeOfImage);
				Assert.Equal((uint)expected.SizeOfHeaders, header.SizeOfHeaders);
				Assert.Equal(expected.CheckSum, header.CheckSum);
				Assert.Equal((ushort)expected.Subsystem, (ushort)header.Subsystem);
				Assert.Equal((ushort)expected.DllCharacteristics, (ushort)header.DllCharacteristics);
				Assert.Equal(expected.SizeOfStackReserve, header.SizeOfStackReserve);
				Assert.Equal(expected.SizeOfStackCommit, header.SizeOfStackCommit);
				Assert.Equal(expected.SizeOfHeapReserve, header.SizeOfHeapReserve);
				Assert.Equal(expected.SizeOfHeapCommit, header.SizeOfHeapCommit);
				Assert.Equal(0U, header.LoaderFlags);
				Assert.Equal((uint)expected.NumberOfRvaAndSizes, header.NumberOfRvaAndSizes);

				Assert.Equal(outputKind == OutputKind.WindowsApplication ? dnlib.PE.Subsystem.WindowsGui : dnlib.PE.Subsystem.WindowsCui, header.Subsystem);
			}
		}

		[Theory, MemberData(nameof(Files))]
		public void DataDirectoriesHaveTheValuesOfTheFile(Platform platform, OutputKind outputKind) {
			var image = Compile(platform, outputKind);
			using (var peImage = new PEImage(image))
			using (var peReader = new PEReader(new MemoryStream(image))) {
				var expected = peReader.PEHeaders.PEHeader!;
				var directories = peImage.ImageNTHeaders.OptionalHeader.DataDirectories;
				// In the order of the file
				var expectedDirectories = new[] {
					expected.ExportTableDirectory, expected.ImportTableDirectory, expected.ResourceTableDirectory, expected.ExceptionTableDirectory,
					expected.CertificateTableDirectory, expected.BaseRelocationTableDirectory, expected.DebugTableDirectory, expected.CopyrightTableDirectory,
					expected.GlobalPointerTableDirectory, expected.ThreadLocalStorageTableDirectory, expected.LoadConfigTableDirectory, expected.BoundImportTableDirectory,
					expected.ImportAddressTableDirectory, expected.DelayImportTableDirectory, expected.CorHeaderTableDirectory,
				};

				Assert.Equal(16, directories.Length);
				for (int i = 0; i < expectedDirectories.Length; i++) {
					Assert.Equal((uint)expectedDirectories[i].RelativeVirtualAddress, (uint)directories[i].VirtualAddress);
					Assert.Equal((uint)expectedDirectories[i].Size, directories[i].Size);
				}
				Assert.Equal(0U, directories[15].Size);
				// The file has Win32 resources and a .NET header
				Assert.NotEqual(0U, directories[2].Size);
				Assert.NotEqual(0U, directories[14].Size);
			}
		}

		[Theory, MemberData(nameof(Files))]
		public void SectionHeadersHaveTheValuesOfTheFile(Platform platform, OutputKind outputKind) {
			var image = Compile(platform, outputKind);
			using (var peImage = new PEImage(image))
			using (var peReader = new PEReader(new MemoryStream(image))) {
				var expectedSections = peReader.PEHeaders.SectionHeaders;
				var sections = peImage.ImageSectionHeaders;

				Assert.Equal(expectedSections.Length, sections.Count);
				Assert.Contains(sections, a => a.DisplayName == ".text");
				for (int i = 0; i < sections.Count; i++) {
					var expected = expectedSections[i];
					var section = sections[i];
					Assert.Equal(expected.Name, section.DisplayName);
					// The name is 8 bytes, with zeroes after a shorter name
					Assert.Equal(8, section.Name.Length);
					Assert.Equal(expected.Name, System.Text.Encoding.UTF8.GetString(section.Name).TrimEnd('\0'));
					Assert.Equal((uint)expected.VirtualSize, section.VirtualSize);
					Assert.Equal((uint)expected.VirtualAddress, (uint)section.VirtualAddress);
					Assert.Equal((uint)expected.SizeOfRawData, section.SizeOfRawData);
					Assert.Equal((uint)expected.PointerToRawData, section.PointerToRawData);
					Assert.Equal((uint)expected.PointerToRelocations, section.PointerToRelocations);
					Assert.Equal((uint)expected.PointerToLineNumbers, section.PointerToLinenumbers);
					Assert.Equal(expected.NumberOfRelocations, section.NumberOfRelocations);
					Assert.Equal(expected.NumberOfLineNumbers, section.NumberOfLinenumbers);
					Assert.Equal((uint)expected.SectionCharacteristics, section.Characteristics);
				}
			}
		}

		[Theory, MemberData(nameof(Files))]
		public void DotNetHeaderHasTheValuesOfTheFile(Platform platform, OutputKind outputKind) {
			var image = Compile(platform, outputKind);
			using (var module = ModuleDefMD.Load(image))
			using (var peReader = new PEReader(new MemoryStream(image))) {
				var expected = peReader.PEHeaders.CorHeader!;
				var header = module.Metadata.ImageCor20Header;

				Assert.Equal(0x48U, header.CB);
				Assert.Equal(expected.MajorRuntimeVersion, header.MajorRuntimeVersion);
				Assert.Equal(expected.MinorRuntimeVersion, header.MinorRuntimeVersion);
				Assert.Equal((uint)expected.Flags, (uint)header.Flags);
				Assert.Equal((uint)expected.EntryPointTokenOrRelativeVirtualAddress, header.EntryPointToken_or_RVA);
				Assert.False(header.HasNativeHeader);

				var directories = new[] {
					(expected.MetadataDirectory, header.Metadata),
					(expected.ResourcesDirectory, header.Resources),
					(expected.StrongNameSignatureDirectory, header.StrongNameSignature),
					(expected.CodeManagerTableDirectory, header.CodeManagerTable),
					(expected.VtableFixupsDirectory, header.VTableFixups),
					(expected.ExportAddressTableJumpsDirectory, header.ExportAddressTableJumps),
					(expected.ManagedNativeHeaderDirectory, header.ManagedNativeHeader),
				};
				foreach (var (expectedDirectory, directory) in directories) {
					Assert.Equal((uint)expectedDirectory.RelativeVirtualAddress, (uint)directory.VirtualAddress);
					Assert.Equal((uint)expectedDirectory.Size, directory.Size);
				}
				Assert.NotEqual(0U, header.Metadata.Size);

				// What the module says about itself comes from these headers
				Assert.Equal(header.Flags, module.Cor20HeaderFlags);
				Assert.Equal(peReader.PEHeaders.PEHeader!.Magic == PEMagic.PE32Plus ? 8 : 4, module.GetPointerSize(4));
				if (outputKind == OutputKind.DynamicallyLinkedLibrary)
					Assert.Null(module.EntryPoint);
				else
					Assert.Equal(header.EntryPointToken_or_RVA, module.EntryPoint!.MDToken.Raw);
			}
		}
	}
}
