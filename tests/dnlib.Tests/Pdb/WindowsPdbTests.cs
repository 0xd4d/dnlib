// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// Windows PDB files (the format before portable PDBs). dnlib has two readers: one in C# that works
	/// everywhere, and one that uses the native Microsoft.DiaSymReader.Native library through COM. Its writer
	/// always uses that library.
	///
	/// The tests need a Windows PDB as input. Only the C# compiler on Windows can create one (it uses the same
	/// native library), so all these tests only run on Windows.
	/// </summary>
	public class WindowsPdbTests {
		static void SkipUnlessWindows() =>
			Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "A Windows PDB can only be created on Windows");

		static CompiledAssembly Portable => Fixtures.Fixtures.Features;
		static CompiledAssembly Windows => Fixtures.Fixtures.FeaturesWithWindowsPdb;

		static ModuleDefMD Load(CompiledAssembly assembly, PdbReaderOptions options = PdbReaderOptions.None) {
			var module = ModuleDefMD.Load(assembly.Image, new ModuleCreationOptions {
				PdbFileOrData = assembly.Pdb,
				PdbOptions = options,
				TryToLoadPdbFromDisk = false,
			});
			Assert.NotNull(module.PdbState);
			return module;
		}

		// The debug info that a Windows PDB and a portable PDB of the same code store in the same way: where each
		// statement is in the source code, and the names of the local variables and constants in each scope.
		// Imports and custom debug info are stored in other ways and aren't in this text.
		static string GetSourceLevelDebugInfo(ModuleDef module) {
			var text = new StringBuilder();
			foreach (var method in module.GetTypes().SelectMany(a => a.Methods)) {
				var body = method.Body;
				if (body is null)
					continue;
				text.AppendLine(method.FullName);
				var indexes = new Dictionary<dnlib.DotNet.Emit.Instruction, int>();
				for (int i = 0; i < body.Instructions.Count; i++) {
					indexes[body.Instructions[i]] = i;
					var sequencePoint = body.Instructions[i].SequencePoint;
					if (sequencePoint is not null)
						text.AppendLine($"  #{i}: {Path.GetFileName(sequencePoint.Document.Url)} {sequencePoint.StartLine},{sequencePoint.StartColumn}-{sequencePoint.EndLine},{sequencePoint.EndColumn}");
				}
				foreach (var local in body.Variables.Where(a => a.Name is not null).OrderBy(a => a.Index))
					text.AppendLine($"  local {local.Index} {local.Name}");
				if (body.PdbMethod?.Scope is PdbScope rootScope)
					AppendScope(text, rootScope, indexes, "  ");
			}
			return text.ToString();
		}

		static void AppendScope(StringBuilder text, PdbScope scope, Dictionary<dnlib.DotNet.Emit.Instruction, int> indexes, string indent) {
			string Index(dnlib.DotNet.Emit.Instruction? instruction) => instruction is null ? "end" : "#" + indexes[instruction];
			// A scope without locals and constants isn't written, and the scopes aren't indented by their depth:
			// a Windows PDB has one more scope around a method's scopes than a portable PDB
			if (scope.Variables.Count != 0 || scope.Constants.Count != 0) {
				text.AppendLine($"{indent}scope {Index(scope.Start)}-{Index(scope.End)}");
				foreach (var local in scope.Variables.OrderBy(a => a.Index))
					text.AppendLine($"{indent}  local {local.Index} {local.Name}");
				foreach (var constant in scope.Constants.OrderBy(a => a.Name))
					text.AppendLine($"{indent}  constant {constant.Name} = {constant.Value ?? "null"} ({constant.Value?.GetType().Name})");
			}
			foreach (var child in scope.Scopes)
				AppendScope(text, child, indexes, indent);
		}

		[Fact]
		public void CompilerCreatesAWindowsPdb() {
			SkipUnlessWindows();

			// A Windows PDB is an MSF file
			Assert.StartsWith("Microsoft C/C++ MSF 7.00", Encoding.ASCII.GetString(Windows.Pdb!, 0, 30));
		}

		[Fact]
		public void ManagedReaderReadsTheSameAsThePortablePdbReader() {
			SkipUnlessWindows();
			using (var portable = Load(Portable))
			using (var windows = Load(Windows)) {
				Assert.Equal(PdbFileKind.WindowsPDB, windows.PdbState!.PdbFileKind);
				Assert.Equal(GetSourceLevelDebugInfo(portable), GetSourceLevelDebugInfo(windows));
			}
		}

		[Fact]
		public void ComReaderReadsTheSameAsTheManagedReader() {
			SkipUnlessWindows();
			using (var managed = Load(Windows))
			using (var com = Load(Windows, PdbReaderOptions.MicrosoftComReader))
				ModuleComparer.AssertEqual(managed, com, new ModuleDumpOptions { Pdb = true, MaxStack = true });
		}

		static CompiledAssembly Write(ModuleDefMD module, PdbFileKind pdbKind, ILogger? logger = null) {
			module.PdbState!.PdbFileKind = pdbKind;
			var options = new ModuleWriterOptions(module) {
				WritePdb = true,
				PdbStream = new MemoryStream(),
				PdbFileName = "written.pdb",
			};
			if (logger is not null)
				options.Logger = logger;
			var image = new MemoryStream();
			module.Write(image, options);
			return new CompiledAssembly(image.ToArray(), ((MemoryStream)options.PdbStream).ToArray());
		}

		[Fact]
		public void WindowsPdbSurvivesAWrite() {
			SkipUnlessWindows();
			using (var original = Load(Windows)) {
				var written = Write(original, PdbFileKind.WindowsPDB);

				Assert.StartsWith("Microsoft C/C++ MSF 7.00", Encoding.ASCII.GetString(written.Pdb!, 0, 30));
				using (var reloaded = Load(written))
					ModuleComparer.AssertEqual(original, reloaded, new ModuleDumpOptions { Pdb = true });
				Assert.Equal(0, AssemblyRunner.RunEntryPoint(written.Image));
			}
		}

		// The debug build of DebugInfoSource has custom debug info that a release build doesn't have: the
		// edit-and-continue maps, and the dynamic flags and the tuple element names of local variables
		[Theory]
		[InlineData(PdbReaderOptions.None)]
		[InlineData(PdbReaderOptions.MicrosoftComReader)]
		public void WindowsPdbOfADebugBuildSurvivesAWrite(PdbReaderOptions readerOptions) {
			SkipUnlessWindows();
			using (var original = Load(DebugInfoSource.CompiledWithWindowsPdb, readerOptions)) {
				var kinds = PortablePdbReaderTests.GetMethodCustomDebugInfoKinds(original);
				Assert.Contains(PdbCustomDebugInfoKind.DynamicLocals, kinds);
				Assert.Contains(PdbCustomDebugInfoKind.TupleElementNames, kinds);
				Assert.Contains(PdbCustomDebugInfoKind.EditAndContinueLocalSlotMap, kinds);

				var written = Write(original, PdbFileKind.WindowsPDB);

				using (var reloaded = Load(written, readerOptions))
					ModuleComparer.AssertEqual(original, reloaded, new ModuleDumpOptions { Pdb = true });
			}
		}

		// Each format has custom debug info that the other one can't store (eg. the info about iterator methods
		// only exists in portable PDBs). The writer reports it as an error, which throws unless the logger
		// ignores errors. The rest is converted.
		[Theory, InlineData(PdbFileKind.WindowsPDB), InlineData(PdbFileKind.PortablePDB)]
		public void WritingAsTheOtherFormatReportsCustomDebugInfoItCanNotStore(PdbFileKind targetKind) {
			SkipUnlessWindows();
			using (var original = Load(targetKind == PdbFileKind.WindowsPDB ? Portable : Windows))
				Assert.Throws<ModuleWriterException>(() => Write(original, targetKind));
		}

		[Fact]
		public void PortablePdbCanBeWrittenAsAWindowsPdb() {
			SkipUnlessWindows();
			using (var original = Load(Portable)) {
				var expected = GetSourceLevelDebugInfo(original);

				var written = Write(original, PdbFileKind.WindowsPDB, DummyLogger.NoThrowInstance);

				using (var reloaded = Load(written)) {
					Assert.Equal(PdbFileKind.WindowsPDB, reloaded.PdbState!.PdbFileKind);
					Assert.Equal(expected, GetSourceLevelDebugInfo(reloaded));
				}
			}
		}

		[Fact]
		public void WindowsPdbCanBeWrittenAsAPortablePdb() {
			SkipUnlessWindows();
			using (var original = Load(Windows)) {
				var expected = GetSourceLevelDebugInfo(original);

				var written = Write(original, PdbFileKind.PortablePDB, DummyLogger.NoThrowInstance);

				using (var reloaded = Load(written)) {
					Assert.Equal(PdbFileKind.PortablePDB, reloaded.PdbState!.PdbFileKind);
					Assert.Equal(expected, GetSourceLevelDebugInfo(reloaded));
				}
			}
		}

		// The values and the types of the fixture's constants. A Windows PDB stores them in other ways than a
		// portable PDB: eg. a bool as a 16-bit number, and an empty string and a null string as the same value.
		// Fixed in 773b54ae (strings), ac3edc8c (the COM reader returned a double as a date) and 4edcb0fb (enums).
		[Theory]
		[InlineData(PdbReaderOptions.None)]
		[InlineData(PdbReaderOptions.MicrosoftComReader)]
		public void ConstantsHaveTheirValuesAndTypes(PdbReaderOptions readerOptions) {
			SkipUnlessWindows();
			using (var module = Load(Windows, readerOptions)) {
				var rootScope = module.Find("Features.Code", isReflectionName: true)!.FindMethod("Locals")!.Body!.PdbMethod!.Scope!;
				var constants = GetScopes(rootScope).SelectMany(a => a.Constants).ToDictionary(a => a.Name!, a => a.Value);

				Assert.Equal(3, constants["Factor"]);
				Assert.Equal("label", constants["Label"]);
				Assert.Equal(0.5, constants["Half"]);
				Assert.Equal((byte)2, constants["Favorite"]);
				Assert.Null(constants["NoObject"]);
				Assert.Equal("", constants["Empty"]);
				Assert.Null(constants["NoText"]);
				Assert.Equal(1E+300, constants["Huge"]);
				Assert.Equal(0.25f, constants["Quarter"]);
				Assert.Equal(long.MinValue, constants["Lowest"]);
				Assert.Equal(ulong.MaxValue, constants["Highest"]);
				Assert.Equal('x', constants["Letter"]);
				Assert.Equal(true, constants["Yes"]);
				Assert.Equal((sbyte)-5, constants["Small"]);
				Assert.Equal((short)-300, constants["Medium"]);
				Assert.Equal(12.5m, constants["Price"]);
			}
		}

		static IEnumerable<PdbScope> GetScopes(PdbScope scope) {
			yield return scope;
			foreach (var child in scope.Scopes) {
				foreach (var nested in GetScopes(child))
					yield return nested;
			}
		}

		// Fixed in 0ea1d899: the COM PDB writer got the whole stream, tried to read the data in it as a PDB and
		// threw. Like a portable PDB, the PDB is written at the stream's position.
		[Fact]
		public void WindowsPdbIsWrittenAtTheStreamsPosition() {
			SkipUnlessWindows();
			using (var module = Load(Windows)) {
				var stream = new MemoryStream();
				var dataBeforePdb = Enumerable.Repeat((byte)0xAA, 10).ToArray();
				stream.Write(dataBeforePdb, 0, dataBeforePdb.Length);
				// Old data that's longer than the PDB
				stream.Write(new byte[Windows.Pdb!.Length * 3], 0, Windows.Pdb.Length * 3);
				stream.Position = dataBeforePdb.Length;
				var options = new ModuleWriterOptions(module) { WritePdb = true, PdbStream = stream, PdbFileName = "written.pdb" };
				var image = new MemoryStream();

				module.Write(image, options);

				var all = stream.ToArray();
				Assert.Equal(dataBeforePdb, all.Take(dataBeforePdb.Length));
				Assert.Equal(stream.Length, stream.Position);
				var written = new CompiledAssembly(image.ToArray(), all.Skip(dataBeforePdb.Length).ToArray());
				Assert.StartsWith("Microsoft C/C++ MSF 7.00", Encoding.ASCII.GetString(written.Pdb!, 0, 30));
				using (var reloaded = Load(written))
					Assert.Equal(GetSourceLevelDebugInfo(module), GetSourceLevelDebugInfo(reloaded));
			}
		}

		// Fixed in 2ad84703: the writer added a PdbMethod and a scope to every method that had none
		[Fact]
		public void WritingAWindowsPdbDoesNotChangeTheModule() {
			SkipUnlessWindows();
			using (var module = Load(Windows)) {
				var type = module.Find("Features.Code", isReflectionName: true)!;
				var withoutDebugInfo = new MethodDefUser("WithoutDebugInfo", MethodSig.CreateStatic(module.CorLibTypes.Void), dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static);
				withoutDebugInfo.Body = new dnlib.DotNet.Emit.CilBody();
				withoutDebugInfo.Body.Instructions.Add(dnlib.DotNet.Emit.OpCodes.Ret.ToInstruction());
				type.Methods.Add(withoutDebugInfo);
				var withoutScope = new MethodDefUser("WithoutScope", MethodSig.CreateStatic(module.CorLibTypes.Void), dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static);
				withoutScope.Body = new dnlib.DotNet.Emit.CilBody { PdbMethod = new PdbMethod() };
				withoutScope.Body.Instructions.Add(dnlib.DotNet.Emit.OpCodes.Ret.ToInstruction());
				type.Methods.Add(withoutScope);

				Write(module, PdbFileKind.WindowsPDB);

				Assert.Null(withoutDebugInfo.Body.PdbMethod);
				Assert.Null(withoutScope.Body.PdbMethod!.Scope);
			}
		}

		// Fixed in 95a1b808: two PdbDocument objects with the same name and checksum are the same document.
		// The sequence points of the second object weren't written.
		[Fact]
		public void SequencePointsOfEqualDocumentObjectsAreWritten() {
			SkipUnlessWindows();
			using (var module = Load(Windows)) {
				var expected = GetSourceLevelDebugInfo(module);
				var original = module.PdbState!.Documents.Single();
				var copy = new PdbDocument(original.Url, original.Language, original.LanguageVendor, original.DocumentType, original.CheckSumAlgorithmId, original.CheckSum);
				Assert.Equal(original, copy);
				// Every second sequence point uses the copy
				int count = 0;
				foreach (var instruction in module.GetTypes().SelectMany(a => a.Methods).Where(a => a.Body is not null).SelectMany(a => a.Body!.Instructions)) {
					if (instruction.SequencePoint is not null && count++ % 2 == 1)
						instruction.SequencePoint.Document = copy;
				}

				var written = Write(module, PdbFileKind.WindowsPDB);

				using (var reloaded = Load(written)) {
					Assert.Single(reloaded.PdbState!.Documents);
					Assert.Equal(expected, GetSourceLevelDebugInfo(reloaded));
				}
			}
		}

		// The PE file can have a checksum of the PDB file (a debug directory entry). dnlib doesn't write one
		// for a Windows PDB, also if the options ask for it (a TODO in ModuleWriterBase).
		[Fact]
		public void NoPdbChecksumIsWrittenForAWindowsPdb() {
			SkipUnlessWindows();
			using (var module = Load(Windows)) {
				var options = new ModuleWriterOptions(module) { WritePdb = true, PdbStream = new MemoryStream(), PdbFileName = "written.pdb" };
				options.PdbOptions |= PdbWriterOptions.Deterministic | PdbWriterOptions.PdbChecksum;
				var image = new MemoryStream();
				module.Write(image, options);

				using (var peImage = new dnlib.PE.PEImage(image.ToArray())) {
					Assert.Contains(peImage.ImageDebugDirectories, a => a.Type == dnlib.PE.ImageDebugType.CodeView);
					Assert.DoesNotContain(peImage.ImageDebugDirectories, a => a.Type == dnlib.PE.ImageDebugType.PdbChecksum);
				}
				// The reader has nothing to validate, so the PDB is accepted
				var creationOptions = new ModuleCreationOptions {
					PdbFileOrData = ((MemoryStream)options.PdbStream).ToArray(),
					PdbOptions = PdbReaderOptions.ValidatePdbChecksum,
					TryToLoadPdbFromDisk = false,
				};
				using (var written = ModuleDefMD.Load(image.ToArray(), creationOptions))
					Assert.NotNull(written.PdbState);
			}
		}

		// The source file and the line of every sequence point, in the order of the instructions
		static List<string> GetSequencePointDocuments(ModuleDef module) =>
			module.GetTypes().SelectMany(a => a.Methods).Where(a => a.Body is not null)
				.SelectMany(method => method.Body!.Instructions.Where(a => a.SequencePoint is not null)
					.Select(a => $"{method.FullName}: {a.SequencePoint!.Document.Url} line {a.SequencePoint.StartLine}"))
				.ToList();

		// The native writer wants the sequence points of a method one source file at a time, so the writer
		// sorts them by file. 27fd6c2c changed how (it looked at all instructions once for every file).
		// There's no test of the time it takes: the native library needs (sequence points x files) time too.
		[Fact]
		public void SequencePointsThatAlternateBetweenSourceFilesAreWritten() {
			SkipUnlessWindows();
			using (var module = Load(Windows)) {
				var original = module.PdbState!.Documents.Single();
				var documents = new List<PdbDocument> { original };
				foreach (var name in new[] { "Second.cs", "Third.cs" }) {
					var document = new PdbDocument(name, original.Language, original.LanguageVendor, original.DocumentType, original.CheckSumAlgorithmId, original.CheckSum);
					module.PdbState.Add(document);
					documents.Add(document);
				}
				// The first sequence point is in the first file, the next one in the second file etc
				int count = 0;
				foreach (var instruction in module.GetTypes().SelectMany(a => a.Methods).Where(a => a.Body is not null).SelectMany(a => a.Body!.Instructions)) {
					if (instruction.SequencePoint is not null)
						instruction.SequencePoint.Document = documents[count++ % documents.Count];
				}
				var expected = GetSequencePointDocuments(module);
				Assert.Contains(expected, a => a.Contains("Third.cs"));

				var written = Write(module, PdbFileKind.WindowsPDB);

				using (var reloaded = Load(written)) {
					Assert.Equal(3, reloaded.PdbState!.Documents.Count());
					Assert.Equal(expected, GetSequencePointDocuments(reloaded));
				}
			}
		}

		static readonly System.Lazy<CompiledAssembly> vbWithWindowsPdb = new System.Lazy<CompiledAssembly>(() =>
			VisualBasicCompiler.Compile(Fixtures.Fixtures.ReadSource("VbFeatures.vb"), new VisualBasicCompilerOptions {
				AssemblyName = "VbFeatures",
				OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication,
				PdbKind = PdbKind.Windows,
			}));

		// Fixed in aac496e7: only Visual Basic has Date constants. The managed reader ignored them.
		[Theory]
		[InlineData(PdbReaderOptions.None)]
		[InlineData(PdbReaderOptions.MicrosoftComReader)]
		public void VisualBasicDateAndDecimalConstantsAreRead(PdbReaderOptions readerOptions) {
			SkipUnlessWindows();
			using (var module = Load(vbWithWindowsPdb.Value, readerOptions)) {
				Assert.Equal(PdbFileKind.WindowsPDB, module.PdbState!.PdbFileKind);
				Assert.Equal(PdbDocumentConstants.LanguageVisualBasic, module.PdbState.Documents.Single().Language);
				var rootScope = module.Find("VbFeatures.Helpers", isReflectionName: true)!.FindMethod("Constants")!.Body!.PdbMethod!.Scope!;
				var constants = GetScopes(rootScope).SelectMany(a => a.Constants).ToDictionary(a => a.Name!, a => a.Value);

				Assert.Equal(new System.DateTime(2007, 5, 6, 8, 9, 10), constants["Day"]);
				Assert.Equal(12.5m, constants["Price"]);
				Assert.Equal("title", constants["Title"]);
			}
		}

		[Fact]
		public void VisualBasicWindowsPdbCanBeWrittenAsAPortablePdb() {
			SkipUnlessWindows();
			using (var original = Load(vbWithWindowsPdb.Value)) {
				var expected = GetSourceLevelDebugInfo(original);

				var written = Write(original, PdbFileKind.PortablePDB, DummyLogger.NoThrowInstance);

				using (var reloaded = Load(written))
					Assert.Equal(expected, GetSourceLevelDebugInfo(reloaded));
				Assert.Equal(0, AssemblyRunner.RunEntryPoint(written.Image));
			}
		}

		// Fixed in 86066117: the native PDB writer needs one outermost scope for the whole method. With a root
		// scope that ended before the method's end, the sequence points after it were lost, and a second
		// outermost scope failed.
		[Fact]
		public void RootScopeThatIsShorterThanTheMethodKeepsAllSequencePoints() {
			SkipUnlessWindows();
			using (var module = Load(Windows)) {
				var body = module.Find("Features.Code", isReflectionName: true)!.FindMethod("Locals")!.Body!;
				int sequencePointCount = body.Instructions.Count(a => a.SequencePoint is not null);
				Assert.True(sequencePointCount > 3);
				var rootScope = body.PdbMethod!.Scope!;
				// The scope ends in the middle of the method
				rootScope.End = body.Instructions[body.Instructions.Count / 2];

				var written = Write(module, PdbFileKind.WindowsPDB, DummyLogger.NoThrowInstance);

				using (var reloaded = Load(written)) {
					var reloadedBody = reloaded.Find("Features.Code", isReflectionName: true)!.FindMethod("Locals")!.Body!;
					Assert.Equal(sequencePointCount, reloadedBody.Instructions.Count(a => a.SequencePoint is not null));
					Assert.Contains(reloadedBody.Variables, a => a.Name == "buffer");
				}
			}
		}

		// Fixed in 482ed58b: a constant without a type is valid in a Windows PDB
		[Fact]
		public void ConstantWithoutATypeSurvivesAWrite() {
			SkipUnlessWindows();
			using (var module = Load(Windows)) {
				var rootScope = module.Find("Features.Code", isReflectionName: true)!.FindMethod("Locals")!.Body!.PdbMethod!.Scope!;
				GetScopes(rootScope).First(a => a.Constants.Count != 0).Constants.Add(new PdbConstant("NoType", null, 77));

				var written = Write(module, PdbFileKind.WindowsPDB, DummyLogger.NoThrowInstance);

				using (var reloaded = Load(written)) {
					var reloadedRoot = reloaded.Find("Features.Code", isReflectionName: true)!.FindMethod("Locals")!.Body!.PdbMethod!.Scope!;
					var constant = GetScopes(reloadedRoot).SelectMany(a => a.Constants).Single(a => a.Name == "NoType");
					Assert.Equal(77, constant.Value);
				}
			}
		}

		// Fixed in 1ffc8fdc: the language and the checksum of a source file are in a stream of the PDB whose
		// name has the file's name with every '/' replaced by '\'. The managed reader looked for the name
		// with '/', so such a document (eg. a build with PathMap=/_/) had no language and no checksum.
		[Theory]
		[InlineData(PdbReaderOptions.None)]
		[InlineData(PdbReaderOptions.MicrosoftComReader)]
		public void DocumentWithSlashesInItsNameHasItsLanguageAndChecksum(PdbReaderOptions readerOptions) {
			SkipUnlessWindows();
			var compiled = CSharpCompiler.Compile("public class Program { public static int Main() { return 0; } }",
				new CSharpCompilerOptions { PdbKind = PdbKind.Windows, SourcePath = "/_/src/Program.cs" });

			using (var module = Load(compiled, readerOptions)) {
				var document = Assert.Single(module.PdbState!.Documents);
				Assert.Equal("/_/src/Program.cs", document.Url);
				Assert.Equal(PdbDocumentConstants.LanguageCSharp, document.Language);
				Assert.Equal(PdbDocumentConstants.HashSHA1, document.CheckSumAlgorithmId);
				Assert.Equal(20, document.CheckSum!.Length);
			}
		}

		// Fixed in 57d99ece: the compiler also writes the states of a state machine to a Windows PDB (custom
		// debug info kind 9). It was read as unknown data and couldn't be converted to a portable PDB.
		[Theory]
		[InlineData(PdbReaderOptions.None)]
		[InlineData(PdbReaderOptions.MicrosoftComReader)]
		public void EditAndContinueDebugInfoIsReadFromAWindowsPdb(PdbReaderOptions readerOptions) {
			SkipUnlessWindows();
			using (var portable = DebugInfoSource.Load())
			using (var windows = Load(DebugInfoSource.CompiledWithWindowsPdb, readerOptions)) {
				var kinds = PortablePdbReaderTests.GetMethodCustomDebugInfoKinds(windows);

				Assert.Contains(PdbCustomDebugInfoKind.EditAndContinueLocalSlotMap, kinds);
				Assert.Contains(PdbCustomDebugInfoKind.EditAndContinueLambdaMap, kinds);
				Assert.Contains(PdbCustomDebugInfoKind.EditAndContinueStateMachineStateMap, kinds);
				Assert.DoesNotContain(PdbCustomDebugInfoKind.Unknown, kinds);
				Assert.Equal(GetSourceLevelDebugInfo(portable), GetSourceLevelDebugInfo(windows));
			}
		}

		[Fact]
		public void EditAndContinueDebugInfoSurvivesTheConversionToAPortablePdb() {
			SkipUnlessWindows();
			using (var portable = DebugInfoSource.Load())
			using (var windows = Load(DebugInfoSource.CompiledWithWindowsPdb)) {
				// Read everything before the kind of the PDB is changed
				var expected = GetSourceLevelDebugInfo(windows);
				var written = Write(windows, PdbFileKind.PortablePDB, DummyLogger.NoThrowInstance);

				using (var converted = Load(written)) {
					Assert.Equal(expected, GetSourceLevelDebugInfo(converted));
					foreach (var methodName in new[] { "Lambda", "Sum" }) {
						var expectedInfos = DebugInfoSource.GetMethod(portable, methodName).CustomDebugInfos;
						var actualInfos = DebugInfoSource.GetMethod(converted, methodName).CustomDebugInfos;
						// The same edit-and-continue data as in the PDB that the compiler created as a portable PDB
						Assert.Equal(
							expectedInfos.OfType<PdbEditAndContinueLocalSlotMapCustomDebugInfo>().Select(a => System.BitConverter.ToString(a.Data)),
							actualInfos.OfType<PdbEditAndContinueLocalSlotMapCustomDebugInfo>().Select(a => System.BitConverter.ToString(a.Data)));
						Assert.Equal(
							expectedInfos.OfType<PdbEditAndContinueLambdaMapCustomDebugInfo>().Select(a => System.BitConverter.ToString(a.Data)),
							actualInfos.OfType<PdbEditAndContinueLambdaMapCustomDebugInfo>().Select(a => System.BitConverter.ToString(a.Data)));
					}
					Assert.Contains(PdbCustomDebugInfoKind.EditAndContinueStateMachineStateMap, PortablePdbReaderTests.GetMethodCustomDebugInfoKinds(converted));
				}
			}
		}
	}
}
