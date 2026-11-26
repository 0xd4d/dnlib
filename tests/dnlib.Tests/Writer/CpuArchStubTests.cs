// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// A managed method that's exported, or the entry point of a mixed-mode file, is called by native code
	/// through a small piece of machine code (a "stub") that jumps to the address in the method's vtable
	/// slot. The writer creates the stub for the processor of the file, and the native writer finds the slot
	/// again from the stub of the file it reads. Every stub that's written must be read as the same slot.
	/// </summary>
	public class CpuArchStubTests {
		const uint SlotRva = 0x00012340;

		static readonly byte[] image = CSharpCompiler.Compile("public class C { public static void M() { } }", new CSharpCompilerOptions { Platform = Microsoft.CodeAnalysis.Platform.X64 }).Image;

		public static TheoryData<Machine, string> Stubs {
			get {
				var data = new TheoryData<Machine, string>();
				foreach (var machine in new[] { Machine.I386, Machine.AMD64, Machine.IA64, Machine.ARMNT, Machine.ARM64, Machine.AMD64_Native_Linux, Machine.I386_Native_Apple, Machine.ARMNT_Native_FreeBSD, Machine.ARM64_Native_Linux }) {
					data.Add(machine, "Export");
					data.Add(machine, "EntryPoint");
				}
				return data;
			}
		}

		// Writes the stub into the code section of a copy of the file. Returns the file and where the stub is.
		static PEImage WriteStubIntoTheFile(CpuArch cpuArch, StubType stubType, out uint stubOffset, out byte[] stub) {
			var newImage = (byte[])image.Clone();
			uint stubRva, alignment = cpuArch.GetStubAlignment(stubType);
			ulong imageBase;
			using (var original = new PEImage(image)) {
				imageBase = original.ImageNTHeaders.OptionalHeader.ImageBase;
				stubRva = ((uint)original.ImageSectionHeaders[0].VirtualAddress + 0x40 + alignment - 1) & ~(alignment - 1);
				stubOffset = (uint)original.ToFileOffset((RVA)stubRva);
			}
			var stream = new MemoryStream();
			cpuArch.WriteStub(stubType, new DataWriter(stream), imageBase, stubRva, SlotRva);
			stub = stream.ToArray();
			stub.CopyTo(newImage, stubOffset);
			return new PEImage(newImage);
		}

		static StubType Parse(string stubType) => (StubType)Enum.Parse(typeof(StubType), stubType);

		[Theory, MemberData(nameof(Stubs))]
		public void WrittenStubIsReadAsTheSameSlot(Machine machine, string stubTypeName) {
			var stubType = Parse(stubTypeName);
			Assert.True(CpuArch.TryGetCpuArch(machine, out var cpuArch));

			using (var peImage = WriteStubIntoTheFile(cpuArch, stubType, out uint stubOffset, out var stub)) {
				Assert.Equal(cpuArch.GetStubSize(stubType), (uint)stub.Length);
				Assert.True(cpuArch.GetStubCodeOffset(stubType) < stub.Length);
				var reader = peImage.CreateReader();
				// The entry point or the export is the address of the code in the stub
				reader.Position = stubOffset + cpuArch.GetStubCodeOffset(stubType);

				Assert.True(cpuArch.TryGetExportedRvaFromStub(ref reader, peImage, out uint slotRva));

				Assert.Equal(SlotRva, slotRva);
			}
		}

		[Theory, MemberData(nameof(Stubs))]
		public void CodeThatIsNotAStubIsNotRead(Machine machine, string stubTypeName) {
			var stubType = Parse(stubTypeName);
			Assert.True(CpuArch.TryGetCpuArch(machine, out var cpuArch));

			using (var peImage = WriteStubIntoTheFile(cpuArch, stubType, out uint stubOffset, out _)) {
				uint codeOffset = stubOffset + cpuArch.GetStubCodeOffset(stubType);

				// Only the first bytes of the stub
				var shortReader = peImage.CreateReader((FileOffset)codeOffset, 5);
				Assert.False(cpuArch.TryGetExportedRvaFromStub(ref shortReader, peImage, out uint slotRva));
				Assert.Equal(0U, slotRva);
				var emptyReader = peImage.CreateReader((FileOffset)codeOffset, 0);
				Assert.False(cpuArch.TryGetExportedRvaFromStub(ref emptyReader, peImage, out _));

				// Other code: the method bodies of the file
				var reader = peImage.CreateReader();
				reader.Position = (uint)peImage.ToFileOffset(peImage.ImageSectionHeaders[0].VirtualAddress) + 0x200;
				Assert.False(cpuArch.TryGetExportedRvaFromStub(ref reader, peImage, out _));
			}
		}

		// Every byte of the stub is changed, one at a time: the stub is then not read, or its address is another
		// one. Nothing throws, also if the address isn't in the file anymore.
		[Theory, MemberData(nameof(Stubs))]
		public void StubWithAChangedByteIsNotReadOrHasAnotherAddress(Machine machine, string stubTypeName) {
			var stubType = Parse(stubTypeName);
			Assert.True(CpuArch.TryGetCpuArch(machine, out var cpuArch));

			using (var original = WriteStubIntoTheFile(cpuArch, stubType, out uint stubOffset, out var stub)) {
				var bytes = original.CreateReader().ToArray();
				int notRead = 0, otherAddress = 0, sameAddress = 0;
				for (int i = 0; i < stub.Length; i++) {
					foreach (byte change in new byte[] { 0xFF, 0x80, 0x01 }) {
						var changed = (byte[])bytes.Clone();
						changed[stubOffset + i] ^= change;
						using (var peImage = new PEImage(changed)) {
							var reader = peImage.CreateReader();
							reader.Position = stubOffset + cpuArch.GetStubCodeOffset(stubType);
							if (!cpuArch.TryGetExportedRvaFromStub(ref reader, peImage, out uint slotRva)) {
								Assert.Equal(0U, slotRva);
								notRead++;
							}
							else if (slotRva == SlotRva)
								sameAddress++;
							else
								otherAddress++;
						}
					}
				}
				// The instructions are checked, the bytes of the address are the address. Only bytes that aren't
				// part of the code (before it, or padding) can be changed without a change.
				Assert.True(notRead > 0, "No changed byte made the stub invalid");
				Assert.True(otherAddress > 0, "No changed byte changed the address");
				Assert.True(sameAddress <= 3 * (stub.Length - 6), $"{sameAddress} changes were ignored");
			}
		}

		// The address in the stub is the address of the slot when the file is loaded: an address below the
		// address of the file isn't in the file
		[Theory]
		[InlineData(Machine.AMD64)]
		[InlineData(Machine.IA64)]
		public void StubWithAnAddressBelowTheImageBaseIsNotRead(Machine machine) {
			Assert.True(CpuArch.TryGetCpuArch(machine, out var cpuArch));
			using (var original = WriteStubIntoTheFile(cpuArch, StubType.Export, out uint stubOffset, out var stub)) {
				var bytes = original.CreateReader().ToArray();
				ulong imageBase = original.ImageNTHeaders.OptionalHeader.ImageBase;
				var slotAddress = LittleEndian.GetBytes(imageBase + SlotRva);
				// The address of the slot is somewhere in the stub
				int index = -1;
				for (int i = 0; i + 8 <= stub.Length; i++) {
					if (stub.Skip(i).Take(8).SequenceEqual(slotAddress))
						index = i;
				}
				Assert.True(index >= 0);
				LittleEndian.GetBytes(imageBase - 1).CopyTo(bytes, stubOffset + index);

				using (var peImage = new PEImage(bytes)) {
					var reader = peImage.CreateReader();
					reader.Position = stubOffset + cpuArch.GetStubCodeOffset(StubType.Export);
					Assert.False(cpuArch.TryGetExportedRvaFromStub(ref reader, peImage, out uint slotRva));
					Assert.Equal(0U, slotRva);
				}
			}
		}

		// Microsoft's C++ compiler (C++/CLI) writes another x64 stub than ilasm: a short jump over a first jump to
		// a second one, and both jump to the address in a slot that is relative to the next instruction.
		// Fixed in this commit: this stub wasn't read. See CppCliExportTests for files of that compiler.
		[Theory]
		[InlineData("EB 08 0F 0B FF 25 11 11 00 00 FF 25 RELATIVE", true)]
		[InlineData("FF 25 RELATIVE", true)]
		[InlineData("EB 00 FF 25 RELATIVE", true)]
		[InlineData("EB 08 0F 0B FF 25 11 11 00 00 FF 24 RELATIVE", false)]
		[InlineData("EB 7F", false)]
		[InlineData("EB 80", false)]
		[InlineData("FF 25 00 00", false)]
		[InlineData("FF 25 00 00 00 80", false)]
		public void StubOfMicrosoftsCppCompilerIsRead(string code, bool isRead) {
			Assert.True(CpuArch.TryGetCpuArch(Machine.AMD64, out var cpuArch));
			var newImage = (byte[])image.Clone();
			uint stubRva, stubOffset;
			using (var original = new PEImage(image)) {
				stubRva = (uint)original.ImageSectionHeaders[0].VirtualAddress + 0x40;
				stubOffset = (uint)original.ToFileOffset((RVA)stubRva);
			}
			var bytes = new System.Collections.Generic.List<byte>();
			foreach (var word in code.Split(' ')) {
				if (word == "RELATIVE")
					// The slot's address minus the address of the next instruction (after these 4 bytes)
					bytes.AddRange(LittleEndian.GetBytes((int)(SlotRva - (stubRva + (uint)bytes.Count + 4))));
				else
					bytes.Add(Convert.ToByte(word, 16));
			}
			// The rest of the section isn't code that could be read as the stub
			for (int i = 0; i < 0x200; i++)
				newImage[stubOffset + i] = 0xCC;
			bytes.CopyTo(newImage, (int)stubOffset);

			using (var peImage = new PEImage(newImage)) {
				var reader = peImage.CreateReader();
				reader.Position = stubOffset;

				Assert.Equal(isRead, cpuArch.TryGetExportedRvaFromStub(ref reader, peImage, out uint slotRva));

				Assert.Equal(isRead ? SlotRva : 0U, slotRva);
			}
		}

		// The ARM64 stub has the address of the slot relative to the stub's page: a number of 4K pages, which can
		// be negative, and the offset in the page. The offset is in the instruction that reads the slot, or it's
		// added by an instruction before it. dnlib and Microsoft's C++ compiler use x9, other tools can use
		// other registers. Added for issue 165: there was no ARM64 stub.
		[Theory]
		// A stub of Microsoft's C++ compiler: adrp x9,page ; ldr x9,[x9,#8] ; br x9
		[InlineData(0x1240U, 0x9008U, "49 00 00 90   29 05 40 F9   20 01 1F D6", true)]
		// adrp x16,page ; ldr x16,[x16,#0x340] ; br x16
		[InlineData(0x2040U, 0x12340U, "90 00 00 90   10 A2 41 F9   00 02 1F D6", true)]
		// The slot is in a page below the stub's page: -3 pages
		[InlineData(0x5040U, 0x2FF8U, "F0 FF FF B0   10 FE 47 F9   00 02 1F D6", true)]
		// The same page
		[InlineData(0x2040U, 0x2008U, "10 00 00 90   10 06 40 F9   00 02 1F D6", true)]
		// adrp x9,page ; add x9,x9,#0x340 ; ldr x10,[x9] ; br x10
		[InlineData(0x2040U, 0x12340U, "89 00 00 90   29 01 0D 91   2A 01 40 F9   40 01 1F D6", true)]
		// adrp x17,page ; ldr x17,[x17,#0x340] ; br x17
		[InlineData(0x2040U, 0x12340U, "91 00 00 90   31 A2 41 F9   20 02 1F D6", true)]
		// adr instead of adrp
		[InlineData(0x2040U, 0U, "90 00 00 10   10 A2 41 F9   00 02 1F D6", false)]
		// The address is read from another register than the one with the page
		[InlineData(0x2040U, 0U, "90 00 00 90   30 A2 41 F9   00 02 1F D6", false)]
		// It jumps to another register than the one that was read
		[InlineData(0x2040U, 0U, "90 00 00 90   10 A2 41 F9   20 02 1F D6", false)]
		// A 32-bit load
		[InlineData(0x2040U, 0U, "90 00 00 90   10 A2 41 B9   00 02 1F D6", false)]
		// blr (a call) instead of br
		[InlineData(0x2040U, 0U, "90 00 00 90   10 A2 41 F9   00 02 3F D6", false)]
		// The add is to another register
		[InlineData(0x2040U, 0U, "89 00 00 90   2B 01 0D 91   2A 01 40 F9   40 01 1F D6", false)]
		// The slot is before the start of the file: -4 pages from page 0x2000
		[InlineData(0x2040U, 0U, "F0 FF FF 90   10 A2 41 F9   00 02 1F D6", false)]
		public void Arm64StubIsRead(uint stubRva, uint slotRva, string code, bool isRead) {
			Assert.True(CpuArch.TryGetCpuArch(Machine.ARM64, out var cpuArch));
			var newImage = (byte[])image.Clone();
			uint stubOffset;
			using (var original = new PEImage(image)) {
				// The file has one page of code, the stub can be in any page since only its RVA is used
				uint sectionRva = (uint)original.ImageSectionHeaders[0].VirtualAddress;
				Assert.Equal(0x2000U, sectionRva);
				stubOffset = (uint)original.ToFileOffset((RVA)(sectionRva + (stubRva & 0xFFF)));
				PatchSectionRva(newImage, original, stubRva & ~0xFFFU);
			}
			var bytes = code.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(a => Convert.ToByte(a, 16)).ToArray();
			for (int i = 0; i < 0x40; i++)
				newImage[stubOffset + i] = 0;
			bytes.CopyTo(newImage, (int)stubOffset);

			using (var peImage = new PEImage(newImage)) {
				var reader = peImage.CreateReader();
				reader.Position = stubOffset;

				Assert.Equal(isRead, cpuArch.TryGetExportedRvaFromStub(ref reader, peImage, out uint readRva));

				Assert.Equal(isRead ? slotRva : 0U, readRva);
			}
		}

		// Moves the first section (the code) to another RVA
		static void PatchSectionRva(byte[] newImage, PEImage original, uint rva) {
			var section = original.ImageSectionHeaders[0];
			LittleEndian.GetBytes(rva).CopyTo(newImage, (int)section.StartOffset + 12);
		}

		// The stub that dnlib writes for ARM64: the instructions of Microsoft's C++ compiler, for any distance to
		// the slot
		[Theory]
		[InlineData(0x2040U, 0x12340U, "89 00 00 90   29 A1 41 F9   20 01 1F D6")]
		[InlineData(0x5040U, 0x2FF8U, "E9 FF FF B0   29 FD 47 F9   20 01 1F D6")]
		[InlineData(0x2FFCU, 0x2000U, "09 00 00 90   29 01 40 F9   20 01 1F D6")]
		[InlineData(0x2000U, 0xFFFFFFF8U, "E9 FF 7F B0   29 FD 47 F9   20 01 1F D6")]
		[InlineData(0xFFFFF000U, 0x2008U, "09 00 80 F0   29 05 40 F9   20 01 1F D6")]
		public void Arm64StubIsWritten(uint stubRva, uint slotRva, string expected) {
			Assert.True(CpuArch.TryGetCpuArch(Machine.ARM64, out var cpuArch));
			foreach (var stubType in new[] { StubType.Export, StubType.EntryPoint }) {
				var stream = new MemoryStream();

				cpuArch.WriteStub(stubType, new DataWriter(stream), 0x180000000, stubRva, slotRva);

				Assert.Equal(expected.Replace(" ", ""), BitConverter.ToString(stream.ToArray()).Replace("-", ""));
			}
		}

		// The instruction that reads the slot can only read at a multiple of 8. dnlib's slots always are.
		[Fact]
		public void Arm64StubOfAnUnalignedSlotIsAnError() {
			Assert.True(CpuArch.TryGetCpuArch(Machine.ARM64, out var cpuArch));

			Assert.Throws<InvalidOperationException>(() => cpuArch.WriteStub(StubType.Export, new DataWriter(new MemoryStream()), 0x180000000, 0x2000, 0x3004));
		}

		[Theory]
		[InlineData(Machine.RISCV64)]
		[InlineData(Machine.LOONGARCH64)]
		[InlineData(Machine.Unknown)]
		[InlineData(Machine.POWERPC)]
		public void ProcessorsWithoutStubs(Machine machine) {
			Assert.False(CpuArch.TryGetCpuArch(machine, out var cpuArch));
			Assert.Null(cpuArch);
		}

		[Fact]
		public void StubTypeThatDoesNotExistIsAnError() {
			foreach (var machine in new[] { Machine.I386, Machine.AMD64, Machine.IA64, Machine.ARMNT, Machine.ARM64 }) {
				Assert.True(CpuArch.TryGetCpuArch(machine, out var cpuArch));
				var invalid = (StubType)99;

				Assert.Throws<ArgumentOutOfRangeException>(() => cpuArch.GetStubAlignment(invalid));
				Assert.Throws<ArgumentOutOfRangeException>(() => cpuArch.GetStubSize(invalid));
				Assert.Throws<ArgumentOutOfRangeException>(() => cpuArch.GetStubCodeOffset(invalid));
				Assert.Throws<ArgumentOutOfRangeException>(() => cpuArch.WriteStub(invalid, new DataWriter(new MemoryStream()), 0x400000, 0x2000, 0x3000));
			}
		}
	}
}
