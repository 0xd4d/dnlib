// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.InvalidMetadata {
	/// <summary>
	/// Sets one metadata table cell of a valid assembly to an invalid value (a row that doesn't exist, an offset
	/// outside a heap, undefined flags), then reads everything and writes the module.
	/// See <see cref="InvalidFileChecker"/> for what dnlib may do with such a file.
	/// </summary>
	public class PatchedTableCellTests {
		static byte[] ValidImage => Fixtures.Fixtures.Features.Image;

		// What a cell is set to: nothing/the first thing, the biggest value that fits, and values around the
		// places where "valid" usually ends (the number of rows, the sign bit)
		static IEnumerable<uint> GetInvalidValues(MetadataPatcher patcher, Table table, string column) {
			uint max = patcher.GetMaxCellValue(table, column);
			yield return 0;
			yield return 1;
			yield return max;
			yield return max - 1;
			yield return max / 2 + 1;
			yield return patcher.GetRowCount(table) + 1;
		}

		// Tests every column of every table, with the rows that are returned by getRows
		static void PatchCells(Func<uint, IEnumerable<uint>> getRows) {
			var patcher = new MetadataPatcher(ValidImage);
			int patches = 0;
			foreach (Table table in Enum.GetValues(typeof(Table))) {
				uint rows = patcher.GetRowCount(table);
				if (rows == 0)
					continue;
				foreach (var column in patcher.GetColumnNames(table)) {
					foreach (uint rid in getRows(rows)) {
						uint original = patcher.GetCell(table, rid, column);
						foreach (uint value in GetInvalidValues(patcher, table, column)) {
							if (value == original)
								continue;
							var image = new MetadataPatcher(ValidImage).SetCell(table, rid, column, value).ToArray();
							InvalidFileChecker.ReadAndWrite(ValidImage, image, $"{table} rid {rid} column {column}: 0x{original:X} -> 0x{value:X}");
							patches++;
						}
					}
				}
			}
			// Make sure the loops did something
			Assert.True(patches > 500, $"Only {patches} cells were patched");
		}

		[Fact]
		public void FirstAndLastRowOfEveryTable() =>
			PatchCells(rows => rows == 1 ? new uint[] { 1 } : new uint[] { 1, rows });

		[Fact, Trait(TestCategories.Name, TestCategories.Slow)]
		public void EveryRowOfEveryTable() =>
			PatchCells(rows => {
				var rids = new List<uint>();
				for (uint rid = 1; rid <= rows; rid++)
					rids.Add(rid);
				return rids;
			});
	}
}
