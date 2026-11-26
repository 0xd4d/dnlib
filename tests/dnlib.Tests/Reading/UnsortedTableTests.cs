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
			ulong sortedBits = LittleEndian.ToUInt64(image, (int)sortedBitsOffset);
			Assert.True((sortedBits & (1UL << (int)table)) != 0, $"{table} isn't a sorted table");
			sortedBits &= ~(1UL << (int)table);
			return patcher.SetBytes(sortedBitsOffset, LittleEndian.GetBytes(sortedBits)).ToArray();
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

		// Mono ignores the bits: it always does a binary search. dnlib can read a file like Mono does, and finds
		// the same rows if the table is in fact sorted.
		[Theory, MemberData(nameof(SortedTables))]
		public void TableThatIsNotMarkedAsSortedIsReadLikeMonoReadsIt(Table table) {
			var image = ClearSortedBit(UncompressedImage, table);

			using (var module = ModuleDefMD.Load(image, new ModuleCreationOptions { Runtime = CLRRuntimeReaderKind.Mono }))
			using (var original = ModuleDefMD.Load(ValidImage, new ModuleCreationOptions { Runtime = CLRRuntimeReaderKind.Mono }))
				ModuleComparer.AssertEqual(original, module);
		}

		// A binary search in a table that isn't sorted doesn't find every row: the module isn't the same, but
		// reading everything works
		[Theory, MemberData(nameof(TablesWhoseRowsCanBeMoved))]
		public void TableWithItsRowsInTheOppositeOrderCanBeReadLikeMonoReadsIt(Table table) {
			var image = CreateImageWithReversedTable(table);

			using (var module = ModuleDefMD.Load(image, new ModuleCreationOptions { Runtime = CLRRuntimeReaderKind.Mono }))
				Assert.NotEmpty(ModuleDumper.Dump(module));
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

		// A class with 'count' constants: Constants.C0 = 100, C1 = 101, ...
		static byte[] CompileConstants(int count) {
			var fields = string.Join(" ", Enumerable.Range(0, count).Select(i => $"public const int C{i} = {100 + i};"));
			return CSharpCompiler.Compile("public class Constants { " + fields + " }").Image;
		}

		// An unsorted table can have two rows for the same owner, here two values of the constant C0: its own row
		// (the first one) and the row of the last constant. The CLR uses the first one if the table has less than
		// 25 rows. With more rows it creates a hash table, and finds the last one. The runtime's reflection tells
		// the tests which one it is.
		[Theory]
		[InlineData(24, 100)]
		[InlineData(25, 124)]
		[InlineData(60, 159)]
		public void RowThatTheClrFindsIsUsedIfTwoRowsHaveTheSameOwner(int constantCount, int expectedValue) {
			var image = new MetadataPatcher(CompileConstants(constantCount)).RenameStream("#~", "#-").ToArray();
			var patcher = new MetadataPatcher(image);
			Assert.Equal((uint)constantCount, patcher.GetRowCount(Table.Constant));
			patcher.SetCell(Table.Constant, (uint)constantCount, "Parent", patcher.GetCell(Table.Constant, 1, "Parent"));
			image = ClearSortedBit(patcher.ToArray(), Table.Constant);

			using (var module = ModuleDefMD.Load(image)) {
				var constants = module.Find("Constants", isReflectionName: true)!;
				Assert.Equal(expectedValue, constants.FindField("C0")!.Constant!.Value);
				Assert.Equal(101, constants.FindField("C1")!.Constant!.Value);
				// Its row is now a row of C0
				Assert.Null(constants.FindField("C" + (constantCount - 1))!.Constant);
			}

			if (Type.GetType("Mono.Runtime") is null) {
				using (var loaded = new LoadedAssembly(image))
					Assert.Equal(expectedValue, loaded.Assembly.GetType("Constants")!.GetField("C0")!.GetRawConstantValue());
			}
		}
	}
}
