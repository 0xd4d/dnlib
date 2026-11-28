// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Pdb.Symbols;
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
		// isIncluded: if it's not null, only the methods that it returns true for are in the text
		static string GetSourceLevelDebugInfo(ModuleDef module, System.Func<MethodDef, bool>? isIncluded = null) {
			var text = new StringBuilder();
			foreach (var method in module.GetTypes().SelectMany(a => a.Methods)) {
				var body = method.Body;
				if (body is null || (isIncluded is not null && !isIncluded(method)))
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
				Assert.Null(withoutScope.Body.PdbMethod.Scope);
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

		// The custom debug info that only a Windows PDB has, created with the API: one of each kind is added to
		// the method Sum of the DebugInfoSource fixture and read again
		[Theory]
		[InlineData(PdbReaderOptions.None)]
		[InlineData(PdbReaderOptions.MicrosoftComReader)]
		public void EveryKindOfWindowsPdbCustomDebugInfoIsWrittenAndRead(PdbReaderOptions readerOptions) {
			SkipUnlessWindows();
			CompiledAssembly written;
			using (var module = Load(DebugInfoSource.CompiledWithWindowsPdb)) {
				var method = DebugInfoSource.GetMethod(module, "Sum");
				var otherMethod = DebugInfoSource.GetMethod(module, "NoLocals");
				var body = method.Body!;
				var infos = method.CustomDebugInfos;
				infos.Clear();

				var usingGroups = new PdbUsingGroupsCustomDebugInfo();
				usingGroups.UsingCounts.Add(2);
				usingGroups.UsingCounts.Add(0);
				usingGroups.UsingCounts.Add(5);
				infos.Add(usingGroups);
				infos.Add(new PdbForwardModuleInfoCustomDebugInfo(otherMethod));
				var hoistedScopes = new PdbStateMachineHoistedLocalScopesCustomDebugInfo();
				hoistedScopes.Scopes.Add(new StateMachineHoistedLocalScope(body.Instructions[0], body.Instructions[2]));
				hoistedScopes.Scopes.Add(new StateMachineHoistedLocalScope(null, null));
				infos.Add(hoistedScopes);
				// The class of a state machine is a nested type of the method's type
				infos.Add(new PdbStateMachineTypeNameCustomDebugInfo(method.DeclaringType!.NestedTypes[0]));
				var dynamicLocals = new PdbDynamicLocalsCustomDebugInfo();
				// A variable is found by its index and its name, so it has the name of the local. An entry
				// with another name is the entry of a constant.
				var dynamicLocal = new PdbDynamicLocal { Local = body.Variables[0] };
				var dynamicConstant = new PdbDynamicLocal { Name = "dynamicConstant" };
				dynamicConstant.Flags.Add(1);
				dynamicLocals.Locals.Add(dynamicConstant);
				dynamicLocal.Flags.Add(1);
				dynamicLocal.Flags.Add(0);
				dynamicLocals.Locals.Add(dynamicLocal);
				infos.Add(dynamicLocals);
				var tupleNames = new PdbTupleElementNamesCustomDebugInfo();
				var tuple = new PdbTupleElementNames { Local = body.Variables[0] };
				tuple.TupleElementNames.Add("first");
				tuple.TupleElementNames.Add("second");
				tupleNames.Names.Add(tuple);
				infos.Add(tupleNames);
				infos.Add(new PdbEditAndContinueLocalSlotMapCustomDebugInfo(new byte[] { 1, 2, 3 }));
				infos.Add(new PdbEditAndContinueLambdaMapCustomDebugInfo(new byte[] { 4, 5 }));

				// A method can forward to the using groups of another method instead of having its own
				otherMethod.CustomDebugInfos.Clear();
				otherMethod.CustomDebugInfos.Add(new PdbForwardMethodInfoCustomDebugInfo(method));

				written = Write(module, PdbFileKind.WindowsPDB);
			}

			using (var module = Load(written, readerOptions)) {
				var method = DebugInfoSource.GetMethod(module, "Sum");
				var otherMethod = DebugInfoSource.GetMethod(module, "NoLocals");
				var body = method.Body!;
				var infos = method.CustomDebugInfos;
				T Single<T>() where T : PdbCustomDebugInfo => Assert.Single(infos.OfType<T>());

				Assert.Equal(new ushort[] { 2, 0, 5 }, Single<PdbUsingGroupsCustomDebugInfo>().UsingCounts);
				Assert.Same(otherMethod, Single<PdbForwardModuleInfoCustomDebugInfo>().Method);
				var scopes = Single<PdbStateMachineHoistedLocalScopesCustomDebugInfo>().Scopes;
				Assert.Equal(2, scopes.Count);
				Assert.Same(body.Instructions[0], scopes[0].Start);
				Assert.Same(body.Instructions[2], scopes[0].End);
				Assert.True(scopes[1].IsSynthesizedLocal);
				Assert.Same(method.DeclaringType!.NestedTypes[0], Single<PdbStateMachineTypeNameCustomDebugInfo>().Type);
				var dynamicLocals = Single<PdbDynamicLocalsCustomDebugInfo>().Locals;
				Assert.Equal(2, dynamicLocals.Count);
				Assert.True(dynamicLocals[0].IsConstant);
				Assert.Equal("dynamicConstant", dynamicLocals[0].Name);
				Assert.Same(body.Variables[0], dynamicLocals[1].Local);
				Assert.Equal(body.Variables[0].Name, dynamicLocals[1].Name);
				Assert.Equal(new byte[] { 1, 0 }, dynamicLocals[1].Flags);
				var tuple = Assert.Single(Single<PdbTupleElementNamesCustomDebugInfo>().Names);
				Assert.Same(body.Variables[0], tuple.Local);
				Assert.Equal(body.Variables[0].Name, tuple.Name);
				Assert.Equal(new[] { "first", "second" }, tuple.TupleElementNames);
				Assert.Equal(new byte[] { 1, 2, 3 }, Single<PdbEditAndContinueLocalSlotMapCustomDebugInfo>().Data);
				Assert.Equal(new byte[] { 4, 5 }, Single<PdbEditAndContinueLambdaMapCustomDebugInfo>().Data);

				Assert.Same(method, Assert.Single(otherMethod.CustomDebugInfos.OfType<PdbForwardMethodInfoCustomDebugInfo>()).Method);
			}
		}

		// A Windows PDB can have the source link data, the source server data and the source code of a document
		// (embedded source). The COM reader reads the source link and source server data again.
		[Fact]
		public void SourceLinkSourceServerAndEmbeddedSourceAreWritten() {
			SkipUnlessWindows();
			var sourceLink = Encoding.UTF8.GetBytes("{\"documents\":{\"/_/*\":\"https://example.com/*\"}}");
			var sourceServer = Encoding.UTF8.GetBytes("SRCSRV: ini ------------------------------------------------\r\nVERSION=2\r\nSRCSRV: end ------------------------------------------------\r\n");
			CompiledAssembly written;
			using (var module = Load(DebugInfoSource.CompiledWithWindowsPdb)) {
				module.CustomDebugInfos.Clear();
				module.CustomDebugInfos.Add(new PdbSourceLinkCustomDebugInfo(sourceLink));
				module.CustomDebugInfos.Add(new PdbSourceServerCustomDebugInfo(sourceServer));
				var document = module.PdbState!.Documents.First();
				document.CustomDebugInfos.Add(new PdbEmbeddedSourceCustomDebugInfo(new byte[] { 0, 0, 0, 0 }.Concat(Encoding.UTF8.GetBytes("class C { }")).ToArray()));
				written = Write(module, PdbFileKind.WindowsPDB);
			}

			using (var module = Load(written, PdbReaderOptions.MicrosoftComReader)) {
				Assert.Equal(sourceLink, Assert.Single(module.CustomDebugInfos.OfType<PdbSourceLinkCustomDebugInfo>()).FileBlob);
				Assert.Equal(sourceServer, Assert.Single(module.CustomDebugInfos.OfType<PdbSourceServerCustomDebugInfo>()).FileBlob);
			}
		}

		// The PDB is written to a file if there's no stream: the file name of the options, or the module's file
		// name with the extension .pdb
		[Theory, InlineData(true), InlineData(false)]
		public void WindowsPdbIsWrittenToAFile(bool withPdbFileName) {
			SkipUnlessWindows();
			using (var directory = new TempDirectory())
			using (var module = Load(DebugInfoSource.CompiledWithWindowsPdb)) {
				module.PdbState!.PdbFileKind = PdbFileKind.WindowsPDB;
				var moduleFile = directory.GetPath("Written.dll");
				var pdbFile = withPdbFileName ? directory.GetPath("Other.pdb") : directory.GetPath("Written.pdb");
				var options = new ModuleWriterOptions(module) { WritePdb = true };
				if (withPdbFileName)
					options.PdbFileName = pdbFile;

				module.Write(moduleFile, options);

				Assert.True(File.Exists(pdbFile));
				using (var written = ModuleDefMD.Load(moduleFile, new ModuleCreationOptions { PdbFileOrData = pdbFile, TryToLoadPdbFromDisk = false }))
					Assert.NotNull(written.PdbState);
			}
		}

		// Writes the Windows fixture with a Windows PDB and the options, returns the files
		static CompiledAssembly WriteWithPdbOptions(PdbWriterOptions pdbOptions, ChecksumAlgorithm algorithm = ChecksumAlgorithm.SHA256) {
			using (var module = Load(Windows)) {
				var options = new ModuleWriterOptions(module) {
					WritePdb = true,
					PdbStream = new MemoryStream(),
					PdbFileName = "written.pdb",
					PdbChecksumAlgorithm = algorithm,
				};
				// The fixture has no PDB checksum, so the options that were copied from it don't ask for one
				options.PdbOptions = pdbOptions;
				var image = new MemoryStream();
				module.Write(image, options);
				return new CompiledAssembly(image.ToArray(), ((MemoryStream)options.PdbStream).ToArray());
			}
		}

		static bool IsPdbLoaded(byte[] image, byte[] pdb, PdbReaderOptions options) {
			var creationOptions = new ModuleCreationOptions { PdbFileOrData = pdb, PdbOptions = options, TryToLoadPdbFromDisk = false };
			using (var module = ModuleDefMD.Load(image, creationOptions))
				return module.PdbState is not null;
		}

		// The PE file can have a checksum of the PDB file (a debug directory entry): a hash of the whole PDB
		// file, with the GUID and the timestamp of the PDB counted as zeros. The native PDB writer can't
		// calculate it, dnlib does. A reader checks it if it's asked to.
		[Theory]
		[InlineData(ChecksumAlgorithm.SHA256, PdbWriterOptions.Deterministic, PdbReaderOptions.None)]
		[InlineData(ChecksumAlgorithm.SHA384, PdbWriterOptions.Deterministic, PdbReaderOptions.None)]
		[InlineData(ChecksumAlgorithm.SHA512, PdbWriterOptions.Deterministic, PdbReaderOptions.MicrosoftComReader)]
		[InlineData(ChecksumAlgorithm.SHA256, PdbWriterOptions.None, PdbReaderOptions.None)]
		public void ChecksumOfAWindowsPdbIsWrittenAndValidated(ChecksumAlgorithm algorithm, PdbWriterOptions otherOptions, PdbReaderOptions readerOptions) {
			SkipUnlessWindows();
			var written = WriteWithPdbOptions(otherOptions | PdbWriterOptions.PdbChecksum, algorithm);

			// The entries are in the order of a file that the compiler wrote with a portable PDB
			using (var peImage = new dnlib.PE.PEImage(written.Image)) {
				var types = peImage.ImageDebugDirectories.Select(a => a.Type).ToList();
				Assert.Equal(dnlib.PE.ImageDebugType.CodeView, types[0]);
				Assert.Equal(dnlib.PE.ImageDebugType.PdbChecksum, types[1]);
				Assert.Equal((otherOptions & PdbWriterOptions.Deterministic) != 0, types.Contains(dnlib.PE.ImageDebugType.Reproducible));
			}

			Assert.True(IsPdbLoaded(written.Image, written.Pdb!, readerOptions | PdbReaderOptions.ValidatePdbChecksum));
			// The last byte of the file is in a page that the readers don't need
			var changedPdb = (byte[])written.Pdb!.Clone();
			changedPdb[changedPdb.Length - 1] ^= 1;
			Assert.False(IsPdbLoaded(written.Image, changedPdb, readerOptions | PdbReaderOptions.ValidatePdbChecksum));
			// Without the option, the checksum isn't looked at
			Assert.True(IsPdbLoaded(written.Image, changedPdb, readerOptions));
		}

		[Fact]
		public void NoPdbChecksumIsWrittenForAWindowsPdbWithoutTheOption() {
			SkipUnlessWindows();
			var written = WriteWithPdbOptions(PdbWriterOptions.Deterministic);

			using (var peImage = new dnlib.PE.PEImage(written.Image))
				Assert.DoesNotContain(peImage.ImageDebugDirectories, a => a.Type == dnlib.PE.ImageDebugType.PdbChecksum);
			// The reader has nothing to validate, so the PDB is accepted
			Assert.True(IsPdbLoaded(written.Image, written.Pdb!, PdbReaderOptions.ValidatePdbChecksum));
		}

		// Two writes of the same module give the same PDB and the same checksum
		[Fact]
		public void DeterministicWindowsPdbHasTheSameChecksumEveryTime() {
			SkipUnlessWindows();
			var first = WriteWithPdbOptions(PdbWriterOptions.Deterministic | PdbWriterOptions.PdbChecksum);
			var second = WriteWithPdbOptions(PdbWriterOptions.Deterministic | PdbWriterOptions.PdbChecksum);

			Assert.Equal(first.Pdb, second.Pdb);
			Assert.Equal(first.Image, second.Image);
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

		static readonly System.Lazy<CompiledAssembly> vbWithPortablePdb = new System.Lazy<CompiledAssembly>(() =>
			VisualBasicCompiler.Compile(Fixtures.Fixtures.ReadSource("VbFeatures.vb"), new VisualBasicCompilerOptions {
				AssemblyName = "VbFeatures",
				OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication,
				PdbKind = PdbKind.Portable,
			}));

		// The locals of a Visual Basic iterator method are fields of a class that the compiler creates. A Windows
		// PDB has them as locals with a special name ($VB$ResumableLocal_...) in a scope of that class's MoveNext
		// method, a portable PDB stores them in another way.
		static bool IsNotAnIteratorClassMethod(MethodDef method) => !method.DeclaringType!.Name.String.StartsWith("VB$StateMachine");

		// In a Windows PDB that the Visual Basic compiler writes, a scope ends at its last instruction. In every
		// other PDB (C#, or a portable PDB), it ends at the first instruction after the scope. dnlib's scopes
		// always end at the first instruction after the scope: the same code has the same scopes with both kinds
		// of PDB files.
		[Fact]
		public void VisualBasicWindowsPdbHasTheScopesOfThePortablePdb() {
			SkipUnlessWindows();
			using (var portable = Load(vbWithPortablePdb.Value))
			using (var windows = Load(vbWithWindowsPdb.Value)) {
				Assert.Equal(PdbFileKind.WindowsPDB, windows.PdbState!.PdbFileKind);
				var expected = GetSourceLevelDebugInfo(portable, IsNotAnIteratorClassMethod);
				Assert.Contains("scope #", expected);
				Assert.Equal(expected, GetSourceLevelDebugInfo(windows, IsNotAnIteratorClassMethod));
			}
		}

		// The scopes of every method as they are in the PDB file: the IL offsets where they start and end
		static string GetScopeOffsetsInThePdbFile(CompiledAssembly assembly) {
			var text = new StringBuilder();
			using (var module = ModuleDefMD.Load(assembly.Image))
			using (var reader = SymbolReaderFactory.Create(PdbReaderOptions.None, module.Metadata, assembly.Pdb!)!) {
				reader.Initialize(module);
				foreach (var method in module.GetTypes().SelectMany(a => a.Methods)) {
					if (reader.GetMethod(method, 1)?.RootScope is not SymbolScope rootScope)
						continue;
					text.Append(method.FullName + ":");
					var scopes = new Stack<SymbolScope>();
					scopes.Push(rootScope);
					while (scopes.Count != 0) {
						var scope = scopes.Pop();
						text.Append($" {scope.StartOffset}-{scope.EndOffset}");
						foreach (var child in scope.Children.Reverse())
							scopes.Push(child);
					}
					text.AppendLine();
				}
			}
			return text.ToString();
		}

		// The scopes of a method (eg. "Features.Code::Locals") that don't start at its first instruction, as they
		// are in the PDB file
		static string[] GetInnerScopes(CompiledAssembly assembly, string method) {
			var line = GetScopeOffsetsInThePdbFile(assembly).Split('\n').Single(a => a.Contains(" " + method + "(")).Trim();
			var scopes = line.Substring(line.LastIndexOf("):") + 2).Trim().Split(' ');
			return scopes.Where(a => !a.StartsWith("0-")).ToArray();
		}

		// What the tests above are about: in the Windows PDB, the scopes inside a method end one byte earlier
		// than in the portable PDB if the compiler is Visual Basic's, and at the same offset if it's C#'s
		[Fact]
		public void CompilersWriteTheEndOfAScopeInDifferentWays() {
			SkipUnlessWindows();
			string OneLess(string scope) {
				var offsets = scope.Split('-');
				return offsets[0] + "-" + (int.Parse(offsets[1]) - 1);
			}

			var visualBasicScopes = GetInnerScopes(vbWithPortablePdb.Value, "VbFeatures.Program::Main");
			Assert.NotEmpty(visualBasicScopes);
			Assert.Equal(visualBasicScopes.Select(OneLess), GetInnerScopes(vbWithWindowsPdb.Value, "VbFeatures.Program::Main"));

			var csharpScopes = GetInnerScopes(Portable, "Features.Code::Locals");
			Assert.NotEmpty(csharpScopes);
			Assert.Equal(csharpScopes, GetInnerScopes(Windows, "Features.Code::Locals"));
		}

		// dnlib writes them like the compiler did. It knows the compiler from the language of the source files
		// in the PDB (the Visual Basic test input has no reference to the Visual Basic runtime).
		[Theory, InlineData(true), InlineData(false)]
		public void ScopesAreWrittenLikeTheCompilerWroteThem(bool visualBasic) {
			SkipUnlessWindows();
			var compiled = visualBasic ? vbWithWindowsPdb.Value : Windows;
			using (var module = Load(compiled)) {
				if (visualBasic)
					Assert.DoesNotContain(module.GetAssemblyRefs(), a => a.Name.String.StartsWith("Microsoft.VisualBasic", System.StringComparison.Ordinal));

				var written = Write(module, PdbFileKind.WindowsPDB);

				Assert.Equal(GetScopeOffsetsInThePdbFile(compiled), GetScopeOffsetsInThePdbFile(written));
			}
		}

		// A C# program can use the Visual Basic runtime: that doesn't make it a Visual Basic program. The test
		// creates its own assembly with the runtime's name, so it doesn't matter which runtime the tests run on.
		[Fact]
		public void CSharpCodeThatReferencesTheVisualBasicRuntimeHasTheScopesOfThePortablePdb() {
			SkipUnlessWindows();
			var runtimeWithAnotherVersion = CSharpCompiler.Compile(@"
[assembly: System.Reflection.AssemblyVersion(""99.0.0.0"")]
namespace NotTheRealRuntime { public static class Text { public static int Len(string text) { return text.Length; } } }",
				new CSharpCompilerOptions { AssemblyName = "Microsoft.VisualBasic" });
			const string source = @"
public class Program {
	public static int Main() {
		int result = NotTheRealRuntime.Text.Len(""abc"") - 3;
		for (int i = 0; i < 3; i++) {
			int twice = i * 2;
			result += twice - twice;
		}
		return result;
	}
}";
			CompiledAssembly Compile(PdbKind pdbKind) {
				var options = new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication, PdbKind = pdbKind, Optimize = false };
				options.References.Add(runtimeWithAnotherVersion.Image);
				return CSharpCompiler.Compile(source, options);
			}
			var withPortablePdb = Compile(PdbKind.Portable);
			var withWindowsPdb = Compile(PdbKind.Windows);

			using (var portable = Load(withPortablePdb))
			using (var windows = Load(withWindowsPdb)) {
				Assert.Contains(windows.GetAssemblyRefs(), a => a.Name == "Microsoft.VisualBasic");
				var expected = GetSourceLevelDebugInfo(portable);
				Assert.Contains("local 2 twice", expected);
				Assert.Equal(expected, GetSourceLevelDebugInfo(windows));

				var written = Write(windows, PdbFileKind.WindowsPDB);
				Assert.Equal(GetScopeOffsetsInThePdbFile(withWindowsPdb), GetScopeOffsetsInThePdbFile(written));
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
				Assert.Equal(20, document.CheckSum.Length);
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

		// The debug info of an async method has a "step" for every await: where the method yields and where it
		// continues. The native writer only accepts steps that are sorted by where the method yields.
		// Fixed in this commit: steps in another order (code can be moved, or the PDB had them like that) made the
		// write throw an ArgumentException. They are sorted now, and two steps at the same instruction are an error.
		[Theory]
		[InlineData("other order", true)]
		[InlineData("no steps", true)]
		[InlineData("same yield instruction", false)]
		public void StepsOfAnAsyncMethodAreWrittenInTheOrderOfTheCode(string change, bool isWritten) {
			SkipUnlessWindows();
			string methodName;
			List<string> expectedSteps;
			var written = new MemoryStream();
			var writtenPdb = new MemoryStream();
			string DescribeStep(PdbAsyncStepInfo step) => $"{step.YieldInstruction.Offset:X} {step.BreakpointMethod.FullName} {step.BreakpointInstruction.Offset:X}";
			using (var module = Load(Windows)) {
				var method = module.GetTypes().SelectMany(a => a.Methods).First(a => a.CustomDebugInfos.OfType<PdbAsyncMethodCustomDebugInfo>().Any(b => b.StepInfos.Count >= 2));
				methodName = method.FullName;
				var steps = method.CustomDebugInfos.OfType<PdbAsyncMethodCustomDebugInfo>().Single().StepInfos;
				switch (change) {
				case "other order":
					var first = steps[0];
					steps.RemoveAt(0);
					steps.Add(first);
					break;
				case "no steps":
					steps.Clear();
					break;
				case "same yield instruction":
					steps[1] = new PdbAsyncStepInfo(steps[0].YieldInstruction, steps[1].BreakpointMethod, steps[1].BreakpointInstruction);
					break;
				}
				expectedSteps = steps.Select(DescribeStep).OrderBy(a => a, System.StringComparer.Ordinal).ToList();
				var options = new ModuleWriterOptions(module) { WritePdb = true, PdbStream = writtenPdb, PdbFileName = "written.pdb" };

				if (!isWritten) {
					var exception = Assert.Throws<ModuleWriterException>(() => module.Write(written, options));
					Assert.Contains("same yield instruction", exception.Message);
					return;
				}
				module.Write(written, options);
			}

			using (var module = Load(new CompiledAssembly(written.ToArray(), writtenPdb.ToArray()))) {
				var method = module.GetTypes().SelectMany(a => a.Methods).Single(a => a.FullName == methodName);
				var asyncMethod = method.CustomDebugInfos.OfType<PdbAsyncMethodCustomDebugInfo>().Single();
				Assert.NotNull(asyncMethod.KickoffMethod);
				// Sorted by the yield offset
				Assert.Equal(expectedSteps, asyncMethod.StepInfos.Select(DescribeStep).ToList());
			}
		}

		// Custom debug info that a Windows PDB can't store is an error of the writer (a ModuleWriterException with
		// the default logger), never another exception.
		// Fixed in this commit: null in the list of a method's custom debug infos threw a NullReferenceException.
		[Theory]
		[InlineData("more than 255 infos", "Too many custom debug infos")]
		[InlineData("null info", "Custom debug info is null")]
		[InlineData("more than 65535 using counts", "UsingCounts contains more than 0xFFFF elements")]
		[InlineData("forwarded to a method of another module", "is not defined in this module")]
		[InlineData("state machine without a type", "State machine type is null")]
		[InlineData("null dynamic local", "Dynamic local is null")]
		[InlineData("dynamic local with 65 flags", "Dynamic local flags is longer than 64 bytes")]
		[InlineData("dynamic local with a long name", "Dynamic local name is longer than 64 chars")]
		[InlineData("dynamic local with a zero character", "Dynamic local name contains a NUL char")]
		[InlineData("null tuple names", "Tuple name info is null")]
		[InlineData("tuple name with a zero character", "NUL char")]
		[InlineData("info of a portable PDB", "isn't supported by Windows PDB files")]
		public void CustomDebugInfoThatCanNotBeWrittenIsAnError(string what, string expectedMessage) {
			SkipUnlessWindows();
			using (var module = Load(DebugInfoSource.CompiledWithWindowsPdb))
			using (var otherModule = Load(Windows)) {
				var method = DebugInfoSource.GetMethod(module, "Sum");
				var infos = method.CustomDebugInfos;
				infos.Clear();
				switch (what) {
				case "more than 255 infos":
					for (int i = 0; i < 256; i++)
						infos.Add(new PdbUsingGroupsCustomDebugInfo());
					break;
				case "null info":
					infos.Add(null!);
					break;
				case "more than 65535 using counts":
					var usingGroups = new PdbUsingGroupsCustomDebugInfo();
					for (int i = 0; i < 0x10000; i++)
						usingGroups.UsingCounts.Add(1);
					infos.Add(usingGroups);
					break;
				case "forwarded to a method of another module":
					infos.Add(new PdbForwardMethodInfoCustomDebugInfo(otherModule.GetTypes().SelectMany(a => a.Methods).First()));
					break;
				case "state machine without a type":
					infos.Add(new PdbStateMachineTypeNameCustomDebugInfo(null!));
					break;
				case "null dynamic local":
					var dynamicLocals = new PdbDynamicLocalsCustomDebugInfo();
					dynamicLocals.Locals.Add(null!);
					infos.Add(dynamicLocals);
					break;
				case "dynamic local with 65 flags":
					var withFlags = new PdbDynamicLocal { Name = "name" };
					for (int i = 0; i < 65; i++)
						withFlags.Flags.Add(1);
					dynamicLocals = new PdbDynamicLocalsCustomDebugInfo();
					dynamicLocals.Locals.Add(withFlags);
					infos.Add(dynamicLocals);
					break;
				case "dynamic local with a long name":
					dynamicLocals = new PdbDynamicLocalsCustomDebugInfo();
					dynamicLocals.Locals.Add(new PdbDynamicLocal { Name = new string('a', 65) });
					infos.Add(dynamicLocals);
					break;
				case "dynamic local with a zero character":
					dynamicLocals = new PdbDynamicLocalsCustomDebugInfo();
					dynamicLocals.Locals.Add(new PdbDynamicLocal { Name = "a\0b" });
					infos.Add(dynamicLocals);
					break;
				case "null tuple names":
					var tupleNames = new PdbTupleElementNamesCustomDebugInfo();
					tupleNames.Names.Add(null!);
					infos.Add(tupleNames);
					break;
				case "tuple name with a zero character":
					tupleNames = new PdbTupleElementNamesCustomDebugInfo();
					var names = new PdbTupleElementNames { Name = "local" };
					names.TupleElementNames.Add("a\0b");
					tupleNames.Names.Add(names);
					infos.Add(tupleNames);
					break;
				case "info of a portable PDB":
					infos.Add(new PdbDefaultNamespaceCustomDebugInfo("Namespace"));
					break;
				}
				var options = new ModuleWriterOptions(module) { WritePdb = true, PdbStream = new MemoryStream(), PdbFileName = "written.pdb" };

				var exception = Assert.Throws<ModuleWriterException>(() => module.Write(new MemoryStream(), options));

				Assert.Contains(expectedMessage, exception.Message);
			}
		}

		// Scopes, locals, async info and an entry point that the writer can't write are errors too
		[Theory]
		[InlineData("scope that ends before it starts", "PDB scope's end instruction is before its start instruction")]
		[InlineData("null scope", "PDB scope is null")]
		[InlineData("scope that is in the method twice", "PDB scope is used more than once in the method")]
		[InlineData("scope that isn't in its parent", "PDB scope isn't inside its parent scope")]
		[InlineData("scope with an instruction of another method", "Instruction was removed from the body but is referenced from PdbScope")]
		[InlineData("null local", "PDB local is null")]
		[InlineData("async method without the method it was created for", "KickoffMethod is null")]
		[InlineData("async method of another module", "is not defined in this module")]
		[InlineData("two async infos", "Duplicate async method custom debug info")]
		[InlineData("async step without a yield instruction", "YieldInstruction is null")]
		[InlineData("async step without a method", "BreakpointMethod is null")]
		[InlineData("async step without an instruction to continue at", "BreakpointInstruction is null")]
		[InlineData("async step that continues at an instruction of another method", "Async method instruction has been removed")]
		[InlineData("entry point of another module", "is not defined in this module")]
		public void DebugInfoThatCanNotBeWrittenIsAnError(string what, string expectedMessage) {
			SkipUnlessWindows();
			using (var module = Load(DebugInfoSource.CompiledWithWindowsPdb))
			using (var otherModule = Load(Windows)) {
				var method = DebugInfoSource.GetMethod(module, "Sum");
				var otherMethod = DebugInfoSource.GetMethod(module, "NoLocals");
				var methodOfOtherModule = otherModule.GetTypes().SelectMany(a => a.Methods).First(a => a.HasBody);
				var instructions = method.Body!.Instructions;
				var root = DebugInfoSource.GetRootScope(module, "Sum");
				var infos = method.CustomDebugInfos;
				switch (what) {
				case "scope that ends before it starts":
					root.Scopes.Add(new PdbScope { Start = instructions[3], End = instructions[1] });
					break;
				case "null scope":
					root.Scopes.Add(null!);
					break;
				case "scope that is in the method twice":
					var twice = new PdbScope { Start = instructions[1], End = instructions[2] };
					root.Scopes.Add(twice);
					root.Scopes.Add(twice);
					break;
				case "scope that isn't in its parent":
					var parent = new PdbScope { Start = instructions[2], End = instructions[3] };
					parent.Scopes.Add(new PdbScope { Start = instructions[1], End = instructions[4] });
					root.Scopes.Add(parent);
					break;
				case "scope with an instruction of another method":
					root.Scopes.Add(new PdbScope { Start = otherMethod.Body!.Instructions[0], End = instructions[2] });
					break;
				case "null local":
					root.Variables.Add(null!);
					break;
				case "async method without the method it was created for":
					infos.Add(new PdbAsyncMethodCustomDebugInfo(null!));
					break;
				case "async method of another module":
					infos.Add(new PdbAsyncMethodCustomDebugInfo(methodOfOtherModule));
					break;
				case "two async infos":
					infos.Add(new PdbAsyncMethodCustomDebugInfo(otherMethod));
					infos.Add(new PdbAsyncMethodCustomDebugInfo(otherMethod));
					break;
				case "async step without a yield instruction":
					AddAsyncStep(new PdbAsyncStepInfo(null!, method, instructions[1]));
					break;
				case "async step without a method":
					AddAsyncStep(new PdbAsyncStepInfo(instructions[0], null!, instructions[1]));
					break;
				case "async step without an instruction to continue at":
					AddAsyncStep(new PdbAsyncStepInfo(instructions[0], method, null!));
					break;
				case "async step that continues at an instruction of another method":
					AddAsyncStep(new PdbAsyncStepInfo(instructions[0], otherMethod, instructions[1]));
					break;
				case "entry point of another module":
					module.PdbState!.UserEntryPoint = methodOfOtherModule;
					break;
				}
				void AddAsyncStep(PdbAsyncStepInfo step) {
					var asyncMethod = new PdbAsyncMethodCustomDebugInfo(otherMethod);
					asyncMethod.StepInfos.Add(step);
					infos.Add(asyncMethod);
				}
				var options = new ModuleWriterOptions(module) { WritePdb = true, PdbStream = new MemoryStream(), PdbFileName = "written.pdb" };

				var exception = Assert.Throws<ModuleWriterException>(() => module.Write(new MemoryStream(), options));

				Assert.Contains(expectedMessage, exception.Message);
			}
		}
	}
}
