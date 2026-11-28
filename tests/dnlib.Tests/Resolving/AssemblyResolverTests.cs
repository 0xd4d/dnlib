// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Resolving {
	/// <summary>
	/// <see cref="AssemblyResolver"/> finds the file of an assembly reference in its search paths, reads it
	/// and remembers it. The tests write small assemblies to a temporary directory.
	/// </summary>
	public sealed class AssemblyResolverTests : IDisposable {
		readonly TempDirectory directory = new TempDirectory();
		// Assemblies in the GAC or in the runtime directory can't get in the way: these names don't exist there
		const string LibraryName = "DnlibTestLibrary";

		public void Dispose() => directory.Dispose();

		// Creates the directory 'directoryName' with an assembly 'name' that has the version
		string WriteLibrary(string directoryName, string name, string version) {
			var source = $"[assembly: System.Reflection.AssemblyVersion(\"{version}\")] public class LibraryClass {{ }}";
			var image = CSharpCompiler.Compile(source, new CSharpCompilerOptions { AssemblyName = name }).Image;
			var libraryDirectory = directory.GetPath(directoryName);
			Directory.CreateDirectory(libraryDirectory);
			var file = Path.Combine(libraryDirectory, name + ".dll");
			File.WriteAllBytes(file, image);
			return file;
		}

		static AssemblyResolver CreateResolver(params string[] searchPaths) {
			// No GAC: only the search paths are used
			var resolver = new AssemblyResolver { UseGAC = false };
			foreach (var path in searchPaths)
				resolver.PreSearchPaths.Add(path);
			resolver.DefaultModuleContext = new ModuleContext(resolver);
			return resolver;
		}

		static AssemblyRefUser CreateReference(string name, string version) => new AssemblyRefUser(name, new Version(version));

		// The resolver keeps the files open
		static void DisposeAll(AssemblyResolver resolver) {
			foreach (var assembly in resolver.GetCachedAssemblies().Distinct().ToList()) {
				foreach (var module in assembly.Modules)
					module.Dispose();
			}
			resolver.Clear();
		}

		[Fact]
		public void AssemblyIsFoundInASearchPath() {
			var file = WriteLibrary("libs", LibraryName, "1.2.3.4");
			var resolver = CreateResolver(Path.GetDirectoryName(file)!);
			try {
				var assembly = resolver.Resolve(CreateReference(LibraryName, "1.2.3.4"), sourceModule: null);

				Assert.NotNull(assembly);
				Assert.Equal(LibraryName, assembly.Name);
				Assert.Equal(new Version(1, 2, 3, 4), assembly.Version);
				Assert.Equal(file, assembly.ManifestModule!.Location);
				Assert.NotNull(assembly.ManifestModule.Find("LibraryClass", isReflectionName: true));
			}
			finally {
				DisposeAll(resolver);
			}
		}

		// The paths are searched in this order: PreSearchPaths, the directory of the module that has the
		// reference, PostSearchPaths
		[Fact]
		public void PostSearchPathsAreSearchedLast() {
			var inPrePath = WriteLibrary("pre", LibraryName, "1.0.0.0");
			var inPostPath = WriteLibrary("post", LibraryName, "1.0.0.0");
			var onlyInPostPath = WriteLibrary("post", "OnlyInPostPath", "1.0.0.0");
			var resolver = CreateResolver(Path.GetDirectoryName(inPrePath)!);
			resolver.PostSearchPaths.Add(Path.GetDirectoryName(inPostPath)!);
			try {
				Assert.Equal(inPrePath, resolver.Resolve(CreateReference(LibraryName, "1.0.0.0"), sourceModule: null)!.ManifestModule!.Location);
				Assert.Equal(onlyInPostPath, resolver.Resolve(CreateReference("OnlyInPostPath", "1.0.0.0"), sourceModule: null)!.ManifestModule!.Location);
			}
			finally {
				DisposeAll(resolver);
			}

			var postOnlyResolver = CreateResolver();
			postOnlyResolver.PostSearchPaths.Add(Path.GetDirectoryName(inPostPath)!);
			try {
				Assert.Equal(inPostPath, postOnlyResolver.Resolve(CreateReference(LibraryName, "1.0.0.0"), sourceModule: null)!.ManifestModule!.Location);
			}
			finally {
				DisposeAll(postOnlyResolver);
			}
		}

		// The modules of a resolved assembly keep a table of their types by name (types are found often
		// in a referenced assembly), unless the resolver is told not to do that
		[Theory, InlineData(true), InlineData(false)]
		public void ResolvedAssemblyHasATypeCacheIfTheResolverEnablesIt(bool enableTypeDefCache) {
			var file = WriteLibrary("libs", LibraryName, "1.0.0.0");
			var resolver = CreateResolver(Path.GetDirectoryName(file)!);
			Assert.True(resolver.EnableTypeDefCache);
			resolver.EnableTypeDefCache = enableTypeDefCache;
			try {
				var assembly = resolver.Resolve(CreateReference(LibraryName, "1.0.0.0"), sourceModule: null)!;

				Assert.Equal(enableTypeDefCache, assembly.ManifestModule!.EnableTypeDefFindCache);
				Assert.NotNull(assembly.Find("LibraryClass", isReflectionName: true));
			}
			finally {
				DisposeAll(resolver);
			}
		}

		[Fact]
		public void AssemblyIsResolvedOnce() {
			var file = WriteLibrary("libs", LibraryName, "1.0.0.0");
			var resolver = CreateResolver(Path.GetDirectoryName(file)!);
			try {
				var first = resolver.Resolve(CreateReference(LibraryName, "1.0.0.0"), sourceModule: null);
				var second = resolver.Resolve(CreateReference(LibraryName, "1.0.0.0"), sourceModule: null);

				Assert.NotNull(first);
				Assert.Same(first, second);
				Assert.Same(first, Assert.Single(resolver.GetCachedAssemblies().Distinct()));
			}
			finally {
				DisposeAll(resolver);
			}
		}

		[Fact]
		public void UnknownAssemblyIsNull() {
			var resolver = CreateResolver(directory.Path);

			Assert.Null(resolver.Resolve(CreateReference("DnlibTestMissingLibrary", "1.0.0.0"), sourceModule: null));
			Assert.Null(resolver.Resolve(null, sourceModule: null));
		}

		// An assembly next to the module that references it is found without a search path
		[Fact]
		public void AssemblyIsFoundNextToTheModuleThatReferencesIt() {
			var libraryFile = WriteLibrary("app", LibraryName, "1.0.0.0");
			var appFile = WriteLibrary("app", "DnlibTestApp", "1.0.0.0");
			var resolver = CreateResolver();
			try {
				using (var app = ModuleDefMD.Load(appFile, resolver.DefaultModuleContext)) {
					var assembly = resolver.Resolve(CreateReference(LibraryName, "1.0.0.0"), app);

					Assert.NotNull(assembly);
					Assert.Equal(libraryFile, assembly.ManifestModule!.Location);
					Assert.Null(resolver.Resolve(CreateReference(LibraryName + "2", "1.0.0.0"), app));
				}
			}
			finally {
				DisposeAll(resolver);
			}
		}

		// Creates the directory 'directoryName' with an assembly 'name' that has the culture
		string WriteLibraryWithCulture(string directoryName, string name, string culture) {
			var source = $"[assembly: System.Reflection.AssemblyVersion(\"1.0.0.0\")] [assembly: System.Reflection.AssemblyCulture(\"{culture}\")]";
			var image = CSharpCompiler.Compile(source, new CSharpCompilerOptions { AssemblyName = name }).Image;
			var libraryDirectory = directory.GetPath(directoryName);
			Directory.CreateDirectory(libraryDirectory);
			var file = Path.Combine(libraryDirectory, name + ".dll");
			File.WriteAllBytes(file, image);
			return file;
		}

		static AssemblyRefUser CreateReference(string name, string version, string culture) =>
			new AssemblyRefUser(name, new Version(version), null, culture);

		// The resources of a program for a language are in a "satellite assembly": App.resources.dll in a directory
		// with the name of the culture. The runtime finds an assembly with a culture there, and so does the resolver.
		// Fixed in 9e081cff (the directory of a module) and in this commit (the other search paths): only the
		// directories themselves were searched.
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void SatelliteAssemblyIsFoundInTheDirectoryOfItsCulture(bool inASearchPath) {
			var appFile = WriteLibrary("app", "DnlibTestApp", "1.0.0.0");
			string germanFile = WriteLibraryWithCulture(Path.Combine(inASearchPath ? "other" : "app", "de"), "DnlibTestApp.resources", "de");
			string frenchFile = WriteLibraryWithCulture(Path.Combine(inASearchPath ? "other" : "app", "fr"), "DnlibTestApp.resources", "fr");
			var resolver = inASearchPath ? CreateResolver(directory.GetPath("other")) : CreateResolver();
			try {
				using (var app = ModuleDefMD.Load(appFile, resolver.DefaultModuleContext)) {
					// Only the directory of the reference's culture is searched. (This is asked first: an assembly with
					// the same name that was found before is the closest match of a reference with another culture.)
					Assert.Null(resolver.Resolve(CreateReference("DnlibTestApp.resources", "1.0.0.0", "es"), app));
					Assert.Null(resolver.Resolve(CreateReference("DnlibTestApp.resources2", "1.0.0.0", "de"), app));

					var german = resolver.Resolve(CreateReference("DnlibTestApp.resources", "1.0.0.0", "de"), app);
					var french = resolver.Resolve(CreateReference("DnlibTestApp.resources", "1.0.0.0", "fr"), app);

					Assert.Equal(germanFile, german?.ManifestModule!.Location);
					Assert.Equal("de", german!.Culture);
					Assert.Equal(frenchFile, french?.ManifestModule!.Location);
				}
			}
			finally {
				DisposeAll(resolver);
			}
		}

		// A reference without a culture isn't searched in the directory of a culture
		[Fact]
		public void AssemblyWithoutACultureIsNotFoundInTheDirectoryOfACulture() {
			var appFile = WriteLibrary("app", "DnlibTestApp", "1.0.0.0");
			WriteLibraryWithCulture(Path.Combine("app", "de"), "DnlibTestApp.resources", "de");
			var resolver = CreateResolver();
			try {
				using (var app = ModuleDefMD.Load(appFile, resolver.DefaultModuleContext)) {
					Assert.Null(resolver.Resolve(CreateReference("DnlibTestApp.resources", "1.0.0.0"), app));
					Assert.Null(resolver.Resolve(CreateReference("DnlibTestApp.resources", "1.0.0.0", "neutral"), app));
				}
			}
			finally {
				DisposeAll(resolver);
			}
		}

		// A culture that can't be the name of a directory isn't used as one
		[Theory]
		[InlineData("..")]
		[InlineData("de/..")]
		[InlineData("de\\..\\de")]
		[InlineData("de;x")]
		public void CultureThatIsNotAWordIsNotADirectory(string culture) {
			var appFile = WriteLibrary("app", "DnlibTestApp", "1.0.0.0");
			WriteLibraryWithCulture(Path.Combine("app", "de"), "DnlibTestApp.resources", "de");
			WriteLibrary("", "DnlibTestApp.resources", "1.0.0.0");
			var resolver = CreateResolver();
			try {
				using (var app = ModuleDefMD.Load(appFile, resolver.DefaultModuleContext))
					Assert.Null(resolver.Resolve(CreateReference("DnlibTestApp.resources", "1.0.0.0", culture), app));
			}
			finally {
				DisposeAll(resolver);
			}
		}

		// A .NET Framework application can list sub directories with its assemblies in its config file
		// (App.exe.config, <probing privatePath="...">). Only directories below the application are used.
		[Fact]
		public void AssemblyIsFoundInAPrivatePathOfTheConfigFile() {
			var libraryFile = WriteLibrary(Path.Combine("app", "lib"), LibraryName, "1.0.0.0");
			WriteLibrary("outside", LibraryName + "Outside", "1.0.0.0");
			WriteLibrary(Path.Combine("app", "not-listed"), LibraryName + "NotListed", "1.0.0.0");
			var appFile = WriteLibrary("app", "DnlibTestApp", "1.0.0.0");
			File.WriteAllText(appFile + ".config", @"<?xml version=""1.0""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <probing privatePath=""missing; lib ;..\outside"" />
    </assemblyBinding>
  </runtime>
</configuration>");
			var resolver = CreateResolver();
			try {
				using (var app = ModuleDefMD.Load(appFile, resolver.DefaultModuleContext)) {
					var assembly = resolver.Resolve(CreateReference(LibraryName, "1.0.0.0"), app);

					Assert.NotNull(assembly);
					Assert.Equal(libraryFile, assembly.ManifestModule!.Location);
					Assert.Null(resolver.Resolve(CreateReference(LibraryName + "Outside", "1.0.0.0"), app));
					Assert.Null(resolver.Resolve(CreateReference(LibraryName + "NotListed", "1.0.0.0"), app));
				}
			}
			finally {
				DisposeAll(resolver);
			}
		}

		// The directory of the application is still used if the config file can't be read
		[Fact]
		public void ConfigFileThatIsNotXmlIsIgnored() {
			var libraryFile = WriteLibrary("app", LibraryName, "1.0.0.0");
			var appFile = WriteLibrary("app", "DnlibTestApp", "1.0.0.0");
			File.WriteAllText(appFile + ".config", "<configuration><not closed>");
			var resolver = CreateResolver();
			try {
				using (var app = ModuleDefMD.Load(appFile, resolver.DefaultModuleContext)) {
					var assembly = resolver.Resolve(CreateReference(LibraryName, "1.0.0.0"), app);

					Assert.NotNull(assembly);
					Assert.Equal(libraryFile, assembly.ManifestModule!.Location);
				}
			}
			finally {
				DisposeAll(resolver);
			}
		}

		// By default, another version of the assembly is good enough
		[Fact]
		public void AnotherVersionIsUsedUnlessAnExactMatchIsRequired() {
			var file = WriteLibrary("libs", LibraryName, "2.0.0.0");
			var resolver = CreateResolver(Path.GetDirectoryName(file)!);
			try {
				var assembly = resolver.Resolve(CreateReference(LibraryName, "1.0.0.0"), sourceModule: null);
				Assert.NotNull(assembly);
				Assert.Equal(new Version(2, 0, 0, 0), assembly.Version);
			}
			finally {
				DisposeAll(resolver);
			}

			var exactResolver = CreateResolver(Path.GetDirectoryName(file)!);
			exactResolver.FindExactMatch = true;
			try {
				Assert.Null(exactResolver.Resolve(CreateReference(LibraryName, "1.0.0.0"), sourceModule: null));
				Assert.NotNull(exactResolver.Resolve(CreateReference(LibraryName, "2.0.0.0"), sourceModule: null));
			}
			finally {
				DisposeAll(exactResolver);
			}
		}

		// Changed in this commit: a requested version number that is 65535 (or that the System.Version doesn't
		// have) has no value. The CLR takes an assembly with any number there and with any numbers after it, so
		// such an assembly is an exact match.
		[Theory]
		[InlineData("65535.65535.65535.65535", true)]
		[InlineData("65535.0.0.0", true)]
		[InlineData("1.65535.65535.65535", true)]
		[InlineData("1.2.65535.65535", true)]
		[InlineData("1.2", true)]
		[InlineData("1.2.65535.9", true)]
		[InlineData("1.2.3.65535", true)]
		[InlineData("1.2.3", true)]
		[InlineData("1.2.3.4", true)]
		[InlineData("2.65535.65535.65535", false)]
		[InlineData("1.3.65535.65535", false)]
		[InlineData("1.3", false)]
		[InlineData("1.2.4.65535", false)]
		[InlineData("1.2.3.5", false)]
		[InlineData("0.0.0.0", false)]
		public void VersionNumberWithoutValueMatchesAnyNumberExactly(string requestedVersion, bool expectedFound) {
			var file = WriteLibrary("libs", LibraryName, "1.2.3.4");
			var resolver = CreateResolver(Path.GetDirectoryName(file)!);
			resolver.FindExactMatch = true;
			try {
				var assembly = resolver.Resolve(CreateReference(LibraryName, requestedVersion), sourceModule: null);
				Assert.Equal(expectedFound ? "1.2.3.4" : null, assembly?.Version.ToString());
			}
			finally {
				DisposeAll(resolver);
			}
		}

		[Fact]
		public void VersionNumberWithoutValueMatchesACachedAssemblyExactly() {
			var resolver = CreateResolver();
			resolver.FindExactMatch = true;
			var assembly = new AssemblyDefUser(LibraryName, new Version(1, 2, 3, 4));
			resolver.AddToCache(assembly);

			Assert.Same(assembly, resolver.Resolve(CreateReference(LibraryName, "1.2.65535.65535"), sourceModule: null));
			Assert.Null(resolver.Resolve(CreateReference(LibraryName, "1.3.65535.65535"), sourceModule: null));
			Assert.Null(resolver.Resolve(CreateReference("Other", "1.2.65535.65535"), sourceModule: null));
		}

		// Fixed in this commit: a name without a version (null) was version 0.0.0.0, which no assembly matched
		// exactly. It's now a reference to any version.
		[Fact]
		public void NameWithoutAVersionMatchesAnyVersionExactly() {
			var resolver = CreateResolver();
			resolver.FindExactMatch = true;
			var assembly = new AssemblyDefUser(LibraryName, new Version(1, 2, 3, 4));
			resolver.AddToCache(assembly);
			var nameWithoutVersion = new AssemblyNameInfo { Name = LibraryName, Culture = "" };
			Assert.Null(nameWithoutVersion.Version);

			Assert.Same(assembly, resolver.Resolve(nameWithoutVersion, sourceModule: null));
			Assert.Null(resolver.Resolve(new AssemblyNameInfo { Name = "Other", Culture = "" }, sourceModule: null));
		}

		[Fact]
		public void TheVersionThatMatchesIsPreferred() {
			var oldFile = WriteLibrary("old", LibraryName, "1.0.0.0");
			var newFile = WriteLibrary("new", LibraryName, "2.0.0.0");
			var resolver = CreateResolver(Path.GetDirectoryName(oldFile)!, Path.GetDirectoryName(newFile)!);
			try {
				var assembly = resolver.Resolve(CreateReference(LibraryName, "2.0.0.0"), sourceModule: null);

				Assert.NotNull(assembly);
				Assert.Equal(newFile, assembly.ManifestModule!.Location);
			}
			finally {
				DisposeAll(resolver);
			}
		}

		[Fact]
		public void AssemblyAddedToTheCacheIsReturned() {
			var resolver = CreateResolver();
			var module = new ModuleDefUser("Cached.dll");
			var assembly = new AssemblyDefUser("DnlibTestCached", new Version(1, 0, 0, 0));
			assembly.Modules.Add(module);

			Assert.True(resolver.AddToCache(assembly));
			// It's already there
			Assert.True(resolver.AddToCache(module));
			// Another assembly with the same name isn't added
			var sameName = new AssemblyDefUser("DnlibTestCached", new Version(1, 0, 0, 0));
			sameName.Modules.Add(new ModuleDefUser("Cached.dll"));
			Assert.False(resolver.AddToCache(sameName));
			Assert.False(resolver.AddToCache((AssemblyDef?)null));

			Assert.Same(assembly, resolver.Resolve(CreateReference("DnlibTestCached", "1.0.0.0"), sourceModule: null));
			Assert.True(resolver.Remove(assembly));
			Assert.False(resolver.Remove(assembly));
			Assert.Null(resolver.Resolve(CreateReference("DnlibTestCached", "1.0.0.0"), sourceModule: null));
		}

		// Fixed in 239c0443: an assembly that was found for a reference with another version is in the cache
		// under both names. Remove() only removed the assembly's own name, so the reference still found it.
		[Fact]
		public void RemovedAssemblyIsNotFoundThroughAReferenceWithAnotherVersion() {
			var resolver = CreateResolver();
			var assembly = new AssemblyDefUser("DnlibTestCached", new Version(2, 0, 0, 0));
			assembly.Modules.Add(new ModuleDefUser("Cached.dll"));
			resolver.AddToCache(assembly);
			var otherVersion = CreateReference("DnlibTestCached", "1.0.0.0");
			Assert.Same(assembly, resolver.Resolve(otherVersion, sourceModule: null));

			Assert.True(resolver.Remove(assembly));

			Assert.Null(resolver.Resolve(otherVersion, sourceModule: null));
			Assert.DoesNotContain(resolver.GetCachedAssemblies(), a => a is not null);
		}

		[Fact]
		public void ClearRemovesAllAssemblies() {
			var resolver = CreateResolver();
			var assembly = new AssemblyDefUser("DnlibTestCached", new Version(1, 0, 0, 0));
			assembly.Modules.Add(new ModuleDefUser("Cached.dll"));
			resolver.AddToCache(assembly);

			resolver.Clear();

			Assert.Empty(resolver.GetCachedAssemblies());
			Assert.Null(resolver.Resolve(CreateReference("DnlibTestCached", "1.0.0.0"), sourceModule: null));
		}

		// A reference is resolved through the module's context
		[Fact]
		public void TypeRefIsResolvedThroughTheModulesAssemblyResolver() {
			var libraryFile = WriteLibrary("libs", LibraryName, "1.0.0.0");
			var resolver = CreateResolver(Path.GetDirectoryName(libraryFile)!);
			try {
				var module = new ModuleDefUser("User.dll") { Context = resolver.DefaultModuleContext };
				new AssemblyDefUser("DnlibTestUser").Modules.Add(module);
				var typeRef = new TypeRefUser(module, string.Empty, "LibraryClass", CreateReference(LibraryName, "1.0.0.0"));

				var typeDef = typeRef.Resolve();

				Assert.NotNull(typeDef);
				Assert.Equal(libraryFile, typeDef.Module!.Location);
				Assert.Null(new TypeRefUser(module, string.Empty, "MissingClass", CreateReference(LibraryName, "1.0.0.0")).Resolve());
			}
			finally {
				DisposeAll(resolver);
			}
		}

		// A file with the assembly's name that isn't a .NET file is skipped
		[Fact]
		public void AFileThatIsNotAnAssemblyIsSkipped() {
			var libraryDirectory = directory.GetPath("notanassembly");
			Directory.CreateDirectory(libraryDirectory);
			File.WriteAllBytes(Path.Combine(libraryDirectory, "DnlibTestNotAnAssembly.dll"), new byte[] { 1, 2, 3, 4 });
			var resolver = CreateResolver(libraryDirectory);

			Assert.Null(resolver.Resolve(CreateReference("DnlibTestNotAnAssembly", "1.0.0.0"), sourceModule: null));
		}

		sealed class PrivatePathsResolver : AssemblyResolver {
			public IEnumerable<string> GetPrivatePaths(ModuleDef? module) => GetModulePrivateSearchPaths(module);
		}

		// A module that wasn't read from a file, or that isn't in an assembly, has no private search paths
		[Fact]
		public void AModuleWithoutAFileOrAnAssemblyHasNoPrivateSearchPaths() {
			var resolver = new PrivatePathsResolver();
			var inAssembly = new ModuleDefUser("InAssembly.dll");
			new AssemblyDefUser("InAssembly").Modules.Add(inAssembly);

			Assert.Empty(resolver.GetPrivatePaths(null));
			Assert.Empty(resolver.GetPrivatePaths(new ModuleDefUser("NoAssembly.dll")));
			Assert.Empty(resolver.GetPrivatePaths(inAssembly));
		}
	}
}
