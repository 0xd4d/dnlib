// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A row of the Constant table has the type of the value (one byte) and the bytes of the value. These
	/// tests write a type that isn't the type of the field, or bytes that are too few or too many for the
	/// type, and read the constant with dnlib and with the runtime that runs the tests. If the runtime
	/// returns a value, dnlib must have the same value.
	/// </summary>
	public class ClrConstantRuleTests {
		const string Source = @"
public static class Marker {
	public const int Number = 0x11223344;
	public const string Text = ""abc"";
}
";
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source);
		static readonly uint markerRid;

		static ClrConstantRuleTests() {
			using (var module = ModuleDefMD.Load(baseImage))
				markerRid = module.Find("Marker", isReflectionName: true)!.Rid;
		}

		// The row of the constant of a field: its Parent column is the row of the field (in the high bits)
		// and 0 (in the 2 low bits: the parent is a field)
		static uint GetConstantRid(string fieldName) {
			var patcher = new MetadataPatcher(baseImage);
			uint fieldRid = patcher.FindRow(Table.Field, "Name", fieldName);
			return Enumerable.Range(1, (int)patcher.GetRowCount(Table.Constant)).Select(a => (uint)a)
				.Single(a => patcher.GetCell(Table.Constant, a, "Parent") == fieldRid << 2);
		}

		// Writes another type (if it's not empty) and other bytes (if they're not null) to the constant of
		// the field. The bytes are hex numbers; they're added to the end of the #Blob heap.
		static byte[] CreateImage(string fieldName, string type, string? value) {
			uint rid = GetConstantRid(fieldName);
			var patcher = new MetadataPatcher(baseImage);
			if (type.Length != 0)
				patcher.SetCell(Table.Constant, rid, "Type", Convert.ToByte(type, 16));
			if (value is null)
				return patcher.ToArray();

			var bytes = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(a => Convert.ToByte(a, 16)).ToArray();
			int heapLength = new MetadataBlock(baseImage).GetStream("#Blob").Data.Length;
			var block = new MetadataBlock(patcher.SetCell(Table.Constant, rid, "Value", (uint)heapLength).ToArray());
			block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			var heap = block.GetStream("#Blob");
			heap.Data = heap.Data.Concat(new[] { (byte)bytes.Length }).Concat(bytes).ToArray();
			return block.Write();
		}

		// "Int32:0x11223344", a string in quotes with \uXXXX for a char that isn't a printable ASCII char, or null
		static string Describe(object? value) {
			switch (value) {
			case null:
				return "null";
			case string text:
				var sb = new StringBuilder("'");
				foreach (char c in text) {
					if (c >= 0x20 && c < 0x7F)
						sb.Append(c);
					else
						sb.Append($"\\u{(int)c:X4}");
				}
				return sb.Append('\'').ToString();
			// The bits of a floating point number
			case float single:
				return $"Single:0x{BitConverter.ToInt32(BitConverter.GetBytes(single), 0):X}";
			case double number:
				return $"Double:0x{BitConverter.DoubleToInt64Bits(number):X}";
			case bool boolean:
				return $"Boolean:{(boolean ? 1 : 0)}";
			case char c:
				return $"Char:0x{(int)c:X}";
			default:
				return $"{value.GetType().Name}:0x{value:X}";
			}
		}

		static string ReadWithDnlib(byte[] image, string fieldName) {
			using (var module = ModuleDefMD.Load(image)) {
				var field = module.ResolveTypeDef(markerRid)!.FindField(fieldName)!;
				return field.Constant is null ? "no constant" : Describe(field.Constant.Value);
			}
		}

		static string ReadWithTheRuntime(byte[] image, string fieldName) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var type = loaded.Assembly.ManifestModule.ResolveType(new MDToken(Table.TypeDef, markerRid).ToInt32());
					var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
					return Describe(field.GetRawConstantValue());
				}
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		// The types: 02 Boolean, 03 Char, 04 SByte, 05 Byte, 06 Int16, 07 UInt16, 08 Int32, 09 UInt32,
		// 0A Int64, 0B UInt64, 0C Single, 0D Double, 0E String, 12 Class (the value is null)
		[Theory]
		[InlineData("Number", "", null, "Int32:0x11223344", "Int32:0x11223344")]
		// Another number type than the field's type, with the bytes of the Int32
		[InlineData("Number", "02", null, "Boolean:1", "Boolean:1")]
		[InlineData("Number", "03", null, "Char:0x3344", "Char:0x3344")]
		[InlineData("Number", "04", null, "SByte:0x44", "SByte:0x44")]
		[InlineData("Number", "05", null, "Byte:0x44", "Byte:0x44")]
		[InlineData("Number", "06", null, "Int16:0x3344", "Int16:0x3344")]
		[InlineData("Number", "07", null, "UInt16:0x3344", "UInt16:0x3344")]
		[InlineData("Number", "09", null, "UInt32:0x11223344", "UInt32:0x11223344")]
		[InlineData("Number", "0A", null, "Int64:0x0", "throws BadImageFormatException")]
		[InlineData("Number", "0B", null, "UInt64:0x0", "throws BadImageFormatException")]
		[InlineData("Number", "0C", null, "Single:0x11223344", "Single:0x11223344")]
		[InlineData("Number", "0D", null, "Double:0x0", "throws BadImageFormatException")]
		[InlineData("Number", "0A", "01 02 03 04 05 06 07 08", "Int64:0x807060504030201", "Int64:0x807060504030201")]
		[InlineData("Number", "0D", "00 00 00 00 00 00 F0 3F", "Double:0x3FF0000000000000", "Double:0x3FF0000000000000")]
		// A string or null in a field that is an Int32
		[InlineData("Number", "0E", null, "'\\u3344\\u1122'", "'\\u3344\\u1122'")]
		[InlineData("Number", "12", null, "null", "throws BadImageFormatException")]
		[InlineData("Number", "12", "00 00 00 00", "null", "null")]
		// Types that can't be the type of a constant
		[InlineData("Number", "00", null, "null", "throws BadImageFormatException")]
		[InlineData("Number", "01", null, "null", "throws NotSupportedException")]
		[InlineData("Number", "18", null, "null", "throws BadImageFormatException")]
		[InlineData("Number", "1C", null, "null", "throws BadImageFormatException")]
		[InlineData("Number", "11", null, "null", "throws BadImageFormatException")]
		[InlineData("Number", "1D", null, "null", "throws BadImageFormatException")]
		[InlineData("Number", "FF", null, "null", "throws BadImageFormatException")]
		// Fewer or more bytes than the type has
		[InlineData("Number", "", "44 33", "Int32:0x0", "throws BadImageFormatException")]
		[InlineData("Number", "", "", "Int32:0x0", "throws BadImageFormatException")]
		[InlineData("Number", "", "44 33 22 11 99 99 99 99", "Int32:0x11223344", "Int32:0x11223344")]
		[InlineData("Number", "02", "02", "Boolean:1", "Boolean:1")]
		[InlineData("Number", "02", "", "Boolean:0", "throws BadImageFormatException")]
		// Strings: UTF-16 chars
		[InlineData("Text", "", null, "'abc'", "'abc'")]
		[InlineData("Text", "", "", "''", "''")]
		[InlineData("Text", "", "61 00 62", "'a'", "'a'")]
		[InlineData("Text", "", "61", "''", "''")]
		[InlineData("Text", "", "61 00 00 00 62 00", "'a\\u0000b'", "'a\\u0000b'")]
		[InlineData("Text", "", "00 D8 61 00", "'\\uD800a'", "'\\uD800a'")]
		// A number or null in a field that is a string
		[InlineData("Text", "08", null, "Int32:0x620061", "Int32:0x620061")]
		[InlineData("Text", "08", "44 33 22 11", "Int32:0x11223344", "Int32:0x11223344")]
		[InlineData("Text", "12", null, "null", "throws BadImageFormatException")]
		[InlineData("Text", "12", "00 00 00 00", "null", "null")]
		[InlineData("Text", "12", "01 00 00 00", "null", "throws BadImageFormatException")]
		[InlineData("Text", "12", "", "null", "throws BadImageFormatException")]
		public void ConstantValue(string fieldName, string type, string? value, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(fieldName, type, value);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image, fieldName), expectedRuntime, () => ReadWithTheRuntime(image, fieldName));
		}
	}
}
