// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// An assembly can have more files than its main file: other modules with types, and files that are
	/// resources. The main file has their names in its File table; a row of the ExportedType table says
	/// which file has a type, a row of the ManifestResource table which file is a resource. These tests write
	/// values that a compiler doesn't write and read the assembly with dnlib. Only .NET Framework loads such
	/// an assembly, .NET doesn't: if .NET Framework runs the tests, it's asked for the same modules, types and
	/// resources.
	/// </summary>
	public sealed class ClrFileTableRuleTests : IClassFixture<ClrFileTableRuleTests.Fixture> {
		/// <summary>
		/// The directory with the files of all tests, and on .NET Framework the AppDomain that loads the assemblies:
		/// .NET Framework can't unload an assembly, only an AppDomain, and the files can't be deleted before that.
		/// </summary>
		public sealed class Fixture : IDisposable {
			public TempDirectory Directory { get; } = new TempDirectory();
			int directories;

			/// <summary>
			/// Creates a new directory for a test
			/// </summary>
			public string CreateDirectory() {
				var path = Directory.GetPath((++directories).ToString());
				System.IO.Directory.CreateDirectory(path);
				return path;
			}

#if NETFRAMEWORK
			AppDomain? domain;
			RuntimeReader? reader;

			public string ReadWithTheRuntime(string mainFile, string testDirectory) {
				if (reader is null) {
					domain = AppDomain.CreateDomain(nameof(ClrFileTableRuleTests), null, AppDomain.CurrentDomain.SetupInformation);
					reader = (RuntimeReader)domain.CreateInstanceFromAndUnwrap(typeof(RuntimeReader).Assembly.Location, typeof(RuntimeReader).FullName);
				}
				return reader.Read(mainFile, testDirectory);
			}
#else
			public string ReadWithTheRuntime(string mainFile, string testDirectory) => throw new NotSupportedException();
#endif

			public void Dispose() {
#if NETFRAMEWORK
				if (domain is not null)
					AppDomain.Unload(domain);
#endif
				Directory.Dispose();
			}
		}

		readonly Fixture fixture;
		readonly string testDirectory;

		public ClrFileTableRuleTests(Fixture fixture) {
			this.fixture = fixture;
			testDirectory = fixture.CreateDirectory();
		}

		string GetPath(string relativePath) => Path.Combine(testDirectory, relativePath);

		const string ModuleSource = @"
public class InModule { }
";
		const string MainSource = @"
public class InMain {
	public static string Method() => typeof(InModule).Name;
}
";
		static readonly byte[] moduleImage = CSharpCompiler.Compile(ModuleSource, new CSharpCompilerOptions { AssemblyName = "Extra", OutputKind = Microsoft.CodeAnalysis.OutputKind.NetModule }).Image;
		static readonly byte[] resourceFile = Encoding.ASCII.GetBytes("Content of the file");
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(MainSource, compilerOptions: CreateOptions());

		static CSharpCompilerOptions CreateOptions() {
			var options = new CSharpCompilerOptions();
			options.ModuleReferences.Add(moduleImage);
			options.LinkedResources.Add("Linked", new KeyValuePair<string, byte[]>("Data.bin", resourceFile));
			options.Resources.Add("Embedded", Encoding.ASCII.GetBytes("Embedded content"));
			return options;
		}

		// The directory of the assembly is "app". Its files are there, and the two other files are also in the
		// directory "app/sub" and in the directory above "app".
		string WriteFiles(byte[] mainImage) {
			foreach (var relativePath in new[] { "app", Path.Combine("app", "sub"), "" }) {
				Directory.CreateDirectory(GetPath(relativePath));
				File.WriteAllBytes(GetPath(Path.Combine(relativePath, "Extra.netmodule")), moduleImage);
				File.WriteAllBytes(GetPath(Path.Combine(relativePath, "Data.bin")), resourceFile);
			}
			var mainFile = GetPath(Path.Combine("app", "TestAssembly.dll"));
			File.WriteAllBytes(mainFile, mainImage);
			return mainFile;
		}

		// The column that has the name of a row
		static string GetNameColumn(Table table) => table == Table.ExportedType ? "TypeName" : "Name";

		// "Table(name of the row).Column"
		static (Table table, uint rid, string column) ParseCell(MetadataPatcher patcher, string text) {
			int open = text.IndexOf('('), close = text.IndexOf(").");
			var table = (Table)Enum.Parse(typeof(Table), text.Substring(0, open));
			uint rid = patcher.FindRow(table, GetNameColumn(table), text.Substring(open + 1, close - open - 1));
			return (table, rid, text.Substring(close + 2));
		}

		// Adds a string to the end of the #Strings heap and returns its offset
		static uint AddString(ref byte[] image, string text) {
			var block = new MetadataBlock(image);
			if (block.Streams.Any(a => a.Name == ClrReaderComparison.PaddingStreamName))
				block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			var heap = block.GetStream("#Strings");
			uint offset = (uint)heap.Data.Length;
			heap.Data = heap.Data.Concat(Encoding.UTF8.GetBytes(text)).Concat(new byte[1]).ToArray();
			image = block.Write();
			return offset;
		}

		// "Table(name of the row).Column = value", separated by "; ". A value is
		//	a number
		//	'text'					a new string. {dir} is the full path of the assembly's directory.
		//	of Table(name).Column	the value of another cell
		//	file name				for Implementation (a coded token: the row number and 0 = File, 1 = AssemblyRef,
		//							2 = ExportedType, 3 = not used): the row of the File table with that name
		byte[] CreateImage(string changes) {
			var image = baseImage;
			var original = new MetadataPatcher(baseImage);
			// The rows are found before anything is changed: a change can be a new name
			var cells = new List<(Table table, uint rid, string column, uint value)>();
			foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
				var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
				var cell = ParseCell(original, parts[0]);
				uint value;
				if (parts[1].StartsWith("'"))
					value = AddString(ref image, parts[1].Trim('\'').Replace("{dir}", GetPath("app")));
				else if (parts[1].StartsWith("of ")) {
					var other = ParseCell(original, parts[1].Substring(3));
					value = original.GetCell(other.table, other.rid, other.column);
				}
				else if (parts[1].StartsWith("file "))
					value = original.FindRow(Table.File, "Name", parts[1].Substring(5)) << 2;
				else
					value = uint.Parse(parts[1]);
				cells.Add((cell.table, cell.rid, cell.column, value));
			}
			var patcher = new MetadataPatcher(image);
			foreach (var cell in cells)
				patcher.SetCell(cell.table, cell.rid, cell.column, cell.value);
			return patcher.ToArray();
		}

		// A path in the test's directory without the directory
		string ToRelativePath(string? path) {
			if (string.IsNullOrEmpty(path))
				return "no path";
			return path!.StartsWith(testDirectory, StringComparison.OrdinalIgnoreCase) ? path.Substring(testDirectory.Length + 1).Replace('\\', '/') : "another directory";
		}

		static string ToText(byte[] data) => string.Concat(Encoding.ASCII.GetString(data).Select(a => a >= 0x20 && a <= 0x7E ? a.ToString() : "?"));

		// modules: the files of all modules of the assembly
		// types: the types that are found by their name, and the file of their module
		// exported: the rows of the ExportedType table: name, where the type is, and the type that it resolves to
		// resources: name, and "embedded" and its content or the name of its file
		string ReadWithDnlib(string mainFile) {
			var context = ModuleDef.CreateModuleContext();
			using (var module = ModuleDefMD.Load(mainFile, context)) {
				var assembly = module.Assembly!;
				((AssemblyResolver)context.AssemblyResolver).AddToCache(assembly);
				var modules = assembly.Modules.ToArray();
				try {
					var types = TypeNames.Select(a => assembly.Find(a, isReflectionName: true)).Where(a => a is not null).Select(a => Describe(a!));
					var exportedTypes = module.ExportedTypes.Select(a => {
						string implementation = a.Implementation is FileDef file ? "file " + file.Name.String.Replace(GetPath("app"), "{dir}") : a.Implementation is null ? "null" : a.Implementation.GetType().Name;
						return $"{a.ReflectionFullName} in {implementation} = {(a.Resolve() is TypeDef resolved ? Describe(resolved) : "null")}";
					});
					var resources = module.Resources.Select(a => a.Name + ": " + (
						a is EmbeddedResource embedded ? "embedded '" + ToText(embedded.CreateReader().ToArray()) + "'" :
						a is LinkedResource linked ? "file " + linked.File?.Name :
						a.ResourceType.ToString()));
					return $"modules: {string.Join(", ", modules.Select(a => a.Location.Length == 0 ? a.Name.String : ToRelativePath(a.Location)))}; types: {string.Join(", ", types)}; " +
						$"exported: {string.Join(", ", exportedTypes)}; resources: {string.Join(", ", resources)}";
				}
				finally {
					foreach (var other in modules)
						other.Dispose();
				}
			}
		}

		string Describe(TypeDef type) => type.ReflectionFullName + " in " + ToRelativePath(type.Module?.Location);

		// The expected value of a case that isn't tried with .NET Framework: it stops the process (an
		// AccessViolationException)
		const string NotAsked = "not asked";

		// The expected value of a case with a full path: .NET Framework doesn't find the type InModule. What it
		// does instead depends on the length of the path.
		const string TypeIsNotFound = "the type isn't found";

		// The names of types that are searched
		static readonly string[] TypeNames = new[] { "InModule", "inmodule", "NoSuchType", "Namespace.InModule", "InMain" };

#if NETFRAMEWORK
		/// <summary>
		/// Loads the assemblies in the AppDomain of the <see cref="Fixture"/>
		/// </summary>
		public sealed class RuntimeReader : MarshalByRefObject {
			public string Read(string mainFile, string testDirectory) {
				Func<string?, string> toRelativePath = path => string.IsNullOrEmpty(path) ? "no path" :
					path!.StartsWith(testDirectory, StringComparison.OrdinalIgnoreCase) ? path.Substring(testDirectory.Length + 1).Replace('\\', '/') : "another directory";
				// LoadFile() loads every file, also if an assembly with the same name is already loaded
				var assembly = System.Reflection.Assembly.LoadFile(mainFile);
				// The types first: the runtime loads a module when it's needed, and GetModules() loads all of them
				string types = string.Join(", ", TypeNames.Select(name => Try(() => assembly.GetType(name) is Type found ? found.FullName + " in " + toRelativePath(found.Module.FullyQualifiedName) : "")).Where(a => a.Length != 0));
				string modules = Try(() => string.Join(", ", assembly.GetModules(true).Select(a => toRelativePath(a.FullyQualifiedName))));
				string resources = Try(() => string.Join(", ", assembly.GetManifestResourceNames().Select(name => name + ": " + Try(() => {
					var info = assembly.GetManifestResourceInfo(name);
					if (info is null)
						return "no info";
					string where = string.IsNullOrEmpty(info.FileName) ? "embedded" : "file " + info.FileName;
					using (var stream = assembly.GetManifestResourceStream(name)) {
						if (stream is null)
							return where + " null";
						var data = new byte[stream.Length];
						stream.Read(data, 0, data.Length);
						return where + " '" + ToText(data) + "'";
					}
				}))));
				return $"types: {types}; modules: {modules}; resources: {resources}";
			}

			static string Try(Func<string> read) {
				try {
					return read();
				}
				catch (Exception ex) {
					return "throws " + ex.GetType().Name;
				}
			}

			// The proxy of this object is used until the AppDomain is unloaded
			public override object? InitializeLifetimeService() => null;
		}
#endif

		string ReadWithTheRuntime(string mainFile) => fixture.ReadWithTheRuntime(mainFile, testDirectory);

		[Theory]
		[InlineData("", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		// A file: 0 = it has metadata (a module), 1 = it has no metadata (a resource)
		[InlineData("File(Extra.netmodule).Flags = 1", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Flags = 2", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Flags = 4294967294", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Flags = 4294967295", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Data.bin).Flags = 0", "modules: app/TestAssembly.dll, app/Extra.netmodule, INVALID; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, throws BadImageFormatException, throws BadImageFormatException, throws BadImageFormatException, InMain in app/TestAssembly.dll; modules: throws BadImageFormatException; resources: Embedded: embedded 'Embedded content', Linked: throws BadImageFormatException")]
		[InlineData("File(Data.bin).Flags = 2", "modules: app/TestAssembly.dll, app/Extra.netmodule, INVALID; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, throws BadImageFormatException, throws BadImageFormatException, throws BadImageFormatException, InMain in app/TestAssembly.dll; modules: throws BadImageFormatException; resources: Embedded: embedded 'Embedded content', Linked: throws BadImageFormatException")]
		// The name of a file
		[InlineData("File(Extra.netmodule).Name = 0", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file  = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = 'Missing.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file Missing.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = 'Extra'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file Extra = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = ' Extra.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file  Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = 'sub\\Extra.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file sub\\Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = 'sub/Extra.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file sub/Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = '.\\Extra.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file .\\Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = './Extra.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file ./Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = '..\\Extra.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file ..\\Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = '../Extra.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file ../Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = 'sub\\..\\Extra.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file sub\\..\\Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = '\\Extra.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file \\Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = '{dir}\\Extra.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file {dir}\\Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", TypeIsNotFound)]
		[InlineData("File(Extra.netmodule).Name = '{dir}\\sub\\Extra.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file {dir}\\sub\\Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", TypeIsNotFound)]
		[InlineData("File(Extra.netmodule).Name = 'Extra.netmodule:stream'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule:stream = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = 'Extra?.netmodule'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file Extra?.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = 'Extra*'", "modules: app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file Extra* = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = 'TestAssembly.dll'; ExportedType(InModule).TypeDefId = 0", "modules: app/TestAssembly.dll, app/TestAssembly.dll; types: InMain in app/TestAssembly.dll; exported: InModule in file TestAssembly.dll = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: throws COMException, throws COMException, throws COMException, throws COMException, InMain in app/TestAssembly.dll; modules: throws COMException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = of File(Data.bin).Name", "modules: app/TestAssembly.dll, INVALID; types: InMain in app/TestAssembly.dll; exported: InModule in file Data.bin = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: throws BadImageFormatException, throws BadImageFormatException, throws BadImageFormatException, throws BadImageFormatException, InMain in app/TestAssembly.dll; modules: throws BadImageFormatException; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Data.bin).Name = of File(Extra.netmodule).Name", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Extra.netmodule", NotAsked)]
		[InlineData("File(Data.bin).Name = 'Missing.bin'", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Missing.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: throws FileNotFoundException")]
		[InlineData("File(Data.bin).Name = 'sub\\Data.bin'", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file sub\\Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: throws FileNotFoundException")]
		[InlineData("File(Data.bin).Name = '..\\Data.bin'", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file ..\\Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: throws FileNotFoundException")]
		[InlineData("File(Data.bin).Name = 0", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file ", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: throws FileNotFoundException; resources: Embedded: embedded 'Embedded content', Linked: throws FileNotFoundException")]
		// The hash of a file
		[InlineData("File(Extra.netmodule).HashValue = 0", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).HashValue = of File(Data.bin).HashValue", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).HashValue = 60000", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "throws BadImageFormatException")]
		[InlineData("File(Data.bin).HashValue = 0", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Data.bin).HashValue = of File(Extra.netmodule).HashValue", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		// The file of a type
		[InlineData("ExportedType(InModule).Implementation = 0", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in null = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ExportedType(InModule).Implementation = file Data.bin", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InMain in app/TestAssembly.dll; exported: InModule in file Data.bin = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ExportedType(InModule).Implementation = 400", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in null = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: throws BadImageFormatException, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ExportedType(InModule).Implementation = 3", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in null = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ExportedType(InModule).Implementation = 5", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InMain in app/TestAssembly.dll; exported: InModule in AssemblyRefMD = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		// The token that the type has in its module. .NET Framework uses the type with that token (it doesn't check
		// the name), and finds no type if the token doesn't exist. Without a token (0) the type is found by its name.
		// Fixed in this commit: dnlib always found the type by its name.
		[InlineData("ExportedType(InModule).TypeDefId = 0", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ExportedType(InModule).TypeDefId = 33554433", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: <Module> in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = <Module> in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: <Module> in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ExportedType(InModule).TypeDefId = 33558431", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ExportedType(InModule).TypeDefId = 4294967295", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = null; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		// Its name and flags (1 = public)
		[InlineData("ExportedType(InModule).TypeName = 'NoSuchType'", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: NoSuchType in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ExportedType(InModule).TypeName = 'inmodule'", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: inmodule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ExportedType(InModule).TypeNamespace = 'Namespace'", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: Namespace.InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ExportedType(InModule).TypeName = of TypeDef(InMain).Name", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InMain in file Extra.netmodule = InMain in app/TestAssembly.dll; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "throws BadImageFormatException")]
		[InlineData("ExportedType(InModule).TypeName = of TypeDef(InMain).Name; ExportedType(InModule).TypeDefId = 0", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InMain in file Extra.netmodule = InMain in app/TestAssembly.dll; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "throws BadImageFormatException")]
		[InlineData("ExportedType(InModule).Flags = 0", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ExportedType(InModule).Flags = 2097153", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		// The file of a resource, and the offset of the resource in that file
		[InlineData("ManifestResource(Linked).Implementation = 0", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: embedded 'Embedded content'", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: embedded 'Embedded content'")]
		[InlineData("ManifestResource(Linked).Implementation = file Extra.netmodule", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Extra.netmodule", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: no info")]
		[InlineData("ManifestResource(Linked).Implementation = 400", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: embedded ''", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: throws BadImageFormatException")]
		[InlineData("ManifestResource(Linked).Implementation = 3", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: embedded 'Embedded content'", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: embedded 'Embedded content'")]
		[InlineData("ManifestResource(Linked).Implementation = 7", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: embedded 'Embedded content'", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: embedded 'Embedded content'")]
		[InlineData("ManifestResource(Linked).Implementation = 6", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: embedded ''", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: throws BadImageFormatException")]
		[InlineData("ManifestResource(Linked).Offset = 1", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ManifestResource(Linked).Offset = 8", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ManifestResource(Linked).Offset = 19", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ManifestResource(Linked).Offset = 100", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ManifestResource(Linked).Offset = 4294967295", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ManifestResource(Linked).Flags = 0", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ManifestResource(Linked).Flags = 2", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ManifestResource(Linked).Name = of ManifestResource(Embedded).Name", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Embedded: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: embedded 'Embedded content', Embedded: embedded 'Embedded content'")]
		[InlineData("ManifestResource(Embedded).Implementation = file Data.bin", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: file Data.bin, Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: file Data.bin 'Content of the file', Linked: file Data.bin 'Content of the file'")]
		[InlineData("ManifestResource(Embedded).Implementation = file Extra.netmodule", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: file Extra.netmodule, Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: no info, Linked: file Data.bin 'Content of the file'")]
		[InlineData("ManifestResource(Embedded).Implementation = file Data.bin; ManifestResource(Embedded).Offset = 0", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule = InModule in app/Extra.netmodule; resources: Embedded: file Data.bin, Linked: file Data.bin", "types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule, app/Data.bin; resources: Embedded: file Data.bin 'Content of the file', Linked: file Data.bin 'Content of the file'")]
		public void FilesTypesAndResources(string changes, string expectedDnlib, string expectedNetFramework) {
			var mainFile = WriteFiles(CreateImage(changes));

			if (expectedNetFramework == NotAsked) {
				Assert.Equal(expectedDnlib, ReadWithDnlib(mainFile));
				return;
			}
			if (expectedNetFramework == TypeIsNotFound) {
				Assert.Equal(expectedDnlib, ReadWithDnlib(mainFile));
				if (!ClrReaderComparison.IsDotNet && !ClrReaderComparison.IsRuntimeNotAsked) {
					string runtime;
					try {
						runtime = ReadWithTheRuntime(mainFile);
					}
					catch (Exception ex) {
						runtime = "throws " + ex.GetType().Name;
					}
					Assert.DoesNotContain("InModule", runtime);
				}
				return;
			}
			ClrReaderComparison.AssertReadOnNetFramework(expectedDnlib, ReadWithDnlib(mainFile), expectedNetFramework, () => ReadWithTheRuntime(mainFile));
		}

		// Windows finds a file if the name has other upper and lower case letters, and it ignores dots and spaces
		// at the end of a name. Fixed in this commit: dnlib didn't load a module whose name ends with a dot or a
		// space.
		[Theory]
		[InlineData("File(Extra.netmodule).Name = 'EXTRA.NETMODULE'", "modules: app/TestAssembly.dll, app/EXTRA.NETMODULE; types: InModule in app/EXTRA.NETMODULE, InMain in app/TestAssembly.dll; exported: InModule in file EXTRA.NETMODULE = InModule in app/EXTRA.NETMODULE; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/EXTRA.NETMODULE, InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/EXTRA.NETMODULE, app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = 'Extra.netmodule '", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule  = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule , InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule , app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = 'Extra.netmodule.'", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule. = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule., InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule., app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		[InlineData("File(Extra.netmodule).Name = 'Extra.netmodule. .'", "modules: app/TestAssembly.dll, app/Extra.netmodule; types: InModule in app/Extra.netmodule, InMain in app/TestAssembly.dll; exported: InModule in file Extra.netmodule. . = InModule in app/Extra.netmodule; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin", "types: InModule in app/Extra.netmodule. ., InMain in app/TestAssembly.dll; modules: app/TestAssembly.dll, app/Extra.netmodule. ., app/Data.bin; resources: Embedded: embedded 'Embedded content', Linked: file Data.bin 'Content of the file'")]
		public void FileNamesThatOnlyWindowsFinds(string changes, string expectedDnlib, string expectedNetFramework) {
			Assert.SkipUnless(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows), "The names are other files if this isn't Windows");
			FilesTypesAndResources(changes, expectedDnlib, expectedNetFramework);
		}
	}
}
