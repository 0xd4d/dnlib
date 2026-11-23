// dnlib: See LICENSE.txt for more info

using Xunit;

namespace dnlib.Tests.InvalidMetadata {
	/// <summary>
	/// Changes one byte of a valid portable PDB at a time, then reads the (valid) module with it, reads all debug
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
	}
}
