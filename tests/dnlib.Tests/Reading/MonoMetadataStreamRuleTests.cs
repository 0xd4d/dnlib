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

		string ReadWithMono(byte[] image) {
			var output = MonoRuntime.Run(fixture.Program, $"\"{fixture.WriteFile(image)}\" {ClrMetadataStreamRuleTests.markerRid}");
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
	}
}
