// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The rules of the CLR's metadata reader that no specification has, and that a file can use to confuse a
	/// tool that reads it in another way. The CLR has two readers (see <c>md/runtime/mdinternaldisp.cpp</c>,
	/// <c>liteweightstgdb.cpp</c> and <c>md/enc/liteweightstgdbrw.cpp</c> in the runtime's source code):
	/// <list type="bullet">
	/// <item>Tables stream <c>#~</c>: if two streams have the same name, the LAST one is used. Names are
	/// compared exactly.</item>
	/// <item>Tables stream <c>#-</c>, or a <c>#Schema</c> stream after the tables stream: the FIRST one is
	/// used, and the case of a name doesn't matter (<c>#strings</c> is the strings heap).</item>
	/// </list>
	/// Every test creates a file with other metadata streams and reads four values that come from the four
	/// heaps, with dnlib and with the runtime that runs the tests: both must read the same values.
	/// </summary>
	public class ClrMetadataStreamRuleTests {
		const string Source = @"
public static class Marker {
	public const int Number = 0x11223344;
	public static string Text() => ""UserString"";
}
";
		static readonly byte[] baseImage = CreateBaseImage(Source);
		// The same with a generic class: the file has the tables that .NET Framework 2.0 added
		static readonly byte[] baseImageWithGenerics = CreateBaseImage(Source + "public class Generic<T> { }");
		internal static readonly uint markerRid = GetMarkerRid();

		static byte[] CreateBaseImage(string source, bool bigColumns = false) =>
			ClrReaderComparison.CreateBaseImage(source, options => {
				if (bigColumns) {
					options.MetadataOptions.TablesHeapOptions.UseENC = true;
					options.MetadataOptions.TablesHeapOptions.ForceBigColumns = true;
				}
			});

		static uint GetMarkerRid() {
			using (var module = ModuleDefMD.Load(baseImage))
				return module.Find("Marker", isReflectionName: true)!.Rid;
		}

		// The metadata of the file without the padding stream
		static MetadataBlock CreateBlock() => new MetadataBlock(baseImage).RemoveStream("#Pad");

		static void Replace(byte[] data, byte[] oldBytes, byte[] newBytes) {
			Assert.Equal(oldBytes.Length, newBytes.Length);
			int count = 0;
			for (int i = 0; i + oldBytes.Length <= data.Length; i++) {
				if (data.Skip(i).Take(oldBytes.Length).SequenceEqual(oldBytes)) {
					newBytes.CopyTo(data, i);
					count++;
				}
			}
			Assert.True(count > 0);
		}

		// A copy of a heap in which the value that the tests read is another value
		static MetadataBlockStream CreateDecoy(MetadataBlock block, string name, string? newName = null) {
			var decoy = block.GetStream(name).Clone();
			switch (name) {
			case "#Strings": Replace(decoy.Data, Encoding.ASCII.GetBytes("Marker"), Encoding.ASCII.GetBytes("Decoy1")); break;
			case "#US": Replace(decoy.Data, Encoding.Unicode.GetBytes("UserString"), Encoding.Unicode.GetBytes("FakeString")); break;
			case "#Blob": Replace(decoy.Data, new byte[] { 0x44, 0x33, 0x22, 0x11 }, new byte[] { 0x88, 0x77, 0x66, 0x55 }); break;
			case "#GUID": decoy.Data = Enumerable.Repeat((byte)0x11, decoy.Data.Length).ToArray(); break;
			default: throw new ArgumentException(name);
			}
			decoy.Name = newName ?? name;
			return decoy;
		}

		static readonly string[] heapNames = { "#Strings", "#US", "#Blob", "#GUID" };

		static List<MetadataBlockStream> CreateDecoys(MetadataBlock block, Func<string, string>? rename = null) =>
			heapNames.Select(name => CreateDecoy(block, name, rename?.Invoke(name))).ToList();

		// One letter for each of the four values: R if it's the real value, D if it's the value of the decoy
		internal static string Describe(string? typeName, string? text, object? number, Guid? mvid) {
			char Letter(bool isReal, bool isDecoy) => isReal ? 'R' : isDecoy ? 'D' : '?';
			return new string(new[] {
				Letter(typeName == "Marker", typeName == "Decoy1"),
				Letter(text == "UserString", text == "FakeString"),
				Letter(Equals(number, 0x11223344), Equals(number, 0x55667788)),
				Letter(mvid is not null && mvid != new Guid("11111111-1111-1111-1111-111111111111") && mvid != Guid.Empty, mvid == new Guid("11111111-1111-1111-1111-111111111111")),
			});
		}

		internal static string ReadWithDnlib(byte[] image, CLRRuntimeReaderKind runtime = CLRRuntimeReaderKind.CLR, uint? typeRid = null) {
			ModuleDefMD module;
			try {
				module = ModuleDefMD.Load(image, new ModuleCreationOptions(runtime));
			}
			catch (BadImageFormatException) {
				return "rejected";
			}
			using (module) {
				var type = module.ResolveTypeDef(typeRid ?? markerRid);
				if (type is null)
					return "no type";
				var text = type.Methods.FirstOrDefault(a => a.MethodSig?.RetType?.ElementType == ElementType.String)?.Body?.Instructions.FirstOrDefault(a => a.Operand is string)?.Operand as string;
				var number = type.Fields.FirstOrDefault(a => a.HasConstant)?.Constant?.Value;
				return Describe(type.Name, text, number, module.Mvid);
			}
		}

		static string ReadWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var module = loaded.Assembly.ManifestModule;
					var type = module.ResolveType(new MDToken(Table.TypeDef, markerRid).ToInt32());
					const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
					var text = (string?)type.GetMethods(flags).Single(a => a.ReturnType == typeof(string)).Invoke(null, null);
					var number = type.GetFields(flags).Single(a => a.IsLiteral).GetRawConstantValue();
					return Describe(type.Name, text, number, module.ModuleVersionId);
				}
			}
			catch (BadImageFormatException) {
				return "rejected";
			}
			catch (FileLoadException) {
				return "rejected";
			}
			catch (System.Security.SecurityException) {
				// "Invalid assembly public key": what it read as the public key isn't one
				return "rejected";
			}
		}

		// What the test changes in the metadata
		internal static byte[] CreateImage(string change) {
			var block = CreateBlock();
			var streams = block.Streams;
			var tables = block.GetStream("#~");
			switch (change) {
			case "nothing":
				break;
			case "decoys first":
				streams.InsertRange(0, CreateDecoys(block));
				break;
			case "decoys last":
				streams.AddRange(CreateDecoys(block));
				break;
			case "decoys with lower case names last":
				streams.AddRange(CreateDecoys(block, name => name.ToLowerInvariant()));
				break;
			case "decoys with lower case names first":
				streams.InsertRange(0, CreateDecoys(block, name => name.ToLowerInvariant()));
				break;
			case "heaps with upper case names":
				foreach (var stream in streams.Where(a => heapNames.Contains(a.Name)))
					stream.Name = stream.Name.ToUpperInvariant();
				break;
			case "unknown stream":
				streams.Insert(0, new MetadataBlockStream("#Foo", new byte[] { 1, 2, 3 }));
				streams.Add(new MetadataBlockStream("#Bar", new byte[0]));
				break;
			case "stream without a name":
				streams.Insert(2, new MetadataBlockStream("", new byte[] { 1, 2, 3, 4 }));
				break;
			case "stream with one char":
				streams.Insert(2, new MetadataBlockStream("#", new byte[] { 1, 2, 3, 4 }));
				break;
			case "#JTD":
				streams.Add(new MetadataBlockStream("#JTD", new byte[4]));
				break;
			case "#Pdb":
				streams.Add(new MetadataBlockStream("#Pdb", new byte[4]));
				break;
			case "two tables streams":
				streams.Add(tables.Clone());
				break;
			case "tables stream and an uncompressed tables stream":
				streams.Add(new MetadataBlockStream("#-", (byte[])tables.Data.Clone()));
				break;
			case "#Schema after the tables stream, decoys last":
				streams.AddRange(CreateDecoys(block));
				streams.Add(new MetadataBlockStream("#Schema", new byte[4]));
				break;
			case "#Schema after the tables stream, decoys first":
				streams.InsertRange(0, CreateDecoys(block));
				streams.Add(new MetadataBlockStream("#Schema", new byte[4]));
				break;
			case "#Schema after the tables stream, decoys with lower case names first":
				streams.InsertRange(0, CreateDecoys(block, name => name.ToLowerInvariant()));
				streams.Add(new MetadataBlockStream("#Schema", new byte[4]));
				break;
			case "#Schema and #JTD after the tables stream":
				streams.Add(new MetadataBlockStream("#Schema", new byte[4]));
				streams.Add(new MetadataBlockStream("#JTD", new byte[4]));
				break;
			case "#jtd":
				streams.Add(new MetadataBlockStream("#jtd", new byte[4]));
				break;
			case "#Schema before the tables stream":
				streams.Insert(0, new MetadataBlockStream("#Schema", new byte[4]));
				break;
			case "#schema after the tables stream, decoys first":
				streams.InsertRange(0, CreateDecoys(block));
				streams.Add(new MetadataBlockStream("#schema", new byte[4]));
				break;
			default:
				throw new ArgumentException(change);
			}
			return block.Write();
		}

		internal static byte[] CreateUncompressedImage(string change) {
			var block = new MetadataBlock(CreateImage(change));
			foreach (var stream in block.Streams.Where(a => a.Name == "#~"))
				stream.Name = "#-";
			return block.Write();
		}

		static byte[] Insert(byte[] data, int offset, params byte[] bytes) =>
			data.Take(offset).Concat(bytes).Concat(data.Skip(offset)).ToArray();

		// The header of the tables stream: 4 reserved bytes, the version (2 bytes), the flags, a reserved
		// byte, a 64-bit mask with a bit for every table that the file has, a 64-bit mask of the sorted
		// tables, and then the 32-bit row counts of the tables that the file has
		const int TablesVersionOffset = 4, TablesFlagsOffset = 6, ValidMaskOffset = 8, RowCountsOffset = 24;

		static int GetEndOfRowCounts(byte[] tables) {
			ulong valid = BitConverter.ToUInt64(tables, ValidMaskOffset);
			int count = 0;
			for (; valid != 0; valid >>= 1)
				count += (int)(valid & 1);
			return RowCountsOffset + count * 4;
		}

		internal static byte[] CreateImageWithAnotherHeader(string change) {
			var block = new MetadataBlock(change.Contains("generic") ? baseImageWithGenerics : baseImage).RemoveStream("#Pad");
			var tables = block.GetStream("#~");
			var strings = block.GetStream("#Strings");
			switch (change) {
			case "extra data after the row counts":
				tables.Data[TablesFlagsOffset] |= 0x40;
				tables.Data = Insert(tables.Data, GetEndOfRowCounts(tables.Data), 0x78, 0x56, 0x34, 0x12);
				break;
			case "extra data flag without the data":
				tables.Data[TablesFlagsOffset] |= 0x40;
				break;
			case "row count of a table that doesn't exist":
				// Table 0x3E: the highest bit but one
				tables.Data = Insert(tables.Data, GetEndOfRowCounts(tables.Data), 5, 0, 0, 0);
				tables.Data[ValidMaskOffset + 7] |= 0x40;
				break;
			case "row count of a table of a portable PDB":
				// Table 0x30 (Document). The CLR's last table is 0x2C.
				tables.Data = Insert(tables.Data, GetEndOfRowCounts(tables.Data), 5, 0, 0, 0);
				tables.Data[ValidMaskOffset + 6] |= 0x01;
				break;
			case "padding flag":
				tables.Data[TablesFlagsOffset] |= 0x08;
				break;
			case "delta only flag":
				tables.Data[TablesFlagsOffset] |= 0x20;
				break;
			case "has delete flag":
				tables.Data[TablesFlagsOffset] |= 0x80;
				break;
			case "tables version 1.0":
			case "tables version 1.0 with generic tables":
				tables.Data[TablesVersionOffset] = 1;
				tables.Data[TablesVersionOffset + 1] = 0;
				break;
			case "tables version 1.1":
				tables.Data[TablesVersionOffset] = 1;
				tables.Data[TablesVersionOffset + 1] = 1;
				break;
			case "tables version 3.0":
				tables.Data[TablesVersionOffset] = 3;
				break;
			case "metadata version 1.0":
				block.MinorVersion = 0;
				break;
			case "metadata version 2.0":
				block.MajorVersion = 2;
				block.MinorVersion = 0;
				break;
			case "storage flags that aren't zero":
				block.StorageFlags = 2;
				break;
			case "storage flag 'extra data' and 4 bytes of extra data":
				// The size of the data and the data
				block.StorageFlags = 1;
				block.DataAfterStorageHeader = new byte[] { 4, 0, 0, 0, 1, 2, 3, 4 };
				break;
			case "reserved fields that aren't zero":
				block.Reserved = 0x12345678;
				block.StorageReserved = 0x9A;
				tables.Data[0] = 0xBC;
				tables.Data[7] = 0xDE;
				break;
			case "strings heap that doesn't end with a zero byte":
				strings.Data = strings.Data.Concat(Encoding.ASCII.GetBytes("NoEnd")).ToArray();
				break;
			case "name at the end of the strings heap without a zero byte": {
				// The name of the class is now a new string at the end of the heap
				var patched = new MetadataPatcher(baseImage).SetCell(Table.TypeDef, markerRid, "Name", (uint)strings.Data.Length).ToArray();
				block = new MetadataBlock(patched).RemoveStream("#Pad");
				strings = block.GetStream("#Strings");
				strings.Data = strings.Data.Concat(Encoding.ASCII.GetBytes("Marker")).ToArray();
				break;
			}
			case "heap that's bigger than the metadata":
				block.GetStream("#US").SizeInHeader = 0x100000;
				break;
			case "unknown stream that's bigger than the metadata":
				block.Streams.Add(new MetadataBlockStream("#Foo", new byte[4]) { SizeInHeader = 0x100000 });
				break;
			case "unknown stream that isn't in the metadata":
				block.Streams.Add(new MetadataBlockStream("#Foo", new byte[4]) { OffsetInHeader = 0x100000 });
				break;
			case "tables stream that's shorter than its tables":
				tables.SizeInHeader = (uint)GetEndOfRowCounts(tables.Data) + 16;
				break;
			case "tables stream that's longer than its tables":
				tables.Data = tables.Data.Concat(new byte[0x40]).ToArray();
				break;
			case "tables stream that ends in its header":
				tables.SizeInHeader = RowCountsOffset + 4;
				break;
			case "metadata size that's too small for the last stream":
				block.SizeInDotNetHeader = (uint)block.CreateMetadata().Length - 8;
				break;
			case "metadata size that's bigger than the metadata":
				block.SizeInDotNetHeader = (uint)block.CreateMetadata().Length + 0x1000;
				break;
			case "metadata size that ends after the stream headers":
				block.SizeInDotNetHeader = 0x90;
				break;
			case "version string without a zero byte":
				block.VersionString = Encoding.ASCII.GetBytes("v4.0.30319AB");
				break;
			case "version string with 0 bytes":
				block.VersionString = new byte[0];
				break;
			case "stream name with 31 characters":
				block.Streams.Add(new MetadataBlockStream(new string('A', 31), new byte[4]));
				break;
			case "stream name with 32 characters":
				block.Streams.Add(new MetadataBlockStream(new string('A', 32), new byte[4]));
				break;
			case "more streams in the header than there are":
				block.StreamCount = (ushort)(block.Streams.Count + 1);
				break;
			case "0x8000 streams in the header":
				// A reader that reads the number as a signed value gets a negative number
				block.StreamCount = 0x8000;
				break;
			case "0xFFFF streams in the header":
				block.StreamCount = 0xFFFF;
				break;
			case "fewer streams in the header than there are":
				// The last stream is the #Blob heap
				Assert.Equal("#Blob", block.Streams.Last().Name);
				block.StreamCount = (ushort)(block.Streams.Count - 1);
				break;
			default:
				throw new ArgumentException(change);
			}
			return block.Write();
		}

		// What the CLR accepts in the headers of the metadata and of the tables. dnlib reads the files that
		// the CLR rejects too, if it can.
		[Theory]
		[InlineData("extra data after the row counts", "RRRR", "RRRR")]
		[InlineData("extra data flag without the data", "????", "rejected")]
		[InlineData("row count of a table that doesn't exist", "RRRR", "RRRR")]
		[InlineData("row count of a table of a portable PDB", "RRRR", "RRRR")]
		[InlineData("padding flag", "RRRR", "RRRR")]
		[InlineData("delta only flag", "RRRR", "RRRR|rejected")]
		[InlineData("has delete flag", "RRRR", "RRRR")]
		[InlineData("tables version 1.0", "RRRR", "RRRR")]
		[InlineData("tables version 1.0 with generic tables", "RRRR", "rejected")]
		[InlineData("tables version 1.1", "RRRR", "rejected")]
		[InlineData("tables version 3.0", "RRRR", "rejected")]
		[InlineData("metadata version 1.0", "RRRR", "rejected")]
		[InlineData("metadata version 2.0", "RRRR", "rejected")]
		[InlineData("storage flags that aren't zero", "RRRR", "rejected")]
		[InlineData("storage flag 'extra data' and 4 bytes of extra data", "rejected", "rejected")]
		[InlineData("reserved fields that aren't zero", "RRRR", "RRRR")]
		[InlineData("strings heap that doesn't end with a zero byte", "RRRR", "RRRR")]
		[InlineData("name at the end of the strings heap without a zero byte", "?RRR", "rejected")]
		[InlineData("heap that's bigger than the metadata", "R?RR", "rejected")]
		[InlineData("unknown stream that's bigger than the metadata", "RRRR", "rejected")]
		[InlineData("unknown stream that isn't in the metadata", "RRRR", "rejected")]
		[InlineData("tables stream that's shorter than its tables", "no type", "rejected")]
		[InlineData("tables stream that's longer than its tables", "RRRR", "RRRR")]
		[InlineData("tables stream that ends in its header", "rejected", "rejected")]
		[InlineData("metadata size that's too small for the last stream", "RRRR", "rejected")]
		[InlineData("metadata size that's bigger than the metadata", "RRRR", "RRRR")]
		[InlineData("metadata size that ends after the stream headers", "RRRR", "rejected")]
		[InlineData("version string without a zero byte", "RRRR", "RRRR")]
		[InlineData("version string with 0 bytes", "RRRR", "RRRR")]
		[InlineData("stream name with 31 characters", "RRRR", "RRRR")]
		[InlineData("stream name with 32 characters", "RRRR", "rejected")]
		[InlineData("more streams in the header than there are", "RRRR", "rejected")]
		[InlineData("0x8000 streams in the header", "rejected", "rejected")]
		[InlineData("0xFFFF streams in the header", "rejected", "rejected")]
		[InlineData("fewer streams in the header than there are", "R??R", "rejected")]
		public void MetadataAndTablesHeader(string change, string expectedDnlib, string expectedRuntime) =>
			AssertRead(CreateImageWithAnotherHeader(change), expectedDnlib, expectedRuntime);

		// The same in a file with uncompressed metadata: the CLR reads it with other code
		[Theory]
		[InlineData("extra data after the row counts", "RRRR", "RRRR")]
		[InlineData("row count of a table that doesn't exist", "RRRR", "RRRR")]
		[InlineData("delta only flag", "RRRR", "RRRR|rejected")]
		[InlineData("tables version 1.0", "RRRR", "RRRR")]
		[InlineData("tables version 1.0 with generic tables", "RRRR", "rejected")]
		[InlineData("tables version 3.0", "RRRR", "rejected")]
		[InlineData("metadata version 2.0", "RRRR", "rejected")]
		[InlineData("storage flags that aren't zero", "RRRR", "rejected")]
		[InlineData("strings heap that doesn't end with a zero byte", "RRRR", "RRRR")]
		public void MetadataAndTablesHeaderOfUncompressedMetadata(string change, string expectedDnlib, string expectedRuntime) {
			var block = new MetadataBlock(CreateImageWithAnotherHeader(change));
			block.GetStream("#~").Name = "#-";

			AssertRead(block.Write(), expectedDnlib, expectedRuntime);
		}

		// The CLR only uses the strings heap up to its last zero byte: a string after it doesn't exist
		[Fact]
		public void StringAfterTheLastZeroByteOfTheStringsHeapIsNotRead() {
			using (var module = ModuleDefMD.Load(CreateImageWithAnotherHeader("name at the end of the strings heap without a zero byte"))) {
				var type = module.ResolveTypeDef(markerRid)!;

				Assert.Equal("", type.Name);
				Assert.True(module.TablesStream.TryReadTypeDefRow(markerRid, out var row));
				Assert.Null(module.StringsStream.Read(row.Name));
				// The other strings are read
				Assert.Equal("Text", type.Methods.Single(a => a.Name == "Text").Name);
			}
		}

		// A #JTD stream says that every column that can have 2 or 4 bytes has 4 bytes. The CLR only looks for
		// it when it reads uncompressed metadata (#-, or #~ with a #Schema stream after it), and the case of
		// its name doesn't matter. The tables of this file have such columns: without the stream, the file
		// can't be read. (With the stream and normal tables it can't be read either, see the cases above.)
		[Theory]
		[InlineData("#-", "#JTD", "RRRR", "RRRR")]
		[InlineData("#-", "#jtd", "RRRR", "RRRR")]
		[InlineData("#~", "#Schema #JTD", "RRRR", "RRRR")]
		[InlineData("#~", "#JTD #Schema", "RRRR", "RRRR")]
		[InlineData("#-", "", "????", "rejected")]
		[InlineData("#~", "#JTD", "????", "rejected")]
		[InlineData("#~", "", "????", "rejected")]
		public void TablesWithBigColumnsNeedAJtdStream(string tablesName, string otherStreams, string expectedDnlib, string expectedRuntime) {
			var block = new MetadataBlock(CreateBaseImage(Source, bigColumns: true)).RemoveStream("#Pad");
			Assert.Equal("#-", block.Streams.Single(a => a.Name == "#-" || a.Name == "#~").Name);
			block.GetStream("#-").Name = tablesName;
			foreach (var name in otherStreams.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
				block.Streams.Add(new MetadataBlockStream(name, new byte[4]));

			AssertRead(block.Write(), expectedDnlib, expectedRuntime);
		}

		[Fact]
		public void ExtraDataOfTheTablesHeaderIsRead() {
			using (var module = ModuleDefMD.Load(CreateImageWithAnotherHeader("extra data after the row counts"))) {
				Assert.True(module.TablesStream.HasExtraData);
				Assert.Equal(0x12345678U, module.TablesStream.ExtraData);
			}
			using (var module = ModuleDefMD.Load(baseImage)) {
				Assert.False(module.TablesStream.HasExtraData);
				Assert.Equal(0U, module.TablesStream.ExtraData);
			}
		}

		// Compares dnlib with the runtime
		static void AssertRead(byte[] image, string expectedDnlib, string expectedRuntime) =>
			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));

		[Theory]
		[InlineData("nothing", "RRRR", "RRRR")]
		[InlineData("decoys first", "RRRR", "RRRR")]
		[InlineData("decoys last", "DDDD", "DDDD")]
		[InlineData("decoys with lower case names last", "RRRR", "RRRR")]
		[InlineData("decoys with lower case names first", "RRRR", "RRRR")]
		[InlineData("unknown stream", "RRRR", "RRRR")]
		// .NET doesn't load a file that has a stream with an empty name (wherever the stream is). dnlib reads it.
		[InlineData("stream without a name", "RRRR", "rejected")]
		[InlineData("stream with one char", "RRRR", "RRRR")]
		[InlineData("#JTD", "RRRR", "RRRR")]
		[InlineData("#Pdb", "RRRR", "RRRR")]
		[InlineData("two tables streams", "RRRR", "rejected")]
		[InlineData("tables stream and an uncompressed tables stream", "RRRR", "rejected")]
		[InlineData("#Schema after the tables stream, decoys last", "RRRR", "RRRR")]
		[InlineData("#Schema after the tables stream, decoys first", "DDDD", "DDDD")]
		[InlineData("#Schema after the tables stream, decoys with lower case names first", "DDDD", "DDDD")]
		[InlineData("#Schema and #JTD after the tables stream", "????", "rejected")]
		[InlineData("#Schema before the tables stream", "RRRR", "rejected")]
		[InlineData("#schema after the tables stream, decoys first", "RRRR", "RRRR")]
		[InlineData("heaps with upper case names", "???R", "rejected")]
		public void CompressedMetadata(string change, string expectedDnlib, string expectedRuntime) =>
			AssertRead(CreateImage(change), expectedDnlib, expectedRuntime);

		[Theory]
		[InlineData("nothing", "RRRR", "RRRR")]
		[InlineData("decoys first", "DDDD", "DDDD")]
		[InlineData("decoys last", "RRRR", "RRRR")]
		[InlineData("decoys with lower case names last", "RRRR", "RRRR")]
		[InlineData("decoys with lower case names first", "DDDD", "DDDD")]
		[InlineData("heaps with upper case names", "RRRR", "RRRR")]
		[InlineData("unknown stream", "RRRR", "RRRR")]
		// .NET doesn't load a file that has a stream with an empty name (wherever the stream is). dnlib reads it.
		[InlineData("stream without a name", "RRRR", "rejected")]
		[InlineData("#Pdb", "RRRR", "RRRR")]
		[InlineData("two tables streams", "RRRR", "rejected")]
		[InlineData("#Schema after the tables stream, decoys first", "DDDD", "DDDD")]
		[InlineData("#Schema before the tables stream", "RRRR", "rejected")]
		[InlineData("#JTD", "????", "rejected")]
		[InlineData("#jtd", "????", "rejected")]
		public void UncompressedMetadata(string change, string expectedDnlib, string expectedRuntime) =>
			AssertRead(CreateUncompressedImage(change), expectedDnlib, expectedRuntime);
	}
}
