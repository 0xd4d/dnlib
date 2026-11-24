// dnlib: See LICENSE.txt for more info

using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Pdb.Dss;
using dnlib.DotNet.Writer;
using dnlib.PE;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// A Windows PDB gives every method an address, and the sequence points of a method are stored with that
	/// address only (not with the method's token). The address of a method is the sum of the code sizes of the
	/// methods before it, so normally every method has its own address. If a method's code size is 0, the next
	/// method gets the same address, and a reader can't know whose sequence points it's reading.
	///
	/// No compiler creates such a PDB. The tests create one by calling the native PDB writer directly (dnlib's
	/// own writer always passes the real code size), and add its debug directory entry to a file that dnlib
	/// writes. They only run on Windows, where the native writer exists.
	/// </summary>
	public class WindowsPdbSameAddressTests {
		const string Source = @"
public class Program {
	public static int Method0() { return 0; }
	public static int Method1() { return 1; }
	public static int Method2() { return 2; }
	public static int Method3() { return 3; }
	public static int Method4() { return 4; }
	public static int Method5() { return 5; }
	public static int Method6() { return 6; }
}";
		const int MethodCount = 7;

		// The line of the only sequence point (at IL offset 0) of a method in the PDB
		static int GetLine(int methodIndex) => 100 + methodIndex;

		/// <summary>
		/// Creates a file and its PDB. Method N is written to the PDB with a code size of
		/// <paramref name="codeSizes"/>[N] and has one sequence point, at line <see cref="GetLine"/>.
		/// </summary>
		static CompiledAssembly WriteFileAndPdb(int[] codeSizes) {
			var image = new MemoryStream();
			var pdb = new MemoryStream();
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image)) {
				var program = module.Find("Program", isReflectionName: true)!;
				var options = new ModuleWriterOptions(module);
				// The writer only has a debug directory if it writes a PDB or if the file is deterministic
				options.PdbOptions = PdbWriterOptions.Deterministic;
				options.WriterEvent += (sender, e) => {
					if (e.Event != ModuleWriterEvent.BeginWritePdb)
						return;
					// The tests skip themselves if this isn't Windows
#pragma warning disable CA1416
					var symbolWriter = SymbolReaderWriterFactory.Create(PdbWriterOptions.None, pdb, "test.pdb")!;
#pragma warning restore CA1416
					using (symbolWriter) {
						symbolWriter.Initialize(e.Writer.Metadata);
						var document = symbolWriter.DefineDocument("test.cs", PdbDocumentConstants.LanguageCSharp, PdbDocumentConstants.LanguageVendorMicrosoft, PdbDocumentConstants.DocumentTypeText)!;
						for (int i = 0; i < codeSizes.Length; i++) {
							uint rid = e.Writer.Metadata.GetRid(program.FindMethod("Method" + i)!);
							symbolWriter.OpenMethod(new MDToken(dnlib.DotNet.MD.Table.Method, rid));
							symbolWriter.OpenScope(0);
							symbolWriter.DefineSequencePoints(document, 1, offsets: new[] { 0 }, lines: new[] { GetLine(i) }, columns: new[] { 1 }, endLines: new[] { GetLine(i) }, endColumns: new[] { 10 });
							// The end of the outermost scope is the method's code size
							symbolWriter.CloseScope(codeSizes[i]);
							symbolWriter.CloseMethod();
						}

						// The file gets the debug directory entry of this PDB so the readers accept the PDB
						uint age = 1;
						symbolWriter.GetDebugInfo(ChecksumAlgorithm.SHA256, ref age, out _, out uint timeDateStamp, out _, out var codeViewData);
						e.Writer.DebugDirectory!.Add(codeViewData!, ImageDebugType.CodeView, majorVersion: 0, minorVersion: 0, timeDateStamp);
						symbolWriter.Close();
					}
				};
				module.Write(image, options);
			}
			return new CompiledAssembly(image.ToArray(), pdb.ToArray());
		}

		// The lines of the sequence points of every method, eg. "100 101,102 -" if the 1st method has one
		// sequence point, the 2nd has two and the 3rd has none
		static string ReadLines(CompiledAssembly written, PdbReaderOptions readerOptions) {
			using (var module = ModuleDefMD.Load(written.Image, new ModuleCreationOptions {
				PdbFileOrData = written.Pdb,
				PdbOptions = readerOptions,
				TryToLoadPdbFromDisk = false,
			})) {
				Assert.NotNull(module.PdbState);
				var program = module.Find("Program", isReflectionName: true)!;
				var lines = Enumerable.Range(0, MethodCount).Select(i => {
					var sequencePoints = program.FindMethod("Method" + i)!.Body!.Instructions.Where(a => a.SequencePoint is not null).ToList();
					// All sequence points in the PDB are at offset 0
					Assert.All(sequencePoints, a => Assert.Equal(0U, a.Offset));
					return sequencePoints.Count == 0 ? "-" : string.Join(",", sequencePoints.Select(a => a.SequencePoint!.StartLine));
				});
				return string.Join(" ", lines);
			}
		}

		static void SkipUnlessWindows() =>
			Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "A Windows PDB can only be created on Windows");

		const string LinesOfEveryMethod = "100 101 102 103 104 105 106";

		// 836482c7 made the order of functions with the same address the order they have in the PDB, but the reader
		// still gave only some of them their sequence points (the first one, sometimes also the last one). Now the
		// 1st sequence points with an address are the 1st method's, the 2nd are the 2nd method's, etc.
		[Theory]
		[InlineData(new[] { 1, 2, 3, 4, 5, 6, 7 })]    // every method has its own address
		[InlineData(new[] { 0, 2, 2, 2, 0, 2, 2 })]    // two pairs of methods
		[InlineData(new[] { 0, 0, 0, 5, 0, 0, 3 })]    // four methods at address 0, three at address 5
		[InlineData(new[] { 0, 0, 0, 0, 0, 0, 0 })]    // all methods at address 0
		public void ManagedReaderGivesEveryMethodItsOwnSequencePoints(int[] codeSizes) {
			SkipUnlessWindows();
			var written = WriteFileAndPdb(codeSizes);

			Assert.Equal(LinesOfEveryMethod, ReadLines(written, PdbReaderOptions.None));
		}

		// The native reader finds sequence points by address, so it can't return the right ones for all methods.
		// It failed for every method if all of them had the address 0, and reading a method body threw.
		[Theory]
		[InlineData(new[] { 1, 2, 3, 4, 5, 6, 7 }, LinesOfEveryMethod)]
		[InlineData(new[] { 0, 0, 0, 5, 0, 0, 3 }, "103 103 103 103 106 106 106")]
		[InlineData(new[] { 0, 0, 0, 0, 0, 0, 0 }, "- - - - - - -")]
		public void ComReaderReadsWhatTheNativeReaderReturns(int[] codeSizes, string expectedLines) {
			SkipUnlessWindows();
			var written = WriteFileAndPdb(codeSizes);

			Assert.Equal(expectedLines, ReadLines(written, PdbReaderOptions.MicrosoftComReader));
		}
	}
}
