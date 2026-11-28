// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Resolving {
	/// <summary>
	/// What <see cref="AssemblyResolver"/> searches for: a reference to a .NET Framework assembly in a CLR 4.0
	/// module is redirected to the version of <see cref="AssemblyResolver.FrameworkRedirectVersion"/>, of the
	/// module's TargetFrameworkAttribute (<see cref="AssemblyResolver.UseTargetFrameworkForFrameworkRedirect"/>),
	/// or of .NET Framework 4.0/4.5. And the <see cref="IAssemblyResolver"/> extension methods that take other
	/// forms of a name.
	/// </summary>
	public class AssemblyResolverRedirectTests {
		const string MicrosoftToken = "b03f5f7f11d50a3a";

		// Remembers the names it was asked to search for, and finds nothing
		sealed class RecordingResolver : AssemblyResolver {
			public readonly List<IAssembly> Searched = new List<IAssembly>();

			public RecordingResolver() => UseGAC = false;

			protected override IEnumerable<string>? FindAssemblies(IAssembly assembly, ModuleDef? sourceModule, bool matchExactly) {
				Searched.Add(assembly);
				return null;
			}
		}

		static ModuleDefMD CompileWithTargetFramework(string targetFramework) {
			var source = $"[assembly: System.Runtime.Versioning.TargetFramework(\"{targetFramework}\")] public class C {{ }}";
			return ModuleDefMD.Load(CSharpCompiler.Compile(source).Image);
		}

		static AssemblyRefUser SystemRuntime40 => new AssemblyRefUser("System.Runtime", new Version(4, 0, 0, 0), new PublicKeyToken(MicrosoftToken));

		static Version? GetSearchedVersion(Action<RecordingResolver> configure, ModuleDef sourceModule) {
			var resolver = new RecordingResolver();
			configure(resolver);
			Assert.Null(resolver.Resolve(SystemRuntime40, sourceModule));
			return resolver.Searched.First().Version;
		}

		static Version? Expected(FrameworkRedirectVersion version) {
			Assert.True(FrameworkRedirect.TryApplyFrameworkRedirect(SystemRuntime40, version, out var redirected));
			return redirected.Version;
		}

		[Fact]
		public void TheFrameworkRedirectVersionIsUsedIfItIsSet() {
			using (var module = CompileWithTargetFramework(".NETFramework,Version=v4.5")) {
				var version = GetSearchedVersion(a => a.FrameworkRedirectVersion = FrameworkRedirectVersion.Net48, module);

				Assert.Equal(Expected(FrameworkRedirectVersion.Net48), version);
				Assert.NotEqual(Expected(FrameworkRedirectVersion.Net45), version);
			}
		}

		// A version older than 4.0 doesn't fit a CLR 4.0 module, the module's runtime version is used instead
		[Fact]
		public void AFrameworkRedirectVersionOlderThan40IsIgnored() {
			using (var module = CompileWithTargetFramework(".NETFramework,Version=v4.8")) {
				var withNet35 = GetSearchedVersion(a => a.FrameworkRedirectVersion = FrameworkRedirectVersion.Net35, module);
				var withoutVersion = GetSearchedVersion(a => { }, module);

				Assert.Equal(withoutVersion, withNet35);
				Assert.NotEqual(Expected(FrameworkRedirectVersion.Net48), withNet35);
			}
		}

		[Fact]
		public void TheTargetFrameworkIsUsedIfTheResolverIsToldTo() {
			using (var module = CompileWithTargetFramework(".NETFramework,Version=v4.8")) {
				var resolver = new RecordingResolver();
				Assert.Null(resolver.FrameworkRedirectVersion);
				Assert.False(resolver.UseTargetFrameworkForFrameworkRedirect);

				var version = GetSearchedVersion(a => a.UseTargetFrameworkForFrameworkRedirect = true, module);

				Assert.Equal(Expected(FrameworkRedirectVersion.Net48), version);
				Assert.NotEqual(GetSearchedVersion(a => { }, module), version);
			}
		}

		// The target framework of a .NET Core module isn't a .NET Framework version
		[Fact]
		public void ATargetFrameworkThatIsNotNetFrameworkIsIgnored() {
			using (var module = CompileWithTargetFramework(".NETCoreApp,Version=v8.0")) {
				module.RuntimeVersion = dnlib.DotNet.MD.MDHeaderRuntimeVersion.MS_CLR_40;

				var version = GetSearchedVersion(a => a.UseTargetFrameworkForFrameworkRedirect = true, module);

				Assert.Equal(GetSearchedVersion(a => { }, module), version);
			}
		}

		[Fact]
		public void NothingIsRedirectedIfRedirectsAreDisabled() {
			using (var module = CompileWithTargetFramework(".NETFramework,Version=v4.8")) {
				var version = GetSearchedVersion(a => { a.EnableFrameworkRedirect = false; a.FrameworkRedirectVersion = FrameworkRedirectVersion.Net48; }, module);

				Assert.Equal(new Version(4, 0, 0, 0), version);
			}
		}

		static AssemblyResolver CreateResolverWithCachedAssembly(out AssemblyDefUser assembly) {
			var resolver = new AssemblyResolver { UseGAC = false };
			assembly = new AssemblyDefUser("DnlibTestCached", new Version(1, 2, 3, 4));
			assembly.Modules.Add(new ModuleDefUser("DnlibTestCached.dll"));
			Assert.True(resolver.AddToCache(assembly));
			return resolver;
		}

		// System.Reflection.AssemblyName and a full name string are other forms of a reference
		[Fact]
		public void AnAssemblyNameOrAFullNameIsResolved() {
			var resolver = CreateResolverWithCachedAssembly(out var assembly);
			var assemblyName = new System.Reflection.AssemblyName("DnlibTestCached, Version=1.2.3.4");

			Assert.Same(assembly, resolver.Resolve(assemblyName, null));
			Assert.Same(assembly, resolver.Resolve("DnlibTestCached, Version=1.2.3.4", null));
			Assert.Same(assembly, resolver.ResolveOrThrow(assemblyName, null));
			Assert.Same(assembly, resolver.ResolveOrThrow("DnlibTestCached, Version=1.2.3.4", null));
			Assert.Same(assembly, resolver.ResolveOrThrow(new AssemblyRefUser(assembly), null));
			Assert.Null(resolver.Resolve((System.Reflection.AssemblyName?)null, null));
			Assert.Null(resolver.Resolve((string?)null, null));
			Assert.Null(resolver.ResolveOrThrow((System.Reflection.AssemblyName?)null, null));
			Assert.Null(resolver.ResolveOrThrow((string?)null, null));
			Assert.Null(resolver.ResolveOrThrow((IAssembly?)null, null));
#pragma warning disable CS0618 // Obsolete: the old names of the OrThrow methods
			Assert.Same(assembly, resolver.ResolveThrow(new AssemblyRefUser(assembly), null));
			Assert.Same(assembly, resolver.ResolveThrow(assemblyName, null));
			Assert.Same(assembly, resolver.ResolveThrow("DnlibTestCached, Version=1.2.3.4", null));
#pragma warning restore CS0618
		}

		[Fact]
		public void TheOrThrowMethodsThrowIfTheAssemblyIsNotFound() {
			var resolver = CreateResolverWithCachedAssembly(out _);

			var e1 = Assert.Throws<AssemblyResolveException>(() => resolver.ResolveOrThrow(new AssemblyRefUser("DnlibTestMissing"), null));
			var e2 = Assert.Throws<AssemblyResolveException>(() => resolver.ResolveOrThrow(new System.Reflection.AssemblyName("DnlibTestMissing"), null));
			var e3 = Assert.Throws<AssemblyResolveException>(() => resolver.ResolveOrThrow("DnlibTestMissing", null));

			Assert.Contains("DnlibTestMissing", e1.Message);
			Assert.Contains("DnlibTestMissing", e2.Message);
			Assert.Contains("DnlibTestMissing", e3.Message);
		}

		[Fact]
		public void AModuleRemovesItsAssemblyFromTheCache() {
			var resolver = CreateResolverWithCachedAssembly(out var assembly);

			Assert.False(resolver.Remove((ModuleDef?)null));
			Assert.False(resolver.Remove((AssemblyDef?)null));
			Assert.True(resolver.Remove(assembly.ManifestModule));

			Assert.Null(resolver.Resolve(new AssemblyRefUser(assembly), null));
		}

		// With FindExactMatch, only the assembly's own name is in the cache
		[Fact]
		public void AnAssemblyIsRemovedIfOnlyExactMatchesAreFound() {
			var resolver = CreateResolverWithCachedAssembly(out var assembly);
			resolver.FindExactMatch = true;

			Assert.True(resolver.Remove(assembly));
			Assert.False(resolver.Remove(assembly));
		}
	}
}
