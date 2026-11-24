// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Resolving {
	/// <summary>
	/// <see cref="AssemblyResolver"/> also looks in the global assembly cache (GAC) of .NET Framework on Windows,
	/// or of Mono if the tests run on Mono. A GAC has a directory for every assembly, with a directory for every
	/// version, culture and public key token of it. The tests resolve Accessibility.dll, a small assembly of
	/// .NET Framework that .NET doesn't have, so it can't be found next to the runtime's files.
	/// </summary>
	public class GacTests {
		const string AssemblyInTheGac = "Accessibility, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

		static void SkipIfThereIsNoGac() =>
			Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || Type.GetType("Mono.Runtime") is not null,
				"Only Windows (.NET Framework) and Mono have a GAC");

		// Resolves a reference and returns the file that was found, or null
		static string? Resolve(AssemblyResolver resolver, string assemblyFullName) {
			resolver.DefaultModuleContext = new ModuleContext(resolver);
			try {
				var assembly = resolver.Resolve(new AssemblyNameInfo(assemblyFullName), sourceModule: null);
				if (assembly is null)
					return null;
				Assert.Equal("Accessibility", assembly.Name);
				return assembly.ManifestModule!.Location;
			}
			finally {
				// The resolver keeps the files open
				foreach (var module in resolver.GetCachedAssemblies().Distinct().SelectMany(a => a.Modules).ToList())
					module.Dispose();
				resolver.Clear();
			}
		}

		static bool IsInTheGac(string? file) =>
			file is not null && file.Replace('\\', '/').IndexOf("/gac", StringComparison.OrdinalIgnoreCase) >= 0;

		[Fact]
		public void AssemblyIsFoundInTheGac() {
			SkipIfThereIsNoGac();

			var file = Resolve(new AssemblyResolver(), AssemblyInTheGac);

			Assert.True(IsInTheGac(file), file);
			// The directory has the version and the public key token
			Assert.Contains("4.0.0.0__b03f5f7f11d50a3a", file);
		}

		[Fact]
		public void GacIsNotSearchedIfItIsDisabled() {
			SkipIfThereIsNoGac();

			var file = Resolve(new AssemblyResolver { UseGAC = false }, AssemblyInTheGac);

			// It's not found, or it's the file in the runtime's directory
			Assert.False(IsInTheGac(file), file);
		}

		// The GAC has no version 4.0.9.9: any version is returned, unless only that version is wanted
		[Theory, InlineData(false), InlineData(true)]
		public void OtherVersionInTheGacIsOnlyFoundWithoutFindExactMatch(bool findExactMatch) {
			SkipIfThereIsNoGac();
			var resolver = new AssemblyResolver { FindExactMatch = findExactMatch, EnableFrameworkRedirect = false };

			var file = Resolve(resolver, AssemblyInTheGac.Replace("4.0.0.0", "4.0.9.9"));

			if (findExactMatch)
				Assert.Null(file);
			else
				Assert.True(IsInTheGac(file), file);
		}

		[Fact]
		public void AssemblyWithAnotherPublicKeyTokenIsNotTheSameAssembly() {
			SkipIfThereIsNoGac();
			var resolver = new AssemblyResolver { FindExactMatch = true };

			Assert.Null(Resolve(resolver, AssemblyInTheGac.Replace("b03f5f7f11d50a3a", "0123456789abcdef")));
		}

		// A name that can't be a file name isn't found, and nothing throws
		[Theory]
		[InlineData("Bad\0Name")]
		[InlineData("Bad|<>\"Name")]
		[InlineData("")]
		public void InvalidNameIsNotFound(string name) {
			var assemblyName = new AssemblyNameInfo { Name = name, Version = new Version(4, 0, 0, 0), PublicKeyOrToken = new PublicKeyToken("b03f5f7f11d50a3a") };
			foreach (bool findExactMatch in new[] { false, true }) {
				var resolver = new AssemblyResolver { FindExactMatch = findExactMatch };
				resolver.DefaultModuleContext = new ModuleContext(resolver);

				Assert.Null(resolver.Resolve(assemblyName, sourceModule: null));
			}
		}
	}
}
