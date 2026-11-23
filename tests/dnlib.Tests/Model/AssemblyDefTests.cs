// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// <see cref="AssemblyDef"/>: loading, names, finding types, friend assemblies and the target framework
	/// </summary>
	public class AssemblyDefTests {
		const string LibrarySource = @"
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

[assembly: AssemblyVersion(""1.2.3.4"")]
[assembly: InternalsVisibleTo(""Friend"")]
[assembly: TargetFramework("".NETFramework,Version=v4.6.1,Profile=Client"", FrameworkDisplayName = ""Some name"")]

namespace Ns {
	public class Outer {
		public class Inner { }
	}
}
";
		static readonly Lazy<byte[]> libraryImage = new Lazy<byte[]>(() =>
			CSharpCompiler.Compile(LibrarySource, new CSharpCompilerOptions { AssemblyName = "Library" }).Image);

		static AssemblyDef LoadLibrary() => AssemblyDef.Load(libraryImage.Value);

		[Fact]
		public void LoadReadsTheAssemblyOfAFile() {
			var fromBytes = AssemblyDef.Load(libraryImage.Value);
			var fromStream = AssemblyDef.Load(new MemoryStream(libraryImage.Value));

			Assert.Equal("Library", fromBytes.Name);
			Assert.Equal(new Version(1, 2, 3, 4), fromBytes.Version);
			Assert.Equal("Library", fromStream.Name);
			Assert.Same(fromBytes, fromBytes.ManifestModule!.Assembly);
			Assert.Same(fromBytes.ManifestModule, Assert.Single(fromBytes.Modules));
		}

		// A netmodule is a module without an assembly
		[Fact]
		public void LoadRejectsAFileWithoutAnAssembly() {
			var netModule = CSharpCompiler.Compile("public class C { }", new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.NetModule }).Image;

			Assert.Throws<BadImageFormatException>(() => AssemblyDef.Load(netModule));
			using (var module = ModuleDefMD.Load(netModule))
				Assert.Null(module.Assembly);
		}

		[Fact]
		public void FullNames() {
			var assembly = LoadLibrary();
			Assert.Equal("Library, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", assembly.FullName);
			Assert.Equal(assembly.FullName, assembly.GetFullNameWithPublicKeyToken());
			Assert.Equal("Library, Version=1.2.3.4, Culture=neutral, PublicKey=null", assembly.GetFullNameWithPublicKey());
			Assert.Equal(assembly.FullName, assembly.ToString());

			assembly.Culture = "de-DE";
			assembly.PublicKey = new PublicKey(new byte[] { 1, 2, 3 });
			Assert.Equal("Library, Version=1.2.3.4, Culture=de-DE, PublicKey=010203", assembly.GetFullNameWithPublicKey());
			Assert.EndsWith("PublicKeyToken=" + assembly.PublicKeyToken, assembly.FullName);
			Assert.Equal(16, assembly.PublicKeyToken!.ToString().Length);
		}

		[Fact]
		public void TypesAndModulesAreFoundByName() {
			var assembly = LoadLibrary();

			Assert.Equal("Ns.Outer", assembly.Find("Ns.Outer", isReflectionName: true)!.FullName);
			Assert.Equal("Ns.Outer/Inner", assembly.Find("Ns.Outer+Inner", isReflectionName: true)!.FullName);
			Assert.Equal("Ns.Outer/Inner", assembly.Find("Ns.Outer/Inner", isReflectionName: false)!.FullName);
			Assert.Null(assembly.Find("Ns.Missing", isReflectionName: true));

			var module = assembly.ManifestModule!;
			var typeRef = new TypeRefUser(module, "Ns", "Outer", module);
			Assert.Same(assembly.Find("Ns.Outer", isReflectionName: true), assembly.Find(typeRef));
			Assert.Null(assembly.Find((TypeRef?)null));

			Assert.Same(module, assembly.FindModule(module.Name));
			Assert.Same(module, assembly.FindModule(module.Name.String.ToUpperInvariant()));
			Assert.Null(assembly.FindModule("Missing.dll"));
			Assert.Null(assembly.FindModule(null));
		}

		static AssemblyDefUser CreateAssembly(string name) {
			var assembly = new AssemblyDefUser(name, new Version(1, 0, 0, 0));
			assembly.Modules.Add(new ModuleDefUser(name + ".dll"));
			return assembly;
		}

		// 'Friend' can use the internal types of 'Library': Library has InternalsVisibleTo("Friend")
		[Fact]
		public void FriendAssembly() {
			var library = LoadLibrary();

			Assert.True(CreateAssembly("Friend").IsFriendAssemblyOf(library));
			Assert.False(CreateAssembly("Stranger").IsFriendAssemblyOf(library));
			Assert.False(library.IsFriendAssemblyOf(CreateAssembly("Friend")));
			Assert.True(library.IsFriendAssemblyOf(library));
			Assert.False(library.IsFriendAssemblyOf(null));
		}

		// A signed assembly is only a friend if the attribute has its public key
		[Fact]
		public void SignedAssemblyIsNotAFriendOfAnUnsignedAssembly() {
			var signedFriend = CreateAssembly("Friend");
			signedFriend.PublicKey = new PublicKey(new byte[] { 1, 2, 3, 4 });

			Assert.False(signedFriend.IsFriendAssemblyOf(LoadLibrary()));
		}

		[Fact]
		public void TargetFrameworkAttributeIsParsed() {
			var library = LoadLibrary();

			Assert.True(library.TryGetOriginalTargetFrameworkAttribute(out var framework, out var version, out var profile));
			Assert.Equal(".NETFramework", framework);
			Assert.Equal(new Version(4, 6, 1, 0), version);
			Assert.Equal("Client", profile);
		}

		[Fact]
		public void AssemblyWithoutATargetFrameworkAttribute() {
			var assembly = AssemblyDef.Load(CSharpCompiler.Compile("public class C { }").Image);

			Assert.False(assembly.TryGetOriginalTargetFrameworkAttribute(out _, out _, out _));
			Assert.False(CreateAssembly("New").TryGetOriginalTargetFrameworkAttribute(out _, out _, out _));
		}

		[Fact]
		public void ProcessorArchitectureAndContentTypeAreBitsOfTheAttributes() {
			var assembly = CreateAssembly("New");
			Assert.True(assembly.IsProcessorArchitectureNone);
			Assert.True(assembly.IsContentTypeDefault);

			assembly.ProcessorArchitecture = AssemblyAttributes.PA_AMD64;
			assembly.ContentType = AssemblyAttributes.ContentType_WindowsRuntime;

			Assert.False(assembly.IsProcessorArchitectureNone);
			Assert.Equal(AssemblyAttributes.PA_AMD64, assembly.ProcessorArchitecture);
			Assert.True(assembly.IsContentTypeWindowsRuntime);
			Assert.Equal(AssemblyAttributes.PA_AMD64 | AssemblyAttributes.ContentType_WindowsRuntime, assembly.Attributes);

			assembly.ProcessorArchitecture = AssemblyAttributes.PA_MSIL;
			Assert.True(assembly.IsProcessorArchitectureMSIL);
			Assert.True(assembly.IsContentTypeWindowsRuntime);
		}

		[Fact]
		public void PublicKeyIsWritten() {
			var assembly = LoadLibrary();
			assembly.PublicKey = new PublicKey(new byte[] { 1, 2, 3, 4 });
			var stream = new MemoryStream();
			assembly.Write(stream);

			var written = AssemblyDef.Load(stream.ToArray());
			Assert.Equal(new byte[] { 1, 2, 3, 4 }, written.PublicKey.Data);
			Assert.Equal(assembly.PublicKeyToken!.ToString(), written.PublicKeyToken!.ToString());
		}
	}
}
