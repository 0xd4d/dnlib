// dnlib: See LICENSE.txt for more info

using Xunit;

namespace dnlib.Tests.InvalidMetadata {
	/// <summary>
	/// Changes one byte of a valid portable PDB or Windows PDB at a time, then reads the (valid) module with it, reads all debug
	/// info and writes the module and a PDB. An invalid PDB must never stop a module from loading: dnlib reads
	/// the debug info it can and ignores the rest. See <see cref="InvalidFileChecker"/>.
	/// </summary>
	public class PatchedPdbByteTests {
		static byte[] Image => Fixtures.Fixtures.Features.Image;
		static byte[] ValidPdb => Fixtures.Fixtures.Features.Pdb!;

		// step: every step-th byte is changed
		static void PatchBytes(int step, byte[] values) {
			for (int offset = 0; offset < ValidPdb.Length; offset += step) {
				foreach (byte value in values) {
					if (ValidPdb[offset] == value)
						continue;
					var pdb = (byte[])ValidPdb.Clone();
					pdb[offset] = value;
					InvalidFileChecker.ReadAndWriteWithPdb(Image, ValidPdb, pdb, $"PDB byte at offset 0x{offset:X}: 0x{ValidPdb[offset]:X2} -> 0x{value:X2}");
				}
			}
		}

		[Fact]
		public void EverySeventhByte() =>
			// 7 doesn't divide the size of anything in the file, so over time every kind of field is changed
			PatchBytes(7, new byte[] { 0xFF });

		[Fact, Trait(TestCategories.Name, TestCategories.Slow)]
		public void EveryByte() =>
			PatchBytes(1, new byte[] { 0x00, 0xFF });

		[Fact]
		public void TruncatedPdb() {
			for (int length = 0; length < ValidPdb.Length; length += 13) {
				var pdb = new byte[length];
				System.Array.Copy(ValidPdb, pdb, length);
				InvalidFileChecker.ReadAndWriteWithPdb(Image, ValidPdb, pdb, $"PDB truncated to {length} of {ValidPdb.Length} bytes");
			}
		}

		// The same with a Windows PDB, which only Windows can create. It's a file with pages and streams: the
		// first pages say where the pages of every stream are.
		static byte[] ImageOfWindowsPdb => Fixtures.Fixtures.FeaturesWithWindowsPdb.Image;
		static byte[] ValidWindowsPdb => Fixtures.Fixtures.FeaturesWithWindowsPdb.Pdb!;

		static void SkipUnlessWindows() =>
			Assert.SkipUnless(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows), "A Windows PDB can only be created on Windows");

		static void PatchWindowsPdbBytes(int step, byte[] values) {
			for (int offset = 0; offset < ValidWindowsPdb.Length; offset += step) {
				foreach (byte value in values) {
					if (ValidWindowsPdb[offset] == value)
						continue;
					var pdb = (byte[])ValidWindowsPdb.Clone();
					pdb[offset] = value;
					InvalidFileChecker.ReadAndWriteWithPdb(ImageOfWindowsPdb, ValidWindowsPdb, pdb, $"Windows PDB byte at offset 0x{offset:X}: 0x{ValidWindowsPdb[offset]:X2} -> 0x{value:X2}");
				}
			}
		}

		[Fact]
		public void EveryFortyFirstByteOfAWindowsPdb() {
			SkipUnlessWindows();
			PatchWindowsPdbBytes(41, new byte[] { 0xFF });
		}

		// The header (the first page) and the list of the streams' pages have the numbers that everything else
		// is found with
		[Fact]
		public void EveryByteOfTheFirstPageOfAWindowsPdb() {
			SkipUnlessWindows();
			for (int offset = 0; offset < 0x60; offset++) {
				foreach (byte value in new byte[] { 0x00, 0x01, 0x7F, 0x80, 0xFF }) {
					if (ValidWindowsPdb[offset] == value)
						continue;
					var pdb = (byte[])ValidWindowsPdb.Clone();
					pdb[offset] = value;
					InvalidFileChecker.ReadAndWriteWithPdb(ImageOfWindowsPdb, ValidWindowsPdb, pdb, $"Windows PDB byte at offset 0x{offset:X}: 0x{ValidWindowsPdb[offset]:X2} -> 0x{value:X2}");
				}
			}
		}

		[Fact, Trait(TestCategories.Name, TestCategories.Slow)]
		public void EveryByteOfAWindowsPdb() {
			SkipUnlessWindows();
			PatchWindowsPdbBytes(1, new byte[] { 0x00, 0xFF });
		}

		[Fact]
		public void TruncatedWindowsPdb() {
			SkipUnlessWindows();
			for (int length = 0; length < ValidWindowsPdb.Length; length += 257) {
				var pdb = new byte[length];
				System.Array.Copy(ValidWindowsPdb, pdb, length);
				InvalidFileChecker.ReadAndWriteWithPdb(ImageOfWindowsPdb, ValidWindowsPdb, pdb, $"Windows PDB truncated to {length} of {ValidWindowsPdb.Length} bytes");
			}
		}
	}
}
