// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Loading {
	/// <summary>
	/// An assembly can have more than one module: the main module (with the assembly's manifest) lists the
	/// other files in its File table, and they must be in the same directory. <c>AssemblyDef.Modules</c>
	/// loads them when it's read. The tests write such an assembly to a directory.
	/// </summary>
	public sealed class MultiModuleAssemblyTests : IDisposable {
		readonly TempDirectory directory = new TempDirectory();

		public void Dispose() => directory.Dispose();

		// Writes Main.dll (class InMain) and Other.netmodule (class InOther) of the assembly "Main"
		string WriteAssembly() {
			var main = new ModuleDefUser("Main.dll");
			var other = new ModuleDefUser("Other.netmodule") { Kind = ModuleKind.NetModule };
			var assembly = new AssemblyDefUser("Main", new Version(1, 0, 0, 0));
			assembly.Modules.Add(main);
			assembly.Modules.Add(other);
			main.Types.Add(new TypeDefUser("Ns", "InMain", main.CorLibTypes.Object.TypeDefOrRef));
			other.Types.Add(new TypeDefUser("Ns", "InOther", other.CorLibTypes.Object.TypeDefOrRef) { Attributes = TypeAttributes.Public });
			// Like a compiler does: the main module says in which file the public type of the other module is.
			// That's also what puts the file into the File table, the writer doesn't add the assembly's
			// other modules by itself.
			var file = new FileDefUser("Other.netmodule", dnlib.DotNet.FileAttributes.ContainsMetadata, null);
			main.ExportedTypes.Add(new ExportedTypeUser(main, 0, "Ns", "InOther", TypeAttributes.Public, file));
			other.Write(directory.GetPath("Other.netmodule"));
			main.Write(directory.GetPath("Main.dll"));
			return directory.GetPath("Main.dll");
		}

		static void DisposeModules(AssemblyDef assembly) {
			foreach (var module in assembly.Modules.ToArray())
				module.Dispose();
		}

		[Fact]
		public void OtherModuleIsLoadedFromTheDirectoryOfTheMainModule() {
			var assembly = AssemblyDef.Load(WriteAssembly());
			try {
				Assert.Equal(new[] { "Main.dll", "Other.netmodule" }, assembly.Modules.Select(a => a.Name.String));
				var other = assembly.Modules[1];
				Assert.Same(assembly, other.Assembly);
				Assert.False(other.IsManifestModule);
				Assert.Equal(directory.GetPath("Other.netmodule"), other.Location);

				// The types of both modules are types of the assembly
				Assert.NotNull(assembly.Find("Ns.InMain", isReflectionName: true));
				Assert.Same(other, assembly.Find("Ns.InOther", isReflectionName: true)!.Module);
				Assert.Same(other, assembly.FindModule("OTHER.netmodule"));
			}
			finally {
				DisposeModules(assembly);
			}
		}

		[Fact]
		public void ModuleWhoseFileIsMissingIsNotInTheList() {
			var mainFile = WriteAssembly();
			File.Delete(directory.GetPath("Other.netmodule"));

			var assembly = AssemblyDef.Load(mainFile);
			try {
				Assert.Equal(new[] { "Main.dll" }, assembly.Modules.Select(a => a.Name.String));
				// The row is still there
				var mainModule = (ModuleDefMD)assembly.ManifestModule!;
				Assert.Equal(1U, mainModule.TablesStream.FileTable.Rows);
				var file = mainModule.ResolveFile(1)!;
				Assert.Equal("Other.netmodule", file.Name);
				Assert.True(file.ContainsMetadata);
				Assert.Null(assembly.Find("Ns.InOther", isReflectionName: true));
			}
			finally {
				DisposeModules(assembly);
			}
		}

		// The file exists, so the list has a module for it: an empty module with the name INVALID
		[Fact]
		public void FileThatIsNotAModuleIsAnEmptyModule() {
			var mainFile = WriteAssembly();
			File.WriteAllText(directory.GetPath("Other.netmodule"), "this is not a PE file");

			var assembly = AssemblyDef.Load(mainFile);
			try {
				Assert.Equal(new[] { "Main.dll", "INVALID" }, assembly.Modules.Select(a => a.Name.String));
				var placeholder = assembly.Modules[1];
				Assert.Same(assembly, placeholder.Assembly);
				Assert.DoesNotContain(placeholder.Types, a => !a.IsGlobalModuleType);
				Assert.Null(assembly.Find("Ns.InOther", isReflectionName: true));
			}
			finally {
				DisposeModules(assembly);
			}
		}

		// An assembly that's loaded from memory has no directory, so no other module is loaded
		[Fact]
		public void AssemblyThatIsLoadedFromMemoryOnlyHasItsMainModule() {
			var image = File.ReadAllBytes(WriteAssembly());

			var assembly = AssemblyDef.Load(image);

			Assert.Equal(new[] { "Main.dll" }, assembly.Modules.Select(a => a.Name.String));
			Assert.Equal(1U, ((ModuleDefMD)assembly.ManifestModule!).TablesStream.FileTable.Rows);
		}
	}
}
