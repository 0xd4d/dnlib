// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;
using FieldAttributes = dnlib.DotNet.FieldAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The layout of a type is in its flags (auto, sequential or explicit), in a row of the ClassLayout table
	/// (packing size and size) and, for every field with an offset, in a row of the FieldLayout table. These
	/// tests write values that a compiler doesn't write and read the layout with dnlib and with the runtime
	/// that runs the tests (System.Reflection shows it as StructLayoutAttribute and FieldOffsetAttribute). If
	/// the runtime reads a value, dnlib must read the same value. A type without a ClassLayout row has packing
	/// size 0 and size 0 in the runtime, and no ClassLayout in dnlib. The runtime loads the type to show its
	/// layout, so it throws for values that it doesn't allow (a packing size that isn't a power of 2 up to
	/// 128, an explicit layout with a field without an offset, ...); dnlib has the values of the file.
	/// </summary>
	public class ClrLayoutRuleTests {
		// E has a ClassLayout row and two FieldLayout rows, A has none of them
		const string Source = @"
using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Explicit, Pack = 2, Size = 24)]
public struct E {
	[FieldOffset(4)] public int X;
	[FieldOffset(8)] public int Y;
}
public class A {
	public int X;
	public static int S;
}";
		static readonly byte[] baseImage = CSharpCompiler.Compile(Source).Image;

		// "Table.Column = value", separated by "; ". The row is the one of E, or of E.X in the FieldLayout
		// table. A value is a number, or the name of a type or of a field for a column that has one.
		static byte[] CreateImage(string changes) {
			uint GetRid(string name) {
				using (var module = ModuleDefMD.Load(baseImage)) {
					if (name.Contains("."))
						return module.Find(name.Split('.')[0], false)!.FindField(name.Split('.')[1])!.Rid;
					return module.Find(name, false)!.Rid;
				}
			}
			var patcher = new MetadataPatcher(baseImage);
			uint FindRow(Table table, string column, uint value) =>
				Enumerable.Range(1, (int)patcher.GetRowCount(table)).Select(a => (uint)a).Single(a => patcher.GetCell(table, a, column) == value);
			uint typeRow = GetRid("E");
			uint classLayoutRow = FindRow(Table.ClassLayout, "Parent", typeRow);
			uint fieldLayoutRow = FindRow(Table.FieldLayout, "Field", GetRid("E.X"));
			foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
				var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
				var table = (Table)Enum.Parse(typeof(Table), parts[0].Split('.')[0]);
				string column = parts[0].Split('.')[1];
				uint value = char.IsDigit(parts[1][0]) ? uint.Parse(parts[1]) : GetRid(parts[1]);
				patcher.SetCell(table, table == Table.TypeDef ? typeRow : table == Table.ClassLayout ? classLayoutRow : fieldLayoutRow, column, value);
			}
			return patcher.ToArray();
		}

		static string DescribeWithDnlib(TypeDef type) {
			string kind = type.IsExplicitLayout ? "Explicit" : type.IsSequentialLayout ? "Sequential" : type.IsAutoLayout ? "Auto" : "?";
			string layout = type.ClassLayout is null ? "" : $" pack={type.PackingSize} size={type.ClassSize}";
			return kind + layout + string.Concat(type.Fields.Select(a => $" {a.Name}@{(a.FieldOffset is null ? "-" : a.FieldOffset.ToString())}"));
		}

		static string DescribeWithTheRuntime(Type type) {
			var layout = type.StructLayoutAttribute!;
			var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
				.OrderBy(a => a.MetadataToken);
			return $"{layout.Value} pack={layout.Pack} size={layout.Size}" + string.Concat(fields.Select(a => {
				var offset = (FieldOffsetAttribute?)a.GetCustomAttributes(typeof(FieldOffsetAttribute), false).FirstOrDefault();
				return $" {a.Name}@{(offset is null ? "-" : offset.Value.ToString())}";
			}));
		}

		[Theory]
		[InlineData("", "Explicit pack=2 size=24 X@4 Y@8 / Auto X@- S@-", "Explicit pack=2 size=24 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		// The packing size
		[InlineData("ClassLayout.PackingSize = 0", "Explicit pack=0 size=24 X@4 Y@8 / Auto X@- S@-", "Explicit pack=0 size=24 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("ClassLayout.PackingSize = 1", "Explicit pack=1 size=24 X@4 Y@8 / Auto X@- S@-", "Explicit pack=1 size=24 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("ClassLayout.PackingSize = 3", "Explicit pack=3 size=24 X@4 Y@8 / Auto X@- S@-", "throws BadImageFormatException")]
		[InlineData("ClassLayout.PackingSize = 128", "Explicit pack=128 size=24 X@4 Y@8 / Auto X@- S@-", "Explicit pack=128 size=24 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("ClassLayout.PackingSize = 256", "Explicit pack=256 size=24 X@4 Y@8 / Auto X@- S@-", "throws TypeLoadException")]
		[InlineData("ClassLayout.PackingSize = 65535", "Explicit pack=65535 size=24 X@4 Y@8 / Auto X@- S@-", "throws TypeLoadException")]
		// The size
		[InlineData("ClassLayout.ClassSize = 0", "Explicit pack=2 size=0 X@4 Y@8 / Auto X@- S@-", "Explicit pack=2 size=0 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("ClassLayout.ClassSize = 1", "Explicit pack=2 size=1 X@4 Y@8 / Auto X@- S@-", "Explicit pack=2 size=1 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("ClassLayout.ClassSize = 2147483647", "Explicit pack=2 size=2147483647 X@4 Y@8 / Auto X@- S@-", "Explicit pack=2 size=2147483647 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("ClassLayout.ClassSize = 2147483648", "Explicit pack=2 size=2147483648 X@4 Y@8 / Auto X@- S@-", "throws TypeLoadException")]
		[InlineData("ClassLayout.ClassSize = 4294967295", "Explicit pack=2 size=4294967295 X@4 Y@8 / Auto X@- S@-", "throws TypeLoadException")]
		// The type of the row
		[InlineData("ClassLayout.Parent = 0", "Explicit X@4 Y@8 / Auto X@- S@-", "Explicit pack=0 size=0 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("ClassLayout.Parent = A", "Explicit X@4 Y@8 / Auto pack=2 size=24 X@- S@-", "Explicit pack=0 size=0 X@4 Y@8 / Auto pack=2 size=24 X@- S@-")]
		[InlineData("ClassLayout.Parent = 1000", "Explicit X@4 Y@8 / Auto X@- S@-", "Explicit pack=0 size=0 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		// The layout in the flags of E: 0x100108 = sequential, 0x100110 = explicit, 0x100100 = auto,
		// 0x100118 = not used
		[InlineData("TypeDef.Flags = 1048840", "Sequential pack=2 size=24 X@4 Y@8 / Auto X@- S@-", "Sequential pack=2 size=24 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("TypeDef.Flags = 1048832", "Auto pack=2 size=24 X@4 Y@8 / Auto X@- S@-", "Auto pack=2 size=24 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("TypeDef.Flags = 1048856", "? pack=2 size=24 X@4 Y@8 / Auto X@- S@-", "throws TypeLoadException")]
		[InlineData("TypeDef.Flags = 1048832; ClassLayout.Parent = 0", "Auto X@4 Y@8 / Auto X@- S@-", "Auto pack=0 size=0 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("TypeDef.Flags = 1048848; ClassLayout.Parent = 0", "Explicit X@4 Y@8 / Auto X@- S@-", "Explicit pack=0 size=0 X@4 Y@8 / Auto pack=0 size=0 X@- S@-")]
		// The offset of a field
		[InlineData("FieldLayout.OffSet = 0", "Explicit pack=2 size=24 X@0 Y@8 / Auto X@- S@-", "Explicit pack=2 size=24 X@0 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("FieldLayout.OffSet = 8", "Explicit pack=2 size=24 X@8 Y@8 / Auto X@- S@-", "Explicit pack=2 size=24 X@8 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("FieldLayout.OffSet = 1000", "Explicit pack=2 size=24 X@1000 Y@8 / Auto X@- S@-", "Explicit pack=2 size=24 X@1000 Y@8 / Auto pack=0 size=0 X@- S@-")]
		[InlineData("FieldLayout.OffSet = 2147483647", "Explicit pack=2 size=24 X@2147483647 Y@8 / Auto X@- S@-", "throws TypeLoadException")]
		[InlineData("FieldLayout.OffSet = 2147483648", "Explicit pack=2 size=24 X@2147483648 Y@8 / Auto X@- S@-", "throws TypeLoadException")]
		// The field of the row
		[InlineData("FieldLayout.Field = 0", "Explicit pack=2 size=24 X@- Y@8 / Auto X@- S@-", "throws TypeLoadException")]
		[InlineData("FieldLayout.Field = E.Y", "Explicit pack=2 size=24 X@- Y@4 / Auto X@- S@-", "throws TypeLoadException")]
		[InlineData("FieldLayout.Field = A.X", "Explicit pack=2 size=24 X@- Y@- / Auto X@4 S@-", "throws TypeLoadException")]
		[InlineData("FieldLayout.Field = A.S", "Explicit pack=2 size=24 X@- Y@- / Auto X@- S@4", "throws TypeLoadException")]
		[InlineData("FieldLayout.Field = 1000", "Explicit pack=2 size=24 X@- Y@- / Auto X@- S@-", "throws TypeLoadException")]
		public void LayoutOfATypeAndItsFields(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			string dnlib;
			using (var module = ModuleDefMD.Load(image))
				dnlib = DescribeWithDnlib(module.Find("E", false)!) + " / " + DescribeWithDnlib(module.Find("A", false)!);

			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, () => {
				using (var loaded = new LoadedAssembly(image))
					return DescribeWithTheRuntime(loaded.Assembly.GetType("E")!) + " / " + DescribeWithTheRuntime(loaded.Assembly.GetType("A")!);
			});
		}
	}
}
