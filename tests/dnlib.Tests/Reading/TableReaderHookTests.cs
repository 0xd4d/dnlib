// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A program can have metadata tables in the file that aren't the ones it uses when it runs (it changes
	/// them in memory). A tool that knows the real values gives the <see cref="TablesStream"/> an
	/// <see cref="IRowReader{TRow}"/> for the rows of the Method table and an <see cref="IColumnReader"/>
	/// for single columns: they're asked before the file is read.
	/// </summary>
	public class TableReaderHookTests {
		const string Source = @"
public static class Program {
	static int One() => 1;
	static int Two() => 2;
	public static int Main() => One() == 2 && Two() == 1 ? 0 : 1;
}
public class Other {
	public int Field;
	public void Method() { }
}
";
		static readonly byte[] image = CSharpCompiler.Compile(Source, new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication }).Image;

		// Returns rows for some methods, and false for all other rows
		sealed class MethodRowReader : IRowReader<RawMethodRow> {
			public Dictionary<uint, RawMethodRow> Rows { get; } = new Dictionary<uint, RawMethodRow>();
			public List<uint> Calls { get; } = new List<uint>();

			public bool TryReadRow(uint rid, out RawMethodRow row) {
				Calls.Add(rid);
				return Rows.TryGetValue(rid, out row);
			}
		}

		// Returns values for some cells, and false for all others
		sealed class ColumnReader : IColumnReader {
			public Dictionary<(Table, uint, string), uint> Values { get; } = new Dictionary<(Table, uint, string), uint>();
			public HashSet<(Table, string)> ColumnsRead { get; } = new HashSet<(Table, string)>();

			public bool ReadColumn(MDTable table, uint rid, ColumnInfo column, out uint value) {
				ColumnsRead.Add((table.Table, column.Name));
				return Values.TryGetValue((table.Table, rid, column.Name), out value);
			}
		}

		static uint GetMethodRid(string name) {
			using (var module = ModuleDefMD.Load(image))
				return module.Find("Program", isReflectionName: true)!.FindMethod(name)!.Rid;
		}

		static RawMethodRow ReadMethodRowOfTheFile(uint rid) {
			using (var module = ModuleDefMD.Load(image)) {
				Assert.True(module.TablesStream.TryReadMethodRow(rid, out var row));
				return row;
			}
		}

		// The reader gives One() the row of Two() with its own name, and the other way round: the code is swapped
		static MethodRowReader CreateReaderThatSwapsTheCode() {
			uint one = GetMethodRid("One"), two = GetMethodRid("Two");
			var rowOfOne = ReadMethodRowOfTheFile(one);
			var rowOfTwo = ReadMethodRowOfTheFile(two);
			var reader = new MethodRowReader();
			reader.Rows[one] = new RawMethodRow(rowOfTwo.RVA, rowOfOne.ImplFlags, rowOfOne.Flags, rowOfOne.Name, rowOfOne.Signature, rowOfOne.ParamList);
			reader.Rows[two] = new RawMethodRow(rowOfOne.RVA, rowOfTwo.ImplFlags, rowOfTwo.Flags, rowOfTwo.Name, rowOfTwo.Signature, rowOfTwo.ParamList);
			return reader;
		}

		[Fact]
		public void MethodRowOfTheReaderIsUsed() {
			var reader = CreateReaderThatSwapsTheCode();
			using (var module = ModuleDefMD.Load(image)) {
				module.TablesStream.MethodRowReader = reader;
				Assert.Same(reader, module.TablesStream.MethodRowReader);
				var program = module.Find("Program", isReflectionName: true)!;

				Assert.Equal(2, program.FindMethod("One")!.Body!.Instructions[0].GetLdcI4Value());
				Assert.Equal(1, program.FindMethod("Two")!.Body!.Instructions[0].GetLdcI4Value());
				// The reader had no row for Main: the row of the file
				Assert.Equal(Code.Call, program.FindMethod("Main")!.Body!.Instructions[0].OpCode.Code);
				Assert.Contains(program.FindMethod("Main")!.Rid, reader.Calls);

				// The method that reads a raw row returns the reader's row too
				uint one = program.FindMethod("One")!.Rid;
				Assert.True(module.TablesStream.TryReadMethodRow(one, out var row));
				Assert.Equal(reader.Rows[one].RVA, row.RVA);
				// A row that doesn't exist isn't asked for
				reader.Calls.Clear();
				Assert.False(module.TablesStream.TryReadMethodRow(module.TablesStream.MethodTable.Rows + 1, out _));
				Assert.False(module.TablesStream.TryReadMethodRow(0, out _));
				Assert.Empty(reader.Calls);
			}
		}

		[Fact]
		public void WrittenProgramHasTheRowsOfTheReader() {
			Assert.Equal(1, AssemblyRunner.RunEntryPoint(image));

			var stream = new MemoryStream();
			using (var module = ModuleDefMD.Load(image)) {
				module.TablesStream.MethodRowReader = CreateReaderThatSwapsTheCode();
				module.Write(stream);
			}

			Assert.Equal(0, AssemblyRunner.RunEntryPoint(stream.ToArray()));
		}

		[Fact]
		public void ColumnReaderThatHasNoValuesChangesNothing() {
			var reader = new ColumnReader();
			using (var expected = ModuleDefMD.Load(image))
			using (var module = ModuleDefMD.Load(image)) {
				module.TablesStream.ColumnReader = reader;
				Assert.Same(reader, module.TablesStream.ColumnReader);

				ModuleComparer.AssertEqual(expected, module);

				// It's asked for the columns that say where the members of a type and the parameters of a
				// method start
				Assert.Contains((Table.TypeDef, "FieldList"), reader.ColumnsRead);
				Assert.Contains((Table.TypeDef, "MethodList"), reader.ColumnsRead);
				Assert.Contains((Table.Method, "ParamList"), reader.ColumnsRead);
			}
		}

		// The methods of a type are the rows from its MethodList up to the MethodList of the next type. If
		// the reader says that the methods of Program start where the methods of the next type start,
		// Program has no methods: they're methods of the type before it.
		[Fact]
		public void ColumnOfTheReaderIsUsed() {
			using (var module = ModuleDefMD.Load(image)) {
				var tables = module.TablesStream;
				uint program = Enumerable.Range(1, (int)tables.TypeDefTable.Rows).Select(a => (uint)a).Single(rid => {
					Assert.True(tables.TryReadTypeDefRow(rid, out var row));
					return module.StringsStream.ReadNoNull(row.Name) == "Program";
				});
				Assert.True(tables.TryReadTypeDefRow(program + 1, out var nextType));
				var reader = new ColumnReader();
				reader.Values[(Table.TypeDef, program, "MethodList")] = nextType.MethodList;
				tables.ColumnReader = reader;

				Assert.Empty(module.Find("Program", isReflectionName: true)!.Methods);
				var typeBeforeProgram = module.ResolveTypeDef(program - 1)!;
				Assert.Equal(new[] { "One", "Two", "Main" }, typeBeforeProgram.Methods.Select(a => a.Name.String).Reverse().Take(3).Reverse().ToArray());
				Assert.Equal(new[] { "Method", ".ctor" }, module.Find("Other", isReflectionName: true)!.Methods.Select(a => a.Name.String).ToArray());

				// Reading the column returns the reader's value, reading the whole row returns the row of the file
				var methodList = tables.TypeDefTable.Columns.Single(a => a.Name == "MethodList");
				Assert.True(tables.TryReadColumn(tables.TypeDefTable, program, methodList, out uint value));
				Assert.Equal(nextType.MethodList, value);
				Assert.True(tables.TryReadColumn(tables.TypeDefTable, program, methodList.Index, out value));
				Assert.Equal(nextType.MethodList, value);
				Assert.True(tables.TryReadTypeDefRow(program, out var rowOfTheFile));
				Assert.NotEqual(nextType.MethodList, rowOfTheFile.MethodList);
				// A row that doesn't exist isn't asked for
				Assert.False(tables.TryReadColumn(tables.TypeDefTable, tables.TypeDefTable.Rows + 1, methodList, out value));
				Assert.Equal(0U, value);
			}
		}
	}
}
