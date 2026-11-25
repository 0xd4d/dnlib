// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.Tests.Reading;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Resolving {
	/// <summary>
	/// A row of the AssemblyRef table is the name, version, culture and public key token of another assembly.
	/// The runtime finds the file of that assembly in the directory of the program and in some directories in
	/// it; dnlib's <see cref="AssemblyResolver"/> does the same. These tests write names, cultures and versions
	/// that a compiler doesn't write and ask dnlib for the file. .NET Framework is asked for the same if it runs
	/// the tests (.NET has other rules: the program has a list of all its files). If .NET Framework finds a
	/// file, dnlib must find the same file.
	/// </summary>
	public sealed class ClrAssemblyBindingRuleTests : IDisposable {
		// Every copy of the library says where its file is
		const string LibrarySource = @"
public class Library {
	public static string Location => typeof(Library).Assembly.Location;
}
";
		const string GermanLibrarySource = @"[assembly: System.Reflection.AssemblyCulture(""de"")]" + LibrarySource;
		// An instance of Program stores the location or the exception where the code that created it finds it
		const string MainSource = @"
using System;
using System.Runtime.CompilerServices;
public class Program {
	[MethodImpl(MethodImplOptions.NoInlining)]
	static string GetLibraryLocation() => Library.Location;

	public Program() {
		string result;
		try {
			result = GetLibraryLocation();
		}
		catch (Exception ex) {
			result = ""throws "" + ex.GetType().Name;
		}
		AppDomain.CurrentDomain.SetData(""result"", result);
	}
}
";
		static readonly CSharpCompilerOptions libraryOptions = new CSharpCompilerOptions { AssemblyName = "Lib" };
		static readonly byte[] libraryImage = CSharpCompiler.Compile("[assembly: System.Reflection.AssemblyVersion(\"1.2.3.4\")]" + LibrarySource, libraryOptions).Image;
		static readonly byte[] germanLibraryImage = CSharpCompiler.Compile("[assembly: System.Reflection.AssemblyVersion(\"1.2.3.4\")]" + GermanLibrarySource, libraryOptions).Image;
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(MainSource, compilerOptions: CreateOptions(libraryImage));

		// The same library with a strong name, and the code that uses it
		static readonly StrongNameKey key = CreateKey();
		static readonly byte[] signedLibraryImage = Sign(libraryImage);
		static readonly byte[] signedGermanLibraryImage = Sign(germanLibraryImage);
		static readonly byte[] signedBaseImage = ClrReaderComparison.CreateBaseImage(MainSource, compilerOptions: CreateOptions(signedLibraryImage));

		static CSharpCompilerOptions CreateOptions(byte[] library) {
			var options = new CSharpCompilerOptions { AssemblyName = "Main" };
			options.References.Add(library);
			return options;
		}

		// A new key pair in the format of an .snk file
		static StrongNameKey CreateKey() {
			using (var rsa = new System.Security.Cryptography.RSACryptoServiceProvider(1024)) {
				var blob = rsa.ExportCspBlob(includePrivateParameters: true);
				// The key's algorithm id is at offset 4. .NET exports a "key exchange" key (CALG_RSA_KEYX, 0xA400),
				// strong name keys (sn.exe -k) are signature keys (CALG_RSA_SIGN, 0x2400). The key is the same.
				blob[4] = 0x00;
				blob[5] = 0x24;
				return new StrongNameKey(blob);
			}
		}

		static byte[] Sign(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var options = new dnlib.DotNet.Writer.ModuleWriterOptions(module);
				options.InitializeStrongNameSigning(module, key);
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		}

		readonly TempDirectory directory = new TempDirectory();

		public void Dispose() => directory.Dispose();

		// The directory of the program is "app". The library is there, in directories in it, and in the directory
		// above it. The one in the directory "de" has the culture "de".
		string WriteFiles(byte[] mainImage, bool signed = false) {
			foreach (var relativePath in new[] { "app", "app/Lib", "app/sub", "app/de", "app/de/Lib", "" }) {
				var path = directory.GetPath(relativePath.Replace('/', Path.DirectorySeparatorChar));
				Directory.CreateDirectory(path);
				File.WriteAllBytes(Path.Combine(path, "Lib.dll"), relativePath.StartsWith("app/de") ? (signed ? signedGermanLibraryImage : germanLibraryImage) : (signed ? signedLibraryImage : libraryImage));
			}
			var mainFile = Path.Combine(directory.GetPath("app"), "Main.dll");
			File.WriteAllBytes(mainFile, mainImage);
			return mainFile;
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

		// Adds a blob to the end of the #Blob heap and returns its offset
		static uint AddBlob(ref byte[] image, byte[] blob) {
			var block = new MetadataBlock(image);
			if (block.Streams.Any(a => a.Name == ClrReaderComparison.PaddingStreamName))
				block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			var heap = block.GetStream("#Blob");
			uint offset = (uint)heap.Data.Length;
			var length = blob.Length < 0x80 ? new[] { (byte)blob.Length } : new[] { (byte)(0x80 | (blob.Length >> 8)), (byte)blob.Length };
			heap.Data = heap.Data.Concat(length).Concat(blob).ToArray();
			image = block.Write();
			return offset;
		}

		// "Column = value", separated by "; ": a column of the AssemblyRef row of the library. A value is
		//	a number
		//	'text'			a new string. {dir} is the full path of the program's directory.
		//	[hex bytes]		a new blob
		//	the public key	a new blob: the public key of the library with a strong name
		byte[] CreateImage(string changes, bool signed = false) {
			var image = signed ? signedBaseImage : baseImage;
			uint row = new MetadataPatcher(image).FindRow(Table.AssemblyRef, "Name", "Lib");
			var cells = new List<(string column, uint value)>();
			foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
				var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
				uint value;
				if (parts[1].StartsWith("'"))
					value = AddString(ref image, parts[1].Substring(1, parts[1].Length - 2).Replace("{dir}", directory.GetPath("app")));
				else if (parts[1].StartsWith("["))
					value = AddBlob(ref image, parts[1].Trim('[', ']').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(a => Convert.ToByte(a, 16)).ToArray());
				else if (parts[1] == "the public key")
					value = AddBlob(ref image, key.PublicKey);
				else
					value = uint.Parse(parts[1]);
				cells.Add((parts[0], value));
			}
			var patcher = new MetadataPatcher(image);
			foreach (var cell in cells)
				patcher.SetCell(Table.AssemblyRef, row, cell.column, cell.value);
			return patcher.ToArray();
		}

		// A path in the test's directory without the directory
		static string ToRelativePath(string path, string testDirectory) =>
			path.StartsWith(testDirectory, StringComparison.OrdinalIgnoreCase) ? path.Substring(testDirectory.Length + 1).Replace('\\', '/') : "another directory";

		// The file of the library that the resolver finds for the main file's reference
		string ReadWithDnlib(string mainFile, bool findExactMatch = false) {
			var context = ModuleDef.CreateModuleContext();
			((AssemblyResolver)context.AssemblyResolver).FindExactMatch = findExactMatch;
			using (var module = ModuleDefMD.Load(mainFile, context)) {
				var reference = module.GetAssemblyRefs().Single(a => a.Name != module.CorLibTypes.AssemblyRef.Name);
				var assembly = context.AssemblyResolver.Resolve(reference, module);
				if (assembly is null)
					return "null";
				try {
					return ToRelativePath(assembly.ManifestModule!.Location, directory.Path);
				}
				finally {
					foreach (var other in assembly.Modules)
						other.Dispose();
				}
			}
		}

		// Runs the code in an AppDomain whose directory is the directory of the program
		string ReadWithTheRuntime(string mainFile) {
#if NETFRAMEWORK
			var domain = AppDomain.CreateDomain(nameof(ClrAssemblyBindingRuleTests), null, new AppDomainSetup { ApplicationBase = Path.GetDirectoryName(mainFile) });
			try {
				// The object stays in the AppDomain
				domain.CreateInstance("Main", "Program");
				var result = (string)domain.GetData("result");
				return result.StartsWith("throws ") ? result : ToRelativePath(result, directory.Path);
			}
			finally {
				AppDomain.Unload(domain);
			}
#else
			throw new NotSupportedException();
#endif
		}

		[Theory]
		[InlineData("", "app/Lib.dll", "app/Lib.dll")]
		// The name
		[InlineData("Name = 'NoSuchLib'", "null", "throws FileNotFoundException")]
		[InlineData("Name = 'Lib.dll'", "null", "throws FileNotFoundException")]
		[InlineData("Name = 'Lib '", "app/Lib.dll", "throws FileNotFoundException")]
		[InlineData("Name = ' Lib'", "app/Lib.dll", "throws FileNotFoundException")]
		[InlineData("Name = 'Lib.'", "null", "throws FileNotFoundException")]
		[InlineData("Name = 'Li*'", "null", "throws FileNotFoundException")]
		[InlineData("Name = 'Lib:x'", "null", "throws FileNotFoundException")]
		[InlineData("Name = 0", "null", "throws FileLoadException")]
		// A name with a directory. Fixed in this commit: dnlib opened such a file (it never was the result: the
		// assembly in the file has another name).
		[InlineData("Name = 'sub\\Lib'", "null", "throws FileNotFoundException")]
		[InlineData("Name = 'sub/Lib'", "null", "throws FileNotFoundException")]
		[InlineData("Name = '.\\Lib'", "null", "throws FileNotFoundException")]
		[InlineData("Name = './Lib'", "null", "throws FileNotFoundException")]
		[InlineData("Name = '..\\Lib'", "null", "throws FileNotFoundException")]
		[InlineData("Name = '../Lib'", "null", "throws FileNotFoundException")]
		[InlineData("Name = '\\Lib'", "null", "throws FileNotFoundException")]
		[InlineData("Name = '{dir}\\Lib'", "null", "throws FileNotFoundException")]
		[InlineData("Name = '{dir}\\sub\\Lib'", "null", "throws FileNotFoundException")]
		// The culture: the runtime looks for the library in a directory with the name of the culture, and only
		// there. Fixed in this commit: dnlib didn't look in that directory.
		[InlineData("Locale = 'de'", "app/de/Lib.dll", "app/de/Lib.dll")]
		[InlineData("Locale = 'de-DE'", "app/Lib.dll", "throws FileNotFoundException")]
		[InlineData("Locale = 'fr'", "app/Lib.dll", "throws FileNotFoundException")]
		[InlineData("Locale = 'neutral'", "app/Lib.dll", "app/Lib.dll")]
		[InlineData("Locale = 'sub'", "app/sub/Lib.dll", "throws FileLoadException")]
		[InlineData("Locale = 'de;x'", "app/Lib.dll", "throws FileNotFoundException")]
		[InlineData("Locale = 'de,x'", "app/Lib.dll", "throws FileNotFoundException")]
		[InlineData("Locale = 'de '", "app/Lib.dll", "throws FileNotFoundException")]
		[InlineData("Locale = '..'", "app/Lib.dll", "throws FileNotFoundException")]
		[InlineData("Locale = '.'", "app/Lib.dll", "throws FileLoadException")]
		[InlineData("Locale = 'de\\..\\sub'", "app/Lib.dll", "throws FileNotFoundException")]
		[InlineData("Locale = '{dir}\\sub'", "app/Lib.dll", "throws FileNotFoundException")]
		// The version: the library has no strong name, so every version is the same assembly. 65535 = no value.
		[InlineData("MajorVersion = 9", "app/Lib.dll", "app/Lib.dll")]
		[InlineData("MajorVersion = 0; MinorVersion = 0; BuildNumber = 0; RevisionNumber = 0", "app/Lib.dll", "app/Lib.dll")]
		[InlineData("RevisionNumber = 65535", "app/Lib.dll", "app/Lib.dll")]
		[InlineData("BuildNumber = 65535; RevisionNumber = 65535", "app/Lib.dll", "app/Lib.dll")]
		[InlineData("MajorVersion = 65535", "app/Lib.dll", "app/Lib.dll")]
		[InlineData("MajorVersion = 65535; MinorVersion = 65535; BuildNumber = 65535; RevisionNumber = 65535", "app/Lib.dll", "app/Lib.dll")]
		// Flags: 1 = the blob is a public key and not a token, 0x100 = retargetable
		[InlineData("Flags = 1", "app/Lib.dll", "app/Lib.dll")]
		[InlineData("Flags = 256", "app/Lib.dll", "app/Lib.dll")]
		[InlineData("Flags = 4294967294", "app/Lib.dll", "throws BadImageFormatException")]
		public void LibraryOfAReference(string changes, string expectedDnlib, string expectedNetFramework) {
			var mainFile = WriteFiles(CreateImage(changes));

			ClrReaderComparison.AssertReadOnNetFramework(expectedDnlib, ReadWithDnlib(mainFile), expectedNetFramework, () => ReadWithTheRuntime(mainFile));
		}

		// The library has a strong name: version 1.2.3.4, a public key, and the reference has the token of the key.
		// The runtime then only loads a library with the version and the key of the reference. dnlib's resolver
		// does the same if its FindExactMatch is true; by default it also returns a library that isn't the same.
		// The value of dnlib is "the file by default; exact: the file if FindExactMatch is true".
		[Theory]
		[InlineData("", "app/Lib.dll; exact: app/Lib.dll", "app/Lib.dll")]
		// The version. 65535 = no value.
		[InlineData("RevisionNumber = 5", "app/Lib.dll; exact: null", "throws FileLoadException")]
		[InlineData("RevisionNumber = 3", "app/Lib.dll; exact: null", "throws FileLoadException")]
		[InlineData("MajorVersion = 2", "app/Lib.dll; exact: null", "throws FileLoadException")]
		[InlineData("MajorVersion = 0; MinorVersion = 0; BuildNumber = 0; RevisionNumber = 0", "app/Lib.dll; exact: null", "throws FileLoadException")]
		[InlineData("RevisionNumber = 65535", "app/Lib.dll; exact: app/Lib.dll", "app/Lib.dll")]
		[InlineData("BuildNumber = 65535", "app/Lib.dll; exact: app/Lib.dll", "app/Lib.dll")]
		[InlineData("BuildNumber = 65535; RevisionNumber = 65535", "app/Lib.dll; exact: app/Lib.dll", "app/Lib.dll")]
		[InlineData("MinorVersion = 65535; BuildNumber = 65535; RevisionNumber = 65535", "app/Lib.dll; exact: app/Lib.dll", "app/Lib.dll")]
		[InlineData("MinorVersion = 65535", "app/Lib.dll; exact: app/Lib.dll", "app/Lib.dll")]
		[InlineData("MajorVersion = 65535", "app/Lib.dll; exact: app/Lib.dll", "app/Lib.dll")]
		[InlineData("MajorVersion = 65535; MinorVersion = 65535; BuildNumber = 65535; RevisionNumber = 65535", "app/Lib.dll; exact: app/Lib.dll", "app/Lib.dll")]
		[InlineData("MajorVersion = 9; RevisionNumber = 65535", "app/Lib.dll; exact: null", "throws FileLoadException")]
		[InlineData("BuildNumber = 9; RevisionNumber = 65535", "app/Lib.dll; exact: null", "throws FileLoadException")]
		[InlineData("MinorVersion = 9; BuildNumber = 65535; RevisionNumber = 65535", "app/Lib.dll; exact: null", "throws FileLoadException")]
		// The public key token, or the public key if the flag 1 is set
		[InlineData("PublicKeyOrToken = 0", "app/Lib.dll; exact: null", "throws FileLoadException")]
		[InlineData("PublicKeyOrToken = [01 02 03 04 05 06 07 08]", "app/Lib.dll; exact: null", "throws FileLoadException")]
		[InlineData("PublicKeyOrToken = [01 02 03]", "app/Lib.dll; exact: null", "throws FileLoadException")]
		[InlineData("PublicKeyOrToken = the public key", "app/Lib.dll; exact: null", "throws ArgumentException")]
		[InlineData("PublicKeyOrToken = the public key; Flags = 1", "app/Lib.dll; exact: app/Lib.dll", "app/Lib.dll")]
		[InlineData("Flags = 1", "app/Lib.dll; exact: null", "throws SecurityException")]
		[InlineData("PublicKeyOrToken = 0; Flags = 1", "app/Lib.dll; exact: null", "throws FileLoadException")]
		[InlineData("Flags = 256", "app/Lib.dll; exact: app/Lib.dll", "throws FileLoadException")]
		// The culture
		[InlineData("Locale = 'de'", "app/de/Lib.dll; exact: app/de/Lib.dll", "app/de/Lib.dll")]
		[InlineData("Locale = 'fr'", "app/Lib.dll; exact: null", "throws FileNotFoundException")]
		[InlineData("Locale = 'sub'", "app/sub/Lib.dll; exact: null", "throws FileLoadException")]
		[InlineData("Locale = 'de'; RevisionNumber = 5", "app/de/Lib.dll; exact: null", "throws FileLoadException")]
		// The name
		[InlineData("Name = 'NoSuchLib'", "null; exact: null", "throws FileNotFoundException")]
		public void LibraryWithAStrongNameOfAReference(string changes, string expectedDnlib, string expectedNetFramework) {
			var mainFile = WriteFiles(CreateImage(changes, signed: true), signed: true);
			string dnlib = $"{ReadWithDnlib(mainFile)}; exact: {ReadWithDnlib(mainFile, findExactMatch: true)}";

			ClrReaderComparison.AssertReadOnNetFramework(expectedDnlib, dnlib, expectedNetFramework, () => ReadWithTheRuntime(mainFile));
		}

		// Shows the files that the resolver wants to open
		sealed class ResolverWithVisibleFiles : AssemblyResolver {
			public List<string> GetFiles(IAssembly assembly, ModuleDef module) {
				var files = new List<string>();
				foreach (bool matchExactly in new[] { true, false }) {
					files.AddRange(PreFindAssemblies(assembly, module, matchExactly) ?? new string[0]);
					files.AddRange(FindAssemblies(assembly, module, matchExactly) ?? new string[0]);
					files.AddRange(PostFindAssemblies(assembly, module, matchExactly) ?? new string[0]);
				}
				return files;
			}
		}

		// The file "sub/Lib.dll" exists in every search path, but a name with a directory or a full path is no
		// file name: such a name could make the resolver open any file, also one on another computer.
		// Fixed in this commit: the resolver opened that file.
		[Theory]
		[InlineData("Lib", true)]
		[InlineData("sub/Lib", false)]
		[InlineData("sub\\Lib", false)]
		[InlineData("../Lib", false)]
		[InlineData("..\\Lib", false)]
		[InlineData("{dir}/sub/Lib", false)]
		[InlineData("\\\\localhost\\NoSuchShare\\Lib", false)]
		public void NameWithADirectoryIsNotAFileThatIsOpened(string name, bool isSearched) {
			var mainFile = WriteFiles(baseImage);
			var resolver = new ResolverWithVisibleFiles();
			resolver.PreSearchPaths.Add(directory.GetPath("app"));
			resolver.PostSearchPaths.Add(directory.GetPath("app"));
			using (var module = ModuleDefMD.Load(mainFile, new ModuleContext(resolver))) {
				var reference = new AssemblyRefUser(name.Replace("{dir}", directory.GetPath("app")), new Version(1, 2, 3, 4));

				var files = resolver.GetFiles(reference, module);

				Assert.Equal(isSearched, files.Count != 0);
				Assert.Equal(isSearched, resolver.Resolve(reference, module) is not null);
				resolver.Clear();
			}
		}

		// Windows finds a file or a directory if the name has other upper and lower case letters
		[Theory]
		[InlineData("Name = 'lib'", "app/lib.dll", "app/lib.dll")]
		[InlineData("Locale = 'DE'", "app/DE/Lib.dll", "app/DE/Lib.dll")]
		public void NamesThatOnlyWindowsFinds(string changes, string expectedDnlib, string expectedNetFramework) {
			Assert.SkipUnless(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows), "The names are other files if this isn't Windows");
			LibraryOfAReference(changes, expectedDnlib, expectedNetFramework);
		}
	}
}
