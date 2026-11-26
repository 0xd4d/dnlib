// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The initial value of a static field is in the file: the field has the HasFieldRVA flag and a row in
	/// the FieldRVA table with the address (RVA) of the data. A compiler writes that for the elements of
	/// an array. These tests write rows that a compiler doesn't write and read the data with dnlib and with
	/// the runtime that runs the tests (which creates the array from it). If the runtime reads data, dnlib
	/// must read the same data.
	/// </summary>
	public class ClrFieldRvaRuleTests {
		const string Source = @"
public class C {
	public static readonly int[] A = new int[] { 0x11, 0x12, 0x13, 0x14 };
	public static readonly int[] B = new int[] { 0x21, 0x22, 0x23, 0x24 };
}";
		static readonly byte[] baseImage = CSharpCompiler.Compile(Source).Image;

		static string Describe(byte[]? data) => data is null ? "null" :
			string.Join(" ", Enumerable.Range(0, data.Length / 4).Select(a => LittleEndian.ToInt32(data, a * 4).ToString("X")));

		// The compiler's fields with the data of A and B
		static FieldDef[] GetDataFields(ModuleDefMD module) {
			var fields = module.GetTypes().SelectMany(a => a.Fields).Where(a => a.HasFieldRVA).ToArray();
			Assert.Equal(2, fields.Length);
			return Describe(fields[0].InitialValue) == "11 12 13 14" ? fields : new[] { fields[1], fields[0] };
		}

		// "What = value", separated by "; ". 'What' is "RVA" or "Field" (the columns of the FieldRVA row of
		// A's data) or "Flags" (of the field with A's data). A value: a number, "+N" (the original value
		// plus N), "B" (the value that B has there) or "-rva" (the flags without HasFieldRVA).
		static byte[] CreateImage(string changes) {
			uint fieldA, fieldB;
			using (var module = ModuleDefMD.Load(baseImage)) {
				var fields = GetDataFields(module);
				fieldA = fields[0].Rid;
				fieldB = fields[1].Rid;
			}
			var patcher = new MetadataPatcher(baseImage);
			uint rowA = Enumerable.Range(1, 2).Select(a => (uint)a).Single(a => patcher.GetCell(Table.FieldRVA, a, "Field") == fieldA);
			uint rowB = 3 - rowA;
			foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
				var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
				string what = parts[0], value = parts[1];
				if (what == "Flags") {
					uint flags = patcher.GetCell(Table.Field, fieldA, "Flags");
					patcher.SetCell(Table.Field, fieldA, "Flags", value == "-rva" ? flags & ~(uint)FieldAttributes.HasFieldRVA : uint.Parse(value));
					continue;
				}
				uint original = patcher.GetCell(Table.FieldRVA, rowA, what);
				uint newValue = value == "B" ? patcher.GetCell(Table.FieldRVA, rowB, what) :
					value.StartsWith("+") ? original + uint.Parse(value.Substring(1)) : uint.Parse(value);
				patcher.SetCell(Table.FieldRVA, rowA, what, newValue);
			}
			return patcher.ToArray();
		}

		[Theory]
		[InlineData("", "11 12 13 14 / 21 22 23 24", "11 12 13 14 / 21 22 23 24")]
		[InlineData("RVA = B", "21 22 23 24 / 21 22 23 24", "21 22 23 24 / 21 22 23 24")]
		[InlineData("RVA = +4", "12 13 14 0 / 21 22 23 24", "12 13 14 0 / 21 22 23 24")]
		[InlineData("RVA = +1", "12000000 13000000 14000000 0 / 21 22 23 24", "12000000 13000000 14000000 0 / 21 22 23 24")]
		// An address in the headers of the file: the data is the start of the file ("MZ")
		[InlineData("RVA = 0", "905A4D 3 4 FFFF / 21 22 23 24", "905A4D 3 4 FFFF / 21 22 23 24")]
		[InlineData("RVA = 1", "300905A 4000000 FF000000 B80000FF / 21 22 23 24", "300905A 4000000 FF000000 B80000FF / 21 22 23 24")]
		// An address that isn't in the file: dnlib has no data. .NET 10 reads the memory at that address, and
		// the process stops with a fatal error if there is none, so the runtime isn't asked.
		[InlineData("RVA = 1048576", "null / 21 22 23 24", "not asked")]
		[InlineData("RVA = 2147483648", "null / 21 22 23 24", "not asked")]
		[InlineData("RVA = 4294967295", "null / 21 22 23 24", "not asked")]
		[InlineData("RVA = 4294967280", "null / 21 22 23 24", "not asked")]
		// The field of the row
		[InlineData("Field = B", "null / 21 22 23 24", "throws TypeInitializationException")]
		[InlineData("Field = B; RVA = B", "null / 21 22 23 24", "throws TypeInitializationException")]
		[InlineData("Field = 0", "null / 21 22 23 24", "throws TypeInitializationException")]
		[InlineData("Field = 1000", "null / 21 22 23 24", "throws TypeInitializationException")]
		// The flag of the field
		[InlineData("Flags = -rva", "null / 21 22 23 24", "throws TypeInitializationException")]
		[InlineData("Flags = -rva; RVA = 0", "null / 21 22 23 24", "throws TypeInitializationException")]
		public void InitialValueOfAField(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			string dnlib;
			using (var module = ModuleDefMD.Load(baseImage)) {
				var originalFields = GetDataFields(module);
				using (var patched = ModuleDefMD.Load(image)) {
					var a = patched.ResolveField(originalFields[0].Rid)!;
					var b = patched.ResolveField(originalFields[1].Rid)!;
					dnlib = Describe(a.InitialValue) + " / " + Describe(b.InitialValue);
				}
			}

			Func<string> readWithTheRuntime = () => {
				using (var loaded = new LoadedAssembly(image)) {
					var type = loaded.Assembly.GetType("C")!;
					var a = (int[])type.GetField("A")!.GetValue(null)!;
					var b = (int[])type.GetField("B")!.GetValue(null)!;
					return string.Join(" ", a.Select(x => x.ToString("X"))) + " / " + string.Join(" ", b.Select(x => x.ToString("X")));
				}
			};
			// .NET reads the memory at the address also if it isn't in the file: a fatal error stops the process if
			// there's no memory, else the field has the values that are there
			if (expectedRuntime == "not asked") {
				Assert.Equal(expectedDnlib, dnlib);
				ClrReaderComparison.AssertRuntimeStopsTheProcess(stopsNetFramework: false, readWithTheRuntime, orReadsOtherMemory: true);
				return;
			}
			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, readWithTheRuntime);
		}
	}
}
