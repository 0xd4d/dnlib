// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// <see cref="TablesStream"/> has a method for every metadata table that reads a row into a struct
	/// (<c>TryReadTypeDefRow()</c> returns a <see cref="RawTypeDefRow"/>, and so on), and one method that reads any
	/// column of any table (<c>TryReadColumn()</c>). Both must return the same values. The methods and structs
	/// are found with reflection: there are 53 of them.
	/// </summary>
	public class RawTableRowTests {
		static readonly ModuleDefMD module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image);

		static MethodInfo? GetRowReader(Table table) => typeof(TablesStream).GetMethod($"TryRead{table}Row");

		public static TheoryData<Table> TablesWithRows {
			get {
				var tables = new TheoryData<Table>();
				foreach (var mdTable in module.TablesStream.MDTables) {
					if (mdTable.Rows != 0)
						tables.Add(mdTable.Table);
				}
				return tables;
			}
		}

		// Reads a row with the table's own method. Returns null if the method returns false.
		static object? ReadRow(Table table, uint rid) {
			var reader = GetRowReader(table);
			Assert.True(reader is not null, $"TablesStream has no method for table {table}");
			var arguments = new object?[] { rid, null };
			bool wasRead = (bool)reader.Invoke(module.TablesStream, arguments)!;
			return wasRead ? arguments[1] : null;
		}

		// The value of a column of a row struct, through the struct's indexer
		static uint GetColumn(object row, int index) =>
			(uint)row.GetType().GetProperty("Item")!.GetValue(row, new object[] { index })!;

		[Theory, MemberData(nameof(TablesWithRows))]
		public void RowStructHasTheValuesOfTheColumns(Table table) {
			var mdTable = module.TablesStream.Get(table)!;
			for (uint rid = 1; rid <= mdTable.Rows; rid++) {
				var row = ReadRow(table, rid);

				Assert.NotNull(row);
				for (int column = 0; column < mdTable.Columns.Count; column++) {
					Assert.True(module.TablesStream.TryReadColumn(mdTable, rid, column, out uint expected));
					Assert.True(expected == GetColumn(row, column), $"{table} row {rid}, column {mdTable.Columns[column].Name}");
				}
				// There's no column after the last one
				Assert.Equal(0U, GetColumn(row, mdTable.Columns.Count));
				Assert.Equal(0U, GetColumn(row, -1));
			}
		}

		public static TheoryData<Table> AllTables {
			get {
				var tables = new TheoryData<Table>();
				foreach (var mdTable in module.TablesStream.MDTables) {
					// Not every table number is a table
					if (GetRowReader(mdTable.Table) is not null)
						tables.Add(mdTable.Table);
				}
				return tables;
			}
		}

		// Also in a table without rows (eg. a table of a portable PDB)
		[Theory, MemberData(nameof(AllTables))]
		public void RowThatDoesNotExistIsNotRead(Table table) {
			var mdTable = module.TablesStream.Get(table)!;

			Assert.Null(ReadRow(table, 0));
			Assert.Null(ReadRow(table, mdTable.Rows + 1));
			Assert.Null(ReadRow(table, uint.MaxValue));
		}

		// A row struct's fields are its columns in the order of the table, and its constructor has one parameter
		// for every column
		[Fact]
		public void EveryTableHasARowStructWithAFieldForEveryColumn() {
			int count = 0;
			foreach (var mdTable in module.TablesStream.MDTables) {
				// The numbers 0x2D - 0x2F aren't tables
				if (!Enum.IsDefined(typeof(Table), mdTable.Table))
					continue;
				var reader = GetRowReader(mdTable.Table);
				Assert.True(reader is not null, $"TablesStream has no method for table {mdTable.Table}");
				var rowType = reader.GetParameters()[1].ParameterType.GetElementType()!;
				// GenericParam has a second constructor for the extra column that .NET 1.1 beta files have
				var constructor = rowType.GetConstructors().Single(a => a.GetParameters().Length == mdTable.Columns.Count);
				var parameters = constructor.GetParameters();
				Assert.Equal(mdTable.Columns.Count, parameters.Length);

				// Create a row whose column values are 1, 2, 3, ... and read them through the indexer
				var arguments = parameters.Select((a, index) => Convert.ChangeType(index + 1, a.ParameterType)).ToArray();
				var row = constructor.Invoke(arguments);
				for (int column = 0; column < parameters.Length; column++) {
					Assert.Equal((uint)column + 1, GetColumn(row, column));
					Assert.Equal(mdTable.Columns[column].Name, parameters[column].Name);
				}
				// A column that the table doesn't have is 0
				Assert.Equal(0U, GetColumn(row, parameters.Length));
				Assert.Equal(0U, GetColumn(row, -1));
				count++;
			}
			Assert.True(count >= 53, $"Only {count} tables were checked");
		}

		// RawRowEqualityComparer compares the rows of every table (the writer uses it to find a row that it
		// already has). For every row struct: two rows with the same column values are equal and have the
		// same hash code, and a row with another value in any one column isn't equal.
		[Fact]
		public void RowsAreEqualIfAllTheirColumnsAreEqual() {
			var comparer = new RawRowEqualityComparer();
			int count = 0;
			foreach (var comparerInterface in typeof(RawRowEqualityComparer).GetInterfaces()) {
				// IEqualityComparer<RawTypeDefRow> etc
				var rowType = comparerInterface.GetGenericArguments()[0];
				var equals = comparerInterface.GetMethod("Equals")!;
				var getHashCode = comparerInterface.GetMethod("GetHashCode")!;
				// The constructor with a parameter for every column (GenericParam has a second one)
				var constructor = rowType.GetConstructors().OrderByDescending(a => a.GetParameters().Length).First();
				var parameters = constructor.GetParameters();

				object CreateRow(int changedColumn) {
					var arguments = parameters.Select((a, index) => Convert.ChangeType(index + 1 + (index == changedColumn ? 100 : 0), a.ParameterType)).ToArray();
					return constructor.Invoke(arguments);
				}

				var row = CreateRow(changedColumn: -1);
				var sameRow = CreateRow(changedColumn: -1);
				Assert.True((bool)equals.Invoke(comparer, new[] { row, sameRow })!, rowType.Name);
				Assert.Equal(getHashCode.Invoke(comparer, new[] { row }), getHashCode.Invoke(comparer, new[] { sameRow }));
				for (int column = 0; column < parameters.Length; column++) {
					var otherRow = CreateRow(column);
					Assert.False((bool)equals.Invoke(comparer, new[] { row, otherRow })!, $"{rowType.Name}.{parameters[column].Name}");
					Assert.False((bool)equals.Invoke(comparer, new[] { otherRow, row })!, $"{rowType.Name}.{parameters[column].Name}");
				}
				count++;
			}
			Assert.True(count >= 53, $"Only {count} row structs were checked");
		}

		// Six tables that no compiler writes rows to: the two edit-and-continue tables, and four tables for
		// the processors and operating systems of an assembly that the runtime ignores. A listener of the
		// writer's events can add rows to any table, which is how the test gets a file that has them.
		[Fact]
		public void RowsOfTablesThatCompilersDoNotUseAreRead() {
			byte[] image;
			using (var source = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image)) {
				var options = new dnlib.DotNet.Writer.ModuleWriterOptions(source);
				options.WriterEvent += (sender, e) => {
					if (e.Event != dnlib.DotNet.Writer.ModuleWriterEvent.MDEndCreateTables)
						return;
					var tables = e.Writer.Metadata.TablesHeap;
					tables.ENCLogTable.Create(new RawENCLogRow(0x06000001, 1));
					tables.ENCLogTable.Create(new RawENCLogRow(0x02000002, 0));
					tables.ENCMapTable.Create(new RawENCMapRow(0x06000001));
					tables.AssemblyProcessorTable.Create(new RawAssemblyProcessorRow(0x8664));
					tables.AssemblyOSTable.Create(new RawAssemblyOSRow(2, 10, 1));
					tables.AssemblyRefProcessorTable.Create(new RawAssemblyRefProcessorRow(0x14C, 1));
					tables.AssemblyRefOSTable.Create(new RawAssemblyRefOSRow(2, 6, 3, 1));
				};
				var stream = new System.IO.MemoryStream();
				source.Write(stream, options);
				image = stream.ToArray();
			}

			using (var written = ModuleDefMD.Load(image)) {
				var tables = written.TablesStream;
				Assert.Equal(2U, tables.ENCLogTable.Rows);
				Assert.True(tables.TryReadENCLogRow(2, out var encLog));
				Assert.Equal(0x02000002U, encLog.Token);
				Assert.Equal(0U, encLog.FuncCode);
				Assert.True(tables.TryReadENCMapRow(1, out var encMap));
				Assert.Equal(0x06000001U, encMap.Token);
				Assert.True(tables.TryReadAssemblyProcessorRow(1, out var processor));
				Assert.Equal(0x8664U, processor.Processor);
				Assert.True(tables.TryReadAssemblyOSRow(1, out var os));
				Assert.Equal(new uint[] { 2, 10, 1 }, new[] { os.OSPlatformId, os.OSMajorVersion, os.OSMinorVersion });
				Assert.True(tables.TryReadAssemblyRefProcessorRow(1, out var refProcessor));
				Assert.Equal(new uint[] { 0x14C, 1 }, new[] { refProcessor.Processor, refProcessor.AssemblyRef });
				Assert.True(tables.TryReadAssemblyRefOSRow(1, out var refOS));
				Assert.Equal(new uint[] { 2, 6, 3, 1 }, new[] { refOS.OSPlatformId, refOS.OSMajorVersion, refOS.OSMinorVersion, refOS.AssemblyRef });
				Assert.False(tables.TryReadAssemblyOSRow(2, out _));

				// The rows don't change what the module is
				using (var original = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image))
					dnlib.TestUtils.Dumping.ModuleComparer.AssertEqual(original, written);
			}
		}
	}
}
