// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The Mono runtime reads metadata with other rules than the CLR, and dnlib reads a file like Mono does if
	/// <see cref="ModuleCreationOptions.Runtime"/> is <see cref="CLRRuntimeReaderKind.Mono"/>. These tests read
	/// the files of <see cref="ClrMetadataStreamRuleTests"/> (other metadata streams and headers) in that way,
	/// and a small program that the <c>mono</c> command runs reads the same four values. If Mono reads the
	/// values, dnlib must read the same ones.
	/// </summary>
	/// <remarks>
	/// There are two Mono runtimes: the one that is part of .NET, and the older one that the <c>mono</c> command
	/// is (version 6). dnlib does what .NET's Mono does: a <c>#JTD</c> stream means that all columns are big.
	/// The older Mono doesn't know that stream ("Unknown heap type") and reads the tables as they are: the
	/// three cases with a <c>#JTD</c> stream are the only ones where it reads values and dnlib reads other ones.
	///
	/// Mono is only asked by the tests for .NET Framework (the files then reference mscorlib) and only if the
	/// <c>mono</c> command exists. Every file needs a new process: Mono stops the process if it doesn't like
	/// the metadata.
	/// </remarks>
	public sealed class MonoMetadataStreamRuleTests : IClassFixture<MonoMetadataStreamRuleTests.Fixture> {
		// Prints "<result>" and then the name of the type with the row number of the argument, the string that its method returns,
		// the value of its constant and the id of the module, or "rejected" if the file can't be loaded
		const string ProgramSource = @"
using System;
using System.IO;
using System.Linq;
using System.Reflection;
static class Program {
	static int Main(string[] args) {
		try {
			var module = Assembly.LoadFile(args[0]).ManifestModule;
			var type = module.ResolveType(0x02000000 + int.Parse(args[1]));
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
			var text = (string)type.GetMethods(flags).Single(a => a.ReturnType == typeof(string)).Invoke(null, null);
			var number = type.GetFields(flags).Single(a => a.IsLiteral).GetRawConstantValue();
			Console.Write(""<result>"" + type.Name + ""\n"" + text + ""\n"" + number + ""\n"" + module.ModuleVersionId);
		}
		catch (BadImageFormatException) {
			Console.Write(""<result>rejected"");
		}
		catch (FileLoadException) {
			Console.Write(""<result>rejected"");
		}
		return 0;
	}
}
";

		/// <summary>
		/// The directory with the program and the files that it reads
		/// </summary>
		public sealed class Fixture : IDisposable {
			public TempDirectory Directory { get; } = new TempDirectory();
			string? program;
			int files;

			public string Program {
				get {
					if (program is null) {
						var image = CSharpCompiler.Compile(ProgramSource, new CSharpCompilerOptions { AssemblyName = "ReadWithMono", OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication }).Image;
						File.WriteAllBytes(Directory.GetPath("ReadWithMono.exe"), image);
						program = Directory.GetPath("ReadWithMono.exe");
					}
					return program;
				}
			}

			public string WriteFile(byte[] image) {
				var file = Directory.GetPath($"file{++files}.dll");
				File.WriteAllBytes(file, image);
				return file;
			}

			public void Dispose() => Directory.Dispose();
		}

		readonly Fixture fixture;

		public MonoMetadataStreamRuleTests(Fixture fixture) => this.fixture = fixture;

		// true if the tests can ask Mono
		static bool IsMonoAsked {
			get {
#if NETFRAMEWORK
				return MonoRuntime.IsAvailable;
#else
				return false;
#endif
			}
		}

		string ReadWithMono(byte[] image, uint? typeRid = null) {
			var output = MonoRuntime.Run(fixture.Program, $"\"{fixture.WriteFile(image)}\" {typeRid ?? ClrMetadataStreamRuleTests.markerRid}");
			if (output is null)
				return "stops the process";
			// Mono writes warnings about the metadata to the output too
			output = output.Substring(output.LastIndexOf("<result>") + "<result>".Length);
			if (output == "rejected")
				return output;
			var values = output.Split('\n');
			return ClrMetadataStreamRuleTests.Describe(values[0], values[1], int.TryParse(values[2], out int number) ? number : null, new Guid(values[3]));
		}

		// "kind: what the test changes in the metadata". The kinds are the test methods of ClrMetadataStreamRuleTests.
		static byte[] CreateImage(string kindAndChange) {
			int index = kindAndChange.IndexOf(": ");
			string kind = kindAndChange.Substring(0, index), change = kindAndChange.Substring(index + 2);
			switch (kind) {
			case "header":
				return ClrMetadataStreamRuleTests.CreateImageWithAnotherHeader(change);
			case "header, uncompressed":
				var block = new MetadataBlock(ClrMetadataStreamRuleTests.CreateImageWithAnotherHeader(change));
				block.GetStream("#~").Name = "#-";
				return block.Write();
			case "streams":
				return ClrMetadataStreamRuleTests.CreateImage(change);
			case "streams, uncompressed":
				return ClrMetadataStreamRuleTests.CreateUncompressedImage(change);
			default:
				throw new ArgumentException(kind);
			}
		}

		// The values: R = the real value, D = the value of a decoy stream, ? = another value
		[Theory]
		// MetadataAndTablesHeader
		[InlineData("header: extra data after the row counts", "????", "stops the process")]
		[InlineData("header: extra data flag without the data", "RRRR", "RRRR")]
		[InlineData("header: row count of a table that doesn't exist", "RRRR", "RRRR")]
		[InlineData("header: row count of a table of a portable PDB", "RRRR", "RRRR")]
		[InlineData("header: padding flag", "RRRR", "RRRR")]
		[InlineData("header: delta only flag", "RRRR", "RRRR")]
		[InlineData("header: has delete flag", "RRRR", "RRRR")]
		[InlineData("header: tables version 1.0", "RRRR", "RRRR")]
		[InlineData("header: tables version 1.0 with generic tables", "RRRR", "RRRR")]
		[InlineData("header: tables version 1.1", "RRRR", "RRRR")]
		[InlineData("header: tables version 3.0", "RRRR", "RRRR")]
		[InlineData("header: metadata version 1.0", "RRRR", "RRRR")]
		[InlineData("header: metadata version 2.0", "RRRR", "RRRR")]
		[InlineData("header: storage flags that aren't zero", "RRRR", "RRRR")]
		[InlineData("header: storage flag 'extra data' and 4 bytes of extra data", "rejected", "stops the process")]
		[InlineData("header: reserved fields that aren't zero", "RRRR", "RRRR")]
		[InlineData("header: strings heap that doesn't end with a zero byte", "RRRR", "RRRR")]
		[InlineData("header: name at the end of the strings heap without a zero byte", "RRRR", "RRRR")]
		[InlineData("header: heap that's bigger than the metadata", "RRRR", "RRRR")]
		[InlineData("header: unknown stream that's bigger than the metadata", "RRRR", "RRRR")]
		[InlineData("header: unknown stream that isn't in the metadata", "RRRR", "RRRR")]
		[InlineData("header: tables stream that's shorter than its tables", "RRRR", "RRRR")]
		[InlineData("header: tables stream that's longer than its tables", "RRRR", "RRRR")]
		[InlineData("header: tables stream that ends in its header", "RRRR", "RRRR")]
		[InlineData("header: metadata size that's too small for the last stream", "RRRR", "RRRR")]
		[InlineData("header: metadata size that's bigger than the metadata", "RRRR", "RRRR")]
		[InlineData("header: metadata size that ends after the stream headers", "RRRR", "RRRR")]
		[InlineData("header: version string without a zero byte", "RRRR", "RRRR")]
		[InlineData("header: version string with 0 bytes", "RRRR", "RRRR")]
		[InlineData("header: stream name with 31 characters", "RRRR", "RRRR")]
		[InlineData("header: stream name with 32 characters", "RRRR", "RRRR")]
		[InlineData("header: more streams in the header than there are", "RRRR", "RRRR")]
		[InlineData("header: fewer streams in the header than there are", "R??R", "stops the process")]
		// MetadataAndTablesHeaderOfUncompressedMetadata
		[InlineData("header, uncompressed: extra data after the row counts", "????", "stops the process")]
		[InlineData("header, uncompressed: row count of a table that doesn't exist", "RRRR", "RRRR")]
		[InlineData("header, uncompressed: delta only flag", "RRRR", "RRRR")]
		[InlineData("header, uncompressed: tables version 1.0", "RRRR", "RRRR")]
		[InlineData("header, uncompressed: tables version 1.0 with generic tables", "RRRR", "RRRR")]
		[InlineData("header, uncompressed: tables version 3.0", "RRRR", "RRRR")]
		[InlineData("header, uncompressed: metadata version 2.0", "RRRR", "RRRR")]
		[InlineData("header, uncompressed: storage flags that aren't zero", "RRRR", "RRRR")]
		[InlineData("header, uncompressed: strings heap that doesn't end with a zero byte", "RRRR", "RRRR")]
		// CompressedMetadata
		[InlineData("streams: nothing", "RRRR", "RRRR")]
		[InlineData("streams: decoys first", "RRRR", "RRRR")]
		[InlineData("streams: decoys last", "DDDD", "DDDD")]
		[InlineData("streams: decoys with lower case names last", "RRRR", "RRRR")]
		[InlineData("streams: decoys with lower case names first", "RRRR", "RRRR")]
		[InlineData("streams: unknown stream", "RRRR", "RRRR")]
		[InlineData("streams: stream without a name", "RRRR", "RRRR")]
		[InlineData("streams: stream with one char", "RRRR", "RRRR")]
		[InlineData("streams: #JTD", "????", "RRRR")]
		[InlineData("streams: #Pdb", "RRRR", "RRRR")]
		[InlineData("streams: two tables streams", "RRRR", "RRRR")]
		[InlineData("streams: tables stream and an uncompressed tables stream", "RRRR", "RRRR")]
		[InlineData("streams: #Schema after the tables stream, decoys last", "DDDD", "DDDD")]
		[InlineData("streams: #Schema after the tables stream, decoys first", "RRRR", "RRRR")]
		[InlineData("streams: #Schema after the tables stream, decoys with lower case names first", "RRRR", "RRRR")]
		[InlineData("streams: #Schema and #JTD after the tables stream", "????", "RRRR")]
		[InlineData("streams: #Schema before the tables stream", "RRRR", "RRRR")]
		[InlineData("streams: #schema after the tables stream, decoys first", "RRRR", "RRRR")]
		[InlineData("streams: heaps with upper case names", "???R", "stops the process")]
		// UncompressedMetadata
		[InlineData("streams, uncompressed: nothing", "RRRR", "RRRR")]
		[InlineData("streams, uncompressed: decoys first", "RRRR", "RRRR")]
		[InlineData("streams, uncompressed: decoys last", "DDDD", "DDDD")]
		[InlineData("streams, uncompressed: decoys with lower case names last", "RRRR", "RRRR")]
		[InlineData("streams, uncompressed: decoys with lower case names first", "RRRR", "RRRR")]
		[InlineData("streams, uncompressed: heaps with upper case names", "???R", "stops the process")]
		[InlineData("streams, uncompressed: unknown stream", "RRRR", "RRRR")]
		[InlineData("streams, uncompressed: stream without a name", "RRRR", "RRRR")]
		[InlineData("streams, uncompressed: #Pdb", "RRRR", "RRRR")]
		[InlineData("streams, uncompressed: two tables streams", "RRRR", "RRRR")]
		[InlineData("streams, uncompressed: #Schema after the tables stream, decoys first", "RRRR", "RRRR")]
		[InlineData("streams, uncompressed: #Schema before the tables stream", "RRRR", "RRRR")]
		[InlineData("streams, uncompressed: #JTD", "????", "RRRR")]
		[InlineData("streams, uncompressed: #jtd", "RRRR", "RRRR")]
		public void MetadataStreamsAndHeaders(string change, string expectedDnlib, string expectedMono) {
			var image = CreateImage(change);
			string dnlib = ClrMetadataStreamRuleTests.ReadWithDnlib(image, CLRRuntimeReaderKind.Mono);

			Assert.Equal(expectedDnlib, dnlib);
			if (IsMonoAsked)
				Assert.Equal(expectedMono, ReadWithMono(image));
		}

		// The same type as in the other files, and a second constant: the Constant table has two rows
		const string Source = @"
public static class Marker {
	public const int Number = 0x11223344;
	public static string Text() => ""UserString"";
}
public static class Second {
	public const int Other = 0x55667788;
}
";
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source);
		static readonly uint markerRid = new MetadataPatcher(baseImage).FindRow(DotNet.MD.Table.TypeDef, "Name", "Marker");

		static byte[] SetUInt32(byte[] image, long offset, uint value) =>
			new MetadataPatcher(image).SetBytes(offset, BitConverter.GetBytes(value)).ToArray();

		// The changes, separated by "; ":
		//	size of #Name = number				the size of a stream in its header
		//	rows of Table |= number				sets bits of the number of rows of a table in the tables header
		//	sorted tables = number				the bits of the tables header that say which tables are sorted
		//	constants reversed					the two rows of the Constant table in the other order
		//	.NET header size = number			the size in the .NET header (it's 0x48)
		//	metadata size = number				the size of the metadata in the .NET header
		static byte[] CreateImageWithOtherSizes(string changes) {
			var image = baseImage;
			foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
				var parts = change.Split(new[] { " |= ", " = " }, StringSplitOptions.None);
				uint value = parts.Length == 2 ? (parts[1].StartsWith("0x") ? Convert.ToUInt32(parts[1], 16) : uint.Parse(parts[1])) : 0;
				var patcher = new MetadataPatcher(image);
				if (parts[0].StartsWith("size of "))
					image = patcher.SetStreamSize(parts[0].Substring(8), value).ToArray();
				else if (parts[0].StartsWith("rows of ")) {
					var table = (DotNet.MD.Table)Enum.Parse(typeof(DotNet.MD.Table), parts[0].Substring(8));
					image = patcher.SetRowCount(table, patcher.GetRowCount(table) | value).ToArray();
				}
				else if (parts[0] == "sorted tables")
					// The tables header is 24 bytes, the last 8 are the bits of the sorted tables
					image = patcher.SetBytes(patcher.GetStreamFileOffset("#~") + 16, BitConverter.GetBytes((ulong)value)).ToArray();
				else if (parts[0] == "constants reversed") {
					foreach (var column in patcher.GetColumnNames(DotNet.MD.Table.Constant)) {
						uint first = patcher.GetCell(DotNet.MD.Table.Constant, 1, column);
						patcher.SetCell(DotNet.MD.Table.Constant, 1, column, patcher.GetCell(DotNet.MD.Table.Constant, 2, column));
						patcher.SetCell(DotNet.MD.Table.Constant, 2, column, first);
					}
					image = patcher.ToArray();
				}
				else {
					using (var peImage = new dnlib.PE.PEImage(image)) {
						// The .NET header starts with its size. The address and the size of the metadata are at offset 8.
						var directory = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
						long headerOffset = (long)peImage.ToFileOffset(directory.VirtualAddress);
						if (parts[0] == ".NET header size")
							image = SetUInt32(image, headerOffset, value);
						else if (parts[0] == "metadata size")
							image = SetUInt32(image, headerOffset + 12, value);
						else
							throw new ArgumentException(change);
					}
				}
			}
			return image;
		}

		// Sizes and numbers that Mono doesn't use or uses in another way than the CLR. The last value of the CLR
		// column is what dnlib reads if it reads the file like the CLR.
		[Theory]
		[InlineData("", "RRRR", "RRRR", "RRRR")]
		// Mono doesn't use the size of a stream
		[InlineData("size of #Strings = 0", "RRRR", "stops the process", "?RRR")]
		[InlineData("size of #Strings = 0xFFFFFFFF", "RRRR", "RRRR", "?RRR")]
		[InlineData("size of #US = 0", "RRRR", "stops the process", "R?RR")]
		[InlineData("size of #Blob = 0", "RRRR", "stops the process", "R??R")]
		[InlineData("size of #Blob = 0xFFFFFFFF", "RRRR", "RRRR", "R??R")]
		[InlineData("size of #GUID = 0", "RRRR", "stops the process", "RRR?")]
		[InlineData("size of #~ = 0", "RRRR", "RRRR", "rejected")]
		[InlineData("size of #~ = 24", "RRRR", "RRRR", "rejected")]
		[InlineData("size of #~ = 0xFFFFFFFF", "RRRR", "RRRR", "rejected")]
		[InlineData("size of #Strings = 0; size of #US = 0; size of #Blob = 0; size of #GUID = 0; size of #~ = 0", "RRRR", "stops the process", "rejected")]
		// Mono only uses 24 bits of a number of rows
		[InlineData("rows of TypeDef |= 0x01000000", "RRRR", "RRRR", "RRRR")]
		[InlineData("rows of TypeDef |= 0xFF000000", "RRRR", "RRRR", "RRRR")]
		[InlineData("rows of Constant |= 0x80000000", "RRRR", "RRRR", "RRRR")]
		[InlineData("rows of Module |= 0x01000000; rows of TypeRef |= 0x01000000; rows of Field |= 0x01000000; rows of Method |= 0x01000000", "RRRR", "RRRR", "RRRR")]
		[InlineData("rows of TypeDef |= 0x00010000", "???R", "stops the process", "???R")]
		// Mono does a binary search in every table that must be sorted, whatever the header says
		[InlineData("sorted tables = 0", "RRRR", "RRRR", "RRRR")]
		[InlineData("sorted tables = 0xFFFFFFFF", "RRRR", "RRRR", "RRRR")]
		[InlineData("constants reversed", "RRRR", "RRRR", "RR?R")]
		[InlineData("constants reversed; sorted tables = 0", "RRRR", "RRRR", "RR?R")]
		// The sizes in the .NET header
		[InlineData(".NET header size = 0", "RRRR", "RRRR", "rejected")]
		[InlineData(".NET header size = 0x47", "RRRR", "RRRR", "rejected")]
		[InlineData(".NET header size = 0xFFFFFFFF", "RRRR", "RRRR", "RRRR")]
		[InlineData("metadata size = 0", "RRRR", "RRRR", "RRRR")]
		[InlineData("metadata size = 16", "RRRR", "RRRR", "RRRR")]
		[InlineData("metadata size = 0xFFFFFFFF", "RRRR", "RRRR", "RRRR")]
		public void SizesAndRowCounts(string changes, string expectedDnlib, string expectedMono, string expectedDnlibLikeTheClr) {
			var image = CreateImageWithOtherSizes(changes);
			string dnlib = ClrMetadataStreamRuleTests.ReadWithDnlib(image, CLRRuntimeReaderKind.Mono, markerRid);
			string dnlibLikeTheClr = ClrMetadataStreamRuleTests.ReadWithDnlib(image, CLRRuntimeReaderKind.CLR, markerRid);

			Assert.Equal(expectedDnlib, dnlib);
			Assert.Equal(expectedDnlibLikeTheClr, dnlibLikeTheClr);
			if (IsMonoAsked)
				Assert.Equal(expectedMono, ReadWithMono(image, markerRid));
		}
	}
}
