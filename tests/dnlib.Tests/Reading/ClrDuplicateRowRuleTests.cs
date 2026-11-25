// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// Some tables have at most one row for an owner (a field has one constant, a type has one layout) and
	/// are sorted by the owner, so a reader finds the row with a binary search. If two or more rows have the
	/// same owner, the row that is found depends on the search. These tests give some rows the same owner
	/// and read the value with dnlib and with the runtime that runs the tests: dnlib must find the row that
	/// the CLR finds.
	/// </summary>
	public class ClrDuplicateRowRuleTests {
		// A class with constants F1 = 1, F2 = 2, ... The Constant table then has a row for every field, in
		// the order of the fields.
		static byte[] CreateImageWithConstants(int count) {
			var fields = string.Join("\n", Enumerable.Range(1, count).Select(a => $"	public const int F{a} = {a};"));
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile("public static class Marker {\n" + fields + "\n}").Image)) {
				// Only this class: the compiler can add other types with constants
				foreach (var type in module.Types.Where(a => !a.IsGlobalModuleType && a.Name != "Marker").ToArray())
					module.Types.Remove(type);
				module.CustomAttributes.Clear();
				var stream = new MemoryStream();
				module.Write(stream);
				return stream.ToArray();
			}
		}

		static string ReadConstantsWithDnlib(byte[] image, int count) {
			using (var module = ModuleDefMD.Load(image)) {
				var type = module.Find("Marker", isReflectionName: true)!;
				return string.Join(" ", Enumerable.Range(1, count).Select(a => type.FindField("F" + a)!.Constant?.Value?.ToString() ?? "-"));
			}
		}

		static string ReadConstantsWithTheRuntime(byte[] image, int count) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var type = loaded.Assembly.GetType("Marker", throwOnError: true)!;
					return string.Join(" ", Enumerable.Range(1, count).Select(a => {
						try {
							return type.GetField("F" + a)!.GetRawConstantValue()?.ToString() ?? "null";
						}
						catch (Exception) {
							return "-";
						}
					}));
				}
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		// The rows first..last of the table get the owner of row 'first'. What is read is the value of every
		// field or type ("-": it has no row).
		public static IEnumerable<object[]> Cases() {
			foreach (int count in new[] { 2, 3, 4, 5, 6, 7, 8, 9, 15, 16, 17 }) {
				for (int first = 1; first < count; first++) {
					for (int last = first + 1; last <= count && last <= first + 3; last++)
						yield return new object[] { count, first, last };
				}
			}
		}

		[Theory]
		[MemberData(nameof(Cases))]
		public void ConstantOfAFieldWithMoreThanOneRow(int count, int first, int last) {
			var patcher = new MetadataPatcher(CreateImageWithConstants(count));
			Assert.Equal((uint)count, patcher.GetRowCount(Table.Constant));
			uint owner = patcher.GetCell(Table.Constant, (uint)first, "Parent");
			for (int rid = first + 1; rid <= last; rid++)
				patcher.SetCell(Table.Constant, (uint)rid, "Parent", owner);
			var image = patcher.ToArray();

			ClrReaderComparison.AssertSameAsTheRuntime(ReadConstantsWithDnlib(image, count), () => ReadConstantsWithTheRuntime(image, count));
		}

		// Structs S1, S2, ... with the sizes 1, 2, ... The ClassLayout table then has a row for every struct.
		static byte[] CreateImageWithLayouts(int count) {
			var structs = string.Join("\n", Enumerable.Range(1, count).Select(a => $"[StructLayout(LayoutKind.Sequential, Size = {a})] public struct S{a} {{ }}"));
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile("using System.Runtime.InteropServices;\n" + structs).Image)) {
				foreach (var type in module.Types.Where(a => !a.IsGlobalModuleType && !a.Name.StartsWith("S")).ToArray())
					module.Types.Remove(type);
				module.CustomAttributes.Clear();
				var stream = new MemoryStream();
				module.Write(stream);
				return stream.ToArray();
			}
		}

		static string ReadLayoutsWithDnlib(byte[] image, int count) {
			using (var module = ModuleDefMD.Load(image)) {
				return string.Join(" ", Enumerable.Range(1, count).Select(a => {
					var type = module.Find("S" + a, isReflectionName: true)!;
					return type.ClassLayout is null ? "-" : type.ClassLayout.ClassSize.ToString();
				}));
			}
		}

		static string ReadLayoutsWithTheRuntime(byte[] image, int count) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					return string.Join(" ", Enumerable.Range(1, count).Select(a => {
						// A struct without a row has the size 0
						int size = loaded.Assembly.GetType("S" + a, throwOnError: true)!.StructLayoutAttribute!.Size;
						return size == 0 ? "-" : size.ToString();
					}));
				}
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		[Theory]
		[MemberData(nameof(Cases))]
		public void LayoutOfATypeWithMoreThanOneRow(int count, int first, int last) {
			var patcher = new MetadataPatcher(CreateImageWithLayouts(count));
			Assert.Equal((uint)count, patcher.GetRowCount(Table.ClassLayout));
			uint owner = patcher.GetCell(Table.ClassLayout, (uint)first, "Parent");
			for (int rid = first + 1; rid <= last; rid++)
				patcher.SetCell(Table.ClassLayout, (uint)rid, "Parent", owner);
			var image = patcher.ToArray();

			ClrReaderComparison.AssertSameAsTheRuntime(ReadLayoutsWithDnlib(image, count), () => ReadLayoutsWithTheRuntime(image, count));
		}

		// Only the types of the source code: the compiler can add other types
		static byte[] CompileOnly(string source, Func<string, bool> isTypeOfTheSource) {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(source).Image)) {
				foreach (var type in module.Types.Where(a => !a.IsGlobalModuleType && !isTypeOfTheSource(a.Name)).ToArray())
					module.Types.Remove(type);
				module.CustomAttributes.Clear();
				module.Assembly!.CustomAttributes.Clear();
				var stream = new MemoryStream();
				module.Write(stream);
				return stream.ToArray();
			}
		}

		// Gives the rows first..last of the table the owner of row 'first'
		static byte[] SetSameOwner(byte[] image, Table table, string ownerColumn, int count, int first, int last) {
			var patcher = new MetadataPatcher(image);
			Assert.Equal((uint)count, patcher.GetRowCount(table));
			uint owner = patcher.GetCell(table, (uint)first, ownerColumn);
			for (int rid = first + 1; rid <= last; rid++)
				patcher.SetCell(table, (uint)rid, ownerColumn, owner);
			return patcher.ToArray();
		}

		// Fields F1, F2, ... that are marshalled as the native types 2, 3, ... The FieldMarshal table then
		// has a row for every field.
		[Theory]
		[MemberData(nameof(Cases))]
		public void NativeTypeOfAFieldWithMoreThanOneRow(int count, int first, int last) {
			// Native types that have no other values in the table
			var nativeTypes = new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 15, 19, 20, 21, 22, 25, 26, 28, 31 };
			var fields = string.Join("\n", Enumerable.Range(1, count).Select(a => $"	[MarshalAs((UnmanagedType){nativeTypes[a - 1]})] public int F{a};"));
			var image = CompileOnly("using System.Runtime.InteropServices;\npublic class Marker {\n" + fields + "\n}", name => name == "Marker");
			image = SetSameOwner(image, Table.FieldMarshal, "Parent", count, first, last);

			string dnlib;
			using (var module = ModuleDefMD.Load(image)) {
				var type = module.Find("Marker", isReflectionName: true)!;
				dnlib = string.Join(" ", Enumerable.Range(1, count).Select(a => type.FindField("F" + a)!.MarshalType is MarshalType marshalType ? ((int)marshalType.NativeType).ToString() : "-"));
			}

			ClrReaderComparison.AssertSameAsTheRuntime(dnlib, () => {
				using (var loaded = new LoadedAssembly(image)) {
					var type = loaded.Assembly.GetType("Marker", throwOnError: true)!;
					return string.Join(" ", Enumerable.Range(1, count).Select(a => {
						var attribute = (System.Runtime.InteropServices.MarshalAsAttribute?)type.GetField("F" + a)!.GetCustomAttributes(typeof(System.Runtime.InteropServices.MarshalAsAttribute), false).FirstOrDefault();
						return attribute is null ? "-" : ((int)attribute.Value).ToString();
					}));
				}
			});
		}

		// Methods M1, M2, ... of a native library with the entry points E1, E2, ... The ImplMap table then
		// has a row for every method.
		[Theory]
		[MemberData(nameof(Cases))]
		public void EntryPointOfAMethodWithMoreThanOneRow(int count, int first, int last) {
			var methods = string.Join("\n", Enumerable.Range(1, count).Select(a => $"	[DllImport(\"lib\", EntryPoint = \"E{a}\")] public static extern void M{a}();"));
			var image = CompileOnly("using System.Runtime.InteropServices;\npublic class Marker {\n" + methods + "\n}", name => name == "Marker");
			image = SetSameOwner(image, Table.ImplMap, "MemberForwarded", count, first, last);

			string dnlib;
			using (var module = ModuleDefMD.Load(image)) {
				var type = module.Find("Marker", isReflectionName: true)!;
				dnlib = string.Join(" ", Enumerable.Range(1, count).Select(a => type.FindMethod("M" + a)!.ImplMap?.Name.String ?? "-"));
			}

			ClrReaderComparison.AssertSameAsTheRuntime(dnlib, () => {
				using (var loaded = new LoadedAssembly(image)) {
					var type = loaded.Assembly.GetType("Marker", throwOnError: true)!;
					return string.Join(" ", Enumerable.Range(1, count).Select(a => {
						var attribute = (System.Runtime.InteropServices.DllImportAttribute?)type.GetMethod("M" + a)!.GetCustomAttributes(typeof(System.Runtime.InteropServices.DllImportAttribute), false).FirstOrDefault();
						// A method without a row has an empty entry point
						return string.IsNullOrEmpty(attribute?.EntryPoint) ? "-" : attribute!.EntryPoint!;
					}));
				}
			});
		}

		// Types T1, T2 and T3 with three attributes each: the CustomAttribute table has 9 rows, sorted by
		// the type. A reader finds one row of a type with a binary search and then the rows before and
		// after it with the same owner.
		static readonly byte[] attributeImage = CompileOnly(@"
using System;
public class A1 : Attribute { } public class A2 : Attribute { } public class A3 : Attribute { }
public class A4 : Attribute { } public class A5 : Attribute { } public class A6 : Attribute { }
public class A7 : Attribute { } public class A8 : Attribute { } public class A9 : Attribute { }
[A1, A2, A3] public class T1 { }
[A4, A5, A6] public class T2 { }
[A7, A8, A9] public class T3 { }
", name => name.Length == 2);

		// Row 'row' gets the owner of row 'ownerOfRow' (0: no owner), and so does row 'secondRow' if it isn't 0
		public static IEnumerable<object[]> AttributeCases() {
			for (int row = 1; row <= 9; row++) {
				foreach (int ownerOfRow in new[] { 0, 1, 4, 7 }) {
					yield return new object[] { row, 0, ownerOfRow };
					if (row <= 7)
						yield return new object[] { row, row + 2, ownerOfRow };
				}
			}
		}

		[Theory]
		[MemberData(nameof(AttributeCases))]
		public void AttributesOfATypeIfTheRowsOfAnOwnerAreNotTogether(int row, int secondRow, int ownerOfRow) {
			var patcher = new MetadataPatcher(attributeImage);
			Assert.Equal(9U, patcher.GetRowCount(Table.CustomAttribute));
			uint owner = ownerOfRow == 0 ? 0 : patcher.GetCell(Table.CustomAttribute, (uint)ownerOfRow, "Parent");
			patcher.SetCell(Table.CustomAttribute, (uint)row, "Parent", owner);
			if (secondRow != 0)
				patcher.SetCell(Table.CustomAttribute, (uint)secondRow, "Parent", owner);
			var image = patcher.ToArray();

			var typeNames = new[] { "T1", "T2", "T3" };
			string dnlib;
			using (var module = ModuleDefMD.Load(image))
				dnlib = string.Join(" ", typeNames.Select(a => $"{a}[{string.Join(",", module.Find(a, isReflectionName: true)!.CustomAttributes.Select(b => b.TypeFullName))}]"));

			ClrReaderComparison.AssertSameAsTheRuntime(dnlib, () => {
				using (var loaded = new LoadedAssembly(image))
					return string.Join(" ", typeNames.Select(a => $"{a}[{string.Join(",", loaded.Assembly.GetType(a, throwOnError: true)!.GetCustomAttributes(false).Select(b => b.GetType().Name))}]"));
			});
		}
	}
}
