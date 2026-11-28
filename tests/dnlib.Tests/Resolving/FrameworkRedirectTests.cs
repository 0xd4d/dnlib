// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Resolving {
	/// <summary>
	/// .NET Framework loads its own assemblies in the version that's installed, whatever version a reference
	/// has: a reference to mscorlib 2.0.0.0 is mscorlib 4.0.0.0 in .NET Framework 4. <see cref="FrameworkRedirect"/>
	/// has the tables with these versions.
	/// </summary>
	public class FrameworkRedirectTests {
		// The public key tokens of the .NET Framework assemblies
		const string EcmaToken = "b77a5c561934e089";
		const string MicrosoftToken = "b03f5f7f11d50a3a";

		static AssemblyRefUser CreateReference(string name, string version, string publicKeyToken, string culture = "") =>
			new AssemblyRefUser(name, new Version(version), new PublicKeyToken(publicKeyToken), culture);

		[Fact]
		public void OlderFrameworkAssemblyIsRedirectedToVersion4() {
			IAssembly assembly = CreateReference("mscorlib", "2.0.0.0", EcmaToken);

			FrameworkRedirect.ApplyFrameworkRedirect(ref assembly, FrameworkRedirectVersion.Net40);

			Assert.Equal("mscorlib", assembly.Name);
			Assert.Equal(new Version(4, 0, 0, 0), assembly.Version);
			Assert.Equal(EcmaToken, assembly.PublicKeyOrToken!.ToString());
		}

		[Fact]
		public void Version1AssemblyIsRedirectedToVersion2() {
			IAssembly assembly = CreateReference("mscorlib", "1.0.5000.0", EcmaToken);

			FrameworkRedirect.ApplyFrameworkRedirect(ref assembly, FrameworkRedirectVersion.Net20);

			Assert.Equal(new Version(2, 0, 0, 0), assembly.Version);
		}

		[Fact]
		public void ReferenceWithTheRightVersionIsNotChanged() {
			var reference = CreateReference("mscorlib", "4.0.0.0", EcmaToken);

			Assert.True(FrameworkRedirect.TryApplyFrameworkRedirect(reference, FrameworkRedirectVersion.Net40, out var redirected));
			Assert.Same(reference, redirected);
		}

		[Fact]
		public void OriginalReferenceIsNotChanged() {
			var reference = CreateReference("System.Core", "3.5.0.0", EcmaToken);

			Assert.True(FrameworkRedirect.TryApplyFrameworkRedirect(reference, FrameworkRedirectVersion.Net40, out var redirected));

			Assert.NotSame(reference, redirected);
			Assert.Equal(new Version(4, 0, 0, 0), redirected.Version);
			Assert.Equal(new Version(3, 5, 0, 0), reference.Version);
		}

		[Fact]
		public void OtherAssembliesAreNotRedirected() {
			// Not a .NET Framework assembly
			Assert.False(FrameworkRedirect.TryApplyFrameworkRedirect(CreateReference("MyLibrary", "1.0.0.0", EcmaToken), FrameworkRedirectVersion.Net40, out _));
			// The name of one, but another public key token
			Assert.False(FrameworkRedirect.TryApplyFrameworkRedirect(CreateReference("mscorlib", "2.0.0.0", "0123456789abcdef"), FrameworkRedirectVersion.Net40, out _));
			// A satellite assembly
			Assert.False(FrameworkRedirect.TryApplyFrameworkRedirect(CreateReference("mscorlib", "2.0.0.0", EcmaToken, "de"), FrameworkRedirectVersion.Net40, out _));
		}

		[Fact]
		public void NameIsComparedWithoutCase() =>
			Assert.True(FrameworkRedirect.TryApplyFrameworkRedirect(CreateReference("MSCORLIB", "2.0.0.0", EcmaToken), FrameworkRedirectVersion.Net40, out _));

		// The facade assemblies got higher versions in later .NET Framework versions
		[Fact]
		public void RedirectedVersionDependsOnTheFrameworkVersion() {
			var reference = CreateReference("System.Runtime", "4.0.0.0", MicrosoftToken);

			Assert.True(FrameworkRedirect.TryApplyFrameworkRedirect(reference, FrameworkRedirectVersion.Net451, out var net451));
			Assert.Equal(new Version(4, 0, 10, 0), net451.Version);
			Assert.True(FrameworkRedirect.TryApplyFrameworkRedirect(reference, FrameworkRedirectVersion.Net45, out var net45));
			Assert.True(net45.Version < net451.Version);
			Assert.True(FrameworkRedirect.TryApplyFrameworkRedirect(reference, FrameworkRedirectVersion.Net48, out var net48));
			Assert.True(net48.Version >= net451.Version);
		}

		static ModuleDefMD CompileWithTargetFramework(string? targetFramework) {
			var source = targetFramework is null ? "public class C { }" :
				$"[assembly: System.Runtime.Versioning.TargetFramework(\"{targetFramework}\")] public class C {{ }}";
			return ModuleDefMD.Load(CSharpCompiler.Compile(source).Image);
		}

		[Theory]
		[InlineData(".NETFramework,Version=v4.7.2", FrameworkRedirectVersion.Net472)]
		[InlineData(".NETFramework,Version=v4.5", FrameworkRedirectVersion.Net45)]
		[InlineData(".NETFramework,Version=v4.8.1", FrameworkRedirectVersion.Net481)]
		[InlineData(".NETFramework,Version=v3.5", FrameworkRedirectVersion.Net35)]
		[InlineData(".NETFramework,Version=v3.0", FrameworkRedirectVersion.Net30)]
		[InlineData(".NETFramework,Version=v2.0", FrameworkRedirectVersion.Net20)]
		// A version that isn't known is the newest known version below it
		[InlineData(".NETFramework,Version=v4.9", FrameworkRedirectVersion.Net481)]
		public void FrameworkVersionOfAModuleIsInItsTargetFrameworkAttribute(string targetFramework, FrameworkRedirectVersion expected) {
			using (var module = CompileWithTargetFramework(targetFramework)) {
				Assert.True(FrameworkRedirect.TryGetFrameworkRedirectVersion(module, out var version));
				Assert.Equal(expected, version);
			}
		}

		[Fact]
		public void ModuleForAnotherFrameworkHasNoFrameworkVersion() {
			using (var module = CompileWithTargetFramework(".NETCoreApp,Version=v8.0"))
				Assert.False(FrameworkRedirect.TryGetFrameworkRedirectVersion(module, out _));
			Assert.False(FrameworkRedirect.TryGetFrameworkRedirectVersion(null, out _));
		}

		// Without the attribute, the CLR version in the metadata header decides
		[Fact]
		public void ModuleWithoutTheAttributeUsesItsRuntimeVersion() {
			using (var module = CompileWithTargetFramework(null)) {
				Assert.True(module.IsClr40);
				Assert.True(FrameworkRedirect.TryGetFrameworkRedirectVersion(module, out var version));
				Assert.Equal(FrameworkRedirectVersion.Net40, version);

				module.RuntimeVersion = dnlib.DotNet.MD.MDHeaderRuntimeVersion.MS_CLR_20;
				Assert.True(FrameworkRedirect.TryGetFrameworkRedirectVersion(module, out version));
				Assert.Equal(FrameworkRedirectVersion.Net20, version);
			}
		}

		// With a module instead of a framework version, only the module's CLR version is used: the versions
		// of .NET Framework 4.0/4.5 for a CLR 4.0 module, and of 2.0-3.5 for a CLR 2.0 module
		[Fact]
		public void ReferenceIsRedirectedForTheModulesRuntimeVersion() {
			using (var module = CompileWithTargetFramework(".NETFramework,Version=v4.5.1")) {
				IAssembly reference = CreateReference("mscorlib", "2.0.0.0", EcmaToken);
				FrameworkRedirect.ApplyFrameworkRedirect(ref reference, module);
				Assert.Equal(new Version(4, 0, 0, 0), reference.Version);

				module.RuntimeVersion = dnlib.DotNet.MD.MDHeaderRuntimeVersion.MS_CLR_20;
				reference = CreateReference("mscorlib", "1.0.5000.0", EcmaToken);
				FrameworkRedirect.ApplyFrameworkRedirect(ref reference, module);
				Assert.Equal(new Version(2, 0, 0, 0), reference.Version);

				reference = CreateReference("mscorlib", "1.0.5000.0", EcmaToken);
				FrameworkRedirect.ApplyFrameworkRedirect(ref reference, (ModuleDef?)null);
				Assert.Equal(new Version(1, 0, 5000, 0), reference.Version);
			}
		}

		// Every .NET Framework 4 version has a table: System.Runtime has the version of that framework
		[Theory]
		[InlineData(FrameworkRedirectVersion.Net46)]
		[InlineData(FrameworkRedirectVersion.Net461)]
		[InlineData(FrameworkRedirectVersion.Net462)]
		[InlineData(FrameworkRedirectVersion.Net47)]
		[InlineData(FrameworkRedirectVersion.Net471)]
		public void EveryFrameworkVersionRedirectsSystemRuntime(FrameworkRedirectVersion version) {
			var reference = CreateReference("System.Runtime", "4.0.0.0", MicrosoftToken);

			Assert.True(FrameworkRedirect.TryApplyFrameworkRedirect(reference, version, out var redirected));
			Assert.True(FrameworkRedirect.TryApplyFrameworkRedirect(reference, FrameworkRedirectVersion.Net451, out var net451));
			Assert.True(redirected.Version >= net451.Version);
		}

		// The version of the source module's runtime, and the old methods for CLR 2.0 and CLR 4.0
		[Fact]
		public void ReferenceIsRedirectedForTheRuntimeOfTheSourceModule() {
			using (var module = CompileWithTargetFramework(".NETFramework,Version=v4.5.1")) {
				Assert.True(FrameworkRedirect.TryApplyFrameworkRedirect(CreateReference("mscorlib", "2.0.0.0", EcmaToken), module, out var redirected));
				Assert.Equal(new Version(4, 0, 0, 0), redirected.Version);
				module.RuntimeVersion = dnlib.DotNet.MD.MDHeaderRuntimeVersion.MS_CLR_20;
				Assert.True(FrameworkRedirect.TryApplyFrameworkRedirect(CreateReference("mscorlib", "1.0.5000.0", EcmaToken), module, out redirected));
				Assert.Equal(new Version(2, 0, 0, 0), redirected.Version);
				module.RuntimeVersion = "v1.0.3705";
				Assert.False(FrameworkRedirect.TryApplyFrameworkRedirect(CreateReference("mscorlib", "1.0.5000.0", EcmaToken), module, out _));
				Assert.False(FrameworkRedirect.TryApplyFrameworkRedirect(CreateReference("mscorlib", "1.0.5000.0", EcmaToken), (ModuleDef?)null, out _));
			}
#pragma warning disable CS0618 // Obsolete
			IAssembly v2 = CreateReference("mscorlib", "1.0.5000.0", EcmaToken);
			FrameworkRedirect.ApplyFrameworkRedirectV2(ref v2);
			Assert.Equal(new Version(2, 0, 0, 0), v2.Version);
			IAssembly v4 = CreateReference("mscorlib", "2.0.0.0", EcmaToken);
			FrameworkRedirect.ApplyFrameworkRedirectV4(ref v4);
			Assert.Equal(new Version(4, 0, 0, 0), v4.Version);
			Assert.True(FrameworkRedirect.TryApplyFrameworkRedirectV2(CreateReference("mscorlib", "1.0.5000.0", EcmaToken), out var redirectedV2));
			Assert.Equal(new Version(2, 0, 0, 0), redirectedV2.Version);
			Assert.True(FrameworkRedirect.TryApplyFrameworkRedirectV4(CreateReference("mscorlib", "2.0.0.0", EcmaToken), out var redirectedV4));
			Assert.Equal(new Version(4, 0, 0, 0), redirectedV4.Version);
#pragma warning restore CS0618
		}
	}
}
