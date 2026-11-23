// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;
using System.Text;

namespace dnlib.TestUtils {
	/// <summary>
	/// Reads the names of the functions a PE file exports (its export directory), without dnlib:
	/// the PE headers are read by System.Reflection.Metadata's <see cref="PEReader"/>.
	/// </summary>
	public static class PEExports {
		/// <summary>
		/// Returns the names of the exported functions in the order they're stored (sorted by name).
		/// Functions that are only exported by ordinal have no name and aren't returned.
		/// </summary>
		public static List<string> GetNames(byte[] image) {
			var names = new List<string>();
			using (var peReader = new PEReader(ImmutableArray.Create(image))) {
				var directory = peReader.PEHeaders.PEHeader!.ExportTableDirectory;
				if (directory.Size == 0)
					return names;

				// IMAGE_EXPORT_DIRECTORY: NumberOfNames is at offset 24, AddressOfNames (the RVA of an array
				// with the RVA of each name) at offset 32
				var exportDirectory = peReader.GetSectionData(directory.RelativeVirtualAddress).GetReader();
				exportDirectory.Offset = 24;
				int numberOfNames = exportDirectory.ReadInt32();
				exportDirectory.Offset = 32;
				int addressOfNames = exportDirectory.ReadInt32();

				var nameRvas = peReader.GetSectionData(addressOfNames).GetReader();
				for (int i = 0; i < numberOfNames; i++) {
					var name = peReader.GetSectionData(nameRvas.ReadInt32()).GetReader();
					var bytes = new List<byte>();
					for (byte b = name.ReadByte(); b != 0; b = name.ReadByte())
						bytes.Add(b);
					names.Add(Encoding.UTF8.GetString(bytes.ToArray()));
				}
			}
			return names;
		}
	}
}
