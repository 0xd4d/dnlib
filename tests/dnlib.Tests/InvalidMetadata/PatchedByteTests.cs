// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet.MD;
using dnlib.PE;
using Xunit;

namespace dnlib.Tests.InvalidMetadata {
	/// <summary>
	/// Changes one byte of a valid assembly at a time, then reads everything and writes the module.
	/// See <see cref="InvalidFileChecker"/> for what dnlib may do with such a file.
	///
	/// <see cref="PatchedTableCellTests"/> only changes the metadata tables. This also reaches the PE headers,
	/// the .NET header, the metadata header, the heaps, the method bodies and the resources.
	/// </summary>
	public class PatchedByteTests {
		static byte[] ValidImage => Fixtures.Fixtures.Features.Image;

		static readonly byte[] allValues = { 0x00, 0x01, 0x7F, 0x80, 0xFF };

		static void PatchBytes(int start, int end, byte[] values) {
			int patches = 0;
			for (int offset = start; offset < end; offset++) {
				foreach (byte value in values) {
					if (ValidImage[offset] == value)
						continue;
					var image = (byte[])ValidImage.Clone();
					image[offset] = value;
					InvalidFileChecker.ReadAndWrite(ValidImage, image, $"byte at offset 0x{offset:X}: 0x{ValidImage[offset]:X2} -> 0x{value:X2}");
					patches++;
				}
			}
			Assert.True(patches > 0, "No byte was patched");
		}

		// The file offset of the first metadata table row. Everything from the metadata header to here is
		// header data: the metadata header, the stream headers and the header of the tables stream (#~).
		static void GetMetadataHeaders(out int start, out int end) {
			using (var peImage = new PEImage((byte[])ValidImage.Clone())) {
				var metadata = MetadataFactory.CreateMetadata(peImage);
				start = (int)metadata.MetadataHeader.StartOffset;
				end = int.MaxValue;
				foreach (var table in metadata.TablesStream.MDTables) {
					if (!table.IsEmpty)
						end = Math.Min(end, (int)table.StartOffset);
				}
			}
		}

		[Fact]
		public void PEHeaders() {
			// The DOS header, the PE headers and the section headers are at the start of the file
			using (var peImage = new PEImage((byte[])ValidImage.Clone())) {
				int end = (int)peImage.ImageSectionHeaders[peImage.ImageSectionHeaders.Count - 1].EndOffset;
				PatchBytes(0, end, allValues);
			}
		}

		[Fact]
		public void DotNetHeader() {
			using (var peImage = new PEImage((byte[])ValidImage.Clone())) {
				var cor20Header = MetadataFactory.CreateMetadata(peImage).ImageCor20Header;
				PatchBytes((int)cor20Header.StartOffset, (int)cor20Header.EndOffset, allValues);
			}
		}

		[Fact]
		public void MetadataHeaders() {
			GetMetadataHeaders(out int start, out int end);
			PatchBytes(start, end, allValues);
		}

		[Fact, Trait(TestCategories.Name, TestCategories.Slow)]
		public void EveryByteOfTheFile() =>
			PatchBytes(0, ValidImage.Length, new byte[] { 0x00, 0xFF });
	}
}
