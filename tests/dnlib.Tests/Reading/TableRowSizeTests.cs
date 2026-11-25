// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using dnlib.DotNet.MD;
using dnlib.IO;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A column that is the number of a row in another table has 2 bytes, or 4 bytes if that table has more
	/// than 0xFFFF rows. A column that can be a row of one of several tables (a coded token) uses 1 to 5 of
	/// its bits to say which table, so it gets 4 bytes at 0x10000 >> bits rows in the biggest of these tables.
	/// A column that is an offset in a heap has 4 bytes if the header of the tables says so. If dnlib gets one
	/// size wrong, it reads every row after the first one of that table, and all tables after it, from the
	/// wrong place.
	///
	/// The tests create the header of the tables with every table at every one of these limits, and compare
	/// the row sizes of all tables with the ones of System.Reflection.Metadata, whose tests of this
	/// (<c>LargeTablesAndHeapsTests</c>) have the sizes as numbers. The rows themselves are zero bytes.
	/// </summary>
	public class TableRowSizeTests {
		const int TableCount = 64;
		const int FirstDebugTable = (int)Table.Document;

		// The tables of the specification, without the ones that only uncompressed metadata (#-) has
		static readonly Table[] typeSystemTables = Enumerable.Range(0, FirstDebugTable).Select(a => (Table)a)
			.Where(a => Enum.IsDefined(typeof(Table), a))
			.Where(a => a != Table.FieldPtr && a != Table.MethodPtr && a != Table.ParamPtr && a != Table.EventPtr && a != Table.PropertyPtr)
			.Where(a => a != Table.ENCLog && a != Table.ENCMap)
			// No compiler writes these tables, and System.Reflection.Metadata rejects a file that has them
			.Where(a => a != Table.AssemblyProcessor && a != Table.AssemblyOS && a != Table.AssemblyRefProcessor && a != Table.AssemblyRefOS)
			.ToArray();

		static readonly Table[] debugTables = Enumerable.Range(FirstDebugTable, (int)Table.CustomDebugInformation + 1 - FirstDebugTable).Select(a => (Table)a).ToArray();

		// A table's rows around the number where a column that has a row of that table gets bigger:
		// 0x10000 rows, and 0x10000 >> bits for the coded tokens (they have 1 to 5 bits)
		static IEnumerable<uint> RowCountsAtTheLimits() {
			for (int bits = 0; bits <= 5; bits++) {
				uint limit = 0x10000U >> bits;
				yield return limit - 1;
				yield return limit;
				yield return limit + 1;
			}
		}

		// No row of the test files is bigger
		const int MaxRowSize = 48;

		static void Align(MemoryStream stream) {
			while (stream.Length % 4 != 0)
				stream.WriteByte(0);
		}

		// The metadata with a tables stream that has the header and rows of zero bytes.
		// externalRowCounts: null, or the rows of the tables of the module in the #Pdb stream of a portable PDB.
		static byte[] CreateMetadata(uint[] rowCounts, byte heapFlags, uint[]? externalRowCounts) {
			var tables = new MemoryStream();
			var writer = new BinaryWriter(tables);
			writer.Write(0U);           // reserved
			writer.Write((byte)2);      // major version
			writer.Write((byte)0);      // minor version
			writer.Write(heapFlags);
			writer.Write((byte)1);      // reserved
			ulong valid = 0;
			long rows = 0;
			for (int i = 0; i < TableCount; i++) {
				if (rowCounts[i] != 0)
					valid |= 1UL << i;
				rows += rowCounts[i];
			}
			writer.Write(valid);
			writer.Write(valid);        // sorted tables: rows of zero bytes are sorted
			for (int i = 0; i < TableCount; i++) {
				if (rowCounts[i] != 0)
					writer.Write(rowCounts[i]);
			}
			tables.SetLength(tables.Length + rows * MaxRowSize);

			byte[]? pdb = null;
			if (externalRowCounts is not null) {
				var pdbStream = new MemoryStream();
				var pdbWriter = new BinaryWriter(pdbStream);
				pdbWriter.Write(new byte[20]);  // the ID of the PDB
				pdbWriter.Write(0U);            // entry point
				ulong external = 0;
				for (int i = 0; i < TableCount; i++) {
					if (externalRowCounts[i] != 0)
						external |= 1UL << i;
				}
				pdbWriter.Write(external);
				for (int i = 0; i < TableCount; i++) {
					if (externalRowCounts[i] != 0)
						pdbWriter.Write(externalRowCounts[i]);
				}
				pdb = pdbStream.ToArray();
			}

			var streams = new List<KeyValuePair<string, byte[]>>();
			if (pdb is not null)
				streams.Add(new KeyValuePair<string, byte[]>("#Pdb", pdb));
			streams.Add(new KeyValuePair<string, byte[]>("#~", tables.ToArray()));

			var metadata = new MemoryStream();
			writer = new BinaryWriter(metadata);
			var version = Encoding.ASCII.GetBytes("v4.0.30319\0\0");
			writer.Write(0x424A5342);
			writer.Write((ushort)1);
			writer.Write((ushort)1);
			writer.Write(0U);
			writer.Write(version.Length);
			writer.Write(version);
			writer.Write((ushort)0);
			writer.Write((ushort)streams.Count);
			int offset = (int)metadata.Length + streams.Sum(a => 8 + ((a.Key.Length + 1 + 3) & ~3));
			foreach (var stream in streams) {
				writer.Write(offset);
				writer.Write(stream.Value.Length);
				writer.Write(Encoding.ASCII.GetBytes(stream.Key));
				writer.Write((byte)0);
				Align(metadata);
				offset += (stream.Value.Length + 3) & ~3;
			}
			foreach (var stream in streams) {
				writer.Write(stream.Value);
				Align(metadata);
			}
			return metadata.ToArray();
		}

		static readonly byte[] baseImage = new Writer.CreatedModule().Write();

		// A PE file with the metadata: it's added to the end of the last section of a small module
		static byte[] CreateImage(byte[] metadata) {
			using (var peReader = new System.Reflection.PortableExecutable.PEReader(ImmutableArray.Create(baseImage))) {
				var headers = peReader.PEHeaders;
				int lastIndex = headers.SectionHeaders.Length - 1;
				var last = headers.SectionHeaders[lastIndex];
				Assert.Equal(baseImage.Length, last.PointerToRawData + last.SizeOfRawData);
				var image = new byte[baseImage.Length + metadata.Length];
				baseImage.CopyTo(image, 0);
				metadata.CopyTo(image, baseImage.Length);

				// A section header has 0x28 bytes: the name (8 bytes), the size and the address in memory,
				// the size and the offset in the file
				int sectionHeader = headers.PEHeaderStartOffset + headers.CoffHeader.SizeOfOptionalHeader + lastIndex * 0x28;
				int newSize = last.SizeOfRawData + metadata.Length;
				BitConverter.GetBytes(newSize).CopyTo(image, sectionHeader + 8);
				BitConverter.GetBytes(newSize).CopyTo(image, sectionHeader + 16);
				// The size of the image in memory is at offset 56 of the optional header
				BitConverter.GetBytes(last.VirtualAddress + ((newSize + 0x1FFF) & ~0x1FFF)).CopyTo(image, headers.PEHeaderStartOffset + 56);
				// The .NET header: its size, the runtime version, and then the address and the size of the metadata
				BitConverter.GetBytes(last.VirtualAddress + last.SizeOfRawData).CopyTo(image, headers.CorHeaderStartOffset + 8);
				BitConverter.GetBytes(metadata.Length).CopyTo(image, headers.CorHeaderStartOffset + 12);
				return image;
			}
		}

		// dnlib reads the metadata of a module from a PE file, and metadata that isn't in a PE file as the
		// metadata of a portable PDB file
		static dnlib.DotNet.MD.Metadata ReadWithDnlib(byte[] metadata, bool isPdb) =>
			isPdb ? MetadataFactory.CreateStandalonePortablePDB(ByteArrayDataReaderFactory.Create(metadata, filename: null), verify: true) :
			MetadataFactory.CreateMetadata(new dnlib.PE.PEImage(CreateImage(metadata)));

		// Reads the metadata with both readers and compares the row sizes of the tables
		static void AssertSameRowSizes(uint[] rowCounts, byte heapFlags, uint[]? externalRowCounts, string what) {
			var metadata = CreateMetadata(rowCounts, heapFlags, externalRowCounts);
			bool isPdb = externalRowCounts is not null;

			using (var dnlibMetadata = ReadWithDnlib(metadata, isPdb))
			using (var provider = MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(metadata))) {
				var reader = provider.GetMetadataReader();
				foreach (var table in isPdb ? debugTables : typeSystemTables) {
					var mdTable = dnlibMetadata.TablesStream.Get(table)!;
					Assert.True(rowCounts[(int)table] == mdTable.Rows, $"{what}: the rows of {table}");
					int expected = reader.GetTableRowSize((TableIndex)(int)table);
					Assert.True((uint)expected == mdTable.RowSize, $"{what}: the row size of {table} is {expected}, dnlib has {mdTable.RowSize}");
					Assert.True(expected <= MaxRowSize, "MaxRowSize is too small");
				}
			}
		}

		// Every table has a row, so no column is bigger than it must be
		static uint[] OneRowIn(Table[] tables) {
			var rowCounts = new uint[TableCount];
			foreach (var table in tables)
				rowCounts[(int)table] = 1;
			return rowCounts;
		}

		[Fact]
		public void RowSizesOfTheTablesOfAModuleWhenOneTableGetsBig() {
			foreach (var table in typeSystemTables) {
				// System.Reflection.Metadata reads a module with exactly one Module row
				if (table == Table.Module)
					continue;
				foreach (uint rows in RowCountsAtTheLimits()) {
					var rowCounts = OneRowIn(typeSystemTables);
					rowCounts[(int)table] = rows;
					AssertSameRowSizes(rowCounts, 0, null, $"{table} with 0x{rows:X} rows");
				}
			}
		}

		// The debug tables are in a portable PDB file. It has no tables of the module, only how many rows they
		// have (in the #Pdb stream): the debug tables have columns with rows of these tables.
		[Fact]
		public void RowSizesOfTheDebugTablesWhenOneDebugTableGetsBig() {
			foreach (var table in debugTables) {
				foreach (uint rows in RowCountsAtTheLimits()) {
					var rowCounts = OneRowIn(debugTables);
					rowCounts[(int)table] = rows;
					AssertSameRowSizes(rowCounts, 0, OneRowIn(typeSystemTables), $"{table} with 0x{rows:X} rows");
				}
			}
		}

		[Fact]
		public void RowSizesOfTheDebugTablesWhenOneTableOfTheModuleGetsBig() {
			foreach (var table in typeSystemTables) {
				foreach (uint rows in RowCountsAtTheLimits()) {
					var externalRowCounts = OneRowIn(typeSystemTables);
					externalRowCounts[(int)table] = rows;
					AssertSameRowSizes(OneRowIn(debugTables), 0, externalRowCounts, $"{table} of the module with 0x{rows:X} rows");
				}
			}
		}

		// The flags of the header: 1 = the offsets in the #Strings heap have 4 bytes, 2 = #GUID, 4 = #Blob
		[Theory]
		[InlineData(0), InlineData(1), InlineData(2), InlineData(3), InlineData(4), InlineData(5), InlineData(6), InlineData(7)]
		public void RowSizesWithBigHeapOffsets(byte heapFlags) {
			AssertSameRowSizes(OneRowIn(typeSystemTables), heapFlags, null, "module");
			AssertSameRowSizes(OneRowIn(debugTables), heapFlags, OneRowIn(typeSystemTables), "PDB");
		}

		// All tables are big at once
		[Fact]
		public void RowSizesWhenAllTablesAreBig() {
			var rowCounts = OneRowIn(typeSystemTables);
			foreach (var table in typeSystemTables.Where(a => a != Table.Module))
				rowCounts[(int)table] = 0x10000;
			AssertSameRowSizes(rowCounts, 7, null, "module");

			var debugRowCounts = OneRowIn(debugTables);
			foreach (var table in debugTables)
				debugRowCounts[(int)table] = 0x10000;
			rowCounts[(int)Table.Module] = 0x10000;
			AssertSameRowSizes(debugRowCounts, 7, rowCounts, "PDB");
		}
	}
}
