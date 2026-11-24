// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// Some tables are sorted by the row that their rows belong to (eg. the Constant table by the field,
	/// parameter or property that has the constant), so a reader finds a row with a binary search. In
	/// "uncompressed" metadata (the <c>#-</c> stream, see <see cref="UncompressedMetadataTests"/>) a table doesn't
	/// have to be sorted: the tables header has a bit for every table that says if it is. The tests reverse the
	/// rows of a table and clear its bit.
	/// </summary>
	public class UnsortedTableTests {
		static byte[] ValidImage => Fixtures.Fixtures.Features.Image;

		// The tables that are sorted in a normal file (the test input has no DeclSecurity rows)
		public static TheoryData<Table> SortedTables => new TheoryData<Table> {
			Table.InterfaceImpl, Table.Constant, Table.CustomAttribute, Table.FieldMarshal, Table.ClassLayout,
			Table.FieldLayout, Table.MethodSemantics, Table.MethodImpl, Table.ImplMap, Table.FieldRVA,
			Table.NestedClass, Table.GenericParam, Table.GenericParamConstraint,
		};

		// The same without GenericParam: other rows have the numbers of its rows (the constraints and the custom
		// attributes of a generic parameter), so the rows can't be moved without changing the module
		public static TheoryData<Table> TablesWhoseRowsCanBeMoved {
			get {
				var tables = new TheoryData<Table>();
				foreach (var table in SortedTables.Select(a => a.Data).Where(a => a != Table.GenericParam))
					tables.Add(table);
				return tables;
			}
		}

		static byte[] UncompressedImage => new MetadataPatcher(ValidImage).RenameStream("#~", "#-").ToArray();

		// Clears the bit of a table in the header of the tables stream that says that the table is sorted
		static byte[] ClearSortedBit(byte[] image, Table table) {
			var patcher = new MetadataPatcher(image);
			// The header is 24 bytes, the last 8 are the bits of the sorted tables
			long sortedBitsOffset = patcher.GetStreamFileOffset("#-") + 16;
			ulong sortedBits = BitConverter.ToUInt64(image, (int)sortedBitsOffset);
			Assert.True((sortedBits & (1UL << (int)table)) != 0, $"{table} isn't a sorted table");
			sortedBits &= ~(1UL << (int)table);
			return patcher.SetBytes(sortedBitsOffset, BitConverter.GetBytes(sortedBits)).ToArray();
		}

		static byte[] CreateImageWithReversedTable(Table table) {
			var image = UncompressedImage;
			var original = new MetadataPatcher(image);
			var patcher = new MetadataPatcher(image);
			uint rows = original.GetRowCount(table);
			Assert.True(rows >= 2, $"The test input has no {table} rows to reverse");
			foreach (var column in original.GetColumnNames(table)) {
				for (uint rid = 1; rid <= rows; rid++)
					patcher.SetCell(table, rid, column, original.GetCell(table, rows + 1 - rid, column));
			}
			return ClearSortedBit(patcher.ToArray(), table);
		}

		// The reader can't use a binary search anymore, but it finds the same rows
		[Theory, MemberData(nameof(SortedTables))]
		public void TableThatIsNotMarkedAsSortedIsRead(Table table) {
			var image = ClearSortedBit(UncompressedImage, table);

			using (var module = ModuleDefMD.Load(image)) {
				Assert.False(module.TablesStream.IsSorted(module.TablesStream.Get(table)!));
				using (var original = ModuleDefMD.Load(ValidImage))
					ModuleComparer.AssertEqual(original, module);
			}
			if (Type.GetType("Mono.Runtime") is null)
				Assert.Equal(0, AssemblyRunner.RunEntryPoint(image));
		}

		// The lines of a module's dump in alphabetical order
		static string[] GetSortedDumpLines(ModuleDef module) {
			var lines = ModuleDumper.Dump(module).Split('\n');
			Array.Sort(lines, StringComparer.Ordinal);
			return lines;
		}

		// It's the same module, but the interfaces, custom attributes, nested types etc of a type are in the
		// order of their rows, which is now the opposite order: the dumps have the same lines in another order.
		[Theory, MemberData(nameof(TablesWhoseRowsCanBeMoved))]
		public void TableWithItsRowsInTheOppositeOrderIsRead(Table table) {
			var image = CreateImageWithReversedTable(table);

			using (var module = ModuleDefMD.Load(image))
			using (var original = ModuleDefMD.Load(ValidImage))
				Assert.Equal(GetSortedDumpLines(original), GetSortedDumpLines(module));
		}

		// The CLR reads such a file too
		[Theory, MemberData(nameof(TablesWhoseRowsCanBeMoved))]
		public void ProgramWithATableInTheOppositeOrderRuns(Table table) {
			Assert.SkipWhen(Type.GetType("Mono.Runtime") is not null, "Mono doesn't support unsorted tables");

			Assert.Equal(0, AssemblyRunner.RunEntryPoint(CreateImageWithReversedTable(table)));
		}
	}
}
