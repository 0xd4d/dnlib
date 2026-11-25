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
				foreach (var machine in new[] { Machine.I386, Machine.AMD64, Machine.IA64, Machine.ARMNT, Machine.AMD64_Native_Linux, Machine.I386_Native_Apple, Machine.ARMNT_Native_FreeBSD }) {
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
				var slotAddress = BitConverter.GetBytes(imageBase + SlotRva);
				// The address of the slot is somewhere in the stub
				int index = -1;
				for (int i = 0; i + 8 <= stub.Length; i++) {
					if (stub.Skip(i).Take(8).SequenceEqual(slotAddress))
						index = i;
				}
				Assert.True(index >= 0);
				BitConverter.GetBytes(imageBase - 1).CopyTo(bytes, stubOffset + index);

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
					bytes.AddRange(BitConverter.GetBytes((int)(SlotRva - (stubRva + (uint)bytes.Count + 4))));
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

		[Theory]
		[InlineData(Machine.ARM64)]
		[InlineData(Machine.ARM64_Native_Linux)]
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
			foreach (var machine in new[] { Machine.I386, Machine.AMD64, Machine.IA64, Machine.ARMNT }) {
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
