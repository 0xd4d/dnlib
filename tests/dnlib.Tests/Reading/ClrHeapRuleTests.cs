// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// What the CLR reads from a heap if the data isn't what a compiler writes: a length that's too long or
	/// that uses more bytes than needed, an offset that isn't the start of a value or that's not in the heap,
	/// a heap that doesn't exist. Every test changes one value of a file and reads it with dnlib and with the
	/// runtime that runs the tests. dnlib must read what the CLR reads, and where the CLR fails, dnlib has no
	/// value (null) or an empty value.
	///
	/// What the tests show: a length can use more bytes than it needs. A user string is the chars that fit
	/// in its length, with or without the last byte. The CLR doesn't read a user string at offset 0 or with
	/// length 0, dnlib reads an empty string. A GUID only has to start in the heap. A name can start in
	/// another name, and bytes that aren't UTF-8 are U+FFFD. A constant can have more bytes than its type,
	/// a signature can have bytes after its end.
	/// </summary>
	public class ClrHeapRuleTests {
		const string Source = @"
public static class Marker {
	public const int Number = 0x11223344;
	public static string Text() => ""UserString"";
}
";
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source);
		static readonly uint markerRid, textMethodRid;

		static ClrHeapRuleTests() {
			using (var module = ModuleDefMD.Load(baseImage)) {
				var type = module.Find("Marker", isReflectionName: true)!;
				markerRid = type.Rid;
				textMethodRid = type.FindMethod("Text")!.Rid;
			}
		}

		static MetadataBlock CreateBlock(byte[] image) => new MetadataBlock(image).RemoveStream(ClrReaderComparison.PaddingStreamName);

		static byte[] Insert(byte[] data, int offset, params byte[] bytes) =>
			data.Take(offset).Concat(bytes).Concat(data.Skip(offset)).ToArray();

		// A string with every char that isn't a printable ASCII char as \uXXXX
		static string Quote(string? text) {
			if (text is null)
				return "null";
			var sb = new StringBuilder("'");
			foreach (char c in text) {
				if (c >= 0x20 && c < 0x7F)
					sb.Append(c);
				else
					sb.Append($"\\u{(int)c:X4}");
			}
			return sb.Append('\'').ToString();
		}

		// Reads a value of the Marker class with the runtime. An exception is "throws" and its name.
		static string ReadWithTheRuntime(byte[] image, Func<Module, Type, string> read) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var module = loaded.Assembly.ManifestModule;
					return read(module, module.ResolveType(new MDToken(Table.TypeDef, markerRid).ToInt32()));
				}
			}
			catch (TargetInvocationException ex) {
				return "throws " + ex.InnerException!.GetType().Name;
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

		// ----- #US -----

		// Changes the operand of the only ldstr instruction of the file: 0x72, and then the token 0x70000001
		static byte[] SetUserStringOffset(byte[] image, uint offset) {
			var oldCode = new byte[] { 0x72, 0x01, 0x00, 0x00, 0x70 };
			var positions = Enumerable.Range(0, image.Length - oldCode.Length).Where(a => image.Skip(a).Take(oldCode.Length).SequenceEqual(oldCode)).ToArray();
			int position = Assert.Single(positions);
			var patched = (byte[])image.Clone();
			BitConverter.GetBytes(0x70000000 | offset).CopyTo(patched, position + 1);
			return patched;
		}

		static byte[] CreateImageWithAnotherUserString(string change) {
			var image = baseImage;
			// The heap: a zero byte, and then the string "UserString": its length in bytes (20 for the chars
			// and 1 for the last byte), the chars, and a last byte (0: the string has no special chars)
			int heapLength = new MetadataBlock(image).GetStream("#US").Data.Length;
			switch (change) {
			case "ldstr with offset 0": image = SetUserStringOffset(image, 0); break;
			case "ldstr with the offset of the first char": image = SetUserStringOffset(image, 2); break;
			case "ldstr with the offset of the last byte of the heap": image = SetUserStringOffset(image, (uint)heapLength - 1); break;
			case "ldstr with the offset of the end of the heap": image = SetUserStringOffset(image, (uint)heapLength); break;
			case "ldstr with an offset after the heap": image = SetUserStringOffset(image, (uint)heapLength + 0x100); break;
			}
			var block = CreateBlock(image);
			var heap = block.GetStream("#US");
			Assert.Equal(21, heap.Data[1]);
			Assert.Equal((byte)'U', heap.Data[2]);
			switch (change) {
			case "nothing": break;
			case "length without the last byte": heap.Data[1] = 20; break;
			case "length that ends in a char": heap.Data[1] = 19; break;
			case "length 1": heap.Data[1] = 1; break;
			case "length 0": heap.Data[1] = 0; break;
			case "length in 2 bytes": heap.Data = Insert(heap.Data, 1, 0x80); break;
			case "length in 4 bytes": heap.Data = Insert(heap.Data, 1, 0xC0, 0x00, 0x00); break;
			case "length that ends after the heap": heap.Data[1] = 0x7F; break;
			case "length that ends at the end of the heap": heap.Data[1] = (byte)(heap.Data.Length - 2); break;
			case "length that ends one byte after the heap": heap.Data[1] = (byte)(heap.Data.Length - 1); break;
			case "length byte 0xFF": heap.Data[1] = 0xFF; break;
			case "length byte 0xE0": heap.Data[1] = 0xE0; break;
			case "heap that ends in the string": heap.Data = heap.Data.Take(12).ToArray(); break;
			case "heap that ends after the length": heap.Data = heap.Data.Take(2).ToArray(); break;
			case "heap with 0 bytes": heap.Data = new byte[0]; break;
			case "no #US stream": block.RemoveStream("#US"); break;
			default:
				Assert.StartsWith("ldstr", change);
				break;
			}
			return block.Write();
		}

		[Theory]
		[InlineData("nothing", "'UserString'", "'UserString'")]
		[InlineData("length without the last byte", "'UserString'", "'UserString'")]
		[InlineData("length that ends in a char", "'UserStrin'", "'UserStrin'")]
		[InlineData("length 1", "''", "''")]
		[InlineData("length 0", "''", "throws BadImageFormatException")]
		[InlineData("length in 2 bytes", "'UserString'", "'UserString'")]
		[InlineData("length in 4 bytes", "'UserString'", "'UserString'")]
		[InlineData("length that ends after the heap", "''", "throws BadImageFormatException")]
		[InlineData("length that ends at the end of the heap", "'UserString\\u0000'", "'UserString\\u0000'")]
		[InlineData("length that ends one byte after the heap", "''", "throws BadImageFormatException")]
		[InlineData("length byte 0xFF", "''", "throws BadImageFormatException")]
		[InlineData("length byte 0xE0", "''", "throws BadImageFormatException")]
		[InlineData("heap that ends in the string", "''", "throws BadImageFormatException")]
		[InlineData("heap that ends after the length", "''", "throws BadImageFormatException")]
		[InlineData("heap with 0 bytes", "''", "throws BadImageFormatException")]
		[InlineData("no #US stream", "''", "throws BadImageFormatException")]
		[InlineData("ldstr with offset 0", "''", "throws BadImageFormatException")]
		[InlineData("ldstr with the offset of the first char", "''", "throws BadImageFormatException")]
		[InlineData("ldstr with the offset of the last byte of the heap", "''", "throws BadImageFormatException")]
		[InlineData("ldstr with the offset of the end of the heap", "''", "throws BadImageFormatException")]
		[InlineData("ldstr with an offset after the heap", "''", "throws BadImageFormatException")]
		public void UserString(string change, string expectedDnlib, string expectedRuntime) {
			var image = CreateImageWithAnotherUserString(change);

			string dnlib;
			using (var module = ModuleDefMD.Load(image)) {
				var method = module.ResolveMethod(textMethodRid)!;
				dnlib = Quote(method.Body!.Instructions[0].Operand as string);
			}

			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, () => ReadWithTheRuntime(image, (module, type) =>
				Quote((string?)type.GetMethods(PublicStatic).Single(a => a.Name == "Text").Invoke(null, null))));
		}

		// ----- #Blob -----

		// Adds a blob to the end of the #Blob heap and writes its offset to a column
		static byte[] SetBlob(Table table, uint rid, string column, uint? offset, params byte[] blob) {
			int heapLength = new MetadataBlock(baseImage).GetStream("#Blob").Data.Length;
			var block = CreateBlock(new MetadataPatcher(baseImage).SetCell(table, rid, column, offset ?? (uint)heapLength).ToArray());
			var heap = block.GetStream("#Blob");
			heap.Data = heap.Data.Concat(blob).ToArray();
			return block.Write();
		}

		static byte[] CreateImageWithAnotherConstant(string change) {
			// The value of the constant is a blob: its length (4) and the 4 bytes of the Int32
			switch (change) {
			case "4 bytes": return SetConstant(null, 4, 0x44, 0x33, 0x22, 0x11);
			case "5 bytes": return SetConstant(null, 5, 0x44, 0x33, 0x22, 0x11, 0x55);
			case "3 bytes": return SetConstant(null, 3, 0x44, 0x33, 0x22);
			case "3 bytes and a byte after them": return SetConstant(null, 3, 0x44, 0x33, 0x22, 0x99, 0x99, 0x99, 0x99);
			case "0 bytes": return SetConstant(null, 0);
			case "0 bytes and bytes after them": return SetConstant(null, 0, 0x44, 0x33, 0x22, 0x11);
			case "length in 2 bytes": return SetConstant(null, 0x80, 4, 0x44, 0x33, 0x22, 0x11);
			case "length in 4 bytes": return SetConstant(null, 0xC0, 0, 0, 4, 0x44, 0x33, 0x22, 0x11);
			case "length that ends after the heap": return SetConstant(null, 0x7F, 0x44, 0x33, 0x22, 0x11);
			case "length byte 0xFF": return SetConstant(null, 0xFF, 0x44, 0x33, 0x22, 0x11);
			case "offset 0": return SetConstant(0);
			case "offset of the end of the heap": return SetConstant(null);
			case "offset after the heap": return SetConstant(0x8000);
			default: throw new ArgumentException(change);
			}
		}

		static byte[] SetConstant(uint? offset, params byte[] blob) => SetBlob(Table.Constant, 1, "Value", offset, blob);

		static string Hex(object? value) => value is int number ? "0x" + number.ToString("X8") : value is null ? "null" : value.GetType().Name;

		[Theory]
		[InlineData("4 bytes", "0x11223344", "0x11223344")]
		[InlineData("5 bytes", "0x11223344", "0x11223344")]
		[InlineData("3 bytes", "0x00000000", "throws BadImageFormatException")]
		[InlineData("3 bytes and a byte after them", "0x00000000", "throws BadImageFormatException")]
		[InlineData("0 bytes", "0x00000000", "throws BadImageFormatException")]
		[InlineData("0 bytes and bytes after them", "0x00000000", "throws BadImageFormatException")]
		[InlineData("length in 2 bytes", "0x11223344", "0x11223344")]
		[InlineData("length in 4 bytes", "0x11223344", "0x11223344")]
		[InlineData("length that ends after the heap", "0x00000000", "throws BadImageFormatException")]
		[InlineData("length byte 0xFF", "0x00000000", "throws BadImageFormatException")]
		[InlineData("offset 0", "0x00000000", "throws BadImageFormatException")]
		[InlineData("offset of the end of the heap", "0x00000000", "throws BadImageFormatException")]
		[InlineData("offset after the heap", "0x00000000", "throws BadImageFormatException")]
		public void ConstantValue(string change, string expectedDnlib, string expectedRuntime) {
			var image = CreateImageWithAnotherConstant(change);

			string dnlib;
			using (var module = ModuleDefMD.Load(image)) {
				var field = module.ResolveTypeDef(markerRid)!.FindField("Number")!;
				dnlib = field.Constant is null ? "no constant" : Hex(field.Constant.Value);
			}

			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, () => ReadWithTheRuntime(image, (module, type) =>
				Hex(type.GetFields(PublicStatic).Single(a => a.Name == "Number").GetRawConstantValue())));
		}

		static byte[] CreateImageWithAnotherSignature(string change) {
			// The signature of Text(): a static method (0), 0 parameters, returns a string (0x0E)
			switch (change) {
			case "3 bytes": return SetSignature(null, 3, 0, 0, 0x0E);
			case "length in 2 bytes": return SetSignature(null, 0x80, 3, 0, 0, 0x0E);
			case "length in 4 bytes": return SetSignature(null, 0xC0, 0, 0, 3, 0, 0, 0x0E);
			case "a byte after the signature": return SetSignature(null, 4, 0, 0, 0x0E, 0x08);
			case "length without the return type": return SetSignature(null, 2, 0, 0, 0x0E);
			case "length 0": return SetSignature(null, 0, 0, 0, 0x0E);
			case "parameter count in 2 bytes": return SetSignature(null, 4, 0, 0x80, 0, 0x0E);
			case "length that ends after the heap": return SetSignature(null, 0x7F, 0, 0, 0x0E);
			case "offset 0": return SetSignature(0);
			case "offset after the heap": return SetSignature(0x8000);
			default: throw new ArgumentException(change);
			}
		}

		static byte[] SetSignature(uint? offset, params byte[] blob) => SetBlob(Table.Method, textMethodRid, "Signature", offset, blob);

		[Theory]
		[InlineData("3 bytes", "System.String", "System.String")]
		[InlineData("length in 2 bytes", "System.String", "System.String")]
		[InlineData("length in 4 bytes", "System.String", "System.String")]
		[InlineData("a byte after the signature", "System.String", "System.String")]
		[InlineData("length without the return type", "no signature", "throws BadImageFormatException")]
		[InlineData("length 0", "no signature", "throws TypeLoadException")]
		[InlineData("parameter count in 2 bytes", "System.String", "System.String")]
		[InlineData("length that ends after the heap", "no signature", "throws TypeLoadException")]
		[InlineData("offset 0", "no signature", "throws TypeLoadException")]
		[InlineData("offset after the heap", "no signature", "throws TypeLoadException")]
		public void MethodSignature(string change, string expectedDnlib, string expectedRuntime) {
			var image = CreateImageWithAnotherSignature(change);

			string dnlib;
			using (var module = ModuleDefMD.Load(image)) {
				var method = module.ResolveMethod(textMethodRid)!;
				dnlib = method.MethodSig is null ? "no signature" : method.MethodSig.RetType?.FullName ?? "no return type";
			}

			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, () => ReadWithTheRuntime(image, (module, type) => {
				var method = (MethodInfo)module.ResolveMethod(new MDToken(Table.Method, textMethodRid).ToInt32())!;
				return method.ReturnType.FullName!;
			}));
		}

		// ----- #GUID -----

		static byte[] CreateImageWithAnotherMvid(string change) {
			// The heap has one GUID. An index is a number of a GUID (the first one is 1), not an offset.
			var image = baseImage;
			switch (change) {
			case "index 0": image = SetMvidIndex(0); break;
			case "index 2 of 1": image = SetMvidIndex(2); break;
			case "index 2 of 2": image = SetMvidIndex(2); break;
			case "index 2 of 1 and 15 bytes": image = SetMvidIndex(2); break;
			}
			var block = CreateBlock(image);
			var heap = block.GetStream("#GUID");
			Assert.Equal(16, heap.Data.Length);
			heap.Data = firstGuid.ToByteArray();
			var bytesOfSecond = secondGuid.ToByteArray();
			switch (change) {
			case "nothing":
			case "index 0":
			case "index 2 of 1":
				break;
			case "index 2 of 2": heap.Data = heap.Data.Concat(bytesOfSecond).ToArray(); break;
			case "index 2 of 1 and 15 bytes": heap.Data = heap.Data.Concat(bytesOfSecond.Take(15)).ToArray(); break;
			case "heap with 20 bytes": heap.Data = heap.Data.Concat(new byte[4]).ToArray(); break;
			case "heap with 15 bytes": heap.Data = heap.Data.Take(15).ToArray(); break;
			case "heap with 0 bytes": heap.Data = new byte[0]; break;
			case "no #GUID stream": block.RemoveStream("#GUID"); break;
			default: throw new ArgumentException(change);
			}
			return block.Write();
		}

		static byte[] SetMvidIndex(uint index) => new MetadataPatcher(baseImage).SetCell(Table.Module, 1, "Mvid", index).ToArray();

		static readonly Guid firstGuid = new Guid("11111111-1111-1111-1111-111111111111");
		static readonly Guid secondGuid = new Guid("22222222-2222-2222-2222-222222222222");

		static string Describe(Guid? guid) =>
			guid is null ? "null" :
			guid == firstGuid ? "first" :
			guid == secondGuid ? "second" :
			guid == Guid.Empty ? "zero" :
			guid.ToString()!;

		[Theory]
		[InlineData("nothing", "first", "first")]
		[InlineData("index 0", "null", "zero")]
		[InlineData("index 2 of 1", "null", "throws BadImageFormatException")]
		[InlineData("index 2 of 2", "second", "second")]
		[InlineData("index 2 of 1 and 15 bytes", "22222222-2222-2222-2222-222222222200", "22222222-2222-2222-2222-222222222200")]
		[InlineData("heap with 20 bytes", "first", "first")]
		[InlineData("heap with 15 bytes", "11111111-1111-1111-1111-111111111100", "11111111-1111-1111-1111-111111111100")]
		[InlineData("heap with 0 bytes", "null", "throws BadImageFormatException")]
		[InlineData("no #GUID stream", "null", "throws BadImageFormatException")]
		public void Mvid(string change, string expectedDnlib, string expectedRuntime) {
			var image = CreateImageWithAnotherMvid(change);

			string dnlib;
			using (var module = ModuleDefMD.Load(image))
				dnlib = Describe(module.Mvid);

			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, () => ReadWithTheRuntime(image, (module, type) =>
				Describe(module.ModuleVersionId)));
		}

		// ----- #Strings -----

		static byte[] CreateImageWithAnotherTypeName(string change) {
			var original = new MetadataBlock(baseImage).GetStream("#Strings").Data;
			uint nameOffset = new MetadataPatcher(baseImage).GetCell(Table.TypeDef, markerRid, "Name");
			var image = baseImage;
			switch (change) {
			case "offset of the second char": image = SetNameOffset(nameOffset + 1); break;
			case "offset of the zero byte of the name": image = SetNameOffset(nameOffset + 6); break;
			case "offset 0": image = SetNameOffset(0); break;
			case "offset of the end of the heap": image = SetNameOffset((uint)original.Length); break;
			case "offset after the heap": image = SetNameOffset((uint)original.Length + 0x100); break;
			case "char in more bytes than needed":
			case "name with 1023 chars":
			case "name with 1024 chars":
			case "name with 5000 chars":
				// The name is a new string at the end of the heap
				image = SetNameOffset((uint)original.Length);
				break;
			}
			var block = CreateBlock(image);
			var heap = block.GetStream("#Strings");
			switch (change) {
			case "nothing":
				break;
			case "byte that isn't UTF-8":
				heap.Data[nameOffset] = 0xFF;
				break;
			case "char in more bytes than needed":
				// 'M' (0x4D) in 2 bytes, and then "a"
				heap.Data = heap.Data.Concat(new byte[] { 0xC1, 0x8D, (byte)'a', 0 }).ToArray();
				break;
			case "name with 1023 chars": heap.Data = heap.Data.Concat(new byte[1023].Select(a => (byte)'N')).Concat(new byte[1]).ToArray(); break;
			case "name with 1024 chars": heap.Data = heap.Data.Concat(new byte[1024].Select(a => (byte)'N')).Concat(new byte[1]).ToArray(); break;
			case "name with 5000 chars": heap.Data = heap.Data.Concat(new byte[5000].Select(a => (byte)'N')).Concat(new byte[1]).ToArray(); break;
			case "heap with 0 bytes": heap.Data = new byte[0]; break;
			case "no #Strings stream": block.RemoveStream("#Strings"); break;
			default:
				Assert.StartsWith("offset", change);
				break;
			}
			return block.Write();
		}

		static byte[] SetNameOffset(uint offset) => new MetadataPatcher(baseImage).SetCell(Table.TypeDef, markerRid, "Name", offset).ToArray();

		// A long name is its first char and its length
		static string DescribeName(string? name) => name is not null && name.Length > 100 ? $"{name[0]} x {name.Length}" : Quote(name);

		[Theory]
		[InlineData("nothing", "'Marker'", "'Marker'")]
		[InlineData("offset of the second char", "'arker'", "'arker'")]
		[InlineData("offset of the zero byte of the name", "''", "''")]
		[InlineData("offset 0", "''", "''")]
		[InlineData("offset of the end of the heap", "''", "throws BadImageFormatException")]
		[InlineData("offset after the heap", "''", "throws BadImageFormatException")]
		[InlineData("byte that isn't UTF-8", "'\\uFFFDarker'", "'\\uFFFDarker'")]
		[InlineData("char in more bytes than needed", "'\\uFFFD\\uFFFDa'", "'\\uFFFD\\uFFFDa'")]
		[InlineData("name with 1023 chars", "N x 1023", "N x 1023")]
		[InlineData("name with 1024 chars", "N x 1024", "N x 1024")]
		[InlineData("name with 5000 chars", "N x 5000", "N x 5000")]
		[InlineData("heap with 0 bytes", "''", "throws BadImageFormatException")]
		[InlineData("no #Strings stream", "''", "throws BadImageFormatException")]
		public void TypeName(string change, string expectedDnlib, string expectedRuntime) {
			var image = CreateImageWithAnotherTypeName(change);

			string dnlib;
			using (var module = ModuleDefMD.Load(image))
				dnlib = DescribeName(module.ResolveTypeDef(markerRid)!.Name);

			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, () => ReadWithTheRuntime(image, (module, type) =>
				DescribeName(type.Name)));
		}

		// ----- The sizes of the offsets -----

		// Writes the tables again with offsets of 4 bytes instead of 2 bytes for some heaps, and sets the
		// flags of the tables header that say so
		static byte[] UseBigOffsets(MDStreamFlags flags) {
			var block = CreateBlock(baseImage);
			var tables = block.Streams.Single(a => a.Name == "#~");
			var patcher = new MetadataPatcher(baseImage);
			var output = new MemoryStream();
			using (var module = ModuleDefMD.Load(baseImage)) {
				var mdTables = module.TablesStream.MDTables.Where(a => a.Rows != 0).ToArray();
				// The header ends after the row counts of the tables that have rows
				const int FlagsOffset = 6, RowCountsOffset = 24;
				output.Write(tables.Data, 0, RowCountsOffset + mdTables.Length * 4);
				foreach (var mdTable in mdTables) {
					for (uint rid = 1; rid <= mdTable.Rows; rid++) {
						foreach (var column in mdTable.Columns) {
							bool isBig =
								(column.ColumnSize == ColumnSize.Strings && (flags & MDStreamFlags.BigStrings) != 0) ||
								(column.ColumnSize == ColumnSize.GUID && (flags & MDStreamFlags.BigGUID) != 0) ||
								(column.ColumnSize == ColumnSize.Blob && (flags & MDStreamFlags.BigBlob) != 0);
							var value = BitConverter.GetBytes(patcher.GetCell(mdTable.Table, rid, column.Name));
							output.Write(value, 0, isBig ? 4 : column.Size);
						}
					}
				}
				tables.Data = output.ToArray();
				tables.Data[FlagsOffset] |= (byte)flags;
			}
			return block.Write();
		}

		// The flags say how many bytes an offset has, not the sizes of the heaps: a file with small heaps
		// can have offsets of 4 bytes
		[Theory]
		[InlineData(MDStreamFlags.BigStrings, "#~")]
		[InlineData(MDStreamFlags.BigGUID, "#~")]
		[InlineData(MDStreamFlags.BigBlob, "#~")]
		[InlineData(MDStreamFlags.BigStrings | MDStreamFlags.BigGUID | MDStreamFlags.BigBlob, "#~")]
		[InlineData(MDStreamFlags.BigStrings, "#-")]
		[InlineData(MDStreamFlags.BigGUID, "#-")]
		[InlineData(MDStreamFlags.BigBlob, "#-")]
		[InlineData(MDStreamFlags.BigStrings | MDStreamFlags.BigGUID | MDStreamFlags.BigBlob, "#-")]
		public void SmallHeapsCanHaveBigOffsets(MDStreamFlags flags, string tablesName) {
			var block = new MetadataBlock(UseBigOffsets(flags));
			block.GetStream("#~").Name = tablesName;
			var image = block.Write();

			string dnlib;
			using (var original = ModuleDefMD.Load(baseImage))
			using (var module = ModuleDefMD.Load(image)) {
				Assert.Equal(flags, module.TablesStream.Flags);
				// The Module table has offsets in the #Strings and in the #GUID heap, the Constant table in the #Blob heap
				Assert.True(module.TablesStream.ModuleTable.RowSize + module.TablesStream.ConstantTable.RowSize > original.TablesStream.ModuleTable.RowSize + original.TablesStream.ConstantTable.RowSize);
				var type = module.ResolveTypeDef(markerRid)!;
				dnlib = $"{type.Name} {Hex(type.FindField("Number")!.Constant!.Value)} {module.Mvid == original.Mvid}";
			}
			Assert.Equal("Marker 0x11223344 True", dnlib);

			Guid realMvid;
			using (var loaded = new LoadedAssembly(baseImage))
				realMvid = loaded.Assembly.ManifestModule.ModuleVersionId;
			ClrReaderComparison.AssertRead(dnlib, dnlib, dnlib, () => ReadWithTheRuntime(image, (module, type) =>
				$"{type.Name} {Hex(type.GetFields(PublicStatic).Single(a => a.Name == "Number").GetRawConstantValue())} {module.ModuleVersionId == realMvid}"));
		}
	}
}
