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
	}
}
