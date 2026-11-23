// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.InvalidMetadata {
	/// <summary>
	/// dnlib must be able to read files with invalid metadata (eg. obfuscated assemblies). These tests set one
	/// metadata table cell of a valid assembly to an invalid value (a row that doesn't exist, an offset outside
	/// a heap, undefined flags), then read everything and write the module. Only these exceptions are allowed:
	/// <list type="bullet">
	/// <item><see cref="BadImageFormatException"/> when the file is opened</item>
	/// <item>none while the module is read</item>
	/// <item><see cref="ModuleWriterException"/> when it's written (the writer reports what it can't write)</item>
	/// </list>
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
							ReadAndWrite(image, $"{table} rid {rid} column {column}: 0x{original:X} -> 0x{value:X}");
							patches++;
						}
					}
				}
			}
			// Make sure the loops did something
			Assert.True(patches > 500, $"Only {patches} cells were patched");
		}

		static void ReadAndWrite(byte[] image, string patch) {
			ModuleDefMD module;
			try {
				module = ModuleDefMD.Load(image);
			}
			catch (BadImageFormatException) {
				return;
			}
			catch (Exception ex) {
				throw new InvalidOperationException($"Opening the file threw {ex.GetType().Name} (patch: {patch})", ex);
			}

			using (module) {
				try {
					// Reads everything dnlib can return
					ModuleDumper.Dump(module, new ModuleDumpOptions { MaxStack = true });
				}
				catch (Exception ex) {
					throw new InvalidOperationException($"Reading the module threw {ex.GetType().Name} (patch: {patch})", ex);
				}

				try {
					module.Write(new MemoryStream(), new ModuleWriterOptions(module));
				}
				catch (ModuleWriterException) {
				}
				catch (Exception ex) {
					throw new InvalidOperationException($"Writing the module threw {ex.GetType().Name} (patch: {patch})", ex);
				}
			}
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
