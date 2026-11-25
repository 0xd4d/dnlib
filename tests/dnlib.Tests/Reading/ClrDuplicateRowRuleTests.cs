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

			AssertSame(ReadConstantsWithDnlib(image, count), () => ReadConstantsWithTheRuntime(image, count));
		}

		// dnlib must read what .NET reads. Another CLR (.NET Framework) may read something else: the test is
		// then skipped with a message that says what it read. Mono isn't asked.
		static void AssertSame(string dnlib, Func<string> readWithTheRuntime) {
			if (ClrReaderComparison.IsMono)
				return;
			string runtime = readWithTheRuntime();
			if (ClrReaderComparison.IsDotNet)
				Assert.Equal(runtime, dnlib);
			else
				Assert.SkipUnless(runtime == dnlib, $"This CLR reads '{runtime}', dnlib reads '{dnlib}'");
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

			AssertSame(ReadLayoutsWithDnlib(image, count), () => ReadLayoutsWithTheRuntime(image, count));
		}
	}
}
