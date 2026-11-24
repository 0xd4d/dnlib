// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.Tests.InvalidMetadata;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// Normally the fields of a type are rows of the Field table that follow each other, in the order of the
	/// types (the same for methods and parameters). "Uncompressed" metadata (<c>#-</c>) can have pointer tables
	/// instead: the type has a range of rows in the FieldPtr table, and each of those rows has the number of a
	/// Field row. dnlib writes such metadata if the fields must keep their row numbers but the types have
	/// another order, and that's how the tests create it.
	/// </summary>
	public class PointerTableTests {
		// The fixture with its types in another order, and fields, methods and parameters that keep their rows
		static readonly Lazy<byte[]> imageWithPointerTables = new Lazy<byte[]>(() => {
			using (var module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image)) {
				MoveLastTypeToTheFront(module);
				var options = new ModuleWriterOptions(module);
				options.MetadataOptions.Flags |= MetadataFlags.PreserveFieldRids | MetadataFlags.PreserveMethodRids | MetadataFlags.PreserveParamRids;
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		});

		// Index 0 is the <Module> type, which must stay the first one
		static void MoveLastTypeToTheFront(ModuleDef module) {
			var last = module.Types[module.Types.Count - 1];
			module.Types.RemoveAt(module.Types.Count - 1);
			module.Types.Insert(1, last);
		}

		[Fact]
		public void WrittenFileHasPointerTables() {
			using (var module = ModuleDefMD.Load(imageWithPointerTables.Value)) {
				Assert.False(module.Metadata.IsCompressed);
				var tables = module.TablesStream;
				Assert.Equal(tables.FieldTable.Rows, tables.FieldPtrTable.Rows);
				Assert.Equal(tables.MethodTable.Rows, tables.MethodPtrTable.Rows);
				// The parameters are still in the order of their methods, so they need no pointer table
				Assert.Equal(0U, tables.ParamPtrTable.Rows);
				Assert.True(tables.FieldPtrTable.Rows > 10);
			}
		}

		[Fact]
		public void ModuleWithPointerTablesIsTheSameModule() {
			using (var expected = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image))
			using (var module = ModuleDefMD.Load(imageWithPointerTables.Value)) {
				MoveLastTypeToTheFront(expected);

				ModuleComparer.AssertEqual(expected, module);
			}
		}

		// The members kept their row numbers
		[Fact]
		public void MembersHaveTheirOriginalTokens() {
			using (var original = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image))
			using (var module = ModuleDefMD.Load(imageWithPointerTables.Value)) {
				var originalMethods = original.GetTypes().SelectMany(a => a.Methods).ToDictionary(a => a.FullName, a => a.MDToken);
				var originalFields = original.GetTypes().SelectMany(a => a.Fields).ToDictionary(a => a.FullName, a => a.MDToken);

				foreach (var method in module.GetTypes().SelectMany(a => a.Methods))
					Assert.Equal(originalMethods[method.FullName], method.MDToken);
				foreach (var field in module.GetTypes().SelectMany(a => a.Fields))
					Assert.Equal(originalFields[field.FullName], field.MDToken);
			}
		}

		[Fact]
		public void ModuleWithPointerTablesSurvivesAWrite() {
			var written = RoundTrip.CheckWithNewMetadata(imageWithPointerTables.Value);
			RoundTrip.CheckWithPreservedMetadata(imageWithPointerTables.Value);

			// With the default options, the writer creates normal metadata without pointer tables
			using (var module = ModuleDefMD.Load(written)) {
				Assert.True(module.Metadata.IsCompressed);
				Assert.Equal(0U, module.TablesStream.FieldPtrTable.Rows);
			}
		}

		// The CLR loads a file with pointer tables. Mono doesn't.
		[Fact]
		public void ProgramWithPointerTablesRuns() {
			Assert.SkipWhen(Type.GetType("Mono.Runtime") is not null, "Mono doesn't load metadata with pointer tables");

			Assert.Equal(0, AssemblyRunner.RunEntryPoint(imageWithPointerTables.Value));
		}

		// Invalid pointer tables: every cell gets the values 0, a row that exists and one that doesn't. A row
		// number that's used twice must not add a member to two types or twice to one type (fixed in 832c6d24).
		[Theory]
		[InlineData(Table.FieldPtr, "Field")]
		[InlineData(Table.MethodPtr, "Method")]
		public void InvalidPointerTableCells(Table table, string column) {
			var valid = imageWithPointerTables.Value;
			var patcher = new MetadataPatcher(valid);
			uint rows = patcher.GetRowCount(table);
			uint step = Math.Max(1, rows / 12);
			for (uint rid = 1; rid <= rows; rid += step) {
				foreach (uint value in new uint[] { 0, 1, rows, rows + 1, 0xFFFF }) {
					if (patcher.GetCell(table, rid, column) == value)
						continue;
					var image = new MetadataPatcher(valid).SetCell(table, rid, column, value).ToArray();
					InvalidFileChecker.ReadAndWrite(valid, image, $"{table} row {rid}: {column} = {value}");
				}
			}
		}

		[Fact]
		public void MemberThatTwoPointerRowsUseIsOnlyInOneType() {
			var valid = imageWithPointerTables.Value;
			var patcher = new MetadataPatcher(valid);
			// The last pointer row now points at the first method
			uint rows = patcher.GetRowCount(Table.MethodPtr);
			uint firstMethod = patcher.GetCell(Table.MethodPtr, 1, "Method");
			var image = patcher.SetCell(Table.MethodPtr, rows, "Method", firstMethod).ToArray();

			using (var module = ModuleDefMD.Load(image)) {
				var methods = module.GetTypes().SelectMany(a => a.Methods).ToList();
				Assert.Equal(methods.Count, methods.Distinct().Count());
				Assert.All(methods, a => Assert.Contains(a, a.DeclaringType!.Methods));
			}
		}
	}
}
