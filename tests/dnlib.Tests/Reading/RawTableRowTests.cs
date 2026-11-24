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

		[Theory, MemberData(nameof(TablesWithRows))]
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
	}
}
